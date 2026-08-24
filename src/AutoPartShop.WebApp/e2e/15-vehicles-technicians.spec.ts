import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

test.describe('Vehicles & Technicians', () => {
    test('create a vehicle', async ({ page }) => {
        const model = `E2E Model ${uniqueSuffix()}`;
        await page.goto('/inventory/vehicles/create');

        await page.locator('#make').fill('E2E Motors');
        await page.locator('#model').fill(model);
        await page.locator('#year').fill('2024');
        await pickDropdownOption(page, page.locator('#engineType'), 'Petrol', { typeToFilter: true });

        // The vehicles list isn't sorted newest-first and paginates — with many vehicles
        // accumulated from repeated runs, list-visibility checks get flaky. The create
        // response status is what this test actually cares about.
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/vehicles') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Create', exact: true }).click()
        ]);
        expect(response.ok()).toBe(true);
        await expect(page).toHaveURL(/\/inventory\/vehicles$/);
    });

    test('create a technician', async ({ page }) => {
        const name = `E2E Technician ${uniqueSuffix()}`;
        await page.goto('/sales/technicians/create');

        await page.getByPlaceholder('e.g., John Doe').fill(name);
        await page.getByPlaceholder('e.g., +8801716625369').fill('+8801700000010');

        await page.getByRole('button', { name: 'Create Technician' }).click();
        await expect(page.getByText(/created successfully/i)).toBeVisible({ timeout: 10_000 });
    });
});
