import { test, expect } from '@playwright/test';

/**
 * Generate -> Approve for the current month. Payroll runs are unique per month, so a
 * second run of this spec within the same calendar month can't generate a new one — it
 * falls back to opening whatever run already exists for this month instead.
 */
test.describe('HR — Payroll', () => {
    test('generate (or reuse) this month\'s payroll run and approve it', async ({ page }) => {
        await page.goto('/hr/payroll');
        await page.getByRole('button', { name: 'Generate Payroll' }).click();

        const dialog = page.getByRole('dialog');
        await dialog.getByRole('button', { name: 'Generate', exact: true }).click();

        const navigated = await page.waitForURL(/\/hr\/payroll\/view\?id=/, { timeout: 8_000 }).then(() => true).catch(() => false);
        if (!navigated) {
            // Already generated for this period — close the dialog and open the existing run.
            await page.keyboard.press('Escape').catch(() => {});
            await page.locator('table tbody tr').first().dblclick();
            await expect(page).toHaveURL(/\/hr\/payroll\/view\?id=/, { timeout: 10_000 });
        }

        if (await page.getByRole('button', { name: 'Approve', exact: true }).isVisible().catch(() => false)) {
            await page.getByRole('button', { name: 'Approve', exact: true }).click();
            await page.getByRole('button', { name: 'Yes', exact: true }).click();
            await expect(page.getByText('Payroll approved')).toBeVisible({ timeout: 10_000 });
        }
    });
});
