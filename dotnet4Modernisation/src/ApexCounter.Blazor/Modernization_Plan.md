# Transformation Plan: ApexCounter

- **Solution Path**: 938859ca-2291-4c1d-bf0d-f1c2db92752b_extracted/sourceCode/ApexCounter.sln
- **Created**: 2025-01-27T12:00:00Z
- **Default Target Framework**: net8.0

## Projects

### 1. ApexCounter

- **Path**: ApexCounter.csproj
- **Current Framework**: v4.8
- **Target Framework**: net8.0

#### Migration Overview

- **Complexity**: medium
- **Summary**: Migration of ApexCounter from v4.8. 1 NuGet reference(s) to review.
- **Estimated Changes**: 1
- **Blocking Issues**:
  - None
- **Notes**: Complexity raised from low to medium (framework floor for v4.8)

#### Work Items

- Update target framework: v4.8 -> net8.0 (convert legacy project to SDK-style)
- Replace package: Microsoft.NETFramework.ReferenceAssemblies.net40 (no automated upgrade path — manual replacement required)

#### NuGet Packages

- None

#### Legacy Files (16)

##### Kind: `.asax`

| # | Source | Target Path |
|---|--------|-------------|
| 1 | `Global.asax` | `Program.cs` |

##### Kind: `.aspx`

| # | Source | Target Path |
|---|--------|-------------|
| 2 | `Account/Password.aspx` | `Components/Pages/Account/Password.razor` |
| 3 | `Catalog/Edit.aspx` | `Components/Pages/Catalog/Edit.razor` |
| 4 | `Catalog/List.aspx` | `Components/Pages/Catalog/List.razor` |
| 5 | `Customers/Edit.aspx` | `Components/Pages/Customers/Edit.razor` |
| 6 | `Customers/List.aspx` | `Components/Pages/Customers/List.razor` |
| 7 | `Default.aspx` | `Components/Pages/Default.razor` |
| 8 | `Login.aspx` | `Components/Pages/Login.razor` |
| 9 | `Orders/Detail.aspx` | `Components/Pages/Orders/Detail.razor` |
| 10 | `Orders/List.aspx` | `Components/Pages/Orders/List.razor` |
| 11 | `Orders/New.aspx` | `Components/Pages/Orders/New.razor` |
| 12 | `Reports/LowStock.aspx` | `Components/Pages/Reports/LowStock.razor` |

##### Kind: `.config`

| # | Source | Target Path |
|---|--------|-------------|
| 13 | `Web.config` | `appsettings.json` |

##### Kind: `WebForms code-only controls (.cs)`

| # | Source | Target Path |
|---|--------|-------------|
| 14 | `Code/AuthData.cs` | `(skill decides)` |
| 15 | `Code/CounterPage.cs` | `(skill decides)` |

##### Kind: `.master`

| # | Source | Target Path |
|---|--------|-------------|
| 16 | `Site.Master` | `Components/Layout/MainLayout.razor` |

#### Migration Notes

ApexCounter is a self-contained ASP.NET Web Forms order-management application with no external project dependencies. The migration is a full rewrite of the Web Forms UI into Blazor components, plus porting the data-access and business-logic layer to .NET 8.

Key migration concerns:

- **FormsAuthentication must be replaced with ASP.NET Core cookie authentication.** The entire app uses `FormsAuthentication` with `<deny users="?"/>` in Web.config. `AuthData.TryLogin` sets a Forms ticket and populates session; `AuthData.SignOut` calls `FormsAuthentication.SignOut()`. Replace with `HttpContext.SignInAsync` / `SignOutAsync` using cookie authentication middleware. The `[Authorize]` attribute or Blazor's `<AuthorizeView>` should enforce access.
- **Session-based user identity (`HttpContext.Current.Session`) must be eliminated.** `AuthData.EnsureSession()`, `CurrentUserId()`, and `CurrentDisplayName()` rely on `HttpContext.Current.Session`. In Blazor Server, session and `HttpContext` are unavailable after the initial render. Replace with claims stored in the authentication cookie and accessed via `AuthenticationStateProvider`.
- **All `HttpContext.Current` static access in `AuthData` and `SiteMaster` must switch to DI-based `IHttpContextAccessor` or Blazor's cascading `AuthenticationState`.**
- **`System.Data.SqlClient` → `Microsoft.Data.SqlClient`** in `Db.cs` and all data classes. This is a namespace/package swap; the ADO.NET API surface is compatible.
- **`ConfigurationManager.ConnectionStrings` → `IConfiguration`** in `Db.cs`. The connection string moves from Web.config to appsettings.json. Since it uses Integrated Security (no embedded password), no secret redaction is needed.
- **`CounterPage : Page` base class** provides `InitializeCulture()` (hardcoded en-US) and `OnLoad` (calls `EnsureSession`, catches `SqlException`). Culture should move to middleware (`app.UseRequestLocalization`). Error handling moves to Blazor error boundaries.
- **`PasswordHasher.cs` uses `SHA1.Create()`** which is obsolete but must be retained for backward compatibility with existing password hashes in the database. Wrap usage to suppress the obsolete warning and document the tech debt.
- **`Site.Master` → `MainLayout.razor`**: The master page has a sign-out button and display-name label that depend on `AuthData` and `HttpContext`. Re-implement as Blazor component logic using `AuthenticationStateProvider`.
- **Web.config connection string** uses `(localdb)\MSSQLLocalDB` with Integrated Security — no embedded secrets. Transfer as-is to appsettings.json `ConnectionStrings:ApexCounter`.

#### Dependencies

- None

#### Migration Risks

- None
