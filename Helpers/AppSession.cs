using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Helpers
{
    public static class AppSession
    {
        public static User CurrentUser { get; set; }

        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsGiamDoc => CurrentUser?.Role == "Giám đốc";
        public static bool IsKeToan => CurrentUser?.Role == "Kế toán";
        public static bool IsThuKho => CurrentUser?.Role == "Thủ kho";
        public static bool IsNhanVien => CurrentUser?.Role == "Nhân viên";
        public static bool IsKinhDoanh => CurrentUser?.Role == "Kinh doanh";
        public static bool IsSanXuat => CurrentUser?.Role == "Sản xuất";

        /// <summary>User có bị buộc đổi mật khẩu không</summary>
        public static bool PhaiDoiMatKhau => CurrentUser?.BatBuocDoiMatKhau ?? false;

        /// <summary>Màn hình Quản lý khách hàng: Kế toán, Quản lý kho, Nhân viên (+ Admin).</summary>
        public static bool CanViewCustomerModule =>
            IsAdmin || IsKeToan || IsThuKho || IsNhanVien;

        /// <summary>Kiểm tra user có phải là Admin hoặc Giám đốc không</summary>
        public static bool IsAdminOrGiamDoc => IsAdmin || IsGiamDoc;
    }
}