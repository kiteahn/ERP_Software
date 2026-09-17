using System;
using System.Windows;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Services;

namespace PhanMemInAnERP.Views
{
    /// <summary>
    /// Dialog đổi mật khẩu bắt buộc.
    /// User phải đổi mật khẩu trước khi sử dụng hệ thống.
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public ChangePasswordDialog()
        {
            InitializeComponent();
            this.Loaded += ChangePasswordDialog_Loaded;
        }

        private void ChangePasswordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Kiểm tra có phải bị bắt buộc đổi mật khẩu không
            bool isForcedChange = AppSession.CurrentUser?.BatBuocDoiMatKhau == true;

            if (isForcedChange)
            {
                // Ẩn field mật khẩu cũ và thay đổi thông báo
                txtOldPassword.Visibility = Visibility.Collapsed;
                lblOldPassword.Visibility = Visibility.Collapsed;
                txtThongBao.Text = "Mật khẩu của bạn đã được Admin reset. Bạn phải đổi mật khẩu mới trước khi sử dụng hệ thống.";
                
                // Đổi title
                this.Title = "Đổi mật khẩu bắt buộc";
            }

            // Focus vào ô mật khẩu mới
            txtNewPassword.Focus();
        }

        private void BtnDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            string matKhauCu = txtOldPassword.Password;
            string matKhauMoi = txtNewPassword.Password;
            string xacNhanMatKhau = txtConfirmPassword.Password;

            // Validate
            if (string.IsNullOrWhiteSpace(matKhauMoi))
            {
                MessageBox.Show("Mật khẩu mới không được để trống", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return;
            }

            if (matKhauMoi.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return;
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận mật khẩu không khớp", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Focus();
                return;
            }

            // Kiểm tra mật khẩu cũ (nếu user không bị reset bắt buộc)
            if (AppSession.CurrentUser != null && !AppSession.CurrentUser.BatBuocDoiMatKhau)
            {
                // Kiểm tra có phải BCrypt hash không, nếu không thì so sánh trực tiếp
                bool isValidPassword = AppSession.CurrentUser.Password.StartsWith("$2")
                    ? BCrypt.Net.BCrypt.Verify(matKhauCu, AppSession.CurrentUser.Password)
                    : matKhauCu == AppSession.CurrentUser.Password;

                if (!isValidPassword)
                {
                    MessageBox.Show("Mật khẩu cũ không đúng", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtOldPassword.Focus();
                    return;
                }
            }

            // Đổi mật khẩu
            try
            {
                using var db = DbContextFactory.Create();
                
                var user = db.Users.Find(AppSession.CurrentUser.Id);
                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin user", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Hash mật khẩu mới
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);
                user.Password = hashedPassword;
                user.BatBuocDoiMatKhau = false;

                db.SaveChanges();

                // Refresh AppSession
                AppSession.CurrentUser = user;

                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            // Không cho hủy nếu bị bắt buộc đổi mật khẩu
            if (AppSession.CurrentUser?.BatBuocDoiMatKhau == true)
            {
                MessageBox.Show("Bạn phải đổi mật khẩu trước khi sử dụng hệ thống", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = false;
            this.Close();
        }
    }
}
