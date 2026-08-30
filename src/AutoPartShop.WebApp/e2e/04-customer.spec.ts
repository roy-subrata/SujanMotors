import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';

test.describe('Customer creation', () => {
    test('create a customer and see it in the list', async ({ page }) => {
        const firstName = 'E2E';
        const lastName = `Cust${uniqueSuffix()}`;
        // Bangladeshi mobile format the phone-uniqueness check expects; unique per run.
        const phone = '017' + Date.now().toString().slice(-8);

        await page.goto('/sales/customers/create');

        await page.getByPlaceholder('e.g., John').fill(firstName);
        await page.getByPlaceholder('e.g., Doe').fill(lastName);
        await page.getByPlaceholder('e.g., +8801716625369').fill(phone);

        await page.getByRole('button', { name: 'Create Customer' }).click();

        await expect(page).toHaveURL(/\/sales\/customers$/);
        await expect(page.getByText(`${firstName} ${lastName}`).first()).toBeVisible();
    });

    test('duplicate phone number is rejected', async ({ page }) => {
        const phone = '018' + Date.now().toString().slice(-8);

        for (const [first, last] of [['Dup', 'One'], ['Dup', 'Two']]) {
            await page.goto('/sales/customers/create');
            await page.getByPlaceholder('e.g., John').fill(first);
            await page.getByPlaceholder('e.g., Doe').fill(last);
            await page.getByPlaceholder('e.g., +8801716625369').fill(phone);
            await page.getByRole('button', { name: 'Create Customer' }).click();
        }

        // Second attempt with the same phone must not silently succeed. The UI only
        // surfaces a generic "Failed to create customer" here — the specific reason
        // (phone already in use) is in the API response but the form doesn't display it.
        await expect(page).toHaveURL(/\/sales\/customers\/create/);
        await expect(page.getByText(/failed to create customer/i).first()).toBeVisible();
    });
});
