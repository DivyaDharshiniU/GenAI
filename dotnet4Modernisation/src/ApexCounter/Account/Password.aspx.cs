using System;

namespace ApexCounter
{
    public partial class PasswordPage : CounterPage
    {
        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            if (txtNew.Text != txtConfirm.Text)
            {
                lblMessage.Text = Messages.PasswordMismatch;
                return;
            }
            ActionResult result = AuthData.ChangePassword(AuthData.CurrentUserId(), txtCurrent.Text, txtNew.Text);
            lblMessage.Text = result.Message;
            if (result.Ok)
            {
                txtCurrent.Text = "";
                txtNew.Text = "";
                txtConfirm.Text = "";
            }
        }
    }
}
