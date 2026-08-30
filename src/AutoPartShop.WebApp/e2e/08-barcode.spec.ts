import { test, expect } from '@playwright/test';

/**
 * Barcode/label printing lives on accepted Goods Receipts (see project docs: label
 * printing moved off Parts onto Goods Receipts + Stock Lots). Business-flow and manual
 * testing earlier in this session already produced at least one ACCEPTED receipt, so
 * this opens the most recent one and exercises the label dialog rather than re-running
 * a full purchase just to get there.
 */
test.describe('Barcode labels', () => {
    test('an accepted goods receipt offers barcode label printing', async ({ page }) => {
        await page.goto('/procurement/goods-receipts');
        await page.getByRole('button', { name: 'Accepted' }).click();
        await page.waitForTimeout(500);

        const firstAccepted = page.locator('a', { hasText: /^GRN\d+$/ }).first();
        await expect(firstAccepted).toBeVisible();
        await firstAccepted.click();

        await expect(page.getByText('Print Labels').first()).toBeVisible();
        // Click the per-item "Print Labels" action (not the section heading).
        await page.getByRole('button', { name: 'Print Labels' }).click();

        // The shared BarcodeDialog renders a QR + linear barcode preview.
        const dialogContent = page.locator('svg, canvas, img[src*="barcode"], img[src*="qr"]');
        await expect(dialogContent.first()).toBeVisible({ timeout: 10_000 });
    });
});
