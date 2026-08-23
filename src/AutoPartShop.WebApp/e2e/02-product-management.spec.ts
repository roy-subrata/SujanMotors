import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * Category "Cabin", brand "XYG" and unit "Pieces" are pre-existing master data
 * (seeded in an earlier session against this dev DB) — reused here rather than
 * re-created, since creating them isn't the workflow under test.
 */
const partName = `E2E Part ${uniqueSuffix()}`;
const updatedName = `${partName} (Updated)`;

test.describe('Product (Part) management', () => {
    test.describe.configure({ mode: 'serial' });

    test('create a new part', async ({ page }) => {
        await page.goto('/inventory/parts/create');

        await page.getByPlaceholder(/brake pad/i).fill(partName);
        await pickDropdownOption(page, page.getByPlaceholder(/search category/i), 'Cabin', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search brand/i), 'XYG', { typeToFilter: true });
        await pickDropdownOption(page, page.getByPlaceholder(/search unit/i).first(), 'Pieces', { typeToFilter: true });

        // p-inputnumber renders the placeholder on both its wrapper element and the real
        // <input> inside it; .last() lands on the actual input.
        await page.getByPlaceholder('0.00').last().fill('199.50');
        await page.getByPlaceholder('0').last().fill('3');

        await page.getByRole('button', { name: 'Create Part' }).first().click();

        await expect(page).toHaveURL(/\/inventory\/parts$/);
        await expect(page.getByText(partName).first()).toBeVisible();
    });

    test('search finds the part by name and by SKU', async ({ page }) => {
        await page.goto('/inventory/parts');
        const search = page.getByPlaceholder('Search by part name, SKU, or part number...');

        await search.fill(partName);
        await search.press('Enter');
        await page.waitForTimeout(500); // debounce
        await expect(page.getByText(partName).first()).toBeVisible();
        const skuText = await page.locator('.order-card, tr', { hasText: partName }).first().innerText();
        const skuMatch = skuText.match(/SKU\d+/);

        await search.fill('zzz-no-such-part-zzz');
        await search.press('Enter');
        await page.waitForTimeout(500);
        await expect(page.getByText(partName).first()).not.toBeVisible();

        if (skuMatch) {
            await search.fill(skuMatch[0]);
            await search.press('Enter');
            await page.waitForTimeout(500);
            await expect(page.getByText(partName).first()).toBeVisible();
        }
    });

    test('edit updates the part name and price', async ({ page }) => {
        await page.goto('/inventory/parts');
        const search = page.getByPlaceholder('Search by part name, SKU, or part number...');
        await search.fill(partName);
        await search.press('Enter');
        await page.waitForTimeout(500);

        const row = page.locator('.order-card, tr', { hasText: partName }).first();
        await row.getByRole('button').last().click(); // "more" action trigger
        await page.getByText('Edit', { exact: true }).click();

        await expect(page).toHaveURL(/\/inventory\/parts\/edit/);
        const nameInput = page.getByPlaceholder(/brake pad/i);
        await nameInput.fill('');
        await nameInput.fill(updatedName);
        await page.getByPlaceholder('0.00').last().fill('249.00');

        await page.getByRole('button', { name: /save|update/i }).first().click();

        await expect(page).toHaveURL(/\/inventory\/parts$/);
        await expect(page.getByText(updatedName).first()).toBeVisible();
    });
});
