using System.Globalization;

namespace PhanMemInAnERP.ViewModels
{
    /// <summary>Mục trong ComboBox số lượng — hiển thị dạng 20.000, binding Value với Quantity.</summary>
    public sealed class QuantityListItem
    {
        private static readonly CultureInfo Vi = new CultureInfo("vi-VN");

        public int Value { get; }
        public string Label { get; }

        public QuantityListItem(int value)
        {
            Value = value;
            Label = value.ToString("N0", Vi);
        }
    }
}
