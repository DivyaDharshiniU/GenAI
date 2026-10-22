using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;

namespace ApexCounter
{
    public static class AuthData
    {
        public static bool TryLogin(string userName, string password, out string error)
        {
            error = null;
            string name = userName == null ? "" : userName.Trim();
            DataTable table = Db.Query(
                "SELECT UserId, DisplayName, PasswordSalt, PasswordHash FROM dbo.AppUser WHERE UserName = @name AND IsActive = 1",
                new SqlParameter("@name", name));
            if (table.Rows.Count != 1)
            {
                error = Messages.LoginFailed;
                return false;
            }

            DataRow row = table.Rows[0];
            string hash = PasswordHasher.Hash(Convert.ToString(row["PasswordSalt"]), password ?? "");
            string stored = Convert.ToString(row["PasswordHash"]).Trim();
            if (!string.Equals(hash, stored, StringComparison.OrdinalIgnoreCase))
            {
                error = Messages.LoginFailed;
                return false;
            }

            Remember(Convert.ToInt32(row["UserId"]), Convert.ToString(row["DisplayName"]));
            return true;
        }

        public static void EnsureSession()
        {
            HttpContext context = HttpContext.Current;
            if (context.Session["UserId"] != null)
            {
                return;
            }
            if (context.User == null || !context.User.Identity.IsAuthenticated)
            {
                return;
            }

            DataTable table = Db.Query(
                "SELECT UserId, DisplayName FROM dbo.AppUser WHERE UserName = @name AND IsActive = 1",
                new SqlParameter("@name", context.User.Identity.Name));
            if (table.Rows.Count != 1)
            {
                return;
            }
            Remember(Convert.ToInt32(table.Rows[0]["UserId"]), Convert.ToString(table.Rows[0]["DisplayName"]));
        }

        public static int CurrentUserId()
        {
            EnsureSession();
            return Convert.ToInt32(HttpContext.Current.Session["UserId"]);
        }

        public static string CurrentDisplayName()
        {
            object value = HttpContext.Current.Session["DisplayName"];
            return value == null ? "" : Convert.ToString(value);
        }

        public static ActionResult ChangePassword(int userId, string currentPassword, string newPassword)
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

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
            HttpContext.Current.Session.Abandon();
        }

        private static void Remember(int userId, string displayName)
        {
            HttpContext.Current.Session["UserId"] = userId;
            HttpContext.Current.Session["DisplayName"] = displayName;
        }
    }
}
