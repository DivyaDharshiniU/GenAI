/*
    Finish line for the databases.

    After the same writes on both apps (FINISH_LINE.md):

        sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d master -i 05_score_parity.sql

    PARITY: PASS means ApexCounter and ApexCounter_Modern agree on
    customers, products, orders, lines, stock totals, and sign-ins.
    Clock times are ignored. Wording on screen is scored in FINISH_LINE.md.
*/
IF DB_ID(N'ApexCounter') IS NULL OR DB_ID(N'ApexCounter_Modern') IS NULL
BEGIN
    RAISERROR('Both ApexCounter and ApexCounter_Modern must exist. Run reset.ps1.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID('tempdb..#diff') IS NOT NULL DROP TABLE #diff;
CREATE TABLE #diff (
    CheckName nvarchar(40) NOT NULL,
    KeyValue nvarchar(80) NOT NULL,
    LegacyValue nvarchar(200) NULL,
    ModernValue nvarchar(200) NULL
);

INSERT INTO #diff
SELECT N'customer', ISNULL(l.AccountCode, m.AccountCode),
       l.CompanyName + N'|' + l.ContactName + N'|' + l.Phone + N'|' + l.City + N'|' + CAST(l.IsActive AS nvarchar(1)),
       m.CompanyName + N'|' + m.ContactName + N'|' + m.Phone + N'|' + m.City + N'|' + CAST(m.IsActive AS nvarchar(1))
FROM ApexCounter.dbo.Customer l
FULL OUTER JOIN ApexCounter_Modern.dbo.Customer m ON m.AccountCode = l.AccountCode
WHERE l.AccountCode IS NULL OR m.AccountCode IS NULL
   OR l.CompanyName <> m.CompanyName OR l.ContactName <> m.ContactName
   OR l.Phone <> m.Phone OR l.City <> m.City OR l.IsActive <> m.IsActive;

INSERT INTO #diff
SELECT N'product', ISNULL(l.Sku, m.Sku),
       l.ProductName + N'|' + CAST(l.UnitPrice AS nvarchar(20)) + N'|' + CAST(l.OnHand AS nvarchar(20))
         + N'|' + CAST(l.ReorderPoint AS nvarchar(20)) + N'|' + CAST(l.IsActive AS nvarchar(1)),
       m.ProductName + N'|' + CAST(m.UnitPrice AS nvarchar(20)) + N'|' + CAST(m.OnHand AS nvarchar(20))
         + N'|' + CAST(m.ReorderPoint AS nvarchar(20)) + N'|' + CAST(m.IsActive AS nvarchar(1))
FROM ApexCounter.dbo.Product l
FULL OUTER JOIN ApexCounter_Modern.dbo.Product m ON m.Sku = l.Sku
WHERE l.Sku IS NULL OR m.Sku IS NULL
   OR l.ProductName <> m.ProductName OR l.UnitPrice <> m.UnitPrice
   OR l.OnHand <> m.OnHand OR l.ReorderPoint <> m.ReorderPoint OR l.IsActive <> m.IsActive;

INSERT INTO #diff
SELECT N'order', ISNULL(l.OrderNumber, m.OrderNumber),
       lc.AccountCode + N'|' + l.Status + N'|' + CAST(l.Subtotal AS nvarchar(20)) + N'|' + CAST(l.Tax AS nvarchar(20)) + N'|' + CAST(l.Total AS nvarchar(20)),
       mc.AccountCode + N'|' + m.Status + N'|' + CAST(m.Subtotal AS nvarchar(20)) + N'|' + CAST(m.Tax AS nvarchar(20)) + N'|' + CAST(m.Total AS nvarchar(20))
FROM ApexCounter.dbo.SalesOrder l
INNER JOIN ApexCounter.dbo.Customer lc ON lc.CustomerId = l.CustomerId
FULL OUTER JOIN ApexCounter_Modern.dbo.SalesOrder m ON m.OrderNumber = l.OrderNumber
LEFT JOIN ApexCounter_Modern.dbo.Customer mc ON mc.CustomerId = m.CustomerId
WHERE l.OrderNumber IS NULL OR m.OrderNumber IS NULL
   OR lc.AccountCode <> mc.AccountCode OR l.Status <> m.Status
   OR l.Subtotal <> m.Subtotal OR l.Tax <> m.Tax OR l.Total <> m.Total;

