# Apex Counter — ground-truth notes (observed)

Ground-truth agent run. Read-only. No application files were edited.

**Authority:** Web Forms app at `http://localhost:5080/`, database `ApexCounter` on `(localdb)\MSSQLLocalDB`, signed in as `clerk` / `counter2011`. Header shows **Mara Ellison** (the person, not the username).

**Sources used:** `GROUND_TRUTH.md`, `FINISH_LINE.md`, `src/ApexCounter/Code/Rules.cs` (verbatim sentences and rounding), `src/ApexCounter/Code/OrderData.cs` (status/stock/ledger logic), the live `Login.aspx` render, and direct queries against `ApexCounter`.

**Formatting:** money is `en-US` with two decimals (`48.72`); dates like `11 Oct 2026 09:15`; a blank picked/invoiced time renders as a single hyphen `-`.

---

## Verified seed state (queried from ApexCounter)

Gate 1 result: **`SEED: PASS`**.

Orders:

| Order | Status | Total | Invoiced at |
|---|---|---|---|
| AC-0001 | Invoiced | 77.18 | 2026-10-20 (not today) |
| AC-0002 | Picked | 92.40 | - |
| AC-0003 | Open | 304.50 | - |
| AC-0004 | Cancelled | 32.81 | - |
| AC-0005 | Invoiced | 48.72 | today |
| AC-0006 | Open | 243.39 | - |

- Open orders: **2** (`AC-0003`, `AC-0006`).
- Invoiced today: **48.72** (`AC-0005` only; `AC-0001` was invoiced on an earlier date).

Products at or under reorder point:

| SKU | On hand | Reorder | Active |
|---|---|---|---|
| BR-40 | 8 | 12 | yes |
| CB-250 | 4 | 6 | yes |
| CT-30 | 15 | 15 | yes |
| TM-1 | 0 | 5 | yes |
| WR-10 | 3 | 8 | yes |
| DISC-1 | 0 | 2 | **no (inactive)** |

- Active low stock: **5** (BR-40, CB-250, CT-30, TM-1, WR-10).
- Catalog "Low stock only": **6 rows**, because it also shows inactive `DISC-1`.
- Low-stock report: **5 rows**, active only, no `DISC-1`.

Customers:

| Account | Company | Contact | City | Active |
|---|---|---|---|---|
| ACME-01 | Acme Switchgear | Lina Shah | Dubai | yes |
| DUNE-19 | Dune Fabrication | Yusuf Haddad | Ajman | yes |
| HARB-14 | Harbor Lights LLC | Omar Nasser | Sharjah | yes |
| NILE-07 | Nile Cold Storage | Priya Raman | Abu Dhabi | yes |
| OLD-99 | Retired Yard Co. | Closed Account | Dubai | **no (inactive)** |
| PALM-03 | Palm Clinic Fit-Out | Sara Ibrahim | Dubai | yes |
| QASR-02 | Qasr Mall Services | Helen Cho | Dubai | yes |

- Active customers: **6**, paged 5 + 1.
- All customers: **7** (includes inactive `OLD-99`). On an unfiltered **All** list, `OLD-99` is on **page 2** unless you search `OLD-99`.
- **Active** filter **hides** `OLD-99`. **Inactive** filter shows only Retired Yard Co. on a fresh seed.

---

## Pages — exact sentences and effects

### `Login.aspx`
Only this page and the stylesheet are open to anonymous visitors. Confirmed from the live render:
- Empty username field: **"Enter a username."**
- Empty password field: **"Enter a password."**
- Wrong password or unknown user: **"That username and password do not match an active account."**
- Database unreachable: **"The counter database is not reachable. Run database/reset.ps1 against LocalDB, then refresh."**

No order, money, or stock change. On success the header shows the person's name (**Mara Ellison** for `clerk`, **Owen Briggs** for `supervisor`).

