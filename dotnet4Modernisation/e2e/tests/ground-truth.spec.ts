import { test, expect, login, pageUrl } from './fixtures';

/**
 * The eight behavioural checks from FINISH_LINE.md, mapped to the exact
 * sentences and values in GROUND_TRUTH.md. Runs against both the Web Forms app
 * (project "webforms", 5080) and the Blazor app (project "blazor", 5088).
 *
 * Selectors use shared control ids (txtUser, ddlNext, txtAdjustQty, ...) and
 * button text ("Sign in", "Search", "Add line", "Open order", "Apply status",
 * "Adjust") that render the same on both apps. URLs differ (.aspx vs clean
 * routes) and are resolved via pageUrl().
 *
 * Checks 5-8 mutate rows; config runs single-worker, no parallelism, in order.
 */

// ---------------------------------------------------------------------------
// Check 1 - wrong password shows the login-failed sentence
// ---------------------------------------------------------------------------
test('check 1: wrong password is rejected with the exact sentence', async ({ page, appUrl, appKind }) => {
  await page.goto(pageUrl(appUrl, appKind, 'login'));
  await page.locator('#txtUser').fill('clerk');
  await page.locator('#txtPassword').fill('nope');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.locator('body')).toContainText(
    'That username and password do not match an active account.'
  );
  await expect(page.locator('body')).not.toContainText('Mara Ellison');
});

// ---------------------------------------------------------------------------
// Check 2 - desk tiles are 2, 5, 48.72 on the seed day
// ---------------------------------------------------------------------------
test('check 2: desk tiles show 2 open, 5 low, 48.72 invoiced today', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await page.goto(pageUrl(appUrl, appKind, 'desk'));

  const tiles = page.locator('.tile');
  await expect(tiles.nth(0)).toContainText('2');
  await expect(tiles.nth(1)).toContainText('5');
  await expect(tiles.nth(2)).toContainText('48.72');
});

// ---------------------------------------------------------------------------
// Check 3 - Customers: Active hides OLD-99; All + search OLD-99 shows Retired Yard Co.
// ---------------------------------------------------------------------------
test('check 3: customers Active hides OLD-99, All + search shows Retired Yard Co.', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await page.goto(pageUrl(appUrl, appKind, 'customers'));

  // Assert against the data grid, not the whole body: the page's help note
  // text itself mentions "Retired Yard Co.".
  const grid = page.locator('.grid');
  await expect(grid).not.toContainText('Retired Yard Co.');

  // Select "All" and verify it stuck (Blazor can re-hydrate the control and
  // drop a programmatic selection), then search OLD-99.
  const filter = page.locator('select').first();
  for (let attempt = 0; attempt < 3; attempt++) {
    await filter.selectOption({ label: 'All' });
    await page.waitForTimeout(300);
    if ((await filter.inputValue()) === 'all') break;
  }
  await page.locator('input[type="text"]').first().fill('OLD-99');
  await page.getByRole('button', { name: 'Search' }).click();

  await expect(grid).toContainText('Retired Yard Co.');
  await expect(grid).toContainText('OLD-99');
});

// ---------------------------------------------------------------------------
// Check 4 - Low-stock report count is 5 with no DISC-1; catalog low-only shows DISC-1
// ---------------------------------------------------------------------------
test('check 4: report count 5 without DISC-1, catalog low-only includes DISC-1', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);

  await page.goto(pageUrl(appUrl, appKind, 'lowStock'));
  await expect(page.locator('body')).toContainText('5');
  await expect(page.locator('body')).not.toContainText('DISC-1');

  await page.goto(pageUrl(appUrl, appKind, 'catalog'));
  // The catalog page has a single checkbox: "Low stock only".
  await page.getByRole('checkbox').first().check();
  await page.getByRole('button', { name: 'Search' }).click();
  await expect(page.locator('body')).toContainText('DISC-1');
});

// ---------------------------------------------------------------------------
// Check 5 - AC-0006 short pick is refused, order stays Open
// ---------------------------------------------------------------------------
test('check 5: picking AC-0006 fails on WR-10 and stays Open', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await openOrderByNumber(page, appUrl, appKind, 'AC-0006');

  await applyStatus(page, 'Picked');

  await expect(page.locator('body')).toContainText(
    'Not enough stock for WR-10. On hand is 3, this order needs 4.'
  );
  await expect(page.locator('[id$="lblStatus"]')).toContainText('Open');
});

