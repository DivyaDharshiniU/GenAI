# Final Project 03 — Apex Counter

**Your job:** modernize the parts desk you are given.

| | You start with | You hand in |
|---|---|---|
| Runtime | **.NET Framework 4.0** | **.NET 8** |
| UI framework | **ASP.NET Web Forms** | **Blazor Server** |
| Pages | `.aspx` + C# code-behind | `.razor` |
| Layout | `Site.Master` | a Blazor layout |
| Config | `Web.config` | `appsettings.json` |
| Startup | `Global.asax` | `Program.cs` |
| Database | SQL Server `ApexCounter` | the **same tables**, in `ApexCounter_Modern` |
| URL | `http://localhost:5080/` | `http://localhost:5088/` |

Do not turn it into MVC, Razor Pages, React, or a plain API. Do not leave it on Web Forms. Web Forms does not run on .NET 8. Do not target .NET 10. The target is **.NET 8** and the UI is **Blazor Server**.

AWS Transform does the first rewrite. You then make the Blazor app behave like the Web Forms app. The behavior contract is [GROUND_TRUTH.md](./GROUND_TRUTH.md).

← [S18 Final Project](../) · [Course README](../../README.md)

---

## What you are given

Apex Counter is the 2011 counter desk for Apex Industrial Supply. Clerks sign in, find customers, sell parts, and move stock.

The code in [`src/`](./src/) is the legacy application. It is already **ASP.NET Web Forms on .NET Framework 4.0**. You do not rewrite that project by hand. You run it, then you port it.

Sign-in for both apps: `clerk` or `supervisor`, password `counter2011`.

---

## What you do, in order

### 1. Run the Web Forms app and learn the ground truth