### `Default.aspx` — Desk
Three tiles: open-order count = **2**, active low-stock count = **5**, sum of **Invoiced** order totals whose invoice date is today = **48.72**. Grid = latest orders, newest first, at most 8, no paging. **Open** navigates to that order. No sentence; read-only. No money/stock change.

### `Customers/List.aspx`
Opens filtered to **Active**. Search matches company, contact, account code, or city. Page size 5.
- Active: 6 rows (5 + 1), `OLD-99` hidden.
- Inactive: Retired Yard Co. only.
- All: 7 rows; `OLD-99` on page 2 unless searched.
No money/stock change.

### `Customers/Edit.aspx`
New customer or `?id=`. Account code is unique.
- Save on a new row redirects to `Edit.aspx?id=` and shows **"Saved {account}"** (`Messages.CustomerSaved = "Saved {0}."`).
- Duplicate account code: **"Account code {0} is already used."**
- Missing record: **"That record is not on file."**
Inactive customers disappear from the new-order dropdown. No stock change.

### `Catalog/List.aspx`
Search matches SKU or name. Page size 5. A row is highlighted when on hand is at or under the reorder point, including inactive products. **Low stock only** = 6 rows and includes inactive `DISC-1` on page 1. This is not the same list as the report. No money/stock change.

### `Catalog/Edit.aspx`
Price and reorder point save without changing on-hand and without rewriting order lines.
- Save: **"Saved {0}."** (`Messages.ProductSaved`).
- Duplicate SKU: **"SKU {0} is already used."**
- Bad price: **"Enter a price of 0 or more."**
- Bad reorder: **"Enter a reorder point of 0 or more."**
A new product starts at on hand 0; the adjust panel appears after the first save. Adjust writes a `StockLedger` row with reason `adjust`:
- Success: **"On hand for {0} is now {1}."** (`Messages.AdjustOk`).
- Zero: **"Enter a quantity that is not zero."** (refused).
- Below zero: **"That adjustment would take {0} below zero. On hand is {1}."** (refused; on hand stays put).
- Missing reason: **"Enter a reason for the stock change."**
- Bad adjust value: **"Enter a whole number for the adjustment, other than zero."**

### `Orders/List.aspx`
Filter by status and by customer company. Page size 5. Newest first (by CreatedAt desc, OrderId desc). No money/stock change.

### `Orders/New.aspx`
Dropdowns list **active** customers and **active** products only. Draft is remembered when you leave the page; dropped on **Clear draft** or **Sign out**.
- Add a line: **"Added {0}."** (`Messages.LineAdded`).
- Add a SKU already on the draft: quantity is added and the **first** unit price is kept — **"Added {0} more to {1}. On this order the quantity is now {2}."** (`Messages.LineMerged`).
- No customer: **"Choose a customer."**
- Inactive customer: **"That customer is not active."**
- No lines: **"Add at least one line."**
- No product: **"Choose a product."**
- Inactive product: **"That product is not active."**
- Bad quantity: **"Enter a whole number of 1 or more."**

Tax preview = 5 percent, half away from zero, two decimals. **Open order** writes status `Open`, copies the draft onto the lines, and **does not change stock**. Success sentence **"Opened {0}."** (`Messages.OrderSaved = "Opened {0}."`), then lands on the new order's detail page. Order number is `AC-` + id padded to four digits.

### `Orders/Detail.aspx`
Shows the **stored** subtotal, tax, and total (not recomputed). Ledger rows for the order are shown underneath. Status transitions (confirmed from `OrderData.Transition`):

| From | Allowed to | Stock effect | Ledger reason |
|---|---|---|---|
| Open | Picked | deducts stock (or refused if short) | `pick` (QtyChange negative) |
| Open | Cancelled | no stock change | none |
| Picked | Invoiced | no stock change | none |
| Picked | Cancelled | adds stock back | `cancel-return` (QtyChange positive) |
| Invoiced | — | Apply disabled | — |
| Cancelled | — | Apply disabled | — |

