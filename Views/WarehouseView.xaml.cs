using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PhanMemInAnERP.ViewModels;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Views
{
    /// <summary>
    /// Interaction logic for WarehouseView.xaml
    /// </summary>
    public partial class WarehouseView : Window
    {
        public WarehouseView()
        {
            InitializeComponent();
            DataContext = new WarehouseViewModel();
        }

        private void InvReportSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || DataContext is not WarehouseViewModel vm) return;
            if (e.AddedItems[0] is string sel)
                vm.SetInventoryReportSearchFromCombo(sel);
        }

        private void SCMaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not WarehouseViewModel vm) return;
            if (sender is ComboBox combo && combo.SelectedItem is string selected && selected != "(Tất cả vật tư)")
            {
                // Parse mã vật tư từ text "Mã | Tên"
                var parts = selected.Split('|');
                if (parts.Length > 0)
                {
                    var maVT = parts[0].Trim();
                    vm.SCSelectedMaterial = vm.Materials?.FirstOrDefault(m => m.Code == maVT);
                }
            }
            else
            {
                vm.SCSelectedMaterial = null;
            }
        }
    }

    public class LoaiPhieuBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string loaiPhieu)
            {
                return loaiPhieu == "NHẬP"
                    ? new SolidColorBrush(Color.FromRgb(0xe8, 0xf5, 0xe9)) // Xanh nhạt
                    : new SolidColorBrush(Color.FromRgb(0xff, 0xeb, 0xee)); // Đỏ nhạt
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
