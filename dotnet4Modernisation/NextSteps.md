# Next Steps

## Transformation Context
- **Solution**: ApexCounter
- **Source Framework**: .NET Framework 4.8 (ASP.NET Web Forms)
- **Target Framework**: net8.0 (Blazor Server)
- **Projects Transformed**: 1 — ApexCounter
- **Transformation Date**: 2026-10-31

## Priority Summary
The transformation completed cleanly with zero remaining build errors. All 16 legacy files were successfully transformed and 134 build errors were resolved. Outstanding work is entirely post-transformation hardening — no blockers exist to running the application. Address items in this order:

1. **(High — Security)** Replace the obsolete SHA1 password hasher with PBKDF2. This is a production security vulnerability and should be resolved before any public deployment.
2. **(High)** End-to-end validation of the authentication flow, since the entire auth stack was replaced (FormsAuthentication → ASP.NET Core cookie auth).
3. **(Medium)** Verify the SQL Server connection string in `appsettings.json` matches the actual target environment.
4. **(Low)** Smoke-test all transformed Razor pages for behavioral parity with the original Web Forms pages.

## Current Build Status
The solution built successfully at the end of transformation with **0 errors and 0 warnings**. All legacy `System.Web` references have been eliminated.

## Remaining Build Errors
None. The solution is in a clean build state.

## Incomplete Transformations
None. All 16 source files were fully transformed.

## Package Issues
No packages failed to migrate automatically. The following packages should be spot-checked at runtime for compatibility with net8.0, as version combinations may surface behavioral differences:

| Package | Version | Action |
|---|---|---|
| `System.Configuration.ConfigurationManager` | 10.0.14 | Verify no runtime breaking changes affect any remaining config reads |
| `Microsoft.Data.SqlClient` | 7.1.1 | Confirm all CRUD operations succeed; review release notes for net8.0 behavioral changes |
| `Microsoft.AspNetCore.SystemWebAdapters` | 2.3.0 | Confirm no runtime compatibility issues with net8.0; verify no residual `System.Web` shim dependencies remain in the codebase |

## Code Changes Required

### 1. Replace SHA1 Password Hashing with PBKDF2
**File**: `Code/PasswordHasher.cs` (or wherever `SHA1.Create()` is called)

SHA1 is cryptographically broken. Replace with PBKDF2 using ASP.NET Core's built-in `PasswordHasher<T>`.

```csharp
// Remove:
using var sha1 = SHA1.Create();
var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));

// Replace with (ASP.NET Core Identity-compatible PBKDF2):
var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
var hashed = hasher.HashPassword(null, password);

// Verify:
var result = hasher.VerifyHashedPassword(null, storedHash, inputPassword);
// result == PasswordVerificationResult.Success  →  verified
// result == PasswordVerificationResult.Failed   →  invalid
```

**Critical — existing database hashes are SHA1.** A one-time transparent rehash strategy must be implemented in the login flow (`Components/Pages/Login.razor` or the auth service it calls). Do not perform a bulk password reset without user notification.

Implement the following fallback logic in the login handler:

1. Attempt verification using the new PBKDF2 hasher.
2. If that fails, fall back to SHA1 verification.
3. If SHA1 succeeds, immediately rehash the password with PBKDF2, persist the new hash to the database, then complete the login.
4. Once all active users have logged in at least once and no SHA1 hashes remain in the database, remove the SHA1 fallback path entirely.

---

### 2. Review and Harden the Authentication Flow
**Files**:
- `Program.cs`
- `Code/AuthData.cs`
- `Components/Pages/Login.razor`
- `Components/Pages/Account/Password.razor`

Confirm each of the following is correctly implemented:

- Cookie authentication middleware is registered **before** `app.UseAuthorization()` in `Program.cs`.
- `[Authorize]` attributes or `<AuthorizeView>` are applied to all pages that previously relied on the `CounterPage` base class for authorization checks.
- Sign-out calls `HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` to clear the cookie.
- Claims populated at login (user ID, role, etc.) match what downstream Razor components read via `AuthenticationStateProvider`.
- Cookie settings in `Program.cs` (expiration, `SlidingExpiration`, `SecurePolicy`, `HttpOnly`) replicate the security posture of the original `FormsAuthentication` settings from `Web.config` (timeout, `requireSSL`).

---

### 3. Verify Connection String in `appsettings.json`
**File**: `appsettings.json`

