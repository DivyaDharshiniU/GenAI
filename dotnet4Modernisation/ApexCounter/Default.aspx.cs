using System;
using System.Data;

namespace ApexCounter
{
    public partial class DeskPage : CounterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            DataTable counts = DeskData.Counts();
            DataRow row = counts.Rows[0];
            lblOpen.Text = Convert.ToString(row["OpenOrders"]);
            lblLow.Text = Convert.ToString(row["LowStock"]);
            lblSales.Text = Convert.ToDecimal(row["TodaySales"]).ToString("N2");
            gvRecent.DataSource = DeskData.Recent();
            gvRecent.DataBind();
        }
    }
}
