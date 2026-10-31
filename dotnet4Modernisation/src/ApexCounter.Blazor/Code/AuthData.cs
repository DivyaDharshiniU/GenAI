using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace ApexCounter
{
    /// <summary>
    /// Authentication and identity service for the ApexCounter application.
    /// Replaces the legacy static AuthData helper that relied on FormsAuthentication
    /// and HttpContext.Current.Session. Now uses ASP.NET Core cookie authentication
    /// with claims-based identity.
    /// </summary>
    public class AuthData
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthData(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP context available.");

        /// <summary>
        /// Validates the supplied credentials against the database and, on success,
        /// signs the user in via ASP.NET Core cookie authentication with claims
        /// carrying UserId and DisplayName.
        /// </summary>
        public async Task<bool> TryLoginAsync(string userName, string password, Action<string> onError)
        {
            string name = userName == null ? "" : userName.Trim();
            DataTable table = Db.Query(
                "SELECT UserId, DisplayName, PasswordSalt, PasswordHash FROM dbo.AppUser WHERE UserName = @name AND IsActive = 1",
                new SqlParameter("@name", name));

            if (table.Rows.Count != 1)
            {
                onError?.Invoke(Messages.LoginFailed);
                return false;
            }

            DataRow row = table.Rows[0];
            string hash = PasswordHasher.Hash(Convert.ToString(row["PasswordSalt"]), password ?? "");
            string stored = Convert.ToString(row["PasswordHash"]).Trim();
            if (!string.Equals(hash, stored, StringComparison.OrdinalIgnoreCase))
            {
                onError?.Invoke(Messages.LoginFailed);
                return false;
            }

            int userId = Convert.ToInt32(row["UserId"]);
            string displayName = Convert.ToString(row["DisplayName"]);

            await SignInWithClaimsAsync(name, userId, displayName);
            return true;
        }

        /// <summary>
        /// Synchronous overload preserved for callers that cannot easily go async.
        /// Delegates to <see cref="TryLoginAsync"/> and blocks.
        /// </summary>
        public bool TryLogin(string userName, string password, out string error)
        {
            string capturedError = null;
            bool result = TryLoginAsync(userName, password, e => capturedError = e)
                .GetAwaiter().GetResult();
            error = capturedError;
            return result;
        }

        /// <summary>
        /// In the legacy application this re-populated Session from the auth cookie
        /// when Session had expired. With claims-based auth the identity data lives
        /// in the cookie itself and does not need a separate session refresh.
        /// This method is retained as a no-op for call-site compatibility during migration.
        /// </summary>
        public void EnsureSession()
        {
            // No-op: UserId and DisplayName are now stored as claims in the
            // authentication cookie, so there is no session to re-populate.
        }

        /// <summary>
        /// Returns the current authenticated user's ID from the UserId claim.
        /// </summary>
        public int CurrentUserId()
        {
            var principal = HttpContext.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            string value = principal.FindFirst("UserId")?.Value;
            return value != null ? Convert.ToInt32(value) : 0;
        }

        /// <summary>
        /// Returns the current authenticated user's display name from the DisplayName claim.
        /// </summary>
        public string CurrentDisplayName()
        {
            var principal = HttpContext.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return "";
            }

            return principal.FindFirst("DisplayName")?.Value ?? "";
        }

        /// <summary>
        /// Changes the password for the given user, verifying the current password first.
        /// This method is pure data logic and does not depend on the authentication mechanism.
        /// </summary>
        public ActionResult ChangePassword(int userId, string currentPassword, string newPassword)
        {
            DataTable table = Db.Query(
                "SELECT PasswordSalt, PasswordHash FROM dbo.AppUser WHERE UserId = @id",
                new SqlParameter("@id", userId));

            if (table.Rows.Count != 1)
            {
                return ActionResult.Fail(Messages.NotFound);
            }

            DataRow row = table.Rows[0];
            string salt = Convert.ToString(row["PasswordSalt"]);
            string currentHash = PasswordHasher.Hash(salt, currentPassword ?? "");
            if (!string.Equals(currentHash, Convert.ToString(row["PasswordHash"]).Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ActionResult.Fail(Messages.PasswordWrong);
            }

            if (newPassword == null || newPassword.Length < 6)
            {
                return ActionResult.Fail(Messages.PasswordShort);
            }

            string updated = PasswordHasher.Hash(salt, newPassword);
            using (SqlConnection connection = Db.Open())
            {
                Db.Exec(connection, null,
                    "UPDATE dbo.AppUser SET PasswordHash = @hash WHERE UserId = @id",
                    new SqlParameter("@hash", updated),
                    new SqlParameter("@id", userId));
            }

            return ActionResult.Success(Messages.PasswordOk, userId);
        }

        /// <summary>
        /// Signs the current user out of the application by clearing the
        /// authentication cookie.
        /// </summary>
        public async Task SignOutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Synchronous overload preserved for callers that cannot easily go async.
        /// </summary>
        public void SignOut()
        {
            SignOutAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a claims principal with UserId, DisplayName, and Name claims,
        /// then signs the user in via the cookie authentication scheme.
        /// </summary>
        private async Task SignInWithClaimsAsync(string userName, int userId, string displayName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim("UserId", userId.ToString()),
                new Claim("DisplayName", displayName ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false
                });
        }
    }
}
