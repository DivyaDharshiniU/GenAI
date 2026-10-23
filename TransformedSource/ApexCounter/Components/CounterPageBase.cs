using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.SqlClient;

namespace ApexCounter.Components
{
    /// <summary>
    /// Base component that replaces the legacy CounterPage : System.Web.UI.Page.
    ///
    /// Original responsibilities and how they are handled:
    ///   1. InitializeCulture() set en-US → now handled by culture middleware in Program.cs.
    ///   2. OnLoad called AuthData.EnsureSession() → auth is handled by [Authorize] attribute
    ///      and CascadingAuthenticationState; user identity comes from claims.
    ///   3. OnLoad caught SqlException and called SiteMaster.ShowDatabaseDown() → database
    ///      error notification is surfaced via the DatabaseDown flag for the layout to display.
    ///   4. QueryId() parsed Request.QueryString["id"] → use [SupplyParameterFromQuery] or
    ///      the GetQueryId() helper below with NavigationManager.
    ///
    /// Derived Blazor components can @inherits CounterPageBase to reuse these helpers.
    /// </summary>
    public abstract class CounterPageBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        [CascadingParameter]
        private Task<AuthenticationState>? AuthState { get; set; }

        /// <summary>
        /// When true, the layout should display a database-unavailable banner.
        /// Replaces the legacy SiteMaster.ShowDatabaseDown() call.
        /// </summary>
        protected bool DatabaseDown { get; private set; }

        /// <summary>
        /// Message to show when the database is unavailable.
        /// </summary>
        protected string DatabaseDownMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Parses the "id" query-string parameter and returns it as a non-negative integer.
        /// Returns 0 when the parameter is absent, non-numeric, or negative.
        /// Mirrors the legacy CounterPage.QueryId() behavior exactly.
        /// </summary>
        protected int GetQueryId()
        {
            var uri = new Uri(Navigation.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("id", out var values) &&
                int.TryParse(values.ToString(), out int id) &&
                id >= 0)
            {
                return id;
            }

            return 0;
        }

        /// <summary>
        /// Returns the current authenticated user's UserId from claims.
        /// Returns 0 when the user is not authenticated or the claim is missing.
        /// Replaces AuthData.CurrentUserId() which read from Session.
        /// </summary>
        protected int GetCurrentUserId()
        {
            if (AuthState == null) return 0;
            var authState = AuthState.Result;
            var user = authState.User;
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }

            return 0;
        }

        /// <summary>
        /// Returns the current authenticated user's DisplayName from claims.
        /// Returns empty string when not authenticated or claim is missing.
        /// Replaces AuthData.CurrentDisplayName() which read from Session.
        /// </summary>
        protected string GetCurrentDisplayName()
        {
            if (AuthState == null) return string.Empty;
            var authState = AuthState.Result;
            var user = authState.User;
            var claim = user.FindFirst(ClaimTypes.Name);
            return claim?.Value ?? string.Empty;
        }

        /// <summary>
        /// Wraps a data-loading action with SqlException handling.
        /// On SqlException, sets DatabaseDown = true and the message, mirroring
        /// the legacy CounterPage.OnLoad catch block that called SiteMaster.ShowDatabaseDown().
        /// </summary>
        protected void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (SqlException)
            {
                DatabaseDown = true;
                DatabaseDownMessage = Messages.DbDown;
            }
        }

        /// <summary>
        /// Async version of SafeExecute for async data-loading actions.
        /// </summary>
        protected async Task SafeExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (SqlException)
            {
                DatabaseDown = true;
                DatabaseDownMessage = Messages.DbDown;
            }
        }
    }
}
