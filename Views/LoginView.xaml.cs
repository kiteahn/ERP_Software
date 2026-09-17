using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;

namespace PhanMemInAnERP.Views
{
    public partial class LoginView : Window
    {
        private LoginRedirectWindow? _redirectWindow;
        private bool _passwordRevealed;

        public LoginView()
        {
            InitializeComponent();
        }

        private static string GetRedirectMessage(string? role)
        {
            string label = role switch
            {
                "Admin" => "Admin",
                "Kế toán" => "Kế toán",
                "Nhân viên" => "Nhân viên",
                "Quản lý kho" => "Quản lý kho",
                _ => string.IsNullOrWhiteSpace(role) ? "hệ thống" : role.Trim()
            };
            return $"Đang chuyển hướng tới {label}...";
        }

        private void CredentialField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
                e.Handled = true;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e) => TryLogin();

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _passwordRevealed = !_passwordRevealed;
            if (_passwordRevealed)
            {
                LvwPasswordVisibleTextBox.Text = LvwPasswordBox.Password;
                LvwPasswordBox.Visibility = Visibility.Collapsed;
                LvwPasswordVisibleTextBox.Visibility = Visibility.Visible;
                LvwPasswordVisibilityIcon.Kind = PackIconKind.EyeOff;
                LvwTogglePasswordVisibilityButton.ToolTip = "Ẩn mật khẩu";
                LvwPasswordVisibleTextBox.Focus();
                LvwPasswordVisibleTextBox.CaretIndex = LvwPasswordVisibleTextBox.Text.Length;
            }
            else
            {
                LvwPasswordBox.Password = LvwPasswordVisibleTextBox.Text;
                LvwPasswordVisibleTextBox.Visibility = Visibility.Collapsed;
                LvwPasswordBox.Visibility = Visibility.Visible;
                LvwPasswordVisibilityIcon.Kind = PackIconKind.Eye;
                LvwTogglePasswordVisibilityButton.ToolTip = "Hiện mật khẩu";
                LvwPasswordBox.Focus();
            }
        }

        private string GetPasswordTrimmed() =>
            _passwordRevealed ? LvwPasswordVisibleTextBox.Text.Trim() : LvwPasswordBox.Password.Trim();

        private void TryLogin()
        {
            string username = LvwUsernameTextBox.Text.Trim();
            string password = GetPasswordTrimmed();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ tài khoản và mật khẩu!");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Kiểm tra User trong database
                    var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

                    if (user != null)
                    {
                        AppSession.CurrentUser = user;

                        LvwErrorTextBlock.Visibility = Visibility.Collapsed;
                        LvwSubmitButton.IsEnabled = false;

                        _redirectWindow = new LoginRedirectWindow(GetRedirectMessage(user.Role))
                        {
                            Owner = this
                        };
                        _redirectWindow.Show();

                        // Cho popup vẽ một nhịp, rồi tạo MainWindow (Loaded = đã áp phân quyền trong MainWindow_Loaded)
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                var main = new MainWindow();
                                main.Opacity = 0;
                                void OnMainLoaded(object _, RoutedEventArgs __)
                                {
                                    main.Loaded -= OnMainLoaded;
                                    main.Opacity = 1;
                                    _redirectWindow?.Close();
                                    _redirectWindow = null;
                                    Close();
                                }
                                main.Loaded += OnMainLoaded;
                                main.Show();
                            }
                            catch (Exception ex)
                            {
                                _redirectWindow?.Close();
                                _redirectWindow = null;
                                LvwSubmitButton.IsEnabled = true;
                                AppSession.CurrentUser = null;
                                ShowError("Không mở được giao diện chính: " + ex.Message);
                            }
                        }), DispatcherPriority.ApplicationIdle);
                    }
                    else
                    {
                        ShowError("Sai tài khoản hoặc mật khẩu. Vui lòng thử lại!");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi kết nối database: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            LvwErrorTextBlock.Text = message;
            LvwErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}