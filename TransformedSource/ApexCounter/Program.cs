using System.Globalization;
using ApexCounter;
using ApexCounter.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

// Blazor Server component hosting.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpContext access for the Logout endpoint and anywhere a component
// needs it (e.g. cookie sign-in from Login page).
builder.Services.AddHttpContextAccessor();

// Cookie authentication — replaces FormsAuthentication.
// LoginPath / LogoutPath mirror the legacy <forms> element:
//   loginUrl="~/Login.aspx"  timeout="60"  name=".APEXCOUNTER"
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = ".APEXCOUNTER";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Session — replaces InProc session with 60-minute timeout.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database helper — was static, now scoped so each request gets its own
// instance backed by IConfiguration for the connection string.
builder.Services.AddScoped<Db>();
builder.Services.AddScoped<AuthData>();
builder.Services.AddScoped<CatalogData>();
builder.Services.AddScoped<CustomerData>();
builder.Services.AddScoped<DeskData>();
builder.Services.AddScoped<OrderData>();

var app = builder.Build();

// --- Middleware pipeline (order is load-bearing) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Culture middleware — replaces CounterPage.InitializeCulture().
app.Use(async (context, next) =>
{
    var culture = new CultureInfo("en-US");
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseAntiforgery();

// Logout endpoint — FormsAuthentication.SignOut() → SignOutAsync.
// Blazor Server components cannot call SignOutAsync (no live HTTP
// request during the SignalR circuit), so a minimal endpoint handles it.
app.MapGet("/Logout", async (HttpContext http) =>
{
    http.Session.Clear();
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Login");
});

// Modern Blazor Server entry point (.NET 8+).
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
