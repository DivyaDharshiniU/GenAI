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

app.UseHttpsRedirection();
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
// Blazor Server entry — modern .NET 8 pattern using MapRazorComponents.
// NOT MapBlazorHub + MapFallbackToPage("/_Host").
// ---------------------------------------------------------------------------
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
