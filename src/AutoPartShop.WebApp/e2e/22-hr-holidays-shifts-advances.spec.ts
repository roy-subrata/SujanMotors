import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

test.describe('HR — Holidays, Shifts & Salary Advances', () => {
    test('add a holiday', async ({ page }) => {
        const name = `E2E Holiday ${uniqueSuffix()}`;
        // A holiday date collides (silently rejected) with one already seeded on the same
        // date by an earlier run, so pick a day-of-month that varies per run rather than a
        // fixed one.
        const day = String((Date.now() % 25) + 1);
        await page.goto('/hr/holidays');
        await page.getByRole('button', { name: 'Add Holiday' }).click();

        const dateInput = page.locator('p-datepicker input').first();
        await dateInput.click();
        await page.getByRole('gridcell', { name: day, exact: true }).first().click();

        await page.getByPlaceholder(/Eid-ul-Fitr/i).fill(name);
        await page.getByRole('button', { name: 'Save', exact: true }).click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
    });

    test('add a shift', async ({ page }) => {
        const name = `E2E Shift ${uniqueSuffix()}`;
        await page.goto('/hr/shifts');
        await page.getByRole('button', { name: 'Add Shift' }).click();

        await page.getByPlaceholder(/Morning 9-6/i).fill(name);
        const timeInputs = page.locator('input[type="time"]');
        await timeInputs.first().fill('09:00');
        await timeInputs.last().fill('18:00');

        await page.getByRole('button', { name: 'Save', exact: true }).click();
        await expect(page.getByText(name).first()).toBeVisible({ timeout: 10_000 });
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
