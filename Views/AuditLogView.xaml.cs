using System.Windows;
using PhanMemInAnERP.ViewModels;

namespace PhanMemInAnERP.Views
{
    public partial class AuditLogView : Window
    {
        public AuditLogView()
        {
            InitializeComponent();
            DataContext = new AuditLogViewModel();
        }
    }
}
