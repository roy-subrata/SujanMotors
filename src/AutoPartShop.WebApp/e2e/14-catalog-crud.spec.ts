import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * Lightweight create+verify coverage for the catalog/master-data CRUD pages: Categories,
 * Brands, Units, Discounts and Warehouses. These are simple single-dialog (or single-page)
 * forms with one clearly required field, so each test just creates a record and confirms
 * it shows up in its list — the create flow itself is the thing worth verifying, not every
 * optional field.
 *
 * These lists aren't sorted newest-first and their search boxes only filter on Enter, so
 * asserting "visible in the default list view" gets flaky once enough records have piled
 * up from repeated runs (the new record lands on page 2+). Asserting on the create
 * response status is what these tests actually care about (the record persisted) and
 * isn't sensitive to list pagination/sorting.
 */
test.describe('Catalog master-data CRUD', () => {
    test('create a category', async ({ page }) => {
        const name = `E2E Category ${uniqueSuffix()}`;
        await page.goto('/inventory/categories');
        await page.getByRole('button', { name: 'Add Category' }).click();
        // The dialog's (onShow) handler calls createForm.reset(...) once its open animation
        // finishes — filling immediately after the click races that reset and gets wiped.
        await page.waitForTimeout(800);
        await page.locator('#c-name').fill(name);
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/categories') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Add Category' }).last().click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('create a brand', async ({ page }) => {
        const name = `E2E Brand ${uniqueSuffix()}`;
        await page.goto('/inventory/brands');
        await page.getByRole('button', { name: 'Add Brand' }).click();
        await page.waitForTimeout(800);
        await page.locator('#c-name').fill(name);
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/brands') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Add Brand' }).last().click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('create a unit', async ({ page }) => {
        const suffix = uniqueSuffix();
        const name = `E2E Unit ${suffix}`;
        await page.goto('/inventory/units');
        await page.getByRole('button', { name: 'Add Unit' }).click();
        await page.waitForTimeout(800);
        await page.locator('#create-name').fill(name);
        // uniqueSuffix() is timestamp-prefixed; slice(0, N) barely varies between
        // close-together runs, so take the actual random tail instead.
        await page.locator('#create-symbol').fill(suffix.slice(-5));
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/units') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Create', exact: true }).click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('create a cart-level discount', async ({ page }) => {
        const name = `E2E Discount ${uniqueSuffix()}`;
        await page.goto('/inventory/discounts');
        await page.getByRole('button', { name: 'Add Discount' }).click();
        await page.waitForTimeout(800);

        await page.locator('#createName').fill(name);
        // The type p-select defaults to PERCENTAGE ("Percentage (%)"), so its placeholder
        // text never renders — open it by its current selected label instead.
        await pickDropdownOption(page, page.getByText('Percentage (%)', { exact: false }), 'Fixed Amount');
        await page.locator('#createValue').fill('50');

        // Typing into this datepicker updates the visible input text but doesn't reliably
        // clear the reactive form control's "required" state — clicking the calendar day
        // cell (the picker's own supported interaction) does.
        const startDate = page.locator('#createStartDate');
        await startDate.click();
        await page.getByRole('gridcell', { name: '23', exact: true }).click();

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/discounts') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Create Discount' }).click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('create a warehouse', async ({ page }) => {
        const name = `E2E Warehouse ${uniqueSuffix()}`;
        await page.goto('/inventory/warehouses/create');
        await page.locator('#name').fill(name);
        await page.locator('#location').fill('456 E2E Storage Rd, Dhaka');

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/warehouses') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Create Warehouse' }).click()
        ]);
        expect(response.ok()).toBe(true);
        await expect(page).toHaveURL(/\/inventory\/warehouses$/);
    });
});
