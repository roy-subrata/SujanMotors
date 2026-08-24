import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption } from './utils/ui';

test.describe('HR — Attendance & Leave', () => {
    test('mark all staff present for today and save', async ({ page }) => {
        await page.goto('/hr/attendance');
        await page.locator('button:has(.pi-check-square)').click();
        await page.getByRole('button', { name: 'Save All' }).click();
        await expect(page.getByText(/saved/i).first()).toBeVisible({ timeout: 10_000 });
    });

    test('submit a leave request', async ({ page }) => {
        // A dedicated employee (rather than reusing whichever "E2E" employee sorts first)
        // guarantees this employee has no pre-existing pending/approved leave to collide
        // with — the backend rejects any date range that overlaps one already on file.
        const employeeName = `E2E Leave ${uniqueSuffix()}`;
        await page.goto('/hr/employees/create');
        await page.getByPlaceholder('e.g., Abdul Karim').fill(employeeName);
        await page.getByPlaceholder('e.g., +8801716625369').fill('018' + Date.now().toString().slice(-8));
        await page.getByPlaceholder(/Salesperson, Cashier, Storekeeper/i).fill('E2E Test Role');
        await pickDropdownOption(page, page.getByText('Select department', { exact: false }), 'Sales');
        await page.locator('p-inputnumber input').first().fill('25000');
        await page.getByRole('button', { name: 'Create Employee' }).click();
        await expect(page).toHaveURL(/\/hr\/employees$/);

        await page.goto('/hr/leave-requests');
        await page.getByRole('button', { name: 'New Leave Request' }).click();

        await pickDropdownOption(page, page.getByText('Select employee', { exact: false }), employeeName);

        // Leave Type defaults to "Casual" (a valid selection) so it's left untouched.
        const fromDate = page.locator('p-datepicker input').first();
        await fromDate.click();
        await page.getByRole('gridcell', { name: '24', exact: true }).first().click();

        const toDate = page.locator('p-datepicker input').last();
        await toDate.click();
        await page.getByRole('gridcell', { name: '25', exact: true }).first().click();

        await page.getByPlaceholder('Reason for leave...').fill(`E2E leave reason ${uniqueSuffix()}`);
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().includes('/api/v1/leaverequests') && r.request().method() === 'POST', { timeout: 15_000 }),
            page.getByRole('button', { name: 'Save', exact: true }).click()
        ]);
        expect(response.ok()).toBe(true);
    });
});
