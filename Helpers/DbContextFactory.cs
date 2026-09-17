using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>
    /// Factory class để tạo AppDbContext với AuditInterceptor đã được đăng ký.
    /// Sử dụng class này thay vì "new AppDbContext()" để bật tính năng audit log tự động.
    /// </summary>
    public static class DbContextFactory
    {
        private static readonly AuditInterceptor _auditInterceptor = new();

        /// <summary>
        /// Tạo mới một AppDbContext với AuditInterceptor đã được đăng ký.
        /// </summary>
        /// <returns>DbContext đã configure với interceptor</returns>
        public static AppDbContext CreateWithAudit()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            string connectionString = "Server=localhost;Database=PhanMemInAnDB;User Id=sa;Password=123;TrustServerCertificate=True;";
            
            optionsBuilder.UseSqlServer(connectionString)
                .AddInterceptors(_auditInterceptor);

            return new AppDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Tạo mới một AppDbContext thông thường (không có audit interceptor).
        /// Chỉ dùng method này khi không cần ghi audit log.
        /// </summary>
        /// <returns>DbContext thông thường</returns>
        public static AppDbContext Create()
        {
            return new AppDbContext();
        }

        /// <summary>
        /// Đảm bảo bảng __EFMigrationsHistory tồn tại trong database.
        /// </summary>
        /// <param name="db">AppDbContext instance</param>
        public static void EnsureMigrationHistoryExists(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END
            ");
        }
    }
}
