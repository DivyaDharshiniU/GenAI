using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace ApexCounter
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                lblUser.Text = AuthData.CurrentDisplayName();
            }
            catch (SqlException)
            {
                ShowDatabaseDown();
            }
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            AuthData.SignOut();
            Response.Redirect("~/Login.aspx");
        }

        public void ShowDatabaseDown()
        {
            pnlDb.Visible = true;
            lblDb.Text = Messages.DbDown;
        }
    }
}
