using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng phiếu xuất kho - lưu thông tin phiếu xuất NVL ra khỏi kho.
    /// Cấu trúc: Header + Detail (1 phiếu có nhiều vật liệu).
    /// </summary>
    public class PhieuXuatKho
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã phiếu xuất kho (format: PXK-YYMMDD001)</summary>
        [Required, MaxLength(20)]
        public string MaPhieuXuat { get; set; }

        /// <summary>Ngày xuất kho</summary>
        public DateTime NgayXuat { get; set; } = DateTime.Now;

        /// <summary>
        /// Mục đích xuất kho:
        /// Sản xuất, Bán hàng, Hủy, Trả NCC, Khác
        /// </summary>
        [Required, MaxLength(50)]
        public string MucDich { get; set; }

        /// <summary>Lệnh sản xuất liên quan (nullable)</summary>
        public int? ProductionOrderId { get; set; }

        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder ProductionOrder { get; set; }

        /// <summary>Ghi chú</summary>
        public string GhiChu { get; set; }

        /// <summary>Người xuất (Thủ kho)</summary>
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /// <summary>Tổng giá trị xuất kho</summary>
        public decimal TongGiaTri { get; set; } = 0;

        /// <summary>Chi tiết phiếu xuất kho</summary>
        public virtual ICollection<ChiTietPhieuXuatKho> ChiTiet { get; set; }
    }

    /// <summary>
    /// Chi tiết phiếu xuất kho - danh sách NVL được xuất trong 1 phiếu.
    /// </summary>
    public class ChiTietPhieuXuatKho
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Phiếu xuất kho cha</summary>
        public int PhieuXuatKhoId { get; set; }

        [ForeignKey("PhieuXuatKhoId")]
        public virtual PhieuXuatKho PhieuXuatKho { get; set; }

        /// <summary>Vật liệu xuất</summary>
        public int MaterialId { get; set; }

        [ForeignKey("MaterialId")]
        public virtual Material Material { get; set; }

        /// <summary>Số lượng xuất</summary>
        public decimal SoLuong { get; set; }

        /// <summary>Đơn giá xuất tại thời điểm xuất kho</summary>
        public decimal DonGia { get; set; }

        /// <summary>Thành tiền = SoLuong * DonGia</summary>
        public decimal ThanhTien { get; set; }

        /// <summary>Ghi chú</summary>
        public string GhiChu { get; set; }
    }

    /// <summary>
    /// Các mục đích xuất kho.
    /// </summary>
    public static class MucDichXuatKho
    {
        public const string SanXuat = "Sản xuất";
        public const string BanHang = "Bán hàng";
        public const string Huy = "Hủy";
        public const string TraNCC = "Trả NCC";
        public const string Khac = "Khác";

        public static readonly string[] All = { SanXuat, BanHang, Huy, TraNCC, Khac };
    }
}