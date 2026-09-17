-- ============================================
-- PhanMemInAnERP - Database Update Script
-- File: UpdateInvoicesTable.sql
-- Description: Thêm các cột còn thiếu vào bảng Invoices
-- ============================================

PRINT '=== CẬP NHẬT BẢNG Invoices ===';

-- Thêm SalesOrderId
IF COL_LENGTH('Invoices', 'SalesOrderId') IS NULL
BEGIN
    ALTER TABLE Invoices ADD SalesOrderId INT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm SalesOrderId';
END

-- Thêm CustomerId
IF COL_LENGTH('Invoices', 'CustomerId') IS NULL
BEGIN
    ALTER TABLE Invoices ADD CustomerId INT NULL;
    PRINT 'Đã thêm CustomerId';
END

-- Thêm Phone
IF COL_LENGTH('Invoices', 'Phone') IS NULL
BEGIN
    ALTER TABLE Invoices ADD Phone NVARCHAR(MAX) NOT NULL DEFAULT '';
    PRINT 'Đã thêm Phone';
END

-- Thêm Address
IF COL_LENGTH('Invoices', 'Address') IS NULL
BEGIN
    ALTER TABLE Invoices ADD Address NVARCHAR(MAX) NOT NULL DEFAULT '';
    PRINT 'Đã thêm Address';
END

-- Thêm TK_No
IF COL_LENGTH('Invoices', 'TK_No') IS NULL
BEGIN
    ALTER TABLE Invoices ADD TK_No NVARCHAR(MAX) NULL;
    PRINT 'Đã thêm TK_No';
END

-- Thêm TK_Co
IF COL_LENGTH('Invoices', 'TK_Co') IS NULL
BEGIN
    ALTER TABLE Invoices ADD TK_Co NVARCHAR(MAX) NULL;
    PRINT 'Đã thêm TK_Co';
END

-- Thêm AccountType
IF COL_LENGTH('Invoices', 'AccountType') IS NULL
BEGIN
    ALTER TABLE Invoices ADD AccountType NVARCHAR(50) NULL;
    PRINT 'Đã thêm AccountType';
END

-- Thêm Status
IF COL_LENGTH('Invoices', 'Status') IS NULL
BEGIN
    ALTER TABLE Invoices ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Chưa thu';
    PRINT 'Đã thêm Status';
END

-- Thêm PaymentDate
IF COL_LENGTH('Invoices', 'PaymentDate') IS NULL
BEGIN
    ALTER TABLE Invoices ADD PaymentDate DATETIME2 NULL;
    PRINT 'Đã thêm PaymentDate';
END

-- Thêm Notes
IF COL_LENGTH('Invoices', 'Notes') IS NULL
BEGIN
    ALTER TABLE Invoices ADD Notes NVARCHAR(MAX) NOT NULL DEFAULT '';
    PRINT 'Đã thêm Notes';
END

-- Thêm GrandTotal
IF COL_LENGTH('Invoices', 'GrandTotal') IS NULL
BEGIN
    ALTER TABLE Invoices ADD GrandTotal FLOAT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm GrandTotal';
END

-- Thêm VATAmount
IF COL_LENGTH('Invoices', 'VATAmount') IS NULL
BEGIN
    ALTER TABLE Invoices ADD VATAmount FLOAT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm VATAmount';
END

-- Sửa Symbol thành nullable nếu cần
IF EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = 'Symbol' AND Object_ID = Object_ID('Invoices') AND is_nullable = 0
)
BEGIN
    ALTER TABLE Invoices ALTER COLUMN Symbol NVARCHAR(50) NULL;
    PRINT 'Đã sửa Symbol thành nullable';
END

-- =============================================
-- TẠO INDEX
-- =============================================
PRINT '=== TẠO INDEX ===';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_SalesOrderId')
BEGIN
    CREATE INDEX IX_Invoices_SalesOrderId ON Invoices(SalesOrderId);
    PRINT 'Đã tạo IX_Invoices_SalesOrderId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_CustomerId')
BEGIN
    CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
    PRINT 'Đã tạo IX_Invoices_CustomerId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_Status')
BEGIN
    CREATE INDEX IX_Invoices_Status ON Invoices(Status);
    PRINT 'Đã tạo IX_Invoices_Status';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_InvoiceDate')
BEGIN
    CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
    PRINT 'Đã tạo IX_Invoices_InvoiceDate';
END

-- =============================================
-- TẠO FOREIGN KEYS
-- =============================================
PRINT '=== TẠO FOREIGN KEYS ===';

-- FK Invoices -> SalesOrders
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Invoices_SalesOrders_SalesOrderId'
    AND parent_object_id = OBJECT_ID('Invoices')
)
BEGIN
    ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_SalesOrders_SalesOrderId
    FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id) ON DELETE NO ACTION;
    PRINT 'Đã tạo FK_Invoices_SalesOrders_SalesOrderId';
END

-- FK Invoices -> Customers
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Invoices_Customers_CustomerId'
    AND parent_object_id = OBJECT_ID('Invoices')
)
BEGIN
    ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_Customers_CustomerId
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id);
    PRINT 'Đã tạo FK_Invoices_Customers_CustomerId';
END

PRINT '=== HOÀN TẤT ===';
