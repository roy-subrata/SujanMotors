import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

test.describe('HR — Holidays, Shifts & Salary Advances', () => {
    test('add a holiday', async ({ page }) => {
        const name = `E2E Holiday ${uniqueSuffix()}`;
        await page.goto('/hr/holidays');
        await page.getByRole('button', { name: 'Add Holiday' }).click();

        const dateInput = page.locator('p-datepicker input').first();
        await page.getByPlaceholder(/Eid-ul-Fitr/i).fill(name);

        // A holiday date collides (rejected) with one already seeded on the same date by an
        // earlier run. With this suite run many times against the same dev DB, a large
        // fraction of the 1-25 day range can end up taken — including in contiguous runs
        // (e.g. 21-25 all at once), which defeats a deterministic +1 walk starting nearby.
        // Re-roll an independent random day each attempt instead, and retry generously.
        let response;
        for (let attempt = 0; attempt < 8; attempt++) {
            const day = Math.floor(Math.random() * 25) + 1;
            await dateInput.click();
            await page.getByRole('gridcell', { name: String(day), exact: true }).first().click();

            [response] = await Promise.all([
                page.waitForResponse((r) => r.url().endsWith('/api/v1/holidays') && r.request().method() === 'POST', { timeout: 15_000 }),
                page.getByRole('button', { name: 'Save', exact: true }).click()
            ]);
            if (response.ok()) break;
        }
        expect(response!.ok()).toBe(true);
    });

    test('add a shift', async ({ page }) => {
        const name = `E2E Shift ${uniqueSuffix()}`;
        await page.goto('/hr/shifts');
        await page.getByRole('button', { name: 'Add Shift' }).click();

        await page.getByPlaceholder(/Morning 9-6/i).fill(name);
        const timeInputs = page.locator('input[type="time"]');
        await timeInputs.first().fill('09:00');
        await timeInputs.last().fill('18:00');

        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/shifts') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Save', exact: true }).click()
        ]);
        expect(response.ok()).toBe(true);
    });

    test('give a salary advance', async ({ page }) => {
        await page.goto('/hr/advances');
        await page.getByRole('button', { name: 'Give Advance' }).click();

        await pickDropdownOption(page, page.getByText('Select employee', { exact: false }), 'E2E');
        await page.getByPlaceholder(/e\.g\., 5000/i).fill('1000');

        await page.getByRole('button', { name: 'Give Advance' }).last().click();
        await expect(page.getByText(/success/i).first()).toBeVisible({ timeout: 10_000 });
    });

    test('payroll runs page loads', async ({ page }) => {
        await page.goto('/hr/payroll');
        await expect(page.getByRole('heading', { name: 'Payroll' }).first()).toBeVisible();
    });
});
