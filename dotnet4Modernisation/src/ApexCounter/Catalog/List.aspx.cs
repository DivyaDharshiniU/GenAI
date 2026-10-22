using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class CatalogListPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvProducts.PageIndex = 0;
            BindGrid();
        }

        protected void gvProducts_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProducts.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvProducts_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            int onHand = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "OnHand"));
            int reorder = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "ReorderPoint"));
            if (onHand <= reorder) e.Row.CssClass = "low";
        }

        private void BindGrid()
        {
            gvProducts.DataSource = CatalogData.List(txtSearch.Text, chkLow.Checked);
            gvProducts.DataBind();
        }
    }
}
