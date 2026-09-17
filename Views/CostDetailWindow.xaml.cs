using System.Windows;

namespace PhanMemInAnERP.Views
{
    public partial class CostDetailWindow : Window
    {
        public CostDetailWindow(string detailText)
        {
            InitializeComponent();
            TxtDetail.Text = detailText ?? "";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
