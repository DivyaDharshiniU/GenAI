using System;
using System.Data.SqlClient;

namespace ApexCounter
{
    public partial class LoginPage : CounterPage
    {
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            string error;
            try
            {
                if (!AuthData.TryLogin(txtUser.Text, txtPassword.Text, out error))
                {
                    lblError.Text = error;
                    return;
                }
            }
            catch (SqlException)
            {
                lblError.Text = Messages.DbDown;
                return;
            }
            System.Web.Security.FormsAuthentication.RedirectFromLoginPage(txtUser.Text.Trim(), false);
        }
    }
}
