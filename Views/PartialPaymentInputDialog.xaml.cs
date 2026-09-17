using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace PhanMemInAnERP.Views
{
    public partial class PartialPaymentInputDialog : Window
    {
        private readonly double _maxDebt;

        public double EnteredAmount { get; private set; }

        public PartialPaymentInputDialog(double maxDebt)
        {
            InitializeComponent();
            _maxDebt = Math.Max(0, maxDebt);
            TxtMaxDebt.Text = _maxDebt.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
            TxtAmount.Focus();
        }

        private static bool TryParseAmount(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var s = text.Trim().Replace(".", "").Replace(",", "").Replace(" ", "");
            s = Regex.Replace(s, @"[^\d]", "");
            if (string.IsNullOrEmpty(s)) return false;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(TxtAmount.Text, out var v) || v <= 0)
            {
                MessageBox.Show("Vui lòng nhập số tiền hợp lệ (lớn hơn 0).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (v > _maxDebt + 0.01)
            {
                MessageBox.Show(
                    $"Số tiền nhập ({v:N0} đ) vượt quá công nợ hiện tại ({_maxDebt:N0} đ).",
                    "Không hợp lệ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            EnteredAmount = v;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
