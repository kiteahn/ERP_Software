using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Audit Log (Nhật ký hệ thống).
    /// </summary>
    public class AuditLogViewModel : BaseViewModel
    {
        private readonly AppDbContext _context;

        public ObservableCollection<AuditLog> DanhSachLog { get; set; } = new();

        public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime DenNgay { get; set; } = DateTime.Today;

        private string _filterUser = "";
        public string FilterUser
        {
            get => _filterUser;
            set { _filterUser = value; OnPropertyChanged(); }
        }

        public string[] CacHanhDong { get; } = new[]
        {
            "Tất cả", "CREATE", "UPDATE", "DELETE", "LOGIN", "LOGOUT", "EXPORT", "KETCUYEN"
        };

        private string _filterHanhDong = "Tất cả";
        public string FilterHanhDong
        {
            get => _filterHanhDong;
            set { _filterHanhDong = value; OnPropertyChanged(); }
        }

        private int _trangHienTai = 1;
        public int TrangHienTai
        {
            get => _trangHienTai;
            set { _trangHienTai = value; OnPropertyChanged(); }
        }

        private int _tongSoBanGhi;
        public int TongSoBanGhi
        {
            get => _tongSoBanGhi;
            set { _tongSoBanGhi = value; OnPropertyChanged(); }
        }

        private int _tongTrang;
        public int TongTrang
        {
            get => _tongTrang;
            set { _tongTrang = value; OnPropertyChanged(); }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> DanhSachUser { get; set; } = new();

        public ICommand TimKiemCommand { get; }
        public ICommand TrangSauCommand { get; }
        public ICommand TrangTruocCommand { get; }
        public ICommand XuatExcelCommand { get; }

        public AuditLogViewModel()
        {
            _context = new AppDbContext();

            // Kiểm tra quyền
            var user = AppSession.CurrentUser;
            if (user == null || (user.Role != "Admin" && user.Role != "Giám đốc"))
            {
                MessageBox.Show("Bạn không có quyền truy cập Nhật ký hệ thống.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadDanhSachUser();
            TimKiemCommand = new AsyncRelayCommand(async () => await TimKiemAsync());
            TrangSauCommand = new RelayCommand(async () => { if (TrangHienTai < TongTrang) await TrangSauAsync(); }, () => TrangHienTai < TongTrang);
            TrangTruocCommand = new RelayCommand(async () => { if (TrangHienTai > 1) await TrangTruocAsync(); }, () => TrangHienTai > 1);
            XuatExcelCommand = new AsyncRelayCommand(async () => await XuatExcelAsync());

            // Tải dữ liệu ban đầu
            _ = TimKiemAsync();
        }

        private void LoadDanhSachUser()
        {
            var users = _context.AuditLogs
                .Select(a => a.NguoiThucHien)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            DanhSachUser.Clear();
            DanhSachUser.Add("");
            foreach (var user in users)
            {
                DanhSachUser.Add(user);
            }
        }

        private async Task TimKiemAsync()
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                // Filter theo ngày
                query = query.Where(a => a.ThoiGian >= TuNgay.Date
                    && a.ThoiGian <= DenNgay.Date.AddDays(1));

                // Filter theo hành động
                if (!string.IsNullOrEmpty(FilterHanhDong) && FilterHanhDong != "Tất cả")
                {
                    query = query.Where(a => a.HanhDong == FilterHanhDong);
                }

                // Filter theo user
                if (!string.IsNullOrEmpty(FilterUser))
                {
                    query = query.Where(a => a.NguoiThucHien == FilterUser);
                }

                // Đếm tổng
                TongSoBanGhi = await query.CountAsync();
                TongTrang = Math.Max(1, (int)Math.Ceiling(TongSoBanGhi / (double)PageSize));

                if (TrangHienTai > TongTrang) TrangHienTai = 1;

                // Phân trang
                var logs = await query
                    .OrderByDescending(a => a.ThoiGian)
                    .Skip((TrangHienTai - 1) * PageSize)
                    .Take(PageSize)
                    .Include(a => a.User)
                    .ToListAsync();

                DanhSachLog.Clear();
                foreach (var log in logs)
                {
                    DanhSachLog.Add(log);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task TrangSauAsync()
        {
            if (TrangHienTai < TongTrang)
            {
                TrangHienTai++;
                await TimKiemAsync();
            }
        }

        private async Task TrangTruocAsync()
        {
            if (TrangHienTai > 1)
            {
                TrangHienTai--;
                await TimKiemAsync();
            }
        }

        private async Task XuatExcelAsync()
        {
            try
            {
                var exportService = new ExportService(_context,
                    new BaoCaoService(_context, new SoCaiService(_context)));
                var data = await exportService.XuatAuditLogExcelAsync(TuNgay, DenNgay);

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"AuditLog_{TuNgay:yyyyMMdd}_{DenNgay:yyyyMMdd}.xlsx"
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
    }
}