This step needs **Windows**. .NET Framework and IIS Express do not run on macOS.

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with **ASP.NET and web development**, the **.NET Framework 4.8 targeting pack**, and **SQL Server Express LocalDB**. The project targets `v4.0`. Windows runs it on the 4.8 runtime that replaced 4.0 in place. The project restores `Microsoft.NETFramework.ReferenceAssemblies.net40`, so Visual Studio can compile `v4.0` without the old targeting pack. If the first build says the reference assemblies are missing, build once more so Restore can finish.
2. Also install the **.NET 8 SDK**. You need it for the app you are about to produce.
3. Install the [AWS Toolkit for Visual Studio](https://docs.aws.amazon.com/transform/latest/userguide/dotnet-ide-vs.html) from the Visual Studio Marketplace. You use it in step 2. Sign-in is IAM Identity Center, not an access key. The start URL comes from the instructor. If you were not given one, ask before step 2.
4. From the `database` folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\reset.ps1
```

If PowerShell says `sqlcmd was not found`, install [SQL Server command-line tools](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility) and run the script again. Or open `01_schema.sql`, then `02_seed.sql`, then `03_modern_copy.sql` in SQL Server Management Studio, in that order, against `(localdb)\MSSQLLocalDB`.

That script creates two databases with the same tables and the same seed:

| Database | Used by |
|---|---|
| `ApexCounter` | The Web Forms app. Do not point the new app here. |
| `ApexCounter_Modern` | The Blazor app. Reset before you compare the two. |

5. Open [`src/ApexCounter.sln`](./src/ApexCounter.sln) and press F5. The site should be `http://localhost:5080/`. If Visual Studio picks another port, set the project URL back to 5080.
6. Run gate 1 in [FINISH_LINE.md](./FINISH_LINE.md). `04_check_seed.sql` must print `SEED: PASS`, and the desk must show **2**, **5**, and **48.72**. Then walk the pages in [GROUND_TRUTH.md](./GROUND_TRUTH.md). Open order `AC-0006` and try to mark it **Picked**. It must fail. `WR-10` has 3 on hand and the order needs 4. Nothing is deducted. Customers opens on **Active**, so `OLD-99` is hidden until you choose **All** and search `OLD-99` (it is on the second page of an unfiltered All list).

If Restore still cannot build `v4.0`, change `TargetFrameworkVersion` in `ApexCounter.csproj` and `targetFramework` on `<compilation>` in `Web.config` to `v4.8`. Do not change the pages. The code uses only .NET 4.0 APIs. Keep `v4.0` when the machine allows it. AWS Transform accepts 4.0 and 4.8.

If the seed check fails, run `reset.ps1` again and confirm the server name. Do not edit the Web Forms project. It is the ground truth.

Another SQL Server instance:

```powershell
$env:APEX_SQL_SERVER = ".\SQLEXPRESS"
powershell -ExecutionPolicy Bypass -File .\reset.ps1
```

Then put that server name in the `ApexCounter` connection string in `Web.config`.

### 2. Convert a copy with AWS Transform

AWS Transform does the first rewrite. Do not ask a chatbot to rewrite the `.aspx` files as the first step.

The .NET modernization job is free. Do not start a **Custom transformation**. You are not deploying anything. Both apps keep running on your PC.

This project is already a shape Transform accepts: a C# **Web Application** (it has a `.csproj`), .NET Framework 4.0, no `.ashx` / `.svc` / `.asmx` files, no web references, and no third-party UI controls. Web Site projects are not accepted. This is not one of those.

In Visual Studio, Transform **writes onto the project you select**. Copy first, or you will destroy the ground truth.

1. Close Visual Studio. From this project folder (`03_Apex_Counter`):

```powershell
Copy-Item -Recurse .\src\ApexCounter .\src\ApexCounter.Blazor
```

2. Open **only** `src/ApexCounter.Blazor/ApexCounter.csproj`. Leave `src/ApexCounter.sln` alone. That solution is the Web Forms app.
3. Sign in: **Extensions**, then **AWS Toolkit** or **AWS Transform**, then **Getting Started**. Choose **AWS Transform authentication**, paste the IAM Identity Center start URL, and choose **Connect**. The browser sign-in should end with **Connected with IAM Identity Center**.
4. In Solution Explorer, right-click the Blazor copy and choose **Port with AWS Transform** (some versions say **Port project with AWS Transform**).
5. Create or choose a workspace. In **Choose a .NET target**, select **.NET 8**. Not .NET 10.
6. Set **Transformation Mode** to **Interactive**. Choose **Start**.
7. When the plan appears, read it before you approve. It must port **ASP.NET Web Forms to Blazor Server**. If the plan says MVC, Razor Pages, WebAssembly, or "leave Web Forms", tell the chat: "Port the UI to Blazor Server. Keep the job setting Transform UI: ASP.NET Web Forms to Blazor turned on." Approve only after the plan says that. The official mapping is:

| Web Forms (.NET Framework 4.0) | Blazor Server (.NET 8) |
|---|---|
| `*.aspx`, `*.aspx.cs` | `*.razor` under `Components/Pages` |
| `Site.Master` | a `layout.razor` |
| `Web.config` | `appsettings.json` |
| `Global.asax` | `Program.cs` |
| IIS Express on port 5080 | Kestrel on port **5088** |

8. Leave Visual Studio open. The Job Plan window shows the remaining time. If a diff list appears, apply it to the **copy** only.
9. Download `NextSteps.md` from the Job Plan window (download icon, or **Download next steps** on the transformation report). Keep that file. The repair agent uses it.
10. Confirm the copy before you go on:
    - `src/ApexCounter` still contains `.aspx` files.
    - `src/ApexCounter.Blazor` contains `.razor` files, and the target framework is `net8.0`. If Transform wrote `net10.0`, change it to `net8.0`.
    - If the copy is still Web Forms, MVC, or Razor Pages, delete that copy, copy `src/ApexCounter` again, and rerun the job. Do not repair the wrong UI.

11. Point the Blazor app at the other database. In `appsettings.json`, the catalog must be `ApexCounter_Modern` on the same server. Same tables. Different database. A typical string:

```text
Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ApexCounter_Modern;Integrated Security=True
```

12. Set the Blazor URL to `http://localhost:5088/`. That is `applicationUrl` in `Properties/launchSettings.json`. Port 5080 stays with the Web Forms app so both can run together.
13. Run `reset.ps1` again so both databases are a fresh seed. Start the Web Forms app from `src/ApexCounter.sln` (F5, port 5080). Start the Blazor app from the copy (`dotnet run` or F5 in a second Visual Studio window, port 5088).

Docs: [Modernizing .NET in Visual Studio](https://docs.aws.amazon.com/transform/latest/userguide/dotnet-ide-vs.html) and [Web Forms to Blazor](https://docs.aws.amazon.com/transform/latest/userguide/dotnet-porting-ui.html).

### 3. Ground truth, then evaluate, then repair

The Web Forms site on port 5080, reading `ApexCounter`, is the answer. The Blazor site on port 5088, reading `ApexCounter_Modern`, is what you are fixing.

A Blazor home page is not done. Transform often leaves a project that does not build, or that builds and still gets stock, money, or sentences wrong. You close that gap with three agents, in this order, and you repeat the last two until [FINISH_LINE.md](./FINISH_LINE.md) passes.

Do not let any agent edit `src/ApexCounter`, `GROUND_TRUTH.md`, or the table names. The repair agent may edit only `src/ApexCounter.Blazor`.

**Agent 1 — ground truth.** Run this once, after gate 1 passes. It only reads.

```text
You are the ground-truth agent for Apex Counter. Read GROUND_TRUTH.md, FINISH_LINE.md, and src/ApexCounter/Code/Rules.cs. The running app is http://localhost:5080/ signed in as clerk / counter2011. Database ApexCounter on (localdb)\MSSQLLocalDB.

Do not edit any file. For each page in GROUND_TRUTH.md and for each of the eight checks in FINISH_LINE.md, write the exact sentence on screen and what happens to order status, money, and stock. Customers: Active hides OLD-99. All plus a search for OLD-99 shows Retired Yard Co. The low-stock report count is 5 and has no DISC-1. Catalog "Low stock only" includes DISC-1 on the first page.

Save the notes as ground-truth.md next to this README. Stop.
```

**Agent 2 — evaluate.** It only reads. Run it after Agent 1, and again after every repair.

```text
You are the evaluate agent. Read ground-truth.md and FINISH_LINE.md. Do not edit application code.

Web Forms http://localhost:5080/ uses database ApexCounter.
Blazor http://localhost:5088/ uses database ApexCounter_Modern.
Sign in as clerk / counter2011 on both.

Do checks 1 to 4 on both apps. Then do checks 5 to 8 on the Web Forms app, and the same clicks on the Blazor app. Do not run reset.ps1 between the two apps. Checks 5 to 8 change rows. A reset in the middle makes the parity score meaningless.

Write eval.md. For each check: pass or fail, the sentence you saw, and the stock or status if it differs. Then run database/05_score_parity.sql. Quote SEED or PARITY lines and any diff rows. Stop. Do not fix the code.
```

**Agent 3 — repair.** It may edit only the Blazor project.

```text
You are the repair agent. Read eval.md and NextSteps.md. Edit only src/ApexCounter.Blazor. Do not edit src/ApexCounter, the SQL scripts, or GROUND_TRUTH.md.

Make the Blazor app show the same sentences and write the same rows as the Web Forms app. Stock moves on pick, not on open and not on invoice. A short pick is refused and the order stays Open. Tax is 5 percent, half away from zero, two decimals. Keep the connection string on ApexCounter_Modern and the URL on http://localhost:5088/. Build the Blazor project. Stop. Do not mark the job done. The evaluate agent decides.
```

Then run Agent 2 again. If `eval.md` still has a fail, run Agent 3 again. You are done only when all three are true:

1. `database/04_check_seed.sql` prints `SEED: PASS`, and the Web Forms desk shows 2, 5, and 48.72.
2. The eight screen checks pass on **both** apps.
3. `database/05_score_parity.sql` prints `PARITY: PASS`.

If parity fails, look at `LegacyValue` versus `ModernValue`, repair the Blazor app, and start again from a fresh `reset.ps1`. Do the eight checks on both apps again before you rerun the parity script.

---

## Rules the Blazor app has to keep

The sentences are in `src/ApexCounter/Code/Rules.cs` on the legacy side. The new app does not have to use that file. It does have to show the same sentences and do the same work.

- Stock does not move when an order is opened. It moves when the order is **picked**.
- If any line is short, the pick is refused, nothing is deducted, and the order stays **Open**.
- **Invoiced** does not change stock. Cancelling a **picked** order puts the stock back. Cancelling an **open** order does not.
- **Invoiced** and **Cancelled** do not move again.
- On-hand cannot go below zero.
- Tax is 5 percent of the subtotal, rounded half away from zero to two decimals. The unit price is copied onto the line.
- A catalog price change does not rewrite old lines. Adding the same SKU again keeps the first price and adds the quantity.
- The low-stock report lists **active** products only. The catalog "low stock only" filter also shows inactive products that are low.
- Changing `clerk`'s password leaves `supervisor` on `counter2011`, and the other way around.

---

## What "done" means

Follow [FINISH_LINE.md](./FINISH_LINE.md). You are done when all three are true:

1. `database/04_check_seed.sql` prints `SEED: PASS` and the Web Forms desk shows 2, 5, and 48.72.
2. The eight screen checks pass on **both** apps.
3. `database/05_score_parity.sql` prints `PARITY: PASS`.

Submit the Blazor project. Leave the Web Forms project in the repo as the original.
