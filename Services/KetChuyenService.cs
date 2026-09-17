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
    /// Service kết chuyển cuối kỳ kế toán.
    /// Thực hiện kết chuyển doanh thu, chi phí, xác định kết quả kinh doanh.
    /// </summary>
    public class KetChuyenService
    {
        private readonly AppDbContext _context;
        private readonly AccountingService _accountingService;
        private readonly SoCaiService _soCaiService;

        public KetChuyenService(
            AppDbContext context,
            AccountingService accountingService,
            SoCaiService soCaiService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _accountingService = accountingService ?? throw new ArgumentNullException(nameof(accountingService));
            _soCaiService = soCaiService ?? throw new ArgumentNullException(nameof(soCaiService));
        }

        /// <summary>
        /// Kiểm tra xem kỳ này đã kết chuyển chưa.
        /// </summary>
        private bool DaKetChuyenTrongKy(DateTime kyBatDau, DateTime kyKetThuc)
        {
            var tuBatDau = kyBatDau.Date;
            var denKetThuc = kyKetThuc.Date.AddDays(1);

            return _context.AuditLogs
                .Any(a => a.HanhDong == "KETCUYEN"
                    && a.ThoiGian >= tuBatDau
                    && a.ThoiGian < denKetThuc);
        }

        /// <summary>
        /// Kiểm tra người dùng có quyền kết chuyển không.
        /// </summary>
        private bool CoQuyenKetChuyen()
        {
            var user = Helpers.AppSession.CurrentUser;
            if (user == null) return false;
            return user.Role == "Admin" || user.Role == "Kế toán";
        }

        /// <summary>
        /// Thực hiện kết chuyển cuối kỳ.
        /// </summary>
        /// <param name="kyBatDau">Ngày bắt đầu kỳ</param>
        /// <param name="kyKetThuc">Ngày kết thúc kỳ</param>
        /// <param name="nguoiThucHien">Người thực hiện</param>
        /// <returns>Danh sách batch đã tạo + kết quả kết chuyển</returns>
        public async Task<KetChuyenResult> KetChuyenCuoiKyAsync(
            DateTime kyBatDau,
            DateTime kyKetThuc,
            string nguoiThucHien)
        {
            // Validate quyền
            if (!CoQuyenKetChuyen())
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền thực hiện kết chuyển cuối kỳ. Chỉ Admin và Kế toán được phép.");
            }

            // Validate kỳ
            if (kyBatDau > kyKetThuc)
            {
                throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");
            }

            // Kiểm tra đã kết chuyển chưa
            if (DaKetChuyenTrongKy(kyBatDau, kyKetThuc))
            {
                throw new InvalidOperationException(
                    $"Kỳ {kyBatDau:MM/yyyy} đã được kết chuyển rồi. Không thể kết chuyển trùng kỳ.");
            }

            var batches = new List<AccountingJournalBatch>();
            var now = DateTime.Now;

            // BƯỚC 1: Lấy số dư cuối kỳ các TK
            decimal dt511 = await _soCaiService.GetSoDuCuoiKyAsync("511", kyKetThuc);
            decimal dt515 = await _soCaiService.GetSoDuCuoiKyAsync("515", kyKetThuc);
            decimal dt521 = await _soCaiService.GetSoDuCuoiKyAsync("521", kyKetThuc);
            decimal cp632 = await _soCaiService.GetSoDuCuoiKyAsync("632", kyKetThuc);
            decimal cp635 = await _soCaiService.GetSoDuCuoiKyAsync("635", kyKetThuc);
            decimal cp641 = await _soCaiService.GetSoDuCuoiKyAsync("641", kyKetThuc);
            decimal cp642 = await _soCaiService.GetSoDuCuoiKyAsync("642", kyKetThuc);

            // BƯỚC 2: Kết chuyển doanh thu 511
            if (dt511 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_DOANH_THU", 0, dt511, "KetChuyen", "KC_DOANH_THU");
                batches.Add(batch);
            }

            // BƯỚC 3: Kết chuyển DT tài chính 515
            if (dt515 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_DOANH_THU_TAI_CHINH", 0, dt515, "KetChuyen", "KC_DOANH_THU_TAI_CHINH");
                batches.Add(batch);
            }

            // BƯỚC 4: Kết chuyển giảm trừ 521
            if (dt521 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_GIAM_TRU", 0, dt521, "KetChuyen", "KC_GIAM_TRU");
                batches.Add(batch);
            }

            // BƯỚC 5: Kết chuyển giá vốn 632
            if (cp632 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_GIA_VON", 0, cp632, "KetChuyen", "KC_GIA_VON");
                batches.Add(batch);
            }

            // BƯỚC 6: Kết chuyển CP tài chính 635
            if (cp635 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_CP_TAI_CHINH", 0, cp635, "KetChuyen", "KC_CP_TAI_CHINH");
                batches.Add(batch);
            }

            // BƯỚC 7: Kết chuyển CP bán hàng 641
            if (cp641 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_CP_BH", 0, cp641, "KetChuyen", "KC_CP_BH");
                batches.Add(batch);
            }

            // BƯỚC 8: Kết chuyển CP QLDN 642
            if (cp642 > 0)
            {
                var batch = await _accountingService.TriggerKeToanAsync(
                    "KC_CP_QLDN", 0, cp642, "KetChuyen", "KC_CP_QLDN");
                batches.Add(batch);
            }

            // BƯỚC 9: Xác định kết quả kinh doanh
            decimal ck911 = await _soCaiService.GetSoDuCuoiKyAsync("911", kyKetThuc);
            string loaiKetQua = "";
            if (ck911 > 0)
            {
                // Lãi: Nợ 911 / Có 421
                var batch = await _accountingService.TriggerKeToanAsync(
                    "XAC_DINH_LAI", 0, ck911, "KetChuyen", "XAC_DINH_LAI");
                batches.Add(batch);
                loaiKetQua = "LÃI";
            }
            else if (ck911 < 0)
            {
                // Lỗ: Nợ 421 / Có 911
                var batch = await _accountingService.TriggerKeToanAsync(
                    "XAC_DINH_LO", 0, Math.Abs(ck911), "KetChuyen", "XAC_DINH_LO");
                batches.Add(batch);
                loaiKetQua = "LỖ";
            }
            else
            {
                loaiKetQua = "HÒA VỐN";
            }

            // Ghi audit log tổng hợp
            try
            {
                using var auditCtx = new AppDbContext();
                auditCtx.AuditLogs.Add(new AuditLog
                {
                    ThoiGian = now,
                    NguoiThucHien = nguoiThucHien,
                    UserId = Helpers.AppSession.CurrentUser?.Id,
                    HanhDong = "KETCUYEN",
                    TenBang = "KetChuyenCuoiKy",
                    IdBanGhi = 0,
                    DuLieuMoi = $"Kết chuyển {kyBatDau:MM/yyyy} — KQ: {loaiKetQua} {Math.Abs(ck911):N0}đ",
                    DiaChiIP = "system"
                });
                await auditCtx.SaveChangesAsync();
            }
            catch { }

            return new KetChuyenResult
            {
                Success = true,
                Batches = batches,
                TongDoanhThu = dt511 + dt515 - dt521,
                TongChiPhi = cp632 + cp635 + cp641 + cp642,
                KetQuaKinhDoanh = ck911,
                LoaiKetQua = loaiKetQua,
                KyBatDau = kyBatDau,
                KyKetThuc = kyKetThuc,
                NgayKetChuyen = now
            };
        }
    }

    /// <summary>
    /// Kết quả kết chuyển cuối kỳ.
    /// </summary>
    public class KetChuyenResult
    {
        public bool Success { get; set; }
        public List<AccountingJournalBatch> Batches { get; set; } = new();
        public decimal TongDoanhThu { get; set; }
        public decimal TongChiPhi { get; set; }
        public decimal KetQuaKinhDoanh { get; set; }
        public string LoaiKetQua { get; set; } = "";
        public DateTime KyBatDau { get; set; }
        public DateTime KyKetThuc { get; set; }
        public DateTime NgayKetChuyen { get; set; }
    }
}
