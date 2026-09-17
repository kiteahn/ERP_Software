using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>Hiển thị số dạng N0 theo vi-VN (1.900, 400.000); parse lại khi nhập.</summary>
    public sealed class VnMoneyFormatConverter : IValueConverter
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return "";
            return value switch
            {
                double d => Math.Round(d, 0, MidpointRounding.AwayFromZero).ToString("N0", Vi),
                int i => i.ToString("N0", Vi),
                long l => l.ToString("N0", Vi),
                float f => Math.Round(f, 0, MidpointRounding.AwayFromZero).ToString("N0", Vi),
                decimal m => Math.Round(m, 0, MidpointRounding.AwayFromZero).ToString("N0", Vi),
                _ => value.ToString() ?? ""
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var effective = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
            {
                if (effective == typeof(int)) return 0;
                return 0d;
            }

            var t = new string(s.Trim().Where(ch => !char.IsWhiteSpace(ch) && ch != '\u00a0').ToArray());
            if (t.Length == 0)
            {
                if (effective == typeof(int)) return 0;
                return 0d;
            }

            t = t.Replace(".", "");
            if (!double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return DependencyProperty.UnsetValue;

            if (effective == typeof(int))
                return (int)Math.Round(d, MidpointRounding.AwayFromZero);
            if (effective == typeof(long))
                return (long)Math.Round(d, MidpointRounding.AwayFromZero);
            if (effective == typeof(float))
                return (float)d;
            if (effective == typeof(decimal))
                return (decimal)Math.Round(d, 0, MidpointRounding.AwayFromZero);
            return d;
        }
    }
}
