using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace ApexCounter
{
    public class AuthData
    {
        private readonly Db _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthData(Db db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string? Error)> TryLoginAsync(string userName, string password)
        {
            string name = userName == null ? "" : userName.Trim();
            DataTable table = _db.Query(
                "SELECT UserId, DisplayName, PasswordSalt, PasswordHash FROM dbo.AppUser WHERE UserName = @name AND IsActive = 1",
                new SqlParameter("@name", name));

            if (table.Rows.Count != 1)
            {
                return (false, Messages.LoginFailed);
            }

            DataRow row = table.Rows[0];
            string hash = PasswordHasher.Hash(Convert.ToString(row["PasswordSalt"]), password ?? "");
            string stored = Convert.ToString(row["PasswordHash"]).Trim();
            if (!string.Equals(hash, stored, StringComparison.OrdinalIgnoreCase))
            {
                return (false, Messages.LoginFailed);
            }

            int userId = Convert.ToInt32(row["UserId"]);
            string displayName = Convert.ToString(row["DisplayName"]);
            await RememberAsync(userId, displayName);
            return (true, null);
        }

        /// <summary>
        /// Returns the current authenticated user's ID from claims, or 0 if not authenticated.
        /// </summary>
        public int CurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("UserId");
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }

            return 0;
        }

        /// <summary>
        /// Returns the current authenticated user's display name from claims, or empty string.
        /// </summary>
        public string CurrentDisplayName()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return "";
            }

            var claim = httpContext.User.FindFirst("DisplayName");
            return claim != null ? claim.Value : "";
        }

        public ActionResult ChangePassword(int userId, string currentPassword, string newPassword)
        {
            DataTable table = _db.Query(
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
            using (SqlConnection connection = _db.Open())
            {
                _db.Exec(connection, null,
                    "UPDATE dbo.AppUser SET PasswordHash = @hash WHERE UserId = @id",
                    new SqlParameter("@hash", updated),
                    new SqlParameter("@id", userId));
            }

            return ActionResult.Success(Messages.PasswordOk, userId);
        }

        public async Task SignOutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        /// <summary>
        /// Issues a cookie authentication ticket with UserId and DisplayName as claims.
        /// Replaces the legacy FormsAuthentication ticket + Session storage.
        /// </summary>
        private async Task RememberAsync(int userId, string displayName)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("UserId", userId.ToString()),
                new Claim("DisplayName", displayName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false
                    });
            }
        }
    }
}
