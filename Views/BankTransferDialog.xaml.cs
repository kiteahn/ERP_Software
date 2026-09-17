using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.ViewModels;

namespace PhanMemInAnERP.Views
{
    public partial class BankTransferDialog : Window
    {
        private readonly SalesViewModel _vm;

        public BankTransferDialog(SalesViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            TxtAccount.Text = BankTransferInfo.AccountNumber;
            TxtBank.Text = BankTransferInfo.BankName;
            TxtHolder.Text = BankTransferInfo.AccountHolder;

            if (vm.CustomerDebtInfo != null)
                TxtContent.Text = $"{vm.CustomerDebtInfo.CustomerName} - Thu {vm.ManualPaymentAmount:N0} đ";
            else
                TxtContent.Text = "";

            string? qrPath = BankTransferInfo.ResolveQrImagePath();
            if (qrPath != null)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(Path.GetFullPath(qrPath));
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    QrImage.Source = bmp;
                    QrImage.Visibility = Visibility.Visible;
                }
                catch
                {
                    QrMissingHint.Visibility = Visibility.Visible;
                }
            }
            else
            {
                QrMissingHint.Visibility = Visibility.Visible;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.ConfirmBankTransferPayment())
                DialogResult = true;
        }

        private void NotYet_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Chưa ghi nhận thu tiền. Khách chưa chuyển khoản — vui lòng nhấn \"Đã chuyển khoản\" sau khi nhận được tiền.",
                "Chưa chuyển khoản",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = false;
        }
    }
}
