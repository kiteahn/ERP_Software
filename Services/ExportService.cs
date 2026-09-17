using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service xuất file Excel/PDF báo cáo.
    /// </summary>
    public class ExportService
    {
        private readonly AppDbContext _context;
        private readonly BaoCaoService _baoCaoService;

        public ExportService(AppDbContext context, BaoCaoService baoCaoService)
        {
            _context = context;
            _baoCaoService = baoCaoService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        #region EXCEL

        /// <summary>
        /// Xuất báo cáo doanh thu ra Excel.
        /// </summary>
        public async Task<byte[]> XuatBaoCaoDoanhThuExcelAsync(DateTime from, DateTime to)
        {
            var baoCao = await _baoCaoService.GetBaoCaoDoanhThuAsync(from, to);
            var chiTiet511 = await _baoCaoService.GetChiTietDT511Async(from, to);

            using var package = new ExcelPackage();

            // Sheet 1: Tổng quan
            var sheet1 = package.Workbook.Worksheets.Add("Tổng quan");
            sheet1.Cells[1, 1].Value = $"BÁO CÁO DOANH THU {from:MM/yyyy} — {to:MM/yyyy}";
            sheet1.Cells[1, 1, 1, 6].Merge = true;
            sheet1.Cells[1, 1].Style.Font.Size = 14;
            sheet1.Cells[1, 1].Style.Font.Bold = true;
            sheet1.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sheet1.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
            sheet1.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);

            var kpis = new (string Label, decimal Value, bool IsPercent)[]
            {
                ("Doanh thu thuần", baoCao.DoanhThuThuan, false),
                ("Giá vốn", baoCao.GiaVon, false),
                ("Lợi nhuận gộp", baoCao.LoiNhuanGop, false),
                ("Tỷ lệ LN gộp (%)", baoCao.TyLeLoiNhuanGop, true),
                ("Số hóa đơn", baoCao.SoHoaDon, false),
                ("Số khách hàng", baoCao.SoKhachHang, false)
            };

            for (int i = 0; i < kpis.Length; i++)
            {
                sheet1.Cells[3 + i, 1].Value = kpis[i].Label;
                sheet1.Cells[3 + i, 1].Style.Font.Bold = true;
                sheet1.Cells[3 + i, 2].Value = kpis[i].Value;
                sheet1.Cells[3 + i, 2].Style.Numberformat.Format = kpis[i].IsPercent ? "0.00%" : "#,##0";
            }

            // Sheet 2: TK 511 chi tiết
            var sheet2 = package.Workbook.Worksheets.Add("TK 511 Chi tiết");
            sheet2.Cells[1, 1].Value = "MaTK";
            sheet2.Cells[1, 2].Value = "Tên tài khoản";
            sheet2.Cells[1, 3].Value = "Doanh thu";
            sheet2.Cells[1, 4].Value = "Tỷ trọng %";
            for (int i = 0; i < chiTiet511.Count; i++)
            {
                sheet2.Cells[2 + i, 1].Value = chiTiet511[i].MaTK;
                sheet2.Cells[2 + i, 2].Value = chiTiet511[i].TenTK;
                sheet2.Cells[2 + i, 3].Value = (double)chiTiet511[i].TongDoanhThu;
                sheet2.Cells[2 + i, 4].Value = (double)chiTiet511[i].TyTrong / 100;
            }

            foreach (var ws in package.Workbook.Worksheets)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            return await Task.FromResult(package.GetAsByteArray());
        }

        /// <summary>
        /// Xuất bảng cân đối số phát sinh ra Excel.
        /// </summary>
        public async Task<byte[]> XuatBangCanDoiSoPhatSinhExcelAsync(DateTime from, DateTime to)
        {
            var soCaiService = new SoCaiService(_context);
            var canDoi = await soCaiService.GetBangCanDoiAsync(from, to);

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Bảng CĐSPS");

            sheet.Cells[1, 1].Value = $"BẢNG CÂN ĐỐI SỐ PHÁT SINH — {from:MM/yyyy}";
            sheet.Cells[1, 1, 1, 8].Merge = true;
            sheet.Cells[1, 1].Style.Font.Size = 14;
            sheet.Cells[1, 1].Style.Font.Bold = true;

            var headers = new[] { "Số TK", "Tên TK", "Dư ĐK Nợ", "Dư ĐK Có", "PS Nợ", "PS Có", "Dư CK Nợ", "Dư CK Có" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, 1 + i].Value = headers[i];
                sheet.Cells[3, 1 + i].Style.Font.Bold = true;
                sheet.Cells[3, 1 + i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, 1 + i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
                sheet.Cells[3, 1 + i].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            for (int i = 0; i < canDoi.Count; i++)
            {
                var row = canDoi[i];
                sheet.Cells[4 + i, 1].Value = row.MaTK;
                sheet.Cells[4 + i, 2].Value = row.TenTK;
                sheet.Cells[4 + i, 3].Value = (double)row.DuDauKyNo;
                sheet.Cells[4 + i, 4].Value = (double)row.DuDauKyCo;
                sheet.Cells[4 + i, 5].Value = (double)row.PhatSinhNo;
                sheet.Cells[4 + i, 6].Value = (double)row.PhatSinhCo;
                sheet.Cells[4 + i, 7].Value = (double)row.DuCuoiKyNo;
                sheet.Cells[4 + i, 8].Value = (double)row.DuCuoiKyCo;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }

        /// <summary>
        /// Xuất sổ cái một tài khoản ra Excel.
        /// </summary>
        public async Task<byte[]> XuatSoCaiExcelAsync(string maTK, DateTime from, DateTime to)
        {
            var soCaiService = new SoCaiService(_context);
            var danhSach = await soCaiService.GetTheoTKAsync(maTK, from, to);
            var tk = await _context.TaiKhoanKeToans.FirstOrDefaultAsync(t => t.MaTK == maTK);

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add($"TK {maTK}");

            sheet.Cells[1, 1].Value = $"SỔ CÁI TK {maTK} — {tk?.TenTK ?? ""}";
            sheet.Cells[2, 1].Value = $"Từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}";
            sheet.Cells[1, 1, 2, 6].Style.Font.Bold = true;

            var headers = new[] { "Ngày", "Số CT", "Diễn giải", "Đối tượng", "Nợ", "Có" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[4, 1 + i].Value = headers[i];
                sheet.Cells[4, 1 + i].Style.Font.Bold = true;
                sheet.Cells[4, 1 + i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[4, 1 + i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
                sheet.Cells[4, 1 + i].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            for (int i = 0; i < danhSach.Count; i++)
            {
                var sc = danhSach[i];
                sheet.Cells[5 + i, 1].Value = sc.NgayHachToan;
                sheet.Cells[5 + i, 1].Style.Numberformat.Format = "dd/MM/yyyy";
                sheet.Cells[5 + i, 2].Value = sc.SoChungTu;
                sheet.Cells[5 + i, 3].Value = sc.DienGiai;
                sheet.Cells[5 + i, 4].Value = sc.DoiTuong;
                if (sc.SoTienNo > 0) sheet.Cells[5 + i, 5].Value = (double)sc.SoTienNo;
                if (sc.SoTienCo > 0) sheet.Cells[5 + i, 6].Value = (double)sc.SoTienCo;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }

        /// <summary>
        /// Xuất danh sách công nợ ra Excel.
        /// </summary>
        public async Task<byte[]> XuatDanhSachCongNoExcelAsync()
        {
            var congNo = await _context.CongNoKhachHangs
                .Include(c => c.Customer)
                .OrderByDescending(c => c.SoNgayQuaHan)
                .ToListAsync();

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Công nợ phải thu");

            var headers = new[] { "Mã KH", "Tên KH", "Số HĐ", "Ngày HĐ", "Ngày ĐH", "Phải thu", "Đã thu", "Còn lại", "Số ngày QH", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, 1 + i].Value = headers[i];
                sheet.Cells[1, 1 + i].Style.Font.Bold = true;
                sheet.Cells[1, 1 + i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[1, 1 + i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
                sheet.Cells[1, 1 + i].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            for (int i = 0; i < congNo.Count; i++)
            {
                var cn = congNo[i];
                sheet.Cells[2 + i, 1].Value = cn.Customer?.MaKH ?? "";
                sheet.Cells[2 + i, 2].Value = cn.Customer?.Name ?? "";
                sheet.Cells[2 + i, 3].Value = cn.Invoice?.InvoiceNo ?? "";
                sheet.Cells[2 + i, 4].Value = cn.NgayHoaDon;
                sheet.Cells[2 + i, 4].Style.Numberformat.Format = "dd/MM/yyyy";
                sheet.Cells[2 + i, 5].Value = cn.NgayDenHan;
                sheet.Cells[2 + i, 5].Style.Numberformat.Format = "dd/MM/yyyy";
                sheet.Cells[2 + i, 6].Value = (double)cn.SoTienPhaiThu;
                sheet.Cells[2 + i, 7].Value = (double)cn.SoTienDaThu;
                sheet.Cells[2 + i, 8].Value = (double)cn.SoTienConLai;
                sheet.Cells[2 + i, 9].Value = cn.SoNgayQuaHan;
                sheet.Cells[2 + i, 10].Value = cn.TrangThai;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }

        /// <summary>
        /// Xuất audit log ra Excel.
        /// </summary>
        public async Task<byte[]> XuatAuditLogExcelAsync(DateTime from, DateTime to)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ThoiGian >= from.Date && a.ThoiGian <= to.Date.AddDays(1))
                .OrderByDescending(a => a.ThoiGian)
                .Include(a => a.User)
                .ToListAsync();

            using var package = new ExcelPackage();

            // Sheet 1: Dữ liệu chi tiết
            var sheet1 = package.Workbook.Worksheets.Add("Nhật ký");
            var headers1 = new[] { "Thời gian", "Người dùng", "Hành động", "Bảng", "ID", "Mô tả" };
            for (int i = 0; i < headers1.Length; i++)
            {
                sheet1.Cells[1, 1 + i].Value = headers1[i];
                sheet1.Cells[1, 1 + i].Style.Font.Bold = true;
                sheet1.Cells[1, 1 + i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet1.Cells[1, 1 + i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
                sheet1.Cells[1, 1 + i].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                sheet1.Cells[2 + i, 1].Value = log.ThoiGian;
                sheet1.Cells[2 + i, 1].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                sheet1.Cells[2 + i, 2].Value = log.NguoiThucHien;
                sheet1.Cells[2 + i, 3].Value = log.HanhDong;
                sheet1.Cells[2 + i, 4].Value = log.TenBang;
                sheet1.Cells[2 + i, 5].Value = log.IdBanGhi;
                sheet1.Cells[2 + i, 6].Value = log.DuLieuMoi ?? "";
            }

            sheet1.Cells[sheet1.Dimension.Address].AutoFitColumns();
            return await Task.FromResult(package.GetAsByteArray());
        }

        #endregion

        #region PDF

        /// <summary>
        /// Xuất báo giá ra PDF.
        /// </summary>
        #endregion

        #region HELPER

        /// <summary>
        /// Convert số tiền sang chữ tiếng Việt.
        /// </summary>
        public static string SoTienBangChu(decimal soTien)
        {
            if (soTien == 0) return "Không đồng";
            var units = new[] { "", "nghìn", "triệu", "tỷ" };
            var digits = new[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            string result = "";
            long num = (long)soTien;
            int unitIndex = 0;
            while (num > 0)
            {
                if (num % 1000 != 0 || result == "")
                {
                    int n = (int)(num % 1000);
                    string s = "";
                    int tram = n / 100;
                    int chuc = (n % 100) / 10;
                    int donvi = n % 10;
                    if (tram > 0) s += digits[tram] + " trăm ";
                    if (chuc > 0) s += digits[chuc] + " mươi ";
                    if (donvi > 0) s += digits[donvi] + " ";
                    result = s + units[unitIndex] + " " + result;
                }
                num /= 1000;
                unitIndex++;
            }
            return (result.Trim() + " đồng").Replace("  ", " ");
        }

        #endregion
    }
}
