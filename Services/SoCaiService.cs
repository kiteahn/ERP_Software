using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service quản lý sổ cái - truy vấn và tính số dư tài khoản.
    /// </summary>
    public class SoCaiService
    {
        private readonly AppDbContext _context;

        public SoCaiService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Lấy danh sách sổ cái theo tài khoản trong khoảng thời gian.
        /// </summary>
        public async Task<List<SoCai>> GetTheoTKAsync(string maTK, DateTime from, DateTime to)
        {
            return await _context.SoCais
                .Where(s => s.MaTK == maTK
                    && s.NgayHachToan >= from.Date
                    && s.NgayHachToan <= to.Date)
                .OrderBy(s => s.NgayHachToan)
                .ThenBy(s => s.Id)
                .Include(s => s.JournalBatch)
                .ToListAsync();
        }

        /// <summary>
        /// Tính số dư cuối kỳ của một tài khoản.
        /// </summary>
        /// <param name="maTK">Mã tài khoản</param>
        /// <param name="denNgay">Ngày tính số dư</param>
        /// <returns>Số dư cuối kỳ (dương = Nợ, âm = Có)</returns>
        public async Task<decimal> GetSoDuCuoiKyAsync(string maTK, DateTime denNgay)
        {
            var tk = await _context.TaiKhoanKeToans
                .FirstOrDefaultAsync(t => t.MaTK == maTK);

            if (tk == null) return 0;

            var soDuNo = await _context.SoCais
                .Where(s => s.MaTK == maTK && s.NgayHachToan <= denNgay.Date)
                .SumAsync(s => s.SoTienNo);

            var soDuCo = await _context.SoCais
                .Where(s => s.MaTK == maTK && s.NgayHachToan <= denNgay.Date)
                .SumAsync(s => s.SoTienCo);

            // TK tăng bên Nợ (Tài sản, Chi phí): Số dư = Nợ - Có
            // TK tăng bên Có (Nguồn vốn, Doanh thu): Số dư = Có - Nợ
            if (tk.LoTang)
            {
                return soDuNo - soDuCo;
            }
            else
            {
                return soDuCo - soDuNo;
            }
        }

        /// <summary>
        /// Lấy bảng cân đối số phát sinh trong kỳ.
        /// </summary>
        public async Task<List<CanDoiPhatSinhDto>> GetBangCanDoiAsync(DateTime from, DateTime to)
        {
            var result = new List<CanDoiPhatSinhDto>();
            var taiKhoans = await _context.TaiKhoanKeToans
                .Where(t => t.IsActive)
                .OrderBy(t => t.MaTK)
                .ToListAsync();

            foreach (var tk in taiKhoans)
            {
                // Số dư đầu kỳ
                decimal duDauNo = 0, duDauCo = 0;
                var duDauKy = await GetSoDuCuoiKyAsync(tk.MaTK, from.AddDays(-1));
                if (tk.LoTang)
                {
                    if (duDauKy >= 0) duDauNo = duDauKy;
                    else duDauCo = Math.Abs(duDauKy);
                }
                else
                {
                    if (duDauKy >= 0) duDauCo = duDauKy;
                    else duDauNo = Math.Abs(duDauKy);
                }

                // Phát sinh trong kỳ
                var psNo = await _context.SoCais
                    .Where(s => s.MaTK == tk.MaTK
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienNo);

                var psCo = await _context.SoCais
                    .Where(s => s.MaTK == tk.MaTK
                        && s.NgayHachToan >= from.Date
                        && s.NgayHachToan <= to.Date)
                    .SumAsync(s => s.SoTienCo);

                // Số dư cuối kỳ
                decimal duCuoiNo = 0, duCuoiCo = 0;
                var duCuoiKy = await GetSoDuCuoiKyAsync(tk.MaTK, to);
                if (tk.LoTang)
                {
                    if (duCuoiKy >= 0) duCuoiNo = duCuoiKy;
                    else duCuoiCo = Math.Abs(duCuoiKy);
                }
                else
                {
                    if (duCuoiKy >= 0) duCuoiCo = duCuoiKy;
                    else duCuoiNo = Math.Abs(duCuoiKy);
                }

                // Chỉ thêm nếu có phát sinh hoặc có số dư
                if (duDauNo > 0 || duDauCo > 0 || psNo > 0 || psCo > 0 || duCuoiNo > 0 || duCuoiCo > 0)
                {
                    result.Add(new CanDoiPhatSinhDto
                    {
                        MaTK = tk.MaTK,
                        TenTK = tk.TenTK,
                        LoaiTK = tk.LoaiTK,
                        DuDauKyNo = duDauNo,
                        DuDauKyCo = duDauCo,
                        PhatSinhNo = psNo,
                        PhatSinhCo = psCo,
                        DuCuoiKyNo = duCuoiNo,
                        DuCuoiKyCo = duCuoiCo
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy tổng phát sinh Nợ và Có trong kỳ.
        /// </summary>
        public async Task<(decimal TongNo, decimal TongCo)> GetTongPhatSinhAsync(DateTime from, DateTime to)
        {
            var tongNo = await _context.SoCais
                .Where(s => s.NgayHachToan >= from.Date && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienNo);

            var tongCo = await _context.SoCais
                .Where(s => s.NgayHachToan >= from.Date && s.NgayHachToan <= to.Date)
                .SumAsync(s => s.SoTienCo);

            return (tongNo, tongCo);
        }

        /// <summary>
        /// Lấy lịch sử hạch toán của một đối tượng.
        /// </summary>
        public async Task<List<SoCai>> GetTheoDoiTuongAsync(string doiTuong, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.SoCais.Where(s => s.DoiTuong == doiTuong);

            if (from.HasValue)
                query = query.Where(s => s.NgayHachToan >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(s => s.NgayHachToan <= to.Value.Date);

            return await query
                .OrderByDescending(s => s.NgayHachToan)
                .Include(s => s.JournalBatch)
                .ToListAsync();
        }
    }

    /// <summary>
    /// DTO bảng cân đối số phát sinh.
    /// </summary>
    public class CanDoiPhatSinhDto
    {
        public string MaTK { get; set; } = "";
        public string TenTK { get; set; } = "";
        public string LoaiTK { get; set; } = "";
        public decimal DuDauKyNo { get; set; }
        public decimal DuDauKyCo { get; set; }
        public decimal PhatSinhNo { get; set; }
        public decimal PhatSinhCo { get; set; }
        public decimal DuCuoiKyNo { get; set; }
        public decimal DuCuoiKyCo { get; set; }
    }
}
