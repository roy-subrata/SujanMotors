import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';

/**
 * The admin session usually already has a till open (opened by earlier POS specs, or by
 * manual testing against this dev DB) — this spec opens one only if none is open, then
 * records a cash drop. It deliberately never closes the session: other specs' POS/quick-sale
 * flows depend on an open till, and closing here would break specs that run afterwards.
 */
test.describe('Till Sessions', () => {
    test('open till if needed, then record a cash drop', async ({ page }) => {
        await page.goto('/sales/till-sessions');

        const openTillForm = page.locator('.open-till-card');
        if (await openTillForm.isVisible().catch(() => false)) {
            const formInputs = openTillForm.locator('.till-form-grid input');
            await formInputs.nth(0).click();
            await formInputs.nth(0).fill(`E2E Terminal ${uniqueSuffix()}`);
            await formInputs.nth(1).fill('1000');

            await page.getByRole('button', { name: 'Open Till' }).click();
            await expect(page.getByText(/opened/i)).toBeVisible({ timeout: 10_000 });
        }

        await expect(page.getByRole('button', { name: 'Record Cash Drop' })).toBeVisible();
        const dropsCountBefore = await page.locator('.drops-table tbody tr').count().catch(() => 0);

        await page.getByRole('button', { name: 'Record Cash Drop' }).click();
        // The opening-float form (also placeholder "0.00") only renders when no session is
        // open, and a session is guaranteed open by this point, so this is unambiguous.
        await page.getByPlaceholder('0.00').fill('50');
        // The dialog's submit button's accessible name (role=button) proved flaky to match
        // reliably; its inner label span's exact text doesn't collide with the "Record Cash
        // Drop" trigger button (whose span reads "Record Cash Drop", not "Record").
        await page.getByText('Record', { exact: true }).click();

        await expect(page.getByText(/recorded/i).first()).toBeVisible({ timeout: 10_000 });
        await expect(page.locator('.drops-table tbody tr')).toHaveCount(dropsCountBefore + 1);
    });
});
