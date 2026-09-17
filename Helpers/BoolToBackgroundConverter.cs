using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>
    /// Chuyển Bool → Brush nền:
    /// True → Xanh lá (#28a745) = dữ liệu từ nguồn tự động
    /// False → Xám nhạt (#f5f5f5) = nhập tay
    /// </summary>
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool fromSource && fromSource)
            {
                // Xanh lá khi dùng nguồn tự động
                return new SolidColorBrush(Color.FromRgb(0x28, 0xa7, 0x45));
            }
            // Xám nhạt khi nhập tay
            return new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf5));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
