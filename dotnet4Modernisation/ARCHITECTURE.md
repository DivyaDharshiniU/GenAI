# ApexCounter — Architecture: Before vs After Transformation

Migration from **ASP.NET Web Forms (.NET Framework 4.0)** to **Blazor Server (.NET 8.0)**.

---

## High-Level Before / After

```mermaid
flowchart LR
    subgraph BEFORE["BEFORE — ASP.NET Web Forms (.NET Framework 4.0)"]
        direction TB
        B_Browser["Browser<br/>(full page postbacks)"]
        B_IIS["IIS + System.Web pipeline"]
        B_Global["Global.asax<br/>(app entry / events)"]
        B_Master["Site.Master<br/>(master page)"]
        B_Pages[".aspx + .aspx.cs + .aspx.designer.cs<br/>11 Web Forms pages"]
        B_Base["CounterPage : System.Web.UI.Page"]
        B_Auth["FormsAuthentication<br/>+ Session state"]
        B_Data["Static data classes<br/>Db / AuthData / CatalogData /<br/>CustomerData / OrderData"]
        B_Cfg["Web.config<br/>(connection strings, auth, culture)"]
        B_Sql["System.Data.SqlClient"]
        B_DB[("SQL Server")]

        B_Browser --> B_IIS --> B_Global --> B_Master --> B_Pages
        B_Pages --> B_Base
        B_Pages --> B_Auth
        B_Pages --> B_Data
        B_Data --> B_Sql --> B_DB
        B_Cfg -.config.-> B_Global
        B_Cfg -.ConfigurationManager.-> B_Data
    end

    subgraph AFTER["AFTER — Blazor Server (.NET 8.0)"]
        direction TB
        A_Browser["Browser<br/>(SignalR circuit, interactive)"]
        A_Kestrel["Kestrel + ASP.NET Core<br/>middleware pipeline"]
        A_Program["Program.cs<br/>(minimal hosting / DI setup)"]
        A_App["App.razor + Routes.razor<br/>(AuthorizeRouteView)"]
        A_Layout["MainLayout.razor<br/>(layout + NavLink nav)"]
        A_Pages[".razor components<br/>Components/Pages/*<br/>(Account, Catalog, Customers,<br/>Orders, Reports)"]
        A_Base["CounterPageBase : ComponentBase"]
        A_Auth["Cookie Authentication<br/>claims-based (ClaimsPrincipal)<br/>/Logout minimal API"]
        A_Data["Scoped DI services<br/>Db / AuthData / CatalogData /<br/>CustomerData / OrderData"]
        A_Cfg["appsettings.json<br/>+ IConfiguration"]
        A_Sql["Microsoft.Data.SqlClient 7.1.1"]
        A_DB[("SQL Server")]

        A_Browser --> A_Kestrel --> A_Program --> A_App --> A_Layout --> A_Pages
        A_Pages --> A_Base
        A_Pages --> A_Auth
        A_Pages --> A_Data
        A_Data --> A_Sql --> A_DB
        A_Cfg -.IConfiguration.-> A_Program
        A_Cfg -.GetConnectionString.-> A_Data
    end

    BEFORE ==>|AWS Transform| AFTER
```

---

## Component-by-Component Mapping

```mermaid
flowchart LR
    subgraph OLD["Web Forms (.NET Framework 4.0)"]
        direction TB
        O1["Global.asax / Global.asax.cs"]
        O2["Site.Master (.master)"]
        O3[".aspx pages (11)"]
        O4[".aspx.cs code-behind"]
        O5[".aspx.designer.cs"]
        O6["CounterPage : Page"]
        O7["FormsAuthentication + Session"]
        O8["Static classes (Db, *Data)"]
        O9["ConfigurationManager"]
        O10["System.Data.SqlClient"]
        O11["Web.config"]
        O12["Legacy .csproj (net40)"]
    end

    subgraph NEW["Blazor Server (.NET 8.0)"]
        direction TB
        N1["Program.cs"]
        N2["MainLayout.razor + App.razor"]
        N3[".razor components"]
        N4["@code blocks in .razor"]
        N5["(removed — no designer files)"]
        N6["CounterPageBase : ComponentBase"]
        N7["Cookie auth (claims) + /Logout API"]
        N8["Scoped DI services"]
        N9["IConfiguration (constructor injection)"]
        N10["Microsoft.Data.SqlClient 7.1.1"]
        N11["appsettings.json"]
        N12["SDK-style .csproj (net8.0, Sdk.Web)"]
    end

    O1 --> N1
    O2 --> N2
    O3 --> N3
    O4 --> N4
    O5 --> N5
    O6 --> N6
    O7 --> N7
    O8 --> N8
    O9 --> N9
    O10 --> N10
    O11 --> N11
    O12 --> N12
```

---

## Request Lifecycle: Before vs After

```mermaid
sequenceDiagram
    participant U as Browser
    participant S as Server

    Note over U,S: BEFORE — Web Forms (stateless postbacks)
    U->>S: HTTP GET/POST (full page)
    S->>S: Page lifecycle (Init→Load→Render)
    S->>S: FormsAuth cookie + Session lookup
    S->>S: Static Db.Method() → SQL Server
    S-->>U: Full HTML page (ViewState)

    Note over U,S: AFTER — Blazor Server (persistent circuit)
    U->>S: Initial HTTP GET
    S-->>U: Rendered HTML + blazor.server.js
    U->>S: Open SignalR circuit (WebSocket)
    U->>S: UI event over circuit
    S->>S: Cookie auth (ClaimsPrincipal)
    S->>S: Scoped service (_db) → SQL Server
    S-->>U: DOM diff over SignalR
```

---

## Summary of Key Shifts

| Concern | Before | After |
|---|---|---|
| UI framework | ASP.NET Web Forms (.aspx) | Blazor Server (.razor) |
| Rendering | Full-page postbacks + ViewState | SignalR circuit, DOM diffing |
| Entry point | Global.asax | Program.cs (minimal hosting) |
| Layout | Site.Master | MainLayout.razor / App.razor |
| Base page | CounterPage : System.Web.UI.Page | CounterPageBase : ComponentBase |
| Auth | FormsAuthentication + Session | Cookie auth, claims-based + /Logout API |
| Data access | Static classes | Scoped DI services |
| Config | Web.config + ConfigurationManager | appsettings.json + IConfiguration |
| SQL client | System.Data.SqlClient | Microsoft.Data.SqlClient 7.1.1 |
| Target framework | .NET Framework 4.0 | net8.0 |
| Project format | Legacy .csproj | SDK-style (Microsoft.NET.Sdk.Web) |
| Host | IIS + System.Web | Kestrel + ASP.NET Core |

**File stats:** 65 files changed — 14 added, 46 deleted, 5 modified. +1 NuGet (Microsoft.Data.SqlClient 7.1.1), -1 NuGet (Microsoft.NETFramework.ReferenceAssemblies.net40).
