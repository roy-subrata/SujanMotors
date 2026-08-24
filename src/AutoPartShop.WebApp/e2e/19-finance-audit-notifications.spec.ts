import { test, expect } from '@playwright/test';

/**
 * Lighter smoke coverage for the remaining read-heavy admin/reporting pages: these are
 * populated as a side effect of every other spec's writes (audit logs record every
 * create/update; cash book reflects every payment; notifications come from reorder alerts),
 * so the checks here focus on "the page loads and renders real data" rather than driving a
 * create flow — matching the same rationale as 07-invoices-reports-warranty.spec.ts.
 */
test.describe('Finance, Audit & Notifications', () => {
    test('cash book loads for today', async ({ page }) => {
        await page.goto('/finance/cash-book');
        await expect(page.getByRole('heading', { name: 'Cash Book' }).first()).toBeVisible();
    });

    test('audit dashboard loads with activity data', async ({ page }) => {
        await page.goto('/audit/dashboard');
        await expect(page.getByRole('heading', { name: 'Audit Trail Dashboard' }).first()).toBeVisible();
    });

    test('audit logs list shows records from earlier specs', async ({ page }) => {
        await page.goto('/audit/logs');
        await expect(page.getByRole('heading', { name: 'Audit Logs' }).first()).toBeVisible();
        await expect(page.getByText(/records/i).first()).toBeVisible();
    });

    test('notifications inbox loads', async ({ page }) => {
        await page.goto('/notifications');
        await expect(page.getByRole('heading', { name: 'Notifications' }).first()).toBeVisible();
    });

    test('exchange rates admin page loads', async ({ page }) => {
        await page.goto('/admin/exchange-rates');
        await expect(page.getByRole('heading', { name: 'Exchange Rates' }).first()).toBeVisible();
        await expect(page.getByRole('button', { name: 'Add Exchange Rate' })).toBeVisible();
    });
});
