import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * Purchase -> receive -> return: a supplier return can only target a PO that is
 * CONFIRMED/PARTIAL/DELIVERED (see purchase-returns-form.component.ts loadPurchaseOrders),
 * so this spec drives a full purchase-and-receive cycle first (mirroring
 * 05-business-flow.spec.ts) before creating the return itself.
 */
const partName = `E2E PR Part ${uniqueSuffix()}`;
const supplierName = `E2E PR Supplier ${uniqueSuffix()}`;
let poNumber = '';

test.describe.serial('Purchase Returns', () => {
    test('0. seed a supplier', async ({ page }) => {
        await page.goto('/inventory/suppliers/create');
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('PR Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`pr-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000003');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 PR Road');
        await page.getByPlaceholder('e.g., Mumbai').fill('Dhaka');
        await page.getByPlaceholder('e.g., Maharashtra').fill('Dhaka');
        await page.getByPlaceholder('e.g., 400001').fill('1207');
        await page.getByRole('button', { name: 'Create Supplier' }).click();
        await expect(page).toHaveURL(/\/inventory\/suppliers$/);
        await expect(page.getByText(supplierName).first()).toBeVisible();
    });

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

    test('2. create, submit and confirm a purchase order for 10 units', async ({ page }) => {
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
        await itemRow.getByRole('spinbutton').first().fill('10');

        await page.getByRole('button', { name: 'Create Order' }).click();
        await expect(page.getByText(/created successfully/i)).toBeVisible();

        const poCell = page.locator('a', { hasText: /^PO\d+$/ }).first();
        poNumber = (await poCell.innerText()).trim();
        expect(poNumber).toMatch(/^PO\d+$/);

        await poCell.click();
        await expect(page).toHaveURL(/\/procurement\/purchase-orders\/view/);

        await page.getByRole('button', { name: 'Submit', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/submitted successfully/i)).toBeVisible();

        await page.getByRole('button', { name: 'Confirm', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/confirmed successfully/i)).toBeVisible();
    });

    test('3. receive the goods and accept into stock', async ({ page }) => {
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

    test('4. create a purchase return against the PO and it lands as PENDING', async ({ page }) => {
        await page.goto('/procurement/purchase-returns/create');

        const poField = page.getByPlaceholder('Select or search PO...').last();
        await poField.click();
        await poField.fill(poNumber);
        const poOption = page.getByRole('option', { name: poNumber, exact: false }).first();
        await expect(poOption).toBeVisible({ timeout: 10_000 });
        await poOption.click();

        // Return Date defaults to today (form init: returnDate: [new Date(), ...]) —
        // left untouched rather than typed, since typing into an already-filled datepicker
        // appends to the existing text instead of replacing it.

        await pickDropdownOption(page, page.getByPlaceholder('Select reason...'), 'Defective');

        // Pulls in every line from the selected PO with its full received quantity —
        // nothing further to fill for a single-line return.
        await page.getByRole('button', { name: 'Add All Items' }).click();

        await page.getByRole('button', { name: 'Create Return' }).click();

        await expect(page).toHaveURL(/\/procurement\/purchase-returns$/);
        await expect(page.getByText('PENDING').first()).toBeVisible();
    });
});
