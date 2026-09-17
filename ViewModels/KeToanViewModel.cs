using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Kế toán tổng hợp (4 tab).
    /// </summary>
    public class KeToanViewModel : BaseViewModel
    {
        private readonly AppDbContext _context;

        #region Tab 1: Sổ Cái

        public ObservableCollection<TaiKhoanKeToan> DanhSachTK { get; set; } = new();
        private TaiKhoanKeToan? _tkChon;
        public TaiKhoanKeToan? TKChon
        {
            get => _tkChon;
            set { _tkChon = value; OnPropertyChanged(); }
        }

        public DateTime TuNgay_SoCai { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime DenNgay_SoCai { get; set; } = DateTime.Today;

        public ObservableCollection<SoCai> DanhSachSoCai { get; set; } = new();

        private decimal _soDuDauKy;
        public decimal SoDuDauKy
        {
            get => _soDuDauKy;
            set { _soDuDauKy = value; OnPropertyChanged(); }
        }

        private decimal _phatSinhNo;
        public decimal PhatSinhNo
        {
            get => _phatSinhNo;
            set { _phatSinhNo = value; OnPropertyChanged(); }
        }

        private decimal _phatSinhCo;
        public decimal PhatSinhCo
        {
            get => _phatSinhCo;
            set { _phatSinhCo = value; OnPropertyChanged(); }
        }

        private decimal _soDuCuoiKy;
        public decimal SoDuCuoiKy
        {
            get => _soDuCuoiKy;
            set { _soDuCuoiKy = value; OnPropertyChanged(); }
        }

        public ICommand XemSoCaiCommand { get; }
        public ICommand XuatExcelSoCaiCommand { get; }

        #endregion

        #region Tab 2: Cân Đối Số Phát Sinh

        public DateTime TuNgay_CanDoi { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime DenNgay_CanDoi { get; set; } = DateTime.Today;

        public ObservableCollection<CanDoiPhatSinhDto> DanhSachCanDoi { get; set; } = new();

        private decimal _tongDuDauNo;
        public decimal TongDuDauNo { get => _tongDuDauNo; set { _tongDuDauNo = value; OnPropertyChanged(); } }

        private decimal _tongDuDauCo;
        public decimal TongDuDauCo { get => _tongDuDauCo; set { _tongDuDauCo = value; OnPropertyChanged(); } }

        private decimal _tongPsNo;
        public decimal TongPsNo { get => _tongPsNo; set { _tongPsNo = value; OnPropertyChanged(); } }

        private decimal _tongPsCo;
        public decimal TongPsCo { get => _tongPsCo; set { _tongPsCo = value; OnPropertyChanged(); } }

        private decimal _tongDuCuoiNo;
        public decimal TongDuCuoiNo { get => _tongDuCuoiNo; set { _tongDuCuoiNo = value; OnPropertyChanged(); } }

        private decimal _tongDuCuoiCo;
        public decimal TongDuCuoiCo { get => _tongDuCuoiCo; set { _tongDuCuoiCo = value; OnPropertyChanged(); } }

        public ICommand TaiLaiCanDoiCommand { get; }
        public ICommand XuatExcelCanDoiCommand { get; }

        #endregion

        #region Tab 3: Kết Chuyển Cuối Kỳ

        public DateTime KyBatDau { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime KyKetThuc { get; set; } = DateTime.Today;

        public ObservableCollection<XemTruocButToanDto> DanhSachXemTruoc { get; set; } = new();

        private bool _daXemTruoc;
        public bool DaXemTruoc { get => _daXemTruoc; set { _daXemTruoc = value; OnPropertyChanged(); } }

        private string _ketQuaKetChuyen = "";
        public string KetQuaKetChuyen { get => _ketQuaKetChuyen; set { _ketQuaKetChuyen = value; OnPropertyChanged(); } }

        private bool _dangXuLy;
        public bool DangXuLy { get => _dangXuLy; set { _dangXuLy = value; OnPropertyChanged(); } }

        public ICommand XemTruocCommand { get; }
        public ICommand KetChuyenCommand { get; }

        #endregion

        #region Tab 4: Báo Cáo Doanh Thu

        public DateTime TuNgay_BaoCao { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime DenNgay_BaoCao { get; set; } = DateTime.Today;

        private BaoCaoDoanhThuKeToanDto? _baoCao;
        public BaoCaoDoanhThuKeToanDto? BaoCao { get => _baoCao; set { _baoCao = value; OnPropertyChanged(); } }

        public ObservableCollection<ChiTietTK511Dto> ChiTiet511 { get; set; } = new();

        public ICommand XemBaoCaoCommand { get; }
        public ICommand XuatExcelBaoCaoCommand { get; }

        #endregion

        public KeToanViewModel()
        {
            _context = new AppDbContext();

            // Load danh sách tài khoản
            LoadTaiKhoan();

            // Tab 1
            XemSoCaiCommand = new AsyncRelayCommand(async () => await XemSoCaiAsync());
            XuatExcelSoCaiCommand = new AsyncRelayCommand(async () => await XuatExcelSoCaiAsync());

            // Tab 2
            TaiLaiCanDoiCommand = new AsyncRelayCommand(async () => await TaiLaiCanDoiAsync());
            XuatExcelCanDoiCommand = new AsyncRelayCommand(async () => await XuatExcelCanDoiAsync());

            // Tab 3
            XemTruocCommand = new AsyncRelayCommand(async () => await XemTruocAsync());
            KetChuyenCommand = new AsyncRelayCommand(async () => await KetChuyenAsync());

            // Tab 4
            XemBaoCaoCommand = new AsyncRelayCommand(async () => await XemBaoCaoAsync());
            XuatExcelBaoCaoCommand = new AsyncRelayCommand(async () => await XuatExcelBaoCaoAsync());
        }

        private void LoadTaiKhoan()
        {
            var taiKhoans = _context.TaiKhoanKeToans
                .Where(t => t.IsActive)
                .OrderBy(t => t.MaTK)
                .ToList();

            DanhSachTK.Clear();
            foreach (var tk in taiKhoans)
            {
                DanhSachTK.Add(tk);
            }

            if (DanhSachTK.Any())
            {
                TKChon = DanhSachTK.First();
            }
        }

        #region Tab 1: Sổ Cái

        private async Task XemSoCaiAsync()
        {
            if (TKChon == null) return;

            try
            {
                var soCaiService = new SoCaiService(_context);
                var danhSach = await soCaiService.GetTheoTKAsync(TKChon.MaTK, TuNgay_SoCai, DenNgay_SoCai);

                DanhSachSoCai.Clear();
                foreach (var sc in danhSach)
                {
                    DanhSachSoCai.Add(sc);
                }

                // Tính số dư
                var soDuDau = await soCaiService.GetSoDuCuoiKyAsync(TKChon.MaTK, TuNgay_SoCai.AddDays(-1));
                var psNo = danhSach.Sum(x => x.SoTienNo);
                var psCo = danhSach.Sum(x => x.SoTienCo);
                var soDuCuoi = await soCaiService.GetSoDuCuoiKyAsync(TKChon.MaTK, DenNgay_SoCai);

                SoDuDauKy = soDuDau;
                PhatSinhNo = psNo;
                PhatSinhCo = psCo;
                SoDuCuoiKy = soDuCuoi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sổ cái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task XuatExcelSoCaiAsync()
        {
            if (TKChon == null) return;

            try
            {
                var exportService = new ExportService(_context, new BaoCaoService(_context, new SoCaiService(_context)));
                var data = await exportService.XuatSoCaiExcelAsync(TKChon.MaTK, TuNgay_SoCai, DenNgay_SoCai);

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"SoCai_TK{TKChon.MaTK}_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, data);
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Tab 2: Cân Đối Số Phát Sinh

        private async Task TaiLaiCanDoiAsync()
        {
            try
            {
                var soCaiService = new SoCaiService(_context);
                var canDoi = await soCaiService.GetBangCanDoiAsync(TuNgay_CanDoi, DenNgay_CanDoi);

                DanhSachCanDoi.Clear();
                foreach (var item in canDoi)
                {
                    DanhSachCanDoi.Add(item);
                }

                // Tính tổng
                TongDuDauNo = canDoi.Sum(x => x.DuDauKyNo);
                TongDuDauCo = canDoi.Sum(x => x.DuDauKyCo);
                TongPsNo = canDoi.Sum(x => x.PhatSinhNo);
                TongPsCo = canDoi.Sum(x => x.PhatSinhCo);
                TongDuCuoiNo = canDoi.Sum(x => x.DuCuoiKyNo);
                TongDuCuoiCo = canDoi.Sum(x => x.DuCuoiKyCo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bảng cân đối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task XuatExcelCanDoiAsync()
        {
            try
            {
                var exportService = new ExportService(_context, new BaoCaoService(_context, new SoCaiService(_context)));
                var data = await exportService.XuatBangCanDoiSoPhatSinhExcelAsync(TuNgay_CanDoi, DenNgay_CanDoi);

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"BangCanDoiSPS_{TuNgay_CanDoi:yyyyMMdd}_{DenNgay_CanDoi:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, data);
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Tab 3: Kết Chuyển Cuối Kỳ

        private async Task XemTruocAsync()
        {
            try
            {
                var accountingService = new AccountingService(_context);

                DanhSachXemTruoc.Clear();
                var cacNghiepVu = new[]
                {
                    ("KC_DOANH_THU", "Kết chuyển DT 511"),
                    ("KC_GIA_VON", "Kết chuyển GV 632"),
                    ("KC_CP_BH", "Kết chuyển CPBH 641"),
                    ("KC_CP_QLDN", "Kết chuyển CPQLDN 642")
                };

                foreach (var (maNV, tenNV) in cacNghiepVu)
                {
                    try
                    {
                        var xemTruoc = await accountingService.XemTruocButToanAsync(maNV, 1000000);
                        foreach (var item in xemTruoc)
                        {
                            DanhSachXemTruoc.Add(item);
                        }
                    }
                    catch { }
                }

                DaXemTruoc = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xem trước: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task KetChuyenAsync()
        {
            var result = MessageBox.Show(
                $"Xác nhận kết chuyển cuối kỳ {KyBatDau:MM/yyyy}?\n\nThao tác này sẽ tạo các bút toán kết chuyển doanh thu, chi phí và xác định kết quả kinh doanh.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                DangXuLy = true;

                var accountingService = new AccountingService(_context);
                var soCaiService = new SoCaiService(_context);
                var ketChuyenService = new KetChuyenService(_context, accountingService, soCaiService);

                var ketQua = await ketChuyenService.KetChuyenCuoiKyAsync(
                    KyBatDau, KyKetThuc,
                    AppSession.CurrentUser?.FullName ?? "System");

                if (ketQua.Success)
                {
                    KetQuaKetChuyen = $"{ketQua.LoaiKetQua}: {ketQua.KetQuaKinhDoanh:N0} đ";
                    MessageBox.Show(
                        $"Kết chuyển thành công!\n\n" +
                        $"Kỳ: {ketQua.KyBatDau:MM/yyyy}\n" +
                        $"Doanh thu: {ketQua.TongDoanhThu:N0} đ\n" +
                        $"Chi phí: {ketQua.TongChiPhi:N0} đ\n" +
                        $"KQKD: {ketQua.LoaiKetQua} {ketQua.KetQuaKinhDoanh:N0} đ",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết chuyển: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DangXuLy = false;
            }
        }

        #endregion

        #region Tab 4: Báo Cáo Doanh Thu

        private async Task XemBaoCaoAsync()
        {
            try
            {
                var soCaiService = new SoCaiService(_context);
                var baoCaoService = new BaoCaoService(_context, soCaiService);

                BaoCao = await baoCaoService.GetBaoCaoDoanhThuAsync(TuNgay_BaoCao, DenNgay_BaoCao);

                var chiTiet = await baoCaoService.GetChiTietDT511Async(TuNgay_BaoCao, DenNgay_BaoCao);
                ChiTiet511.Clear();
                foreach (var item in chiTiet)
                {
                    ChiTiet511.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task XuatExcelBaoCaoAsync()
        {
            try
            {
                var exportService = new ExportService(_context, new BaoCaoService(_context, new SoCaiService(_context)));
                var data = await exportService.XuatBaoCaoDoanhThuExcelAsync(TuNgay_BaoCao, DenNgay_BaoCao);

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"BaoCaoDoanhThu_{TuNgay_BaoCao:yyyyMMdd}_{DenNgay_BaoCao:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, data);
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
