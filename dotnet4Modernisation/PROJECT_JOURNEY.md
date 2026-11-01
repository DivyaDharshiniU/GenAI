# Apex Counter — Modernization Project Journey

A reference record of modernizing the **Apex Counter** parts-desk application from
legacy ASP.NET Web Forms to .NET 8 Blazor Server, including how AWS Transform did
the port, how a three-agent workflow guarded quality, and how Playwright closed
the UI-verification gap.

> Scope note: the legacy Web Forms app (`src/ApexCounter`) is the immutable
> **ground truth** and must never be edited. All modernization work happens in the
> Blazor copy (`src/ApexCounter.Blazor`). The behavior contract is
> [`GROUND_TRUTH.md`](./GROUND_TRUTH.md); the acceptance gates are
> [`FINISH_LINE.md`](./FINISH_LINE.md).

---

## 1. Goal

Take a 2011-era counter-desk web app and modernize it **without changing its
behavior**. A clerk using the new app must see the same words on screen and the
app must write the same database rows as the legacy app.

| | Start with | Hand in |
|---|---|---|
| Runtime | .NET Framework 4.0 | **.NET 8** |
| UI framework | ASP.NET Web Forms | **Blazor Server** |
| Pages | `.aspx` + code-behind | `.razor` |
| Layout | `Site.Master` | Blazor layout |
| Config | `Web.config` | `appsettings.json` |
| Startup | `Global.asax` | `Program.cs` |
| Database | SQL Server `ApexCounter` | same tables in `ApexCounter_Modern` |
| URL | `http://localhost:5080/` | `http://localhost:5088/` |

Hard constraints: do **not** convert to MVC, Razor Pages, WebAssembly, or a plain
API. Target **.NET 8** (not 10). Keep the UI **Blazor Server**.

---

## 2. Before / After Architecture Overview

### Before — ASP.NET Web Forms (.NET Framework 4.0)

```
Browser  ──HTTP──▶  IIS Express (port 5080)
                       │
            Global.asax / Web.config
                       │
            Site.Master  +  *.aspx / *.aspx.cs   (server controls, __VIEWSTATE postbacks)
                       │
            Code/*.cs  (OrderData, CatalogData, CustomerData, AuthData, Rules, Db)
                       │
                  SQL Server  ApexCounter   ((localdb)\MSSQLLocalDB)
```

- Stateful page model: `__VIEWSTATE` round-trips, `AutoPostBack` on controls.
- Forms authentication; `HttpContext.Current.Session` holds user id / display name.
- Business rules and the exact on-screen sentences live in `Code/Rules.cs`.

### After — Blazor Server (.NET 8)

```
Browser  ──HTTP + SignalR circuit──▶  Kestrel (port 5088)
                       │
                  Program.cs  (middleware, auth, endpoints)
                       │
     Components/Layout/*.razor  +  Components/Pages/**/*.razor   (InteractiveServer)
                       │
            Code/*.cs  (ported almost verbatim — same SQL, same Rules)
                       │
              SQL Server  ApexCounter_Modern   ((localdb)\MSSQLLocalDB)
```

- Interactive rendering over a **SignalR circuit**; UI events (`@onclick`,
  `@bind`) travel to the server and back.
- Cookie authentication; user id / display name carried as **claims**
  (`UserId`, `DisplayName`) instead of Session.
- The data/business layer (`Code/*.cs`) was ported essentially **byte-for-byte** —
  the same SQL queries, the same `Money.Round`, the same `Messages.*` sentences.
  This is why most behavior matched immediately; the gaps were all in the UI layer
  (how Blazor commits input and runs handlers), not in the rules.

### Key architectural mapping

| Web Forms concept | Blazor Server equivalent |
|---|---|
| `*.aspx` / code-behind | `*.razor` component under `Components/Pages` |
| `Site.Master` | `Components/Layout/MainLayout.razor` (+ `EmptyLayout.razor` for login) |
| `Web.config` | `appsettings.json` |
| `Global.asax` | `Program.cs` |
| Forms auth + Session | Cookie auth + claims |
| `AutoPostBack` / `__VIEWSTATE` | SignalR circuit + `@bind` / `@onclick` |
| IIS Express @ 5080 | Kestrel @ 5088 |

---

## 3. Transformation Journey (chronological)

