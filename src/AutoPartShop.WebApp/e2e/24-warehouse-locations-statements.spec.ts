import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, pickAutocompleteOption } from './utils/ui';

test.describe('Warehouse Locations & Account Statements', () => {
    test('create a warehouse location', async ({ page }) => {
        await page.goto('/inventory/warehouse-locations/create');

        await pickDropdownOption(page, page.locator('#warehouseId'), 'Malawoori', { typeToFilter: true });
        await page.locator('#zone').fill('A');
        // uniqueSuffix() is `Date.now().toString(36) + <4 random chars>` — the leading
        // chars are the slow-changing timestamp, so slice(0, N) barely varies between
        // close-together runs. slice(-4) takes the actual random tail instead.
        await page.locator('#aisle').fill(uniqueSuffix().slice(-4));
        await page.locator('#rack').fill('B1');
        await page.locator('#bin').fill('12');

        await page.getByRole('button', { name: 'Create Location' }).click();
        await expect(page).toHaveURL(/\/inventory\/warehouse-locations$/);
    });

    test('customer account summary renders real data for Walk-in Customer', async ({ page }) => {
        await page.goto('/sales/customer-account-summary');
        await expect(page.getByRole('heading', { name: 'Customer Account Summary' }).first()).toBeVisible();

        const customerSearch = page.getByPlaceholder('Search customer...');
        await pickAutocompleteOption(page, customerSearch, 'Walk', 'Walk-in Customer');

        await expect(page.getByText('Walk-in Customer', { exact: false }).first()).toBeVisible({ timeout: 10_000 });
    });

    test('supplier account summary renders real data', async ({ page }) => {
        await page.goto('/procurement/supplier-account-summary');
        await expect(page.getByRole('heading', { name: 'Supplier Account Summary' }).first()).toBeVisible();

        const supplierSearch = page.getByPlaceholder('Search supplier...');
        await pickAutocompleteOption(page, supplierSearch, 'E2E');
        await page.getByRole('button', { name: 'Generate', exact: true }).click();

        await expect(page.getByText(/transactions?/i).first()).toBeVisible({ timeout: 10_000 });
    });
});
