import { test as base, expect, Page } from '@playwright/test';

/**
 * Custom test options so one spec runs against either app.
 *   appUrl  - base URL of the app under test
 *   appKind - 'webforms' | 'blazor'
 *
 * The two apps differ in routing: Web Forms uses *.aspx URLs, Blazor uses clean
 * routes. pageUrl() hides that difference so the spec stays single-source.
 */
export type AppOptions = {
  appUrl: string;
  appKind: 'webforms' | 'blazor';
};

export const test = base.extend<AppOptions>({
  appUrl: ['http://localhost:5080', { option: true }],
  appKind: ['webforms', { option: true }],
});

export { expect };

type AppKind = 'webforms' | 'blazor';

/** Logical page name -> relative path per app. */
const ROUTES: Record<string, Record<AppKind, string>> = {
  login: { webforms: 'Login.aspx', blazor: 'Login' },
  desk: { webforms: 'Default.aspx', blazor: '' },
  customers: { webforms: 'Customers/List.aspx', blazor: 'Customers/List' },
  catalog: { webforms: 'Catalog/List.aspx', blazor: 'Catalog/List' },
  ordersList: { webforms: 'Orders/List.aspx', blazor: 'Orders/List' },
  ordersNew: { webforms: 'Orders/New.aspx', blazor: 'Orders/New' },
  lowStock: { webforms: 'Reports/LowStock.aspx', blazor: 'Reports/LowStock' },
};

export function pageUrl(appUrl: string, appKind: AppKind, name: keyof typeof ROUTES) {
  const rel = ROUTES[name][appKind];
  return rel ? `${appUrl}/${rel}` : `${appUrl}/`;
}

/**
 * Sign in as clerk / counter2011 and wait until the desk is shown.
 * Ground truth: the header shows the person's name (Mara Ellison), not the
 * username. If an app fails to actually sign in (e.g. the Blazor
 * cookie-in-circuit bug), this helper fails and marks the check failed.
 *
 * The login page on both apps has ids txtUser / txtPassword and no master page,
 * so raw id selectors are safe here. We assert on the header text rather than a
 * strict URL regex, because the Web Forms desk is Default.aspx while Blazor's is
 * the app root.
 */
export async function login(
  page: Page,
  appUrl: string,
  appKind: AppKind,
  user = 'clerk',
  password = 'counter2011'
) {
  await page.goto(pageUrl(appUrl, appKind, 'login'));

  const userBox = page.locator('#txtUser');
  const passBox = page.locator('#txtPassword');

  // On the Blazor static-SSR login page, blazor.web.js can re-hydrate the form
  // after load and wipe programmatically-filled values, which made the POST
  // arrive with a blank username intermittently. Fill, then verify the values
  // actually stuck right before submitting; re-fill if they were cleared.
  for (let attempt = 0; attempt < 3; attempt++) {
    await userBox.fill(user);
    await passBox.fill(password);
    await page.waitForTimeout(300);
    if ((await userBox.inputValue()) === user && (await passBox.inputValue()) === password) {
      break;
    }
  }

  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL((url) => !/login/i.test(url.pathname), { timeout: 15000 });

  // Authenticated: the site header shows the signed-in person.
  await expect(page.locator('.who')).toContainText('Mara Ellison', { timeout: 15000 });
}
