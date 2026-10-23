using System;
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
    public class CatalogData
    {
        private readonly Db _db;

        public CatalogData(Db db)
        {
            _db = db;
        }

        public DataTable List(string term, bool lowOnly)
        {
            string lowClause = lowOnly ? " AND OnHand <= ReorderPoint" : "";
            return _db.Query(@"
                SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint,
                       CASE WHEN IsActive = 1 THEN N'Yes' ELSE N'No' END AS ActiveText
                FROM dbo.Product
                WHERE (Sku LIKE @term OR ProductName LIKE @term)" + lowClause + @"
                ORDER BY Sku", new SqlParameter("@term", _db.Like(term)));
        }

        public DataTable LowStockReport()
        {
            return _db.Query(@"
                SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint
                FROM dbo.Product
                WHERE IsActive = 1 AND OnHand <= ReorderPoint
                ORDER BY Sku");
        }

        public DataTable ActiveForOrder()
        {
            return _db.Query(@"
                SELECT ProductId, Sku + N' - ' + ProductName AS Label
                FROM dbo.Product
                WHERE IsActive = 1
                ORDER BY Sku");
        }

        public DataRow Find(int productId)
        {
            DataTable table = _db.Query("SELECT ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint, IsActive FROM dbo.Product WHERE ProductId = @id", new SqlParameter("@id", productId));
            if (table.Rows.Count != 1)
                return null;
            return table.Rows[0];
        }

        public ActionResult Save(int productId, string sku, string productName, decimal unitPrice, int reorderPoint, bool isActive)
        {
            string code = sku.Trim();
            decimal price = Money.Round(unitPrice);
            using (SqlConnection connection = _db.Open())
            {
                object existing = _db.Scalar(connection, null, "SELECT ProductId FROM dbo.Product WHERE Sku = @sku AND ProductId <> @id", new SqlParameter("@sku", code), new SqlParameter("@id", productId));
                if (existing != null && existing != DBNull.Value)
                {
                    return ActionResult.Fail(string.Format(Messages.DuplicateSku, code));
                }

                if (productId == 0)
                {
                    object id = _db.Scalar(connection, null, @"
                        INSERT INTO dbo.Product (Sku, ProductName, UnitPrice, OnHand, ReorderPoint, IsActive)
                        VALUES (@sku, @name, @price, 0, @reorder, @active);
                        SELECT CAST(SCOPE_IDENTITY() AS int);", new SqlParameter("@sku", code), new SqlParameter("@name", productName.Trim()), _db.MoneyParam("@price", price), new SqlParameter("@reorder", reorderPoint), new SqlParameter("@active", isActive));
                    return ActionResult.Success(string.Format(Messages.ProductSaved, code), Convert.ToInt32(id));
                }

                int rows = _db.Exec(connection, null, @"
                    UPDATE dbo.Product
                    SET Sku = @sku, ProductName = @name, UnitPrice = @price, ReorderPoint = @reorder, IsActive = @active
                    WHERE ProductId = @id", new SqlParameter("@sku", code), new SqlParameter("@name", productName.Trim()), _db.MoneyParam("@price", price), new SqlParameter("@reorder", reorderPoint), new SqlParameter("@active", isActive), new SqlParameter("@id", productId));
                if (rows != 1)
                    return ActionResult.Fail(Messages.NotFound);
                return ActionResult.Success(string.Format(Messages.ProductSaved, code), productId);
            }
        }

        /* On hand moves only through the ledger. A price edit does not rewrite old order lines. */
        public ActionResult Adjust(int productId, int quantityChange, string note, int userId)
        {
            if (quantityChange == 0)
                return ActionResult.Fail(Messages.AdjustZero);
            if (string.IsNullOrEmpty(note) || note.Trim().Length == 0)
                return ActionResult.Fail(Messages.AdjustNote);
            using (SqlConnection connection = _db.Open())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                DataTable locked = _db.Query(connection, transaction, "SELECT Sku, OnHand FROM dbo.Product WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @id", new SqlParameter("@id", productId));
                if (locked.Rows.Count != 1)
                    return ActionResult.Fail(Messages.NotFound);
                string sku = Convert.ToString(locked.Rows[0]["Sku"]);
                int onHand = Convert.ToInt32(locked.Rows[0]["OnHand"]);
                int next = onHand + quantityChange;
                if (next < 0)
                {
                    return ActionResult.Fail(string.Format(Messages.AdjustBelow, sku, onHand));
                }

                _db.Exec(connection, transaction, "UPDATE dbo.Product SET OnHand = @onHand WHERE ProductId = @id", new SqlParameter("@onHand", next), new SqlParameter("@id", productId));
                _db.Exec(connection, transaction, @"
                    INSERT INTO dbo.StockLedger (ProductId, OrderId, QtyChange, Reason, Note, CreatedAt, CreatedBy)
                    VALUES (@productId, NULL, @qty, N'adjust', @note, GETDATE(), @userId)", new SqlParameter("@productId", productId), new SqlParameter("@qty", quantityChange), new SqlParameter("@note", note.Trim()), new SqlParameter("@userId", userId));
                transaction.Commit();
                return ActionResult.Success(string.Format(Messages.AdjustOk, sku, next), next);
            }
        }
    }
}