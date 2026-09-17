using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.ViewModels
{
    public class AdminUserViewModel : BaseViewModel
    {
        // ===== NHÂN VIÊN =====
        public ObservableCollection<User> UsersList { get; set; } = new ObservableCollection<User>();

        private User _currentUser = new User { FullName = "", Email = "", PhoneNumber = "", Role = "" };
        public User CurrentUser { get => _currentUser; set { _currentUser = value; OnPropertyChanged(); } }

        public ObservableCollection<string> Roles { get; set; } = new ObservableCollection<string>
        {
            "Admin", "Kế toán", "Quản lý kho", "Nhân viên"
        };

        // ===== KHÁCH HÀNG =====
        public ObservableCollection<Customer> CustomersList { get; set; } = new ObservableCollection<Customer>();

        private Customer _currentCustomer = new Customer();
        public Customer CurrentCustomer
        {
            get => _currentCustomer;
            set { _currentCustomer = value; OnPropertyChanged(); }
        }

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                if (_selectedCustomer != null)
                {
                    CurrentCustomer = new Customer
                    {
                        Id = _selectedCustomer.Id,
                        Name = _selectedCustomer.Name,
                        Phone = _selectedCustomer.Phone,
                        Address = _selectedCustomer.Address,
                        TaxCode = _selectedCustomer.TaxCode,
                        SalesOrderCount = _selectedCustomer.SalesOrderCount,
                        SalesOrderTotal = _selectedCustomer.SalesOrderTotal
                    };
                }
            }
        }

        // ===== COMMANDS NHÂN VIÊN =====
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }

        // ===== COMMANDS KHÁCH HÀNG =====
        public ICommand SaveCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand ClearCustomerCommand { get; }

        public AdminUserViewModel()
        {
            // Nhân viên
            SaveCommand = new RelayCommand(SaveUser);
            DeleteCommand = new RelayCommand<User>(DeleteUser);
            ClearCommand = new RelayCommand(ClearForm);

            // Khách hàng
            SaveCustomerCommand = new RelayCommand(SaveCustomer);
            DeleteCustomerCommand = new RelayCommand<Customer>(DeleteCustomer);
            ClearCustomerCommand = new RelayCommand(ClearCustomerForm);

            LoadData();
        }

        private void LoadData()
        {
            using (var db = new AppDbContext())
            {
                var users = db.Users.ToList();
                var payments = db.Payments.ToList();

                static string Norm(string? s) => (s ?? "").Trim();

                foreach (var u in users)
                {
                    var name = Norm(u.FullName);
                    var login = Norm(u.Username);
                    var matches = payments.Where(p =>
                    {
                        var sn = Norm(p.StaffName);
                        if (sn.Length == 0) return false;
                        return string.Equals(sn, name, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(sn, login, StringComparison.OrdinalIgnoreCase);
                    }).ToList();
                    u.PaymentReceiptCount = matches.Count;
                    u.PaymentTotalAmount = matches.Sum(p => p.Amount);
                }

                UsersList = new ObservableCollection<User>(users);

                var customers = db.Customers.ToList();
                var orderStats = db.SalesOrders
                    .GroupBy(o => (o.CustomerName ?? "").Trim())
                    .Select(g => new
                    {
                        Name = g.Key,
                        Cnt = g.Count(),
                        Total = g.Sum(o => o.TotalAmount > 0 ? o.TotalAmount : 0)
                    })
                    .ToList()
                    .ToDictionary(x => x.Name, x => (x.Cnt, x.Total), StringComparer.Ordinal);
                foreach (var c in customers)
                {
                    var key = (c.Name ?? "").Trim();
                    if (orderStats.TryGetValue(key, out var s))
                    {
                        c.SalesOrderCount = s.Cnt;
                        c.SalesOrderTotal = s.Total;
                    }
                    else
                    {
                        c.SalesOrderCount = 0;
                        c.SalesOrderTotal = 0;
                    }
                }
                CustomersList = new ObservableCollection<Customer>(customers);
            }
            OnPropertyChanged(nameof(UsersList));
            OnPropertyChanged(nameof(CustomersList));
        }

        // ==================== NHÂN VIÊN ====================
        private void SaveUser()
        {
            if (string.IsNullOrEmpty(CurrentUser.Username) || string.IsNullOrEmpty(CurrentUser.Role))
            {
                    MessageBox.Show("Vui lòng nhập Tên đăng nhập, Mật khẩu và chọn Quyền!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(CurrentUser.Password))
                {
                    MessageBox.Show("Vui lòng nhập Mật khẩu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

            using (var db = new AppDbContext())
            {
                if (CurrentUser.Id == 0)
                {
                    if (db.Users.Any(u => u.Username == CurrentUser.Username))
                    {
                        MessageBox.Show("Tên đăng nhập đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (string.IsNullOrEmpty(CurrentUser.Password)) CurrentUser.Password = "123456";
                    // Đảm bảo các cột NOT NULL trong DB không bị null
                    CurrentUser.FullName = CurrentUser.FullName ?? "";
                    CurrentUser.Email = CurrentUser.Email ?? "";
                    CurrentUser.Role = CurrentUser.Role ?? "";
                    CurrentUser.PhoneNumber = CurrentUser.PhoneNumber ?? "";
                    db.Users.Add(CurrentUser);
                }
                else
                {
                    db.Users.Update(CurrentUser);
                }
                db.SaveChanges();
            }

            MessageBox.Show("Lưu tài khoản thành công!", "Thành công");
            ClearForm();
            LoadData();
        }

        private void DeleteUser(User user)
        {
            if (user == null) return;

            if (user.Username == "admin")
            {
                MessageBox.Show("Không thể xóa tài khoản Admin gốc!", "Từ chối", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn xóa tài khoản {user.Username}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    var dbUser = db.Users.Find(user.Id);
                    if (dbUser != null)
                    {
                        db.Users.Remove(dbUser);
                        db.SaveChanges();
                    }
                }
                LoadData();
            }
        }

        private void ClearForm()
        {
            CurrentUser = new User { FullName = "", Email = "", PhoneNumber = "", Role = "" };
        }

        // ==================== KHÁCH HÀNG ====================
        private void SaveCustomer()
        {
            if (string.IsNullOrEmpty(CurrentCustomer.Name))
            {
                MessageBox.Show("Vui lòng nhập Tên khách hàng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentCustomer.NormalizeForDatabase();

            using (var db = new AppDbContext())
            {
                if (CurrentCustomer.Id == 0)
                {
                    if (db.Customers.Any(c => c.Name == CurrentCustomer.Name))
                    {
                        MessageBox.Show($"Khách hàng \"{CurrentCustomer.Name}\" đã tồn tại!", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    db.Customers.Add(CurrentCustomer);
                }
                else
                {
                    db.Customers.Update(CurrentCustomer);
                }
                db.SaveChanges();
            }

            MessageBox.Show("Lưu khách hàng thành công!", "Thành công");
            ClearCustomerForm();
            LoadData();
        }

        private void DeleteCustomer(Customer customer)
        {
            if (MessageBox.Show($"Bạn có chắc muốn xóa khách hàng \"{customer.Name}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    db.Customers.Remove(customer);
                    db.SaveChanges();
                }
                LoadData();
            }
        }

        private void ClearCustomerForm()
        {
            CurrentCustomer = new Customer { Name = "", Phone = "", Address = "", TaxCode = "" };
            SelectedCustomer = null;
        }
    }
}
