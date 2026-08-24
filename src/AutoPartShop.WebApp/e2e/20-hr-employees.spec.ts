import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

test.describe('HR — Employees', () => {
    test('create an employee', async ({ page }) => {
        const name = `E2E Employee ${uniqueSuffix()}`;
        await page.goto('/hr/employees/create');

        await page.getByPlaceholder('e.g., Abdul Karim').fill(name);
        await page.getByPlaceholder('e.g., +8801716625369').fill('017' + Date.now().toString().slice(-8));
        await page.getByPlaceholder(/Salesperson, Cashier, Storekeeper/i).fill('E2E Test Role');
        await pickDropdownOption(page, page.getByText('Select department', { exact: false }), 'Sales');
        await page.locator('p-inputnumber input').first().fill('25000');

        await page.getByRole('button', { name: 'Create Employee' }).click();
        await expect(page).toHaveURL(/\/hr\/employees$/);
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });
});
