using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service truy vấn và quản lý AuditLog.
    /// Chỉ Admin và Giám đốc mới có quyền truy cập.
    /// </summary>
    public class AuditLogService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Kiểm tra người dùng hiện tại có phải là Admin hoặc Giám đốc không.
        /// </summary>
        private static bool HasPermission()
        {
            var user = AppSession.CurrentUser;
            if (user == null) return false;

            // Admin hoặc Giám đốc được phép truy cập
            return user.Role == "Admin" || user.Role == "Giám đốc";
        }

        /// <summary>
        /// Ném exception nếu không có quyền.
        /// </summary>
        private static void RequirePermission()
        {
            if (!HasPermission())
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền truy cập AuditLog. Chỉ Admin và Giám đốc mới được phép xem.");
            }
        }

        public AuditLogService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Lấy lịch sử thay đổi của một bản ghi cụ thể.
        /// </summary>
        /// <param name="tenBang">Tên bảng cần xem</param>
        /// <param name="idBanGhi">ID của bản ghi cần xem</param>
        /// <returns>Danh sách các thay đổi theo thứ tự thời gian giảm dần (mới nhất trước)</returns>
        /// <exception cref="UnauthorizedAccessException">Khi không có quyền Admin/Giám đốc</exception>
        public List<AuditLog> GetByBang(string tenBang, int idBanGhi)
        {
            RequirePermission();

            if (string.IsNullOrWhiteSpace(tenBang))
            {
                throw new ArgumentException("Tên bảng không được để trống", nameof(tenBang));
            }

            return _context.AuditLogs
                .Where(a => a.TenBang == tenBang && a.IdBanGhi == idBanGhi)
                .OrderByDescending(a => a.ThoiGian)
                .Include(a => a.User)
                .ToList();
        }

        /// <summary>
        /// Lấy lịch sử thay đổi của một người dùng trong khoảng thời gian.
        /// </summary>
        /// <param name="userId">ID của người dùng cần xem</param>
        /// <param name="from">Thời gian bắt đầu</param>
        /// <param name="to">Thời gian kết thúc</param>
        /// <returns>Danh sách các thay đổi theo thứ tự thời gian giảm dần</returns>
        /// <exception cref="UnauthorizedAccessException">Khi không có quyền Admin/Giám đốc</exception>
        public List<AuditLog> GetByNguoiDung(int userId, DateTime from, DateTime to)
        {
            RequirePermission();

            if (from > to)
            {
                throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
            }

            return _context.AuditLogs
                .Where(a => a.UserId == userId 
                           && a.ThoiGian >= from 
                           && a.ThoiGian <= to)
                .OrderByDescending(a => a.ThoiGian)
                .Include(a => a.User)
                .ToList();
        }

        /// <summary>
        /// Lấy tất cả audit log với phân trang.
        /// </summary>
        /// <param name="page">Số trang (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang</param>
        /// <returns>Danh sách audit log với thông tin phân trang</returns>
        /// <exception cref="UnauthorizedAccessException">Khi không có quyền Admin/Giám đốc</exception>
        public (List<AuditLog> Items, int TotalCount, int TotalPages) GetAll(int page, int pageSize)
        {
            RequirePermission();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 bản ghi/trang

            var query = _context.AuditLogs.AsQueryable();

            // Đếm tổng số bản ghi
            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Lấy dữ liệu theo trang
            var items = query
                .OrderByDescending(a => a.ThoiGian)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.User)
                .ToList();

            return (items, totalCount, totalPages);
        }

        /// <summary>
        /// Tìm kiếm audit log theo nhiều tiêu chí.
        /// </summary>
        /// <param name="tenBang">Tên bảng (optional)</param>
        /// <param name="hanhDong">Loại hành động: CREATE, UPDATE, DELETE (optional)</param>
        /// <param name="from">Thời gian bắt đầu (optional)</param>
        /// <param name="to">Thời gian kết thúc (optional)</param>
        /// <param name="page">Số trang</param>
        /// <param name="pageSize">Số bản ghi/trang</param>
        public (List<AuditLog> Items, int TotalCount, int TotalPages) Search(
            string? tenBang = null,
            string? hanhDong = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 20)
        {
            RequirePermission();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _context.AuditLogs.AsQueryable();

            // Áp dụng bộ lọc
            if (!string.IsNullOrWhiteSpace(tenBang))
            {
                query = query.Where(a => a.TenBang == tenBang);
            }

            if (!string.IsNullOrWhiteSpace(hanhDong))
            {
                query = query.Where(a => a.HanhDong == hanhDong);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.ThoiGian >= from.Value);
            }

            if (to.HasValue)
            {
                // Thêm 1 ngày để bao gồm cả ngày kết thúc
                var toEndOfDay = to.Value.Date.AddDays(1);
                query = query.Where(a => a.ThoiGian < toEndOfDay);
            }

            // Đếm tổng
            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Lấy dữ liệu
            var items = query
                .OrderByDescending(a => a.ThoiGian)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.User)
                .ToList();

            return (items, totalCount, totalPages);
        }

        /// <summary>
        /// Lấy thống kê tổng quan về audit log.
        /// </summary>
        public AuditStatistics GetStatistics(DateTime? from = null, DateTime? to = null)
        {
            RequirePermission();

            var query = _context.AuditLogs.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(a => a.ThoiGian >= from.Value);
            }

            if (to.HasValue)
            {
                var toEndOfDay = to.Value.Date.AddDays(1);
                query = query.Where(a => a.ThoiGian < toEndOfDay);
            }

            var logs = query.ToList();

            return new AuditStatistics
            {
                TotalRecords = logs.Count,
                CreateCount = logs.Count(l => l.HanhDong == "CREATE"),
                UpdateCount = logs.Count(l => l.HanhDong == "UPDATE"),
                DeleteCount = logs.Count(l => l.HanhDong == "DELETE"),
                UniqueUsers = logs.Select(l => l.UserId).Distinct().Count(),
                UniqueTables = logs.Select(l => l.TenBang).Distinct().Count(),
                FromDate = logs.Min(l => l.ThoiGian),
                ToDate = logs.Max(l => l.ThoiGian)
            };
        }
    }

    /// <summary>
    /// Thống kê audit log.
    /// </summary>
    public class AuditStatistics
    {
        public int TotalRecords { get; set; }
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueTables { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
