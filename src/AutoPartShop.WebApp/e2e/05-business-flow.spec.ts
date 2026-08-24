import { test, expect } from '@playwright/test';
import { execSync } from 'node:child_process';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * The centerpiece flow: Purchase 10 units -> stock +10 -> sell 3 -> stock 7 ->
 * refresh -> still 7 -> confirmed directly against the SQL Server database (not
 * just the API), so a caching bug in either layer can't hide a wrong answer.
 *
 * Runs as one serial scenario because each step's assertion depends on the
 * previous step's state (PO must be Confirmed before a GRN can reference it, etc).
 */
const partName = `E2E Flow Part ${uniqueSuffix()}`;
const supplierName = `E2E Flow Supplier ${uniqueSuffix()}`;
let poNumber = '';

function queryStockOnHand(partNameToFind: string): number {
    const sql = `SET NOCOUNT ON; SELECT sl.QuantityOnHand FROM StockLevels sl JOIN Parts p ON p.Id = sl.PartId WHERE p.Name = '${partNameToFind.replace(/'/g, "''")}';`;
    const out = execSync(
        `docker exec autopartshop.db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -C -No -d AutoPartShopDevDb -h -1 -Q "${sql}"`,
        { encoding: 'utf-8' }
    );
    const value = out.trim().split('\n')[0]?.trim();
    return Number(value);
}