1. **Ran the legacy app and captured the ground truth.** Verified the seed with
   `04_check_seed.sql` (`SEED: PASS`) and walked every page per `GROUND_TRUTH.md`.
2. **Ported a copy with AWS Transform** into `src/ApexCounter.Blazor` (details in
   §4). Confirmed it came out as genuine Blazor Server on `net8.0`.
3. **Pointed the copy at the second database** (`ApexCounter_Modern`) and set the
   URL to port 5088.
4. **Ran the three-agent quality loop** (§5): ground-truth → evaluate → repair,
   repeating evaluate/repair until the gates pass.
5. **Added a Playwright suite** (§7) to actually drive both UIs — the piece that
   let the evaluate step observe interactive behavior instead of guessing.
6. **Fixed the defects Playwright + parity surfaced**, one repair cycle at a time
   (§6), re-evaluating after each.

---

## 4. How the Transformation Was Done Using AWS Transform

AWS Transform (the .NET modernization job in the AWS Toolkit for Visual Studio)
performed the first automated rewrite. Workflow used:

1. **Copy first.** Transform writes onto the project you select, so the legacy
   project was copied to `src/ApexCounter.Blazor` before running anything —
   protecting the ground truth.
2. **Opened only the Blazor copy** (`src/ApexCounter.Blazor/ApexCounter.csproj`),
   not the legacy solution.
3. **Authenticated** via IAM Identity Center (AWS Transform authentication), not
   an access key.
4. **Port with AWS Transform** → chose **.NET 8** target, **Interactive**
   transformation mode.
