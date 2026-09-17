using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service xử lý các thao tác liên quan đến Customer (Khách hàng).
    /// Cung cấp các method cho việc tạo mã, soft delete, và kiểm tra trạng thái khách hàng.
    /// </summary>
    public class CustomerService
    {
        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Tạo mã khách hàng tự động theo format "KH-{YY}{0001}".
        /// YY = 2 số cuối của năm hiện tại (timezone Bắc Kinh).
        /// 0001 = số thứ tự tăng dần trong năm, reset mỗi năm.
        /// </summary>
        /// <returns>Mã khách hàng mới, ví dụ: "KH-260001"</returns>
        public string GenerateMaKH()
        {
            // Lấy năm hiện tại theo giờ Bắc Kinh (UTC+8)
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")
            );
            
            int year = now.Year;
            string yearPrefix = year.ToString().Substring(2); // Lấy 2 số cuối: 2026 -> "26"

            // Tìm số lớn nhất trong năm hiện tại
            string patternPrefix = $"KH-{yearPrefix}";
            
            // Load tất cả MaKH về memory trước để tránh SQL NULL issue
            var allMaKH = _context.Customers
                .AsEnumerable()
                .Select(c => c.MaKH ?? "")
                .Where(m => m.StartsWith(patternPrefix))
                .OrderByDescending(m => m)
                .FirstOrDefault();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(allMaKH))
            {
                // Parse số cuối: "KH-260001" -> lấy "0001" -> 1
                string numberPart = allMaKH.Substring(allMaKH.Length - 4);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            // Format: KH-26{0001}
            return $"KH-{yearPrefix}{nextNumber:D4}";
        }

        /// <summary>
        /// Thực hiện soft delete (xóa mềm) khách hàng.
        /// Khách hàng không bị xóa thật sự khỏi database, chỉ đánh dấu NgayXoa.
        /// </summary>
        /// <param name="id">ID của khách hàng cần xóa</param>
        /// <param name="nguoiXoa">Tên người thực hiện xóa</param>
        /// <exception cref="ArgumentException">Khi không tìm thấy khách hàng hoặc đã bị xóa trước đó</exception>
        public void SoftDelete(int id, string nguoiXoa)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                throw new ArgumentException($"Không tìm thấy khách hàng với ID: {id}");
            }

            if (customer.DaBiXoa())
            {
                throw new ArgumentException($"Khách hàng '{customer.Name}' đã bị xóa trước đó vào lúc {customer.NgayXoa}");
            }

            // Thực hiện soft delete
            customer.NguoiXoa = nguoiXoa;
            customer.NgayXoa = DateTime.Now;
            _context.Customers.Update(customer);
            _context.SaveChanges();
        }

        /// <summary>
        /// Lấy danh sách tất cả khách hàng đang hoạt động (chưa bị xóa mềm).
        /// Chỉ trả về những khách hàng có NgayXoa == null.
        /// </summary>
        /// <returns>Danh sách khách hàng đang hoạt động</returns>
        public List<Customer> GetActive()
        {
            return _context.Customers
                .AsEnumerable()
                .Where(c => c.NgayXoa == null)
                .OrderBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Kiểm tra khách hàng có thể thanh toán hay không.
        /// Nếu khách hàng đang ở trạng thái "Nợ xấu", sẽ throw exception ngăn không cho tạo đơn mới.
        /// </summary>
        /// <param name="customerId">ID của khách hàng cần kiểm tra</param>
        /// <exception cref="InvalidOperationException">Khi khách hàng đang nợ xấu</exception>
        public void ValidateKhachHangCoTheThanhToan(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                throw new ArgumentException($"Không tìm thấy khách hàng với ID: {customerId}");
            }

            // Kiểm tra trạng thái nợ xấu
            if (customer.TrangThai == "Nợ xấu")
            {
                throw new InvalidOperationException("Khách hàng đang nợ xấu, không thể tạo đơn mới");
            }
        }

        /// <summary>
        /// Tạo mới khách hàng với mã tự động.
        /// </summary>
        /// <param name="customer">Đối tượng khách hàng cần tạo</param>
        /// <param name="nguoiTao">Tên người tạo</param>
        /// <returns>Khách hàng đã được tạo với mã mới</returns>
        public Customer Create(Customer customer, string nguoiTao)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            // Tạo mã tự động
            customer.MaKH = GenerateMaKH();
            customer.NguoiTao = nguoiTao;
            customer.NgayTao = DateTime.Now;
            customer.TrangThai = customer.TrangThai ?? "Hoạt động";

            // Chuẩn hóa dữ liệu trước khi lưu
            customer.NormalizeForDatabase();

            _context.Customers.Add(customer);
            _context.SaveChanges();

            return customer;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng.
        /// </summary>
        /// <param name="customer">Đối tượng khách hàng đã được chỉnh sửa</param>
        /// <param name="nguoiSua">Tên người sửa</param>
        public void Update(Customer customer, string nguoiSua)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            var existingCustomer = _context.Customers.Find(customer.Id);
            if (existingCustomer == null)
            {
                throw new ArgumentException($"Không tìm thấy khách hàng với ID: {customer.Id}");
            }

            if (existingCustomer.DaBiXoa())
            {
                throw new InvalidOperationException("Không thể cập nhật khách hàng đã bị xóa");
            }

            // Cập nhật các trường được phép sửa
            existingCustomer.Name = customer.Name;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Address = customer.Address;
            existingCustomer.TaxCode = customer.TaxCode;
            existingCustomer.Email = customer.Email;
            existingCustomer.NguoiLienHe = customer.NguoiLienHe;
            existingCustomer.GhiChu = customer.GhiChu;
            existingCustomer.TrangThai = customer.TrangThai;
            
            // Audit fields
            existingCustomer.NguoiSua = nguoiSua;
            existingCustomer.NgaySua = DateTime.Now;

            _context.Customers.Update(existingCustomer);
            _context.SaveChanges();
        }

        /// <summary>
        /// Lấy khách hàng theo ID.
        /// </summary>
        /// <param name="id">ID khách hàng</param>
        /// <returns>Khách hàng hoặc null nếu không tìm thấy</returns>
        public Customer GetById(int id)
        {
            return _context.Customers.Find(id);
        }

        /// <summary>
        /// Lấy khách hàng theo mã.
        /// </summary>
        /// <param name="maKH">Mã khách hàng</param>
        /// <returns>Khách hàng hoặc null nếu không tìm thấy</returns>
        public Customer GetByMaKH(string maKH)
        {
            return _context.Customers
                .AsEnumerable() // Chuyển sang LINQ to Objects để xử lý null an toàn
                .FirstOrDefault(c => c.MaKH == maKH);
        }

        /// <summary>
        /// Khôi phục khách hàng đã bị xóa mềm.
        /// </summary>
        /// <param name="id">ID khách hàng cần khôi phục</param>
        /// <param name="nguoiSua">Tên người thực hiện khôi phục</param>
        public void Restore(int id, string nguoiSua)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                throw new ArgumentException($"Không tìm thấy khách hàng với ID: {id}");
            }

            if (!customer.DaBiXoa())
            {
                throw new InvalidOperationException("Khách hàng chưa bị xóa, không cần khôi phục");
            }

            // Khôi phục: xóa thông tin xóa mềm
            customer.NguoiXoa = null;
            customer.NgayXoa = null;
            customer.NguoiSua = nguoiSua;
            customer.NgaySua = DateTime.Now;

            _context.Customers.Update(customer);
            _context.SaveChanges();
        }

        /// <summary>
        /// Lấy tất cả khách hàng (bao gồm cả đã xóa mềm).
        /// </summary>
        /// <returns>Danh sách tất cả khách hàng</returns>
        public List<Customer> GetAll()
        {
            return _context.Customers
                .AsEnumerable()
                .OrderBy(c => c.DaBiXoa())
                .ThenBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách khách hàng có thể chọn để tạo báo giá/đơn hàng.
        /// Chỉ trả khách có TrangThai = "Hoạt động" VÀ NgayXoa == null.
        /// </summary>
        /// <returns>Danh sách khách hàng hợp lệ</returns>
        public List<Customer> GetDanhSachChonKhach()
        {
            return _context.Customers
                .AsEnumerable()
                .Where(c => (c.TrangThai ?? "") == "Hoạt động" && c.NgayXoa == null)
                .OrderBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách khách hàng đang ở trạng thái "Nợ xấu".
        /// Kèm theo tổng công nợ hiện tại của từng khách (từ Invoices - Payments).
        /// </summary>
        /// <returns>Danh sách khách nợ xấu với thông tin công nợ</returns>
        public List<KhachNoXauInfo> GetKhachNoXau()
        {
            // Lấy tất cả khách nợ xấu - load về memory rồi filter bằng LINQ to Objects
            var khachNoXau = _context.Customers
                .AsEnumerable()
                .Where(c => c.NgayXoa == null && (c.TrangThai ?? "") == "Nợ xấu")
                .ToList();

            // Lấy tất cả hóa đơn và phiếu thu
            var allInvoices = _context.Invoices.ToList();
            var allPayments = _context.Payments.ToList();

            var result = new List<KhachNoXauInfo>();

            foreach (var kh in khachNoXau)
            {
                // Lấy các hóa đơn của khách này
                var hoaDonCuaKhach = allInvoices
                    .Where(i => i.CustomerName == kh.Name)
                    .ToList();

                if (hoaDonCuaKhach.Count == 0)
                {
                    // Khách nợ xấu nhưng chưa có hóa đơn
                    result.Add(new KhachNoXauInfo
                    {
                        CustomerId = kh.Id,
                        MaKH = kh.MaKH ?? "",
                        TenKhachHang = kh.Name,
                        Phone = kh.Phone ?? "",
                        Email = kh.Email ?? "",
                        TrangThai = kh.TrangThai,
                        TongNoHienTai = 0,
                        SoHoaDon = 0
                    });
                    continue;
                }

                // Tính công nợ
                double tongHoaDon = hoaDonCuaKhach.Sum(h => h.TotalAmount);
                var hoaDonNos = hoaDonCuaKhach.Select(h => h.InvoiceNo).ToList();
                double daThanhToan = allPayments
                    .Where(p => hoaDonNos.Contains(p.RefInvoiceNo))
                    .Sum(p => p.Amount);

                double congNo = tongHoaDon - daThanhToan;

                result.Add(new KhachNoXauInfo
                {
                    CustomerId = kh.Id,
                    MaKH = kh.MaKH ?? "",
                    TenKhachHang = kh.Name,
                    Phone = kh.Phone ?? "",
                    Email = kh.Email ?? "",
                    TrangThai = kh.TrangThai,
                    TongNoHienTai = congNo,
                    SoHoaDon = hoaDonCuaKhach.Count
                });
            }

            return result.OrderByDescending(r => r.TongNoHienTai).ToList();
        }
    }

    /// <summary>
    /// Thông tin khách hàng nợ xấu.
    /// </summary>
    public class KhachNoXauInfo
    {
        public int CustomerId { get; set; }
        public string MaKH { get; set; } = "";
        public string TenKhachHang { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public double TongNoHienTai { get; set; }
        public int SoHoaDon { get; set; }
    }
}
