using System;
using System.Globalization;
using System.Threading;
using System.Web.UI;

namespace ApexCounter
{
    public class CounterPage : Page
    {
        protected override void InitializeCulture()
        {
            Culture = "en-US";
            UICulture = "en-US";
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            base.InitializeCulture();
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                AuthData.EnsureSession();
                base.OnLoad(e);
            }
            catch (System.Data.SqlClient.SqlException)
            {
                SiteMaster master = Master as SiteMaster;
                if (master != null) master.ShowDatabaseDown();
            }
        }

        protected int QueryId()
        {
            int id;
            if (!int.TryParse(Request.QueryString["id"], out id)) return 0;
            if (id < 0) return 0;
            return id;
        }
    }
}
