/*
    Apex Counter database.

    This is the contract both applications share:
      ApexCounter         the legacy Web Forms app (ground truth)
      ApexCounter_Modern  a copy for the .NET 8 app (created by 03_modern_copy.sql)

    Do not rename tables or columns. The modernized app has to keep working
    against this shape. reset.ps1 drops and recreates ApexCounter, then refreshes
    the modern copy.
*/
IF DB_ID(N'ApexCounter') IS NOT NULL
BEGIN
    ALTER DATABASE ApexCounter SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ApexCounter;
END
GO
CREATE DATABASE ApexCounter;
GO
USE ApexCounter;
GO

CREATE TABLE dbo.AppUser (
    UserId        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUser PRIMARY KEY,
    UserName      nvarchar(40)  NOT NULL CONSTRAINT UQ_AppUser_UserName UNIQUE,
    DisplayName   nvarchar(80)  NOT NULL,
    PasswordSalt  nvarchar(20)  NOT NULL,
    PasswordHash  char(40)      NOT NULL,
    IsActive      bit           NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1)
);
GO

CREATE TABLE dbo.Customer (
    CustomerId    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY,
    AccountCode   nvarchar(20)  NOT NULL CONSTRAINT UQ_Customer_AccountCode UNIQUE,
    CompanyName   nvarchar(120) NOT NULL,
    ContactName   nvarchar(80)  NOT NULL,
    Phone         nvarchar(30)  NOT NULL,
    City          nvarchar(60)  NOT NULL,
    IsActive      bit           NOT NULL CONSTRAINT DF_Customer_IsActive DEFAULT (1)
);
GO

CREATE TABLE dbo.Product (
    ProductId     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Product PRIMARY KEY,
    Sku           nvarchar(20)  NOT NULL CONSTRAINT UQ_Product_Sku UNIQUE,
    ProductName   nvarchar(120) NOT NULL,
    UnitPrice     decimal(18,2) NOT NULL,
    OnHand        int           NOT NULL,
    ReorderPoint  int           NOT NULL,
    IsActive      bit           NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT (1),
    CONSTRAINT CK_Product_OnHand CHECK (OnHand >= 0),
    CONSTRAINT CK_Product_Price CHECK (UnitPrice >= 0),
    CONSTRAINT CK_Product_Reorder CHECK (ReorderPoint >= 0)
);
GO

CREATE TABLE dbo.SalesOrder (
    OrderId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesOrder PRIMARY KEY,
    OrderNumber   nvarchar(20)  NOT NULL CONSTRAINT UQ_SalesOrder_OrderNumber UNIQUE,
    CustomerId    int           NOT NULL CONSTRAINT FK_SalesOrder_Customer REFERENCES dbo.Customer (CustomerId),
    Status        nvarchar(20)  NOT NULL,
    CreatedAt     datetime      NOT NULL,
    PickedAt      datetime      NULL,
    InvoicedAt    datetime      NULL,
    Subtotal      decimal(18,2) NOT NULL,
    Tax           decimal(18,2) NOT NULL,
    Total         decimal(18,2) NOT NULL,
    CreatedBy     int           NOT NULL CONSTRAINT FK_SalesOrder_User REFERENCES dbo.AppUser (UserId),
    CONSTRAINT CK_SalesOrder_Status CHECK (Status IN (N'Open', N'Picked', N'Invoiced', N'Cancelled')),
    CONSTRAINT CK_SalesOrder_Money CHECK (Subtotal >= 0 AND Tax >= 0 AND Total >= 0)
);
GO

CREATE TABLE dbo.SalesOrderLine (
    LineId        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesOrderLine PRIMARY KEY,
    OrderId       int           NOT NULL CONSTRAINT FK_Line_Order REFERENCES dbo.SalesOrder (OrderId),
    ProductId     int           NOT NULL CONSTRAINT FK_Line_Product REFERENCES dbo.Product (ProductId),
    Sku           nvarchar(20)  NOT NULL,
    ProductName   nvarchar(120) NOT NULL,
    Quantity      int           NOT NULL,
    UnitPrice     decimal(18,2) NOT NULL,
    LineTotal     decimal(18,2) NOT NULL,
    CONSTRAINT UQ_Line_OrderProduct UNIQUE (OrderId, ProductId),
    CONSTRAINT CK_Line_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_Line_Money CHECK (UnitPrice >= 0 AND LineTotal >= 0)
);
GO

CREATE TABLE dbo.StockLedger (
    LedgerId      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockLedger PRIMARY KEY,
    ProductId     int           NOT NULL CONSTRAINT FK_Ledger_Product REFERENCES dbo.Product (ProductId),
    OrderId       int           NULL CONSTRAINT FK_Ledger_Order REFERENCES dbo.SalesOrder (OrderId),
    QtyChange     int           NOT NULL,
    Reason        nvarchar(40)  NOT NULL,
    Note          nvarchar(200) NULL,
    CreatedAt     datetime      NOT NULL,
    CreatedBy     int           NOT NULL CONSTRAINT FK_Ledger_User REFERENCES dbo.AppUser (UserId),
    CONSTRAINT CK_Ledger_Reason CHECK (Reason IN (N'receipt', N'pick', N'cancel-return', N'adjust'))
);
GO
