using System.Globalization;
using ApexCounter;
using ApexCounter.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

// Blazor Server components (modern .NET 8 pattern).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cookie authentication — replaces Web.config <authentication mode="Forms">.
// LoginPath "/Login" matches the ported Login.razor page route.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".APEXCOUNTER";
    });

builder.Services.AddAuthorization();

// Cascading auth state so <AuthorizeRouteView> and <AuthorizeView> work in
// Blazor components without a manual <CascadingAuthenticationState> wrapper.
builder.Services.AddCascadingAuthenticationState();

// IHttpContextAccessor — needed by ported components / services that must
// read the current principal or perform sign-in/sign-out via HttpContext.
builder.Services.AddHttpContextAccessor();

// AuthData — scoped authentication/identity service (ported from static legacy helper).
builder.Services.AddScoped<AuthData>();

// Request localization — replaces CounterPage.InitializeCulture() which
// hardcoded en-US on every page load.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var enUs = new CultureInfo("en-US");
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(enUs);
    options.SupportedCultures = new[] { enUs };
    options.SupportedUICultures = new[] { enUs };
});

// ---------------------------------------------------------------------------
// Db layer — supply IConfiguration so Db.Open() can read the connection
// string from appsettings.json (ConnectionStrings:ApexCounter).
// Db.Configure() was added during the Db.cs migration and must be called
// once at startup; without it every database call throws.
// ---------------------------------------------------------------------------
Db.Configure(builder.Configuration);

// ---------------------------------------------------------------------------
// Build
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline — order is load-bearing (see ASP.NET Core docs).
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// No HttpsRedirection: the contract requires the app to serve plain HTTP on
// http://localhost:5088/ (port 5080 stays with the Web Forms app).
app.UseStaticFiles();

// Localization must run before routing so culture is set for all requests.
app.UseRequestLocalization();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ---------------------------------------------------------------------------
// Logout endpoint — must be a real HTTP endpoint, not a Blazor component,
// because SignOutAsync requires a live HTTP request (not available inside a
// Blazor Server SignalR circuit).
// ---------------------------------------------------------------------------
app.MapGet("/Logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Login");
});

// ---------------------------------------------------------------------------
// Login endpoint — sign-in must happen on a real HTTP request so the auth
// cookie is written to the response. Doing SignInAsync inside a Blazor render
// (even static SSR) does not reliably flush the Set-Cookie header, so the form
// on Login.razor posts here instead.
// ---------------------------------------------------------------------------
app.MapPost("/auth/login", async (HttpContext http, AuthData authData) =>
{
    var form = await http.Request.ReadFormAsync();
    string userName = form["userName"].ToString();
    string password = form["password"].ToString();
    string returnUrl = form["returnUrl"].ToString();

    // Empty-field checks mirror the legacy RequiredFieldValidators.
    bool userBlank = string.IsNullOrWhiteSpace(userName);
    bool passBlank = string.IsNullOrWhiteSpace(password);
    if (userBlank || passBlank)
    {
        string flag = userBlank && passBlank ? "both" : (userBlank ? "user" : "pass");
        return Results.Redirect($"/Login?error=required&f={flag}");
    }

    string? error;
    bool ok;
    try
    {
        // TryLogin validates credentials and calls HttpContext.SignInAsync with
        // Name, UserId and DisplayName claims. Here HttpContext is a live request,
        // so the cookie is written to the response.
        ok = authData.TryLogin(userName, password, out error);
    }
    catch (Microsoft.Data.SqlClient.SqlException)
    {
        return Results.Redirect("/Login?error=db");
    }

    if (!ok)
    {
        return Results.Redirect("/Login?error=bad");
    }

    string redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    return Results.Redirect(redirect);
});

// ---------------------------------------------------------------------------
// Blazor Server entry — modern .NET 8 pattern using MapRazorComponents.
// NOT MapBlazorHub + MapFallbackToPage("/_Host").
// ---------------------------------------------------------------------------
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
