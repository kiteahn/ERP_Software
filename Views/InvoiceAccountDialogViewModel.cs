using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PhanMemInAnERP.Views
{
    public class InvoiceAccountDialogViewModel : BaseViewModel
    {
        private readonly AppDbContext _db = new AppDbContext();
        private Invoice _invoice;

        public string InvoiceNo => _invoice.InvoiceNo ?? "";
        public string CustomerName => _invoice.CustomerName ?? "";
        public double TotalAmount => _invoice.TotalAmount;

        private string _tkNo;
        public string TK_No
        {
            get => _tkNo;
            set { _tkNo = value; OnPropertyChanged(); }
        }

        private string _tkCo;
        public string TK_Co
        {
            get => _tkCo;
            set { _tkCo = value; OnPropertyChanged(); }
        }

        private string _accountType;
        public string AccountType
        {
            get => _accountType;
            set { _accountType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> AccountTypeOptions { get; } = new ObservableCollection<string>
        {
            "Công nợ (131)", "Tiền mặt (111)", "Ngân hàng (112)"
        };

        public InvoiceAccountDialogViewModel(Invoice invoice)
        {
            _invoice = invoice;
            TK_No = invoice.TK_No ?? "131";
            TK_Co = invoice.TK_Co ?? "511";
            AccountType = invoice.AccountType ?? "Công nợ (131)";
        }

        /// <summary>
        /// Lưu thay đổi tài khoản vào database
        /// </summary>
        public bool SaveChanges()
        {
            try
            {
                var dbInvoice = _db.Invoices.Find(_invoice.Id);
                if (dbInvoice != null)
                {
                    dbInvoice.TK_No = TK_No;
                    dbInvoice.TK_Co = TK_Co;
                    dbInvoice.AccountType = AccountType;
                    _db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
