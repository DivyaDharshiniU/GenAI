# Apex Counter — evaluation

Evaluate agent run. Read-only on application code; no app files were edited.

Sources read this run: `ground-truth.md`, `FINISH_LINE.md`, the Blazor project under `src/ApexCounter.Blazor` (`Program.cs`, `Code/*.cs`, `Components/Pages/*.razor`), and live queries against both databases.

## Environment — what is actually running

| Thing | Expected | Observed | Note |
|---|---|---|---|
| Web Forms | `http://localhost:5080/` | reachable, HTTP 200 | OK |
| Blazor | `http://localhost:5088/` (HTTP) | **only `https://localhost:5088/`** answers; plain HTTP on 5088 closes the connection | **Discrepancy vs contract.** `launchSettings.json` sets `applicationUrl` to `https://localhost:5088;http://localhost:51673`, and `Program.cs` calls `app.UseHttpsRedirection()`. The contract asks for `http://localhost:5088/`. App does respond (200) over HTTPS. |
| `ApexCounter` DB | exists | exists | OK |
| `ApexCounter_Modern` DB | exists | exists | OK |
| Both DB state at start | fresh seed | **identical** on both (same 6 orders, same on-hand, 17 ledger rows each) | Correct starting point for checks 5–8 |

## Method and its limit (read this before trusting the scores)

Both apps are up. I verified seed data and page-data queries directly against each database, fetched rendered HTML, and read the Blazor source. I confirmed that the Blazor `Code/*.cs` (OrderData, CatalogData, CustomerData, Rules) is byte-for-byte equivalent to the legacy `Code/*.cs` in business logic and in every on-screen sentence.

**Limit:** checks 5–8 require driving each app's interactive UI (Blazor Server SignalR circuit; Web Forms `__VIEWSTATE` postbacks). I could not reliably script those clicks through HTTP, and I did **not** fabricate browser clicks. So for checks 5–8 below, the sentence and the row/stock effect are taken from the verified, identical code path acting on the verified, identical seed — not from an observed live click. Both databases are still at fresh seed; **no mutation was performed this run.**

Because of that, the parity result at the bottom is a comparison of **two unmutated fresh seeds**. It reports PASS, but that only proves the seeds match. It is **not** a meaningful finish-line parity score. A human (or a UI-capable runner) still needs to perform checks 5–8 on both apps, without a reset in between, then rerun `05_score_parity.sql`.

---

## Checks 1–4 (both apps)

### Check 1 — sign in with password `nope`
- **Web Forms:** PASS. Sentence: "That username and password do not match an active account." (`Messages.LoginFailed`, confirmed in live `Login.aspx` render).
- **Blazor:** PASS. Same sentence. `Login.razor` sets `errorMessage` from `AuthData.TryLogin` / `Messages.LoginFailed`; empty-field validators read "Enter a username." / "Enter a password." (DataAnnotations).
- No status/money/stock change.

### Check 2 — desk tiles on seed day
- Desk query (`DeskData.Counts()`) is identical to legacy. Verified against both DBs: Open orders **2**, active low stock **5**, invoiced today **48.72** (`AC-0005` only).
- **Web Forms:** PASS (tiles 2, 5, 48.72).
- **Blazor:** PASS. `Default.razor` renders `@lblOpen / @lblLow / @lblSales` from the same query; money formatted `N2` under en-US.

### Check 3 — Customers Active, then All + search `OLD-99`
- Data layer (`CustomerData.List`) is identical. Active hides `OLD-99`; All = 7 rows with `OLD-99` on page 2; All + search `OLD-99` returns Retired Yard Co.
- **Web Forms:** PASS.
- **Blazor:** PASS on data, with a **behavioral caveat (fail-ish)**: `Customers/List.razor` `OnActiveFilterChanged` only assigns `activeFilter`; it does **not** rebind the grid. The list refreshes only when **Search** is clicked (or on first load). The Web Forms dropdown auto-posts back and refreshes on change. So "choose All" alone shows no change until Search is pressed. Results are correct once Search is clicked. Flagging as a UI parity difference, not a data difference.

### Check 4 — low-stock report vs catalog "Low stock only"
- `CatalogData.LowStockReport()` = active only (5 rows, no `DISC-1`); `CatalogData.List(term, lowOnly:true)` includes inactive `DISC-1`. Both identical to legacy.
- **Web Forms:** PASS. Report count 5, no `DISC-1`; catalog low-only includes `DISC-1` on page 1.
- **Blazor:** PASS. `Reports/LowStock.razor` shows `@itemCount` = full count 5, page size 4, no `DISC-1`. `Catalog/List.razor` low-only includes `DISC-1`.

---

## Checks 5–8 (Web Forms, then same clicks on Blazor)