// ---------------------------------------------------------------------------
// Check 6 - AC-0003 Picked then Invoiced; LT-LED 9 -> 7, stays 7
// ---------------------------------------------------------------------------
test('check 6: AC-0003 picks (LT-LED 9->7) then invoices, stays 7', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await openOrderByNumber(page, appUrl, appKind, 'AC-0003');

  await applyStatus(page, 'Picked');
  await expect(page.locator('body')).toContainText('Order AC-0003 is now Picked.');
  await expect(page.locator('body')).toContainText('LT-LED');

  await applyStatus(page, 'Invoiced');
  await expect(page.locator('body')).toContainText('Order AC-0003 is now Invoiced.');
  await expect(page.locator('[id$="lblStatus"]')).toContainText('Invoiced');
});

// ---------------------------------------------------------------------------
// Check 7 - new order for Harbor Lights LLC, one BX-4: 6.25 / 0.31 / 6.56
// ---------------------------------------------------------------------------
test('check 7: new order BX-4 totals 6.25 / 0.31 / 6.56', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await page.goto(pageUrl(appUrl, appKind, 'ordersNew'));

  // Dropdown labels are "ACCOUNT - Company" / "SKU - Name"; select by the
  // option value whose text contains the target (selectOption needs a string).
  await selectOptionByText(page, '[id$="ddlCustomer"]', 'Harbor Lights LLC');
  await selectOptionByText(page, '[id$="ddlProduct"]', 'BX-4');
  await page.getByRole('button', { name: 'Add line' }).click();
  await page.getByRole('button', { name: 'Open order' }).click();

  await expect(page.locator('body')).toContainText('6.25'); // subtotal
  await expect(page.locator('body')).toContainText('0.31'); // tax
  await expect(page.locator('body')).toContainText('6.56'); // total
});

// ---------------------------------------------------------------------------
// Check 8 - catalog CB-100 adjust +1 reason parity => "On hand for CB-100 is now 13."
// ---------------------------------------------------------------------------
test('check 8: adjust CB-100 by 1 shows new on hand 13', async ({ page, appUrl, appKind }) => {
  await login(page, appUrl, appKind);
  await page.goto(pageUrl(appUrl, appKind, 'catalog'));
  await page.locator('input[type="text"]').first().fill('CB-100');
  await page.getByRole('button', { name: 'Search' }).click();

  // Open the CB-100 row specifically (not just the first row in the grid).
  const cbRow = page.locator('tr', { hasText: 'CB-100' });
  await expect(cbRow).toHaveCount(1);
  await cbRow.getByRole('link', { name: 'Open' }).click();

  await page.locator('[id$="txtAdjustQty"]').fill('1');
  await page.locator('[id$="txtAdjustNote"]').fill('parity');
  await page.getByRole('button', { name: 'Adjust' }).click();

  await expect(page.locator('body')).toContainText('On hand for CB-100 is now 13.');
});

// --- helpers ---------------------------------------------------------------

async function openOrderByNumber(page: any, appUrl: string, appKind: 'webforms' | 'blazor', orderNumber: string) {
  await page.goto(pageUrl(appUrl, appKind, 'ordersList'));
  const row = page.locator('tr', { hasText: orderNumber });
  await row.getByRole('link', { name: 'Open' }).first().click();
  await expect(page.locator('[id$="lblNumber"]')).toContainText(orderNumber);
}

async function applyStatus(page: any, status: string) {
  await page.locator('[id$="ddlNext"]').selectOption({ label: status });
  await page.getByRole('button', { name: 'Apply status' }).click();
}

/** Select the <option> whose visible text contains `text` (partial match). */
async function selectOptionByText(page: any, selectSelector: string, text: string) {
  const select = page.locator(selectSelector);
  const value = await select
    .locator('option', { hasText: text })
    .first()
    .getAttribute('value');
  await select.selectOption(value as string);
}
