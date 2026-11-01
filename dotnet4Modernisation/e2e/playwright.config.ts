import { defineConfig, devices } from '@playwright/test';

/**
 * Two projects, one per app under test:
 *   - webforms  -> http://localhost:5080  (ASP.NET Web Forms, database ApexCounter)      the ground truth
 *   - blazor    -> http://localhost:5088  (Blazor Server .NET 8, database ApexCounter_Modern)
 *
 * The app URL is passed to the tests via the `appUrl` test-option (see fixtures.ts),
 * so a single spec runs against both. Override with:
 *   BLAZOR_URL=https://localhost:5099 npx playwright test
 *
 * ignoreHTTPSErrors is on so the suite still works if an app is served over a
 * self-signed https cert (e.g. the Blazor default before the http fix).
 */
const WEBFORMS_URL = process.env.WEBFORMS_URL ?? 'http://localhost:5080';
const BLAZOR_URL = process.env.BLAZOR_URL ?? 'http://localhost:5088';

export default defineConfig({
  testDir: './tests',
  // The mutating checks (5-8) must run in order and not in parallel, because
  // they change shared database rows. Keep it simple and deterministic.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  // Interactive Blazor Server renders content over a SignalR circuit, so give
  // assertions and actions more room than the defaults to avoid flaky timeouts.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    ignoreHTTPSErrors: true,
    actionTimeout: 15_000,
    navigationTimeout: 20_000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'webforms',
      use: { ...devices['Desktop Chrome'], appUrl: WEBFORMS_URL, appKind: 'webforms' },
    },
    {
      name: 'blazor',
      use: { ...devices['Desktop Chrome'], appUrl: BLAZOR_URL, appKind: 'blazor' },
    },
  ],
});
