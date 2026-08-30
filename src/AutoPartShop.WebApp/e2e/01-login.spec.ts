import { test, expect } from '@playwright/test';
import { ADMIN_CREDENTIALS } from './utils/api';

// This spec deliberately starts unauthenticated — it's testing the login flow itself,
// so it must not inherit the project's saved admin session.
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Login', () => {
    test('valid credentials sign the user in and land on the dashboard', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
        await page.getByPlaceholder('••••••••').fill(ADMIN_CREDENTIALS.password);
        await page.getByRole('button', { name: 'Sign In' }).click();

        await expect(page).toHaveURL('/');
        await expect(page.getByText('Dashboard').first()).toBeVisible();
        await expect(page.getByText('System Administrator')).toBeVisible();
    });

    test('wrong password shows an error and does not navigate away', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
        await page.getByPlaceholder('••••••••').fill('WrongPassword123!');
        await page.getByRole('button', { name: 'Sign In' }).click();

        await expect(page.getByText(/invalid credentials/i)).toBeVisible();
        await expect(page).toHaveURL(/\/login/);
    });

    test('empty fields are rejected client-side without a network round-trip', async ({ page }) => {
        await page.goto('/login');
        await page.getByRole('button', { name: 'Sign In' }).click();

        await expect(page.getByText(/username is required/i)).toBeVisible();
        await expect(page.getByText(/password is required/i)).toBeVisible();
        await expect(page).toHaveURL(/\/login/);
    });

    test('logged-in user can log out and lands back on the login screen', async ({ page }) => {
        await page.goto('/login');
        await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
        await page.getByPlaceholder('••••••••').fill(ADMIN_CREDENTIALS.password);
        await page.getByRole('button', { name: 'Sign In' }).click();
        await expect(page).toHaveURL('/');

        await page.getByText('System Administrator').click();
        await page.getByText(/log ?out/i).click();

        await expect(page).toHaveURL(/\/login/);
    });

    test('an unauthenticated visit to a protected route redirects to login', async ({ page }) => {
        await page.goto('/inventory/parts');
        await expect(page).toHaveURL(/\/login/);
    });
});
