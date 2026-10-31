using System.Data;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace ApexCounter
{
    public static class DeskData
    {
        public static DataTable Counts()
        {
            return Db.Query(@"
                SELECT
                    (SELECT COUNT(*) FROM dbo.SalesOrder WHERE Status = N'Open') AS OpenOrders,
                    (SELECT COUNT(*) FROM dbo.Product WHERE IsActive = 1 AND OnHand <= ReorderPoint) AS LowStock,
                    (SELECT ISNULL(SUM(Total), 0) FROM dbo.SalesOrder
                       WHERE Status = N'Invoiced'
                         AND CONVERT(date, InvoicedAt) = CONVERT(date, GETDATE())) AS TodaySales");
        }

        public static DataTable Recent()
        {
            return Db.Query(@"
                SELECT TOP 8
                    o.OrderId, o.OrderNumber, c.CompanyName, o.Status, o.CreatedAt, o.Total
                FROM dbo.SalesOrder o
                INNER JOIN dbo.Customer c ON c.CustomerId = o.CustomerId
                ORDER BY o.CreatedAt DESC, o.OrderId DESC");
        }
    }

    public static class CustomerData
    {
        public static DataTable List(string term, string activeFilter)
        {
            string activeClause = "";
            if (activeFilter == "yes")
                activeClause = " AND IsActive = 1";
            if (activeFilter == "no")
                activeClause = " AND IsActive = 0";
            return Db.Query(@"
                SELECT CustomerId, AccountCode, CompanyName, ContactName, Phone, City,
                       CASE WHEN IsActive = 1 THEN N'Yes' ELSE N'No' END AS ActiveText
                FROM dbo.Customer
                WHERE (CompanyName LIKE @term OR ContactName LIKE @term OR AccountCode LIKE @term OR City LIKE @term)" + activeClause + @"
                ORDER BY CompanyName", new SqlParameter("@term", Db.Like(term)));
        }

        public static DataRow Find(int customerId)
        {
            DataTable table = Db.Query("SELECT CustomerId, AccountCode, CompanyName, ContactName, Phone, City, IsActive FROM dbo.Customer WHERE CustomerId = @id", new SqlParameter("@id", customerId));
            if (table.Rows.Count != 1)
                return null;
            return table.Rows[0];
        }

        public static ActionResult Save(int customerId, string accountCode, string companyName, string contactName, string phone, string city, bool isActive)
        {
            string account = accountCode.Trim();
            object existing = null;
            using (SqlConnection connection = Db.Open())
            {
                existing = Db.Scalar(connection, null, "SELECT CustomerId FROM dbo.Customer WHERE AccountCode = @account AND CustomerId <> @id", new SqlParameter("@account", account), new SqlParameter("@id", customerId));
                if (existing != null && existing != System.DBNull.Value)
                {
                    return ActionResult.Fail(string.Format(Messages.DuplicateAccount, account));
                }

                if (customerId == 0)
                {
                    object id = Db.Scalar(connection, null, @"
                        INSERT INTO dbo.Customer (AccountCode, CompanyName, ContactName, Phone, City, IsActive)
                        VALUES (@account, @company, @contact, @phone, @city, @active);
                        SELECT CAST(SCOPE_IDENTITY() AS int);", new SqlParameter("@account", account), new SqlParameter("@company", companyName.Trim()), new SqlParameter("@contact", contactName.Trim()), new SqlParameter("@phone", phone.Trim()), new SqlParameter("@city", city.Trim()), new SqlParameter("@active", isActive));
                    int newId = System.Convert.ToInt32(id);
                    return ActionResult.Success(string.Format(Messages.CustomerSaved, account), newId);
                }

                int rows = Db.Exec(connection, null, @"
                    UPDATE dbo.Customer
                    SET AccountCode = @account, CompanyName = @company, ContactName = @contact,
                        Phone = @phone, City = @city, IsActive = @active
                    WHERE CustomerId = @id", new SqlParameter("@account", account), new SqlParameter("@company", companyName.Trim()), new SqlParameter("@contact", contactName.Trim()), new SqlParameter("@phone", phone.Trim()), new SqlParameter("@city", city.Trim()), new SqlParameter("@active", isActive), new SqlParameter("@id", customerId));
                if (rows != 1)
                    return ActionResult.Fail(Messages.NotFound);
                return ActionResult.Success(string.Format(Messages.CustomerSaved, account), customerId);
            }
        }
    }
}