using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Phiếu giao hàng - lưu thông tin giao sản phẩm cho khách.
    /// </summary>
    public class PhieuGiaoHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã phiếu giao hàng (format: PGH-YYMMDD001)</summary>
        [Required, MaxLength(20)]
        public string MaPGH { get; set; }

        /// <summary>Đơn hàng liên quan</summary>
        public int SalesOrderId { get; set; }

        [ForeignKey("SalesOrderId")]
        public virtual SalesOrder SalesOrder { get; set; }

        /// <summary>Ngày giao hàng dự kiến</summary>
        public DateTime NgayGiao { get; set; }

        /// <summary>Người giao hàng</summary>
        [MaxLength(100)]
        public string NguoiGiao { get; set; }

        /// <summary>Địa chỉ giao hàng</summary>
        [MaxLength(500)]
        public string DiaChiGiao { get; set; }

        /// <summary>Số điện thoại nhận hàng</summary>
        [MaxLength(20)]
        public string SoDienThoaiNhanHang { get; set; }

        /// <summary>
        /// Trạng thái giao hàng:
        /// Chờ giao, Đang giao, Đã giao, Giao thất bại, Hoàn trả
        /// </summary>
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Chờ giao";

        /// <summary>Ngày giao thực tế</summary>
        public DateTime? NgayGiaoThucTe { get; set; }

        /// <summary>Người nhận hàng (chữ ký)</summary>
        [MaxLength(100)]
        public string NguoiNhanHang { get; set; }

        /// <summary>Ghi chú giao hàng (lý do thất bại nếu có)</summary>
        public string GhiChuGiao { get; set; }

        /// <summary>Đường dẫn ảnh xác nhận (chữ ký/biên nhận)</summary>
        [MaxLength(500)]
        public string HinhAnhXacNhan { get; set; }

        /// <summary>Người tạo phiếu</summary>
        [MaxLength(100)]
        public string NguoiTao { get; set; }

        /// <summary>Ngày tạo</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Chi tiết phiếu giao hàng</summary>
        public virtual ICollection<ChiTietPhieuGiaoHang> ChiTiet { get; set; }
    }

    /// <summary>
    /// Chi tiết phiếu giao hàng - danh sách sản phẩm trong phiếu giao.
    /// </summary>
    public class ChiTietPhieuGiaoHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Phiếu giao hàng cha</summary>
        public int PhieuGiaoHangId { get; set; }

        [ForeignKey("PhieuGiaoHangId")]
        public virtual PhieuGiaoHang PhieuGiaoHang { get; set; }

        /// <summary>Tên sản phẩm</summary>
        [MaxLength(200)]
        public string TenSanPham { get; set; }

        /// <summary>Số lượng giao</summary>
        public int SoLuong { get; set; }

        /// <summary>Đơn vị tính</summary>
        [MaxLength(50)]
        public string DonViTinh { get; set; }

        /// <summary>Ghi chú</summary>
        public string GhiChu { get; set; }
    }

    /// <summary>
    /// Các trạng thái giao hàng.
    /// </summary>
    public static class TrangThaiGiaoHang
    {
        public const string ChoGiao = "Chờ giao";
        public const string DangGiao = "Đang giao";
        public const string DaGiao = "Đã giao";
        public const string ThatBai = "Giao thất bại";
        public const string HoanTra = "Hoàn trả";

        public static readonly string[] All = { ChoGiao, DangGiao, DaGiao, ThatBai, HoanTra };
    }
}