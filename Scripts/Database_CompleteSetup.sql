-- ============================================
-- PhanMemInAnERP - Database Setup Script (Complete)
-- File: Database_CompleteSetup.sql
-- Description: Script tổng hợp đầy đủ cho toàn bộ hệ thống
-- Bao gồm: Hoá đơn, Thu tiền, Nhập kho SX, Xuất kho bán hàng
-- ============================================

PRINT '=== PHAN MEM IN AN ERP - DATABASE SETUP ===';
PRINT 'Thời gian: ' + CONVERT(NVARCHAR, GETDATE(), 120);
GO

-- =============================================
-- PHẦN 1: CẬP NHẬT SALESORDERS (TK_No, TK_Co)
-- =============================================
PRINT '--- Phần 1: Cập nhật SalesOrders ---';

IF COL_LENGTH('SalesOrders', 'TK_No') IS NULL
BEGIN
    ALTER TABLE SalesOrders ADD TK_No NVARCHAR(20) NULL;
    PRINT '✓ Đã thêm TK_No';
END

IF COL_LENGTH('SalesOrders', 'TK_Co') IS NULL
BEGIN
    ALTER TABLE SalesOrders ADD TK_Co NVARCHAR(20) NULL;
    PRINT '✓ Đã thêm TK_Co';
END
GO

-- =============================================
-- PHẦN 2: EXPORTTRANSACTIONS - MaterialId nullable
-- =============================================
PRINT '--- Phần 2: Sửa ExportTransactions ---';

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = 'MaterialId'
    AND Object_ID = Object_ID('ExportTransactions')
    AND is_nullable = 0
)
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ExportTransactions_Materials_MaterialId')
        ALTER TABLE ExportTransactions DROP CONSTRAINT FK_ExportTransactions_Materials_MaterialId;

    ALTER TABLE ExportTransactions ALTER COLUMN MaterialId INT NULL;

    ALTER TABLE ExportTransactions
    ADD CONSTRAINT FK_ExportTransactions_Materials_MaterialId
    FOREIGN KEY (MaterialId) REFERENCES Materials(Id);
    PRINT '✓ Đã sửa MaterialId thành nullable';
END
GO

-- =============================================
-- PHẦN 3: PRODUCTIONIMPORTS
-- =============================================
PRINT '--- Phần 3: Tạo ProductionImports ---';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionImports')
BEGIN
    CREATE TABLE ProductionImports (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TicketNo NVARCHAR(50) NOT NULL,
        ImportDate DATETIME2 NOT NULL,
        ProductionOrderId INT NOT NULL,
        SalesOrderId INT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        Unit NVARCHAR(50) NOT NULL,
        UnitPrice FLOAT NOT NULL,
        TotalPrice FLOAT NOT NULL,
        UserId INT NOT NULL,
        Notes NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Chưa xuất',
        ExportDate DATETIME2 NULL,
        ExportTicketNo NVARCHAR(50) NOT NULL DEFAULT ''
    );

    CREATE INDEX IX_ProductionImports_ProductionOrderId ON ProductionImports(ProductionOrderId);
    CREATE INDEX IX_ProductionImports_SalesOrderId ON ProductionImports(SalesOrderId);
    CREATE INDEX IX_ProductionImports_UserId ON ProductionImports(UserId);
    CREATE INDEX IX_ProductionImports_Status ON ProductionImports(Status);
    PRINT '✓ Đã tạo ProductionImports';
END
GO

-- =============================================
-- PHẦN 4: INVOICES (HÓA ĐƠN)
-- =============================================
PRINT '--- Phần 4: Tạo Invoices ---';

-- Xóa bảng Invoices cũ nếu tồn tại (để tạo mới hoàn toàn)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    -- Xóa constraints
    DECLARE @sql NVARCHAR(MAX) = N'';
    SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
        + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID('Invoices');

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_Invoices_InvoiceId')
        SET @sql += N'ALTER TABLE Payments DROP CONSTRAINT FK_Payments_Invoices_InvoiceId;' + CHAR(13);

    EXEC sp_executesql @sql;

    DROP TABLE IF EXISTS Invoices;
    PRINT 'Đã xóa Invoices cũ';
END

CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNo NVARCHAR(100) NOT NULL,
    InvoiceDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    RefOrderNo NVARCHAR(100) NOT NULL DEFAULT '',
    SalesOrderId INT NOT NULL,
    CustomerId INT NULL,
    CustomerName NVARCHAR(MAX) NOT NULL DEFAULT '',
    Phone NVARCHAR(MAX) NOT NULL DEFAULT '',
    Address NVARCHAR(MAX) NOT NULL DEFAULT '',
    TaxCode NVARCHAR(MAX) NOT NULL DEFAULT '',
    TotalAmount FLOAT NOT NULL DEFAULT 0,
    SubTotal FLOAT NOT NULL DEFAULT 0,
    VATPercent FLOAT NOT NULL DEFAULT 0.08,
    VATAmount FLOAT NOT NULL DEFAULT 0,
    GrandTotal FLOAT NOT NULL DEFAULT 0,
    TK_No NVARCHAR(MAX) NULL,
    TK_Co NVARCHAR(MAX) NULL,
    AccountType NVARCHAR(50) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Chưa thu',
    PaymentDate DATETIME2 NULL,
    Notes NVARCHAR(MAX) NOT NULL DEFAULT '',
    TemplateNo NVARCHAR(50) NOT NULL DEFAULT '1/001',
    Symbol NVARCHAR(50) NOT NULL DEFAULT 'KT/26E',
    UserId INT NOT NULL DEFAULT 1,
    DueDate DATETIME2 NOT NULL DEFAULT DATEADD(DAY, 30, GETDATE())
);

CREATE INDEX IX_Invoices_SalesOrderId ON Invoices(SalesOrderId);
CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
CREATE INDEX IX_Invoices_Status ON Invoices(Status);
CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
CREATE INDEX IX_Invoices_RefOrderNo ON Invoices(RefOrderNo);

ALTER TABLE Invoices
ADD CONSTRAINT FK_Invoices_SalesOrders_SalesOrderId
FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id) ON DELETE NO ACTION;

ALTER TABLE Invoices
ADD CONSTRAINT FK_Invoices_Customers_CustomerId
FOREIGN KEY (CustomerId) REFERENCES Customers(Id);

PRINT '✓ Đã tạo Invoices';
GO

-- =============================================
-- PHẦN 5: CẬP NHẬT PAYMENTS
-- =============================================
PRINT '--- Phần 5: Cập nhật Payments ---';

IF COL_LENGTH('Payments', 'AccountType') IS NULL
BEGIN
    ALTER TABLE Payments ADD AccountType NVARCHAR(50) NULL;
    PRINT '✓ Đã thêm AccountType';
END

IF COL_LENGTH('Payments', 'InvoiceId') IS NULL
BEGIN
    ALTER TABLE Payments ADD InvoiceId INT NULL;
    PRINT '✓ Đã thêm InvoiceId';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_Invoices_InvoiceId')
BEGIN
    ALTER TABLE Payments
    ADD CONSTRAINT FK_Payments_Invoices_InvoiceId
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id);
    PRINT '✓ Đã tạo FK Payments_Invoices';
END

CREATE INDEX IX_Payments_InvoiceId ON Payments(InvoiceId);
CREATE INDEX IX_Payments_RefInvoiceNo ON Payments(RefInvoiceNo);
GO

-- =============================================
-- PHẦN 6: PRODUCTIONIMPORTS FOREIGN KEYS
-- =============================================
PRINT '--- Phần 6: ProductionImports FK ---';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionImports')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ProductionImports_ProductionOrders')
    BEGIN
        ALTER TABLE ProductionImports
        ADD CONSTRAINT FK_ProductionImports_ProductionOrders
        FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(Id);
        PRINT '✓ Đã tạo FK ProductionImports_ProductionOrders';
    END

    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ProductionImports_SalesOrders')
    BEGIN
        ALTER TABLE ProductionImports
        ADD CONSTRAINT FK_ProductionImports_SalesOrders
        FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id);
        PRINT '✓ Đã tạo FK ProductionImports_SalesOrders';
    END
END
GO

-- =============================================
-- PHẦN 7: INDEX TỐI ƯU
-- =============================================
PRINT '--- Phần 7: Tạo indexes tối ưu ---';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_CustomerName')
BEGIN
    -- Lưu ý: Không tạo index trên NVARCHAR(MAX), sử dụng full-text nếu cần
    PRINT 'Bỏ qua index trên CustomerName (NVARCHAR(MAX))';
END

PRINT '✓ Tất cả indexes đã được tạo';
GO

PRINT '=== HOÀN TẤT SETUP DATABASE ===';
PRINT 'Tổng số bảng đã cập nhật: SalesOrders, ExportTransactions, ProductionImports, Invoices, Payments';
PRINT 'Có thể bắt đầu sử dụng phần mềm!';
GO
