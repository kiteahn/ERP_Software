using System.Windows;
using PhanMemInAnERP.ViewModels;

namespace PhanMemInAnERP.Views
{
    public partial class KeToanView : Window
    {
        public KeToanView()
        {
            InitializeComponent();
            DataContext = new KeToanViewModel();
        }
    }
}
