import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, pickAutocompleteOption } from './utils/ui';

/**
 * Drives a Sales Order through its full lifecycle via the standalone sales-order-form
 * (not the POS quick-sale used elsewhere): Create -> Confirm -> Generate Proforma (list row
 * action) -> Ready for Delivery -> Generate Challan -> Deliver. This is the same order that
 * feeds both the Proforma Invoices and Challans / Pending Deliveries lists, so one flow
 * exercises all of them.
 */
const partName = `E2E SO Part ${uniqueSuffix()}`;
const customerLastName = `SOFlow ${uniqueSuffix()}`;
const customerPhone = '019' + Date.now().toString().slice(-8);
let soId = '';

test.describe.serial('Sales Order lifecycle (Proforma, Challan, Delivery)', () => {
    test('0. seed a customer', async ({ page }) => {
        await page.goto('/sales/customers/create');
        await page.getByPlaceholder('e.g., John').fill('E2E');
        await page.getByPlaceholder('e.g., Doe').fill(customerLastName);
        await page.getByPlaceholder('e.g., +8801716625369').fill(customerPhone);
        // The sales-order form's customerEmail and customerCity fields are required
        // (auto-filled from the customer record but still must be non-empty), so this
        // customer needs both set, unlike the plain 04-customer.spec.ts smoke test.
        await page.getByPlaceholder('e.g., customer@example.com').fill(`so-flow-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., Tangail').fill('Dhaka');
        await page.getByRole('button', { name: 'Create Customer' }).click();
        await expect(page).toHaveURL(/\/sales\/customers$/);
    });

    test('1. seed a part with stock', async ({ page }) => {
        await page.goto('/inventory/parts/create');
        await page.getByPlaceholder(/brake pad/i).fill(partName);
        await pickDropdownOption(page, page.getByPlaceholder(/search category/i), 'Cabin', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search brand/i), 'XYG', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search unit/i).first(), 'Pieces', { typeToFilter: true });
        await page.getByPlaceholder('0.00').last().fill('150');
        await page.getByPlaceholder('0').last().fill('2');
        await page.getByRole('button', { name: 'Create Part' }).first().click();
        await expect(page).toHaveURL(/\/inventory\/parts$/);

        const supplierName = `E2E SO Supplier ${uniqueSuffix()}`;
        await page.goto('/inventory/suppliers/create');
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('SO Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`so-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000007');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 SO Road');
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

    test('2. create a sales order via the full form', async ({ page }) => {
        await page.goto('/sales/sales-orders/create');

        const customerSearch = page.getByPlaceholder('Search by name, email, or customer code...');
        await pickAutocompleteOption(page, customerSearch, customerLastName);

        await pickDropdownOption(page, page.getByText('Select warehouse', { exact: false }), 'Malawoori');

        const deliveryDate = page.getByPlaceholder('Select date...').last();
        await deliveryDate.click();
        await deliveryDate.pressSequentially('2026-08-29', { delay: 20 });
        await page.getByRole('heading', { name: 'Create Sales Order' }).click();

        const partSearch = page.getByPlaceholder('Search part by name, code, or SKU...');
        await pickAutocompleteOption(page, partSearch, partName);

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/salesorder') && r.request().method() === 'POST' && r.status() === 201),
            page.getByRole('button', { name: 'Create', exact: true }).click()
        ]);
        const body = await response.json();
        soId = body.id;
        expect(soId).toBeTruthy();
        await expect(page).toHaveURL(/\/sales\/sales-orders$/);
    });

    test('3. confirm the order', async ({ page }) => {
        await page.goto(`/sales/sales-orders/view?id=${soId}&mode=view`);
        await page.getByRole('button', { name: 'Confirm Order' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Ready for Delivery' })).toBeVisible({ timeout: 10_000 });
    });

    test('4. generate a proforma invoice for the order', async ({ page }) => {
        await page.goto('/sales/sales-orders');
        await page.getByPlaceholder(/search/i).first().fill(customerLastName);
        await page.waitForTimeout(500);

        // The list renders both a desktop <table> and a mobile card layout in the DOM
        // simultaneously (CSS toggles which is visible) — a bare "tr" locator can match a
        // row in the hidden one, whose action button clicks without effect. Scope to the
        // actual table body.
        const row = page.locator('table tbody tr', { hasText: customerLastName }).first();
        await expect(row).toBeVisible({ timeout: 10_000 });

        // This popup menu proved flaky: some background poll appears to periodically
        // rebuild the menu's DOM while it's open, so it sometimes fails to open on the
        // first click, or gets torn down between becoming visible and being clicked.
        // Retry the whole open-then-click sequence with a short per-attempt timeout.
        const menuItem = page.getByText('Generate Proforma', { exact: true });
        let opened = false;
        for (let attempt = 0; attempt < 8 && !opened; attempt++) {
            await row.getByRole('button').last().click();
            try {
                await menuItem.click({ timeout: 1_500 });
                opened = true;
            } catch {
                await page.keyboard.press('Escape').catch(() => {});
            }
        }
        expect(opened).toBe(true);

        await expect(page.getByText('Generate Proforma Invoice')).toBeVisible({ timeout: 10_000 });
        // The Valid Until datepicker's calendar overlay auto-opens and covers the Generate
        // button beneath it; Escape closes the whole dialog here rather than just the
        // calendar, and clicking elsewhere can reopen it. Force the click through instead —
        // the button is the real target, just visually obscured.
        await page.getByText('Generate', { exact: true }).click({ force: true });
        await expect(page.getByText('Generate Proforma Invoice')).not.toBeVisible({ timeout: 10_000 });
    });

    test('5. mark ready for delivery and generate a challan', async ({ page }) => {
        await page.goto(`/sales/sales-orders/view?id=${soId}&mode=view`);
        await page.getByRole('button', { name: 'Ready for Delivery' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Generate Challan' })).toBeVisible({ timeout: 10_000 });

        await page.getByRole('button', { name: 'Generate Challan' }).click();
        await page.getByRole('button', { name: 'Generate & Print' }).click();
        await expect(page.getByRole('button', { name: 'Deliver Now' })).toBeVisible({ timeout: 10_000 });
    });

    test('6. deliver the order', async ({ page }) => {
        await page.goto(`/sales/sales-orders/view?id=${soId}&mode=view`);
        await page.getByRole('button', { name: 'Deliver Now' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        // Once delivered, none of the lifecycle action buttons remain.
        await expect(page.getByRole('button', { name: 'Confirm Order' })).not.toBeVisible({ timeout: 10_000 });
        await expect(page.getByRole('button', { name: 'Deliver Now' })).not.toBeVisible({ timeout: 10_000 });
    });
});
