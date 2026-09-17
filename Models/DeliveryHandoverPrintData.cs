using System;
using System.Collections.Generic;

namespace PhanMemInAnERP.Models
{
    /// <summary>Dữ liệu in phiếu bàn giao hàng cho khách (sau khi SX xong).</summary>
    public class DeliveryHandoverPrintData
    {
        public string PhieuSo { get; set; } = "";
        public DateTime NgayLap { get; set; } = DateTime.Now;
        public string KhachHang { get; set; } = "";
        public string DiaChi { get; set; } = "";
        public string DienThoai { get; set; } = "";
        public string SoDonHang { get; set; } = "";
        public string SoLenhSx { get; set; } = "";
        public string SoHoaDon { get; set; } = "";
        public string SanPham { get; set; } = "";
        public int SoLuong { get; set; }
        public string DonVi { get; set; } = "cái";
        public List<string> NoiDungSanXuat { get; set; } = new List<string>();
        public string GhiChu { get; set; } = "";
        public string NguoiBanGiao { get; set; } = "";
        public string TongDonHang { get; set; } = ""; // ví dụ: "15.000.000 đ"
    }
}
