import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';

test.describe('Supplier creation', () => {
    test('create a supplier and see it in the list', async ({ page }) => {
        const supplierName = `E2E Supplier ${uniqueSuffix()}`;

        await page.goto('/inventory/suppliers/create');

        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('Jane Doe');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`supplier-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801' + Date.now().toString().slice(-9));
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('123 Test Road');
        await page.getByPlaceholder('e.g., Mumbai').fill('Dhaka');
        await page.getByPlaceholder('e.g., Maharashtra').fill('Dhaka');
        await page.getByPlaceholder('e.g., 400001').fill('1207');

        await page.getByRole('button', { name: 'Create Supplier' }).click();

        await expect(page).toHaveURL(/\/inventory\/suppliers$/);
        await expect(page.getByText(supplierName).first()).toBeVisible();
    });

    test('required fields keep the submit button disabled', async ({ page }) => {
        await page.goto('/inventory/suppliers/create');
        await expect(page.getByRole('button', { name: 'Create Supplier' })).toBeDisabled();
    });
});
