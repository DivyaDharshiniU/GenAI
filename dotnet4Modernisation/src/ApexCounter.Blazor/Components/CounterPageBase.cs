using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;

namespace ApexCounter.Components
{
    /// <summary>
    /// Base component for all authenticated pages in the ApexCounter application.
    /// Replaces the legacy <c>CounterPage : System.Web.UI.Page</c> which provided:
    /// <list type="bullet">
    ///   <item>Culture initialization (now handled by <c>app.UseRequestLocalization()</c> in Program.cs)</item>
    ///   <item>Session-based authentication enforcement via <c>AuthData.EnsureSession()</c>
    ///         (now handled by claims in the authentication cookie + <c>[Authorize]</c>)</item>
    ///   <item>SQL error handling that delegated to <c>SiteMaster.ShowDatabaseDown()</c></item>
    ///   <item>A <c>QueryId()</c> helper for reading the "id" query-string parameter</item>
    /// </list>
    /// Derived Blazor pages should use <c>@inherits CounterPageBase</c> and add
    /// <c>@attribute [Authorize]</c> to enforce authentication.
    /// </summary>
    public abstract class CounterPageBase : ComponentBase
    {
        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        /// <summary>
        /// When <c>true</c>, the database is unreachable. Derived pages can show a
        /// user-friendly message instead of their normal content.
        /// Replaces the legacy <c>SiteMaster.ShowDatabaseDown()</c> cross-component call.
        /// </summary>
        protected bool IsDatabaseDown { get; private set; }

        /// <summary>
        /// Message to display when the database is down.
        /// </summary>
        protected string DatabaseDownMessage { get; private set; } = string.Empty;

        /// <summary>
        /// The authenticated user's UserId claim (mirrors the old Session["UserId"]).
        /// Populated from the authentication cookie claims.
        /// </summary>
        protected int CurrentUserId { get; private set; }

        /// <summary>
        /// The authenticated user's DisplayName claim (mirrors the old Session["DisplayName"]).
        /// Populated from the authentication cookie claims.
        /// </summary>
        protected string CurrentDisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// Replaces the legacy <c>OnLoad</c> which called <c>AuthData.EnsureSession()</c>
        /// and caught <see cref="SqlException"/> to show the database-down panel.
        /// Culture initialization is no longer needed here — it is handled globally
        /// by <c>app.UseRequestLocalization()</c> in Program.cs.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity is { IsAuthenticated: true })
                {
                    var userIdClaim = user.FindFirst("UserId");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                    {
                        CurrentUserId = uid;
                    }

                    var displayNameClaim = user.FindFirst("DisplayName");
                    if (displayNameClaim != null)
                    {
                        CurrentDisplayName = displayNameClaim.Value;
                    }
                }
            }
            catch (SqlException)
            {
                IsDatabaseDown = true;
                DatabaseDownMessage = Messages.DbDown;
            }

            await base.OnInitializedAsync();
        }

        /// <summary>
        /// Parses the "id" query-string parameter from the current URL.
        /// Returns 0 if the parameter is missing, non-numeric, or negative.
        /// Replaces <c>Request.QueryString["id"]</c> from the legacy base page.
        /// </summary>
        protected int QueryId()
        {
            var uri = new Uri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var values))
            {
                if (int.TryParse(values.FirstOrDefault(), out int id) && id >= 0)
                {
                    return id;
                }
            }

            return 0;
        }
    }
}
