using System.Globalization;
using System.Linq;
using System.Windows;
namespace PhanMemInAnERP.Views
{
    public partial class PlatePriceEditDialog : Window
    {
        public PlatePriceEditDialog(double largePerColor, double smallPerColor)
        {
            InitializeComponent();
            TxtLarge.Text = FormatMoney(largePerColor);
            TxtSmall.Text = FormatMoney(smallPerColor);
        }

        public double LargePerColor { get; private set; }
        public double SmallPerColor { get; private set; }

        private static string FormatMoney(double x) =>
            Math.Round(x, 0, MidpointRounding.AwayFromZero).ToString("N0", new CultureInfo("vi-VN"));

        private static bool TryParseVnMoney(string? s, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return true;
            var t = new string(s.Trim().Where(ch => !char.IsWhiteSpace(ch) && ch != '\u00a0').ToArray());
            if (t.Length == 0) return true;
            t = t.Replace(".", "");
            return double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseVnMoney(TxtLarge.Text, out double l) || !TryParseVnMoney(TxtSmall.Text, out double s))
            {
                MessageBox.Show("Giá không hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (l <= 0 || s <= 0)
            {
                MessageBox.Show("Giá phải lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LargePerColor = l;
            SmallPerColor = s;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