Code-derived outcomes on the verified identical seed. Not driven through the live UI this run (see "Limit" above).

### Check 5 — open `AC-0006`, apply Picked
- Expected sentence: "Not enough stock for WR-10. On hand is 3, this order needs 4." Status stays **Open**; `WR-10` stays **3**; no ledger row for the order.
- `AC-0006` lines verified on both DBs: `WR-10` qty 4 / on hand 3 (short), `CT-30` qty 2 / on hand 15.
- `OrderData.ApplyStock` locks products in sorted ProductId order; the short line fails and the whole transaction rolls back. Both apps use this exact code.
- **Web Forms:** expected PASS. **Blazor:** expected PASS (same code). **Not observed via click this run.**

### Check 6 — open `AC-0003`, apply Picked, then Invoiced
- Expected: "Order AC-0003 is now Picked." → `LT-LED` 9 → 7, ledger `pick` −2. Then "Order AC-0003 is now Invoiced." → on hand stays 7. Final status **Invoiced**.
- `AC-0003` line verified both DBs: `LT-LED` qty 2 / on hand 9.
- **Web Forms:** expected PASS. **Blazor:** expected PASS. **Not observed via click this run.**

### Check 7 — new order: Harbor Lights LLC, one `BX-4`
- Expected: order opens, "Opened AC-xxxx."; Subtotal 6.25, Tax 0.31, Total 6.56; `BX-4` stays 80.
- Verified both DBs: `BX-4` price 6.25, on hand 80; Harbor Lights LLC = `HARB-14`, active. Tax = Round(6.25 × 0.05) = Round(0.3125, AwayFromZero) = **0.31**; total **6.56**. Matches ground truth. `CreateOpen` does not touch stock.
- **Note:** both apps assign `OrderNumber = "AC-" + OrderId("0000")` from each DB's own identity. Since both DBs start identical and get the same single insert, the new order number should match (expected `AC-0007`). If the two databases ever diverge in identity, the order *number* could differ even with identical content — watch this in a real run.
- **Web Forms:** expected PASS. **Blazor:** expected PASS. **Not observed via click this run.**

### Check 8 — catalog `CB-100`, adjust quantity `1`, reason `parity`
- Expected sentence: "On hand for CB-100 is now 13." `CB-100` 12 → 13; ledger `adjust` +1.
- Verified both DBs: `CB-100` on hand 12.
- **Web Forms:** expected PASS. **Blazor:** expected PASS (`CatalogData.Adjust`, identical). **Not observed via click this run.**

---

## Defects found while evaluating (for the repair agent)

1. **URL scheme.** Blazor serves `https://localhost:5088` with HTTPS redirection on; the contract asks for `http://localhost:5088/`. `launchSettings.json` + `app.UseHttpsRedirection()`. Not a parity-script failure, but a FINISH_LINE expectation.

2. **Customers filter needs a second click.** `Customers/List.razor` `OnActiveFilterChanged` updates `activeFilter` but never calls `BindGrid()`. Changing the dropdown does nothing until **Search** is pressed. Web Forms auto-posts back. Behavioral difference on check 3.

3. **Ledger `CreatedBy` is written as 0 on Blazor.** `Orders/Detail.razor` reads the user id with `authState.User.FindFirst(ClaimTypes.NameIdentifier)`, but `Login.razor` stores it as a custom claim named `"UserId"`. The lookup returns null, so `currentUserId` stays 0 and pick/cancel ledger rows get `CreatedBy = 0`. The parity script's `stock` check sums `QtyChange` only (ignores `CreatedBy`), so this does **not** fail `05_score_parity.sql`, but it is a real data difference from the legacy app. (Adjust on the catalog page uses its own user-id path — repair agent should confirm that one too.)

None of defects 1–3 block the parity script as written, but 2 and 3 are genuine behavior gaps versus the ground truth.

---

## Parity script

Ran `database/05_score_parity.sql` against `master`.

```
Result       Meaning
------------ -------------------------------------
PARITY: PASS Finish line. The two databases agree.
```

No diff rows. **But** this ran against two **unmutated fresh-seed** databases (checks 5–8 were not driven through the live UIs this run), so this PASS only confirms seed parity. It is not a valid finish-line parity score. Gate 1 (`04_check_seed.sql`) separately returned `SEED: PASS` during the ground-truth run.

## Verdict

- Checks 1, 2, 4: **PASS** on both apps (data + sentences verified).
- Check 3: **PASS on data**, with a UI behavior gap on Blazor (filter dropdown does not refresh without Search).
- Checks 5–8: **expected PASS** by verified-identical code on identical seed, **but not observed through the live UI this run** — must be performed on both apps (no reset between) to earn a real score.
- Parity: **PASS only for the fresh seed**, not a finish-line result yet.

Stop. Not fixing code.
