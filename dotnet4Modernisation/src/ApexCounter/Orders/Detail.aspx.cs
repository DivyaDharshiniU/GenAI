using System;
using System.Data;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class OrderDetailPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindOrder();
        }

        protected void btnApply_Click(object sender, EventArgs e)
        {
            ActionResult result = OrderData.Transition(QueryId(), ddlNext.SelectedValue, AuthData.CurrentUserId());
            lblMessage.Text = result.Message;
            BindOrder();
        }

        private void BindOrder()
        {
            int id = QueryId();
            DataRow header = id == 0 ? null : OrderData.Header(id);
            if (header == null)
            {
                lblNumber.Text = "Order";
                lblMessage.Text = Messages.NotFound;
                pnlApply.Visible = false;
                return;
            }

            lblNumber.Text = Convert.ToString(header["OrderNumber"]);
            lblCustomer.Text = Convert.ToString(header["AccountCode"]) + " " + Convert.ToString(header["CompanyName"]);
            lblStatus.Text = Convert.ToString(header["Status"]);
            lblCreated.Text = Convert.ToDateTime(header["CreatedAt"]).ToString("dd MMM yyyy HH:mm");
            lblPicked.Text = header["PickedAt"] == DBNull.Value ? "-" : Convert.ToDateTime(header["PickedAt"]).ToString("dd MMM yyyy HH:mm");
            lblInvoiced.Text = header["InvoicedAt"] == DBNull.Value ? "-" : Convert.ToDateTime(header["InvoicedAt"]).ToString("dd MMM yyyy HH:mm");
            lblSubtotal.Text = Convert.ToDecimal(header["Subtotal"]).ToString("N2");
            lblTax.Text = Convert.ToDecimal(header["Tax"]).ToString("N2");
            lblTotal.Text = Convert.ToDecimal(header["Total"]).ToString("N2");

            gvLines.DataSource = OrderData.Lines(id);
            gvLines.DataBind();
            gvLedger.DataSource = OrderData.Ledger(id);
            gvLedger.DataBind();

            string status = Convert.ToString(header["Status"]);
            ddlNext.Items.Clear();
            string[] choices = OrderData.NextStatuses(status);
            for (int i = 0; i < choices.Length; i++)
            {
                ddlNext.Items.Add(new ListItem(choices[i], choices[i]));
            }
            ddlNext.SelectedValue = status;
            btnApply.Enabled = choices.Length > 1;
            pnlApply.Visible = true;
        }
    }
}
