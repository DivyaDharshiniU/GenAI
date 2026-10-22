# Apex Counter — ground truth

This is the behavior the **.NET 8 Blazor** app must match.

The running **ASP.NET Web Forms** site (.NET Framework 4.0, port 5080, database `ApexCounter`) is the authority. After `reset.ps1`, the desk must match the table below. If it does not, run the reset again. Do not edit the Web Forms project, and do not change this file to fit a bad run.

Reset first, on the same calendar day you check the desk tile:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\reset.ps1
```

Legacy site: `http://localhost:5080/`  
Sign in: `clerk` / `counter2011` or `supervisor` / `counter2011`  
Header shows **Mara Ellison** or **Owen Briggs**, not the username.

Money on screen uses `en-US`: `48.72`, dates like `11 Oct 2026 09:15`. A blank picked or invoiced time is a single hyphen `-`.

## Fresh database

| Check | Value |
|---|---|
| Open orders | 2 (`AC-0003`, `AC-0006`) |
| Active products at or under reorder | 5 |
| Invoiced today | 48.72 |
| Active customers | 6, paged 5 + 1 |
| Customers, All | 7, including inactive **Retired Yard Co.** (`OLD-99`) |
| Catalog, low stock only | 6 rows. Includes inactive `DISC-1` |
| Low-stock report | 5 rows. Does **not** include `DISC-1` |

Low active SKUs: `BR-40` (8 on hand, reorder 12), `CB-250` (4 / 6), `CT-30` (15 / 15), `TM-1` (0 / 5), `WR-10` (3 / 8).

`AC-0006` asks for 4 of `WR-10`. On hand is 3. Picking it must fail.

## Pages

### `Login.aspx`

Anonymous visitors can open only this page and the stylesheet. Wrong password, or an unknown user, shows: "That username and password do not match an active account." Empty fields ask for a username and a password. A dead database shows the `reset.ps1` sentence from `Code/Rules.cs`.

### `Default.aspx` — Desk

Three tiles: open-order count, active low-stock count, sum of **Invoiced** order totals whose invoice date is today. The grid is the latest orders, newest first, at most 8, no paging. **Open** goes to that order.

### `Customers/List.aspx`

Opens filtered to **Active**. Search matches company, contact, account code, or city. Page size 5. **Inactive** shows Retired Yard only, on a fresh seed. **All** has 7 rows, so `OLD-99` is on page 2 unless you search `OLD-99`.

### `Customers/Edit.aspx`

New customer, or `?id=`. Account code is unique. Save on a new row sends you to `Edit.aspx?id=` and says `Saved {account}`. Inactive customers disappear from the new-order dropdown.

### `Catalog/List.aspx`

Search matches SKU or name. Page size 5. A row is highlighted when on hand is at or under the reorder point, including inactive products. **Low stock only** is not the same list as the report.

### `Catalog/Edit.aspx`

Price and reorder point save without changing on-hand, and without rewriting order lines. A new product starts at on hand 0; the adjust panel appears after the first save. Adjust writes a ledger row with reason `adjust`. Zero is refused. A change that would go below zero is refused and on hand stays put. The sentence names the SKU and the current on hand.

### `Orders/List.aspx`

Filter by status and by customer company. Page size 5. Newest first.

### `Orders/New.aspx`

Dropdowns list active customers and active products only. The draft is remembered if you leave the page, and dropped on **Clear draft** or **Sign out**. Adding a SKU already on the draft adds the quantity and keeps the first unit price. Tax preview is 5 percent, half away from zero, two decimals. **Open order** writes status `Open`, copies the draft onto the lines, and does not change stock. You land on the new order's detail page. The order number is `AC-` plus the id, four digits.

### `Orders/Detail.aspx`

Shows the stored subtotal, tax, and total, not a recomputed price. Ledger rows for this order are underneath.

| From | Allowed | Stock |
|---|---|---|
| Open | Picked, Cancelled | Pick deducts. Cancel does nothing. |
| Picked | Invoiced, Cancelled | Invoice does nothing. Cancel adds the stock back (`cancel-return`). |
| Invoiced | none | Apply is disabled. |
| Cancelled | none | Apply is disabled. |

Choosing the current status says "No status change." An illegal move names both statuses. A short pick says `Not enough stock for {sku}. On hand is {n}, this order needs {n}.` and leaves the order Open.

### `Reports/LowStock.aspx`

Active products only, page size 4. The count above the grid is the full count, not the page count. **Open** goes to the catalog edit page.

### `Account/Password.aspx`

Current password must match. New password at least 6 characters, and must match the confirmation. The salt is kept; only the hash changes. Changing `clerk` leaves `supervisor` on `counter2011`.

## Two clicks that prove the stock rule

Reset, sign in, then:

1. Open `AC-0006`. Apply **Picked**. The message names `WR-10`, on hand 3, need 4. Status stays Open. `WR-10` on hand stays 3. No new ledger row for that order.
2. Open `AC-0003` (2 × `LT-LED`, on hand 9). Apply **Picked**. Status becomes Picked. `LT-LED` on hand becomes 7. The ledger shows `-2` with reason `pick`. Apply **Invoiced**. On hand stays 7.

Do this on `ApexCounter` only when you are teaching the rule. Before a parity run, reset so both databases are back to the fresh seed. The modernized app writes to `ApexCounter_Modern`, never to `ApexCounter`.
