using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service quản lý phân quyền theo module.
    /// Sử dụng IMemoryCache để cache kết quả với TTL 5 phút.
    /// </summary>
    public class PermissionService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Prefix cho cache key.
        /// </summary>
        private const string CacheKeyPrefix = "Permission_";

        public PermissionService(AppDbContext context, IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Lấy thông tin phân quyền của một vai trò trên một module.
        /// Kết quả được cache trong 5 phút.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò (VD: "Admin", "Giám đốc", "Kế toán")</param>
        /// <param name="tenModule">Tên module (VD: "Quotations", "SalesOrders")</param>
        /// <returns>Object PhanQuyen hoặc null nếu không tìm thấy</returns>
        public PhanQuyen GetQuyen(string vaiTro, string tenModule)
        {
            if (string.IsNullOrWhiteSpace(vaiTro) || string.IsNullOrWhiteSpace(tenModule))
            {
                return null;
            }

            // Tạo cache key
            string cacheKey = $"{CacheKeyPrefix}{vaiTro}_{tenModule}";

            // Thử lấy từ cache
            if (_cache.TryGetValue(cacheKey, out PhanQuyen cachedPermission))
            {
                return cachedPermission;
            }

            // Query từ database
            var permission = _context.PhanQuyen
                .FirstOrDefault(p => p.VaiTro == vaiTro && p.TenModule == tenModule);

            // Cache kết quả (cả null cũng cache để tránh query lặp lại)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheDuration);

            _cache.Set(cacheKey, permission, cacheOptions);

            return permission;
        }

        /// <summary>
        /// Kiểm tra xem một vai trò có quyền cụ thể trên module hay không.
        /// Kết quả được cache trong 5 phút.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò</param>
        /// <param name="tenModule">Tên module</param>
        /// <param name="loaiQuyen">Loại quyền: "Xem", "Tao", "Sua", "Xoa", "Duyet", "XuatBC"</param>
        /// <returns>True nếu có quyền, False nếu không có quyền hoặc không tìm thấy</returns>
        public bool HasPermission(string vaiTro, string tenModule, string loaiQuyen)
        {
            if (string.IsNullOrWhiteSpace(vaiTro) || 
                string.IsNullOrWhiteSpace(tenModule) || 
                string.IsNullOrWhiteSpace(loaiQuyen))
            {
                return false;
            }

            var permission = GetQuyen(vaiTro, tenModule);
            
            if (permission == null)
            {
                return false;
            }

            return permission.KiemTraQuyen(loaiQuyen);
        }

        /// <summary>
        /// Xóa cache của một vai trò cụ thể.
        /// Gọi method này khi cập nhật phân quyền.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò cần xóa cache</param>
        public void ClearCache(string vaiTro)
        {
            // Lấy tất cả các module được phân quyền cho vai trò này
            var modules = _context.PhanQuyen
                .Where(p => p.VaiTro == vaiTro)
                .Select(p => p.TenModule)
                .ToList();

            // Xóa cache cho từng module
            foreach (var module in modules)
            {
                string cacheKey = $"{CacheKeyPrefix}{vaiTro}_{module}";
                _cache.Remove(cacheKey);
            }
        }

        /// <summary>
        /// Xóa toàn bộ cache phân quyền.
        /// Gọi method này khi cập nhật toàn bộ ma trận phân quyền.
        /// </summary>
        public void ClearAllCache()
        {
            // Lấy tất cả các combination VaiTro + Module
            var combinations = _context.PhanQuyen
                .Select(p => new { p.VaiTro, p.TenModule })
                .ToList();

            // Xóa cache cho từng combination
            foreach (var combo in combinations)
            {
                string cacheKey = $"{CacheKeyPrefix}{combo.VaiTro}_{combo.TenModule}";
                _cache.Remove(cacheKey);
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả các phân quyền của một vai trò.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò</param>
        /// <returns>Danh sách PhanQuyen</returns>
        public List<PhanQuyen> GetAllByVaiTro(string vaiTro)
        {
            if (string.IsNullOrWhiteSpace(vaiTro))
            {
                return new List<PhanQuyen>();
            }

            return _context.PhanQuyen
                .Where(p => p.VaiTro == vaiTro)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách tất cả các phân quyền trên một module.
        /// </summary>
        /// <param name="tenModule">Tên module</param>
        /// <returns>Danh sách PhanQuyen</returns>
        public List<PhanQuyen> GetAllByModule(string tenModule)
        {
            if (string.IsNullOrWhiteSpace(tenModule))
            {
                return new List<PhanQuyen>();
            }

            return _context.PhanQuyen
                .Where(p => p.TenModule == tenModule)
                .ToList();
        }

        /// <summary>
        /// Cập nhật một phân quyền cụ thể.
        /// Tự động xóa cache sau khi cập nhật.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò</param>
        /// <param name="tenModule">Tên module</param>
        /// <param name="coXem">Quyền xem</param>
        /// <param name="coTao">Quyền tạo</param>
        /// <param name="coSua">Quyền sửa</param>
        /// <param name="coXoa">Quyền xóa</param>
        /// <param name="coDuyet">Quyền duyệt</param>
        /// <param name="coXuatBC">Quyền xuất báo cáo</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public bool UpdatePermission(string vaiTro, string tenModule,
            bool coXem, bool coTao, bool coSua, bool coXoa, bool coDuyet, bool coXuatBC)
        {
            var permission = _context.PhanQuyen
                .FirstOrDefault(p => p.VaiTro == vaiTro && p.TenModule == tenModule);

            if (permission == null)
            {
                return false;
            }

            permission.CoXem = coXem;
            permission.CoTao = coTao;
            permission.CoSua = coSua;
            permission.CoXoa = coXoa;
            permission.CoDuyet = coDuyet;
            permission.CoXuatBC = coXuatBC;

            _context.SaveChanges();

            // Xóa cache của vai trò này
            ClearCache(vaiTro);

            return true;
        }

        /// <summary>
        /// Lấy danh sách tất cả vai trò trong hệ thống.
        /// </summary>
        /// <returns>Danh sách tên vai trò</returns>
        public List<string> GetAllVaiTro()
        {
            return _context.PhanQuyen
                .Select(p => p.VaiTro)
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách tất cả modules trong hệ thống.
        /// </summary>
        /// <returns>Danh sách tên module</returns>
        public List<string> GetAllModules()
        {
            return _context.PhanQuyen
                .Select(p => p.TenModule)
                .Distinct()
                .OrderBy(m => m)
                .ToList();
        }

        /// <summary>
        /// Lấy ma trận phân quyền đầy đủ (tất cả vai trò x tất cả module).
        /// </summary>
        /// <returns>Danh sách tất cả PhanQuyen</returns>
        public List<PhanQuyen> GetFullMatrix()
        {
            return _context.PhanQuyen
                .OrderBy(p => p.TenModule)
                .ThenBy(p => p.VaiTro)
                .ToList();
        }
    }
}