test.describe.serial('Purchase -> stock +10 -> sale -> stock 7 -> refresh -> DB confirms', () => {
    test('0. seed a supplier for this flow', async ({ page }) => {
        await page.goto('/inventory/suppliers/create');
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('Flow Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`flow-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000002');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 Flow Road');
        await page.getByPlaceholder('e.g., Mumbai').fill('Dhaka');
        await page.getByPlaceholder('e.g., Maharashtra').fill('Dhaka');
        await page.getByPlaceholder('e.g., 400001').fill('1207');
        await page.getByRole('button', { name: 'Create Supplier' }).click();
        await expect(page).toHaveURL(/\/inventory\/suppliers$/);
        await expect(page.getByText(supplierName).first()).toBeVisible();
    });

    test('1. seed a part for this flow', async ({ page }) => {
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

    test('2. stock starts at zero', async () => {
        // No StockLevel row exists yet for a part that has never been received — that's 0, not a query failure.
        const sql = `SET NOCOUNT ON; SELECT COUNT(*) FROM StockLevels sl JOIN Parts p ON p.Id = sl.PartId WHERE p.Name = '${partName.replace(/'/g, "''")}';`;
        const out = execSync(
            `docker exec autopartshop.db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -C -No -d AutoPartShopDevDb -h -1 -Q "${sql}"`,
            { encoding: 'utf-8' }
        );
        expect(Number(out.trim().split('\n')[0]?.trim())).toBe(0);
    });

    test('3. create, submit and confirm a purchase order for 10 units', async ({ page }) => {
        await page.goto('/procurement/purchase-orders/create');

        await pickDropdownOption(page, page.getByPlaceholder('Select supplier...'), supplierName, { typeToFilter: true });
        // PrimeNG's date combobox parses keystrokes as they arrive — .fill() sets the DOM
        // value directly and skips that parsing, leaving the form control empty.
        await page.getByPlaceholder('Select date...').click();
        await page.getByPlaceholder('Select date...').pressSequentially('2026-08-29', { delay: 20 });
        await page.keyboard.press('Escape');

        const productSearch = page.getByPlaceholder(/search product by name, sku/i);
        await productSearch.click();
        await productSearch.fill(partName);
        await page.getByText(partName, { exact: false }).first().click();

        // The added line's Quantity field defaults to 1; Unit Price is pre-filled from the
        // part's own selling price (non-zero), which is all Accept-into-stock requires later.
        const itemRow = page.locator('tr, .item-row', { hasText: partName }).last();
        await itemRow.getByRole('spinbutton').first().fill('10');

        await page.getByRole('button', { name: 'Create Order' }).click();

        await expect(page.getByText(/created successfully/i)).toBeVisible();
        const poCell = page.locator('a', { hasText: /^PO\d+$/ }).first();
        poNumber = (await poCell.innerText()).trim();
        expect(poNumber).toMatch(/^PO\d+$/);

        // Creating lands back on the list — open the PO's own view page, which is where
        // the Submit/Confirm workflow buttons actually live.
        await poCell.click();
        await expect(page).toHaveURL(/\/procurement\/purchase-orders\/view/);

        await page.getByRole('button', { name: 'Submit', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/submitted successfully/i)).toBeVisible();

        await page.getByRole('button', { name: 'Confirm', exact: true }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText(/confirmed successfully/i)).toBeVisible();
    });

    test('4. receive the goods and accept into stock', async ({ page }) => {
        await page.goto('/procurement/goods-receipts/create');

        const poField = page.getByPlaceholder('Select a purchase order...').last();
        await poField.click();
        await poField.fill(poNumber);
        await page.getByText(poNumber, { exact: false }).first().click();

        await pickDropdownOption(page, page.getByPlaceholder('Select warehouse...').last(), 'Malawoori', { typeToFilter: true });
        await page.getByLabel('No invoice provided by supplier').check();

        await page.getByRole('button', { name: 'Next: Items' }).click();
        // Receiving quantities are pre-filled with remaining quantities (10) and unit cost
        // carries over from the PO line — nothing to change here.
        await page.getByRole('button', { name: 'Next: Review' }).click();
        await page.getByRole('button', { name: 'Create Goods Receipt' }).click();

        await expect(page).toHaveURL(/\/procurement\/goods-receipts$/);
        // The list isn't sorted newest-first, and other GRNs (from earlier manual testing)
        // already exist — filter by this flow's own PO number and wait for that exact row
        // (not just "a row exists") so a slow debounce can't race us into the old one.
        const grnSearch = page.getByPlaceholder(/search by grn number, po number/i);
        await grnSearch.fill(poNumber);
        await grnSearch.press('Enter');
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

    test('5. stock shows +10 in the UI and in the database', async ({ page }) => {
        await page.goto('/inventory/stock');
        await page.getByPlaceholder(/search parts, sku/i).fill(partName);
        await page.waitForTimeout(500);
        await expect(page.locator('tr, .stock-row', { hasText: partName }).getByText('10', { exact: true }).first()).toBeVisible();

        expect(queryStockOnHand(partName)).toBe(10);
    });

    test('6. sell 3 units through the POS', async ({ page }) => {
        await page.goto('/pos');
        const search = page.getByPlaceholder(/search products by name, sku/i);
        await search.fill(partName);
        await page.getByText(partName, { exact: false }).first().click();

        // Cart item is added with quantity 1 in an <input type="number"> (exposed as role
        // "textbox", not "spinbutton", unlike the part form's p-inputnumber fields) — it's
        // the first type=number input following the item's own heading in document order.
        const cartHeading = page.getByRole('heading', { name: partName });
        const qtyInput = cartHeading.locator('xpath=following::input[@type="number"][1]');
        await qtyInput.fill('3');
        await expect(qtyInput).toHaveValue('3');

        const customerSearch = page.getByPlaceholder(/search customer/i);
        await customerSearch.click();
        await customerSearch.fill('Walk');
        await page.getByText('Walk-in Customer', { exact: false }).first().click();

        // Same PrimeNG keystroke-parsing gotcha as the date field — .fill() leaves the
        // "add payment line" button disabled; simulate real typing instead.
        const amountInput = page.getByPlaceholder(/enter amount/i);
        await amountInput.click();
        await amountInput.pressSequentially('450', { delay: 20 });
        // Icon-only "add payment line" button immediately after the amount field.
        await amountInput.locator('xpath=following::button[1]').click();
        await expect(page.getByText(/payment complete/i)).toBeVisible();

        await page.getByRole('button', { name: /Complete Sale/ }).click();
        // The sale posts to the API and returns before any print dialog opens; wait on that
        // response rather than a UI toast, since a browser print dialog can freeze the page.
        await page.waitForResponse((r) => r.url().includes('/salesorder/quick-sale') && r.status() === 201, { timeout: 15_000 });
    });

    test('7. stock shows 7, survives a refresh, and the database agrees', async ({ page }) => {
        await page.goto('/inventory/stock');
        await page.getByPlaceholder(/search parts, sku/i).fill(partName);
        await page.waitForTimeout(500);
        await expect(page.locator('tr, .stock-row', { hasText: partName }).getByText('7', { exact: true }).first()).toBeVisible();

        await page.reload();
        await page.getByPlaceholder(/search parts, sku/i).fill(partName);
        await page.waitForTimeout(500);
        await expect(page.locator('tr, .stock-row', { hasText: partName }).getByText('7', { exact: true }).first()).toBeVisible();

        expect(queryStockOnHand(partName)).toBe(7);
    });
});
