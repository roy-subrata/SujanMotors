import { defineConfig, devices } from '@playwright/test';

/**
 * Standalone mobile-QA run against the dev stack currently serving the app.
 *  - Web:  ng serve on http://localhost:4200 (proxies /api -> 5001)
 *  - API:  dotnet dev API on http://localhost:5001
 *
 * Runs only e2e-mobile/ specs. Not part of `npm run e2e`.
 */
export default defineConfig({
    testDir: './e2e-mobile',
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: 0,
    workers: 1,
    reporter: [['list']],
    timeout: 150_000,
    expect: { timeout: 15_000 },
    use: {
        baseURL: 'http://localhost:4200',
        actionTimeout: 20_000,
        navigationTimeout: 120_000,
        // iPhone 13 phone viewport + touch, but run it on the installed Chromium
        // (WebKit isn't downloaded in this environment).
        ...devices['iPhone 13'],
        defaultBrowserType: 'chromium',
    },
});