-- Bước 1: Thêm 2 cột TK_No và TK_Co vào bảng SalesOrders
-- Kiểm tra cột đã tồn tại chưa trước khi thêm (cho phép NULL)

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

PRINT 'Hoàn tất cập nhật cơ sở dữ liệu';