The connection string was generated targeting LocalDB. Confirm it matches the actual target environment:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ApexCounter;Trusted_Connection=True;"
}
```

Update `Server`, credentials, and database name as required for dev/staging/production. If the original `Web.config` connection string targeted a named SQL Server instance, replicate that configuration here. Do **not** store production credentials directly in this file — use `appsettings.Production.json` or environment variable overrides (see Deployment Considerations).

---

### 4. Verify Static Asset Path
**File**: `Components/Layout/MainLayout.razor`

Confirm the stylesheet link is correct and relative to `wwwroot/`:

```html
<link rel="stylesheet" href="Content/site.css" />
```

Load the application and verify styles render correctly. There must be no leading path issues that cause a 404 for `wwwroot/Content/site.css`.

## Validation & Testing

1. **Build verification**:
   ```bash
   dotnet build ApexCounter.csproj
   ```
   Expected: 0 errors, 0 warnings.

2. **Run the application**:
   ```bash
   dotnet run --project ApexCounter.csproj
   ```

3. **Authentication flow — test each scenario manually**:

   | Scenario | Expected result |
   |---|---|
   | Navigate to a protected page while unauthenticated | Redirect to `Login.razor` |
   | Log in with valid credentials | Redirect to `Default.razor`; session persists across navigations |
   | Log in with invalid credentials | Error message displayed; no redirect |
   | Change password via `Account/Password.razor` | New password works on next login |
   | Sign out | Cookie cleared; protected pages redirect to login |
   | Refresh browser after login | Session persists (cookie-based, not in-memory) |
   | Log in with a legacy SHA1-hashed account (if rehash strategy is deployed) | Login succeeds; hash in DB is silently upgraded to PBKDF2 |

4. **Page-by-page smoke test** (compare behavior against original Web Forms):

   | Original `.aspx` | Transformed `.razor` | Key behaviors to verify |
   |---|---|---|
   | `Default.aspx` | `Components/Pages/Default.razor` | Dashboard loads, data displays correctly |
   | `Catalog/List.aspx` | `Components/Pages/Catalog/List.razor` | Catalog items load; pagination/filtering works |
   | `Catalog/Edit.aspx` | `Components/Pages/Catalog/Edit.razor` | Create and edit save correctly to SQL |
   | `Customers/List.aspx` | `Components/Pages/Customers/List.razor` | Customer list loads |
   | `Customers/Edit.aspx` | `Components/Pages/Customers/Edit.razor` | Customer create/edit saves |
   | `Orders/List.aspx` | `Components/Pages/Orders/List.razor` | Order list loads |
   | `Orders/New.aspx` | `Components/Pages/Orders/New.razor` | New order creation works end-to-end |
   | `Orders/Detail.aspx` | `Components/Pages/Orders/Detail.razor` | Order detail displays correctly |
   | `Reports/LowStock.aspx` | `Components/Pages/Reports/LowStock.razor` | Low stock report renders |

5. **Database connectivity**: Verify all CRUD operations succeed against the target SQL Server using `Microsoft.Data.SqlClient` 7.1.1.

6. **Static assets**: Confirm `wwwroot/Content/site.css` loads without a 404 in the browser developer tools network tab.

## Deployment Considerations

- **Hosting model change**: The application moved from IIS-hosted ASP.NET Web Forms to Kestrel-based Blazor Server. If deploying behind IIS, an `AspNetCoreModule` reverse proxy (in-process or out-of-process) must be configured. Run `dotnet publish -c Release` and confirm a `web.config` is present in the publish output — the .NET SDK generates this automatically for IIS hosting.

- **Static files in publish output**: Confirm `wwwroot/Content/site.css` is included in publish output. Verify no `.csproj` glob exclusion inadvertently omits `wwwroot/**` content.

- **Connection string management**: Never store production credentials in `appsettings.json` committed to source control. Use environment-specific overrides:
  - `appsettings.Production.json` (excluded from source control via `.gitignore`), or
  - Environment variable override: `ConnectionStrings__DefaultConnection=<value>`

- **SHA1 legacy auth during transition**: If deploying before the password hasher replacement is complete, ensure the SHA1 fallback path remains functional so existing users are not locked out. Schedule the full SHA1 removal as an immediate follow-on release once the transparent rehash strategy has been deployed and sufficient time has passed for active users to log in.

- **Cookie security settings**: Before deploying to production, review the following cookie auth settings in `Program.cs` and confirm they match or exceed the original `Web.config` `FormsAuthentication` posture:
  - `ExpireTimeSpan` (replaces `timeout`)
  - `SlidingExpiration`
  - `Cookie.SecurePolicy` — set to `Always` in production (replaces `requireSSL="true"`)
  - `Cookie.HttpOnly` — should be `true`
  - `Cookie.SameSite` — set to `Strict` or `Lax` as appropriate

## Final Step: Containerize & Deploy with AWS Transform

> **Gate: do not start until every item below passes.**
> - [ ] Every check under Validation & Testing above passes
> - [ ] Every item under Deployment Considerations above is handled (databases, queues and other dependencies exist in AWS)
> - [ ] No credentials remain in files you will upload (`appsettings*.json`, `web.config`)
>
> If anything above is incomplete or failing, stop and finish it first.

**What deploys:** web, worker and console projects. Class libraries and test projects are not deployed. WinForms and WPF apps can't run in Linux containers, so skip this step for them. A console app that runs once and exits deploys as an ECS task or a Kubernetes Job, not a long-running service.

Give the prompt below to your coding assistant (Kiro, Claude Code, or another MCP client). It uses the AWS Transform MCP server and the **Source Code Containerization** agent (`vmware-containerization-agent`). That agent builds the image, pushes it to Amazon ECR, generates Terraform or Helm, and deploys to **Amazon ECS or EKS**. For EC2, Elastic Beanstalk or Lambda, stop after Publish Images and deploy the ECR image yourself.

### Setup (once)
- Claude Code: `claude mcp add aws-transform -e AWS_PROFILE=<profile> -- uvx awslabs.aws-transform-mcp-server@latest`
- Kiro: add to `~/.kiro/settings/mcp.json`:
  `{"mcpServers":{"aws-transform":{"command":"uvx","args":["awslabs.aws-transform-mcp-server@latest"],"env":{"AWS_PROFILE":"<profile>"}}}}`
- `AWS_PROFILE` is optional. Without it, the assistant signs in with IAM Identity Center, which opens a browser.

### Prompt
```text
Use the AWS Transform MCP server to containerize and deploy the solution (the target framework).
It is already transformed and validated. Do not start any modernization or assessment job.

Apps to deploy: the web, worker and console projects in the solution (ask me if unsure)
Source: the solution folder at <path> (ask me if not given)
Deploy target: ECS or EKS (ask me)
Region: <region> (ask me if empty)

1. Connect: call get_status. If it is already connected (SSO, cookie, or AWS credentials
   from AWS_PROFILE), skip sign-in. Otherwise ask me for the IAM Identity Center start URL
   and region and call configure(authMode="sso"). Then call switch_profile(region=<Region>)
   before any other tool, so calls don't fail with PROFILE_SELECTION_REQUIRED.
2. Workspace: use the workspace I name. Otherwise list_resources(resource="workspaces")
   and ask me which one to use. Only create a workspace if I say so.
3. Agent: list_resources(resource="agents", agentType="ORCHESTRATOR_AGENT") and confirm
   vmware-containerization-agent has accountAccess "ENABLED". Stop if it doesn't.
4. Job: create_job(workspaceId, jobName="<solution>-containerize",
   objective="Containerize and deploy the solution to <target>", intent="CONTAINERIZATION",
   orchestratorAgent="vmware-containerization-agent"). Then call
   load_instructions(workspaceId, jobId). It may return none; that's fine.
5. The agent asks which workflow to run. Reply with send_message:
   "Source code containerization". When it shows the plan and asks whether to start,
   reply in a full sentence, e.g. "Yes, the plan looks good. Please start execution."
6. Approvals: check list_resources(resource="tasks"), then get_resource(resource="task") for
   each open task (the list can still say CREATED when the task is ready). For a task
   AWAITING_HUMAN_INPUT (Disclaimer, Review Docker Artifacts, Publish Images, IaC, Deploy),
   show me its content and options. Only call complete_task after I approve.
7. Source upload. The Get Source Code step raises no task; it expects a zip.
   - Zip the solution folder, excluding bin/, obj/, .git/, .vs/, node_modules/ and any
     file holding credentials.
   - Call upload_artifact(workspaceId, jobId, content=<zip path>, fileType="ZIP",
     fileName=<name>.zip).
   - send_message: "I uploaded the source as a ZIP artifact <artifactId>. Use it for Get
     Source Code and continue with default settings."
   - If the agent re-asks, restate the choice in a full sentence. Single words like
     "Proceed" don't always advance it.
8. Progress: poll with adaptive_poll plus get_resource(resource="plan"). The agent often
   waits on a chat question without raising a task, so:
   - If a step stays IN_PROGRESS with no new worklogs for two polls, call get_job_status
     with a question ("Is this step waiting on anything from me?") and answer what it asks.
     Show me any choice it offers (base image, which apps, Dockerfile reuse) unless I've
     already given the answer.
   - If a step is SUCCEEDED and the next is NOT_STARTED after two polls, send_message
     asking it to start the next step.
9. Outputs: call list_resources(resource="artifacts", jobId) and take the full
   ".../Generated Outputs/" path from folders[]; pass that exact path as pathPrefix (the
   bare "Generated Outputs/" returns nothing). Download the changes zip and build log with
   get_resource(resource="artifact", savePath=...). Show me what each approval covers
   before I give it:
   - before Review Docker Artifacts: the Dockerfile, .dockerignore and build log
   - before Generate IaC / Deploy: the ECR image URI from Publish Images
   - before Deploy: the generated Terraform or Helm
10. Finish: report the ECR image URI, the IaC location, and the deployed service endpoint
    or status. Surface every tool error verbatim. Never delete workspaces or jobs.
```
