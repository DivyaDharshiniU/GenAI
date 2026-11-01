# Apex Counter — Playwright behavioural tests

Drives a real browser against both apps and asserts the exact sentences and
values from [`../GROUND_TRUTH.md`](../GROUND_TRUTH.md) / [`../FINISH_LINE.md`](../FINISH_LINE.md).

This is **gate 2** (screens). It does not replace `database/05_score_parity.sql`
(gate 3, the row-level DB comparison).

## Projects (parameterised over both apps)

| Project | URL | Database |
|---|---|---|
| `webforms` | http://localhost:5080 | ApexCounter (the ground truth) |
| `blazor` | http://localhost:5088 | ApexCounter_Modern |

Override URLs with `WEBFORMS_URL` / `BLAZOR_URL` env vars.

## Run

```powershell
# from this e2e/ folder
npm install                 # installs @playwright/test (browsers are reused from the shared cache)

npm test                    # both apps, all eight checks
npm run test:webforms       # only the Web Forms app
npm run test:blazor         # only the Blazor app
npm run report              # open the last HTML report
```

## Order matters

Checks 5-8 mutate database rows. The config runs single-worker, no parallelism,
in file order. Do a fresh `database/reset.ps1` before a full run, and do NOT
reset between the two apps — that is what makes the later parity score meaningful.

## The eight checks

1. Wrong password -> "That username and password do not match an active account."
2. Desk tiles -> 2, 5, 48.72
3. Customers: Active hides OLD-99; All + search OLD-99 shows Retired Yard Co.
4. Low-stock report count 5 (no DISC-1); catalog low-only includes DISC-1
5. AC-0006 Picked -> "Not enough stock for WR-10. On hand is 3, this order needs 4.", stays Open
6. AC-0003 Picked (LT-LED 9->7) then Invoiced (stays 7)
7. New order Harbor Lights LLC + BX-4 -> 6.25 / 0.31 / 6.56
8. Catalog CB-100 adjust +1 -> "On hand for CB-100 is now 13."
