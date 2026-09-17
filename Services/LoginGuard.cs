using System;
using System.Windows;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Views;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Helper class kiểm tra và quản lý việc bắt buộc đổi mật khẩu.
    /// Sử dụng trong WPF navigation.
    /// </summary>
    public static class LoginGuard
    {
        /// <summary>
        /// Kiểm tra xem có cần bắt buộc đổi mật khẩu không.
        /// Nếu có, chuyển hướng đến trang đổi mật khẩu.
        /// </summary>
        /// <param name="currentWindow">Window hiện tại</param>
        /// <returns>True nếu cần chuyển hướng, False nếu có thể tiếp tục</returns>
        public static bool CheckAndRedirect(Window currentWindow)
        {
            // Kiểm tra user đã đăng nhập chưa
            if (AppSession.CurrentUser == null)
            {
                return false;
            }

            // Kiểm tra cờ bắt buộc đổi mật khẩu
            if (AppSession.PhaiDoiMatKhau)
            {
                // Đóng window hiện tại
                if (currentWindow != null)
                {
                    currentWindow.Hide();
                }

                // Mở dialog đổi mật khẩu
                var changePasswordDialog = new Views.ChangePasswordDialog
                {
                    Owner = Application.Current.MainWindow
                };

                var result = changePasswordDialog.ShowDialog();

                // Nếu đổi mật khẩu thành công, cờ BatBuocDoiMatKhau đã được set = false
                // Nếu hủy, quay lại màn hình đăng nhập
                if (result != true || AppSession.PhaiDoiMatKhau)
                {
                    // User hủy hoặc đổi không thành công -> đăng xuất
                    AppSession.CurrentUser = null;
                    
                    // Đóng tất cả windows
                    foreach (Window win in Application.Current.Windows.OfType<Window>().ToList())
                    {
                        if (win != Application.Current.MainWindow)
                            win.Close();
                    }

                    // Mở lại login
                    var login = new Views.LoginView();
                    login.Show();
                }

                // Đóng window hiện tại nếu đang mở
                currentWindow?.Close();

                return true; // Đã xử lý chuyển hướng
            }

            return false; // Không cần chuyển hướng
        }

        /// <summary>
        /// Kiểm tra trước khi mở một window mới.
        /// </summary>
        /// <param name="targetWindow">Window muốn mở</param>
        /// <param name="currentWindow">Window hiện tại (để hide)</param>
        /// <returns>True nếu đã chuyển hướng, False nếu có thể mở</returns>
        public static bool GuardWindow(Window targetWindow, Window? currentWindow = null)
        {
            if (CheckAndRedirect(currentWindow))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra và refresh lại thông tin user từ database.
        /// Gọi sau khi đổi mật khẩu thành công.
        /// </summary>
        public static void RefreshCurrentUser()
        {
            if (AppSession.CurrentUser == null) return;

            using var db = Helpers.DbContextFactory.Create();
            var updatedUser = db.Users.Find(AppSession.CurrentUser.Id);
            
            if (updatedUser != null)
            {
                AppSession.CurrentUser = updatedUser;
            }
        }

        /// <summary>
        /// Xử lý khi user đổi mật khẩu thành công.
        /// </summary>
        public static void OnPasswordChanged()
        {
            // Refresh user info từ database
            RefreshCurrentUser();
        }
    }
}
