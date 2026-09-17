-- ============================================
-- PhanMemInAnERP - Database Setup Script
-- File: DatabaseSetup.sql
-- Created: 2026-04-18
-- Description: Tạo/ cập nhật cơ sở dữ liệu cho module Hóa đơn và Thu tiền
-- ============================================

PRINT '=== BẮT ĐẦU CẬP NHẬT CƠ SỞ DỮ LIỆU ===';
GO

-- =============================================
-- 1. THÊM CỘT TK_No, TK_Co VÀO SalesOrders (NẾU CHƯA CÓ)
-- =============================================
PRINT '--- Bước 1: Cập nhật bảng SalesOrders ---';

IF COL_LENGTH('SalesOrders', 'TK_No') IS NULL
BEGIN
    ALTER TABLE SalesOrders
    ADD TK_No NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột TK_No';
END
ELSE
BEGIN
    PRINT 'Cột TK_No đã tồn tại, bỏ qua';
END

IF COL_LENGTH('SalesOrders', 'TK_Co') IS NULL
BEGIN
    ALTER TABLE SalesOrders
    ADD TK_Co NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột TK_Co';
END
ELSE
BEGIN
    PRINT 'Cột TK_Co đã tồn tại, bỏ qua';
END
GO

-- =============================================
-- 2. THAY ĐỔI ExportTransactions.MaterialId THÀNH NULLABLE
-- =============================================
PRINT '--- Bước 2: Sửa cột MaterialId trong ExportTransactions ---';

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'MaterialId'
    AND Object_ID = Object_ID(N'ExportTransactions')
    AND is_nullable = 0
)
BEGIN
    -- Xóa foreign key trước
    IF EXISTS (
        SELECT * FROM sys.foreign_keys
        WHERE name = 'FK_ExportTransactions_Materials_MaterialId'
    )
    BEGIN
        ALTER TABLE ExportTransactions
        DROP CONSTRAINT FK_ExportTransactions_Materials_MaterialId;
        PRINT 'Đã xóa FK ExportTransactions_Materials';
    END

    -- Sửa cột thành nullable
    ALTER TABLE ExportTransactions
    ALTER COLUMN MaterialId INT NULL;
    PRINT 'Đã sửa MaterialId thành NULL';

    -- Thêm lại foreign key
    ALTER TABLE ExportTransactions
    ADD CONSTRAINT FK_ExportTransactions_Materials_MaterialId
    FOREIGN KEY (MaterialId) REFERENCES Materials(Id);
    PRINT 'Đã tạo lại FK ExportTransactions_Materials';
END
ELSE
BEGIN
    PRINT 'Cột MaterialId đã là nullable, bỏ qua';
END
GO

-- =============================================
-- 3. TẠO BẢNG ProductionImports (NẾU CHƯA CÓ)
-- =============================================
PRINT '--- Bước 3: Tạo bảng ProductionImports ---';

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

    -- Tạo indexes
    CREATE INDEX IX_ProductionImports_ProductionOrderId ON ProductionImports(ProductionOrderId);
    CREATE INDEX IX_ProductionImports_SalesOrderId ON ProductionImports(SalesOrderId);
    CREATE INDEX IX_ProductionImports_UserId ON ProductionImports(UserId);
    CREATE INDEX IX_ProductionImports_Status ON ProductionImports(Status);

    PRINT 'Đã tạo bảng ProductionImports';
END
ELSE
BEGIN
    PRINT 'Bảng ProductionImports đã tồn tại, bỏ qua';
END
GO

-- =============================================
-- 4. TẠO BẢNG Invoices (HÓA ĐƠN)
-- =============================================
PRINT '--- Bước 4: Tạo bảng Invoices ---';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    CREATE TABLE Invoices (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNo NVARCHAR(100) NOT NULL,
        InvoiceDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        RefOrderNo NVARCHAR(100) NOT NULL DEFAULT '',
        SalesOrderId INT NOT NULL DEFAULT 0,
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

    -- Tạo indexes
    CREATE INDEX IX_Invoices_SalesOrderId ON Invoices(SalesOrderId);
    CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
    CREATE INDEX IX_Invoices_InvoiceNo ON Invoices(InvoiceNo);
    CREATE INDEX IX_Invoices_Status ON Invoices(Status);
    CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);

    -- Thêm foreign keys
    ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_SalesOrders_SalesOrderId
    FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id) ON DELETE NO ACTION;

    ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_Customers_CustomerId
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id);

    PRINT 'Đã tạo bảng Invoices';
