using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Công nợ khách hàng - theo dõi tiền khách hàng nợ.
    /// </summary>
    public class CongNoKhachHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Khách hàng</summary>
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        /// <summary>Hóa đơn liên quan</summary>
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        /// <summary>Số tiền phải thu</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienPhaiThu { get; set; }

        /// <summary>Số tiền đã thu</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienDaThu { get; set; } = 0;

        /// <summary>Số tiền còn lại</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienConLai { get; set; }

        /// <summary>Ngày hóa đơn</summary>
        public DateTime NgayHoaDon { get; set; }

        /// <summary>Ngày đến hạn thanh toán</summary>
        public DateTime NgayDenHan { get; set; }

        /// <summary>
        /// Trạng thái: Chưa thanh toán / Thanh toán một phần / Đã thanh toán / Quá hạn / Xóa nợ
        /// </summary>
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Chưa thanh toán";

        /// <summary>Số ngày quá hạn (tự tính)</summary>
        public int SoNgayQuaHan { get; set; } = 0;

        /// <summary>Ghi chú</summary>
        public string GhiChu { get; set; }

        /// <summary>Ngày tạo</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Trạng thái công nợ.
    /// </summary>
    public static class TrangThaiCongNo
    {
        public const string ChuaThanhToan = "Chưa thanh toán";
        public const string MotPhan = "Thanh toán một phần";
        public const string DaThanhToan = "Đã thanh toán";
        public const string QuaHan = "Quá hạn";
        public const string XoaNo = "Xóa nợ";

        public static readonly string[] KhachHang = { ChuaThanhToan, MotPhan, DaThanhToan, QuaHan, XoaNo };
    }

    /// <summary>
    /// Hình thức thanh toán.
    /// </summary>
    public static class HinhThucThanhToan
    {
        public const string TienMat = "Tiền mặt";
        public const string ChuyenKhoan = "Chuyển khoản";
        public const string QuetQR = "Quét QR";
        public const string Sec = "Séc";
        public const string TheTinDung = "Thẻ tín dụng";
    }
}
