using System.Linq;
using System.Windows;
using PhanMemInAnERP.Views;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.ViewModels;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            // 🔗 CÀI ĐẶT CÂU NỐI: Khi ở Window Lịch sử nhấn "Tải lại"
            DataBridge.OnRequestLoadQuote += (quote) =>
            {
                // 1. Mở Window Tính giá (nếu chưa mở)
                var win = OpenOrActivate<QuotationView>();

                // 2. Ép kiểu DataContext để đổ dữ liệu vào ViewModel của Window đó
                if (win.DataContext is QuotationViewModel vm)
                {
                    vm.ApplyHistoryQuote(quote);
                }
            };

            // 🔗 KẾT NỐI: Khi nhấn "Chốt Đơn" từ Lịch sử
            DataBridge.OnRequestCreateOrder += (quote) =>
            {
                // 1. Mở Window Bán hàng (nếu chưa mở)
                var win = OpenOrActivate<SalesView>();

                // 2. Gọi lệnh nạp dữ liệu từ báo giá sang viewmodel bán hàng
                if (win.DataContext is SalesViewModel vm)
                {
                    vm.LoadFromQuotation(quote);
                }
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ==========================================
            // KIỂM TRA BẮT BUỘC ĐỔI MẬT KHẨU
            // ==========================================
            if (LoginGuard.CheckAndRedirect(this))
            {
                return; // Đã chuyển hướng đến dialog đổi mật khẩu
            }

            // Hiển thị tên người dùng lên góc (Ví dụ: Chào mừng quay trở lại, Kiệt | Admin)
            

            // ==========================================
            // LOGIC ẨN/HIỆN NÚT THEO ROLE
            // ==========================================

            // 1. Nút Tính Giá (Admin, Nhân viên)
            btnQuotation.Visibility = (AppSession.IsAdmin || AppSession.IsNhanVien) ? Visibility.Visible : Visibility.Collapsed;

            // 2. Nút Lệnh Sản Xuất (Admin, Nhân viên, Quản lý kho)
            btnProduction.Visibility = (AppSession.IsAdmin || AppSession.IsNhanVien || AppSession.IsThuKho) ? Visibility.Visible : Visibility.Collapsed;

            // 3. Nút Kho & Vật Tư (Admin, Kế toán, Quản lý kho)
            btnWarehouse.Visibility = (AppSession.IsAdmin || AppSession.IsKeToan || AppSession.IsThuKho) ? Visibility.Visible : Visibility.Collapsed;

            // 4. Nút Bán Hàng & Công Nợ (Admin, Kế toán, Nhân viên)
            btnSales.Visibility = (AppSession.IsAdmin || AppSession.IsKeToan || AppSession.IsNhanVien) ? Visibility.Visible : Visibility.Collapsed;

            // 5. Nút Khách hàng (Kế toán, Quản lý kho, Nhân viên + Admin)
            btnCustomers.Visibility = AppSession.CanViewCustomerModule ? Visibility.Visible : Visibility.Collapsed;

            // 6. Nút Quản lý Nhân Viên (CHỈ ADMIN)
            btnAdminUser.Visibility = AppSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            // 7. Kế toán tự động — Ánh xạ & xem trước bút toán (Admin, Kế toán)
            btnAccounting.Visibility = (AppSession.IsAdmin || AppSession.IsKeToan) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenAdminUser(object sender, RoutedEventArgs e) => OpenOrActivate<AdminUserView>();
        private void OpenAccountingTools(object sender, RoutedEventArgs e) => OpenOrActivate<AccountingToolsView>();

        // Hàm dùng chung: Kiểm tra cửa sổ đã mở chưa, nếu rồi thì Focus, chưa thì New
        private T OpenOrActivate<T>() where T : Window, new()
        {
            // Kiểm tra bắt buộc đổi mật khẩu trước khi mở window mới
            if (LoginGuard.GuardWindow(null, null))
            {
                // Trả về null nếu đang bị chuyển hướng
                return default(T);
            }

            var win = Application.Current.Windows.OfType<T>().FirstOrDefault();
            if (win == null)
            {
                win = new T();
                win.Show();
            }
            else
            {
                win.Activate();
                if (win.WindowState == WindowState.Minimized)
                    win.WindowState = WindowState.Normal;
            }
            return win;
        }

        private void OpenQuotation(object sender, RoutedEventArgs e) => OpenOrActivate<QuotationView>();
        private void OpenProduction(object sender, RoutedEventArgs e) => OpenOrActivate<ProductionOrderView>();
        private void OpenWarehouse(object sender, RoutedEventArgs e) => OpenOrActivate<WarehouseView>();
        private void OpenSales(object sender, RoutedEventArgs e) => OpenOrActivate<SalesView>();
        private void OpenCustomers(object sender, RoutedEventArgs e) => OpenOrActivate<CustomerView>();

        private void OpenHistory(object sender, RoutedEventArgs e) => OpenOrActivate<HistoryDashboardView>();

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // 1. Đóng tất cả Window đang mở (ngoại trừ MainWindow đang đóng)
                foreach (Window win in Application.Current.Windows.OfType<Window>().ToList())
                {
                    if (win != this)
                        win.Close();
                }

                // 2. Xóa phiên đăng nhập
                AppSession.CurrentUser = null;

                // 3. Mở lại màn hình đăng nhập
                LoginView login = new LoginView();
                login.Show();

                // 4. Đóng MainWindow hiện tại
                this.Close();
            }
        }
    }
}