import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, expectToast } from './utils/ui';

/**
 * Registering a warranty requires the sold part itself to have been created with
 * "This product comes with warranty" enabled (confirmed via manual testing: selecting a
 * line item from a part without that flag fails server-side with "Part does not have
 * warranty") — so this part is seeded with warranty fields set, unlike every other spec's
 * part. The claim is filed as a REPLACEMENT (not REPAIR) so the quick-flow buttons walk
 * straight through Submit for Review -> Approve -> Complete -> Close without needing a
 * technician-assignment dialog in between.
 */
const partName = `E2E Warranty Part ${uniqueSuffix()}`;
let soNumber = '';

test.describe.serial('Warranty registration and claim', () => {
    test('0. seed a part with warranty enabled', async ({ page }) => {
        await page.goto('/inventory/parts/create');
        await page.getByPlaceholder(/brake pad/i).fill(partName);
        await pickDropdownOption(page, page.getByPlaceholder(/search category/i), 'Cabin', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search brand/i), 'XYG', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search unit/i).first(), 'Pieces', { typeToFilter: true });
        await page.getByPlaceholder('0.00').last().fill('150');
        await page.getByPlaceholder('0').last().fill('2');

        await page.getByLabel('This product comes with warranty').check();
        await page.getByPlaceholder('e.g. 12').fill('12');
        await page.locator('select[formcontrolname="warrantyType"]').selectOption('MANUFACTURER');

        await page.getByRole('button', { name: 'Create Part' }).first().click();
        await expect(page).toHaveURL(/\/inventory\/parts$/);
        await expect(page.getByText(partName).first()).toBeVisible();
    });

    test('1. receive stock so the part is sellable', async ({ page }) => {
        const supplierName = `E2E Warranty Supplier ${uniqueSuffix()}`;
        await page.goto('/inventory/suppliers/create');
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('Warranty Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`warranty-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000006');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 Warranty Road');
        await page.getByPlaceholder('e.g., Mumbai').fill('Dhaka');
        await page.getByPlaceholder('e.g., Maharashtra').fill('Dhaka');
        await page.getByPlaceholder('e.g., 400001').fill('1207');
        await page.getByRole('button', { name: 'Create Supplier' }).click();
        await expect(page).toHaveURL(/\/inventory\/suppliers$/);

        await page.goto('/procurement/purchase-orders/create');
        await pickDropdownOption(page, page.getByPlaceholder('Select supplier...'), supplierName, { typeToFilter: true });
        await page.getByPlaceholder('Select date...').click();
        await page.getByPlaceholder('Select date...').pressSequentially('2026-08-29', { delay: 20 });
        await page.keyboard.press('Escape');
        const productSearch = page.getByPlaceholder(/search product by name, sku/i);
        await productSearch.click();
        await productSearch.fill(partName);
        await page.getByText(partName, { exact: false }).first().click();
        const itemRow = page.locator('tr, .item-row', { hasText: partName }).last();
        await itemRow.getByRole('spinbutton').first().fill('5');
        await page.getByRole('button', { name: 'Create Order' }).click();
        await expect(page.getByText(/created successfully/i)).toBeVisible();
        const poCell = page.locator('a', { hasText: /^PO\d+$/ }).first();
        const poNumber = (await poCell.innerText()).trim();
        await poCell.click();
        await expect(page).toHaveURL(/\/procurement\/purchase-orders\/view/);
        await page.getByRole('button', { name: 'Submit', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/submitted successfully/i)).toBeVisible();
        await page.getByRole('button', { name: 'Confirm', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/confirmed successfully/i)).toBeVisible();

        await page.goto('/procurement/goods-receipts/create');
        const poField = page.getByPlaceholder('Select a purchase order...').last();
        await poField.click();
        await poField.fill(poNumber);
        await page.getByText(poNumber, { exact: false }).first().click();
        await pickDropdownOption(page, page.getByPlaceholder('Select warehouse...').last(), 'Malawoori', { typeToFilter: true });
        await page.getByLabel('No invoice provided by supplier').check();
        await page.getByRole('button', { name: 'Next: Items' }).click();
        await page.getByRole('button', { name: 'Next: Review' }).click();
        await page.getByRole('button', { name: 'Create Goods Receipt' }).click();
        await expect(page).toHaveURL(/\/procurement\/goods-receipts$/);
        const grnSearch = page.getByPlaceholder(/search by grn number, po number/i);
        await grnSearch.fill(poNumber);
        await grnSearch.press("Enter");
        const grnRow = page.locator('tr', { hasText: poNumber }).locator('a', { hasText: /^GRN\d+$/ });
        await expect(grnRow).toBeVisible({ timeout: 10_000 });
        await grnRow.click();
        await page.getByRole('button', { name: 'Verify Receipt' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/verified successfully/i)).toBeVisible();
        await page.getByRole('button', { name: 'Accept & Update Stock' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/accepted successfully/i)).toBeVisible();
    });

    test('2. sell the part through the POS', async ({ page }) => {
        await page.goto('/pos');
        const search = page.getByPlaceholder(/search products by name, sku/i);
        await search.fill(partName);
        await page.getByText(partName, { exact: false }).first().click();

        const customerSearch = page.getByPlaceholder(/search customer/i);
        await customerSearch.click();
        await customerSearch.fill('Walk');
        await page.getByText('Walk-in Customer', { exact: false }).first().click();

        const amountInput = page.getByPlaceholder(/enter amount/i);
        await amountInput.click();
        await amountInput.pressSequentially('150', { delay: 20 });
        await amountInput.locator('xpath=following::button[1]').click();
        await expect(page.getByText(/payment complete/i)).toBeVisible();

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().includes('/salesorder/quick-sale') && r.status() === 201, { timeout: 15_000 }),
            page.getByRole('button', { name: /Complete Sale/ }).click()
        ]);
        const body = await response.json();
        soNumber = body.salesOrderNumber;
        expect(soNumber).toMatch(/^SO\d+$/);
    });

    test('3. register a warranty for the sold part', async ({ page }) => {
        await page.goto('/warranty/registrations');
        await page.getByRole('button', { name: 'Register Warranty' }).click();

        const dialog = page.getByRole('dialog');
        await dialog.getByPlaceholder(/enter sales order/i).fill(soNumber);
        await dialog.getByRole('button', { name: 'Find' }).click();

        await expect(dialog.getByText('Order Details')).toBeVisible({ timeout: 10_000 });
        await pickDropdownOption(page, dialog.getByText('Select a part from this order...'), partName);

        await dialog.getByPlaceholder('Enter warranty terms and conditions...').fill('E2E test warranty terms');
        await dialog.getByRole('button', { name: 'Register Warranty' }).click();

        await expectToast(page, /registered successfully/i);
        await expect(page.getByText(partName).first()).toBeVisible();
    });

    test('4. file a replacement claim and walk it through to Closed', async ({ page }) => {
        await page.goto('/warranty/claims');
        await page.getByRole('button', { name: 'New Claim' }).click();

        const dialog = page.getByRole('dialog');
        const warrantySearch = dialog.getByPlaceholder(/search by warranty number, part, or customer/i);
        await warrantySearch.click();
        await warrantySearch.fill(partName);
        // The option's accessible name is just the warranty number (field="warrantyNumber"),
        // not the part name shown in its custom item template — match on visible text instead.
        const warrantyOption = page.getByText(partName, { exact: false }).first();
        await expect(warrantyOption).toBeVisible({ timeout: 10_000 });
        await warrantyOption.click();

        // The resolution-method p-select defaults to "REPAIR" (label "Repair Resolution"),
        // so its placeholder text never renders — open it by its current selected label.
        await pickDropdownOption(page, dialog.getByText('Repair Resolution', { exact: false }), 'Replacement Resolution');
        await dialog.getByPlaceholder('Describe the issue in detail...').fill('E2E test claim: item defective on arrival.');
        await dialog.getByRole('button', { name: 'Create Claim' }).click();

        await expectToast(page, /created successfully/i);

        const claimRow = page.locator('tr', { hasText: partName }).first();
        await expect(claimRow).toBeVisible({ timeout: 10_000 });

        await claimRow.getByRole('button', { name: 'Submit for Review' }).click();
        await expectToast(page, /submitted for review/i);
        await expect(claimRow.getByRole('button', { name: 'Approve' })).toBeVisible({ timeout: 10_000 });

        await claimRow.getByRole('button', { name: 'Approve' }).click();
        await expectToast(page, /approved/i);
        await expect(claimRow.getByRole('button', { name: 'Complete' })).toBeVisible({ timeout: 10_000 });

        await claimRow.getByRole('button', { name: 'Complete' }).click();
        await expectToast(page, /completed/i);
        await expect(claimRow.getByRole('button', { name: 'Close', exact: true })).toBeVisible({ timeout: 10_000 });

        await claimRow.getByRole('button', { name: 'Close', exact: true }).click();
        await expectToast(page, /closed/i);
        await expect(claimRow.getByText('Closed', { exact: false }).first()).toBeVisible({ timeout: 10_000 });
    });
});
