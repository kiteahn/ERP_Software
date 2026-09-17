using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Interceptor tự động ghi log thay đổi dữ liệu vào bảng AuditLog.
    /// Bắt mọi thao tác INSERT, UPDATE, DELETE trên các bảng được giám sát.
    /// Sử dụng AppSession để lấy thông tin người dùng hiện tại.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        /// <summary>
        /// Danh sách các bảng cần giám sát thay đổi.
        /// </summary>
        private static readonly HashSet<string> MonitoredTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "Customers",
            "Quotations",
            "SalesOrders",
            "ProductionOrders",
            "Invoices"
        };

        /// <summary>
        /// Lấy thông tin user từ AppSession (cho WPF app).
        /// </summary>
        private static (int? UserId, string TenNguoiDung) GetCurrentUser()
        {
            var user = Helpers.AppSession.CurrentUser;
            if (user != null)
            {
                return (user.Id, user.FullName ?? user.Username);
            }
            return (null, "System");
        }

        /// <summary>
        /// Lấy tên bảng từ EntityEntry.
        /// </summary>
        private static string GetTableName(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            // Try to get the table name from the Table attribute or use the entity type name
            var tableAttribute = entry.Entity.GetType()
                .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), true)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.Schema.TableAttribute;

            return tableAttribute?.Name ?? entry.Entity.GetType().Name;
        }

        /// <summary>
        /// Serialize entity thành JSON.
        /// </summary>
        private static string SerializeEntity(object entity)
        {
            if (entity == null) return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(entity, options);
            }
            catch
            {
                return entity.ToString();
            }
        }

        /// <summary>
        /// Kiểm tra xem entity có thuộc bảng được giám sát không.
        /// </summary>
        private static bool IsMonitoredTable(string tableName)
        {
            return MonitoredTables.Contains(tableName);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ProcessChanges(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ProcessChanges(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Xử lý các thay đổi và ghi audit log.
        /// </summary>
        private void ProcessChanges(DbContextEventData eventData)
        {
            if (eventData.Context == null) return;

            var context = eventData.Context;
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .ToList();

            if (entries.Count == 0) return;

            // Lấy thông tin user hiện tại
            var (userId, tenNguoiDung) = GetCurrentUser();

            // Lấy thời gian hiện tại (Bắc Kinh timezone)
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")
            );

            foreach (var entry in entries)
            {
                var tableName = GetTableName(entry);

                // Kiểm tra bảng có được giám sát không
                if (!IsMonitoredTable(tableName)) continue;

                // Lấy ID của bản ghi
                int recordId = GetRecordId(entry);
                if (recordId == 0) continue; // Skip nếu không lấy được ID

                // Xác định hành động
                string action = entry.State switch
                {
                    EntityState.Added => "CREATE",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => null
                };

                if (action == null) continue;

                // Serialize dữ liệu cũ và mới
                string oldData = null;
                string newData = null;

                if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    // Lấy giá trị cũ từ OriginalValues
                    oldData = SerializeEntity(entry.OriginalValues.ToObject());
                }

                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    // Lấy giá trị mới từ CurrentValues
                    newData = SerializeEntity(entry.Entity);
                }

                // Tạo AuditLog entry
                var auditLog = new AuditLog
                {
                    ThoiGian = now,
                    NguoiThucHien = tenNguoiDung,
                    UserId = userId,
                    HanhDong = action,
                    TenBang = tableName,
                    IdBanGhi = recordId,
                    DuLieuCu = oldData,
                    DuLieuMoi = newData,
                    DiaChiIP = null // WPF app không có IP
                };

                // Thêm vào context
                context.Set<AuditLog>().Add(auditLog);
            }
        }

        /// <summary>
        /// Lấy ID từ EntityEntry.
        /// </summary>
        private static int GetRecordId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            // Thử lấy từ CurrentValue trước
            var idProperty = entry.Property("Id");
            if (idProperty != null && idProperty.CurrentValue != null)
            {
                if (int.TryParse(idProperty.CurrentValue.ToString(), out int id))
                {
                    return id;
                }
            }

            // Thử cách khác - lấy property có tên là "Id"
            var entityType = entry.Entity.GetType();
            var idPropertyInfo = entityType.GetProperty("Id");
            if (idPropertyInfo != null)
            {
                var value = idPropertyInfo.GetValue(entry.Entity);
                if (value != null && int.TryParse(value.ToString(), out int id))
                {
                    return id;
                }
            }

            return 0;
        }
    }
}
