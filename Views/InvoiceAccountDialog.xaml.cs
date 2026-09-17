using PhanMemInAnERP.Models;
using System;
using System.Windows;

namespace PhanMemInAnERP.Views
{
    public partial class InvoiceAccountDialog : Window
    {
        public InvoiceAccountDialog()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoiceAccountDialogViewModel vm)
            {
                if (vm.SaveChanges())
                {
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
