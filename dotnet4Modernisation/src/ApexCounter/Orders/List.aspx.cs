using System;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class OrderListPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindGrid();
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            gvOrders.PageIndex = 0;
            BindGrid();
        }

        protected void gvOrders_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvOrders.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        private void BindGrid()
        {
            gvOrders.DataSource = OrderData.List(ddlStatus.SelectedValue, txtCustomer.Text);
            gvOrders.DataBind();
        }
    }
}
