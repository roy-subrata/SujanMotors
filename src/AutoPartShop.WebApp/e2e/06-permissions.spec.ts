import { test, expect } from '@playwright/test';

/**
 * "e2elimiteduser" / role "User" was created once via Admin Settings > Users as part of
 * this test session (see final report — creating it surfaced two real bugs: the Users
 * list crashed on load, and Add User was unusable as a result; both fixed in
 * admin.service.ts). This spec starts unauthenticated and logs in as that limited user
 * to verify role-gated routes actually deny a non-admin, not just that admin can see them.
 */
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Permission / role behavior', () => {
    test('a low-privilege "User" cannot reach an Admin/Manager-only route', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill('e2elimiteduser');
        await page.getByPlaceholder('••••••••').fill('TestPass123!');
        await page.getByRole('button', { name: 'Sign In' }).click();
        await expect(page).toHaveURL('/');

        // /hr is gated by roleGuard to ['Admin', 'Manager'] (app.routes.ts) — a plain
        // "User" must be redirected away, not shown the page.
        await page.goto('/hr');
        await expect(page).not.toHaveURL(/\/hr/);
    });

    test('a low-privilege "User" cannot reach Admin Settings', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill('e2elimiteduser');
        await page.getByPlaceholder('••••••••').fill('TestPass123!');
        await page.getByRole('button', { name: 'Sign In' }).click();
        await expect(page).toHaveURL('/');

        await page.goto('/admin-settings');
        await expect(page).not.toHaveURL(/\/admin-settings/);
    });

    test('admin can reach both routes', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill('admin');
        await page.getByPlaceholder('••••••••').fill('Admin@1990');
        await page.getByRole('button', { name: 'Sign In' }).click();
        await expect(page).toHaveURL('/');

        await page.goto('/hr');
        await expect(page).toHaveURL(/\/hr/);

        await page.goto('/admin-settings');
        await expect(page).toHaveURL(/\/admin-settings/);
    });
});
