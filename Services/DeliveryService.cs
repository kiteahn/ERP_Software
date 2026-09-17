using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service quản lý giao hàng.
    /// </summary>
    public class DeliveryService
    {
        private readonly AppDbContext _context;

        public DeliveryService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Tạo phiếu giao hàng từ SalesOrder.
        /// </summary>
        /// <param name="salesOrderId">ID đơn hàng</param>
        /// <param name="dto">Thông tin giao hàng</param>
        /// <returns>ID phiếu giao vừa tạo</returns>
        public int TaoPhieuGiao(int salesOrderId, CreateDeliveryDto dto)
        {
            // 1. Validate SalesOrder
            var salesOrder = _context.SalesOrders.Find(salesOrderId);

            if (salesOrder == null)
                throw new ArgumentException($"Không tìm thấy đơn hàng ID: {salesOrderId}");

            // Kiểm tra trạng thái phải là "Hoàn thành sản xuất"
            if (salesOrder.Status != "Hoàn thành sản xuất")
                throw new InvalidOperationException($"Đơn hàng phải ở trạng thái 'Hoàn thành sản xuất' để tạo phiếu giao. Trạng thái hiện tại: {salesOrder.Status}");

            // 2. Sinh mã PGH tự động
            var maPGH = SinhMaPGH();

            // 3. Lấy thông tin địa chỉ từ SalesOrder
            var diaChiGiao = !string.IsNullOrEmpty(dto.DiaChiGiao) 
                ? dto.DiaChiGiao 
                : (!string.IsNullOrEmpty(salesOrder.Address) 
                    ? salesOrder.Address 
                    : "");

            var soDienThoai = !string.IsNullOrEmpty(dto.SoDienThoaiNhanHang) 
                ? dto.SoDienThoaiNhanHang 
                : salesOrder.Phone ?? "";

            // 4. Tạo phiếu giao hàng
            var phieuGiao = new PhieuGiaoHang
            {
                MaPGH = maPGH,
                SalesOrderId = salesOrderId,
                NgayGiao = dto.NgayGiao,
                NguoiGiao = dto.NguoiGiao,
                DiaChiGiao = diaChiGiao,
                SoDienThoaiNhanHang = soDienThoai,
                TrangThai = TrangThaiGiaoHang.ChoGiao,
                NguoiTao = dto.NguoiTao,
                NgayTao = DateTime.Now
            };

            _context.PhieuGiaoHangs.Add(phieuGiao);
            _context.SaveChanges();

            // 5. Tạo chi tiết phiếu giao hàng
            if (dto.ChiTiet != null && dto.ChiTiet.Any())
            {
                foreach (var item in dto.ChiTiet)
                {
                    var chiTiet = new ChiTietPhieuGiaoHang
                    {
                        PhieuGiaoHangId = phieuGiao.Id,
                        TenSanPham = item.TenSanPham,
                        SoLuong = item.SoLuong,
                        DonViTinh = item.DonViTinh,
                        GhiChu = item.GhiChu
                    };
                    _context.ChiTietPhieuGiaoHangs.Add(chiTiet);
                }
            }
            else
            {
                // Tạo chi tiết mặc định từ thông tin đơn hàng
                var chiTiet = new ChiTietPhieuGiaoHang
                {
                    PhieuGiaoHangId = phieuGiao.Id,
                    TenSanPham = salesOrder.ProductName,
                    SoLuong = (int)salesOrder.Quantity,
                    DonViTinh = "Cái",
                    GhiChu = null
                };
                _context.ChiTietPhieuGiaoHangs.Add(chiTiet);
            }

            _context.SaveChanges();

            // 6. Gửi thông báo cho người giao hàng
            GuiThongBaoNguoiGiao(phieuGiao);

            // 7. Ghi AuditLog
            GhiAuditLog("CREATE", $"Tạo PGH {maPGH} cho đơn hàng {salesOrder.OrderNo}", dto.NguoiTao, phieuGiao.Id);

            return phieuGiao.Id;
        }

        /// <summary>
        /// Xác nhận đã giao hàng thành công.
        /// </summary>
        public void XacNhanDaGiao(int pghId, XacNhanGiaoDto dto)
        {
            var pgh = _context.PhieuGiaoHangs
                .Include(p => p.SalesOrder)
                .FirstOrDefault(p => p.Id == pghId);

            if (pgh == null)
                throw new ArgumentException($"Không tìm thấy phiếu giao hàng ID: {pghId}");

            if (pgh.TrangThai == TrangThaiGiaoHang.DaGiao)
                throw new InvalidOperationException("Phiếu giao hàng đã được xác nhận trước đó.");

            var so = pgh.SalesOrder;

            // 1. Cập nhật trạng thái
            pgh.TrangThai = TrangThaiGiaoHang.DaGiao;
            pgh.NgayGiaoThucTe = dto.NgayGiaoThucTe ?? DateTime.Now;
            pgh.NguoiNhanHang = dto.NguoiNhanHang;
            pgh.GhiChuGiao = dto.GhiChu;

            if (!string.IsNullOrEmpty(dto.HinhAnhXacNhan))
                pgh.HinhAnhXacNhan = dto.HinhAnhXacNhan;

            // 2. Tự động tạo Invoice nếu chưa có (dựa vào RefOrderNo)
            var invoice = _context.Invoices
                .FirstOrDefault(i => i.RefOrderNo == so.OrderNo);

            int invoiceId = 0;
            decimal tongTien = (decimal)so.TotalAmount;

            if (invoice == null)
            {
                var maInvoice = SinhMaInvoice();

                invoice = new Invoice
                {
                    InvoiceNo = maInvoice,
                    InvoiceDate = DateTime.Now,
                    RefOrderNo = so.OrderNo,
                    CustomerName = so.CustomerName,
                    TaxCode = so.TaxCode,
                    SubTotal = so.TotalAmount,
                    VATPercent = 10,
                    TotalAmount = so.TotalAmount * 1.1,
                    DueDate = DateTime.Now.AddDays(so.CreditDays),
                    UserId = so.UserId
                };
                _context.Invoices.Add(invoice);
                _context.SaveChanges();
                invoiceId = invoice.Id;
            }
            else
            {
                invoiceId = invoice.Id;
            }

            // 3. Tự động tạo công nợ nếu PaymentTerm != "Trả ngay"
            if (so.PaymentTerm != "Tiền mặt" && so.PaymentTerm != "Trả ngay")
            {
                // Tìm customer từ SalesOrder
                var customer = _context.Customers
                    .AsEnumerable()
                    .FirstOrDefault(c => c.Name == so.CustomerName);

                var congNo = new CongNoKhachHang
                {
                    CustomerId = customer?.Id ?? 0,
                    InvoiceId = invoiceId,
                    SoTienPhaiThu = tongTien,
                    SoTienDaThu = 0,
                    SoTienConLai = tongTien,
                    NgayHoaDon = invoice.InvoiceDate,
                    NgayDenHan = invoice.DueDate,
                    TrangThai = TrangThaiCongNo.ChuaThanhToan
                };
                _context.CongNoKhachHangs.Add(congNo);
            }

            _context.SaveChanges();

            // 4. Ghi AuditLog
            GhiAuditLog("UPDATE", $"PGH {pgh.MaPGH} đã giao thành công", dto.NguoiXacNhan, pghId);
        }

        /// <summary>
        /// Xác nhận giao hàng thất bại.
        /// </summary>
        public void GiaoThatBai(int pghId, string lyDo, string nguoiCapNhat)
        {
            var pgh = _context.PhieuGiaoHangs
                .Include(p => p.SalesOrder)
                .FirstOrDefault(p => p.Id == pghId);

            if (pgh == null)
                throw new ArgumentException($"Không tìm thấy phiếu giao hàng ID: {pghId}");

            // 1. Cập nhật trạng thái
            pgh.TrangThai = TrangThaiGiaoHang.ThatBai;
            pgh.GhiChuGiao = lyDo;

            _context.SaveChanges();

            // 2. Gửi thông báo cho Kinh doanh
            var salesPerson = _context.Users.FirstOrDefault(u => u.Id == pgh.SalesOrder.UserId);
            var noiDungThongBao = $"PGH {pgh.MaPGH} giao thất bại. Lý do: {lyDo}. " +
                                  $"Khách hàng: {pgh.SalesOrder.CustomerName}. " +
                                  $"Địa chỉ: {pgh.DiaChiGiao}. SĐT: {pgh.SoDienThoaiNhanHang}.";

            var thongBao = new ThongBao
            {
                LoaiThongBao = LoaiThongBao.CanDuyet,
                NoiDung = noiDungThongBao,
                DuongDan = "/DeliveryView",
                UserId = salesPerson?.Id
            };
            _context.ThongBaos.Add(thongBao);

            _context.SaveChanges();

            // 3. Ghi AuditLog
            GhiAuditLog("UPDATE", $"PGH {pgh.MaPGH} - {lyDo}", nguoiCapNhat, pghId);
        }

        /// <summary>
        /// Lấy danh sách phiếu giao hàng.
        /// </summary>
        public List<PhieuGiaoHangDto> GetDanhSachPhieuGiao(string? trangThai = null, int? salesOrderId = null)
        {
            var query = _context.PhieuGiaoHangs
                .Include(p => p.SalesOrder)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(p => p.TrangThai == trangThai);

            if (salesOrderId.HasValue)
                query = query.Where(p => p.SalesOrderId == salesOrderId.Value);

            return query
                .OrderByDescending(p => p.NgayTao)
                .Select(p => new PhieuGiaoHangDto
                {
                    Id = p.Id,
                    MaPGH = p.MaPGH,
                    SalesOrderId = p.SalesOrderId,
                    SoDonHang = p.SalesOrder.OrderNo,
                    NgayGiao = p.NgayGiao,
                    NguoiGiao = p.NguoiGiao,
                    DiaChiGiao = p.DiaChiGiao,
                    SoDienThoaiNhanHang = p.SoDienThoaiNhanHang,
                    TrangThai = p.TrangThai,
                    NgayGiaoThucTe = p.NgayGiaoThucTe,
                    NguoiNhanHang = p.NguoiNhanHang,
                    GhiChuGiao = p.GhiChuGiao,
                    NgayTao = p.NgayTao
                }).ToList();
        }

        /// <summary>
        /// Lấy chi tiết phiếu giao hàng.
        /// </summary>
        public PhieuGiaoHangDto GetPhieuGiaoChiTiet(int pghId)
        {
            var pgh = _context.PhieuGiaoHangs
                .Include(p => p.SalesOrder)
                .Include(p => p.ChiTiet)
                .FirstOrDefault(p => p.Id == pghId);

            if (pgh == null)
                return null;

            return new PhieuGiaoHangDto
            {
                Id = pgh.Id,
                MaPGH = pgh.MaPGH,
                SalesOrderId = pgh.SalesOrderId,
                SoDonHang = pgh.SalesOrder?.OrderNo,
                NgayGiao = pgh.NgayGiao,
                NguoiGiao = pgh.NguoiGiao,
                DiaChiGiao = pgh.DiaChiGiao,
                SoDienThoaiNhanHang = pgh.SoDienThoaiNhanHang,
                TrangThai = pgh.TrangThai,
                NgayGiaoThucTe = pgh.NgayGiaoThucTe,
                NguoiNhanHang = pgh.NguoiNhanHang,
                GhiChuGiao = pgh.GhiChuGiao,
                HinhAnhXacNhan = pgh.HinhAnhXacNhan,
                NgayTao = pgh.NgayTao,
                ChiTiet = pgh.ChiTiet?.Select(c => new ChiTietPhieuGiaoDto
                {
                    TenSanPham = c.TenSanPham,
                    SoLuong = c.SoLuong,
                    DonViTinh = c.DonViTinh,
                    GhiChu = c.GhiChu
                }).ToList()
            };
        }

        #region Helper Methods

        /// <summary>
        /// Sinh mã phiếu giao hàng tự động.
        /// </summary>
        private string SinhMaPGH()
        {
            var prefix = "PGH-" + DateTime.Now.ToString("yyMMdd");
            var count = _context.PhieuGiaoHangs
                .Count(p => p.MaPGH.StartsWith(prefix)) + 1;
            return $"{prefix}{count:D3}";
        }

        /// <summary>
        /// Sinh mã Invoice tự động.
        /// </summary>
        private string SinhMaInvoice()
        {
            var prefix = "INV-" + DateTime.Now.ToString("yyMMdd");
            var count = _context.Invoices
                .Count(i => i.InvoiceNo.StartsWith(prefix)) + 1;
            return $"{prefix}{count:D3}";
        }

        /// <summary>
        /// Gửi thông báo cho người giao hàng.
        /// </summary>
        private void GuiThongBaoNguoiGiao(PhieuGiaoHang pgh)
        {
            // Gửi cho tất cả nhân viên giao hàng
            var nhanVienGiao = _context.Users
                .Where(u => u.Role == "Nhân viên giao hàng" || u.Role == "Thủ kho")
                .ToList();

            foreach (var nv in nhanVienGiao)
            {
                var thongBao = new ThongBao
                {
                    LoaiThongBao = "Giao hàng mới",
                    NoiDung = $"Bạn có phiếu giao hàng mới: {pgh.MaPGH}. " +
                              $"Địa chỉ: {pgh.DiaChiGiao}. SĐT: {pgh.SoDienThoaiNhanHang}.",
                    DuongDan = "/DeliveryView",
                    UserId = nv.Id,
                    DaDoc = false,
                    NgayTao = DateTime.Now
                };
                _context.ThongBaos.Add(thongBao);
            }
        }

        /// <summary>
        /// Ghi audit log.
        /// </summary>
        private void GhiAuditLog(string hanhDong, string moTa, string nguoiThucHien, int recordId)
        {
            var auditLog = new AuditLog
            {
                HanhDong = hanhDong,
                TenBang = "PhieuGiaoHang",
                IdBanGhi = recordId,
                DuLieuMoi = moTa,
                NguoiThucHien = nguoiThucHien ?? "System",
                ThoiGian = DateTime.Now
            };
            _context.AuditLogs.Add(auditLog);
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// DTO tạo phiếu giao hàng.
    /// </summary>
    public class CreateDeliveryDto
    {
        public DateTime NgayGiao { get; set; }
        public string NguoiGiao { get; set; }
        public string DiaChiGiao { get; set; }
        public string SoDienThoaiNhanHang { get; set; }
        public string NguoiTao { get; set; }
        public List<ChiTietPhieuGiaoDto> ChiTiet { get; set; }
    }

    /// <summary>
    /// DTO chi tiết sản phẩm giao.
    /// </summary>
    public class ChiTietPhieuGiaoDto
    {
        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public string DonViTinh { get; set; }
        public string GhiChu { get; set; }
    }

    /// <summary>
    /// DTO xác nhận giao hàng.
    /// </summary>
    public class XacNhanGiaoDto
    {
        public string NguoiNhanHang { get; set; }
        public DateTime? NgayGiaoThucTe { get; set; }
        public string GhiChu { get; set; }
        public string HinhAnhXacNhan { get; set; }
        public string NguoiXacNhan { get; set; }
    }

    /// <summary>
    /// DTO phiếu giao hàng.
    /// </summary>
    public class PhieuGiaoHangDto
    {
        public int Id { get; set; }
        public string MaPGH { get; set; }
        public int SalesOrderId { get; set; }
        public string SoDonHang { get; set; }
        public DateTime NgayGiao { get; set; }
        public string NguoiGiao { get; set; }
        public string DiaChiGiao { get; set; }
        public string SoDienThoaiNhanHang { get; set; }
        public string TrangThai { get; set; }
        public DateTime? NgayGiaoThucTe { get; set; }
        public string NguoiNhanHang { get; set; }
        public string GhiChuGiao { get; set; }
        public string HinhAnhXacNhan { get; set; }
        public DateTime NgayTao { get; set; }
        public List<ChiTietPhieuGiaoDto> ChiTiet { get; set; }
    }

    #endregion
}