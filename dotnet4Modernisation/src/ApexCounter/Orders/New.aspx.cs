using System;
using System.Data;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class OrderNewPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            BindCustomers();
            BindProducts();
            BindDraft();
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            int productId;
            int quantity;
            if (!int.TryParse(ddlProduct.SelectedValue, out productId) || productId <= 0)
            {
                lblMessage.Text = Messages.NeedProduct;
                return;
            }
            if (!int.TryParse(txtQty.Text.Trim(), out quantity) || quantity < 1)
            {
                lblMessage.Text = Messages.QtyRequired;
                return;
            }

            DataRow product = CatalogData.Find(productId);
            if (product == null)
            {
                lblMessage.Text = Messages.NotFound;
                return;
            }
            if (!Convert.ToBoolean(product["IsActive"]))
            {
                lblMessage.Text = Messages.ProductInactive;
                return;
            }

            DataTable draft = Draft();
            DataRow existing = null;
            foreach (DataRow row in draft.Rows)
            {
                if (Convert.ToInt32(row["ProductId"]) == productId) existing = row;
            }

            if (existing == null)
            {
                DataRow line = draft.NewRow();
                line["ProductId"] = productId;
                line["Sku"] = product["Sku"];
                line["ProductName"] = product["ProductName"];
                line["Quantity"] = quantity;
                line["UnitPrice"] = product["UnitPrice"];
                line["LineTotal"] = Money.LineTotal(quantity, Convert.ToDecimal(product["UnitPrice"]));
                draft.Rows.Add(line);
                lblMessage.Text = string.Format(Messages.LineAdded, product["Sku"]);
            }
            else
            {
                int nextQty = Convert.ToInt32(existing["Quantity"]) + quantity;
                existing["Quantity"] = nextQty;
                existing["LineTotal"] = Money.LineTotal(nextQty, Convert.ToDecimal(existing["UnitPrice"]));
                lblMessage.Text = string.Format(Messages.LineMerged, quantity, existing["Sku"], nextQty);
            }

            BindDraft();
        }

        protected void gvLines_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "remove") return;
            int index = Convert.ToInt32(e.CommandArgument);
            DataTable draft = Draft();
            if (index < 0 || index >= draft.Rows.Count) return;
            draft.Rows.RemoveAt(index);
            lblMessage.Text = "";
            BindDraft();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int customerId;
            if (!int.TryParse(ddlCustomer.SelectedValue, out customerId) || customerId <= 0)
            {
                lblMessage.Text = Messages.NeedCustomer;
                return;
            }
            ActionResult result = OrderData.CreateOpen(customerId, AuthData.CurrentUserId(), Draft());
            if (!result.Ok)
            {
                lblMessage.Text = result.Message;
                return;
            }
            Session.Remove("ApexDraft");
            Response.Redirect("Detail.aspx?id=" + result.Id.ToString());
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            Session.Remove("ApexDraft");
            lblMessage.Text = "";
            BindDraft();
        }

        private void BindCustomers()
        {
            ddlCustomer.Items.Clear();
            ddlCustomer.Items.Add(new ListItem("Choose a customer", "0"));
            DataTable table = OrderData.ActiveCustomers();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                ddlCustomer.Items.Add(new ListItem(
                    Convert.ToString(table.Rows[i]["Label"]),
                    Convert.ToString(table.Rows[i]["CustomerId"])));
            }
        }

        private void BindProducts()
        {
            ddlProduct.Items.Clear();
            ddlProduct.Items.Add(new ListItem("Choose a product", "0"));
            DataTable table = CatalogData.ActiveForOrder();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                ddlProduct.Items.Add(new ListItem(
                    Convert.ToString(table.Rows[i]["Label"]),
                    Convert.ToString(table.Rows[i]["ProductId"])));
            }
        }

        private void BindDraft()
        {
            DataTable draft = Draft();
            gvLines.DataSource = draft;
            gvLines.DataBind();

            decimal subtotal = 0m;
            foreach (DataRow row in draft.Rows)
            {
                subtotal += Convert.ToDecimal(row["LineTotal"]);
            }
            subtotal = Money.Round(subtotal);
            decimal tax = Money.Tax(subtotal);
            lblSubtotal.Text = subtotal.ToString("N2");
            lblTax.Text = tax.ToString("N2");
            lblTotal.Text = Money.Round(subtotal + tax).ToString("N2");
        }

        private DataTable Draft()
        {
            DataTable table = Session["ApexDraft"] as DataTable;
            if (table != null) return table;
            table = new DataTable();
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Sku", typeof(string));
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("LineTotal", typeof(decimal));
            Session["ApexDraft"] = table;
            return table;
        }
    }
}
