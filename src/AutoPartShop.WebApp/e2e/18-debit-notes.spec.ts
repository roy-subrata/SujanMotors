import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickAutocompleteOption } from './utils/ui';

/**
 * Standalone flat balance-owed adjustment against a customer — unlike Proforma Invoices,
 * this has its own create form (not generated from an existing order).
 */
test.describe('Debit Notes', () => {
    test('create a debit note for the Walk-in customer', async ({ page }) => {
        await page.goto('/sales/debit-notes/create');

        const customerSearch = page.getByPlaceholder('Search by name, email, or customer code...');
        await pickAutocompleteOption(page, customerSearch, 'Walk', 'Walk-in Customer');

        await page.locator('p-inputnumber input, input[placeholder="0.00"]').first().fill('250');
        const reason = `E2E freight correction ${uniqueSuffix()}`;
        await page.getByPlaceholder(/undercharged on invoice/i).fill(reason);

        await page.getByRole('button', { name: 'Create', exact: true }).click();
        await expect(page).toHaveURL(/\/sales\/debit-notes$/);
        await expect(page.getByText(reason, { exact: false }).first()).toBeVisible({ timeout: 10_000 });
    });
});
