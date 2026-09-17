using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã khách hàng tự sinh format "KH-{YY}{0001}", unique</summary>
        [MaxLength(20)]
        public string MaKH { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        [MaxLength(50)]
        public string TaxCode { get; set; }

        /// <summary>Email khách hàng - có thể null</summary>
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>Người liên hệ - có thể null</summary>
        [MaxLength(100)]
        public string NguoiLienHe { get; set; }

        /// <summary>Ghi chú - có thể null</summary>
        public string GhiChu { get; set; }

        /// <summary>Trạng thái: Hoạt động, Không hoạt động, Nợ xấu</summary>
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Hoạt động";

        /// <summary>Người tạo - có thể null</summary>
        [MaxLength(100)]
        public string NguoiTao { get; set; }

        /// <summary>Ngày tạo - mặc định GETDATE()</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Người sửa - có thể null</summary>
        [MaxLength(100)]
        public string NguoiSua { get; set; }

        /// <summary>Ngày sửa - có thể null</summary>
        public DateTime? NgaySua { get; set; }

        /// <summary>Người xóa - có thể null</summary>
        [MaxLength(100)]
        public string NguoiXoa { get; set; }

        /// <summary>Ngày xóa - cho soft delete, có thể null</summary>
        public DateTime? NgayXoa { get; set; }

        /// <summary>Số đơn hàng (SalesOrder) trùng tên khách — chỉ hiển thị, không lưu DB.</summary>
        [NotMapped]
        public int SalesOrderCount { get; set; }

        /// <summary>Tổng tiền đơn hàng (VNĐ) — chỉ hiển thị, không lưu DB.</summary>
        [NotMapped]
        public double SalesOrderTotal { get; set; }

        /// <summary>
        /// Kiểm tra khách hàng có đang bị xóa mềm (soft delete) hay không.
        /// </summary>
        /// <returns>True nếu đã bị xóa (NgayXoa != null)</returns>
        public bool DaBiXoa()
        {
            return NgayXoa.HasValue;
        }

        /// <summary>
        /// Cột SQL NOT NULL — TextBox rỗng gửi null, phải chuẩn hóa trước SaveChanges.
        /// </summary>
        public void NormalizeForDatabase()
        {
            Name = (Name ?? "").Trim();
            Phone = Phone ?? "";
            Address = Address ?? "";
            TaxCode = TaxCode ?? "";
            MaKH = MaKH ?? "";
            Email = Email ?? "";
            NguoiLienHe = NguoiLienHe ?? "";
            GhiChu = GhiChu ?? "";
            TrangThai = TrangThai ?? "Hoạt động";
            NguoiTao = NguoiTao ?? "";
            NguoiSua = NguoiSua ?? "";
            NguoiXoa = NguoiXoa ?? "";
        }
    }
}
