using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng phân quyền chi tiết theo module và vai trò.
    /// Mỗi dòng xác định quyền cụ thể của một vai trò trên một module.
    /// </summary>
    [Table("PhanQuyen")]
    public class PhanQuyen
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Vai trò: Admin, Giám đốc, Kinh doanh, Kế toán, Thủ kho, Sản xuất</summary>
        [Required]
        [MaxLength(50)]
        public string VaiTro { get; set; }

        /// <summary>Tên module: Quotations, SalesOrders, ProductionOrders, Warehouse, Finance, AuditLog</summary>
        [Required]
        [MaxLength(100)]
        public string TenModule { get; set; }

        /// <summary>Quyền xem dữ liệu</summary>
        public bool CoXem { get; set; } = false;

        /// <summary>Quyền tạo mới</summary>
        public bool CoTao { get; set; } = false;

        /// <summary>Quyền chỉnh sửa</summary>
        public bool CoSua { get; set; } = false;

        /// <summary>Quyền xóa</summary>
        public bool CoXoa { get; set; } = false;

        /// <summary>Quyền duyệt/phê duyệt</summary>
        public bool CoDuyet { get; set; } = false;

        /// <summary>Quyền xuất báo cáo</summary>
        public bool CoXuatBC { get; set; } = false;

        /// <summary>
        /// Kiểm tra xem vai trò có quyền cụ thể nào đó không.
        /// </summary>
        /// <param name="loaiQuyen">Loại quyền: Xem, Tao, Sua, Xoa, Duyet, XuatBC</param>
        /// <returns>True nếu có quyền</returns>
        public bool KiemTraQuyen(string loaiQuyen)
        {
            return loaiQuyen switch
            {
                "Xem" => CoXem,
                "Tao" => CoTao,
                "Sua" => CoSua,
                "Xoa" => CoXoa,
                "Duyet" => CoDuyet,
                "XuatBC" => CoXuatBC,
                _ => false
            };
        }

        /// <summary>
        /// Các loại quyền được hỗ trợ.
        /// </summary>
        public static class LoaiQuyen
        {
            public const string Xem = "Xem";
            public const string Tao = "Tao";
            public const string Sua = "Sua";
            public const string Xoa = "Xoa";
            public const string Duyet = "Duyet";
            public const string XuatBC = "XuatBC";
        }

        /// <summary>
        /// Các module được phân quyền.
        /// </summary>
        public static class Modules
        {
            public const string Quotations = "Quotations";
            public const string SalesOrders = "SalesOrders";
            public const string ProductionOrders = "ProductionOrders";
            public const string Warehouse = "Warehouse";
            public const string Finance = "Finance";
            public const string AuditLog = "AuditLog";
        }
    }
}
