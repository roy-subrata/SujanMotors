import { defineConfig, devices } from '@playwright/test';

/**
 * E2E suite runs against a locally-started dev stack (see e2e/README.md):
 *   - API:  dotnet run --project src/AutoPartShop.Api  (Development env, port 5001)
 *   - Web:  ng serve --port 4301 --proxy-config proxy.conf.json
 * Both must already be running — this config does not manage them (the dev DB
 * takes too long to seed/migrate to spin up fresh per run, and killing/restarting
 * dotnet from a Playwright webServer hook proved unreliable in this environment).
 */
export default defineConfig({
    testDir: './e2e',
    fullyParallel: false, // most specs share/mutate the same catalog + stock — safer serial
    forbidOnly: !!process.env.CI,
    retries: 0,
    workers: 1,
    reporter: [['list'], ['html', { open: 'never', outputFolder: 'e2e-report' }]],
    // Some specs make 2-3 pickAutocompleteOption calls per test, and the customer/supplier
    // list endpoints backing app-lazy-autocomplete have been observed taking up to ~11s
    // under sustained dev load (see pickAutocompleteOption's comment in e2e/utils/ui.ts) —
    // 45s left no headroom for a test with multiple slow lookups.
    timeout: 90_000,
    expect: { timeout: 10_000 },
    use: {
        baseURL: 'http://localhost:4301',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        actionTimeout: 15_000,
    },
    projects: [
        {
            name: 'setup',
            testMatch: /auth\.setup\.ts/,
        },
        {
            name: 'chromium',
            use: { ...devices['Desktop Chrome'], storageState: 'e2e/.auth/admin.json' },
            dependencies: ['setup'],
            testIgnore: /auth\.setup\.ts/,
        },
    ],
});
