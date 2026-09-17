using System;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Dòng trong Sổ Chi Tiết Vật Tư
    /// </summary>
    public class SoChiTietVatTuItem
    {
        public DateTime Ngay { get; set; }
        public string SoPhieu { get; set; }
        public string LoaiPhieu { get; set; } // "NHẬP" hoặc "XUẤT"
        public string MaVatTu { get; set; }
        public string TenVatTu { get; set; }
        public string DonViTinh { get; set; }
        public double TonDauKy { get; set; }
        public double SoLuongNhap { get; set; }
        public double SoLuongXuat { get; set; }
        public double TonCuoiKy { get; set; } // Tồn tức thời sau giao dịch
        public string GhiChu { get; set; }
        public double DonGia { get; set; }
    }

    /// <summary>
    /// Tổng hợp số chi tiết vật tư
    /// </summary>
    public class SoChiTietVatTuSummary
    {
        public double TongNhap { get; set; }
        public double TongXuat { get; set; }
        public double TonDauKy { get; set; }
        public double TonCuoiKy { get; set; }
    }
}