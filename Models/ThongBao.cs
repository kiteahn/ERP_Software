using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng thông báo hệ thống.
    /// </summary>
    public class ThongBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Loại thông báo:
        /// "Tồn kho thấp" / "Đơn hàng mới" / "Báo giá sắp hết hạn" / "Nợ quá hạn" / "PO mới" / "Cần duyệt"
        /// </summary>
        [MaxLength(50)]
        public string LoaiThongBao { get; set; }

        /// <summary>Nội dung thông báo</summary>
        [MaxLength(500)]
        public string NoiDung { get; set; }

        /// <summary>Đường dẫn đến màn hình liên quan (nullable)</summary>
        [MaxLength(200)]
        public string DuongDan { get; set; }

        /// <summary>User nhận thông báo (null = broadcast cho tất cả)</summary>
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /// <summary>Đã đọc chưa</summary>
        public bool DaDoc { get; set; } = false;

        /// <summary>Ngày tạo</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Các loại thông báo.
    /// </summary>
    public static class LoaiThongBao
    {
        public const string TonKhoThap = "Tồn kho thấp";
        public const string DonHangMoi = "Đơn hàng mới";
        public const string BaoGiaHetHan = "Báo giá sắp hết hạn";
        public const string NoQuaHan = "Nợ quá hạn";
        public const string POMoi = "PO mới";
        public const string CanDuyet = "Cần duyệt";
        public const string DonMuaCanNhap = "Đơn mua cần nhập";
        public const string HeThong = "Hệ thống";
        public const string ThanhToanNCC = "Thanh toán NCC";
    }
}