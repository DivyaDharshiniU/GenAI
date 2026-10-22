# Finish line — Apex Counter

You are finished only when all three gates pass. A Blazor site that opens is not the finish line. Matching the Web Forms app is.

| Gate | Proves | Command or action | Pass looks like |
|---|---|---|---|
| 1. Seed | The legacy database is the ground truth | `04_check_seed.sql` | `SEED: PASS` |
| 2. Screens | A clerk sees the same words on both apps | The eight checks below | 8 / 8 |
| 3. Parity | The .NET 8 app wrote the same rows as .NET Framework 4.0 | `05_score_parity.sql` | `PARITY: PASS` |

Gate 1 is the legacy app. Gates 2 and 3 are the port. Do gate 1 before AWS Transform. If gate 1 fails, run `reset.ps1` again and check the SQL Server name. Do not edit the Web Forms project, and do not start Transform.

---

## Gate 1 — seed

Reset, then check. Run this on the same day you seeded, because "invoiced today" uses today's date.

```powershell
cd database
powershell -ExecutionPolicy Bypass -File .\reset.ps1
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d ApexCounter -i .\04_check_seed.sql
```

`SEED: PASS` means the rows match [GROUND_TRUTH.md](./GROUND_TRUTH.md). Then open `http://localhost:5080/` and confirm the desk shows **2**, **5**, and **48.72**. If SQL passes and the desk does not, the bug is in the Web Forms pages. Stop there.

---

## Gate 2 — the same eight things on both screens

Web Forms: `http://localhost:5080/` database `ApexCounter`  
Blazor: `http://localhost:5088/` database `ApexCounter_Modern`

Sign in as `clerk` / `counter2011`. The header says **Mara Ellison**.

Do these on the Web Forms app first. Then do the same clicks on the Blazor app. A check passes only if both apps do it.

| # | Do this | Pass |
|---|---|---|
| 1 | Sign in with password `nope` | "That username and password do not match an active account." |
| 2 | Open the desk, same day as the seed | Tiles are 2, 5, and 48.72 |
| 3 | Customers, filter Active, then All, and search `OLD-99` | Active hides Retired Yard Co. All shows account `OLD-99`. An unfiltered All list puts it on page 2 |
| 4 | Low stock report, then Catalog with "Low stock only" | The count above the report is 5, and `DISC-1` is absent. Catalog low-only includes `DISC-1` on page 1 |
| 5 | Open `AC-0006` and apply Picked | "Not enough stock for WR-10. On hand is 3, this order needs 4." Status stays Open |
| 6 | Open `AC-0003`, apply Picked, then apply Invoiced | Status ends Invoiced. `LT-LED` on hand goes from 9 to 7 and stays 7 |
| 7 | New order: Harbor Lights LLC, one Steel box 4 inch (`BX-4`) | Order opens. Subtotal 6.25, tax 0.31, total 6.56. `BX-4` on hand stays 80 |
| 8 | Catalog, `CB-100`, adjust quantity `1`, reason `parity` | "On hand for CB-100 is now 13." |

Checks 5 through 8 change the database. Do them on **both** apps before gate 3. Do not reset between the two apps.

---

## Gate 3 — parity

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d master -i .\05_score_parity.sql
```

The script compares the two databases. It ignores clock times. It checks customers, prices, on-hand, order status, money, lines, stock ledger totals, and password hashes.

- Any row in the result is a miss. `LegacyValue` is the .NET Framework 4.0 app. `ModernValue` is the .NET 8 app. Fix the Blazor project and run the gate again from a fresh `reset.ps1`.
- `PARITY: PASS` together with 8 / 8 on the screens is the finish line.

---

## What this does not grade

How the Razor looks, how fast it is, and whether AWS Transform needed help. Those can be discussed in the demo. They are not the finish line.
