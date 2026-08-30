import { test, expect } from '@playwright/test';

/**
 * The config-driven reports module (report-configs/*, report-page.component.ts) has ~20
 * report keys across sales/inventory/purchase/financial groups, plus three bespoke pages
 * (Profit & Loss, VAT, Daily Z). This suite doesn't exercise every key — that's config data,
 * not behavior — just one representative generic report (sales-summary, covering the
 * filter->load->export path every :reportKey page shares) and the three bespoke pages.
 *
 * All of these auto-populate their date range and load on init, so no filter interaction is
 * needed to get real data — these are "loads with real data + export works" smoke tests,
 * matching the rationale in 07-invoices-reports-warranty.spec.ts and
 * 19-finance-audit-notifications.spec.ts.
 */
test.describe('Reports — generic report page & bespoke financial reports', () => {
    test('a generic config-driven report loads data and exports xlsx/pdf', async ({ page }) => {
        const [listResponse] = await Promise.all([
            page.waitForResponse(
                (r) => r.url().endsWith('/api/v1/reports/sales/summary') && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.goto('/reports/sales-summary')
        ]);
        expect(listResponse.ok()).toBe(true);
        await expect(page.getByRole('heading', { name: 'Sales Summary' }).first()).toBeVisible();

        const [xlsxResponse] = await Promise.all([
            page.waitForResponse(
                (r) => /\/api\/v1\/reports\/sales\/summary\/export\?format=xlsx$/.test(r.url()) && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.getByRole('button', { name: 'Excel' }).click()
        ]);
        expect(xlsxResponse.ok()).toBe(true);

        const [pdfResponse] = await Promise.all([
            page.waitForResponse(
                (r) => /\/api\/v1\/reports\/sales\/summary\/export\?format=pdf$/.test(r.url()) && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.getByRole('button', { name: 'PDF' }).click()
        ]);
        expect(pdfResponse.ok()).toBe(true);
    });

    test('Profit & Loss statement loads real figures', async ({ page }) => {
        const [response] = await Promise.all([
            page.waitForResponse(
                (r) => r.url().endsWith('/api/v1/reports/financial/profit-loss') && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.goto('/reports/profit-loss')
        ]);
        expect(response.ok()).toBe(true);
        await expect(page.getByRole('heading', { name: 'Profit & Loss' }).first()).toBeVisible();
    });

    test('VAT report loads and the branded PDF handoff downloads', async ({ page }) => {
        const [response] = await Promise.all([
            page.waitForResponse(
                (r) => r.url().endsWith('/api/v1/reports/financial/vat') && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.goto('/reports/vat')
        ]);
        expect(response.ok()).toBe(true);
        await expect(page.getByRole('heading', { name: 'VAT Report' }).first()).toBeVisible();

        const [pdfResponse] = await Promise.all([
            page.waitForResponse(
                (r) => r.url().endsWith('/api/v1/reports/financial/vat/pdf') && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.getByRole('button', { name: 'Download PDF' }).click()
        ]);
        expect(pdfResponse.ok()).toBe(true);
    });

    test('Daily Sales (Z) Report PDF generates for today', async ({ page }) => {
        await page.goto('/reports/daily-z-report');
        await expect(page.getByRole('heading', { name: 'Daily Sales (Z) Report' }).first()).toBeVisible();

        const [pdfResponse] = await Promise.all([
            page.waitForResponse(
                (r) => r.url().endsWith('/api/v1/reports/sales/daily-z-report/pdf') && r.request().method() === 'POST',
                { timeout: 15_000 }
            ),
            page.getByRole('button', { name: 'Download Z Report PDF' }).click()
        ]);
        expect(pdfResponse.ok()).toBe(true);
    });
});
