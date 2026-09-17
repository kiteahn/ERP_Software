-- ============================================
-- PhanMemInAnERP - Database Setup Script (Clean)
-- File: DatabaseSetup_Final.sql
-- Description: Tạo fresh database cho module Hóa đơn và Thu tiền
-- ============================================

PRINT '=== BẮT ĐẦU CẬP NHẬT CƠ SỞ DỮ LIỆU ===';
GO

-- =============================================
-- 1. XÓA BẢNG Invoices nếu tồn tại (chỉ xóa khi cần thiết)
-- =============================================
PRINT '--- Bước 0: Dọn dẹp (nếu cần) ---';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    -- Xóa constraints trước
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_Invoices_InvoiceId')
    BEGIN
        ALTER TABLE Payments DROP CONSTRAINT FK_Payments_Invoices_InvoiceId;
        PRINT 'Đã xóa FK Payments_Invoices';
    END

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_SalesOrders_SalesOrderId')
    BEGIN
        ALTER TABLE Invoices DROP CONSTRAINT FK_Invoices_SalesOrders_SalesOrderId;
        PRINT 'Đã xóa FK_Invoices_SalesOrders';
    END

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_Customers_CustomerId')
    BEGIN
        ALTER TABLE Invoices DROP CONSTRAINT FK_Invoices_Customers_CustomerId;
        PRINT 'Đã xóa FK_Invoices_Customers';
    END

    -- Xóa indexes
    DECLARE @sql NVARCHAR(MAX) = N'';
    SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON Invoices;' + CHAR(13)
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID('Invoices') AND i.name IS NOT NULL;
    EXEC sp_executesql @sql;

    -- Xóa bảng
    DROP TABLE IF EXISTS Invoices;
    PRINT 'Đã xóa bảng Invoices cũ';
END
GO

-- =============================================
-- 2. THÊM CỘT TK_No, TK_Co VÀO SalesOrders (NẾU CHƯA CÓ)
-- =============================================
PRINT '--- Bước 1: Cập nhật bảng SalesOrders ---';

IF COL_LENGTH('SalesOrders', 'TK_No') IS NULL
BEGIN
    ALTER TABLE SalesOrders ADD TK_No NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột TK_No';
END

IF COL_LENGTH('SalesOrders', 'TK_Co') IS NULL
BEGIN
    ALTER TABLE SalesOrders ADD TK_Co NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột TK_Co';
END
GO

-- =============================================
-- 3. THAY ĐỔI ExportTransactions.MaterialId THÀNH NULLABLE
-- =============================================
PRINT '--- Bước 2: Sửa cột MaterialId trong ExportTransactions ---';

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'MaterialId'
    AND Object_ID = Object_ID(N'ExportTransactions')
    AND is_nullable = 0
)
BEGIN
    IF EXISTS (
        SELECT * FROM sys.foreign_keys
        WHERE name = 'FK_ExportTransactions_Materials_MaterialId'
    )
    BEGIN
        ALTER TABLE ExportTransactions DROP CONSTRAINT FK_ExportTransactions_Materials_MaterialId;
        PRINT 'Đã xóa FK ExportTransactions_Materials';
    END

    ALTER TABLE ExportTransactions ALTER COLUMN MaterialId INT NULL;
    PRINT 'Đã sửa MaterialId thành NULL';

    ALTER TABLE ExportTransactions
    ADD CONSTRAINT FK_ExportTransactions_Materials_MaterialId
    FOREIGN KEY (MaterialId) REFERENCES Materials(Id);
    PRINT 'Đã tạo lại FK ExportTransactions_Materials';
END
GO

