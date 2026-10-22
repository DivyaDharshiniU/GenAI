/*
    Gate 1 — the legacy database matches the contract.

    Run after database/reset.ps1, against the same SQL Server:

        sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -d ApexCounter -i 04_check_seed.sql

    SEED: PASS means the rows are the ground truth.
    It does not open the website. You still have to click the pages in FINISH_LINE.md.
*/
USE ApexCounter;
GO

IF OBJECT_ID('tempdb..#miss') IS NOT NULL DROP TABLE #miss;
CREATE TABLE #miss (
    CheckName nvarchar(80) NOT NULL,
    Expected nvarchar(40) NOT NULL,
    Actual nvarchar(40) NOT NULL
);

INSERT INTO #miss
SELECT N'open orders', N'2', CAST(COUNT(*) AS nvarchar(40))
FROM dbo.SalesOrder WHERE Status = N'Open'
HAVING COUNT(*) <> 2;

INSERT INTO #miss
SELECT N'active low stock', N'5', CAST(COUNT(*) AS nvarchar(40))
FROM dbo.Product WHERE IsActive = 1 AND OnHand <= ReorderPoint
HAVING COUNT(*) <> 5;

INSERT INTO #miss
SELECT N'invoiced today', N'48.72', CAST(ISNULL(SUM(Total), 0) AS nvarchar(40))
FROM dbo.SalesOrder
WHERE Status = N'Invoiced' AND CONVERT(date, InvoicedAt) = CONVERT(date, GETDATE())
HAVING ISNULL(SUM(Total), 0) <> 48.72;

INSERT INTO #miss
SELECT N'active customers', N'6', CAST(COUNT(*) AS nvarchar(40))
FROM dbo.Customer WHERE IsActive = 1
HAVING COUNT(*) <> 6;

INSERT INTO #miss
SELECT N'WR-10 on hand', N'3', CAST(OnHand AS nvarchar(40))
FROM dbo.Product WHERE Sku = N'WR-10' AND OnHand <> 3;

INSERT INTO #miss
SELECT N'LT-LED on hand', N'9', CAST(OnHand AS nvarchar(40))
FROM dbo.Product WHERE Sku = N'LT-LED' AND OnHand <> 9;

INSERT INTO #miss
SELECT N'AC-0006 status', N'Open', Status
FROM dbo.SalesOrder WHERE OrderNumber = N'AC-0006' AND Status <> N'Open';

INSERT INTO #miss
SELECT N'AC-0006 total', N'243.39', CAST(Total AS nvarchar(40))
FROM dbo.SalesOrder WHERE OrderNumber = N'AC-0006' AND Total <> 243.39;

INSERT INTO #miss
SELECT N'AC-0003 status', N'Open', Status
FROM dbo.SalesOrder WHERE OrderNumber = N'AC-0003' AND Status <> N'Open';

INSERT INTO #miss
SELECT N'DISC-1 inactive', N'0', CAST(IsActive AS nvarchar(40))
FROM dbo.Product WHERE Sku = N'DISC-1' AND IsActive <> 0;

IF NOT EXISTS (SELECT 1 FROM dbo.Product WHERE Sku = N'WR-10')
    INSERT INTO #miss VALUES (N'WR-10', N'present', N'missing');
IF NOT EXISTS (SELECT 1 FROM dbo.SalesOrder WHERE OrderNumber = N'AC-0006')
    INSERT INTO #miss VALUES (N'AC-0006', N'present', N'missing');

IF EXISTS (SELECT 1 FROM #miss)
BEGIN
    SELECT CheckName, Expected, Actual FROM #miss ORDER BY CheckName;
    RAISERROR('SEED: FAIL. The legacy database is not the ground truth in GROUND_TRUTH.md.', 16, 1);
END
ELSE
    SELECT N'SEED: PASS' AS Result;
GO
