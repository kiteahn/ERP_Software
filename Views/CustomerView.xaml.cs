using System.Windows;
using PhanMemInAnERP.ViewModels;

namespace PhanMemInAnERP.Views
{
    public partial class CustomerView : Window
    {
        public CustomerView()
        {
            InitializeComponent();
            DataContext = new AdminUserViewModel();
        }
    }
}
