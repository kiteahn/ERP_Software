using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service báo cáo tài chính - tính toán từ sổ cái.
    /// </summary>
    public class BaoCaoService
    {
        private readonly AppDbContext _context;
        private readonly SoCaiService _soCaiService;

        public BaoCaoService(AppDbContext context, SoCaiService soCaiService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _soCaiService = soCaiService ?? throw new ArgumentNullException(nameof(soCaiService));
        }

        /// <summary>
        /// Lấy báo cáo doanh thu tổng hợp trong kỳ.
        /// </summary>
        public async Task<BaoCaoDoanhThuKeToanDto> GetBaoCaoDoanhThuAsync(DateTime from, DateTime to)
        {
            // Doanh thu: sum(SoTienCo) WHERE MaTK LIKE '511%'
            var tongDoanhThu = await _context.SoCais
                .Where(s => (s.MaTK == "511" || s.MaTK.StartsWith("511"))
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienCo);

            // Giảm trừ: sum(SoTienNo) WHERE MaTK LIKE '521%'
            var tongGiamTru = await _context.SoCais
                .Where(s => (s.MaTK == "521" || s.MaTK.StartsWith("521"))
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienNo);

            // Doanh thu thuần
            var doanhThuThuan = tongDoanhThu - tongGiamTru;

            // Giá vốn: sum(SoTienNo) WHERE MaTK='632'
            var giaVon = await _context.SoCais
                .Where(s => s.MaTK == "632"
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienNo);

            // Lợi nhuận gộp
            var loiNhuanGop = doanhThuThuan - giaVon;

            // Tỷ lệ lợi nhuận gộp
            decimal tyLeLoiNhuanGop = 0;
            if (tongDoanhThu > 0)
            {
                tyLeLoiNhuanGop = loiNhuanGop / tongDoanhThu * 100;
            }

            // Chi phí bán hàng: sum(SoTienNo) WHERE MaTK='641'
            var chiPhiBH = await _context.SoCais
                .Where(s => s.MaTK == "641"
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienNo);

            // Chi phí QLDN: sum(SoTienNo) WHERE MaTK='642'
            var chiPhiQLDN = await _context.SoCais
                .Where(s => s.MaTK == "642"
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienNo);

            // Lợi nhuận thuần trước thuế
            var loiNhuanThuanTruocThue = loiNhuanGop - chiPhiBH - chiPhiQLDN;

            // Số hóa đơn trong kỳ
            var soHoaDon = await _context.Invoices
                .Where(i => i.InvoiceDate >= from.Date
                    && i.InvoiceDate <= to.Date)
                .CountAsync();

            // Số khách hàng trong kỳ
            var soKhachHang = await _context.Invoices
                .Where(i => i.InvoiceDate >= from.Date
                    && i.InvoiceDate <= to.Date)
                .Select(i => i.CustomerName)
                .Distinct()
                .CountAsync();

            // Doanh thu tháng trước
            var thangTruoc = from.AddMonths(-1);
            var dtThangTruoc = await _context.SoCais
                .Where(s => (s.MaTK == "511" || s.MaTK.StartsWith("511"))
                    && s.NgayHachToan >= thangTruoc.Date
                    && s.NgayHachToan <= thangTruoc.AddMonths(1).AddDays(-1).Date)
                .SumAsync(s => s.SoTienCo);

            var gtThangTruoc = await _context.SoCais
                .Where(s => (s.MaTK == "521" || s.MaTK.StartsWith("521"))
                    && s.NgayHachToan >= thangTruoc.Date
                    && s.NgayHachToan <= thangTruoc.AddMonths(1).AddDays(-1).Date)
                .SumAsync(s => s.SoTienNo);

            var doanhThuThangTruoc = dtThangTruoc - gtThangTruoc;

            // Tăng trưởng
            decimal tyLeTangTruong = 0;
            if (doanhThuThangTruoc > 0)
            {
                tyLeTangTruong = (doanhThuThuan - doanhThuThangTruoc) / doanhThuThangTruoc * 100;
            }

            return new BaoCaoDoanhThuKeToanDto
            {
                TuNgay = from,
                DenNgay = to,
                TongDoanhThu = tongDoanhThu,
                TongGiamTru = tongGiamTru,
                DoanhThuThuan = doanhThuThuan,
                GiaVon = giaVon,
                LoiNhuanGop = loiNhuanGop,
                TyLeLoiNhuanGop = tyLeLoiNhuanGop,
                ChiPhiBH = chiPhiBH,
                ChiPhiQLDN = chiPhiQLDN,
                LoiNhuanThuanTruocThue = loiNhuanThuanTruocThue,
                SoHoaDon = soHoaDon,
                SoKhachHang = soKhachHang,
                DoanhThuThangTruoc = doanhThuThangTruoc,
                TyLeTangTruong = tyLeTangTruong
            };
        }

        /// <summary>
        /// Lấy chi tiết doanh thu theo TK 511.
        /// </summary>
        public async Task<List<ChiTietTK511Dto>> GetChiTietDT511Async(DateTime from, DateTime to)
        {
            var result = new List<ChiTietTK511Dto>();

            var taiKhoans = await _context.TaiKhoanKeToans
                .Where(t => t.IsActive && (t.MaTK == "511" || t.MaTK.StartsWith("511")))
                .OrderBy(t => t.MaTK)
                .ToListAsync();

            decimal tongDoanhThu = await _context.SoCais
                .Where(s => (s.MaTK == "511" || s.MaTK.StartsWith("511"))
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienCo);

            foreach (var tk in taiKhoans)
            {
                var psCo = await _context.SoCais
                    .Where(s => s.MaTK == tk.MaTK
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienCo);

                if (psCo > 0)
                {
                    var chiTiet = new ChiTietTK511Dto
                    {
                        MaTK = tk.MaTK,
                        TenTK = tk.TenTK,
                        TongDoanhThu = psCo
                    };
                    if (tongDoanhThu > 0)
                    {
                        chiTiet.TyTrong = psCo / tongDoanhThu * 100;
                    }
                    result.Add(chiTiet);
                }
            }

            return result.OrderByDescending(x => x.TongDoanhThu).ToList();
        }

        /// <summary>
        /// Lấy báo cáo công nợ tổng hợp.
        /// </summary>
        public async Task<BaoCaoCongNoKeToanDto> GetBaoCaoCongNoAsync()
        {
            // Công nợ khách hàng
            var tongPhaiThu = await _context.CongNoKhachHangs
                .Where(c => c.TrangThai != "Đã thanh toán")
                .SumAsync(c => c.SoTienPhaiThu);

            var tongDaThu = await _context.CongNoKhachHangs
                .Where(c => c.TrangThai != "Đã thanh toán")
                .SumAsync(c => c.SoTienDaThu);

            var tongConLai = tongPhaiThu - tongDaThu;

            var danhSachQuaHan = await _context.CongNoKhachHangs
                .Where(c => c.TrangThai == "Quá hạn")
                .Include(c => c.Customer)
                .OrderByDescending(c => c.SoNgayQuaHan)
                .ToListAsync();

            var tongQuaHan = danhSachQuaHan.Sum(c => c.SoTienConLai);

            var danhSachSapDenHan = await _context.CongNoKhachHangs
                .Where(c => c.TrangThai == "Chưa thanh toán"
                    && c.NgayDenHan <= DateTime.Today.AddDays(7)
                    && c.NgayDenHan >= DateTime.Today)
                .Include(c => c.Customer)
                .OrderBy(c => c.NgayDenHan)
                .ToListAsync();

            return new BaoCaoCongNoKeToanDto
            {
                TongPhaiThu = tongPhaiThu,
                TongDaThu = tongDaThu,
                TongConLai = tongConLai,
                TongQuaHan = tongQuaHan,
                DanhSachQuaHan = danhSachQuaHan,
                DanhSachSapDenHan = danhSachSapDenHan
            };
        }

        /// <summary>
        /// Lấy doanh thu theo khách hàng.
        /// </summary>
        public async Task<DataTable> GetDoanhThuTheoKhachAsync(DateTime from, DateTime to)
        {
            var dt = new DataTable();
            dt.Columns.Add("TenKH", typeof(string));
            dt.Columns.Add("SoDon", typeof(int));
            dt.Columns.Add("TongDoanhThu", typeof(decimal));
            dt.Columns.Add("DaThu", typeof(decimal));
            dt.Columns.Add("ConNo", typeof(decimal));
            dt.Columns.Add("TyLeNo", typeof(decimal));

            var data = await _context.Invoices
                .Where(i => i.InvoiceDate >= from.Date && i.InvoiceDate <= to.Date)
                .GroupBy(i => i.CustomerName)
                .Select(g => new
                {
                    TenKH = g.Key,
                    SoDon = g.Count(),
                    TongDoanhThu = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TongDoanhThu)
                .ToListAsync();

            foreach (var item in data)
            {
                var congNo = await _context.CongNoKhachHangs
                    .Where(c => c.Customer.Name == item.TenKH)
                    .SumAsync(c => c.SoTienConLai);

                var daThu = (decimal)item.TongDoanhThu - congNo;
                decimal tyLeNo = 0;
                if (item.TongDoanhThu > 0)
                {
                    tyLeNo = congNo / (decimal)item.TongDoanhThu * 100;
                }

                dt.Rows.Add(item.TenKH, item.SoDon, item.TongDoanhThu, daThu, congNo, tyLeNo);
            }

            return dt;
        }

        /// <summary>
        /// Lấy doanh thu theo tháng trong năm.
        /// </summary>
        public async Task<DataTable> GetDoanhThuTheoThangAsync(int nam)
        {
            var dt = new DataTable();
            dt.Columns.Add("Thang", typeof(int));
            dt.Columns.Add("SoDon", typeof(int));
            dt.Columns.Add("DoanhThu", typeof(decimal));
            dt.Columns.Add("GiaVon", typeof(decimal));
            dt.Columns.Add("LoiNhuan", typeof(decimal));

            for (int thang = 1; thang <= 12; thang++)
            {
                var from = new DateTime(nam, thang, 1);
                var to = from.AddMonths(1).AddDays(-1);

                var doanhThu = await _context.SoCais
                    .Where(s => (s.MaTK == "511" || s.MaTK.StartsWith("511"))
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienCo);

                var giamTru = await _context.SoCais
                    .Where(s => (s.MaTK == "521" || s.MaTK.StartsWith("521"))
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienNo);

                var giaVon = await _context.SoCais
                    .Where(s => s.MaTK == "632"
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienNo);

                var soDon = await _context.Invoices
                    .Where(i => i.InvoiceDate >= from.Date
                        && i.InvoiceDate <= to.Date)
                    .CountAsync();

                dt.Rows.Add(thang, soDon, doanhThu - giamTru, giaVon, doanhThu - giamTru - giaVon);
            }

            return dt;
        }
    }

    /// <summary>
    /// DTO báo cáo doanh thu (KeToan).
    /// </summary>
    public class BaoCaoDoanhThuKeToanDto
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal TongGiamTru { get; set; }
        public decimal DoanhThuThuan { get; set; }
        public decimal GiaVon { get; set; }
        public decimal LoiNhuanGop { get; set; }
        public decimal TyLeLoiNhuanGop { get; set; }
        public decimal ChiPhiBH { get; set; }
        public decimal ChiPhiQLDN { get; set; }
        public decimal LoiNhuanThuanTruocThue { get; set; }
        public int SoHoaDon { get; set; }
        public int SoKhachHang { get; set; }
        public decimal DoanhThuThangTruoc { get; set; }
        public decimal TyLeTangTruong { get; set; }
    }

    /// <summary>
    /// DTO chi tiết TK 511.
    /// </summary>
    public class ChiTietTK511Dto
    {
        public string MaTK { get; set; } = "";
        public string TenTK { get; set; } = "";
        public decimal TongDoanhThu { get; set; }
        public decimal TyTrong { get; set; }
    }

    /// <summary>
    /// DTO báo cáo công nợ (KeToan).
    /// </summary>
    public class BaoCaoCongNoKeToanDto
    {
        public decimal TongPhaiThu { get; set; }
        public decimal TongDaThu { get; set; }
        public decimal TongConLai { get; set; }
        public decimal TongQuaHan { get; set; }
        public List<CongNoKhachHang> DanhSachQuaHan { get; set; } = new();
        public List<CongNoKhachHang> DanhSachSapDenHan { get; set; } = new();
    }
}
