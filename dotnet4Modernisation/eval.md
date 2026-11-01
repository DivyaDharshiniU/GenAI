# Apex Counter — evaluation

Evaluate agent run. Read-only on application code; no app files were edited.

Sources: `ground-truth.md`, `FINISH_LINE.md`. Checks were driven through a real
browser with the Playwright suite in `e2e/` (both apps, single fresh seed, no
reset between apps), then `database/05_score_parity.sql` was run.

## Environment

| Thing | Expected | Observed |
|---|---|---|
| Web Forms | http://localhost:5080 | up, HTTP 200 |
| Blazor | http://localhost:5088 | up, HTTP 200 (plain HTTP, matches contract) |
| Databases | ApexCounter, ApexCounter_Modern | both present |
| Start state | fresh seed on both | one `reset.ps1`, then both apps run back-to-back, **no reset between** |

Gate 1: `04_check_seed.sql` → **SEED: PASS**.

Method: checks 1–8 run on Web Forms first, then the same clicks on Blazor, in a
single Playwright run (webforms project, then blazor, single worker). Checks 5–8
mutate rows; no reset between the two apps, so the parity score is meaningful.

## Checks 1–4 (both apps)

| # | Check | Web Forms | Blazor | Sentence / value |
|---|---|---|---|---|
| 1 | Wrong password `nope` | PASS | PASS | "That username and password do not match an active account." |
| 2 | Desk tiles | PASS | PASS | 2, 5, 48.72 |
| 3 | Customers Active → All + search OLD-99 | PASS | PASS | Active hides Retired Yard Co.; All + search shows OLD-99 / Retired Yard Co. |
| 4 | Low-stock report vs catalog low-only | PASS | PASS | Report 5, no DISC-1; catalog low-only shows DISC-1 |

Header shows **Mara Ellison** on both after sign-in.

## Checks 5–8 (Web Forms, then same clicks on Blazor)

| # | Check | Web Forms | Blazor | Notes |
|---|---|---|---|---|
| 5 | AC-0006 apply Picked | PASS | PASS | "Not enough stock for WR-10. On hand is 3, this order needs 4." Stays Open; WR-10 stays 3 on both. |
| 6 | AC-0003 Picked then Invoiced | PASS | PASS | "Order AC-0003 is now Picked." / "…is now Invoiced." LT-LED 9→7 on both; final Invoiced. |
| 7 | New order Harbor Lights LLC + BX-4 | PASS | **FAIL** | Web Forms writes AC-0007 (Open, 6.25/0.31/6.56). Blazor: the browser stayed on the New-order form after "Open order" — the order was **not persisted** (ApexCounter_Modern has no AC-0007). |
| 8 | Catalog CB-100 adjust +1 | PASS | **PASS (now fixed)** | Both show "On hand for CB-100 is now 13." and CB-100 = 13 on both DBs. Previously failed on Blazor; the repair-agent fix to `Catalog/Edit.razor` resolved it. |

Playwright this run: **Web Forms 8/8 pass; Blazor 7/8 pass** (only check 7 fails).

### Database state after the run (queried directly)

| Item | ApexCounter | ApexCounter_Modern |
|---|---|---|
| Order count | 7 (incl. AC-0007 Open, 6.56) | 6 (**no AC-0007**) |
| CB-100 on hand | 13 | 13  ✅ (was 12 — now fixed) |
| LT-LED / WR-10 / BX-4 | 7 / 3 / 80 | 7 / 3 / 80 |

## Parity script

`sqlcmd ... -d master -i database/05_score_parity.sql`

```
PARITY: FAIL. The .NET 8 database does not match the .NET Framework 4.0 database.
```

Diff rows (LegacyValue = .NET Framework 4.0, ModernValue = .NET 8):

| CheckName | KeyValue | LegacyValue | ModernValue |
|---|---|---|---|
| order | AC-0007 | `HARB-14\|Open\|6.25\|0.31\|6.56` | NULL |
| line | AC-0007/BX-4 | `1\|6.25\|6.25\|Steel box 4 inch` | NULL |

Both rows are the same root cause: the Blazor "Open order" (check 7) did not
persist the order. The CB-100 product/stock diffs from the previous run are
**gone** — the adjust fix closed them. Parity improved from 4 diff rows to 2.

## Progress vs previous run

| | Previous run | This run |
|---|---|---|
| Blazor check 8 (adjust) | FAIL (CB-100 12 vs 13) | **PASS** (13 on both) |
| Blazor check 7 (new order) | data not persisted | still not persisted |
| Parity diff rows | 4 (order, line, product, stock) | **2** (order, line) |

## Verdict

- Checks 1–6: PASS on both apps.
- Check 8: **now PASS on Blazor** (repair fix verified).
- Check 7: **FAIL on Blazor** — the new order is not written to ApexCounter_Modern.
  The browser stays on the New-order form after clicking "Open order"; this looks
  like the same interactive `@onclick` / `@bind` commit timing issue that affected
  the adjust panel (`Orders/New.razor` "Open order" button + its `@bind` customer/
  product selects).
- Gate 3: **PARITY: FAIL** with 2 diff rows, both from the missing AC-0007.

Not the finish line yet. One remaining defect: the Blazor new-order save does not
commit. Hand to the repair agent (edit only `src/ApexCounter.Blazor`, likely
`Components/Pages/Orders/New.razor`).

Stop. Not fixing code.