INSERT INTO #diff
SELECT N'line',
       ISNULL(lo.OrderNumber, mo.OrderNumber) + N'/' + ISNULL(ll.Sku, ml.Sku),
       CAST(ll.Quantity AS nvarchar(20)) + N'|' + CAST(ll.UnitPrice AS nvarchar(20)) + N'|' + CAST(ll.LineTotal AS nvarchar(20)) + N'|' + ll.ProductName,
       CAST(ml.Quantity AS nvarchar(20)) + N'|' + CAST(ml.UnitPrice AS nvarchar(20)) + N'|' + CAST(ml.LineTotal AS nvarchar(20)) + N'|' + ml.ProductName
FROM ApexCounter.dbo.SalesOrderLine ll
INNER JOIN ApexCounter.dbo.SalesOrder lo ON lo.OrderId = ll.OrderId
FULL OUTER JOIN (
    SELECT o.OrderNumber, line.Sku, line.ProductName, line.Quantity, line.UnitPrice, line.LineTotal
    FROM ApexCounter_Modern.dbo.SalesOrderLine line
    INNER JOIN ApexCounter_Modern.dbo.SalesOrder o ON o.OrderId = line.OrderId
) ml ON ml.OrderNumber = lo.OrderNumber AND ml.Sku = ll.Sku
LEFT JOIN ApexCounter_Modern.dbo.SalesOrder mo ON mo.OrderNumber = ml.OrderNumber
WHERE lo.OrderNumber IS NULL OR ml.OrderNumber IS NULL
   OR ll.Quantity <> ml.Quantity OR ll.UnitPrice <> ml.UnitPrice
   OR ll.LineTotal <> ml.LineTotal OR ll.ProductName <> ml.ProductName;

INSERT INTO #diff
SELECT N'stock',
       ISNULL(lg.Sku, mg.Sku) + N'/' + ISNULL(lg.OrderNumber, mg.OrderNumber) + N'/' + ISNULL(lg.Reason, mg.Reason),
       CAST(lg.QtyChange AS nvarchar(20)),
       CAST(mg.QtyChange AS nvarchar(20))
FROM (
    SELECT p.Sku, ISNULL(o.OrderNumber, N'-') AS OrderNumber, l.Reason, SUM(l.QtyChange) AS QtyChange
    FROM ApexCounter.dbo.StockLedger l
    INNER JOIN ApexCounter.dbo.Product p ON p.ProductId = l.ProductId
    LEFT JOIN ApexCounter.dbo.SalesOrder o ON o.OrderId = l.OrderId
    GROUP BY p.Sku, ISNULL(o.OrderNumber, N'-'), l.Reason
) lg
FULL OUTER JOIN (
    SELECT p.Sku, ISNULL(o.OrderNumber, N'-') AS OrderNumber, l.Reason, SUM(l.QtyChange) AS QtyChange
    FROM ApexCounter_Modern.dbo.StockLedger l
    INNER JOIN ApexCounter_Modern.dbo.Product p ON p.ProductId = l.ProductId
    LEFT JOIN ApexCounter_Modern.dbo.SalesOrder o ON o.OrderId = l.OrderId
    GROUP BY p.Sku, ISNULL(o.OrderNumber, N'-'), l.Reason
) mg ON mg.Sku = lg.Sku AND mg.OrderNumber = lg.OrderNumber AND mg.Reason = lg.Reason
WHERE lg.Sku IS NULL OR mg.Sku IS NULL OR lg.QtyChange <> mg.QtyChange;

INSERT INTO #diff
SELECT N'user', ISNULL(l.UserName, m.UserName),
       l.DisplayName + N'|' + RTRIM(l.PasswordHash) + N'|' + CAST(l.IsActive AS nvarchar(1)),
       m.DisplayName + N'|' + RTRIM(m.PasswordHash) + N'|' + CAST(m.IsActive AS nvarchar(1))
FROM ApexCounter.dbo.AppUser l
FULL OUTER JOIN ApexCounter_Modern.dbo.AppUser m ON m.UserName = l.UserName
WHERE l.UserName IS NULL OR m.UserName IS NULL
   OR l.DisplayName <> m.DisplayName
   OR RTRIM(l.PasswordHash) <> RTRIM(m.PasswordHash)
   OR l.IsActive <> m.IsActive;

IF EXISTS (SELECT 1 FROM #diff)
BEGIN
    SELECT CheckName, KeyValue, LegacyValue, ModernValue FROM #diff ORDER BY CheckName, KeyValue;
    RAISERROR('PARITY: FAIL. The .NET 8 database does not match the .NET Framework 4.0 database.', 16, 1);
END
ELSE
    SELECT N'PARITY: PASS' AS Result, N'Finish line. The two databases agree.' AS Meaning;
GO
