import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

/**
 * Closes out the last pages with zero E2E coverage: Daily Expenses (procurement), Supplier
 * Performance, Company Profile and Backups (admin). Daily Expenses gets a real create-flow
 * test (same shape as 14-catalog-crud.spec.ts); the other three are read-only "loads with
 * real data" smoke checks — Backups deliberately never clicks "Backup Now"/"Restore" here,
 * since those trigger a real backup/restore against the shared dev DB every other spec
 * depends on, not something an automated smoke test should do.
 */
test.describe('Admin & procurement — remaining uncovered pages', () => {
    test('create a daily expense', async ({ page }) => {
        const description = `E2E Expense ${uniqueSuffix()}`;
        await page.goto('/procurement/daily-expenses');
        await page.getByRole('button', { name: 'Add Expense' }).click();

        // The category select's options are fetched from the backend, so pick whatever the
        // first one happens to be rather than a fixed label (pickDropdownOption needs a
        // known option label, which doesn't apply here). Scoped to .p-select-overlay —
        // an unscoped getByRole('option') can resolve to the page-size <select>'s native
        // <option> elements elsewhere on the page instead, which never become visible and
        // times out. The pick occasionally doesn't stick under load (same as the
        // payment-method p-select below) — retry once if the placeholder is still showing.
        const categoryTrigger = page.locator('#category');
        const categoryOption = page.locator('.p-select-overlay').getByRole('option').first();
        await categoryTrigger.click();
        await categoryOption.click();
        if (await page.getByText('Select category', { exact: false }).isVisible().catch(() => false)) {
            await categoryTrigger.click();
            await categoryOption.click();
        }

        await page.locator('#description').fill(description);
        // p-inputNumber's id lands on the wrapper, not the actual <input>.
        const amountInput = page.locator('#amount input');
        await amountInput.click();
        await amountInput.fill('500');

        await pickDropdownOption(page, page.locator('#paymentMethod'), 'Cash');
        if (await page.getByText('Select payment method', { exact: false }).isVisible().catch(() => false)) {
            await pickDropdownOption(page, page.locator('#paymentMethod'), 'Cash');
        }

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/daily-expense') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Save' }).click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('supplier performance report loads', async ({ page }) => {
        await page.goto('/procurement/supplier-performance');
        await expect(page.getByRole('heading', { name: 'Supplier Performance' }).first()).toBeVisible();
    });

    test('company profile settings load', async ({ page }) => {
        await page.goto('/admin/company-profile');
        await expect(page.getByRole('heading', { name: 'Company Profile' }).first()).toBeVisible();
    });

    test('backups page loads without triggering a backup or restore', async ({ page }) => {
        await page.goto('/admin/backups');
        await expect(page.getByRole('heading', { name: 'Backups' }).first()).toBeVisible();
    });
});
