import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, pickAutocompleteOption } from './utils/ui';

/**
 * Quotations are a separate standalone entity (not the legacy /api/v1/quotes
 * "Save as Quotation" shortcut) with their own create form — customer + quick-add
 * line items, same shape as the Sales Order form. A quotation doesn't consume stock,
 * so the part just needs to exist (no purchase/receive cycle needed first).
 */
test.describe('Quotations', () => {
    test('create a quotation', async ({ page }) => {
        const customerLastName = `Quote ${uniqueSuffix()}`;
        await page.goto('/sales/customers/create');
        await page.getByPlaceholder('e.g., John').fill('E2E');
        await page.getByPlaceholder('e.g., Doe').fill(customerLastName);
        await page.getByPlaceholder('e.g., +8801716625369').fill('016' + Date.now().toString().slice(-8));
        // The quotation form's customerEmail field is required (auto-filled from the
        // customer record but still must be non-empty).
        await page.getByPlaceholder('e.g., customer@example.com').fill(`quote-${uniqueSuffix()}@test.com`);
        await page.getByRole('button', { name: 'Create Customer' }).click();
        await expect(page).toHaveURL(/\/sales\/customers$/);

        const partName = `E2E Quote Part ${uniqueSuffix()}`;
        await page.goto('/inventory/parts/create');
        await page.getByPlaceholder(/brake pad/i).fill(partName);
        await pickDropdownOption(page, page.getByPlaceholder(/search category/i), 'Cabin', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search brand/i), 'XYG', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search unit/i).first(), 'Pieces', { typeToFilter: true });
        await page.getByPlaceholder('0.00').last().fill('150');
        await page.getByPlaceholder('0').last().fill('2');
        await page.getByRole('button', { name: 'Create Part' }).first().click();
        await expect(page).toHaveURL(/\/inventory\/parts$/);

        await page.goto('/sales/quotations/create');
        const customerSearch = page.getByPlaceholder('Search by name, email, or customer code...');
        await pickAutocompleteOption(page, customerSearch, customerLastName);

        const partSearch = page.getByPlaceholder(/search part by name, code, or sku/i);
        await pickAutocompleteOption(page, partSearch, partName);

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().includes('/api/v1/quotations') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Create Quotation' }).click()
        ]);
        expect(response.ok()).toBe(true);
    });
});
