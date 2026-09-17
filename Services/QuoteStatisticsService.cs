using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service thống kê báo giá - cung cấp các báo cáo và thống kê về báo giá.
    /// </summary>
    public class QuoteStatisticsService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Các trạng thái được coi là "đã ký" (thành công).
        /// </summary>
        private static readonly string[] TrangThaiDaKy = { "Đã ký", "Đang sản xuất", "Hoàn thành" };

        /// <summary>
        /// Các trạng thái được coi là "từ chối".
        /// </summary>
        private static readonly string[] TrangThaiTuChoi = { "Từ chối" };

        public QuoteStatisticsService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Thống kê tỷ lệ Win/Loss

        /// <summary>
        /// Thống kê tỷ lệ win của báo giá trong khoảng thời gian.
        /// </summary>
        /// <param name="from">Ngày bắt đầu</param>
        /// <param name="to">Ngày kết thúc</param>
        /// <param name="userId">Lọc theo nhân viên tạo (null = tất cả, chỉ Admin/Giám đốc)</param>
        /// <returns>Object thống kê tỷ lệ win</returns>
        public ThongKeTyLeWinResult ThongKeTyLeWin(DateTime from, DateTime to, int? userId = null)
        {
            // Nếu có userId → verify quyền
            if (userId.HasValue)
            {
                var user = _context.Users.Find(userId.Value);
                if (user != null && !IsAdminOrGiamDoc(user.Role))
                {
                    throw new UnauthorizedAccessException("Chỉ Admin và Giám đốc mới được xem thống kê theo nhân viên khác");
                }
            }

            var query = _context.Quotations
                .Where(q => q.NgayTao >= from.Date && q.NgayTao <= to.Date.AddDays(1).AddSeconds(-1));

            if (userId.HasValue)
            {
                query = query.Where(q => q.UserId == userId.Value);
            }

            var quotations = query.ToList();

            int tongBaoGia = quotations.Count;
            int daKy = quotations.Count(q => TrangThaiDaKy.Contains(q.TrangThai));
            int tuChoi = quotations.Count(q => TrangThaiTuChoi.Contains(q.TrangThai));
            int dangCho = tongBaoGia - daKy - tuChoi;

            decimal tyLeWin = tongBaoGia > 0 ? (decimal)daKy / tongBaoGia * 100 : 0;

            // Tổng giá trị các báo giá đã ký
            decimal tongGiaTriWin = quotations
                .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                .Sum(q => (decimal)q.TotalOrderValue);

            // Tổng giá trị các báo giá bị từ chối
            decimal tongGiaTriLoss = quotations
                .Where(q => TrangThaiTuChoi.Contains(q.TrangThai))
                .Sum(q => (decimal)q.TotalOrderValue);

            return new ThongKeTyLeWinResult
            {
                TongBaoGia = tongBaoGia,
                DaKy = daKy,
                TuChoi = tuChoi,
                DangCho = dangCho,
                TyLeWin = Math.Round(tyLeWin, 2),
                TongGiaTriWin = tongGiaTriWin,
                TongGiaTriLoss = tongGiaTriLoss
            };
        }

        #endregion

        #region Thống kê theo khách hàng

        /// <summary>
        /// Thống kê báo giá theo khách hàng.
        /// </summary>
        /// <param name="from">Ngày bắt đầu</param>
        /// <param name="to">Ngày kết thúc</param>
        /// <returns>Danh sách thống kê theo khách hàng</returns>
        public List<ThongKeKhachHangResult> ThongKeTheoKhachHang(DateTime from, DateTime to)
        {
            var quotations = _context.Quotations
                .Where(q => q.NgayTao >= from.Date && q.NgayTao <= to.Date.AddDays(1).AddSeconds(-1))
                .ToList();

            var result = quotations
                .GroupBy(q => q.CustomerName)
                .Select(g => new ThongKeKhachHangResult
                {
                    KhachHang = g.Key,
                    SoBaoGia = g.Count(),
                    SoWin = g.Count(q => TrangThaiDaKy.Contains(q.TrangThai)),
                    TyLeWin = g.Count() > 0
                        ? Math.Round((decimal)g.Count(q => TrangThaiDaKy.Contains(q.TrangThai)) / g.Count() * 100, 2)
                        : 0,
                    DoanhThu = g
                        .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                        .Sum(q => (decimal)q.TotalOrderValue)
                })
                .OrderByDescending(r => r.DoanhThu)
                .ToList();

            return result;
        }

        #endregion

        #region Thống kê theo nhân viên

        /// <summary>
        /// Thống kê báo giá theo nhân viên.
        /// Chỉ Admin và Giám đốc mới được xem.
        /// </summary>
        /// <param name="from">Ngày bắt đầu</param>
        /// <param name="to">Ngày kết thúc</param>
        /// <returns>Danh sách thống kê theo nhân viên</returns>
        public List<ThongKeNhanVienResult> ThongKeTheoNhanVien(DateTime from, DateTime to)
        {
            // Kiểm tra quyền
            var currentUser = AppSession.CurrentUser;
            if (currentUser == null || !IsAdminOrGiamDoc(currentUser.Role))
            {
                throw new UnauthorizedAccessException("Chỉ Admin và Giám đốc mới được xem thống kê theo nhân viên");
            }

            var quotations = _context.Quotations
                .Where(q => q.NgayTao >= from.Date && q.NgayTao <= to.Date.AddDays(1).AddSeconds(-1))
                .Include(q => q.User)
                .ToList();

            var result = quotations
                .GroupBy(q => q.NguoiTao)
                .Select(g => new ThongKeNhanVienResult
                {
                    NhanVien = g.Key,
                    SoBaoGia = g.Count(),
                    SoWin = g.Count(q => TrangThaiDaKy.Contains(q.TrangThai)),
                    TyLeWin = g.Count() > 0
                        ? Math.Round((decimal)g.Count(q => TrangThaiDaKy.Contains(q.TrangThai)) / g.Count() * 100, 2)
                        : 0,
                    DoanhThu = g
                        .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                        .Sum(q => (decimal)q.TotalOrderValue),
                    TyLeTongBaoGia = 0 // Sẽ tính sau
                })
                .OrderByDescending(r => r.DoanhThu)
                .ToList();

            // Tính tỷ lệ % trên tổng
            int tongBaoGia = result.Sum(r => r.SoBaoGia);
            if (tongBaoGia > 0)
            {
                foreach (var item in result)
                {
                    item.TyLeTongBaoGia = Math.Round((decimal)item.SoBaoGia / tongBaoGia * 100, 2);
                }
            }

            return result;
        }

        #endregion

        #region Thống kê theo tháng

        /// <summary>
        /// Thống kê báo giá theo tháng trong năm.
        /// </summary>
        /// <param name="year">Năm cần thống kê</param>
        /// <returns>Danh sách 12 tháng với số liệu</returns>
        public List<ThongKeThangResult> ThongKeTheoThang(int year)
        {
            var quotations = _context.Quotations
                .Where(q => q.NgayTao.Year == year)
                .ToList();

            var result = new List<ThongKeThangResult>();

            for (int month = 1; month <= 12; month++)
            {
                var monthData = quotations.Where(q => q.NgayTao.Month == month).ToList();

                result.Add(new ThongKeThangResult
                {
                    Thang = month,
                    TenThang = GetTenThang(month),
                    SoBaoGia = monthData.Count,
                    SoWin = monthData.Count(q => TrangThaiDaKy.Contains(q.TrangThai)),
                    TyLeWin = monthData.Count > 0
                        ? Math.Round((decimal)monthData.Count(q => TrangThaiDaKy.Contains(q.TrangThai)) / monthData.Count * 100, 2)
                        : 0,
                    DoanhThu = monthData
                        .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                        .Sum(q => (decimal)q.TotalOrderValue)
                });
            }

            return result;
        }

        /// <summary>
        /// Thống kê báo giá so sánh các tháng gần đây (3 tháng gần nhất).
        /// </summary>
        /// <returns>Danh sách tháng với số liệu</returns>
        public List<ThongKeThangResult> ThongKeThangGanNhat()
        {
            var now = DateTime.Now;
            var startMonth = now.AddMonths(-2);

            var quotations = _context.Quotations
                .Where(q => q.NgayTao >= new DateTime(startMonth.Year, startMonth.Month, 1)
                         && q.NgayTao <= now)
                .ToList();

            var result = new List<ThongKeThangResult>();

            for (int i = 0; i < 3; i++)
            {
                var targetDate = startMonth.AddMonths(i);
                var monthData = quotations.Where(q => q.NgayTao.Year == targetDate.Year
                                                  && q.NgayTao.Month == targetDate.Month).ToList();

                result.Add(new ThongKeThangResult
                {
                    Thang = targetDate.Month,
                    TenThang = GetTenThang(targetDate.Month),
                    SoBaoGia = monthData.Count,
                    SoWin = monthData.Count(q => TrangThaiDaKy.Contains(q.TrangThai)),
                    TyLeWin = monthData.Count > 0
                        ? Math.Round((decimal)monthData.Count(q => TrangThaiDaKy.Contains(q.TrangThai)) / monthData.Count * 100, 2)
                        : 0,
                    DoanhThu = monthData
                        .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                        .Sum(q => (decimal)q.TotalOrderValue)
                });
            }

            return result;
        }

        #endregion

        #region Báo giá sắp hết hạn

        /// <summary>
        /// Lấy danh sách báo giá sắp hết hạn gửi cho khách.
        /// </summary>
        /// <param name="soNgay">Số ngày trước khi hết hạn để cảnh báo</param>
        /// <returns>Danh sách báo giá sắp hết hạn</returns>
        public List<BaoGiaHetHanDto> BaoGiaSapHetHan(int soNgay = 3)
        {
            var cutoffDate = DateTime.Now.AddDays(soNgay);
            var now = DateTime.Now;

            var quotations = _context.Quotations
                .Include(q => q.User)
                .Where(q => q.TrangThai == "Đã gửi khách")
                .Where(q => q.NgayTao.AddDays(q.HieuLucNgay) <= cutoffDate)
                .OrderBy(q => q.NgayTao.AddDays(q.HieuLucNgay))
                .ToList();

            return quotations.Select(q => new BaoGiaHetHanDto
            {
                Id = q.Id,
                QuoteNo = q.QuoteNo,
                CustomerName = q.CustomerName,
                NguoiTao = q.NguoiTao,
                NgayTao = q.NgayTao,
                HieuLucNgay = q.HieuLucNgay,
                NgayHetHan = q.NgayTao.AddDays(q.HieuLucNgay),
                SoNgayConLai = Math.Max(0, (int)(q.NgayTao.AddDays(q.HieuLucNgay) - now).TotalDays),
                MucUutien = GetMucUutien((q.NgayTao.AddDays(q.HieuLucNgay) - now).TotalDays)
            }).ToList();
        }

        /// <summary>
        /// Lấy danh sách báo giá đã hết hạn.
        /// </summary>
        /// <returns>Danh sách báo giá đã hết hạn</returns>
        public List<BaoGiaHetHanDto> BaoGiaDaHetHan()
        {
            var now = DateTime.Now;

            var quotations = _context.Quotations
                .Include(q => q.User)
                .Where(q => q.TrangThai == "Đã gửi khách")
                .Where(q => q.NgayTao.AddDays(q.HieuLucNgay) < now)
                .OrderBy(q => q.NgayTao.AddDays(q.HieuLucNgay))
                .ToList();

            return quotations.Select(q => new BaoGiaHetHanDto
            {
                Id = q.Id,
                QuoteNo = q.QuoteNo,
                CustomerName = q.CustomerName,
                NguoiTao = q.NguoiTao,
                NgayTao = q.NgayTao,
                HieuLucNgay = q.HieuLucNgay,
                NgayHetHan = q.NgayTao.AddDays(q.HieuLucNgay),
                SoNgayConLai = -1 * (int)(now - q.NgayTao.AddDays(q.HieuLucNgay)).TotalDays,
                MucUutien = "Hết hạn"
            }).ToList();
        }

        #endregion

        #region Thống kê tổng hợp Dashboard

        /// <summary>
        /// Lấy tổng hợp các số liệu cho Dashboard.
        /// </summary>
        /// <param name="from">Ngày bắt đầu</param>
        /// <param name="to">Ngày kết thúc</param>
        /// <returns>Object tổng hợp</returns>
        public DashboardSummaryResult GetDashboardSummary(DateTime from, DateTime to)
        {
            var quotations = _context.Quotations
                .Where(q => q.NgayTao >= from.Date && q.NgayTao <= to.Date.AddDays(1).AddSeconds(-1))
                .ToList();

            var tongBaoGia = quotations.Count;
            var daKy = quotations.Count(q => TrangThaiDaKy.Contains(q.TrangThai));
            var tyLeWin = tongBaoGia > 0 ? Math.Round((decimal)daKy / tongBaoGia * 100, 2) : 0;
            var doanhThu = quotations
                .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                .Sum(q => (decimal)q.TotalOrderValue);

            // Báo giá đang chờ duyệt
            var choDuyet = quotations.Count(q => q.TrangThai == "Chờ duyệt");
            var daGuiKhach = quotations.Count(q => q.TrangThai == "Đã gửi khách");
            var khachDuyet = quotations.Count(q => q.TrangThai == "Khách duyệt");

            // Top 5 khách hàng
            var topKhachHang = quotations
                .Where(q => TrangThaiDaKy.Contains(q.TrangThai))
                .GroupBy(q => q.CustomerName)
                .Select(g => new { KhachHang = g.Key, DoanhThu = g.Sum(q => (decimal)q.TotalOrderValue) })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();

            // Báo giá sắp hết hạn
            var hetHan = BaoGiaSapHetHan(3).Count;

            return new DashboardSummaryResult
            {
                TongBaoGia = tongBaoGia,
                DaKy = daKy,
                TyLeWin = tyLeWin,
                DoanhThu = doanhThu,
                ChoDuyet = choDuyet,
                DaGuiKhach = daGuiKhach,
                KhachDuyet = khachDuyet,
                SoBaoGiaHetHan = hetHan,
                TopKhachHang = topKhachHang.Select(x => new TopKhachHangDto { Ten = x.KhachHang, DoanhThu = x.DoanhThu }).ToList()
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra user có phải là Admin hoặc Giám đốc.
        /// </summary>
        private static bool IsAdminOrGiamDoc(string role)
        {
            return role == "Admin" || role == "Giám đốc";
        }

        /// <summary>
        /// Lấy tên tháng.
        /// </summary>
        private static string GetTenThang(int month)
        {
            string[] tenThang = {
                "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
                "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"
            };
            return tenThang[month - 1];
        }

        /// <summary>
        /// Xác định mức ưu tiên dựa trên số ngày còn lại.
        /// </summary>
        private static string GetMucUutien(double soNgayConLai)
        {
            if (soNgayConLai <= 0) return "Hết hạn";
            if (soNgayConLai <= 1) return "Rất gấp";
            if (soNgayConLai <= 3) return "Gấp";
            return "Bình thường";
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// Kết quả thống kê tỷ lệ win.
    /// </summary>
    public class ThongKeTyLeWinResult
    {
        public int TongBaoGia { get; set; }
        public int DaKy { get; set; }
        public int TuChoi { get; set; }
        public int DangCho { get; set; }
        public decimal TyLeWin { get; set; }
        public decimal TongGiaTriWin { get; set; }
        public decimal TongGiaTriLoss { get; set; }
    }

    /// <summary>
    /// Kết quả thống kê theo khách hàng.
    /// </summary>
    public class ThongKeKhachHangResult
    {
        public string KhachHang { get; set; }
        public int SoBaoGia { get; set; }
        public int SoWin { get; set; }
        public decimal TyLeWin { get; set; }
        public decimal DoanhThu { get; set; }
    }

    /// <summary>
    /// Kết quả thống kê theo nhân viên.
    /// </summary>
    public class ThongKeNhanVienResult
    {
        public string NhanVien { get; set; }
        public int SoBaoGia { get; set; }
        public int SoWin { get; set; }
        public decimal TyLeWin { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal TyLeTongBaoGia { get; set; }
    }

    /// <summary>
    /// Kết quả thống kê theo tháng.
    /// </summary>
    public class ThongKeThangResult
    {
        public int Thang { get; set; }
        public string TenThang { get; set; }
        public int SoBaoGia { get; set; }
        public int SoWin { get; set; }
        public decimal TyLeWin { get; set; }
        public decimal DoanhThu { get; set; }
    }

    /// <summary>
    /// Thông tin báo giá sắp hết hạn.
    /// </summary>
    public class BaoGiaHetHanDto
    {
        public int Id { get; set; }
        public string QuoteNo { get; set; }
        public string CustomerName { get; set; }
        public string NguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
        public int HieuLucNgay { get; set; }
        public DateTime NgayHetHan { get; set; }
        public int SoNgayConLai { get; set; }
        public string MucUutien { get; set; }
    }

    /// <summary>
    /// Kết quả tổng hợp Dashboard.
    /// </summary>
    public class DashboardSummaryResult
    {
        public int TongBaoGia { get; set; }
        public int DaKy { get; set; }
        public decimal TyLeWin { get; set; }
        public decimal DoanhThu { get; set; }
        public int ChoDuyet { get; set; }
        public int DaGuiKhach { get; set; }
        public int KhachDuyet { get; set; }
        public int SoBaoGiaHetHan { get; set; }
        public List<TopKhachHangDto> TopKhachHang { get; set; }
    }

    /// <summary>
    /// Top khách hàng.
    /// </summary>
    public class TopKhachHangDto
    {
        public string Ten { get; set; }
        public decimal DoanhThu { get; set; }
    }

    #endregion
}