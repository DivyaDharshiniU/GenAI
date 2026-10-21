using System;
using System.Data;
using System.Globalization;

namespace ApexCounter
{
    public partial class CatalogEditPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            int id = QueryId();
            pnlAdjust.Visible = id > 0;
            if (id == 0) return;
            LoadProduct(id);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;
            decimal price;
            int reorder;
            if (!decimal.TryParse(txtPrice.Text.Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out price) || price < 0)
            {
                lblMessage.Text = Messages.BadPrice;
                return;
            }
            if (!int.TryParse(txtReorder.Text.Trim(), out reorder) || reorder < 0)
            {
                lblMessage.Text = Messages.BadReorder;
                return;
            }

            ActionResult result = CatalogData.Save(QueryId(), txtSku.Text, txtName.Text, price, reorder, chkActive.Checked);
            if (!result.Ok || QueryId() > 0)
            {
                lblMessage.Text = result.Message;
                return;
            }
            Response.Redirect("Edit.aspx?id=" + result.Id.ToString() + "&saved=1");
        }

        protected void btnAdjust_Click(object sender, EventArgs e)
        {
            int qty;
            if (!int.TryParse(txtAdjustQty.Text.Trim(), out qty) || qty == 0)
            {
                lblMessage.Text = Messages.BadAdjust;
                return;
            }
            ActionResult result = CatalogData.Adjust(QueryId(), qty, txtAdjustNote.Text, AuthData.CurrentUserId());
            lblMessage.Text = result.Message;
            if (result.Ok)
            {
                lblOnHand.Text = result.Id.ToString();
                txtAdjustQty.Text = "";
                txtAdjustNote.Text = "";
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("List.aspx");
        }

        private void LoadProduct(int id)
        {
            DataRow row = CatalogData.Find(id);
            if (row == null)
            {
                lblMessage.Text = Messages.NotFound;
                pnlAdjust.Visible = false;
                return;
            }
            txtSku.Text = Convert.ToString(row["Sku"]);
            txtName.Text = Convert.ToString(row["ProductName"]);
            txtPrice.Text = Convert.ToDecimal(row["UnitPrice"]).ToString("N2");
            txtReorder.Text = Convert.ToString(row["ReorderPoint"]);
            lblOnHand.Text = Convert.ToString(row["OnHand"]);
            chkActive.Checked = Convert.ToBoolean(row["IsActive"]);
            if (Request.QueryString["saved"] == "1")
            {
                lblMessage.Text = string.Format(Messages.ProductSaved, txtSku.Text);
            }
        }
    }
}
