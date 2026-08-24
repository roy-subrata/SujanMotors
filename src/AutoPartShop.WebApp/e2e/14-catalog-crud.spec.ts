import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * Lightweight create+verify coverage for the catalog/master-data CRUD pages: Categories,
 * Brands, Units, Discounts and Warehouses. These are simple single-dialog (or single-page)
 * forms with one clearly required field, so each test just creates a record and confirms
 * it shows up in its list — the create flow itself is the thing worth verifying, not every
 * optional field.
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
        await page.getByRole('button', { name: 'Add Category' }).last().click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });

    test('create a brand', async ({ page }) => {
        const name = `E2E Brand ${uniqueSuffix()}`;
        await page.goto('/inventory/brands');
        await page.getByRole('button', { name: 'Add Brand' }).click();
        await page.waitForTimeout(800);
        await page.locator('#c-name').fill(name);
        await page.getByRole('button', { name: 'Add Brand' }).last().click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });

    test('create a unit', async ({ page }) => {
        const suffix = uniqueSuffix();
        const name = `E2E Unit ${suffix}`;
        await page.goto('/inventory/units');
        await page.getByRole('button', { name: 'Add Unit' }).click();
        await page.waitForTimeout(800);
        await page.locator('#create-name').fill(name);
        await page.locator('#create-symbol').fill(suffix.slice(0, 5));
        await page.getByRole('button', { name: 'Create', exact: true }).click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
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

        await page.getByRole('button', { name: 'Create Discount' }).click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });

    test('create a warehouse', async ({ page }) => {
        const name = `E2E Warehouse ${uniqueSuffix()}`;
        await page.goto('/inventory/warehouses/create');
        await page.locator('#name').fill(name);
        await page.locator('#location').fill('456 E2E Storage Rd, Dhaka');

        await page.getByRole('button', { name: 'Create Warehouse' }).click();
        await expect(page).toHaveURL(/\/inventory\/warehouses$/);
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });
});
