using System;
using System.Data;
using System.Data.SqlClient;

namespace ApexCounter
{
    public static class CatalogData
    {
        public static DataTable List(string term, bool lowOnly)
        {
            string lowClause = lowOnly ? " AND OnHand <= ReorderPoint" : "";
            return Db.Query(@"
                SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint,
                       CASE WHEN IsActive = 1 THEN N'Yes' ELSE N'No' END AS ActiveText
                FROM dbo.Product
                WHERE (Sku LIKE @term OR ProductName LIKE @term)" + lowClause + @"
                ORDER BY Sku",
                new SqlParameter("@term", Db.Like(term)));
        }

        public static DataTable LowStockReport()
        {
            return Db.Query(@"
                SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint
                FROM dbo.Product
                WHERE IsActive = 1 AND OnHand <= ReorderPoint
                ORDER BY Sku");
        }

        public static DataTable ActiveForOrder()
        {
            return Db.Query(@"
                SELECT ProductId, Sku + N' - ' + ProductName AS Label
                FROM dbo.Product
                WHERE IsActive = 1
                ORDER BY Sku");
        }

        public static DataRow Find(int productId)
        {
            DataTable table = Db.Query(
                "SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint, IsActive FROM dbo.Product WHERE ProductId = @id",
                new SqlParameter("@id", productId));
            if (table.Rows.Count != 1) return null;
            return table.Rows[0];
        }

        public static ActionResult Save(int productId, string sku, string productName, decimal unitPrice, int reorderPoint, bool isActive)
        {
            string code = sku.Trim();
            decimal price = Money.Round(unitPrice);
            using (SqlConnection connection = Db.Open())
            {
                object existing = Db.Scalar(connection, null,
                    "SELECT ProductId FROM dbo.Product WHERE Sku = @sku AND ProductId <> @id",
                    new SqlParameter("@sku", code),
                    new SqlParameter("@id", productId));
                if (existing != null && existing != DBNull.Value)
                {
                    return ActionResult.Fail(string.Format(Messages.DuplicateSku, code));
                }

                if (productId == 0)
                {
                    object id = Db.Scalar(connection, null, @"
                        INSERT INTO dbo.Product (Sku, ProductName, UnitPrice, OnHand, ReorderPoint, IsActive)
                        VALUES (@sku, @name, @price, 0, @reorder, @active);
                        SELECT CAST(SCOPE_IDENTITY() AS int);",
                        new SqlParameter("@sku", code),
                        new SqlParameter("@name", productName.Trim()),
                        Db.MoneyParam("@price", price),
                        new SqlParameter("@reorder", reorderPoint),
                        new SqlParameter("@active", isActive));
                    return ActionResult.Success(string.Format(Messages.ProductSaved, code), Convert.ToInt32(id));
                }

                int rows = Db.Exec(connection, null, @"
                    UPDATE dbo.Product
                    SET Sku = @sku, ProductName = @name, UnitPrice = @price, ReorderPoint = @reorder, IsActive = @active
                    WHERE ProductId = @id",
                    new SqlParameter("@sku", code),
                    new SqlParameter("@name", productName.Trim()),
                    Db.MoneyParam("@price", price),
                    new SqlParameter("@reorder", reorderPoint),
                    new SqlParameter("@active", isActive),
                    new SqlParameter("@id", productId));
                if (rows != 1) return ActionResult.Fail(Messages.NotFound);
                return ActionResult.Success(string.Format(Messages.ProductSaved, code), productId);
            }
        }

        /* On hand moves only through the ledger. A price edit does not rewrite old order lines. */
        public static ActionResult Adjust(int productId, int quantityChange, string note, int userId)
        {
            if (quantityChange == 0) return ActionResult.Fail(Messages.AdjustZero);
            if (string.IsNullOrEmpty(note) || note.Trim().Length == 0) return ActionResult.Fail(Messages.AdjustNote);

            using (SqlConnection connection = Db.Open())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                DataTable locked = Db.Query(connection, transaction,
                    "SELECT Sku, OnHand FROM dbo.Product WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @id",
                    new SqlParameter("@id", productId));
                if (locked.Rows.Count != 1) return ActionResult.Fail(Messages.NotFound);

                string sku = Convert.ToString(locked.Rows[0]["Sku"]);
                int onHand = Convert.ToInt32(locked.Rows[0]["OnHand"]);
                int next = onHand + quantityChange;
                if (next < 0)
                {
                    return ActionResult.Fail(string.Format(Messages.AdjustBelow, sku, onHand));
                }

                Db.Exec(connection, transaction,
                    "UPDATE dbo.Product SET OnHand = @onHand WHERE ProductId = @id",
                    new SqlParameter("@onHand", next),
                    new SqlParameter("@id", productId));
                Db.Exec(connection, transaction, @"
                    INSERT INTO dbo.StockLedger (ProductId, OrderId, QtyChange, Reason, Note, CreatedAt, CreatedBy)
                    VALUES (@productId, NULL, @qty, N'adjust', @note, GETDATE(), @userId)",
                    new SqlParameter("@productId", productId),
                    new SqlParameter("@qty", quantityChange),
                    new SqlParameter("@note", note.Trim()),
                    new SqlParameter("@userId", userId));
                transaction.Commit();
                return ActionResult.Success(string.Format(Messages.AdjustOk, sku, next), next);
            }
        }
    }
}
