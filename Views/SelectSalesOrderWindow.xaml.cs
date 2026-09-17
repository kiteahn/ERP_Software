using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace PhanMemInAnERP.Views
{
    public partial class SelectSalesOrderWindow : Window
    {
        public ObservableCollection<SalesOrder> SalesOrders { get; set; }
        public SalesOrder SelectedSalesOrder { get; set; }

        public SelectSalesOrderWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoadSalesOrders();
        }

        private void LoadSalesOrders()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Chỉ load đơn hàng chưa giao
                    var orders = db.SalesOrders
                        .AsNoTracking()
                        .Where(o => o.Status == "Chưa giao" || o.Status == "Đang sản xuất")
                        .OrderByDescending(o => o.OrderDate)
                        .ToList();

                    SalesOrders = new ObservableCollection<SalesOrder>(orders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải đơn hàng: {ex.Message}", "Lỗi");
                SalesOrders = new ObservableCollection<SalesOrder>();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSalesOrder == null)
            {
                MessageBox.Show("Vui lòng chọn một đơn hàng!", "Thông báo");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