-- =============================================
-- 4. TẠO BẢNG ProductionImports (NẾU CHƯA CÓ)
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

    CREATE INDEX IX_ProductionImports_ProductionOrderId ON ProductionImports(ProductionOrderId);
    CREATE INDEX IX_ProductionImports_SalesOrderId ON ProductionImports(SalesOrderId);
    CREATE INDEX IX_ProductionImports_UserId ON ProductionImports(UserId);
    CREATE INDEX IX_ProductionImports_Status ON ProductionImports(Status);

    PRINT 'Đã tạo bảng ProductionImports';
END
GO

-- =============================================
-- 5. TẠO BẢNG Invoices (HÓA ĐƠN) - FRESH
-- =============================================
PRINT '--- Bước 4: Tạo bảng Invoices ---';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
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

    -- Indexes
    CREATE INDEX IX_Invoices_SalesOrderId ON Invoices(SalesOrderId);
    CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
    CREATE INDEX IX_Invoices_Status ON Invoices(Status);
    CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);

    -- Foreign keys
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
    PRINT 'Bảng Invoices đã tồn tại, bỏ qua (đã xử lý ở bước 0)';
END
GO

-- =============================================
-- 6. THÊM CỘT VÀO Payments (InvoiceId, AccountType)
-- =============================================
PRINT '--- Bước 5: Cập nhật bảng Payments ---';

IF COL_LENGTH('Payments', 'AccountType') IS NULL
BEGIN
    ALTER TABLE Payments ADD AccountType NVARCHAR(50) NULL;
    PRINT 'Đã thêm AccountType';
END

IF COL_LENGTH('Payments', 'InvoiceId') IS NULL
BEGIN
    ALTER TABLE Payments ADD InvoiceId INT NULL;
    PRINT 'Đã thêm InvoiceId';
END

-- FK Payments -> Invoices
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Payments_Invoices_InvoiceId'
)
BEGIN
    ALTER TABLE Payments
    ADD CONSTRAINT FK_Payments_Invoices_InvoiceId
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id);
    PRINT 'Đã tạo FK Payments_Invoices';
END
GO

-- =============================================
-- 7. CẬP NHẬT PRODUCTIONIMPORTS: ĐẢM BẢO FK ĐÚNG
-- =============================================
PRINT '--- Bước 6: Cập nhật ProductionImports FK ---';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionImports')
BEGIN
    -- Kiểm tra và thêm FK ProductionOrders nếu chưa có
    IF NOT EXISTS (
        SELECT * FROM sys.foreign_keys
        WHERE name = 'FK_ProductionImports_ProductionOrders'
    )
    BEGIN
        ALTER TABLE ProductionImports
        ADD CONSTRAINT FK_ProductionImports_ProductionOrders
        FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(Id);
        PRINT 'Đã tạo FK ProductionImports_ProductionOrders';
    END

    -- Kiểm tra và thêm FK SalesOrders nếu chưa có
    IF NOT EXISTS (
        SELECT * FROM sys.foreign_keys
        WHERE name = 'FK_ProductionImports_SalesOrders'
    )
    BEGIN
        ALTER TABLE ProductionImports
        ADD CONSTRAINT FK_ProductionImports_SalesOrders
        FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id);
        PRINT 'Đã tạo FK ProductionImports_SalesOrders';
    END
END
GO

-- =============================================
-- 8. THÊM INDEX TỐI ƯU
-- =============================================
PRINT '=== TẠO INDEX TỐI ƯU ===';

-- Index cho Payments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_RefInvoiceNo')
BEGIN
    CREATE INDEX IX_Payments_RefInvoiceNo ON Payments(RefInvoiceNo);
    PRINT 'Đã tạo IX_Payments_RefInvoiceNo';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_InvoiceId')
BEGIN
    CREATE INDEX IX_Payments_InvoiceId ON Payments(InvoiceId);
    PRINT 'Đã tạo IX_Payments_InvoiceId';
END
GO

PRINT '=== HOÀN TẤT CẬP NHẬT CƠ SỞ DỮ LIỆU ===';
PRINT 'Tất cả các bảng và cột đã được tạo/cập nhật thành công!';
GO
