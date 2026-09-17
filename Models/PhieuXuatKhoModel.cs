using System;
using System.Collections.Generic;

namespace PhanMemInAnERP.Models
{
    public class PhieuXuatKhoModel
    {
        public string SoPhieu { get; set; }
        public DateTime NgayXuat { get; set; }
        public string TaiKhoanNo { get; set; }
        public string TaiKhoanCo { get; set; }
        public string NguoiNhan { get; set; }
        public string DiaChiBoPhan { get; set; }
        public string LyDoXuat { get; set; }
        public string XuatTaiKho { get; set; }
        public List<PhieuXuatKhoChiTiet> ChiTietHangHoa { get; set; } = new List<PhieuXuatKhoChiTiet>();
        public double TongTien { get; set; }
        public string TongTienBangChu { get; set; }
        public string ChungTuGoc { get; set; }
    }

    public class PhieuXuatKhoChiTiet
    {
        public string TenVatTu { get; set; }
        public string MaSo { get; set; }
        public string DVT { get; set; }
        public double SoLuongYeuCau { get; set; }
        public double SoLuongThucXuat { get; set; }
        public double DonGia { get; set; }
        public double ThanhTien => SoLuongThucXuat * DonGia;
    }
}