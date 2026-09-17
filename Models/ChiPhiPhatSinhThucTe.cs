using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng lưu chi phí phát sinh thực tế trong quá trình sản xuất.
    /// Dùng để so sánh với giá dự kiến và phân tích lợi nhuận.
    /// </summary>
    public class ChiPhiPhatSinhThucTe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Báo giá liên quan</summary>
        public int QuotationId { get; set; }

        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; }

        /// <summary>Lệnh sản xuất liên quan (nullable)</summary>
        public int? ProductionOrderId { get; set; }

        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder ProductionOrder { get; set; }

        /// <summary>Tên khoản mục chi phí</summary>
        [Required, MaxLength(200)]
        public string TenKhoanMuc { get; set; }

        /// <summary>Số lượng phát sinh</summary>
        public decimal SoLuong { get; set; }

        /// <summary>Đơn giá</summary>
        public decimal DonGia { get; set; }

        /// <summary>Thành tiền = SoLuong * DonGia</summary>
        public decimal ThanhTien { get; set; }

        /// <summary>
        /// Loại chi phí phát sinh.
        /// Các giá trị: "NVL vượt định mức", "Nhân công thêm", "Sửa chữa lỗi", "Khác"
        /// </summary>
        [MaxLength(50)]
        public string Loai { get; set; }

        /// <summary>Người nhập chi phí</summary>
        [MaxLength(100)]
        public string NguoiNhap { get; set; }

        /// <summary>Ngày nhập</summary>
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        /// <summary>Ghi chú thêm</summary>
        public string GhiChu { get; set; }
    }

    /// <summary>
    /// Danh sách các loại chi phí phát sinh.
    /// </summary>
    public static class LoaiChiPhiPhatSinh
    {
        public const string NVLVuotDinhMuc = "NVL vượt định mức";
        public const string NhanCongThem = "Nhân công thêm";
        public const string SuaChuaLoi = "Sửa chữa lỗi";
        public const string Khac = "Khác";

        public static readonly string[] All = { NVLVuotDinhMuc, NhanCongThem, SuaChuaLoi, Khac };
    }
}