Sentences:
- Same status chosen: **"No status change."**
- Illegal move: **"An order that is {0} cannot move to {1}."**
- Success: **"Order {0} is now {1}."** (`Messages.StatusApplied`).
- Short pick: **"Not enough stock for {0}. On hand is {1}, this order needs {2}."** — order stays Open, nothing deducted, no ledger row for that order.

### `Reports/LowStock.aspx`
Active products only, page size 4. The count above the grid is the **full** count (5), not the page count. No `DISC-1`. **Open** navigates to the catalog edit page. No money/stock change.

### `Account/Password.aspx`
Current password must match. New password at least 6 characters and must match the confirmation. The salt is kept; only the hash changes. Changing `clerk` leaves `supervisor` on `counter2011`.
- Success: **"Password updated."**
- Current wrong: **"The current password does not match."**
- Too short: **"The new password must be at least 6 characters."**
- Mismatch: **"The new password and the confirmation do not match."**

---

## The eight FINISH_LINE checks

### Check 1 — Sign in with password `nope`
Sentence on screen: **"That username and password do not match an active account."** No order/money/stock change.

### Check 2 — Desk on seed day
Tiles: **2** (open orders), **5** (active low stock), **48.72** (invoiced today). Read-only.

### Check 3 — Customers Active, then All, search `OLD-99`
- **Active**: Retired Yard Co. (`OLD-99`) is hidden; 6 active rows (5 + 1).
- **All**: 7 rows; account `OLD-99` appears, on **page 2** of the unfiltered All list.
- **All + search `OLD-99`**: shows **Retired Yard Co.** No money/stock change.

### Check 4 — Low-stock report vs Catalog "Low stock only"
- Low-stock report count above the grid: **5**; `DISC-1` is **absent** (active only).
- Catalog **Low stock only**: includes `DISC-1` on **page 1** (inactive low products shown). No money/stock change.

### Check 5 — Open `AC-0006`, apply Picked
Sentence: **"Not enough stock for WR-10. On hand is 3, this order needs 4."**
- Order status stays **Open**.
- `WR-10` on hand stays **3**.
- No stock deducted; no ledger row written for this order.

### Check 6 — Open `AC-0003`, apply Picked, then apply Invoiced
- Apply Picked: **"Order AC-0003 is now Picked."** `LT-LED` on hand goes **9 → 7**; ledger row `pick`, QtyChange **-2**.
- Apply Invoiced: **"Order AC-0003 is now Invoiced."** On hand stays **7** (invoice does not move stock).
- Final status: **Invoiced**.

### Check 7 — New order: Harbor Lights LLC, one Steel box 4 inch (`BX-4`)
- Order opens with sentence **"Opened AC-..."** (number = `AC-` + new id, four digits).
- Money: subtotal **6.25**, tax **0.31** (5% of 6.25 = 0.3125, rounded half-away-from-zero = 0.31), total **6.56**.
- `BX-4` on hand stays **80** (opening an order does not move stock).

### Check 8 — Catalog `CB-100`, adjust quantity `1`, reason `parity`
Sentence: **"On hand for CB-100 is now 13."** (on hand 12 → 13). Writes a `StockLedger` row, reason `adjust`, QtyChange +1.

---

## Rules that must hold (from Rules.cs / OrderData.cs)
- Stock moves only on **pick**, never on open and never on invoice.
- A short pick is refused: nothing deducted, order stays **Open**.
- Cancelling a **Picked** order returns stock (`cancel-return`); cancelling an **Open** order does not.
- **Invoiced** and **Cancelled** are terminal.
- On hand cannot go below zero.
- Tax = 5% of subtotal, `Math.Round(value, 2, MidpointRounding.AwayFromZero)`; unit price is copied onto the line.
- A catalog price change does not rewrite old lines; re-adding a SKU keeps the first price and adds the quantity.
- Low-stock report = active only; catalog "low stock only" also shows inactive low products.
- Changing one user's password leaves the other on `counter2011`.

Stop.
