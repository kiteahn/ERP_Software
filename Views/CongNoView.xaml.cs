using System.Windows;
using PhanMemInAnERP.ViewModels;

namespace PhanMemInAnERP.Views
{
    public partial class CongNoView : Window
    {
        public CongNoView()
        {
            InitializeComponent();
            DataContext = new CongNoViewModel();
        }
    }
}
