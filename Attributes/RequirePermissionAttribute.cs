using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.Attributes
{
    /// <summary>
    /// Attribute đánh dấu method cần kiểm tra quyền trước khi thực thi.
    /// Dùng trên các action trong ViewModel hoặc API Controller.
    /// 
    /// Ví dụ sử dụng:
    /// [RequirePermission("Quotations", "Tao")]
    /// public void CreateQuote() { }
    /// 
    /// [RequirePermission("SalesOrders", "Duyet")]
    /// public void ApproveOrder() { }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute
    {
        /// <summary>Tên module cần kiểm tra quyền</summary>
        public string Module { get; }

        /// <summary>Loại quyền cần có: Xem, Tao, Sua, Xoa, Duyet, XuatBC</summary>
        public string Permission { get; }

        /// <summary>
        /// Constructor với module và loại quyền.
        /// </summary>
        /// <param name="module">Tên module (VD: "Quotations", "SalesOrders")</param>
        /// <param name="permission">Loại quyền (VD: "Xem", "Tao", "Sua", "Xoa", "Duyet", "XuatBC")</param>
        public RequirePermissionAttribute(string module, string permission)
        {
            Module = module ?? throw new ArgumentNullException(nameof(module));
            Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }

        /// <summary>
        /// Kiểm tra xem người dùng hiện tại có quyền hay không.
        /// Sử dụng IMemoryCache nếu được cung cấp.
        /// </summary>
        /// <param name="cache">IMemoryCache instance (optional)</param>
        /// <returns>True nếu có quyền</returns>
        public bool CheckPermission(IMemoryCache? cache = null)
        {
            var currentUser = AppSession.CurrentUser;
            if (currentUser == null)
            {
                return false;
            }

            string vaiTro = currentUser.Role;
            if (string.IsNullOrEmpty(vaiTro))
            {
                return false;
            }

            // Nếu có cache, sử dụng PermissionService
            if (cache != null)
            {
                using var dbContext = DbContextFactory.Create();
                var permissionService = new PermissionService(dbContext, cache);
                return permissionService.HasPermission(vaiTro, Module, Permission);
            }

            // Fallback: Query trực tiếp không cache
            return CheckPermissionDirectly(vaiTro, Module, Permission);
        }

        /// <summary>
        /// Kiểm tra quyền trực tiếp từ database không qua cache.
        /// </summary>
        private bool CheckPermissionDirectly(string vaiTro, string module, string permission)
        {
            using var dbContext = DbContextFactory.Create();
            var phanQuyen = dbContext.PhanQuyen
                .FirstOrDefault(p => p.VaiTro == vaiTro && p.TenModule == module);

            if (phanQuyen == null)
            {
                return false;
            }

            return phanQuyen.KiemTraQuyen(permission);
        }

        /// <summary>
        /// Lấy thông báo lỗi khi không có quyền.
        /// </summary>
        public string GetErrorMessage()
        {
            var currentUser = AppSession.CurrentUser;
            string userName = currentUser?.FullName ?? currentUser?.Username ?? "Unknown";
            return $"Người dùng '{userName}' không có quyền '{Permission}' trên module '{Module}'.";
        }
    }

    /// <summary>
    /// Helper class để kiểm tra và thực thi RequirePermission attribute.
    /// Sử dụng trong ViewModel hoặc base class.
    /// </summary>
    public static class PermissionChecker
    {
        private static IMemoryCache _cache;

        /// <summary>
        /// Khởi tạo cache cho permission checker.
        /// Gọi method này trong App.xaml.cs startup.
        /// </summary>
        public static void Initialize(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Kiểm tra quyền trước khi thực thi method.
        /// Ném UnauthorizedAccessException nếu không có quyền.
        /// </summary>
        /// <param name="attribute">RequirePermissionAttribute</param>
        /// <exception cref="UnauthorizedAccessException">Khi không có quyền</exception>
        public static void CheckAndThrow(RequirePermissionAttribute attribute)
        {
            if (attribute == null)
            {
                return; // Không có attribute = cho phép
            }

            if (!attribute.CheckPermission(_cache))
            {
                throw new UnauthorizedAccessException(attribute.GetErrorMessage());
            }
        }

        /// <summary>
        /// Kiểm tra quyền, trả về kết quả mà không ném exception.
        /// </summary>
        /// <param name="module">Tên module</param>
        /// <param name="permission">Loại quyền</param>
        /// <returns>True nếu có quyền</returns>
        public static bool HasPermission(string module, string permission)
        {
            if (_cache == null)
            {
                return false;
            }

            var currentUser = AppSession.CurrentUser;
            if (currentUser == null)
            {
                return false;
            }

            using var dbContext = DbContextFactory.Create();
            var permissionService = new PermissionService(dbContext, _cache);
            return permissionService.HasPermission(currentUser.Role, module, permission);
        }

        /// <summary>
        /// Kiểm tra quyền với vai trò cụ thể.
        /// </summary>
        /// <param name="vaiTro">Tên vai trò</param>
        /// <param name="module">Tên module</param>
        /// <param name="permission">Loại quyền</param>
        /// <returns>True nếu có quyền</returns>
        public static bool HasPermission(string vaiTro, string module, string permission)
        {
            if (_cache == null)
            {
                return false;
            }

            using var dbContext = DbContextFactory.Create();
            var permissionService = new PermissionService(dbContext, _cache);
            return permissionService.HasPermission(vaiTro, module, permission);
        }
    }
}
