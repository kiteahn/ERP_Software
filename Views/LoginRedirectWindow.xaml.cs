using System.Windows;

namespace PhanMemInAnERP.Views
{
    public partial class LoginRedirectWindow : Window
    {
        public LoginRedirectWindow(string message)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
        }
    }
}
