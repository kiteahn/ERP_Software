using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.DTOs;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service xử lý các thao tác liên quan đến Báo giá (Quotation).
    /// Quản lý tạo mới, cập nhật trạng thái, và các thao tác khác trên báo giá.
    /// </summary>
    public class QuotationService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Danh sách các trạng thái hợp lệ của báo giá.
        /// </summary>
        public static readonly string[] TrangThaiHienThi = new[]
        {
            "Chờ duyệt", "Đã duyệt", "Đã gửi khách", "Khách duyệt",
            "Đã ký", "Đang sản xuất", "Hoàn thành", "Từ chối", "Hủy"
        };

        /// <summary>
        /// Bảng luật chuyển trạng thái: key = trạng thái hiện tại, value = danh sách trạng thái được phép chuyển đến.
        /// </summary>
        private static readonly Dictionary<string, string[]> LuongTrangThai = new()
        {
            { "Chờ duyệt", new[] { "Đã duyệt", "Từ chối" } },
            { "Đã duyệt", new[] { "Đã gửi khách", "Hủy" } },
            { "Đã gửi khách", new[] { "Khách duyệt", "Từ chối" } },
            { "Khách duyệt", new[] { "Đã ký", "Từ chối" } },
            { "Đã ký", new[] { "Đang sản xuất" } },
            { "Đang sản xuất", new[] { "Hoàn thành" } }
        };

        public QuotationService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Tạo báo giá mới với nhiều mức số lượng.
        /// </summary>
        /// <param name="dto">Thông tin báo giá từ client</param>
        /// <returns>Kết quả tạo báo giá</returns>
        /// <exception cref="ArgumentException">Khi dữ liệu không hợp lệ</exception>
        /// <exception cref="InvalidOperationException">Khi khách hàng nợ xấu</exception>
        public CreateQuotationResult TaoBaoGia(CreateQuotationDto dto)
        {
            var result = new CreateQuotationResult { IsSuccess = false };

            // ========== VALIDATE ==========
            if (dto.ChiTiet == null || dto.ChiTiet.Count == 0)
            {
                result.Errors.Add("Phải có ít nhất 1 mức số lượng");
                return result;
            }

            if (dto.UserId <= 0)
            {
                result.Errors.Add("UserId không hợp lệ");
                return result;
            }

            // Kiểm tra khách hàng không nợ xấu
            var customerService = new CustomerService(_context);
            var customer = _context.Customers
                .AsEnumerable()
                .FirstOrDefault(c => c.Name == dto.CustomerName);
            if (customer != null && customer.TrangThai == "Nợ xấu")
            {
                result.Errors.Add($"Khách hàng '{dto.CustomerName}' đang nợ xấu, không thể tạo báo giá");
                return result;
            }

            // Validate từng detail
            foreach (var detail in dto.ChiTiet)
            {
                if (detail.SoLuong <= 0)
                {
                    result.Errors.Add($"Số lượng phải lớn hơn 0");
                    return result;
                }
            }

            // Kiểm tra trùng số lượng
            var soLuongList = dto.ChiTiet.Select(d => d.SoLuong).ToList();
            if (soLuongList.Distinct().Count() != soLuongList.Count)
            {
                result.Errors.Add("Các mức số lượng không được trùng nhau");
                return result;
            }

            // ========== TẠO QUOTATION ==========
            var quotation = new Quotation
            {
                UserId = dto.UserId,
                QuoteNo = GenerateQuoteNo(),
                QuoteDate = DateTime.Now,
                NguoiTao = _context.Users.Find(dto.UserId)?.Username ?? "System",
                NgayTao = DateTime.Now,
                HieuLucNgay = dto.HieuLucNgay > 0 ? dto.HieuLucNgay : 30,
                ValidityDays = dto.HieuLucNgay > 0 ? dto.HieuLucNgay : 30,
                DeliveryDays = dto.DeliveryDays > 0 ? dto.DeliveryDays : 7,
                ThoiGianGiaoHangDuKien = dto.ThoiGianGiaoHangDuKien,
                TrangThai = "Chờ duyệt",
                GhiChu = dto.GhiChu,

                CustomerName = dto.CustomerName,
                CustomerAddress = dto.CustomerAddress,
                CustomerTaxCode = dto.CustomerTaxCode,

                ProductName = dto.ProductName,
                ProductDimensions = dto.ProductDimensions,
                SoCon = dto.SoCon,
                BuHao = dto.BuHao,

                PaperType = dto.PaperType,
                PaperGsm = dto.PaperGsm,
                PaperPricePerTon = dto.PaperPricePerTon,
                PrintLength = dto.PrintLength,
                PrintWidth = dto.PrintWidth,
                ColorCount = dto.ColorCount,

                IsLargeMachine = dto.IsLargeMachine,
                PlatePricePerColorLargeMachine = dto.PlatePricePerColorLargeMachine,
                PlatePricePerColorSmallMachine = dto.PlatePricePerColorSmallMachine,
                LaminationType = dto.LaminationType,
                LaminationSides = dto.LaminationSides,
                LaminationPrice = dto.LaminationPrice,

                DieCutMoldPrice = dto.DieCutMoldPrice,
                StringPricePerItem = dto.StringPricePerItem,
                ButtonPricePerItem = dto.ButtonPricePerItem,
                BoxPrice = dto.BoxPrice,
                DeliveryFee = dto.DeliveryFee,
                PrintProofFee = dto.PrintProofFee,

                ProfitMargin = dto.ProfitMargin
            };

            // ========== TẠO CHI TIẾT (DETAILS) ==========
            foreach (var detailDto in dto.ChiTiet)
            {
                // Tính TongGiaThanhSanXuat
                decimal tongGiaThanhSanXuat = detailDto.TinhTongGiaThanhSanXuat();

                // Tính GiaMoiCai = TongGiaThanhSanXuat / SoLuong * (1 + LoiNhuan/100)
                decimal giaMoiCai = (decimal)((double)tongGiaThanhSanXuat / detailDto.SoLuong * (1 + dto.ProfitMargin / 100));

                // Tính TongGiaBaoKhach = GiaMoiCai * SoLuong
                decimal tongGiaBaoKhach = giaMoiCai * detailDto.SoLuong;

                var detail = new QuotationDetail
                {
                    SoLuong = detailDto.SoLuong,
                    TienGiay = detailDto.TienGiay,
                    TienMuc = detailDto.TienMuc,
                    TienKem = detailDto.TienKem,
                    TienCanMang = detailDto.TienCanMang,
                    TienMetalize = detailDto.TienMetalize,
                    TienUV = detailDto.TienUV,
                    TienBe = detailDto.TienBe,
                    TienKhuonBe = detailDto.TienKhuonBe,
                    TienDan = detailDto.TienDan,
                    TienDay = detailDto.TienDay,
                    TienNut = detailDto.TienNut,
                    TienThung = detailDto.TienThung,
                    TienXeGiao = detailDto.TienXeGiao,
                    TienProof = detailDto.TienProof,
                    TongGiaThanhSanXuat = tongGiaThanhSanXuat,
                    GiaMoiCai = giaMoiCai,
                    GiaBaoKhach = (decimal)giaMoiCai, // GiaBaoKhach lưu giá 1 cái
                    TongGiaBaoKhach = tongGiaBaoKhach,
                    LaNucMucChinh = false
                };

                quotation.ChiTiet.Add(detail);
            }

            // ========== XÁC ĐỊNH MỨC CHÍNH (SoLuong lớn nhất) ==========
            var mucChinh = quotation.ChiTiet.OrderByDescending(c => c.SoLuong).First();
            mucChinh.LaNucMucChinh = true;

            // Tính tổng giá từ details vào quotation (dùng mức chính)
            quotation.TotalProductionCost = (double)mucChinh.TongGiaThanhSanXuat;
            quotation.QuotedUnitPrice = (double)mucChinh.GiaMoiCai;
            quotation.TotalOrderValue = (double)mucChinh.TongGiaBaoKhach;
            quotation.Quantity = mucChinh.SoLuong;

            // ========== LƯU VÀO DATABASE ==========
            _context.Quotations.Add(quotation);
            _context.SaveChanges();

            // Cập nhật IdMucChinh
            quotation.IdMucChinh = mucChinh.Id;
            _context.Quotations.Update(quotation);
            _context.SaveChanges();

            // ========== GHI AUDIT LOG ==========
            GhiAuditLog("CREATE", "Quotations", quotation.Id,
                null, $"Tạo báo giá {quotation.QuoteNo} với {dto.ChiTiet.Count} mức số lượng",
                quotation.NguoiTao);

            result.IsSuccess = true;
            result.QuotationId = quotation.Id;
            result.QuoteNo = quotation.QuoteNo;
            result.IdMucChinh = mucChinh.Id;
            result.Message = $"Tạo báo giá {quotation.QuoteNo} thành công";

            return result;
        }

        /// <summary>
        /// Cập nhật trạng thái báo giá với luồng trạng thái hợp lệ.
        /// </summary>
        /// <param name="quotationId">ID báo giá</param>
        /// <param name="trangThaiMoi">Trạng thái mới muốn chuyển đến</param>
        /// <param name="nguoiDung">Người dùng thực hiện</param>
        /// <exception cref="ArgumentException">Khi không tìm thấy báo giá hoặc luồng trạng thái không hợp lệ</exception>
        public void CapNhatTrangThai(int quotationId, string trangThaiMoi, string nguoiDung)
        {
            var quotation = _context.Quotations.Find(quotationId);
            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {quotationId}");
            }

            // Kiểm tra trạng thái mới có trong danh sách hợp lệ không
            if (!TrangThaiHienThi.Contains(trangThaiMoi))
            {
                throw new ArgumentException($"Trạng thái '{trangThaiMoi}' không hợp lệ. Các trạng thái hợp lệ: {string.Join(", ", TrangThaiHienThi)}");
            }

            string trangThaiCu = quotation.TrangThai;

            // Kiểm tra luồng trạng thái
            if (LuongTrangThai.TryGetValue(trangThaiCu, out var trangThaiDuocPhep))
            {
                if (!trangThaiDuocPhep.Contains(trangThaiMoi))
                {
                    throw new ArgumentException($"Không thể chuyển từ '{trangThaiCu}' sang '{trangThaiMoi}'. Các trạng thái được phép: {string.Join(", ", trangThaiDuocPhep)}");
                }
            }
            else
            {
                // Các trạng thái "Hoàn thành", "Từ chối", "Hủy" không thể chuyển đi đâu
                if (trangThaiCu == "Hoàn thành" || trangThaiCu == "Từ chối" || trangThaiCu == "Hủy")
                {
                    throw new ArgumentException($"Báo giá đang ở trạng thái '{trangThaiCu}', không thể thay đổi");
                }
            }

            string duLieuCu = $"{{\"TrangThai\": \"{trangThaiCu}\"}}";
            string duLieuMoi = $"{{\"TrangThai\": \"{trangThaiMoi}\"}}";

            // Cập nhật trạng thái
            quotation.TrangThai = trangThaiMoi;
            _context.Quotations.Update(quotation);
            _context.SaveChanges();

            // Ghi AuditLog
            GhiAuditLog("UPDATE", "Quotations", quotationId, duLieuCu, duLieuMoi, nguoiDung);

            // Nếu chuyển sang "Đang sản xuất" → tự động tạo ProductionOrder
            if (trangThaiMoi == "Đang sản xuất")
            {
                TaoProductionOrder(quotation, nguoiDung);
            }
        }

        /// <summary>
        /// Đặt mức số lượng chính cho báo giá.
        /// </summary>
        /// <param name="quotationId">ID báo giá</param>
        /// <param name="detailId">ID của detail sẽ là mức chính</param>
        /// <param name="nguoiDung">Người dùng thực hiện</param>
        /// <exception cref="ArgumentException">Khi không tìm thấy báo giá hoặc detail</exception>
        public void SetMucChinh(int quotationId, int detailId, string nguoiDung)
        {
            var quotation = _context.Quotations
                .Include(q => q.ChiTiet)
                .FirstOrDefault(q => q.Id == quotationId);

            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {quotationId}");
            }

            var detail = quotation.ChiTiet.FirstOrDefault(d => d.Id == detailId);
            if (detail == null)
            {
                throw new ArgumentException($"Không tìm thấy chi tiết báo giá với ID: {detailId}");
            }

            // Đặt tất cả detail về LaNucMucChinh = false
            foreach (var d in quotation.ChiTiet)
            {
                d.LaNucMucChinh = false;
            }

            // Đặt detail được chọn là mức chính
            detail.LaNucMucChinh = true;
            quotation.IdMucChinh = detailId;

            // Cập nhật thông tin tổng từ mức chính
            quotation.TotalProductionCost = (double)detail.TongGiaThanhSanXuat;
            quotation.QuotedUnitPrice = (double)detail.GiaMoiCai;
            quotation.TotalOrderValue = (double)detail.TongGiaBaoKhach;

            _context.SaveChanges();

            // Ghi AuditLog
            GhiAuditLog("UPDATE", "Quotations", quotationId,
                null, $"Đặt mức số lượng chính: {detail.SoLuong} cái",
                nguoiDung);
        }

        /// <summary>
        /// Lấy báo giá theo ID.
        /// </summary>
        public Quotation GetById(int id)
        {
            return _context.Quotations
                .Include(q => q.ChiTiet.OrderBy(c => c.SoLuong))
                .Include(q => q.User)
                .FirstOrDefault(q => q.Id == id);
        }

        /// <summary>
        /// Lấy danh sách báo giá với phân trang.
        /// </summary>
        public (List<Quotation> Items, int TotalCount) GetAll(int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Quotations
                .Include(q => q.ChiTiet)
                .Include(q => q.User)
                .AsQueryable();

            int totalCount = query.Count();
            var items = query
                .OrderByDescending(q => q.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        /// <summary>
        /// Tìm kiếm báo giá theo nhiều tiêu chí.
        /// </summary>
        public (List<Quotation> Items, int TotalCount) Search(
            string? keyword = null,
            string? trangThai = null,
            string? customerName = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _context.Quotations
                .Include(q => q.ChiTiet)
                .Include(q => q.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(q =>
                    q.QuoteNo.Contains(keyword) ||
                    q.CustomerName.Contains(keyword) ||
                    q.ProductName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(q => q.TrangThai == trangThai);
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                query = query.Where(q => q.CustomerName.Contains(customerName));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(q => q.NgayTao >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var toEndOfDay = toDate.Value.Date.AddDays(1);
                query = query.Where(q => q.NgayTao < toEndOfDay);
            }

            int totalCount = query.Count();
            var items = query
                .OrderByDescending(q => q.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        /// <summary>
        /// Xóa báo giá (soft delete hoặc hard delete tùy yêu cầu).
        /// </summary>
        public void Delete(int id, string nguoiXoa)
        {
            var quotation = _context.Quotations.Find(id);
            if (quotation == null)
            {
                throw new ArgumentException($"Không tìm thấy báo giá với ID: {id}");
            }

            // Không cho xóa nếu đang sản xuất hoặc đã hoàn thành
            if (quotation.TrangThai == "Đang sản xuất" || quotation.TrangThai == "Hoàn thành")
            {
                throw new InvalidOperationException($"Không thể xóa báo giá đang ở trạng thái '{quotation.TrangThai}'");
            }

            _context.Quotations.Remove(quotation);
            _context.SaveChanges();

            GhiAuditLog("DELETE", "Quotations", id, $"{{\"QuoteNo\": \"{quotation.QuoteNo}\"}}", null, nguoiXoa);
        }

        #region Private Helper Methods

        /// <summary>
        /// Tạo số báo giá tự động.
        /// </summary>
        private string GenerateQuoteNo()
        {
            string prefix = "BG-" + DateTime.Now.ToString("yyMMdd");
            var lastQuote = _context.Quotations
                .Where(q => q.QuoteNo.StartsWith(prefix))
                .OrderByDescending(q => q.QuoteNo)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastQuote != null)
            {
                string numberPart = lastQuote.QuoteNo.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D3}";
        }

        /// <summary>
        /// Tự động tạo ProductionOrder khi báo giá chuyển sang "Đang sản xuất".
        /// </summary>
        private void TaoProductionOrder(Quotation quotation, string nguoiDung)
        {
            var mucChinh = _context.QuotationDetails.Find(quotation.IdMucChinh);
            if (mucChinh == null)
            {
                mucChinh = _context.QuotationDetails
                    .Where(d => d.QuotationId == quotation.Id)
                    .OrderByDescending(d => d.SoLuong)
                    .First();
            }

            var productionOrder = new ProductionOrder
            {
                UserId = quotation.UserId,
                OrderNo = GenerateProductionOrderNo(),
                OrderDate = DateTime.Now,
                Deadline = DateTime.Now.AddDays(7),

                CustomerName = quotation.CustomerName,
                ProductName = quotation.ProductName,
                Quantity = mucChinh.SoLuong,
                Dimensions = quotation.ProductDimensions,
                Material = quotation.PaperType,

                // Copy thông số in từ báo giá
                PrintLength = quotation.PrintLength,
                PrintWidth = quotation.PrintWidth,
                ColorCount = quotation.ColorCount,
                IsLargeMachine = quotation.IsLargeMachine,
                PlatePricePerColorLarge = quotation.PlatePricePerColorLargeMachine,
                PlatePricePerColorSmall = quotation.PlatePricePerColorSmallMachine,
                LaminationType = quotation.LaminationType,
                LaminationSides = quotation.LaminationSides,
                LaminationPrice = quotation.LaminationPrice,
                PaperPricePerTon = quotation.PaperPricePerTon,
                SoCon = quotation.SoCon,
                BuHao = quotation.BuHao,

                Notes = $"Tạo từ báo giá {quotation.QuoteNo}",
                Status = "Chờ SX"
            };

            _context.ProductionOrders.Add(productionOrder);
            _context.SaveChanges();

            GhiAuditLog("CREATE", "ProductionOrders", productionOrder.Id,
                null, $"Tự động tạo từ báo giá {quotation.QuoteNo}", nguoiDung);
        }

        /// <summary>
        /// Tạo số lệnh sản xuất tự động.
        /// </summary>
        private string GenerateProductionOrderNo()
        {
            string prefix = "LSX-" + DateTime.Now.ToString("yyMMdd");
            var lastOrder = _context.ProductionOrders
                .Where(p => p.OrderNo.StartsWith(prefix))
                .OrderByDescending(p => p.OrderNo)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastOrder != null)
            {
                string numberPart = lastOrder.OrderNo.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D3}";
        }

        /// <summary>
        /// Ghi audit log.
        /// </summary>
        private void GhiAuditLog(string hanhDong, string tenBang, int idBanGhi,
            string duLieuCu, string duLieuMoi, string nguoiThucHien)
        {
            var auditLog = new AuditLog
            {
                ThoiGian = DateTime.Now,
                HanhDong = hanhDong,
                TenBang = tenBang,
                IdBanGhi = idBanGhi,
                DuLieuCu = duLieuCu,
                DuLieuMoi = duLieuMoi,
                NguoiThucHien = nguoiThucHien
            };

            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();
        }

        #endregion
    }
}
