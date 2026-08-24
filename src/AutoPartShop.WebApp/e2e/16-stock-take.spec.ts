import { test, expect } from '@playwright/test';
import { pickDropdownOption } from './utils/ui';

/**
 * Full snapshot -> count -> review -> approve lifecycle. The backend rejects
 * submitForReview with zero counted lines ("At least one line must be counted before
 * submitting for review") even though the confirm-dialog copy on the frontend only warns
 * about uncounted lines rather than blocking — so at least one line's Counted field must
 * be filled in first.
 *
 * Only one open (non-Completed/Cancelled) stock take is allowed per warehouse at a time,
 * so this reuses whatever an earlier run left open — in either Counting or Review status —
 * instead of failing to create a second one.
 */
test.describe('Stock Take', () => {
    test('walk a stock take through to Completed', async ({ page }) => {
        await page.goto('/inventory/stock-takes');
        await page.waitForTimeout(500);

        // Reused open stock takes accumulate a line per part created by every other spec run
        // against this warehouse (100+ lines after enough runs) — the detail GET and the
        // table render of that many rows can take longer than the default 15s action
        // timeout under load, so wait for the fetch itself (attached before the navigating
        // click, not after, so a fast response isn't missed) rather than just the URL change.
        const detailFetch = page.waitForResponse(
            (r) => /\/api\/v1\/stocktake\/[0-9a-f-]+$/i.test(r.url()) && r.request().method() === 'GET',
            { timeout: 30_000 }
        );

        const openRow = page.locator('tr', { hasText: /Counting|Review/ }).first();
        if ((await openRow.count()) > 0) {
            await openRow.click();
        } else {
            await page.getByRole('button', { name: 'New Stock Take' }).click();
            await pickDropdownOption(page, page.getByText('Select warehouse', { exact: false }), 'Malawoori');
            await page.getByRole('button', { name: 'Start Counting' }).click();
        }

        await expect(page).toHaveURL(/\/inventory\/stock-takes\/[0-9a-f-]+$/i, { timeout: 10_000 });
        await detailFetch;

        if (await page.getByText('Counting', { exact: true }).first().isVisible().catch(() => false)) {
            const firstCountInput = page.locator('table tbody tr').first().locator('input[type="number"], .p-inputnumber input').first();
            await expect(firstCountInput).toBeVisible({ timeout: 20_000 });
            await firstCountInput.fill('1');
            await expect(firstCountInput).toHaveValue('1');

            await page.getByRole('button', { name: 'Submit for Review' }).click();
            const [submitResponse] = await Promise.all([
                page.waitForResponse((r) => /\/api\/v1\/stocktake\/[0-9a-f-]+\/submit$/i.test(r.url()) && r.request().method() === 'POST', { timeout: 15_000 }),
                page.getByRole('button', { name: 'Yes', exact: true }).click()
            ]);
            expect(submitResponse.ok()).toBe(true);
            await expect(page.getByText('Review', { exact: true }).first()).toBeVisible({ timeout: 10_000 });
        }

        await page.getByRole('button', { name: 'Approve & Apply' }).click();
        await page.getByRole('button', { name: 'Yes', exact: true }).click();
        await expect(page.getByText('Completed', { exact: true }).first()).toBeVisible({ timeout: 10_000 });
    });
});