5. **Reviewed the plan before approving** — it had to say *ASP.NET Web Forms →
   Blazor Server*. (If a plan says MVC / Razor Pages / WebAssembly / "leave Web
   Forms", reject and correct it.)
6. **Downloaded `NextSteps.md`** from the job — the repair step uses it.
7. **Confirmed the copy**: `src/ApexCounter` still had `.aspx`; the copy had
   `.razor` and `net8.0`.

**The official mapping Transform follows**

| Web Forms | Blazor Server |
|---|---|
| `*.aspx`, `*.aspx.cs` | `*.razor` under `Components/Pages` |
| `Site.Master` | a layout `.razor` |
| `Web.config` | `appsettings.json` |
| `Global.asax` | `Program.cs` |
| IIS Express 5080 | Kestrel 5088 |

**What Transform did well** — produced a real Blazor Server project (`net8.0`,
`MapRazorComponents` + `AddInteractiveServerComponents`), ported the whole
`Code/*.cs` data layer and `Rules.cs` verbatim, replaced Forms auth with cookie
auth scaffolding, and migrated `Web.config` connection strings to
`appsettings.json`.

**What Transform left for the repair step** — the project built and ran, but a
running Blazor home page is not "done". The gaps were all in the interactive UI
plumbing (see §6): login that didn't actually sign in, a layout that showed the
wrong name, and interactive inputs/handlers that didn't commit over the SignalR
circuit. Transform also left leftover artifacts (`.aspx`, `Site.Master`,
`Web.config`, an `artifactWorkspace/` folder) that are noise, plus `PORT-TODO`
comments flagging decisions for a human.

---

## 5. How Quality Was Ensured — the Three-Agent Workflow

Quality is enforced by three agents with **strict, non-overlapping roles**, which
prevents the classic failure of "fixing the test instead of the code".

```
Agent 1 (ground truth)  ──once──▶  Agent 2 (evaluate)  ◀──loop──▶  Agent 3 (repair)
     read-only                        read-only                     writes ONLY the Blazor project
```

### Agent 1 — Ground Truth (read-only, runs once)
Reads `GROUND_TRUTH.md`, `FINISH_LINE.md`, and `Code/Rules.cs`, then walks the
running legacy app and records the exact on-screen sentences and the status / money
/ stock outcome of every page and every check. Output: `ground-truth.md`.
It **never edits code**. This makes the legacy app's behavior the fixed reference.

### Agent 2 — Evaluate (read-only, runs repeatedly)
Reads `ground-truth.md` + `FINISH_LINE.md`, runs the eight checks on **both** apps
(checks 1–4 read-only; 5–8 mutate rows), then runs `05_score_parity.sql`. Output:
`eval.md` with pass/fail per check and the parity result. It **cannot edit code**,
so it can never "cheat" a pass.

Critical rule: checks 5–8 change rows, so you do a single `reset.ps1`, run both
apps back-to-back **without a reset between them**, then score parity. A reset in
the middle makes the parity score meaningless.

### Agent 3 — Repair (the only writer)
Reads `eval.md` + `NextSteps.md` and edits **only** `src/ApexCounter.Blazor` to
make the Blazor app match. Builds the project and stops — it does **not** declare
success. The evaluate agent decides.

### The loop and the three gates (`FINISH_LINE.md`)
Run Agent 1 once, then repeat Agent 2 → Agent 3 until all three gates pass:

| Gate | Proves | Pass looks like |
|---|---|---|
| 1 — Seed | Legacy DB is the ground truth | `04_check_seed.sql` → `SEED: PASS` |
| 2 — Screens | Same words on both apps | 8/8 on both |
| 3 — Parity | Same rows written | `05_score_parity.sql` → `PARITY: PASS` |

### Why the separation matters
- The reference (`GROUND_TRUTH.md` + the port-5080 app) is **immutable**.
- The evaluator **can't edit code**, so a green result is trustworthy.
- The repairer can **only** touch the Blazor copy, so it can't corrupt the ground
  truth or the tests.

---

## 6. Data From Our Runs

### Gate 1 — seed (always passed)
`04_check_seed.sql` against `ApexCounter` → **`SEED: PASS`**. Verified seed values:
open orders 2 (`AC-0003`, `AC-0006`), active low-stock 5, invoiced today 48.72
(`AC-0005`), 6 active customers + inactive `OLD-99`, `DISC-1` inactive.

### Gate 2 — the eight screen checks

| # | Check | Expected (ground truth) |
|---|---|---|
| 1 | Wrong password | "That username and password do not match an active account." |
| 2 | Desk tiles | 2 / 5 / 48.72 |
| 3 | Customers Active→All + search OLD-99 | Active hides Retired Yard Co.; All shows OLD-99 |
| 4 | Low-stock report vs catalog low-only | Report 5, no DISC-1; catalog low-only includes DISC-1 |
| 5 | AC-0006 apply Picked | "Not enough stock for WR-10. On hand is 3, this order needs 4." stays Open |
| 6 | AC-0003 Picked then Invoiced | LT-LED 9→7, stays 7; ends Invoiced |
| 7 | New order Harbor Lights + BX-4 | subtotal 6.25 / tax 0.31 / total 6.56; BX-4 stays 80 |
| 8 | Catalog CB-100 adjust +1 | "On hand for CB-100 is now 13." |

### Progress across evaluate/repair cycles

| Run | Web Forms | Blazor screens | Parity diff rows | Notes |
|---|---|---|---|---|
| Initial (code-reasoned, no live UI) | 8/8 | n/a (not driven) | PASS but meaningless | parity ran on two unmutated seeds |
| Playwright baseline (login reverted on purpose) | 8/8 | 0/8 | — | proved the suite catches real failures |
| After login + display-name fixes | 8/8 | 6–7/8 (flaky) | 4 (order, line, product, stock) | login works; adjust + new order still off |
| After adjust fix (repair agent) | 8/8 | **7/8** | **2** (order, line) | CB-100 now 13 on both; only new-order remains |

### Final parity diff (most recent run) — `PARITY: FAIL`, 2 rows

| CheckName | KeyValue | LegacyValue | ModernValue |
|---|---|---|---|
| order | AC-0007 | `HARB-14\|Open\|6.25\|0.31\|6.56` | NULL |
| line | AC-0007/BX-4 | `1\|6.25\|6.25\|Steel box 4 inch` | NULL |

Both rows are the same root cause: the Blazor **new-order "Open order"** action
did not persist. Database counts confirmed it — `ApexCounter` had 7 orders
(incl. `AC-0007`), `ApexCounter_Modern` had 6.

### Defects found and their status

| # | Defect | How found | Status |
|---|---|---|---|
| 1 | Served HTTPS on 5088 instead of HTTP | eval (contract check) | **Fixed** — removed `UseHttpsRedirection`, set `applicationUrl` to `http://localhost:5088` |
| 2 | CSS not loading (plain UI) | manual observation | **Fixed** — moved `Content/site.css` into `wwwroot/Content/` (ASP.NET Core serves static files from `wwwroot`) |
| 3 | Login silently failed (stayed on /Login) | Playwright | **Fixed** — `SignInAsync` can't write the cookie inside a Blazor render; moved sign-in to a real HTTP `POST /auth/login` endpoint, login page made static-SSR |
| 4 | Header showed "clerk" not "Mara Ellison" | Playwright | **Fixed** — `MainLayout` now reads the `DisplayName` claim |
| 5 | Ledger `CreatedBy = 0` | code read during eval | **Fixed** — read custom `"UserId"` claim, not `ClaimTypes.NameIdentifier` (Orders/Detail + Catalog/Edit) |
| 6 | Stock adjust didn't apply (CB-100 stayed 12) | Playwright + parity | **Fixed** — `@bind:event="oninput"` on adjust inputs so values commit before the click |
| 7 | New order "Open order" didn't persist | Playwright + parity | **Open** — same class as #6, in `Orders/New.razor`; next repair target |

---

## 7. How Playwright Was Used

### Why it was needed
The evaluate agent originally could only script raw HTTP, which **cannot drive a
Blazor Server SignalR circuit or Web Forms `__VIEWSTATE` postbacks**. So checks
5–8 could only be *reasoned about*, not observed. Playwright runs a real browser
(Chromium) with a live JS engine + WebSocket, so it clicks through both apps
exactly like a user — closing that gap.

### Structure (`e2e/`)
- `playwright.config.ts` — two projects parameterised over both apps:
  `webforms` (http://localhost:5080) and `blazor` (http://localhost:5088).
  Single worker, no parallelism, so the mutating checks 5–8 run in order.
- `tests/fixtures.ts` — custom test options (`appUrl`, `appKind`), a `pageUrl()`
  helper mapping logical pages to `.aspx` vs clean routes, and a shared `login()`.
- `tests/ground-truth.spec.ts` — one spec, the eight checks, run against both apps
  (8 × 2 = 16 tests).

### Cross-app selector strategy
Both apps share `Code/*.cs` and `site.css`, so classes (`.tile`, `.grid`) and
control ids match — but Web Forms uses a master page that **prefixes** ids
(`ctl00_MainContent_lblStatus`). Solution: use **suffix attribute selectors**
(`[id$="lblStatus"]`) that match both the prefixed Web Forms id and the exact
Blazor id, plus role/text selectors (`getByRole('button',{name:'Sign in'})`).

### What Playwright caught that HTTP could not
- **Login failure** — the reverted-login experiment: all 8 Blazor checks went red,
  with the app stuck on `/Login`. Proved the tool catches it.
- **Wrong header name** — "clerk" instead of "Mara Ellison".
- **Interactive commit bugs** — adjust (#6) and new order (#7): the on-screen
  preview looked right, but the `@onclick`/`@bind` action didn't commit over the
  circuit. (Parity then confirmed the data wasn't written.)

### What Playwright does NOT replace
Gate 3 (parity) still belongs to `05_score_parity.sql`. Playwright verifies the
**screen**; parity verifies the **rows written**. Example: check 7 passed its
on-screen assertion in one run yet the order wasn't persisted — only parity (or a
direct DB query) caught that. **Run both, in order:** reset → Playwright checks 5–8
on both apps (no reset between) → parity script.

### Reusing cached browsers
Playwright browsers live in a shared per-user cache
(`%USERPROFILE%\AppData\Local\ms-playwright`). With Chromium already present,
install with `PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 npm install` to avoid a
re-download.

### Handy commands
```powershell
cd e2e
npm install                         # browsers reused from the shared cache
npx playwright test                 # both apps, all 8 checks
npx playwright test --project=blazor
npx playwright test --reporter=json # machine-readable results
npx playwright show-report --port 9500
```

---

## 8. Key Learnings and Challenges

- **The business layer ported cleanly; the UI layer was the whole battle.**
  `Code/*.cs` + `Rules.cs` came across byte-for-byte, so money, stock rules, and
  sentences matched immediately. Every real defect was in Blazor's interactive
  plumbing — auth, claims, and event/`@bind` commit timing.

- **Blazor Server interactive timing is the recurring trap.** `@bind` commits on
  the change event over the SignalR circuit; clicking a button before the bind
  commits means the handler sees stale/empty values. Fix pattern:
  `@bind:event="oninput"` (commit as you type) so the action has the data.
  This single pattern explained the adjust bug and the new-order bug.

- **Cookie sign-in cannot happen inside a Blazor render.** `HttpContext.SignInAsync`
  needs a live HTTP response; inside an interactive circuit (or even static SSR
  render) the response is already committed, so the auth cookie is never written.
  Fix: post the login form to a real minimal-API endpoint (`POST /auth/login`).

- **Claims vs Session.** Web Forms kept user id in Session; the Blazor port stored
  it as a custom `"UserId"` claim but some code read `ClaimTypes.NameIdentifier` —
  a silent mismatch that returned 0. Watch claim names after an auth migration.

- **Static files live in `wwwroot`.** The port kept CSS in `Content/`; ASP.NET
  Core only serves `wwwroot` by default, so the site rendered unstyled until the
  file moved. The `WebRootPath was not found` startup warning was the tell.

- **Screen-green ≠ data-correct.** Check 7 passed its UI assertion while the order
  wasn't persisted. This is exactly why the workflow pairs Playwright (gate 2) with
  the parity SQL (gate 3).

- **Separate "the browser can't do it" from "the app is broken."** Early failures
  were flaky because the test raced the circuit (fills getting wiped). Verifying a
  value stuck before submitting, and giving interactive pages larger timeouts,
  separated test-harness noise from genuine app defects.

- **Keep the ground truth immutable and roles separate.** Never editing
  `src/ApexCounter` / `GROUND_TRUTH.md`, and letting only the repair agent write
  (only the Blazor project), is what keeps every green result meaningful.

- **Don't re-run AWS Transform to fix logic bugs.** Transform overwrites the
  project and is non-deterministic; it would discard hand-made fixes. Re-run it
  only if the copy came out as the wrong UI (MVC/Razor Pages/still Web Forms).
  Logic/behavior gaps are the repair agent's job.

- **Operational gotchas (Windows):** the running app locks `ApexCounter.exe`, so
  stop it before `dotnet build`; use PowerShell `;` not `&&`; `$env:VAR` not
  `%VAR%`; and don't launch long-running servers inline (use a background process).

---

## 9. Repository Map (for future reference)

```
dotnet4Modernisation/
├─ GROUND_TRUTH.md          # behavior contract (immutable)
├─ FINISH_LINE.md           # the 3 gates
├─ README.md                # assignment + workflow
├─ ground-truth.md          # Agent 1 output (observed legacy behavior)
├─ eval.md                  # Agent 2 output (latest evaluation + parity)
├─ PROJECT_JOURNEY.md       # this file
├─ database/
│  ├─ reset.ps1             # rebuild both DBs to fresh seed
│  ├─ 01_schema.sql / 02_seed.sql / 03_modern_copy.sql
│  ├─ 04_check_seed.sql     # gate 1
│  └─ 05_score_parity.sql   # gate 3
├─ src/
│  ├─ ApexCounter/          # legacy Web Forms — GROUND TRUTH, never edit
│  └─ ApexCounter.Blazor/   # the .NET 8 Blazor port — repair here only
│     ├─ Program.cs         # middleware, cookie auth, /auth/login, /Logout
│     ├─ appsettings.json   # connection string -> ApexCounter_Modern
│     ├─ wwwroot/Content/site.css
│     ├─ Code/*.cs          # ported data layer + Rules (verbatim)
│     └─ Components/
│        ├─ Layout/ (MainLayout.razor, EmptyLayout.razor)
│        └─ Pages/ (Default, Login, Customers, Catalog, Orders, Reports, Account)
└─ e2e/                     # Playwright suite (gate 2 automation)
   ├─ playwright.config.ts
   └─ tests/ (fixtures.ts, ground-truth.spec.ts)
```

### Run order for a clean end-to-end check
```powershell
# 1. both apps running: Web Forms (VS/IIS Express @5080), Blazor (dotnet run @5088)
# 2. fresh seed
powershell -ExecutionPolicy Bypass -File .\database\reset.ps1
# 3. gate 1
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d ApexCounter -i .\database\04_check_seed.sql
# 4. gate 2 (no reset between apps)
cd e2e; npx playwright test
# 5. gate 3
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d master -i .\database\05_score_parity.sql
```

### Current status at time of writing
Gate 1 PASS · Gate 2: Web Forms 8/8, Blazor 7/8 · Gate 3: PARITY FAIL (2 rows,
both from the un-persisted new order). Remaining work: fix the Blazor new-order
save (`Orders/New.razor`), then re-evaluate from a fresh reset.
