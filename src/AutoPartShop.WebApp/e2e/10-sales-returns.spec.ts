import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, pickAutocompleteOption } from './utils/ui';

/**
 * Sell through the POS, then return one of the sold units. The sales-order number is
 * captured directly from the quick-sale API response (not scraped from the UI) so the
 * return form's own sales-order search can look it up unambiguously.
 */
const partName = `E2E SR Part ${uniqueSuffix()}`;
let soNumber = '';

test.describe.serial('Sales Returns', () => {
    test('1. seed a part', async ({ page }) => {
        await page.goto('/inventory/parts/create');
        await page.getByPlaceholder(/brake pad/i).fill(partName);
        await pickDropdownOption(page, page.getByPlaceholder(/search category/i), 'Cabin', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search brand/i), 'XYG', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search unit/i).first(), 'Pieces', { typeToFilter: true });
        await page.getByPlaceholder('0.00').last().fill('150');
        await page.getByPlaceholder('0').last().fill('2');
        await page.getByRole('button', { name: 'Create Part' }).first().click();
        await expect(page).toHaveURL(/\/inventory\/parts$/);
        await expect(page.getByText(partName).first()).toBeVisible();
    });

    test('2. receive stock so the part is sellable', async ({ page }) => {
        // Reuse the simplest path to sellable stock: a supplier + PO + GRN cycle
        // (same as 05-business-flow.spec.ts), scoped to this spec's own part.
        await page.goto('/inventory/suppliers/create');
        const supplierName = `E2E SR Supplier ${uniqueSuffix()}`;
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('SR Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`sr-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000004');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 SR Road');
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

    test('3. sell 3 units through the POS', async ({ page }) => {
        await page.goto('/pos');
        const search = page.getByPlaceholder(/search products by name, sku/i);
        await search.fill(partName);
        await page.getByText(partName, { exact: false }).first().click();

        const cartHeading = page.getByRole('heading', { name: partName });
        const qtyInput = cartHeading.locator('xpath=following::input[@type="number"][1]');
        await qtyInput.fill('3');
        await expect(qtyInput).toHaveValue('3');

        const customerSearch = page.getByPlaceholder(/search customer/i);
        await customerSearch.click();
        await customerSearch.fill('Walk');
        // The customer search backend has been observed taking up to ~11s under sustained
        // dev load (see pickAutocompleteOption's comment in e2e/utils/ui.ts) — the default
        // 15s action timeout on a bare .click() doesn't leave enough margin after the fill.
        const walkInOption = page.getByText('Walk-in Customer', { exact: false }).first();
        await expect(walkInOption).toBeVisible({ timeout: 25_000 });
        await walkInOption.click();

        const amountInput = page.getByPlaceholder(/enter amount/i);
        await amountInput.click();
        await amountInput.pressSequentially('450', { delay: 20 });
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

    test('4. return 1 of the 3 sold units', async ({ page }) => {
        await page.goto('/sales/sales-returns/create');

        const soSearch = page.getByPlaceholder('Search by order number or customer...');
        await pickAutocompleteOption(page, soSearch, soNumber);

        const whSearch = page.getByPlaceholder('Select warehouse...');
        await pickAutocompleteOption(page, whSearch, 'Malawoori');

        await page.getByText('Reason for Return', { exact: false }).locator('xpath=following::select[1]').selectOption('DAMAGED');

        const qtyInput = page.locator('input.input-qty').first();
        await qtyInput.fill('1');

        await page.getByRole('button', { name: 'Create Return' }).click();

        await expect(page).toHaveURL(/\/sales\/sales-returns$/);
        await expect(page.getByText('Pending').first()).toBeVisible();
    });
});
