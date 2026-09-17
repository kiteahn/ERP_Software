using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>Hiện dòng khi số tiền &gt; 0 (ẩn khoản chi phí không phát sinh).</summary>
    public sealed class VisibleWhenCostPositiveConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
                return d > 0.0001 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