END
ELSE
BEGIN
    PRINT 'Bảng Invoices đã tồn tại, bỏ qua';
END
GO

-- =============================================
-- 5. THÊM CỘT VÀO Payments (InvoiceId, AccountType)
-- =============================================
PRINT '--- Bước 5: Cập nhật bảng Payments ---';

-- Thêm cột AccountType
IF COL_LENGTH('Payments', 'AccountType') IS NULL
BEGIN
    ALTER TABLE Payments
    ADD AccountType NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột AccountType vào Payments';
END
ELSE
BEGIN
    PRINT 'Cột AccountType đã tồn tại trong Payments';
END

-- Thêm cột InvoiceId
IF COL_LENGTH('Payments', 'InvoiceId') IS NULL
BEGIN
    ALTER TABLE Payments
    ADD InvoiceId INT NULL;
    PRINT 'Đã thêm cột InvoiceId vào Payments';
END
ELSE
BEGIN
    PRINT 'Cột InvoiceId đã tồn tại trong Payments';
END

-- Thêm foreign key từ Payments đến Invoices
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Payments_Invoices_InvoiceId'
)
BEGIN
    ALTER TABLE Payments
    ADD CONSTRAINT FK_Payments_Invoices_InvoiceId
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id);
    PRINT 'Đã thêm FK Payments_Invoices';
END
ELSE
BEGIN
    PRINT 'FK Payments_Invoices đã tồn tại';
END
GO

-- =============================================
-- 6. CẬP NHẬT ExportTransactions: SỬA LẠI FK CHO MaterialId NULLABLE
-- =============================================
PRINT '--- Bước 6: Cập nhật ExportTransactions ---';

-- Đảm bảo FK đúng với MaterialId nullable
IF EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_ExportTransactions_Materials_MaterialId'
)
BEGIN
    ALTER TABLE ExportTransactions
    DROP CONSTRAINT FK_ExportTransactions_Materials_MaterialId;
    PRINT 'Đã xóa FK cũ';
END

ALTER TABLE ExportTransactions
ALTER COLUMN MaterialId INT NULL;

ALTER TABLE ExportTransactions
ADD CONSTRAINT FK_ExportTransactions_Materials_MaterialId
FOREIGN KEY (MaterialId) REFERENCES Materials(Id);
PRINT 'Đã cập nhật FK ExportTransactions_Materials';
GO

-- =============================================
-- 7. THÊM INDEX CHO CÁC CỘT THƯỜNG QUERY
-- =============================================
PRINT '--- Bước 7: Tạo indexes tối ưu ---';

-- Index cho Invoices
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_CustomerId')
BEGIN
    CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
    PRINT 'Đã tạo index IX_Invoices_CustomerId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_RefOrderNo')
BEGIN
    CREATE INDEX IX_Invoices_RefOrderNo ON Invoices(RefOrderNo);
    PRINT 'Đã tạo index IX_Invoices_RefOrderNo';
END

-- Index cho ProductionImports
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductionImports_Status')
BEGIN
    CREATE INDEX IX_ProductionImports_Status ON ProductionImports(Status);
    PRINT 'Đã tạo index IX_ProductionImports_Status';
END

-- Index cho Payments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_RefInvoiceNo')
BEGIN
    CREATE INDEX IX_Payments_RefInvoiceNo ON Payments(RefInvoiceNo);
    PRINT 'Đã tạo index IX_Payments_RefInvoiceNo';
END
GO

PRINT '=== HOÀN TẤT CẬP NHẬT CƠ SỞ DỮ LIỆU ===';
PRINT 'Lưu ý: Kiểm tra lại cấu trúc bằng cách chạy VerifyColumns.sql nếu cần';
GO
