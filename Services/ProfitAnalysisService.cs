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
    /// Service phân tích lợi nhuận báo giá - so sánh dự kiến vs thực tế sau sản xuất.
    /// </summary>
    public class ProfitAnalysisService
    {
        private readonly AppDbContext _context;

        public ProfitAnalysisService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Tính lợi nhuận thực tế

        /// <summary>
        /// Tính lợi nhuận thực tế của một báo giá.
        /// </summary>
        /// <param name="quotationId">ID báo giá</param>
        /// <returns>Kết quả phân tích lợi nhuận</returns>
        public ProfitAnalysisResult TinhLoiNhuanThucTe(int quotationId)
        {
            var quotation = _context.Quotations
                .Include(q => q.ChiTiet)
                .FirstOrDefault(q => q.Id == quotationId);

            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {quotationId}");
            }

            // Lấy mức chính
            var mucChinh = quotation.ChiTiet.FirstOrDefault(c => c.LaNucMucChinh)
                ?? quotation.ChiTiet.OrderByDescending(c => c.SoLuong).FirstOrDefault();

            if (mucChinh == null)
            {
                throw new InvalidOperationException("Báo giá không có chi tiết nào");
            }

            // Lấy chi phí phát sinh
            var listChiPhiPhatSinh = _context.ChiPhiPhatSinhThucTe
                .Where(c => c.QuotationId == quotationId)
                .ToList();

            // Tính toán
            decimal doanhThu = mucChinh.TongGiaBaoKhach;
            decimal chiPhiDuKien = mucChinh.TongGiaThanhSanXuat;
            decimal chiPhiPhatSinh = listChiPhiPhatSinh.Sum(c => c.ThanhTien);
            decimal tongChiPhiThucTe = chiPhiDuKien + chiPhiPhatSinh;
            decimal loiNhuanDuKien = doanhThu - chiPhiDuKien;
            decimal loiNhuanThucTe = doanhThu - tongChiPhiThucTe;
            decimal tyLeLoiNhuanDuKien = doanhThu > 0 ? loiNhuanDuKien / doanhThu * 100 : 0;
            decimal tyLeLoiNhuanThucTe = doanhThu > 0 ? loiNhuanThucTe / doanhThu * 100 : 0;
            decimal chenhLech = loiNhuanThucTe - loiNhuanDuKien;

            // Chi tiết theo loại chi phí phát sinh
            var chiTietTheoLoai = listChiPhiPhatSinh
                .GroupBy(c => c.Loai ?? "Khác")
                .Select(g => new ChiPhiLoaiDto
                {
                    Loai = g.Key,
                    SoKhoanMuc = g.Count(),
                    TongTien = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(c => c.TongTien)
                .ToList();

            return new ProfitAnalysisResult
            {
                QuotationId = quotationId,
                QuoteNo = quotation.QuoteNo,
                CustomerName = quotation.CustomerName,
                TrangThai = quotation.TrangThai,

                // Thông tin mức chính
                SoLuongDuKien = mucChinh.SoLuong,
                SoLuongThucTe = mucChinh.SoLuongThucTe,

                // Doanh thu
                DoanhThu = doanhThu,

                // Chi phí dự kiến
                ChiPhiDuKien = chiPhiDuKien,
                TienGiay = mucChinh.TienGiay,
                TienMuc = mucChinh.TienMuc,
                TienKem = mucChinh.TienKem,
                TienCanMang = mucChinh.TienCanMang,
                TienMetalize = mucChinh.TienMetalize,
                TienUV = mucChinh.TienUV,
                TienBe = mucChinh.TienBe,
                TienKhuonBe = mucChinh.TienKhuonBe,
                TienDan = mucChinh.TienDan,
                TienDay = mucChinh.TienDay,
                TienNut = mucChinh.TienNut,
                TienThung = mucChinh.TienThung,
                TienXeGiao = mucChinh.TienXeGiao,
                TienProof = mucChinh.TienProof,
                PhiGiaCongThucTe = mucChinh.PhiGiaCongThucTe ?? 0,

                // Chi phí phát sinh
                TongChiPhiPhatSinh = chiPhiPhatSinh,
                ChiTietPhatSinh = listChiPhiPhatSinh,
                ChiTietTheoLoai = chiTietTheoLoai,

                // Kết quả
                TongChiPhiThucTe = tongChiPhiThucTe,
                LoiNhuanDuKien = loiNhuanDuKien,
                LoiNhuanThucTe = loiNhuanThucTe,
                TyLeLoiNhuanDuKien = Math.Round(tyLeLoiNhuanDuKien, 2),
                TyLeLoiNhuanThucTe = Math.Round(tyLeLoiNhuanThucTe, 2),
                ChenhLech = chenhLech,
                MucChenhLech = chenhLech >= 0 ? "Lãi hơn dự kiến" : "Lỗ hơn dự kiến",

                GhiChuGiaCong = mucChinh.GhiChuGiaCong
            };
        }

        #endregion

        #region So sánh dự kiến vs thực tế

        /// <summary>
        /// So sánh chi tiết từng khoản mục giữa dự kiến và thực tế.
        /// </summary>
        /// <param name="quotationId">ID báo giá</param>
        /// <returns>Danh sách so sánh từng khoản</returns>
        public List<SoSanhKhoanDto> SoSanhDuKienVsThucTe(int quotationId)
        {
            var quotation = _context.Quotations
                .Include(q => q.ChiTiet)
                .FirstOrDefault(q => q.Id == quotationId);

            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {quotationId}");
            }

            var mucChinh = quotation.ChiTiet.FirstOrDefault(c => c.LaNucMucChinh)
                ?? quotation.ChiTiet.OrderByDescending(c => c.SoLuong).FirstOrDefault();

            if (mucChinh == null)
            {
                throw new InvalidOperationException("Báo giá không có chi tiết nào");
            }

            var chiPhiPhatSinh = _context.ChiPhiPhatSinhThucTe
                .Where(c => c.QuotationId == quotationId)
                .ToList();

            var result = new List<SoSanhKhoanDto>();

            // Thêm các khoản dự kiến
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Giấy", DuKien = mucChinh.TienGiay, ThucTe = 0, ChenLech = -mucChinh.TienGiay });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Mực/Kẽm", DuKien = mucChinh.TienMuc + mucChinh.TienKem, ThucTe = 0, ChenLech = -(mucChinh.TienMuc + mucChinh.TienKem) });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Cán màng", DuKien = mucChinh.TienCanMang, ThucTe = 0, ChenLech = -mucChinh.TienCanMang });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Metalize", DuKien = mucChinh.TienMetalize, ThucTe = 0, ChenLech = -mucChinh.TienMetalize });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "UV", DuKien = mucChinh.TienUV, ThucTe = 0, ChenLech = -mucChinh.TienUV });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Bế", DuKien = mucChinh.TienBe, ThucTe = 0, ChenLech = -mucChinh.TienBe });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Khuôn bế", DuKien = mucChinh.TienKhuonBe, ThucTe = 0, ChenLech = -mucChinh.TienKhuonBe });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Dán", DuKien = mucChinh.TienDan, ThucTe = 0, ChenLech = -mucChinh.TienDan });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Dây", DuKien = mucChinh.TienDay, ThucTe = 0, ChenLech = -mucChinh.TienDay });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Nút", DuKien = mucChinh.TienNut, ThucTe = 0, ChenLech = -mucChinh.TienNut });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Thùng", DuKien = mucChinh.TienThung, ThucTe = 0, ChenLech = -mucChinh.TienThung });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Xe giao", DuKien = mucChinh.TienXeGiao, ThucTe = 0, ChenLech = -mucChinh.TienXeGiao });
            result.Add(new SoSanhKhoanDto { KhoanMuc = "Proof", DuKien = mucChinh.TienProof, ThucTe = 0, ChenLech = -mucChinh.TienProof });

            // Thêm chi phí phát sinh vào thực tế
            foreach (var cp in chiPhiPhatSinh)
            {
                var existing = result.FirstOrDefault(r => r.KhoanMuc == cp.TenKhoanMuc);
                if (existing != null)
                {
                    existing.ThucTe += cp.ThanhTien;
                    existing.ChenLech += cp.ThanhTien;
                }
                else
                {
                    result.Add(new SoSanhKhoanDto
                    {
                        KhoanMuc = cp.TenKhoanMuc,
                        DuKien = 0,
                        ThucTe = cp.ThanhTien,
                        ChenLech = cp.ThanhTien,
                        Loai = cp.Loai,
                        GhiChu = cp.GhiChu
                    });
                }
            }

            // Tính lại chênh lệch cho các khoản có phát sinh
            foreach (var item in result.Where(r => r.Loai != null))
            {
                item.ChenLech = item.ThucTe - item.DuKien;
            }

            // Tổng cộng
            result.Add(new SoSanhKhoanDto
            {
                KhoanMuc = "TỔNG CỘNG",
                DuKien = mucChinh.TongGiaThanhSanXuat,
                ThucTe = mucChinh.TongGiaThanhSanXuat + chiPhiPhatSinh.Sum(c => c.ThanhTien),
                ChenLech = chiPhiPhatSinh.Sum(c => c.ThanhTien),
                IsTongCong = true
            });

            return result;
        }

        #endregion

        #region Đơn hàng lỗ

        /// <summary>
        /// Lấy danh sách đơn hàng có lợi nhuận thấp hoặc âm.
        /// Chỉ Admin và Giám đốc mới xem được.
        /// </summary>
        /// <param name="from">Ngày bắt đầu</param>
        /// <param name="to">Ngày kết thúc</param>
        /// <returns>Danh sách đơn lỗ</returns>
        public List<DonHangLoDto> DonHangLoNhieu(DateTime from, DateTime to)
        {
            // Kiểm tra quyền
            var currentUser = AppSession.CurrentUser;
            if (currentUser == null || !IsAdminOrGiamDoc(currentUser.Role))
            {
                throw new UnauthorizedAccessException("Chỉ Admin và Giám đốc mới được xem danh sách đơn lỗ");
            }

            // Lấy tất cả báo giá đã hoàn thành trong khoảng thời gian
            var quotations = _context.Quotations
                .Include(q => q.ChiTiet)
                .Where(q => q.NgayTao >= from.Date && q.NgayTao <= to.Date.AddDays(1).AddSeconds(-1))
                .Where(q => q.TrangThai == "Hoàn thành" || q.TrangThai == "Đang sản xuất")
                .ToList();

            var result = new List<DonHangLoDto>();

            foreach (var q in quotations)
            {
                var mucChinh = q.ChiTiet.FirstOrDefault(c => c.LaNucMucChinh);
                if (mucChinh == null) continue;

                var chiPhiPhatSinh = _context.ChiPhiPhatSinhThucTe
                    .Where(c => c.QuotationId == q.Id)
                    .Sum(c => c.ThanhTien);

                decimal doanhThu = mucChinh.TongGiaBaoKhach;
                decimal chiPhiThucTe = mucChinh.TongGiaThanhSanXuat + chiPhiPhatSinh + (mucChinh.PhiGiaCongThucTe ?? 0);
                decimal loiNhuanThucTe = doanhThu - chiPhiThucTe;
                decimal tyLeLoiNhuan = doanhThu > 0 ? loiNhuanThucTe / doanhThu * 100 : 0;

                // Kiểm tra điều kiện lỗ
                if (loiNhuanThucTe < 0 || tyLeLoiNhuan < 5)
                {
                    decimal loiNhuanDuKien = doanhThu - mucChinh.TongGiaThanhSanXuat;
                    decimal chenhLech = loiNhuanThucTe - loiNhuanDuKien;

                    result.Add(new DonHangLoDto
                    {
                        QuotationId = q.Id,
                        QuoteNo = q.QuoteNo,
                        CustomerName = q.CustomerName,
                        NgayTao = q.NgayTao,
                        NguoiTao = q.NguoiTao,
                        DoanhThu = doanhThu,
                        ChiPhiDuKien = mucChinh.TongGiaThanhSanXuat,
                        ChiPhiPhatSinh = chiPhiPhatSinh,
                        PhiGiaCongThucTe = mucChinh.PhiGiaCongThucTe ?? 0,
                        TongChiPhi = chiPhiThucTe,
                        LoiNhuanDuKien = loiNhuanDuKien,
                        LoiNhuanThucTe = loiNhuanThucTe,
                        TyLeLoiNhuan = Math.Round(tyLeLoiNhuan, 2),
                        MucDo = GetMucDoLo(tyLeLoiNhuan),
                        LyDo = mucChinh.GhiChuGiaCong
                    });
                }
            }

            return result.OrderBy(r => r.TyLeLoiNhuan).ToList();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng lỗ gần đây (trong 30 ngày).
        /// </summary>
        public List<DonHangLoDto> DonHangLoGanDay()
        {
            return DonHangLoNhieu(DateTime.Now.AddDays(-30), DateTime.Now);
        }

        #endregion

        #region Thêm chi phí phát sinh

        /// <summary>
        /// Thêm chi phí phát sinh cho báo giá.
        /// </summary>
        /// <param name="dto">Thông tin chi phí</param>
        /// <returns>Chi phí đã thêm</returns>
        public ChiPhiPhatSinhThucTe ThemChiPhi(AddChiPhiPhatSinhDto dto)
        {
            var quotation = _context.Quotations.Find(dto.QuotationId);
            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {dto.QuotationId}");
            }

            // Kiểm tra nếu có ProductionOrderId
            if (dto.ProductionOrderId.HasValue)
            {
                var prodOrder = _context.ProductionOrders.Find(dto.ProductionOrderId.Value);
                if (prodOrder == null)
                {
                    throw new ArgumentException($"Không tìm thấy lệnh sản xuất với ID: {dto.ProductionOrderId}");
                }
            }

            var chiPhi = new ChiPhiPhatSinhThucTe
            {
                QuotationId = dto.QuotationId,
                ProductionOrderId = dto.ProductionOrderId,
                TenKhoanMuc = dto.TenKhoanMuc,
                SoLuong = dto.SoLuong,
                DonGia = dto.DonGia,
                ThanhTien = dto.SoLuong * dto.DonGia,
                Loai = dto.Loai,
                NguoiNhap = dto.NguoiNhap,
                NgayNhap = DateTime.Now,
                GhiChu = dto.GhiChu
            };

            _context.ChiPhiPhatSinhThucTe.Add(chiPhi);
            _context.SaveChanges();

            // Cập nhật lại lợi nhuận thực tế trong QuotationDetail nếu có
            CapNhatLoiNhuanThucTe(dto.QuotationId);

            return chiPhi;
        }

        /// <summary>
        /// Cập nhật lợi nhuận thực tế cho mức chính của báo giá.
        /// </summary>
        /// <param name="quotationId">ID báo giá</param>
        public void CapNhatLoiNhuanThucTe(int quotationId)
        {
            var quotation = _context.Quotations
                .Include(q => q.ChiTiet)
                .FirstOrDefault(q => q.Id == quotationId);

            if (quotation == null) return;

            var mucChinh = quotation.ChiTiet.FirstOrDefault(c => c.LaNucMucChinh);
            if (mucChinh == null) return;

            var chiPhiPhatSinh = _context.ChiPhiPhatSinhThucTe
                .Where(c => c.QuotationId == quotationId)
                .Sum(c => c.ThanhTien);

            decimal tongChiPhi = mucChinh.TongGiaThanhSanXuat + chiPhiPhatSinh + (mucChinh.PhiGiaCongThucTe ?? 0);
            decimal loiNhuan = mucChinh.TongGiaBaoKhach - tongChiPhi;

            mucChinh.LoiNhuanThucTe = loiNhuan;
            _context.SaveChanges();
        }

        /// <summary>
        /// Xóa chi phí phát sinh.
        /// </summary>
        public bool XoaChiPhi(int chiPhiId, string nguoiYeuCau)
        {
            // Kiểm tra quyền (chỉ Admin/Giám đốc hoặc người tạo)
            var currentUser = AppSession.CurrentUser;
            if (currentUser == null || !IsAdminOrGiamDoc(currentUser.Role))
            {
                throw new UnauthorizedAccessException("Chỉ Admin và Giám đốc mới được xóa chi phí phát sinh");
            }

            var chiPhi = _context.ChiPhiPhatSinhThucTe.Find(chiPhiId);
            if (chiPhi == null) return false;

            int quotationId = chiPhi.QuotationId;

            _context.ChiPhiPhatSinhThucTe.Remove(chiPhi);
            _context.SaveChanges();

            // Cập nhật lại lợi nhuận thực tế
            CapNhatLoiNhuanThucTe(quotationId);

            return true;
        }

        #endregion

        #region Helper Methods

        private static bool IsAdminOrGiamDoc(string role)
        {
            return role == "Admin" || role == "Giám đốc";
        }

        private static string GetMucDoLo(decimal tyLeLoiNhuan)
        {
            if (tyLeLoiNhuan < 0) return "Nghiêm trọng";
            if (tyLeLoiNhuan < 3) return "Cao";
            return "Thấp";
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// Kết quả phân tích lợi nhuận.
    /// </summary>
    public class ProfitAnalysisResult
    {
        public int QuotationId { get; set; }
        public string QuoteNo { get; set; }
        public string CustomerName { get; set; }
        public string TrangThai { get; set; }

        public int SoLuongDuKien { get; set; }
        public int? SoLuongThucTe { get; set; }

        // Doanh thu
        public decimal DoanhThu { get; set; }

        // Chi phí dự kiến
        public decimal ChiPhiDuKien { get; set; }
        public decimal TienGiay { get; set; }
        public decimal TienMuc { get; set; }
        public decimal TienKem { get; set; }
        public decimal TienCanMang { get; set; }
        public decimal TienMetalize { get; set; }
        public decimal TienUV { get; set; }
        public decimal TienBe { get; set; }
        public decimal TienKhuonBe { get; set; }
        public decimal TienDan { get; set; }
        public decimal TienDay { get; set; }
        public decimal TienNut { get; set; }
        public decimal TienThung { get; set; }
        public decimal TienXeGiao { get; set; }
        public decimal TienProof { get; set; }
        public decimal PhiGiaCongThucTe { get; set; }

        // Chi phí phát sinh
        public decimal TongChiPhiPhatSinh { get; set; }
        public List<ChiPhiPhatSinhThucTe> ChiTietPhatSinh { get; set; }
        public List<ChiPhiLoaiDto> ChiTietTheoLoai { get; set; }

        // Kết quả
        public decimal TongChiPhiThucTe { get; set; }
        public decimal LoiNhuanDuKien { get; set; }
        public decimal LoiNhuanThucTe { get; set; }
        public decimal TyLeLoiNhuanDuKien { get; set; }
        public decimal TyLeLoiNhuanThucTe { get; set; }
        public decimal ChenhLech { get; set; }
        public string MucChenhLech { get; set; }
        public string GhiChuGiaCong { get; set; }
    }

    /// <summary>
    /// So sánh từng khoản dự kiến vs thực tế.
    /// </summary>
    public class SoSanhKhoanDto
    {
        public string KhoanMuc { get; set; }
        public decimal DuKien { get; set; }
        public decimal ThucTe { get; set; }
        public decimal ChenLech { get; set; }
        public string Loai { get; set; }
        public string GhiChu { get; set; }
        public bool IsTongCong { get; set; }
    }

    /// <summary>
    /// Chi tiết chi phí theo loại.
    /// </summary>
    public class ChiPhiLoaiDto
    {
        public string Loai { get; set; }
        public int SoKhoanMuc { get; set; }
        public decimal TongTien { get; set; }
    }

    /// <summary>
    /// Thông tin đơn hàng lỗ.
    /// </summary>
    public class DonHangLoDto
    {
        public int QuotationId { get; set; }
        public string QuoteNo { get; set; }
        public string CustomerName { get; set; }
        public DateTime NgayTao { get; set; }
        public string NguoiTao { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal ChiPhiDuKien { get; set; }
        public decimal ChiPhiPhatSinh { get; set; }
        public decimal PhiGiaCongThucTe { get; set; }
        public decimal TongChiPhi { get; set; }
        public decimal LoiNhuanDuKien { get; set; }
        public decimal LoiNhuanThucTe { get; set; }
        public decimal TyLeLoiNhuan { get; set; }
        public string MucDo { get; set; }
        public string LyDo { get; set; }
    }

    /// <summary>
    /// DTO thêm chi phí phát sinh.
    /// </summary>
    public class AddChiPhiPhatSinhDto
    {
        public int QuotationId { get; set; }
        public int? ProductionOrderId { get; set; }
        public string TenKhoanMuc { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string Loai { get; set; }
        public string NguoiNhap { get; set; }
        public string GhiChu { get; set; }
    }

    #endregion
}