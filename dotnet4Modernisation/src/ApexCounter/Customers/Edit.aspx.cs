using System;
using System.Data;

namespace ApexCounter
{
    public partial class CustomerEditPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            int id = QueryId();
            if (id == 0) return;
            DataRow row = CustomerData.Find(id);
            if (row == null)
            {
                lblMessage.Text = Messages.NotFound;
                return;
            }
            txtAccount.Text = Convert.ToString(row["AccountCode"]);
            txtCompany.Text = Convert.ToString(row["CompanyName"]);
            txtContact.Text = Convert.ToString(row["ContactName"]);
            txtPhone.Text = Convert.ToString(row["Phone"]);
            txtCity.Text = Convert.ToString(row["City"]);
            chkActive.Checked = Convert.ToBoolean(row["IsActive"]);
            if (Request.QueryString["saved"] == "1")
            {
                lblMessage.Text = string.Format(Messages.CustomerSaved, txtAccount.Text);
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            ActionResult result = CustomerData.Save(
                QueryId(), txtAccount.Text, txtCompany.Text, txtContact.Text, txtPhone.Text, txtCity.Text, chkActive.Checked);
            if (!result.Ok || QueryId() > 0)
            {
                lblMessage.Text = result.Message;
                return;
            }
            Response.Redirect("Edit.aspx?id=" + result.Id.ToString() + "&saved=1");
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("List.aspx");
        }
    }
}
