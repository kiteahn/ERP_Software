using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Công nợ.
    /// </summary>
    public class CongNoViewModel : BaseViewModel
    {
        private readonly AppDbContext _context;

        public ObservableCollection<CongNoKhachHang> DanhSachCongNo { get; set; } = new();
        public ObservableCollection<string> CacTrangThai { get; set; } = new()
        {
            "Tất cả", "Chưa thanh toán", "Thanh toán một phần", "Đã thanh toán", "Quá hạn"
        };

        private string _filterTrangThai = "Tất cả";
        public string FilterTrangThai
        {
            get => _filterTrangThai;
            set { _filterTrangThai = value; OnPropertyChanged(); }
        }

        private CongNoKhachHang? _congNoDangChon;
        public CongNoKhachHang? CongNoDangChon
        {
            get => _congNoDangChon;
            set { _congNoDangChon = value; OnPropertyChanged(); }
        }

        // KPIs
        private decimal _tongPhaiThu;
        public decimal TongPhaiThu { get => _tongPhaiThu; set { _tongPhaiThu = value; OnPropertyChanged(); } }

        private decimal _tongDaThu;
        public decimal TongDaThu { get => _tongDaThu; set { _tongDaThu = value; OnPropertyChanged(); } }

        private decimal _tongConLai;
        public decimal TongConLai { get => _tongConLai; set { _tongConLai = value; OnPropertyChanged(); } }

        private decimal _tongQuaHan;
        public decimal TongQuaHan { get => _tongQuaHan; set { _tongQuaHan = value; OnPropertyChanged(); } }

        // Thu tiền
        private decimal _soTienThu;
        public decimal SoTienThu
        {
            get => _soTienThu;
            set { _soTienThu = value; OnPropertyChanged(); }
        }

        public string[] CacHinhThucThu { get; } = new[] { "Tiền mặt", "Chuyển khoản" };
        private string _hinhThucThu = "Chuyển khoản";
        public string HinhThucThu { get => _hinhThucThu; set { _hinhThucThu = value; OnPropertyChanged(); } }

        public ICommand TaiLaiCommand { get; }
        public ICommand GhiNhanThanhToanCommand { get; }
        public ICommand XuatExcelCommand { get; }

        public CongNoViewModel()
        {
            _context = new AppDbContext();

            TaiLaiCommand = new AsyncRelayCommand(async () => await TaiLaiAsync());
            GhiNhanThanhToanCommand = new AsyncRelayCommand(async () => await GhiNhanThanhToanAsync(), () => CongNoDangChon != null);
            XuatExcelCommand = new AsyncRelayCommand(async () => await XuatExcelAsync());

            _ = TaiLaiAsync();
        }

        private async Task TaiLaiAsync()
        {
            try
            {
                var query = _context.CongNoKhachHangs
                    .Include(c => c.Customer)
                    .Include(c => c.Invoice)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(FilterTrangThai) && FilterTrangThai != "Tất cả")
                {
                    query = query.Where(c => c.TrangThai == FilterTrangThai);
                }

                var danhSach = await query
                    .OrderByDescending(c => c.SoNgayQuaHan)
                    .ThenBy(c => c.NgayDenHan)
                    .ToListAsync();

                // Cập nhật số ngày quá hạn
                foreach (var cn in danhSach.Where(c => c.TrangThai != "Đã thanh toán"))
                {
                    var ngayQuaHan = (DateTime.Today - cn.NgayDenHan).Days;
                    if (ngayQuaHan > 0 && cn.TrangThai != "Quá hạn")
                    {
                        cn.SoNgayQuaHan = ngayQuaHan;
                    }
                }

                DanhSachCongNo.Clear();
                foreach (var cn in danhSach)
                {
                    DanhSachCongNo.Add(cn);
                }

                // KPIs
                TongPhaiThu = danhSach.Sum(c => c.SoTienPhaiThu);
                TongDaThu = danhSach.Sum(c => c.SoTienDaThu);
                TongConLai = TongPhaiThu - TongDaThu;
                TongQuaHan = danhSach.Where(c => c.TrangThai == "Quá hạn").Sum(c => c.SoTienConLai);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GhiNhanThanhToanAsync()
        {
            if (CongNoDangChon == null) return;

            if (SoTienThu <= 0)
            {
                MessageBox.Show("Số tiền thu phải lớn hơn 0.", "Cảnh báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Xác nhận ghi nhận thanh toán?\n\n" +
                $"Khách hàng: {CongNoDangChon.Customer?.Name ?? ""}\n" +
                $"Số tiền: {SoTienThu:N0} đ\n" +
                $"Hình thức: {HinhThucThu}",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Cập nhật công nợ
                CongNoDangChon.SoTienDaThu += SoTienThu;
                CongNoDangChon.SoTienConLai = CongNoDangChon.SoTienPhaiThu - CongNoDangChon.SoTienDaThu;

                if (CongNoDangChon.SoTienConLai <= 0)
                {
                    CongNoDangChon.TrangThai = "Đã thanh toán";
                    CongNoDangChon.SoTienConLai = 0;
                }
                else if (CongNoDangChon.SoTienDaThu > 0)
                {
                    CongNoDangChon.TrangThai = "Thanh toán một phần";
                }

                await _context.SaveChangesAsync();

                // Ghi nhận thanh toán (Payment)
                var payment = new Payment
                {
                    ReceiptNo = $"PT-{DateTime.Now:yyyyMMddHHmmss}",
                    PaymentDate = DateTime.Now,
                    PaymentMethod = HinhThucThu,
                    CustomerName = CongNoDangChon.Customer?.Name ?? "",
                    RefInvoiceNo = CongNoDangChon.Invoice?.InvoiceNo ?? "",
                    Amount = (double)SoTienThu,
                    Notes = $"Thanh toán công nợ #{CongNoDangChon.Id}",
                    StaffName = AppSession.CurrentUser?.FullName ?? ""
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Tạo bút toán thu tiền
                try
                {
                    var accountingService = new AccountingService(_context);
                    var loaiNV = HinhThucThu == "Tiền mặt" ? "THU_TIEN_KHACH_TM" : "THU_TIEN_KHACH_CK";
                    await accountingService.TriggerKeToanAsync(
                        loaiNV,
                        payment.Id,
                        SoTienThu,
                        "Payments",
                        CongNoDangChon.Customer?.MaKH);
                }
                catch { /* Không throw nếu hạch toán lỗi */ }

                MessageBox.Show("Ghi nhận thanh toán thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SoTienThu = 0;
                await TaiLaiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi ghi nhận: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task XuatExcelAsync()
        {
            try
            {
                var exportService = new ExportService(_context,
                    new BaoCaoService(_context, new SoCaiService(_context)));
                var data = await exportService.XuatDanhSachCongNoExcelAsync();

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"CongNo_KH_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, data);
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Convert trạng thái sang màu.
        /// </summary>
        public static Brush GetTrangThaiColor(string trangThai)
        {
            return trangThai switch
            {
                "Đã thanh toán" => new SolidColorBrush(Color.FromRgb(200, 230, 201)),
                "Quá hạn" => new SolidColorBrush(Color.FromRgb(255, 205, 210)),
                "Thanh toán một phần" => new SolidColorBrush(Color.FromRgb(255, 249, 196)),
                _ => new SolidColorBrush(Color.FromRgb(227, 242, 253))
            };
        }
    }
}
