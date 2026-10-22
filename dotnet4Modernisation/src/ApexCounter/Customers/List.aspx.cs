using System;
using System.Web.UI.WebControls;

namespace ApexCounter
{
    public partial class CustomerListPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvCustomers.PageIndex = 0;
            BindGrid();
        }

        protected void gvCustomers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvCustomers.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        private void BindGrid()
        {
            gvCustomers.DataSource = CustomerData.List(txtSearch.Text, ddlActive.SelectedValue);
            gvCustomers.DataBind();
        }
    }
}
