using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ApexCounter
{
    public static class OrderData
    {
        public static DataTable List(string status, string customerTerm)
        {
            string statusClause = "";
            if (status == "Open" || status == "Picked" || status == "Invoiced" || status == "Cancelled")
            {
                statusClause = " AND o.Status = @status";
            }

            SqlParameter statusParameter = new SqlParameter("@status", status ?? "");
            return Db.Query(@"
                SELECT o.OrderId, o.OrderNumber, c.CompanyName, o.Status, o.CreatedAt, o.Total
                FROM dbo.SalesOrder o
                INNER JOIN dbo.Customer c ON c.CustomerId = o.CustomerId
                WHERE c.CompanyName LIKE @customer" + statusClause + @"
                ORDER BY o.CreatedAt DESC, o.OrderId DESC",
                new SqlParameter("@customer", Db.Like(customerTerm)),
                statusParameter);
        }

        public static DataTable ActiveCustomers()
        {
            return Db.Query(@"
                SELECT CustomerId, AccountCode + N' - ' + CompanyName AS Label
                FROM dbo.Customer
                WHERE IsActive = 1
                ORDER BY CompanyName");
        }

        public static DataRow Header(int orderId)
        {
            DataTable table = Db.Query(@"
                SELECT o.OrderId, o.OrderNumber, o.CustomerId, c.CompanyName, c.AccountCode,
                       o.Status, o.CreatedAt, o.PickedAt, o.InvoicedAt, o.Subtotal, o.Tax, o.Total
                FROM dbo.SalesOrder o
                INNER JOIN dbo.Customer c ON c.CustomerId = o.CustomerId
                WHERE o.OrderId = @id",
                new SqlParameter("@id", orderId));
            if (table.Rows.Count != 1) return null;
            return table.Rows[0];
        }

        public static DataTable Lines(int orderId)
        {
            return Db.Query(@"
                SELECT Sku, ProductName, Quantity, UnitPrice, LineTotal
                FROM dbo.SalesOrderLine
                WHERE OrderId = @id
                ORDER BY LineId",
                new SqlParameter("@id", orderId));
        }

        public static DataTable Ledger(int orderId)
        {
            return Db.Query(@"
                SELECT p.Sku, l.QtyChange, l.Reason, l.Note, l.CreatedAt
                FROM dbo.StockLedger l
                INNER JOIN dbo.Product p ON p.ProductId = l.ProductId
                WHERE l.OrderId = @id
                ORDER BY l.LedgerId",
                new SqlParameter("@id", orderId));
        }

        public static string[] NextStatuses(string status)
        {
            if (status == "Open") return new string[] { "Open", "Picked", "Cancelled" };
            if (status == "Picked") return new string[] { "Picked", "Invoiced", "Cancelled" };
            if (status == "Invoiced") return new string[] { "Invoiced" };
            if (status == "Cancelled") return new string[] { "Cancelled" };
            return new string[] { status };
        }

        public static ActionResult CreateOpen(int customerId, int userId, DataTable draft)
        {
            if (customerId <= 0) return ActionResult.Fail(Messages.NeedCustomer);
            if (draft == null || draft.Rows.Count == 0) return ActionResult.Fail(Messages.NeedLines);

            using (SqlConnection connection = Db.Open())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                object active = Db.Scalar(connection, transaction,
                    "SELECT IsActive FROM dbo.Customer WITH (UPDLOCK, ROWLOCK) WHERE CustomerId = @id",
                    new SqlParameter("@id", customerId));
                if (active == null || active == DBNull.Value || Convert.ToInt32(active) != 1)
                {
                    return ActionResult.Fail(Messages.CustomerInactive);
                }

                decimal subtotal = 0m;
                foreach (DataRow row in draft.Rows)
                {
                    subtotal += Convert.ToDecimal(row["LineTotal"]);
                }
                subtotal = Money.Round(subtotal);
                decimal tax = Money.Tax(subtotal);
                decimal total = Money.Round(subtotal + tax);

                string tempNumber = "TMP-" + Guid.NewGuid().ToString("N").Substring(0, 12);
                object identity = Db.Scalar(connection, transaction, @"
                    INSERT INTO dbo.SalesOrder
                        (OrderNumber, CustomerId, Status, CreatedAt, Subtotal, Tax, Total, CreatedBy)
                    VALUES
                        (@tempNumber, @customerId, N'Open', GETDATE(), @subtotal, @tax, @total, @userId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);",
                    new SqlParameter("@tempNumber", tempNumber),
                    new SqlParameter("@customerId", customerId),
                    Db.MoneyParam("@subtotal", subtotal),
                    Db.MoneyParam("@tax", tax),
                    Db.MoneyParam("@total", total),
                    new SqlParameter("@userId", userId));
                int orderId = Convert.ToInt32(identity);
                string orderNumber = "AC-" + orderId.ToString("0000");
                Db.Exec(connection, transaction,
                    "UPDATE dbo.SalesOrder SET OrderNumber = @number WHERE OrderId = @id",
                    new SqlParameter("@number", orderNumber),
                    new SqlParameter("@id", orderId));

                foreach (DataRow row in draft.Rows)
                {
                    Db.Exec(connection, transaction, @"
                        INSERT INTO dbo.SalesOrderLine
                            (OrderId, ProductId, Sku, ProductName, Quantity, UnitPrice, LineTotal)
                        VALUES
                            (@orderId, @productId, @sku, @name, @qty, @price, @lineTotal)",
                        new SqlParameter("@orderId", orderId),
                        new SqlParameter("@productId", Convert.ToInt32(row["ProductId"])),
                        new SqlParameter("@sku", Convert.ToString(row["Sku"])),
                        new SqlParameter("@name", Convert.ToString(row["ProductName"])),
                        new SqlParameter("@qty", Convert.ToInt32(row["Quantity"])),
                        Db.MoneyParam("@price", Convert.ToDecimal(row["UnitPrice"])),
                        Db.MoneyParam("@lineTotal", Convert.ToDecimal(row["LineTotal"])));
                }

                transaction.Commit();
                return ActionResult.Success(string.Format(Messages.OrderSaved, orderNumber), orderId);
            }
        }

        /*
            Open -> Picked deducts stock, or the whole change is refused.
            Picked -> Invoiced does not touch stock.
            Picked -> Cancelled puts the stock back.
            Open -> Cancelled does not touch stock.
            Invoiced and Cancelled are finished.
        */
        public static ActionResult Transition(int orderId, string nextStatus, int userId)
        {
            using (SqlConnection connection = Db.Open())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                DataTable header = Db.Query(connection, transaction,
                    "SELECT OrderNumber, Status FROM dbo.SalesOrder WITH (UPDLOCK, ROWLOCK) WHERE OrderId = @id",
                    new SqlParameter("@id", orderId));
                if (header.Rows.Count != 1) return ActionResult.Fail(Messages.NotFound);

                string orderNumber = Convert.ToString(header.Rows[0]["OrderNumber"]);
                string status = Convert.ToString(header.Rows[0]["Status"]);
                if (status == nextStatus) return ActionResult.Fail(Messages.NoStatusChange);

                bool legal =
                    (status == "Open" && (nextStatus == "Picked" || nextStatus == "Cancelled")) ||
                    (status == "Picked" && (nextStatus == "Invoiced" || nextStatus == "Cancelled"));
                if (!legal)
                {
                    return ActionResult.Fail(string.Format(Messages.IllegalStatus, status, nextStatus));
                }

                if (nextStatus == "Picked")
                {
                    ActionResult stock = ApplyStock(connection, transaction, orderId, userId, -1, "pick");
                    if (!stock.Ok) return stock;
                    Db.Exec(connection, transaction,
                        "UPDATE dbo.SalesOrder SET Status = N'Picked', PickedAt = GETDATE() WHERE OrderId = @id",
                        new SqlParameter("@id", orderId));
                }
                else if (status == "Picked" && nextStatus == "Cancelled")
                {
                    ActionResult stock = ApplyStock(connection, transaction, orderId, userId, 1, "cancel-return");
                    if (!stock.Ok) return stock;
                    Db.Exec(connection, transaction,
                        "UPDATE dbo.SalesOrder SET Status = N'Cancelled' WHERE OrderId = @id",
                        new SqlParameter("@id", orderId));
                }
                else if (nextStatus == "Invoiced")
                {
                    Db.Exec(connection, transaction,
                        "UPDATE dbo.SalesOrder SET Status = N'Invoiced', InvoicedAt = GETDATE() WHERE OrderId = @id",
                        new SqlParameter("@id", orderId));
                }
                else
                {
                    Db.Exec(connection, transaction,
                        "UPDATE dbo.SalesOrder SET Status = N'Cancelled' WHERE OrderId = @id",
                        new SqlParameter("@id", orderId));
                }

                transaction.Commit();
                return ActionResult.Success(string.Format(Messages.StatusApplied, orderNumber, nextStatus), orderId);
            }
        }

        private static ActionResult ApplyStock(SqlConnection connection, SqlTransaction transaction, int orderId, int userId, int sign, string reason)
        {
            DataTable lines = Db.Query(connection, transaction,
                "SELECT ProductId, Quantity FROM dbo.SalesOrderLine WHERE OrderId = @id ORDER BY ProductId",
                new SqlParameter("@id", orderId));

            Dictionary<int, int> need = new Dictionary<int, int>();
            foreach (DataRow row in lines.Rows)
            {
                int productId = Convert.ToInt32(row["ProductId"]);
                int quantity = Convert.ToInt32(row["Quantity"]);
                if (need.ContainsKey(productId)) need[productId] = need[productId] + quantity;
                else need.Add(productId, quantity);
            }

            List<int> productIds = new List<int>(need.Keys);
            productIds.Sort();

            for (int i = 0; i < productIds.Count; i++)
            {
                int productId = productIds[i];
                int quantity = need[productId];
                DataTable locked = Db.Query(connection, transaction,
                    "SELECT Sku, OnHand FROM dbo.Product WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @id",
                    new SqlParameter("@id", productId));
                string sku = Convert.ToString(locked.Rows[0]["Sku"]);
                int onHand = Convert.ToInt32(locked.Rows[0]["OnHand"]);
                int delta = sign * quantity;
                if (onHand + delta < 0)
                {
                    return ActionResult.Fail(string.Format(Messages.StockShort, sku, onHand, quantity));
                }

                Db.Exec(connection, transaction,
                    "UPDATE dbo.Product SET OnHand = OnHand + @delta WHERE ProductId = @id",
                    new SqlParameter("@delta", delta),
                    new SqlParameter("@id", productId));
                Db.Exec(connection, transaction, @"
                    INSERT INTO dbo.StockLedger (ProductId, OrderId, QtyChange, Reason, Note, CreatedAt, CreatedBy)
                    VALUES (@productId, @orderId, @delta, @reason, NULL, GETDATE(), @userId)",
                    new SqlParameter("@productId", productId),
                    new SqlParameter("@orderId", orderId),
                    new SqlParameter("@delta", delta),
                    new SqlParameter("@reason", reason),
                    new SqlParameter("@userId", userId));
            }

            return ActionResult.Success("", orderId);
        }
    }
}
