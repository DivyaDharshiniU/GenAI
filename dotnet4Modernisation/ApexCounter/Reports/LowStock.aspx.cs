using System;
using System.Data;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class LowStockPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindGrid();
        }

        protected void gvLow_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvLow.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvLow_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            e.Row.CssClass = "low";
        }

        private void BindGrid()
        {
            DataTable table = CatalogData.LowStockReport();
            lblCount.Text = table.Rows.Count.ToString();
            gvLow.DataSource = table;
            gvLow.DataBind();
        }
    }
}
