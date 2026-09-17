using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhanMemInAnERP.Attributes;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;

namespace PhanMemInAnERP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Memory cache dùng chung cho toàn app (singleton).
        /// </summary>
        public static IMemoryCache GlobalCache { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // PDFsharp (Core): map Arial → C:\Windows\Fonts trước khi tạo XFont (chỉ đặt được một lần)
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            // Khởi tạo Memory Cache toàn cục
            var cacheOptions = new MemoryCacheOptions
            {
                SizeLimit = 1024, // Giới hạn 1024 entries
                CompactionPercentage = 0.1 // Nén 10% khi đầy
            };
            GlobalCache = new MemoryCache(cacheOptions);

            // Khởi tạo PermissionChecker với cache
            PermissionChecker.Initialize(GlobalCache);

            // Đánh dấu migration đã áp dụng và thêm cột thiếu TRƯỚC khi load XAML
            MarkMigrationAsApplied();

            var bundle = new MaterialDesignThemes.Wpf.BundledTheme();

            // base.OnStartup phải chạy sau khi đã chuẩn bị xong
            base.OnStartup(e);

            // Giờ WPF đã sẵn sàng, mới tạo LoginView
            var login = new Views.LoginView();
            login.Show();
        }

        private void MarkMigrationAsApplied()
        {
            try
            {
                using var db = DbContextFactory.CreateWithAudit();

                // Đảm bảo bảng __EFMigrationsHistory tồn tại
                DbContextFactory.EnsureMigrationHistoryExists(db);

                var connection = db.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using var command = connection.CreateCommand();

                // Kiểm tra xem có bảng nào trong database không
                command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'";
                var tableCount = Convert.ToInt32(command.ExecuteScalar());

                if (tableCount == 0)
                    return; // Database trống, không cần làm gì

                // BƯỚC 1: Thêm cột thiếu nếu chưa có
                AddMissingColumns(command, "Customers", new[]
                {
                    ("MaKH", "NVARCHAR(20)"),
                    ("Email", "NVARCHAR(100)"),
                    ("NguoiLienHe", "NVARCHAR(100)"),
                    ("GhiChu", "NVARCHAR(MAX)"),
                    ("TrangThai", "NVARCHAR(50)"),
                    ("NguoiTao", "NVARCHAR(100)"),
                    ("NgayTao", "DATETIME2"),
                    ("NguoiSua", "NVARCHAR(100)"),
                    ("NgaySua", "DATETIME2"),
                    ("NguoiXoa", "NVARCHAR(100)"),
                    ("NgayXoa", "DATETIME2")
                });

                AddMissingColumns(command, "Users", new[]
                {
                    ("Email", "NVARCHAR(MAX)"),
                    ("PhoneNumber", "NVARCHAR(MAX)")
                });

                AddMissingColumns(command, "AuditLogs", new[]
                {
                    ("ThoiGian", "DATETIME2"),
                    ("NguoiThucHien", "NVARCHAR(100)"),
                    ("UserId", "INT"),
                    ("HanhDong", "NVARCHAR(20)"),
                    ("TenBang", "NVARCHAR(100)"),
                    ("IdBanGhi", "INT"),
                    ("DuLieuCu", "NVARCHAR(MAX)"),
                    ("DuLieuMoi", "NVARCHAR(MAX)"),
                    ("DiaChiIP", "NVARCHAR(50)")
                });

                AddMissingColumns(command, "Invoices", new[]
                {
                    ("TemplateNo", "NVARCHAR(MAX)"),
                    ("Symbol", "NVARCHAR(MAX)"),
                    ("RefOrderNo", "NVARCHAR(MAX)"),
                    ("SubTotal", "FLOAT"),
                    ("VATPercent", "FLOAT"),
                    ("DueDate", "DATETIME2")
                });

                // Thêm cột thiếu cho ProductionOrders
                AddMissingColumns(command, "ProductionOrders", new[]
                {
                    ("BuHao", "FLOAT"),
                    ("SoCon", "INT"),
                    ("PrintLength", "FLOAT"),
                    ("PlatePricePerColorLarge", "FLOAT"),
                    ("PlatePricePerColorSmall", "FLOAT"),
                    ("InchRong", "FLOAT"),
                    ("InhDai", "FLOAT"),
                    ("SoMau", "INT"),
                    ("KichThuocTinh", "NVARCHAR(100)"),
                    ("LoaiGiay", "NVARCHAR(100)"),
                    ("GhiChu", "NVARCHAR(MAX)")
                });

                // Thêm cột thiếu cho Quotations
                AddMissingColumns(command, "Quotations", new[]
                {
                    ("PlatePricePerColorLarge", "FLOAT"),
                    ("PlatePricePerColorSmall", "FLOAT"),
                    ("KichThuocTinh", "NVARCHAR(100)"),
                    ("LoaiGiay", "NVARCHAR(100)"),
                    ("GhiChu", "NVARCHAR(MAX)")
                });

                // Thêm cột thiếu cho SalesOrders
                AddMissingColumns(command, "SalesOrders", new[]
                {
                    ("GhiChu", "NVARCHAR(MAX)")
                });

                // Thêm cột thiếu cho Payments
                AddMissingColumns(command, "Payments", new[]
                {
                    ("GhiChu", "NVARCHAR(MAX)")
                });

                // Thêm cột thiếu cho Materials
                AddMissingColumns(command, "Materials", new[]
                {
                    ("GhiChu", "NVARCHAR(MAX)")
                });

                // BƯỚC 2: Loại bỏ NULL từ các cột quan trọng trong Customers
                // Update NULL MaKH thành chuỗi rỗng
                command.CommandText = "UPDATE Customers SET MaKH = '' WHERE MaKH IS NULL";
                command.ExecuteNonQuery();

                // Update NULL Email thành chuỗi rỗng
                command.CommandText = "UPDATE Customers SET Email = '' WHERE Email IS NULL";
                command.ExecuteNonQuery();

                // Update NULL NguoiLienHe thành chuỗi rỗng
                command.CommandText = "UPDATE Customers SET NguoiLienHe = '' WHERE NguoiLienHe IS NULL";
                command.ExecuteNonQuery();

                // Update NULL GhiChu thành chuỗi rỗng
                command.CommandText = "UPDATE Customers SET GhiChu = '' WHERE GhiChu IS NULL";
                command.ExecuteNonQuery();

                // Update NULL TrangThai thành 'Hoạt động'
                command.CommandText = "UPDATE Customers SET TrangThai = 'Hoạt động' WHERE TrangThai IS NULL";
                command.ExecuteNonQuery();

                // Update NULL NguoiTao thành ''
                command.CommandText = "UPDATE Customers SET NguoiTao = '' WHERE NguoiTao IS NULL";
                command.ExecuteNonQuery();

                // Update NULL NguoiSua thành ''
                command.CommandText = "UPDATE Customers SET NguoiSua = '' WHERE NguoiSua IS NULL";
                command.ExecuteNonQuery();

                // Update NULL NguoiXoa thành ''
                command.CommandText = "UPDATE Customers SET NguoiXoa = '' WHERE NguoiXoa IS NULL";
                command.ExecuteNonQuery();

                // Update NULL NgayTao thành GETDATE()
                command.CommandText = "UPDATE Customers SET NgayTao = GETDATE() WHERE NgayTao IS NULL";
                command.ExecuteNonQuery();

                // Update NULL cho các bảng khác
                // Quotations
                command.CommandText = "UPDATE Quotations SET GhiChu = '' WHERE GhiChu IS NULL";
                command.ExecuteNonQuery();

                // SalesOrders
                command.CommandText = "UPDATE SalesOrders SET GhiChu = '' WHERE GhiChu IS NULL";
                command.ExecuteNonQuery();

                // Payments
                command.CommandText = "UPDATE Payments SET GhiChu = '' WHERE GhiChu IS NULL";
                command.ExecuteNonQuery();

                // Materials
                command.CommandText = "UPDATE Materials SET GhiChu = '' WHERE GhiChu IS NULL";
                command.ExecuteNonQuery();

                // Invoices
                command.CommandText = "UPDATE Invoices SET TemplateNo = '' WHERE TemplateNo IS NULL";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Invoices SET Symbol = '' WHERE Symbol IS NULL";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Invoices SET RefOrderNo = '' WHERE RefOrderNo IS NULL";
                command.ExecuteNonQuery();

                // BƯỚC 3: XÓA MIGRATION HISTORY để buộc chạy lại logic (để update NULL values)
                command.CommandText = "DELETE FROM [__EFMigrationsHistory]";
                command.ExecuteNonQuery();

                // BƯỚC 4: Đánh dấu InitialCreate là đã áp dụng
                command.CommandText = @"
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (N'20260412070749_InitialCreate', N'8.0.8');
                ";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MarkMigrationAsApplied Error: {ex.Message}");
            }
        }

        private void AddMissingColumns(System.Data.Common.DbCommand command, string tableName, (string ColumnName, string DataType)[] columns)
        {
            foreach (var col in columns)
            {
                try
                {
                    command.CommandText = $@"
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[{tableName}]') AND name = '{col.ColumnName}')
                        BEGIN
                            ALTER TABLE [{tableName}] ADD [{col.ColumnName}] {col.DataType};
                        END
                    ";
                    command.ExecuteNonQuery();
                }
                catch
                {
                    // Bỏ qua lỗi cho từng cột
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dọn dẹp cache khi app đóng
            GlobalCache?.Dispose();
            base.OnExit(e);
        }
    }
}
