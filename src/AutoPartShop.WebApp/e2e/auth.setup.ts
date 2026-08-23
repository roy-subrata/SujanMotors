import { test as setup, expect } from '@playwright/test';
import { ADMIN_CREDENTIALS } from './utils/api';

const authFile = 'e2e/.auth/admin.json';

/** Runs once before the chromium project: logs in through the real UI and persists the session. */
setup('authenticate as admin', async ({ page }) => {
    await page.goto('/login');
    await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
    await page.getByPlaceholder('••••••••').fill(ADMIN_CREDENTIALS.password);
    await page.getByRole('button', { name: 'Sign In' }).click();

    await expect(page).toHaveURL('/');
    await expect(page.getByText('Dashboard').first()).toBeVisible();

    await page.context().storageState({ path: authFile });
});
