using System;
using System.Collections.Generic;
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

namespace PhanMemInAnERP.Views
{
    /// <summary>
    /// Interaction logic for SalesView.xaml
    /// </summary>
    public partial class SalesView : Window
    {
        public SalesView()
        {
            InitializeComponent();
            DataContext = new SalesViewModel();
        }

        private void DebtReportSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || DataContext is not SalesViewModel vm) return;
            if (e.AddedItems[0] is string sel)
                vm.SetDebtReportSearchFromCombo(sel);
        }
    }
}
