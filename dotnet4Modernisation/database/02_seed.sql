/*
    Fresh desk for the day you run this script.

    Both sign-ins use the password counter2011.
    The hash is SHA-1 of (salt + password) as lowercase hex, UTF-8.
    PasswordHasher in the web app uses that same rule.

    AC-0005 is invoiced at GETDATE(), so the desk tile "Invoiced today"
    is non-zero only on the calendar day you seed. Re-run reset.ps1
    the morning of a demo or a parity run.
*/
USE ApexCounter;
GO

SET IDENTITY_INSERT dbo.AppUser ON;
INSERT INTO dbo.AppUser (UserId, UserName, DisplayName, PasswordSalt, PasswordHash, IsActive) VALUES
    (1, N'clerk',      N'Mara Ellison', N'C1erkSalt04', '4ba7a4f99a1b499fa758565e2876fd3dc188b775', 1),
    (2, N'supervisor', N'Owen Briggs',  N'Sup3rSalt04', 'acdbf666adb61a5dd461fea1f5bd5a84102df3a1', 1);
SET IDENTITY_INSERT dbo.AppUser OFF;
GO

SET IDENTITY_INSERT dbo.Customer ON;
INSERT INTO dbo.Customer (CustomerId, AccountCode, CompanyName, ContactName, Phone, City, IsActive) VALUES
    (1, N'ACME-01', N'Acme Switchgear',       N'Lina Shah',       N'04-311-1001', N'Dubai',     1),
    (2, N'HARB-14', N'Harbor Lights LLC',     N'Omar Nasser',     N'06-522-4410', N'Sharjah',   1),
    (3, N'NILE-07', N'Nile Cold Storage',     N'Priya Raman',     N'02-667-9088', N'Abu Dhabi', 1),
    (4, N'QASR-02', N'Qasr Mall Services',    N'Helen Cho',       N'04-228-7731', N'Dubai',     1),
    (5, N'DUNE-19', N'Dune Fabrication',      N'Yusuf Haddad',    N'06-741-2204', N'Ajman',     1),
    (6, N'PALM-03', N'Palm Clinic Fit-Out',   N'Sara Ibrahim',    N'04-390-1182', N'Dubai',     1),
    (7, N'OLD-99',  N'Retired Yard Co.',      N'Closed Account',  N'04-000-0099', N'Dubai',     0);
SET IDENTITY_INSERT dbo.Customer OFF;
GO

SET IDENTITY_INSERT dbo.Product ON;
INSERT INTO dbo.Product (ProductId, Sku, ProductName, UnitPrice, OnHand, ReorderPoint, IsActive) VALUES
    (1,  N'CB-100',  N'Copper bus bar 100A',        42.50,  12, 10, 1),
    (2,  N'CB-250',  N'Copper bus bar 250A',        88.00,   4,  6, 1),
    (3,  N'BR-20',   N'Breaker 20A',                18.75,  40, 15, 1),
    (4,  N'BR-40',   N'Breaker 40A',                27.40,   8, 12, 1),
    (5,  N'WR-12',   N'THHN wire 12 AWG (100ft)',   36.00,  22, 10, 1),
    (6,  N'WR-10',   N'THHN wire 10 AWG (100ft)',   52.00,   3,  8, 1),
    (7,  N'BX-4',    N'Steel box 4 inch',            6.25,  80, 20, 1),
    (8,  N'LT-LED',  N'LED high bay',              145.00,   9,  4, 1),
    (9,  N'CT-30',   N'Conduit 3/4 in (10ft)',      11.90,  15, 15, 1),
    (10, N'TM-1',    N'Torque marker set',           9.50,   0,  5, 1),
    (11, N'DISC-1',  N'Discontinued fuse link',      1.00,   0,  2, 0);
SET IDENTITY_INSERT dbo.Product OFF;
GO

DECLARE @now datetime = GETDATE();

SET IDENTITY_INSERT dbo.SalesOrder ON;
INSERT INTO dbo.SalesOrder
    (OrderId, OrderNumber, CustomerId, Status, CreatedAt, PickedAt, InvoicedAt, Subtotal, Tax, Total, CreatedBy)
VALUES
    (1, N'AC-0001', 1, N'Invoiced',  DATEADD(day, -12, @now), DATEADD(day, -12, @now), DATEADD(day, -11, @now),  73.50,  3.68,  77.18, 1),
    (2, N'AC-0002', 2, N'Picked',    DATEADD(day,  -2, @now), DATEADD(day,  -1, @now), NULL,                       88.00,  4.40,  92.40, 1),
    (3, N'AC-0003', 3, N'Open',      DATEADD(day,  -1, @now), NULL,                    NULL,                      290.00, 14.50, 304.50, 1),
    (4, N'AC-0004', 1, N'Cancelled', DATEADD(day, -20, @now), NULL,                    NULL,                       31.25,  1.56,  32.81, 1),
    (5, N'AC-0005', 4, N'Invoiced',  @now,                    @now,                    @now,                       46.40,  2.32,  48.72, 1),
    (6, N'AC-0006', 5, N'Open',      @now,                    NULL,                    NULL,                      231.80, 11.59, 243.39, 1);
SET IDENTITY_INSERT dbo.SalesOrder OFF;
GO

INSERT INTO dbo.SalesOrderLine
    (OrderId, ProductId, Sku, ProductName, Quantity, UnitPrice, LineTotal)
VALUES
    (1,  3, N'BR-20',  N'Breaker 20A',              2,  18.75,  37.50),
    (1,  5, N'WR-12',  N'THHN wire 12 AWG (100ft)', 1,  36.00,  36.00),
    (2,  2, N'CB-250', N'Copper bus bar 250A',      1,  88.00,  88.00),
    (3,  8, N'LT-LED', N'LED high bay',             2, 145.00, 290.00),
    (4,  7, N'BX-4',   N'Steel box 4 inch',         5,   6.25,  31.25),
    (5,  4, N'BR-40',  N'Breaker 40A',              1,  27.40,  27.40),
    (5, 10, N'TM-1',   N'Torque marker set',        2,   9.50,  19.00),
    (6,  6, N'WR-10',  N'THHN wire 10 AWG (100ft)', 4,  52.00, 208.00),
    (6,  9, N'CT-30',  N'Conduit 3/4 in (10ft)',    2,  11.90,  23.80);
GO

DECLARE @now datetime = GETDATE();

/* Opening receipts, then the picks that already happened. OnHand is the sum. */
INSERT INTO dbo.StockLedger (ProductId, OrderId, QtyChange, Reason, Note, CreatedAt, CreatedBy) VALUES
    (1,  NULL,  12, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (2,  NULL,   5, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (3,  NULL,  42, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (4,  NULL,   9, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (5,  NULL,  23, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (6,  NULL,   3, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (7,  NULL,  80, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (8,  NULL,   9, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (9,  NULL,  15, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (10, NULL,   2, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (11, NULL,   2, N'receipt', N'opening count', DATEADD(day, -30, @now), 2),
    (11, NULL,  -2, N'adjust',  N'written off',   DATEADD(day, -29, @now), 2),
    (3,  1,     -2, N'pick',    NULL,             DATEADD(day, -12, @now), 1),
    (5,  1,     -1, N'pick',    NULL,             DATEADD(day, -12, @now), 1),
    (2,  2,     -1, N'pick',    NULL,             DATEADD(day,  -1, @now), 1),
    (4,  5,     -1, N'pick',    NULL,             @now,                    1),
    (10, 5,     -2, N'pick',    NULL,             @now,                    1);
GO
