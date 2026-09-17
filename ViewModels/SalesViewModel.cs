using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.Services;
using PhanMemInAnERP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PhanMemInAnERP.ViewModels
{
    public class SalesViewModel : BaseViewModel
    {
        public ObservableCollection<SalesOrder> OrdersList { get; set; }
        public ObservableCollection<Invoice> InvoicesList { get; set; }
        public ObservableCollection<Payment> PaymentsList { get; set; }
        public ObservableCollection<DebtReportItem> DebtReports { get; set; }

        // ===== TỔNG HỢP CÔNG NỢ PHẢI THU (TK 131) =====
        public ObservableCollection<DebtSummaryRow> DebtSummaryRows { get; set; } = new ObservableCollection<DebtSummaryRow>();

        private DateTime _debtSummaryFromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime DebtSummaryFromDate { get => _debtSummaryFromDate; set { _debtSummaryFromDate = value; OnPropertyChanged(); } }

        private DateTime _debtSummaryToDate = DateTime.Now.Date;
        public DateTime DebtSummaryToDate { get => _debtSummaryToDate; set { _debtSummaryToDate = value; OnPropertyChanged(); } }

        public ICommand LoadDebtSummaryCommand { get; }
        public ICommand ExportDebtSummaryExcelCommand { get; }

        // ===== SỔ CHI TIẾT BÁN HÀNG =====
        public ObservableCollection<SalesLedgerRow> SalesLedgerRows { get; set; } = new ObservableCollection<SalesLedgerRow>();
        private DateTime _salesLedgerFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime SalesLedgerFrom { get => _salesLedgerFrom; set { _salesLedgerFrom = value; OnPropertyChanged(); } }
        private DateTime _salesLedgerTo = DateTime.Now.Date;
        public DateTime SalesLedgerTo { get => _salesLedgerTo; set { _salesLedgerTo = value; OnPropertyChanged(); } }
        public ICommand LoadSalesLedgerCommand { get; }
        public ICommand ExportSalesLedgerExcelCommand { get; }
        public ICommand ExportAllSalesReportCommand { get; }

        private readonly List<DebtReportItem> _debtReportSource = new List<DebtReportItem>();
        public ObservableCollection<string> DebtReportSearchHints { get; } = new ObservableCollection<string>();

        private string _debtReportSearchText = "";
        public string DebtReportSearchText
        {
            get => _debtReportSearchText;
            set
            {
                var v = value ?? "";
                if (_debtReportSearchText == v) return;
                _debtReportSearchText = v;
                OnPropertyChanged();
                ApplyDebtReportFilter();
            }
        }

        public void SetDebtReportSearchFromCombo(string? selected)
        {
            if (string.IsNullOrEmpty(selected) || selected == "(Tất cả)")
                DebtReportSearchText = "";
            else
                DebtReportSearchText = selected;
        }
        public ObservableCollection<OrderPaymentStatusRow> OrderPaymentStatusList { get; set; } = new ObservableCollection<OrderPaymentStatusRow>();
        public ObservableCollection<User> AvailableStaff { get; set; } = new ObservableCollection<User>();
        public ObservableCollection<Customer> AvailableCustomers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Customer> FilteredCustomers { get; set; } = new ObservableCollection<Customer>();
        public ObservableCollection<Invoice> FilteredInvoices { get; set; } = new ObservableCollection<Invoice>();
        /// <summary>Danh bạ khách (bảng Customers) — ComboBox tạo đơn hàng.</summary>
        public ObservableCollection<Customer> OrderFormCustomers { get; } = new ObservableCollection<Customer>();

        /// <summary>Loại tài khoản cho hoá đơn.</summary>
        public ObservableCollection<string> AccountTypeOptions { get; } = new ObservableCollection<string>
        {
            "Công nợ (131)", "Tiền mặt (111)", "Ngân hàng (112)"
        };

        /// <summary>Lắng nghe event tạo đơn hàng từ Dashboard.</summary>
        private readonly Action<Quotation> _handleCreateOrder;

        private Customer? _selectedOrderCustomer;
        public Customer? SelectedOrderCustomer
        {
            get => _selectedOrderCustomer;
            set
            {
                if (ReferenceEquals(_selectedOrderCustomer, value)) return;
                _selectedOrderCustomer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    CurrentOrder.CustomerName = value.Name ?? "";
                    CurrentOrder.Phone = value.Phone ?? "";
                    if (!string.IsNullOrWhiteSpace(value.Address))
                        CurrentOrder.Address = value.Address;
                    
                    // Chỉ ghi đè TaxCode từ database nếu báo giá không có tax code
                    // Ưu tiên giữ TaxCode từ báo giá (vì khách hàng có thể đặt hàng với MST khác)
                    if (string.IsNullOrWhiteSpace(CurrentOrder.TaxCode))
                        CurrentOrder.TaxCode = value.TaxCode ?? "";

                    // Tự động tải thông tin từ báo giá gần nhất của khách hàng này
                    LoadQuotationInfoForCustomer(value.Name ?? "");

                    OnPropertyChanged(nameof(CurrentOrder));
                }
            }
        }

        /// <summary>
        /// Tự động tải Số lượng, Kích thước, Đơn giá từ báo giá gần nhất của khách hàng.
        /// </summary>
        private void LoadQuotationInfoForCustomer(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Tìm báo giá gần nhất của khách hàng này
                    var latestQuote = db.Quotations
                        .Where(q => q.CustomerName == customerName)
                        .OrderByDescending(q => q.QuoteDate)
                        .FirstOrDefault();

                    if (latestQuote != null)
                    {
                        // Tải thông tin từ báo giá
                        OrdQty = latestQuote.Quantity;
                        OrdPrice = latestQuote.QuotedUnitPrice;
                        CurrentOrder.Dimensions = latestQuote.ProductDimensions ?? "";
                        CurrentOrder.Material = latestQuote.PaperType ?? "";
                        
                        // Chỉ tải TaxCode từ báo giá nếu CurrentOrder chưa có
                        if (string.IsNullOrWhiteSpace(CurrentOrder.TaxCode))
                            CurrentOrder.TaxCode = latestQuote.CustomerTaxCode ?? "";

                        // Liên kết với báo giá đã chọn
                        CurrentOrder.QuotationId = latestQuote.Id;

                        // Cập nhật DataBridge để đồng bộ
                        PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation = latestQuote;

                        OnPropertyChanged(nameof(CurrentOrder));
                        OnPropertyChanged(nameof(OrdQty));
                        OnPropertyChanged(nameof(OrdPrice));
                        OnPropertyChanged(nameof(OrdTotal));

                        System.Diagnostics.Debug.WriteLine($"Đã tải thông tin từ báo giá {latestQuote.QuoteNo} cho khách {customerName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải thông tin báo giá: {ex.Message}");
            }
        }

        private User _selectedStaff;
        public User SelectedStaff { get => _selectedStaff; set { _selectedStaff = value; OnPropertyChanged(); } }

        private SalesOrder _currentOrder = new SalesOrder { OrderNo = "DH-" + DateTime.Now.ToString("yyMMddHHmm") };
        public SalesOrder CurrentOrder
        {
            get => _currentOrder;
            set
            {
                if (_currentOrder != value)
                {
                    _currentOrder = value;
                    OnPropertyChanged();

                    // Khi load đơn hàng cũ, cập nhật combobox tài khoản nếu có
                    if (value != null)
                    {
                        // Nếu đơn hàng có thông tin tài khoản, tìm và chọn trong combobox
                        if (!string.IsNullOrEmpty(value.TK_No))
                        {
                            var matchNo = DebitAccountOptions.FirstOrDefault(opt => opt.StartsWith(value.TK_No + " -"));
                            if (!string.IsNullOrEmpty(matchNo))
                                _selectedDebitAccount = matchNo;
                            else
                                _selectedDebitAccount = DebitAccountOptions.FirstOrDefault() ?? "";
                            OnPropertyChanged(nameof(SelectedDebitAccount));
                        }

                        if (!string.IsNullOrEmpty(value.TK_Co))
                        {
                            var matchCo = CreditAccountOptions.FirstOrDefault(opt => opt.StartsWith(value.TK_Co + " -"));
                            if (!string.IsNullOrEmpty(matchCo))
                                _selectedCreditAccount = matchCo;
                            else
                                _selectedCreditAccount = CreditAccountOptions.FirstOrDefault() ?? "";
                            OnPropertyChanged(nameof(SelectedCreditAccount));
                        }
                    }
                }
            }
        }

        // === COMBOBOX TÀI KHOẢN HẠCH TOÁN ===
        public ObservableCollection<string> DebitAccountOptions { get; } = new ObservableCollection<string>
        {
            "131 - Phải thu của khách hàng",
            "136 - Phải thu nội bộ",
            "138 - Phải thu khác",
            "511 - Doanh thu bán hàng và cung cấp dịch vụ",
            "515 - Doanh thu hoạt động tài chính"
        };

        public ObservableCollection<string> CreditAccountOptions { get; } = new ObservableCollection<string>
        {
            "511 - Doanh thu bán hàng và cung cấp dịch vụ",
            "3331 - Thuế GTGT phải nộp",
            "131 - Phải thu của khách hàng",
            "111 - Tiền mặt",
            "112 - Tiền gửi Ngân hàng"
        };

        private string _selectedDebitAccount;
        public string SelectedDebitAccount
        {
            get => _selectedDebitAccount;
            set { _selectedDebitAccount = value; OnPropertyChanged(); }
        }

        private string _selectedCreditAccount;
        public string SelectedCreditAccount
        {
            get => _selectedCreditAccount;
            set { _selectedCreditAccount = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Trích xuất mã tài khoản từ chuỗi hiển thị (ví dụ: "511 - Doanh thu..." → "511")
        /// </summary>
        private string ExtractAccountCode(string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
                return "";

            displayText = displayText.Trim();
            int separatorPos = displayText.IndexOf(" - ");
            if (separatorPos > 0)
                return displayText.Substring(0, separatorPos);

            return displayText;
        }

        private double _ordQty; public double OrdQty { get => _ordQty; set { _ordQty = value; OnPropertyChanged(); OnPropertyChanged(nameof(OrdTotal)); } }
        private double _ordPrice; public double OrdPrice { get => _ordPrice; set { _ordPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(OrdTotal)); } }
        public double OrdTotal => OrdQty * OrdPrice;

        private string _deliveryTimeText = "";
        public string DeliveryTimeText
        {
            get => _deliveryTimeText;
            set
            {
                _deliveryTimeText = value;
                OnPropertyChanged();
            }
        }

        // ===== CHỌN GIỜ GIAO Hàng =====
        public ObservableCollection<int> AvailableHours { get; } = new ObservableCollection<int>(Enumerable.Range(0, 24));
        public ObservableCollection<int> AvailableMinutes { get; } = new ObservableCollection<int>(Enumerable.Range(0, 60));

        private int _selectedHour = 8;
        public int SelectedHour { get => _selectedHour; set { _selectedHour = value; OnPropertyChanged(); } }

        private int _selectedMinute = 0;
        public int SelectedMinute { get => _selectedMinute; set { _selectedMinute = value; OnPropertyChanged(); } }

        private Invoice _currentInvoice = new Invoice { InvoiceNo = "HD-" + DateTime.Now.ToString("HHmmss") };
        public Invoice CurrentInvoice { get => _currentInvoice; set { _currentInvoice = value; OnPropertyChanged(); } }

        private double _invSubTotal; public double InvSubTotal { get => _invSubTotal; set { _invSubTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(InvTotal)); } }
        private double _invVAT = 10; public double InvVAT { get => _invVAT; set { _invVAT = value; OnPropertyChanged(); OnPropertyChanged(nameof(InvTotal)); } }
        public double InvTotal => InvSubTotal * (1 + InvVAT / 100.0);

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set { _selectedInvoice = value; OnPropertyChanged(); }
        }
        public ICommand ProcessPaymentCommand => new RelayCommand<string>((p) => ProcessPayment(p));
        public void ProcessPayment(string method)
        {
            if (SelectedDebtForPayment == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn từ danh sách nợ!");
                return;
            }

            if (method == "Ghi nợ")
            {
                MessageBox.Show($"🏦 Hóa đơn {SelectedDebtForPayment.InvoiceNo} đã được ghi nhận vào Công nợ.");
            }
            else
            {
                // Thanh toán ngay (Tiền mặt hoặc Chuyển khoản)
                using (var db = new AppDbContext())
                {
                    var stamp = GetPaymentTimestamp();
                    var payment = new Payment
                    {
                        ReceiptNo = "PT-" + stamp.ToString("yyMMddHHmm"),
                        PaymentDate = stamp,
                        CustomerName = SelectedDebtForPayment.CustomerName,
                        RefInvoiceNo = SelectedDebtForPayment.InvoiceNo,
                        Amount = SelectedDebtForPayment.RemainingDebt, // Thu hết số tiền đang nợ
                        PaymentMethod = method,
                        BankAccount = method == "Chuyển khoản" ? BankTransferInfo.AccountNumber : "Tiền mặt",
                        BankName = method == "Chuyển khoản" ? BankTransferInfo.BankName : "N/A",
                        Notes = "Thu tiền hóa đơn " + SelectedDebtForPayment.InvoiceNo,
                        StaffName = SelectedStaff?.FullName ?? "Admin/Hệ thống"
                    };
                    db.Payments.Add(payment);
                    db.SaveChanges();
                }
                MessageBox.Show($"✅ Xác nhận thanh toán {method} cho hóa đơn {SelectedDebtForPayment.InvoiceNo}");
            }

            LoadData(); // Cập nhật lại biểu đồ và danh sách nợ
        }

        private SalesOrder _selectedOrderForInvoice;
        public SalesOrder SelectedOrderForInvoice
        {
            get => _selectedOrderForInvoice;
            set
            {
                _selectedOrderForInvoice = value;
                OnPropertyChanged();
                if (_selectedOrderForInvoice != null)
                {
                    CurrentInvoice.RefOrderNo = _selectedOrderForInvoice.OrderNo;
                    CurrentInvoice.CustomerName = _selectedOrderForInvoice.CustomerName;
                    CurrentInvoice.TaxCode = _selectedOrderForInvoice.TaxCode;

                    // Copy tài khoản từ đơn hàng
                    CurrentInvoice.TK_No = _selectedOrderForInvoice.TK_No ?? "131";
                    CurrentInvoice.TK_Co = _selectedOrderForInvoice.TK_Co ?? "511";

                    InvSubTotal = _selectedOrderForInvoice.TotalAmount;

                    OnPropertyChanged(nameof(CurrentInvoice));
                }
            }
        }

        private Customer? _selectedCustomerForInvoice;
        public Customer? SelectedCustomerForInvoice
        {
            get => _selectedCustomerForInvoice;
            set
            {
                if (ReferenceEquals(_selectedCustomerForInvoice, value)) return;
                _selectedCustomerForInvoice = value;
                OnPropertyChanged();
                if (value != null)
                {
                    InvoiceCustomerName = value.Name ?? "";
                    CurrentInvoice.TaxCode = value.TaxCode ?? "";
                    OnPropertyChanged(nameof(CurrentInvoice));
                }
            }
        }

        private string _invoiceCustomerName;
        public string InvoiceCustomerName
        {
            get => _invoiceCustomerName;
            set
            {
                if (_invoiceCustomerName == value) return;
                _invoiceCustomerName = value;
                OnPropertyChanged();
                if (CurrentInvoice != null)
                {
                    CurrentInvoice.CustomerName = value ?? "";
                }
                UpdateFilteredOrdersForInvoice();
            }
        }

        public ObservableCollection<SalesOrder> FilteredOrdersForInvoice { get; } = new ObservableCollection<SalesOrder>();

        private void UpdateFilteredOrdersForInvoice()
        {
            FilteredOrdersForInvoice.Clear();
            
            string selectedCustomer = (InvoiceCustomerName ?? "").Trim();
            if (string.IsNullOrEmpty(selectedCustomer)) return;

            // Lấy danh sách OrderNo đã có hóa đơn
            var invoicedOrderNos = InvoicesList.Select(i => (i.RefOrderNo ?? "").Trim()).ToList();

            var matchingOrders = OrdersList.Where(o => 
                string.Equals((o.CustomerName ?? "").Trim(), selectedCustomer, StringComparison.OrdinalIgnoreCase) &&
                !invoicedOrderNos.Contains((o.OrderNo ?? "").Trim())
            ).ToList();

            foreach (var order in matchingOrders)
                FilteredOrdersForInvoice.Add(order);
        }

        private Payment _currentPayment = new Payment { ReceiptNo = "PT-" + DateTime.Now.ToString("yyMMddHHmm"), PaymentMethod = "Tiền mặt" };
        public Payment CurrentPayment { get => _currentPayment; set { _currentPayment = value; OnPropertyChanged(); } }

        // --- CUSTOMER-CENTRIC DEBT PROPERTIES ---
        private string _searchCustomerName;
        public string SearchCustomerName
        {
            get => _searchCustomerName;
            set
            {
                _searchCustomerName = value;
                OnPropertyChanged();
                FilterCustomers();
                UpdateCustomerDebtSummary();
            }
        }

        private void FilterCustomers()
        {
            FilteredCustomers.Clear();
            if (string.IsNullOrWhiteSpace(SearchCustomerName))
            {
                // Hiển thị tất cả khách hàng
                foreach (var c in AvailableCustomers)
                    FilteredCustomers.Add(c);
            }
            else
            {
                string search = SearchCustomerName.Trim().ToLower();
                var filtered = AvailableCustomers.Where(c => c.Name?.ToLower().Contains(search) == true);
                foreach (var c in filtered)
                    FilteredCustomers.Add(c);
            }
        }

        /// <summary>
        /// Load danh sách hoá đơn cho khách hàng được chọn
        /// </summary>
        private void LoadInvoicesForCustomer(string customerName)
        {
            FilteredInvoices.Clear();
            if (string.IsNullOrWhiteSpace(customerName)) return;

            // Đảm bảo InvoicesList đã được load
            if (InvoicesList == null || !InvoicesList.Any())
            {
                using (var db = new AppDbContext())
                {
                    var invoices = db.Invoices
                        .Where(i => i.CustomerName == customerName)
                        .OrderByDescending(i => i.InvoiceDate)
                        .ToList();
                    foreach (var inv in invoices)
                        FilteredInvoices.Add(inv);
                }
            }
            else
            {
                var invoices = InvoicesList.Where(i => i.CustomerName == customerName).ToList();
                foreach (var inv in invoices)
                    FilteredInvoices.Add(inv);
            }
        }

        private CustomerDebtSummary _customerDebtInfo;
        public CustomerDebtSummary CustomerDebtInfo
        {
            get => _customerDebtInfo;
            set { _customerDebtInfo = value; OnPropertyChanged(); }
        }

        private Customer _selectedDebtCustomer;
        public Customer SelectedDebtCustomer
        {
            get => _selectedDebtCustomer;
            set
            {
                if (ReferenceEquals(_selectedDebtCustomer, value)) return;
                _selectedDebtCustomer = value;
                OnPropertyChanged();
                if (_selectedDebtCustomer != null)
                {
                    SearchCustomerName = _selectedDebtCustomer.Name;
                    LoadInvoicesForCustomer(_selectedDebtCustomer.Name);
                    UpdateCustomerDebtSummary();
                }
            }
        }

        private double _manualPaymentAmount;
        public double ManualPaymentAmount
        {
            get => _manualPaymentAmount;
            set
            {
                if (value < 0) value = 0;
                _manualPaymentAmount = value;
                OnPropertyChanged();
            }
        }

        public ICommand SmartPaymentCommand { get; }
        public ICommand OpenBankTransferDialogCommand { get; }
        /// <summary>Mở hộp thoại nhập số tiền thu một phần + xác nhận 2 bước rồi ghi nhận thu tiền mặt.</summary>
        public ICommand OpenPartialPaymentWizardCommand { get; }
        public ICommand ClearPaymentDateFilterCommand { get; }

        private bool _isBankTransfer;
        public bool IsBankTransfer { get => _isBankTransfer; set { _isBankTransfer = value; OnPropertyChanged(); } }

        // === BỘ LỌC NGÀY PHIẾU THU ===
        private DateTime? _paymentFilterFromDate;
        public DateTime? PaymentFilterFromDate
        {
            get => _paymentFilterFromDate;
            set { _paymentFilterFromDate = value; OnPropertyChanged(); ApplyPaymentDateFilter(); }
        }

        private DateTime? _paymentFilterToDate;
        public DateTime? PaymentFilterToDate
        {
            get => _paymentFilterToDate;
            set { _paymentFilterToDate = value; OnPropertyChanged(); ApplyPaymentDateFilter(); }
        }

        public ObservableCollection<Payment> FilteredPaymentsList { get; set; } = new ObservableCollection<Payment>();
        private List<Payment> _paymentsSource = new List<Payment>();

        private DebtReportItem _selectedDebtForPayment;
        public DebtReportItem SelectedDebtForPayment
        {
            get => _selectedDebtForPayment;
            set
            {
                _selectedDebtForPayment = value;
                OnPropertyChanged();
                if (_selectedDebtForPayment != null)
                {
                    // Tự copy thông tin sang Phiếu Thu
                    CurrentPayment.RefInvoiceNo = _selectedDebtForPayment.InvoiceNo;
                    CurrentPayment.CustomerName = _selectedDebtForPayment.CustomerName;

                    // Gợi ý số tiền thu bằng đúng số tiền khách còn nợ
                    CurrentPayment.Amount = _selectedDebtForPayment.RemainingDebt;

                    OnPropertyChanged(nameof(CurrentPayment));
                }
            }
        }

        // --- PROPERTIES BÁO CÁO CÔNG NỢ ---
        private double _totalDebt; public double TotalDebt { get => _totalDebt; set { _totalDebt = value; OnPropertyChanged(); } }
        private double _overdueDebt; public double OverdueDebt { get => _overdueDebt; set { _overdueDebt = value; OnPropertyChanged(); } }
        private int _debtCustomerCount; public int DebtCustomerCount { get => _debtCustomerCount; set { _debtCustomerCount = value; OnPropertyChanged(); } }

        // --- TỔNG HỢP TẤT CẢ KHÁCH HÀNG (THU TIỀN) ---
        private double _totalInvoicedAll; public double TotalInvoicedAll { get => _totalInvoicedAll; set { _totalInvoicedAll = value; OnPropertyChanged(); } }
        private double _totalCollectedAll; public double TotalCollectedAll { get => _totalCollectedAll; set { _totalCollectedAll = value; OnPropertyChanged(); } }
        private double _totalRemainingAll; public double TotalRemainingAll { get => _totalRemainingAll; set { _totalRemainingAll = value; OnPropertyChanged(); } }

        /// <summary>Ngày ghi trên phiếu thu (tránh phụ thuộc hoàn toàn vào đồng hệ thống nếu sai năm/tháng).</summary>
        private DateTime _paymentCollectionDate = DateTime.Today;

        public DateTime PaymentCollectionDate
        {
            get => _paymentCollectionDate;
            set
            {
                var d = value.Date;
                if (_paymentCollectionDate == d) return;
                _paymentCollectionDate = d;
                OnPropertyChanged();
            }
        }

        /// <summary>Ngày đã chọn + giờ phút giây hiện tại (mốc thời gian lưu DB).</summary>
        private DateTime GetPaymentTimestamp()
        {
            return PaymentCollectionDate.Date.Add(DateTime.Now.TimeOfDay);
        }

        // COMMANDS
        public ICommand SaveOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand EditInvoiceAccountCommand { get; }
        public ICommand DeletePaymentCommand { get; }
        public ICommand SavePaymentCommand { get; }
        public ICommand PaymentMethodChangedCommand { get; } // Xử lý ẩn hiện ô Ngân hàng
        public ICommand RefreshPaymentStatusTabCommand { get; }
        public ICommand PreviewOrderCommand { get; }

        public SalesViewModel()
        {
            SaveOrderCommand = new RelayCommand(SaveOrder);
            DeleteOrderCommand = new RelayCommand<SalesOrder>(DeleteOrder);
            SaveInvoiceCommand = new RelayCommand(SaveInvoice);
            DeleteInvoiceCommand = new RelayCommand<Invoice>(DeleteInvoice);
            EditInvoiceAccountCommand = new RelayCommand<Invoice>(EditInvoiceAccount);
            DeletePaymentCommand = new RelayCommand<Payment>(DeletePayment);
            SavePaymentCommand = new RelayCommand<string>(ExecuteSavePayment);
            PaymentMethodChangedCommand = new RelayCommand(CheckPaymentMethod);
            SmartPaymentCommand = new RelayCommand<string>(ExecuteSmartPayment, CanExecuteSmartPayment);
            OpenBankTransferDialogCommand = new RelayCommand(OpenBankTransferDialog, () => CanExecuteSmartPayment("Chuyển khoản"));
            OpenPartialPaymentWizardCommand = new RelayCommand(OpenPartialPaymentWizard, CanOpenPartialPaymentWizard);
            RefreshPaymentStatusTabCommand = new RelayCommand(LoadData);
            PreviewOrderCommand = new RelayCommand<SalesOrder>(PreviewOrder);
            ClearPaymentDateFilterCommand = new RelayCommand(ClearPaymentDateFilter);
            LoadDebtSummaryCommand = new RelayCommand(LoadDebtSummary);
            ExportDebtSummaryExcelCommand = new RelayCommand(ExportDebtSummaryExcel);
            LoadSalesLedgerCommand = new RelayCommand(LoadSalesLedger);
            ExportSalesLedgerExcelCommand = new RelayCommand(ExportSalesLedgerExcel);
            ExportAllSalesReportCommand = new RelayCommand(ExportAllSalesReportExcel);

            // Khởi tạo giá trị mặc định cho tài khoản hạch toán
            if (DebitAccountOptions.Any())
                _selectedDebitAccount = DebitAccountOptions.First();
            if (CreditAccountOptions.Any())
                _selectedCreditAccount = CreditAccountOptions.First();

            // Lắng nghe event tạo đơn hàng từ Dashboard (truyền báo giá)
            PhanMemInAnERP.Helpers.DataBridge.OnRequestCreateOrder += ApplyQuotationToOrder;

            // Lắng nghe event dữ liệu thay đổi từ các màn hình khác (xóa, thêm, sửa)
            PhanMemInAnERP.Helpers.DataBridge.OnDataChanged += LoadData;

            LoadData();
            LoadStaff();
        }

        /// <summary>
        /// Điền thông tin từ Báo Giá vào form Đơn Hàng.
        /// Được gọi khi Dashboard gửi event tạo đơn từ báo giá.
        /// </summary>
        private void ApplyQuotationToOrder(Quotation quote)
        {
            if (quote == null) return;

            // Lưu lại tax code từ báo giá (ưu tiên từ báo giá nếu khách không có trong danh bạ)
            string taxCodeFromQuote = quote.CustomerTaxCode ?? "";
            string addressFromQuote = quote.CustomerAddress ?? "";

            CurrentOrder = new SalesOrder
            {
                OrderNo = "DH-" + DateTime.Now.ToString("yyMMddHHmm"),
                OrderDate = DateTime.Now,
                Deadline = DateTime.Now.Date.AddDays(quote.DeliveryDays > 0 ? quote.DeliveryDays : 7),
                DeliveryTime = null,
                CustomerName = quote.CustomerName ?? "",
                ProductName = quote.ProductName ?? "",
                Dimensions = quote.ProductDimensions ?? "",
                Material = quote.PaperType ?? "",
                TaxCode = taxCodeFromQuote,  // Sử dụng tax code từ báo giá
                Address = addressFromQuote,  // Sử dụng địa chỉ từ báo giá
                UserId = AppSession.CurrentUser?.Id ?? 1,
                QuotationId = quote.Id,
                Quantity = 0,
                UnitPrice = 0,
                TotalAmount = 0
            };
            SelectedHour = 8;
            SelectedMinute = 0;
            DeliveryTimeText = "";

            OrdQty = quote.Quantity;
            OrdPrice = quote.QuotedUnitPrice;

            OnPropertyChanged(nameof(CurrentOrder));
            OnPropertyChanged(nameof(OrdQty));
            OnPropertyChanged(nameof(OrdPrice));
            OnPropertyChanged(nameof(OrdTotal));

            // Khớp khách hàng trong ComboBox nếu đã có
            bool customerMatched = false;
            if (!string.IsNullOrWhiteSpace(CurrentOrder.CustomerName))
            {
                var matched = OrderFormCustomers.FirstOrDefault(c =>
                    string.Equals((c.Name ?? "").Trim(), CurrentOrder.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    SelectedOrderCustomer = matched;
                    customerMatched = true;
                }
            }

            // Nếu không tìm thấy khách trong danh bạ, vẫn giữ thông tin từ báo giá
            if (!customerMatched)
            {
                // Đảm bảo TaxCode và Address từ báo giá được giữ lại
                CurrentOrder.TaxCode = taxCodeFromQuote;
                CurrentOrder.Address = addressFromQuote;
                OnPropertyChanged(nameof(CurrentOrder));
            }
        }

        private void LoadStaff()
        {
            using (var db = new AppDbContext())
            {
                var staff = db.Users.ToList();
                AvailableStaff.Clear();
                foreach (var s in staff) AvailableStaff.Add(s);

                // Mặc định chọn user đang đăng nhập
                if (AppSession.CurrentUser != null)
                {
                    SelectedStaff = AvailableStaff.FirstOrDefault(u => u.Id == AppSession.CurrentUser.Id);
                }
                else if (AvailableStaff.Any())
                {
                    SelectedStaff = AvailableStaff.First();
                }
            }
        }

        /// <summary>Hóa đơn dùng cho tab Thu tiền — khớp chọn từ combo (tên đúng) hoặc tìm Contains.</summary>
        private System.Collections.Generic.List<Invoice> GetInvoicesForThuTien(AppDbContext db)
        {
            if (string.IsNullOrWhiteSpace(SearchCustomerName))
                return new System.Collections.Generic.List<Invoice>();

            string typed = SearchCustomerName.Trim();
            if (SelectedDebtCustomer != null &&
                string.Equals((SelectedDebtCustomer.Name ?? "").Trim(), typed, StringComparison.OrdinalIgnoreCase))
            {
                string exact = SelectedDebtCustomer.Name.Trim();
                var list = db.Invoices.Where(i => i.CustomerName == exact).ToList();
                if (list.Count == 0)
                {
                    list = db.Invoices
                        .AsEnumerable()
                        .Where(i => string.Equals((i.CustomerName ?? "").Trim(), exact, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                return list;
            }

            return db.Invoices
                .Where(i => (i.CustomerName ?? "").Contains(typed))
                .ToList();
        }

        private void UpdateCustomerDebtSummary()
        {
            if (string.IsNullOrWhiteSpace(SearchCustomerName))
            {
                CustomerDebtInfo = null;
                ManualPaymentAmount = 0;
                CommandManager.InvalidateRequerySuggested();
                return;
            }

            using (var db = new AppDbContext())
            {
                string typed = SearchCustomerName.Trim();
                var invoices = GetInvoicesForThuTien(db);
                var invoiceNos = invoices.Select(i => i.InvoiceNo).ToList();

                double totalInv = invoices.Sum(i => i.TotalAmount);
                // Chỉ cộng tiền đã thu vào đúng các HĐ của khách — tránh phiếu chỉ trùng tên nhưng không có HĐ → âm nợ
                double totalPaid = invoiceNos.Count == 0
                    ? 0
                    : db.Payments.Where(p => invoiceNos.Contains(p.RefInvoiceNo)).Sum(p => p.Amount);

                CustomerDebtInfo = new CustomerDebtSummary
                {
                    CustomerName = typed,
                    TotalInvoiced = totalInv,
                    TotalPaid = totalPaid
                };

                ManualPaymentAmount = CustomerDebtInfo.CurrentBalance;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecuteSmartPayment(string method)
        {
            return CustomerDebtInfo != null && ManualPaymentAmount > 0;
        }

        private void ExecuteSmartPayment(string method)
        {
            TryExecuteSmartPayment(method);
        }

        /// <summary>Ghi nhận thu tiền chuyển khoản (gọi từ cửa sổ QR). Trả về false nếu không lưu được.</summary>
        public bool ConfirmBankTransferPayment()
        {
            return TryExecuteSmartPayment("Chuyển khoản");
        }

        private bool TryExecuteSmartPayment(string method)
        {
            if (CustomerDebtInfo == null || ManualPaymentAmount <= 0)
            {
                MessageBox.Show("Vui lòng chọn khách hàng và nhập số tiền lớn hơn 0!");
                return false;
            }

            DateTime stamp0 = GetPaymentTimestamp();
            string receiptNo = "PT-" + stamp0.ToString("yyMMddHHmmss");
            double amountToDistribute = ManualPaymentAmount;
            double originalAmount = ManualPaymentAmount; // Lưu số tiền gốc để hiển thị thông báo

            try
            {
                using (var db = new AppDbContext())
                {
                    var outstandingInvoices = GetInvoicesForThuTien(db)
                        .OrderBy(i => i.InvoiceDate)
                        .ToList();

                    bool atLeastOnePaid = false;

                    foreach (var inv in outstandingInvoices)
                    {
                        if (amountToDistribute <= 0) break;

                        double invPaid = db.Payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).Sum(p => p.Amount);
                        double invRemaining = inv.TotalAmount - invPaid;

                        if (invRemaining > 0)
                        {
                            double payForThisInv = Math.Min(amountToDistribute, invRemaining);

                            var p = new Payment
                            {
                                ReceiptNo = receiptNo,
                                PaymentDate = stamp0,
                                CustomerName = CustomerDebtInfo.CustomerName,
                                Amount = payForThisInv,
                                PaymentMethod = method,
                                BankAccount = method == "Chuyển khoản" ? BankTransferInfo.AccountNumber : "Tiền mặt",
                                BankName = method == "Chuyển khoản" ? BankTransferInfo.BankName : "N/A",
                                Notes = $"Thu tiền tự động - Hóa đơn {inv.InvoiceNo}",
                                RefInvoiceNo = inv.InvoiceNo,
                                StaffName = SelectedStaff?.FullName ?? "Admin/Hệ thống",
                                AccountType = inv.AccountType, // Lấy loại tài khoản từ hoá đơn
                                InvoiceId = inv.Id
                            };

                            db.Payments.Add(p);
                            amountToDistribute -= payForThisInv;
                            atLeastOnePaid = true;
                        }
                    }

                    if (amountToDistribute > 0)
                    {
                        var pExtra = new Payment
                        {
                            ReceiptNo = receiptNo,
                            PaymentDate = stamp0,
                            CustomerName = CustomerDebtInfo.CustomerName,
                            Amount = amountToDistribute,
                            PaymentMethod = method,
                            BankAccount = method == "Chuyển khoản" ? BankTransferInfo.AccountNumber : "Tiền mặt",
                            BankName = method == "Chuyển khoản" ? BankTransferInfo.BankName : "N/A",
                            Notes = atLeastOnePaid ? "Tiền dư sau khi trừ hết nợ" : "Khách đưa dư/Trả trước (Chưa có hóa đơn)",
                            RefInvoiceNo = "-",
                            StaffName = SelectedStaff?.FullName ?? "Admin/Hệ thống"
                        };
                        db.Payments.Add(pExtra);
                    }

                    db.SaveChanges();

                    // Cập nhật trạng thái hoá đơn
                    var allAffectedInvoices = GetInvoicesForThuTien(db).Where(i => i.Status == "Chưa thu").ToList();
                    foreach (var inv in allAffectedInvoices)
                    {
                        double totalPaid = db.Payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).Sum(p => p.Amount);
                        if (totalPaid >= inv.TotalAmount)
                        {
                            inv.Status = "Đã thu";
                            inv.PaymentDate = stamp0;
                            db.Entry(inv).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        }
                    }
                    db.SaveChanges();
                }

                MessageBox.Show($"✅ Đã thu {originalAmount:N0} đ từ khách {CustomerDebtInfo.CustomerName} thành công!");
                // Đặt bộ lọc về ngày hôm nay ĐỂ TRƯỚC khi LoadData để danh sách lọc đúng
                PaymentFilterFromDate = DateTime.Today;
                PaymentFilterToDate = DateTime.Today;
                LoadData();
                // Cập nhật lại số tiền nợ còn lại sau khi thanh toán
                UpdateCustomerDebtSummary();
                // Nếu đã trả hết nợ thì reset ManualPaymentAmount về 0
                if (CustomerDebtInfo != null && CustomerDebtInfo.CurrentBalance <= 0)
                {
                    ManualPaymentAmount = 0;
                    OnPropertyChanged(nameof(ManualPaymentAmount));
                }
                return true;
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Lỗi: " + detail, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void OpenBankTransferDialog()
        {
            if (CustomerDebtInfo == null || ManualPaymentAmount <= 0)
            {
                MessageBox.Show("Vui lòng chọn khách hàng và nhập số tiền lớn hơn 0!");
                return;
            }

            var dlg = new Views.BankTransferDialog(this);
            dlg.Owner = Application.Current?.MainWindow;
            dlg.ShowDialog();
        }

        private bool CanOpenPartialPaymentWizard()
        {
            return CustomerDebtInfo != null && CustomerDebtInfo.CurrentBalance > 0;
        }

        private void OpenPartialPaymentWizard()
        {
            if (!CanOpenPartialPaymentWizard())
            {
                MessageBox.Show("Chưa chọn khách hàng hoặc khách không còn công nợ để thu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            double maxThu = CustomerDebtInfo!.CurrentBalance;
            var inputDlg = new Views.PartialPaymentInputDialog(maxThu)
            {
                Owner = Application.Current?.MainWindow
            };
            if (inputDlg.ShowDialog() != true)
                return;

            double amt = inputDlg.EnteredAmount;
            if (amt <= 0)
                return;

            var confirm = MessageBox.Show(
                $"Số tiền thu có phải là {amt:N0} đ không?\n\nVui lòng kiểm tra lại trước khi ghi nhận vào hệ thống.",
                "Xác nhận số tiền thu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
                return;

            ManualPaymentAmount = amt;
            OnPropertyChanged(nameof(ManualPaymentAmount));
            TryExecuteSmartPayment("Tiền mặt");
        }

        private void ExecuteSavePayment(string method)
        {
            if (SelectedDebtForPayment == null)
            {
                MessageBox.Show("Vui lòng chọn 1 hóa đơn nợ!"); return;
            }

            if (method == "Ghi nợ")
            {
                MessageBox.Show($"🏦 Hóa đơn {SelectedDebtForPayment.InvoiceNo} đã nằm trong Công nợ.");
            }
            else
            {
                using (var db = new AppDbContext())
                {
                    var stamp2 = GetPaymentTimestamp();
                    db.Payments.Add(new Payment
                    {
                        ReceiptNo = "PT-" + stamp2.ToString("yyMMddHHmm"),
                        PaymentDate = stamp2,
                        CustomerName = SelectedDebtForPayment.CustomerName,
                        RefInvoiceNo = SelectedDebtForPayment.InvoiceNo,
                        Amount = SelectedDebtForPayment.RemainingDebt,
                        PaymentMethod = method,
                        BankAccount = method == "Chuyển khoản" ? BankTransferInfo.AccountNumber : "Tiền mặt",
                        BankName = method == "Chuyển khoản" ? BankTransferInfo.BankName : "N/A",
                        Notes = "Thu tiền hóa đơn " + SelectedDebtForPayment.InvoiceNo,
                        StaffName = SelectedStaff?.FullName ?? "Admin/Hệ thống"
                    });
                    db.SaveChanges();
                }
                MessageBox.Show($"✅ Đã thu {method} thành công!");
                // Đặt bộ lọc về ngày hôm nay ĐỂ TRƯỚC khi LoadData để danh sách lọc đúng
                PaymentFilterFromDate = DateTime.Today;
                PaymentFilterToDate = DateTime.Today;
            }
            SelectedDebtForPayment = null;
            LoadData();
        }

        private void CheckPaymentMethod()
        {
            IsBankTransfer = CurrentPayment.PaymentMethod == "Chuyển khoản";
        }

        private void LoadData()
        {
            using (var db = new AppDbContext())
            {
                var orders = db.SalesOrders.OrderByDescending(x => x.OrderDate).ToList();
                var invoices = db.Invoices.ToList();
                var payments = db.Payments.OrderByDescending(x => x.PaymentDate).ToList();

                OrdersList = new ObservableCollection<SalesOrder>(orders);
                OnPropertyChanged(nameof(OrdersList));
                InvoicesList = new ObservableCollection<Invoice>(db.Invoices.OrderByDescending(x => x.InvoiceDate).ToList());
                OnPropertyChanged(nameof(InvoicesList));

                // Lưu source để lọc
                _paymentsSource = payments;
                ApplyPaymentDateFilter();

                RebuildOrderPaymentStatusList(orders, invoices, payments);

                // Chỉ lấy danh sách khách hàng đã từng được xuất hóa đơn (có phát sinh nợ)
                var debtorNames = db.Invoices.Select(i => i.CustomerName).Distinct().OrderBy(name => name).ToList();
                AvailableCustomers.Clear();
                foreach (var name in debtorNames)
                {
                    AvailableCustomers.Add(new Customer { Name = name });
                }

                // Khởi tạo FilteredCustomers với tất cả khách hàng
                FilteredCustomers.Clear();
                foreach (var c in AvailableCustomers)
                    FilteredCustomers.Add(c);

                OrderFormCustomers.Clear();
                var customerService = new CustomerService(db);
                foreach (var c in customerService.GetDanhSachChonKhach())
                    OrderFormCustomers.Add(c);

                CalculateDebtReport(db);
            }
        }

        /// <summary>Khớp mục chọn ComboBox khi tên khách đã có trên đơn (vd. từ báo giá).</summary>
        private void SyncOrderCustomerComboSelection()
        {
            var name = (CurrentOrder.CustomerName ?? "").Trim();
            Customer? match = string.IsNullOrEmpty(name)
                ? null
                : OrderFormCustomers.FirstOrDefault(c =>
                    string.Equals((c.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (!ReferenceEquals(_selectedOrderCustomer, match))
            {
                _selectedOrderCustomer = match;
                OnPropertyChanged(nameof(SelectedOrderCustomer));
            }
        }

        private void RebuildOrderPaymentStatusList(
            System.Collections.Generic.List<SalesOrder> orders,
            System.Collections.Generic.List<Invoice> invoices,
            System.Collections.Generic.List<Payment> payments)
        {
            const double eps = 1.0;
            OrderPaymentStatusList.Clear();

            foreach (var order in orders)
            {
                string ono = order.OrderNo ?? "";
                var invsForOrder = invoices
                    .Where(i => string.Equals((i.RefOrderNo ?? "").Trim(), ono.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(i => i.InvoiceDate)
                    .ToList();

                if (invsForOrder.Count == 0)
                {
                    OrderPaymentStatusList.Add(new OrderPaymentStatusRow
                    {
                        OrderNo = ono,
                        CustomerName = order.CustomerName ?? "",
                        OrderDate = order.OrderDate,
                        InvoiceNo = "—",
                        OrderTotal = order.TotalAmount,
                        InvoiceTotal = 0,
                        PaidAmount = 0,
                        Remaining = 0,
                        PaymentStatus = "Chưa có hóa đơn"
                    });
                    continue;
                }

                double invoiceTotal = invsForOrder.Sum(i => i.TotalAmount);
                double paid = 0;
                foreach (var inv in invsForOrder)
                    paid += payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).Sum(p => p.Amount);

                double remaining = Math.Max(0, invoiceTotal - paid);
                string invNoLabel = invsForOrder.Count == 1
                    ? (invsForOrder[0].InvoiceNo ?? "")
                    : $"{invsForOrder[0].InvoiceNo} (+{invsForOrder.Count - 1})";

                string status;
                if (remaining <= eps)
                    status = "Đã thanh toán";
                else if (paid > eps)
                    status = "Thanh toán một phần";
                else
                    status = "Chưa thanh toán";

                OrderPaymentStatusList.Add(new OrderPaymentStatusRow
                {
                    OrderNo = ono,
                    CustomerName = order.CustomerName ?? "",
                    OrderDate = order.OrderDate,
                    InvoiceNo = invNoLabel,
                    OrderTotal = order.TotalAmount,
                    InvoiceTotal = invoiceTotal,
                    PaidAmount = paid,
                    Remaining = remaining,
                    PaymentStatus = status
                });
            }

            OnPropertyChanged(nameof(OrderPaymentStatusList));
        }

        double defaultVAT = 0.10;

        private void DeleteOrder(SalesOrder order)
        {
            if (order == null) return;

            if (MessageBox.Show(
                    $"Bạn có chắc muốn xóa đơn hàng \"{order.OrderNo}\"?\nHóa đơn, phiếu thu gắn hóa đơn và lệnh sản xuất liên quan cũng sẽ bị xóa.",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                // Lưu OrderNo để so sánh
                string deletedOrderNo = order.OrderNo ?? "";
                bool isDeletingCurrentOrder = (CurrentOrder?.OrderNo ?? "") == deletedOrderNo;

                using (var db = new AppDbContext())
                {
                    var entity = db.SalesOrders.Find(order.Id);
                    if (entity == null) return;

                    string orderNo = entity.OrderNo ?? "";

                    // Xóa lệnh sản xuất liên quan
                    foreach (var po in db.ProductionOrders.Where(p => p.SalesOrderId == entity.Id).ToList())
                        db.ProductionOrders.Remove(po);

                    // Xóa hóa đơn và phiếu thu liên quan
                    if (!string.IsNullOrWhiteSpace(orderNo))
                    {
                        foreach (var inv in db.Invoices.Where(i => i.RefOrderNo == orderNo).ToList())
                        {
                            foreach (var pay in db.Payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).ToList())
                                db.Payments.Remove(pay);
                            db.Invoices.Remove(inv);
                        }
                    }

                    db.SalesOrders.Remove(entity);
                    db.SaveChanges();
                }

                // Đồng bộ dữ liệu sau khi xóa
                if (SelectedOrderForInvoice?.Id == order.Id)
                    SelectedOrderForInvoice = null;

                // Nếu đang xóa đơn hàng đang được chỉnh sửa thì reset form
                if (isDeletingCurrentOrder)
                {
                    ResetForm();
                }

                LoadData();
                MessageBox.Show("Đã xóa đơn hàng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa: {ex.InnerException?.Message ?? ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveOrder()
        {
            if (string.IsNullOrWhiteSpace(CurrentOrder.CustomerName))
            {
                MessageBox.Show("Vui lòng nhập Tên khách hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (OrdQty <= 0)
            {
                MessageBox.Show("Vui lòng nhập Số lượng lớn hơn 0!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra trạng thái khách hàng trước khi tạo đơn hàng
            using (var dbCheck = new AppDbContext())
            {
                var customerService = new CustomerService(dbCheck);
                
                // Tìm customer theo tên
                var customer = dbCheck.Customers
                    .AsEnumerable()
                    .FirstOrDefault(c => c.Name == CurrentOrder.CustomerName.Trim() && c.NgayXoa == null);
                
                if (customer != null)
                {
                    // Kiểm tra khách nợ xấu
                    try
                    {
                        customerService.ValidateKhachHangCoTheThanhToan(customer.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Không thể tạo đơn hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    // Khách không có trong danh sách Customers (khách lẻ)
                    // Kiểm tra xem có khách nào trùng tên nhưng ở trạng thái nợ xấu không
                    var badCustomer = dbCheck.Customers
                        .AsEnumerable()
                        .FirstOrDefault(c => c.Name == CurrentOrder.CustomerName.Trim() && c.TrangThai == "Nợ xấu");
                    
                    if (badCustomer != null)
                    {
                        MessageBox.Show("Khách hàng đang nợ xấu, không thể tạo đơn hàng.", "Không thể tạo đơn hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            string customerTrim = CurrentOrder.CustomerName.Trim();
            string productTrim = (CurrentOrder.ProductName ?? "").Trim();
            if (string.IsNullOrEmpty(productTrim)) productTrim = "Chưa nhập";

            using (var db = new AppDbContext())
            {
                bool customerExists = db.SalesOrders.Any(o => o.CustomerName.Trim() == customerTrim);

                bool productExists = db.SalesOrders.Any(o => (o.ProductName ?? "").Trim() == productTrim);

                if (customerExists && productExists)
                {
                    MessageBox.Show($"Trùng lặp! Tên khách hàng \"{customerTrim}\" và Tên sản phẩm \"{productTrim}\" đã tồn tại trong đơn hàng khác.\n\nVui lòng kiểm tra lại trước khi lưu.",
                        "Cảnh báo trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (customerExists)
                {
                    MessageBox.Show($"Trùng lặp! Tên khách hàng \"{customerTrim}\" đã tồn tại trong đơn hàng khác.\n\nVui lòng kiểm tra lại trước khi lưu.",
                        "Cảnh báo trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (productExists)
                {
                    MessageBox.Show($"Trùng lặp! Tên sản phẩm \"{productTrim}\" đã tồn tại trong đơn hàng khác.\n\nVui lòng kiểm tra lại trước khi lưu.",
                        "Cảnh báo trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            CurrentOrder.Quantity = OrdQty;
            CurrentOrder.UnitPrice = OrdPrice;
            CurrentOrder.TotalAmount = OrdTotal;

            CurrentOrder.DeliveryTime = new TimeSpan(SelectedHour, SelectedMinute, 0);

            if (AppSession.CurrentUser != null)
            {
                CurrentOrder.UserId = AppSession.CurrentUser.Id;
            }

            CurrentOrder.Address = string.IsNullOrWhiteSpace(CurrentOrder.Address) ? "Chưa nhập địa chỉ" : CurrentOrder.Address;
            CurrentOrder.Phone = CurrentOrder.Phone ?? "";
            CurrentOrder.CustomerCode = CurrentOrder.CustomerCode ?? "";
            CurrentOrder.Dimensions = string.IsNullOrWhiteSpace(CurrentOrder.Dimensions) ? "N/A" : CurrentOrder.Dimensions.Trim();
            CurrentOrder.Material = (DataBridge.SelectedQuotation?.PaperType)
                        ?? CurrentOrder.Material
                        ?? "Chưa chọn";
            // Cột SQL NOT NULL — ô trống trên form là null
            CurrentOrder.ProductName = string.IsNullOrWhiteSpace(CurrentOrder.ProductName) ? "Chưa nhập" : CurrentOrder.ProductName.Trim();
            CurrentOrder.PaymentTerm = string.IsNullOrWhiteSpace(CurrentOrder.PaymentTerm) ? "Chưa xác định" : CurrentOrder.PaymentTerm.Trim();
            CurrentOrder.TaxCode = CurrentOrder.TaxCode ?? "";
            if (string.IsNullOrWhiteSpace(CurrentOrder.OrderNo))
                CurrentOrder.OrderNo = "DH-" + DateTime.Now.ToString("yyMMddHHmm");

            if (PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation != null)
            {
                CurrentOrder.QuotationId = PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation.Id;

                System.Diagnostics.Debug.WriteLine($"Đã gắn QuotationId: {CurrentOrder.QuotationId} vào đơn hàng.");
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Tạo entity mới chỉ với scalar properties, tránh attach navigation properties
                    var order = new SalesOrder
                    {
                        UserId = AppSession.CurrentUser?.Id ?? 1,
                        OrderNo = CurrentOrder.OrderNo,
                        OrderDate = CurrentOrder.OrderDate,
                        Deadline = CurrentOrder.Deadline,
                        DeliveryTime = CurrentOrder.DeliveryTime,
                        CustomerCode = CurrentOrder.CustomerCode ?? "",
                        CustomerName = CurrentOrder.CustomerName ?? "",
                        Phone = CurrentOrder.Phone ?? "",
                        Address = string.IsNullOrWhiteSpace(CurrentOrder.Address) ? "Chưa nhập địa chỉ" : CurrentOrder.Address,
                        TaxCode = CurrentOrder.TaxCode ?? "",
                        ProductName = CurrentOrder.ProductName ?? "",
                        Quantity = OrdQty,
                        UnitPrice = OrdPrice,
                        TotalAmount = OrdTotal,
                        PaymentTerm = CurrentOrder.PaymentTerm,
                        CreditDays = CurrentOrder.CreditDays,
                        Status = "Chưa giao",
                        Dimensions = CurrentOrder.Dimensions ?? "N/A",
                        Material = CurrentOrder.Material ?? "Chưa chọn",
                        QuotationId = PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation?.Id,
                        TK_No = string.IsNullOrWhiteSpace(SelectedDebitAccount) ? null : ExtractAccountCode(SelectedDebitAccount),
                        TK_Co = string.IsNullOrWhiteSpace(SelectedCreditAccount) ? null : ExtractAccountCode(SelectedCreditAccount)
                    };
                    db.SalesOrders.Add(order);
                    db.SaveChanges();

                    // Tạo lệnh sản xuất
                    var prodOrder = new ProductionOrder
                    {
                        SalesOrderId = order.Id,
                        OrderNo = "LSX-" + order.OrderNo,
                        OrderDate = DateTime.Now,
                        Deadline = order.Deadline,
                        CustomerName = order.CustomerName,
                        ProductName = order.ProductName,
                        Quantity = (int)order.Quantity,
                        Dimensions = order.Dimensions,
                        Material = order.Material,
                        Status = "Chờ SX",
                        UserId = order.UserId,
                        Notes = $"Tạo tự động từ đơn hàng {order.OrderNo}"
                    };
                    db.ProductionOrders.Add(prodOrder);

                    // Tạo hóa đơn tự động với đầy đủ thông tin NOT NULL
                    var tkNo = string.IsNullOrWhiteSpace(SelectedDebitAccount) ? "131" : ExtractAccountCode(SelectedDebitAccount);
                    var tkCo = string.IsNullOrWhiteSpace(SelectedCreditAccount) ? "511" : ExtractAccountCode(SelectedCreditAccount);
                    var invoice = new Invoice
                    {
                        InvoiceNo = "HD-" + DateTime.Now.ToString("HHmmss"),
                        InvoiceDate = DateTime.Now,
                        SalesOrderId = order.Id,
                        CustomerId = order.Id,
                        CustomerName = order.CustomerName ?? "",
                        Phone = order.Phone ?? "",
                        Address = string.IsNullOrWhiteSpace(order.Address) ? "Chưa nhập địa chỉ" : order.Address,
                        TaxCode = order.TaxCode ?? "",
                        RefOrderNo = order.OrderNo ?? "",
                        TotalAmount = order.TotalAmount,
                        SubTotal = order.TotalAmount,
                        VATPercent = defaultVAT,
                        VATAmount = order.TotalAmount * defaultVAT / 100.0,
                        GrandTotal = order.TotalAmount * (1 + defaultVAT / 100.0),
                        Status = "Chưa thu",
                        Notes = $"Tạo tự động từ đơn hàng {order.OrderNo}",
                        TemplateNo = "1/001",
                        Symbol = "KT/26E",
                        UserId = order.UserId,
                        DueDate = DateTime.Now.AddDays(30),
                        TK_No = tkNo,
                        TK_Co = tkCo,
                        AccountType = "Công nợ (131)"
                    };
                    db.Invoices.Add(invoice);

                    db.SaveChanges();
                }
                PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation = null;
                PhanMemInAnERP.Helpers.DataBridge.RaiseDataChanged();  // Thông báo cho các màn hình khác

                MessageBox.Show("Đã lưu Đơn hàng. Lệnh SX và Hóa đơn đã được đồng bộ!", "Thành công");

                ResetForm();
                LoadData();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Lỗi khi lưu: {detail}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewOrder(SalesOrder order)
        {
            if (order == null)
            {
                MessageBox.Show("Không có đơn hàng để xem trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Lấy thông tin khách hàng
                string customerName = order.CustomerName ?? "";
                string customerPhone = order.Phone ?? "";
                string customerAddress = order.Address ?? "";
                string staffName = AppSession.CurrentUser?.Username ?? "N/A";

                // Gọi helper để hiển thị preview
                PhanMemInAnERP.Helpers.SalesOrderPrintHelper.ShowPreviewThenPrint(order, customerName, customerPhone, customerAddress, staffName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở xem trước: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFromQuotation(Quotation quote)
        {
            if (quote == null) return;

            int deliveryDays = quote.DeliveryDays > 0 ? quote.DeliveryDays : 7;
            CurrentOrder = new SalesOrder
            {
                OrderNo = "DH-" + DateTime.Now.ToString("yyMMddHHmm"),
                OrderDate = DateTime.Now,
                Deadline = DateTime.Now.Date.AddDays(deliveryDays),
                CustomerName = quote.CustomerName,
                ProductName = quote.ProductName,
                Dimensions = quote.ProductDimensions,
                Material = quote.PaperType,
                QuotationId = quote.Id,
                UserId = AppSession.CurrentUser?.Id ?? 1
            };

            OrdQty = quote.Quantity;
            OrdPrice = quote.QuotedUnitPrice;

            OnPropertyChanged(nameof(CurrentOrder));
            OnPropertyChanged(nameof(OrdQty));
            OnPropertyChanged(nameof(OrdPrice));
            OnPropertyChanged(nameof(OrdTotal));
            SyncOrderCustomerComboSelection();
        }

        private void ResetForm()
        {
            _selectedOrderCustomer = null;
            OnPropertyChanged(nameof(SelectedOrderCustomer));
            CurrentOrder = new SalesOrder { OrderNo = "DH-" + DateTime.Now.ToString("yyMMddHHmm") };
            OrdQty = 0;
            OrdPrice = 0;
            SelectedHour = 8;
            SelectedMinute = 0;
            DeliveryTimeText = "";
            // Reset tài khoản về mặc định
            SelectedDebitAccount = DebitAccountOptions.FirstOrDefault() ?? "";
            SelectedCreditAccount = CreditAccountOptions.FirstOrDefault() ?? "";
            OnPropertyChanged(nameof(CurrentOrder));
            OnPropertyChanged(nameof(OrdQty));
            OnPropertyChanged(nameof(OrdPrice));
            OnPropertyChanged(nameof(OrdTotal));
            OnPropertyChanged(nameof(DeliveryTimeText));
            OnPropertyChanged(nameof(SelectedDebitAccount));
            OnPropertyChanged(nameof(SelectedCreditAccount));

            // Xóa báo giá đã chọn trong DataBridge
            PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation = null;
        }

        private void SaveInvoice()
        {
            if (string.IsNullOrWhiteSpace(CurrentInvoice.InvoiceNo) || string.IsNullOrWhiteSpace(CurrentInvoice.CustomerName))
            {
                MessageBox.Show("Vui lòng nhập Số hóa đơn và Tên khách hàng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tính toán tổng tiền
            CurrentInvoice.SubTotal = InvSubTotal;
            CurrentInvoice.VATPercent = InvVAT;
            CurrentInvoice.VATAmount = InvSubTotal * InvVAT / 100.0;
            CurrentInvoice.GrandTotal = InvTotal;
            CurrentInvoice.TotalAmount = InvTotal;
            CurrentInvoice.DueDate = CurrentInvoice.InvoiceDate.AddDays(30);

            // Lưu thông tin tài khoản từ đơn hàng được chọn (nếu có)
            if (SelectedOrderForInvoice != null)
            {
                CurrentInvoice.SalesOrderId = SelectedOrderForInvoice.Id;
                // Copy TK từ đơn hàng nếu chưa có
                if (string.IsNullOrEmpty(CurrentInvoice.TK_No))
                    CurrentInvoice.TK_No = SelectedOrderForInvoice.TK_No ?? "131";
                if (string.IsNullOrEmpty(CurrentInvoice.TK_Co))
                    CurrentInvoice.TK_Co = SelectedOrderForInvoice.TK_Co ?? "511";
                if (string.IsNullOrEmpty(CurrentInvoice.AccountType))
                    CurrentInvoice.AccountType = "Công nợ (131)";
            }

            // Điền các field bắt buộc NOT NULL với giá trị mặc định
            if (string.IsNullOrEmpty(CurrentInvoice.RefOrderNo))
                CurrentInvoice.RefOrderNo = "";

            if (string.IsNullOrEmpty(CurrentInvoice.TaxCode))
                CurrentInvoice.TaxCode = "";

            if (string.IsNullOrEmpty(CurrentInvoice.TemplateNo))
                CurrentInvoice.TemplateNo = "1/001";

            if (string.IsNullOrEmpty(CurrentInvoice.Symbol))
                CurrentInvoice.Symbol = "KT/26E";

            if (string.IsNullOrEmpty(CurrentInvoice.Address))
                CurrentInvoice.Address = "Chưa nhập địa chỉ";

            if (string.IsNullOrEmpty(CurrentInvoice.Phone))
                CurrentInvoice.Phone = "";

            if (string.IsNullOrEmpty(CurrentInvoice.Notes))
                CurrentInvoice.Notes = "";

            if (string.IsNullOrEmpty(CurrentInvoice.Status))
                CurrentInvoice.Status = "Chưa thu";

            if (CurrentInvoice.UserId == 0)
                CurrentInvoice.UserId = AppSession.CurrentUser?.Id ?? 1;

            using (var db = new AppDbContext())
            {
                if (db.Invoices.Any(i => i.InvoiceNo == CurrentInvoice.InvoiceNo))
                {
                    MessageBox.Show($"Hóa đơn số \"{CurrentInvoice.InvoiceNo}\" đã tồn tại!", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                db.Invoices.Add(CurrentInvoice);
                db.SaveChanges();
            }
            MessageBox.Show("Lưu Hóa Đơn thành công!");
            CurrentInvoice = new Invoice { InvoiceNo = "HD-" + DateTime.Now.ToString("HHmmss") };
            InvSubTotal = 0;
            SelectedOrderForInvoice = null;
            LoadData();
        }

        private void DeleteInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            if (MessageBox.Show($"Bạn có chắc muốn xóa hóa đơn \"{invoice.InvoiceNo}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var entity = db.Invoices.Find(invoice.Id);
                    if (entity != null)
                    {
                        db.Invoices.Remove(entity);
                        db.SaveChanges();
                    }
                }

                if (SelectedInvoice?.Id == invoice.Id)
                    SelectedInvoice = null;

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa: {ex.InnerException?.Message ?? ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mở dialog để chọn loại tài khoản cho hoá đơn
        /// </summary>
        private void EditInvoiceAccount(Invoice invoice)
        {
            if (invoice == null) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var dbInvoice = db.Invoices.Find(invoice.Id);
                    if (dbInvoice != null)
                    {
                        // Tạo cửa sổ chọn tài khoản
                        var dialog = new Views.InvoiceAccountDialog
                        {
                            Owner = Application.Current.MainWindow,
                            DataContext = new InvoiceAccountDialogViewModel(dbInvoice)
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            // Đã lưu trong dialog
                            LoadData();
                            MessageBox.Show($"Đã cập nhật tài khoản cho hoá đơn {dbInvoice.InvoiceNo}", "Thành công");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void DeletePayment(Payment payment)
        {
            if (payment == null) return;

            string label = string.IsNullOrWhiteSpace(payment.ReceiptNo) ? $"#{payment.Id}" : payment.ReceiptNo;
            if (MessageBox.Show(
                    $"Xóa phiếu thu \"{label}\" ({payment.Amount:N0} đ — {payment.CustomerName})?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var entity = db.Payments.Find(payment.Id);
                    if (entity != null)
                    {
                        db.Payments.Remove(entity);
                        db.SaveChanges();
                    }
                }

                LoadData();
                UpdateCustomerDebtSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa phiếu thu: {ex.InnerException?.Message ?? ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDebtReport(AppDbContext db)
        {
            var allInvoices = db.Invoices.ToList();
            var allPayments = db.Payments.ToList();
            var reportList = new ObservableCollection<DebtReportItem>();

            double tempTotalDebt = 0;
            double tempOverdueDebt = 0;

            foreach (var inv in allInvoices)
            {
                double paid = allPayments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).Sum(p => p.Amount);
                double remain = Math.Max(0, inv.TotalAmount - paid);

                if (remain > 1)
                {
                    bool isOverdue = DateTime.Now > inv.DueDate;

                    reportList.Add(new DebtReportItem
                    {
                        CustomerName = inv.CustomerName,
                        InvoiceNo = inv.InvoiceNo,
                        InvoiceDate = inv.InvoiceDate,
                        DueDate = inv.DueDate,
                        TotalAmount = inv.TotalAmount,
                        PaidAmount = paid,
                        RemainingDebt = remain,
                        Status = isOverdue ? "Quá hạn" : "Trong hạn"
                    });

                    tempTotalDebt += remain;
                    if (isOverdue) tempOverdueDebt += remain;
                }
            }

            _debtReportSource.Clear();
            _debtReportSource.AddRange(reportList);
            RebuildDebtSearchHints();
            ApplyDebtReportFilter();

            TotalDebt = tempTotalDebt;
            OverdueDebt = tempOverdueDebt;
            DebtCustomerCount = reportList.Select(x => x.CustomerName).Distinct().Count();

            // Tính tổng hợp cho tất cả khách hàng (tab Thu tiền)
            TotalInvoicedAll = allInvoices.Sum(i => i.TotalAmount);
            TotalCollectedAll = allPayments.Sum(p => p.Amount);
            TotalRemainingAll = TotalDebt;
        }

        private void RebuildDebtSearchHints()
        {
            DebtReportSearchHints.Clear();
            DebtReportSearchHints.Add("(Tất cả)");
            foreach (var name in _debtReportSource
                         .Select(x => (x.CustomerName ?? "").Trim())
                         .Where(n => n.Length > 0)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                DebtReportSearchHints.Add(name);
            OnPropertyChanged(nameof(DebtReportSearchHints));
        }

        private void ClearPaymentDateFilter()
        {
            _paymentFilterFromDate = null;
            _paymentFilterToDate = null;
            OnPropertyChanged(nameof(PaymentFilterFromDate));
            OnPropertyChanged(nameof(PaymentFilterToDate));
            ApplyPaymentDateFilter();
        }

        private void ApplyPaymentDateFilter()
        {
            var filtered = _paymentsSource.AsEnumerable();

            if (_paymentFilterFromDate.HasValue)
            {
                var fromDate = _paymentFilterFromDate.Value.Date;
                filtered = filtered.Where(p => p.PaymentDate.Date >= fromDate);
            }

            if (_paymentFilterToDate.HasValue)
            {
                filtered = filtered.Where(p => p.PaymentDate.Date <= _paymentFilterToDate.Value.Date);
            }

            FilteredPaymentsList = new ObservableCollection<Payment>(filtered.ToList());
            OnPropertyChanged(nameof(FilteredPaymentsList));
        }

        private void ApplyDebtReportFilter()
        {
            IEnumerable<DebtReportItem> q = _debtReportSource;
            var s = (DebtReportSearchText ?? "").Trim();
            if (s.Length > 0)
            {
                var low = s.ToLowerInvariant();
                q = q.Where(x =>
                    (!string.IsNullOrEmpty(x.CustomerName) && x.CustomerName.ToLowerInvariant().Contains(low)) ||
                    (!string.IsNullOrEmpty(x.InvoiceNo) && x.InvoiceNo.ToLowerInvariant().Contains(low)));
            }

            DebtReports = new ObservableCollection<DebtReportItem>(q.ToList());
            OnPropertyChanged(nameof(DebtReports));
        }

        // ---------- TỔNG HỢP CÔNG NỢ PHẢI THU (TK 131) ----------
        private void LoadDebtSummary()
        {
            var end = DebtSummaryToDate.Date.AddDays(1).AddTicks(-1);
            using var db = new AppDbContext();

            var invoices = db.Invoices
                .Where(i => i.InvoiceDate >= DebtSummaryFromDate.Date && i.InvoiceDate <= end)
                .ToList();
            var payments = db.Payments.ToList();

            var custNames = invoices.Select(i => i.CustomerName).Distinct().ToList();
            var rows = new List<DebtSummaryRow>();

            foreach (var cn in custNames)
            {
                var cInv = invoices.Where(i => i.CustomerName == cn).ToList();
                var openingInvoices = db.Invoices
                    .Where(i => i.CustomerName == cn && i.InvoiceDate < DebtSummaryFromDate.Date)
                    .ToList();

                double paidBefore = payments
                    .Where(p => cInv.Any(ci => ci.InvoiceNo == p.RefInvoiceNo))
                    .Sum(p => p.Amount);
                double paidPeriod = payments
                    .Where(p => p.RefInvoiceNo != null &&
                                cInv.Any(ci => ci.InvoiceNo == p.RefInvoiceNo) &&
                                p.PaymentDate >= DebtSummaryFromDate.Date && p.PaymentDate <= end)
                    .Sum(p => p.Amount);

                double openingDebt = openingInvoices.Sum(i => i.TotalAmount) -
                    payments.Where(p => openingInvoices.Any(oi => oi.InvoiceNo == p.RefInvoiceNo)).Sum(p => p.Amount);
                double periodDebt = cInv.Sum(i => i.TotalAmount);
                double closingDebt = openingDebt + periodDebt - paidPeriod;

                rows.Add(new DebtSummaryRow
                {
                    CustomerName = cn,
                    OpeningDebt = openingDebt,
                    PeriodDebt = periodDebt,
                    PeriodCredit = paidPeriod,
                    ClosingDebt = closingDebt,
                    InvoiceCount = cInv.Count
                });
            }

            DebtSummaryRows = new ObservableCollection<DebtSummaryRow>(rows.OrderBy(r => r.CustomerName));
            OnPropertyChanged(nameof(DebtSummaryRows));
        }

        private void ExportDebtSummaryExcel()
        {
            if (!DebtSummaryRows.Any())
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"CongNo_TK131_{DebtSummaryFromDate:ddMMyyyy}_{DebtSummaryToDate:ddMMyyyy}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var pkg = new OfficeOpenXml.ExcelPackage();
                var ws = pkg.Workbook.Worksheets.Add("Công Nợ TK 131");
                ws.Cells["A1"].Value = $"TỔNG HỢP CÔNG NỢ PHẢI THU — Tài Khoản 131 — {DebtSummaryFromDate:dd/MM/yyyy} → {DebtSummaryToDate:dd/MM/yyyy}";
                ws.Cells["A1:F1"].Merge = true;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 13;

                ws.Cells["A3"].Value = "Tên khách hàng";
                ws.Cells["B3"].Value = "Nợ đầu kỳ";
                ws.Cells["C3"].Value = "Phát sinh Nợ";
                ws.Cells["D3"].Value = "Phát sinh Có";
                ws.Cells["E3"].Value = "Nợ cuối kỳ";
                ws.Cells["F3"].Value = "Số HĐ";
                ws.Cells["A3:F3"].Style.Font.Bold = true;

                int r = 4;
                foreach (var row in DebtSummaryRows)
                {
                    ws.Cells[r, 1].Value = row.CustomerName;
                    ws.Cells[r, 2].Value = row.OpeningDebt;
                    ws.Cells[r, 3].Value = row.PeriodDebt;
                    ws.Cells[r, 4].Value = row.PeriodCredit;
                    ws.Cells[r, 5].Value = row.ClosingDebt;
                    ws.Cells[r, 6].Value = row.InvoiceCount;
                    r++;
                }
                ws.Columns.AutoFit();
                pkg.SaveAs(new System.IO.FileInfo(dlg.FileName));
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                MessageBox.Show("Đã xuất báo cáo công nợ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- SỔ CHI TIẾT BÁN HÀNG ----------
        private void LoadSalesLedger()
        {
            var end = SalesLedgerTo.Date.AddDays(1).AddTicks(-1);
            using var db = new AppDbContext();

            var invoices = db.Invoices
                .Where(i => i.InvoiceDate >= SalesLedgerFrom.Date && i.InvoiceDate <= end)
                .ToList();
            var payments = db.Payments.ToList();

            var rows = new List<SalesLedgerRow>();
            foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
            {
                var paid = payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo).Sum(p => p.Amount);
                rows.Add(new SalesLedgerRow
                {
                    InvoiceDate = inv.InvoiceDate,
                    InvoiceNo = inv.InvoiceNo,
                    CustomerName = inv.CustomerName,
                    SubTotal = inv.SubTotal,
                    VatAmount = inv.SubTotal * (inv.VATPercent / 100.0),
                    TotalInvoice = inv.TotalAmount,
                    PaidAmount = paid,
                    Remaining = inv.TotalAmount - paid
                });
            }

            SalesLedgerRows = new ObservableCollection<SalesLedgerRow>(rows);
            OnPropertyChanged(nameof(SalesLedgerRows));
        }

        private void ExportSalesLedgerExcel()
        {
            if (!SalesLedgerRows.Any())
            {
                MessageBox.Show("Không có dữ liệu sổ chi tiết.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"SoChiTietBanHang_{SalesLedgerFrom:ddMMyyyy}_{SalesLedgerTo:ddMMyyyy}.xlsx" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var pkg = new OfficeOpenXml.ExcelPackage();
                var ws = pkg.Workbook.Worksheets.Add("Sổ Chi Tiết BH");
                ws.Cells["A1"].Value = $"SỔ CHI TIẾT BÁN HÀNG — {SalesLedgerFrom:dd/MM/yyyy} → {SalesLedgerTo:dd/MM/yyyy}";
                ws.Cells["A1:H1"].Merge = true;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 13;
                var hdr = new[] { "Ngày HĐ", "Số HĐ", "Khách Hàng", "Tiền Hàng", "Thuế GTGT", "Tổng HĐ", "Đã Thu", "Còn Nợ" };
                for (int c = 0; c < hdr.Length; c++) ws.Cells[3, c + 1].Value = hdr[c];
                ws.Cells["A3:H3"].Style.Font.Bold = true;

                int r = 4;
                foreach (var row in SalesLedgerRows)
                {
                    ws.Cells[r, 1].Value = row.InvoiceDate.ToString("dd/MM/yyyy");
                    ws.Cells[r, 2].Value = row.InvoiceNo;
                    ws.Cells[r, 3].Value = row.CustomerName;
                    ws.Cells[r, 4].Value = row.SubTotal;
                    ws.Cells[r, 5].Value = row.VatAmount;
                    ws.Cells[r, 6].Value = row.TotalInvoice;
                    ws.Cells[r, 7].Value = row.PaidAmount;
                    ws.Cells[r, 8].Value = row.Remaining;
                    r++;
                }
                ws.Columns.AutoFit();
                pkg.SaveAs(new System.IO.FileInfo(dlg.FileName));
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAllSalesReportExcel()
        {
            if (!SalesLedgerRows.Any()) LoadSalesLedger();
            if (!DebtSummaryRows.Any()) LoadDebtSummary();

            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"BaoCaoBanHang_{DateTime.Now:ddMMyyyy_HHmm}.xlsx" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var pkg = new OfficeOpenXml.ExcelPackage();
                var ws1 = pkg.Workbook.Worksheets.Add("Sổ Chi Tiết BH");
                ws1.Cells["A1"].Value = "SỔ CHI TIẾT BÁN HÀNG";
                ws1.Cells["A1:H1"].Merge = true;
                ws1.Cells["A1"].Style.Font.Bold = true;
                var hdr1 = new[] { "Ngày HĐ", "Số HĐ", "Khách Hàng", "Tiền Hàng", "Thuế GTGT", "Tổng HĐ", "Đã Thu", "Còn Nợ" };
                for (int c = 0; c < hdr1.Length; c++) ws1.Cells[3, c + 1].Value = hdr1[c];
                ws1.Cells["A3:H3"].Style.Font.Bold = true;
                int r1 = 4;
                foreach (var row in SalesLedgerRows)
                {
                    ws1.Cells[r1, 1].Value = row.InvoiceDate.ToString("dd/MM/yyyy");
                    ws1.Cells[r1, 2].Value = row.InvoiceNo;
                    ws1.Cells[r1, 3].Value = row.CustomerName;
                    ws1.Cells[r1, 4].Value = row.SubTotal;
                    ws1.Cells[r1, 5].Value = row.VatAmount;
                    ws1.Cells[r1, 6].Value = row.TotalInvoice;
                    ws1.Cells[r1, 7].Value = row.PaidAmount;
                    ws1.Cells[r1, 8].Value = row.Remaining;
                    r1++;
                }

                var ws2 = pkg.Workbook.Worksheets.Add("Công Nợ TK131");
                ws2.Cells["A1"].Value = "TỔNG HỢP CÔNG NỢ PHẢI THU (TK 131)";
                ws2.Cells["A1:F1"].Merge = true;
                ws2.Cells["A1"].Style.Font.Bold = true;
                var hdr2 = new[] { "Tên KH", "Nợ đầu kỳ", "Phát sinh Nợ", "Phát sinh Có", "Nợ cuối kỳ", "Số HĐ" };
                for (int c = 0; c < hdr2.Length; c++) ws2.Cells[3, c + 1].Value = hdr2[c];
                ws2.Cells["A3:F3"].Style.Font.Bold = true;
                int r2 = 4;
                foreach (var row in DebtSummaryRows)
                {
                    ws2.Cells[r2, 1].Value = row.CustomerName;
                    ws2.Cells[r2, 2].Value = row.OpeningDebt;
                    ws2.Cells[r2, 3].Value = row.PeriodDebt;
                    ws2.Cells[r2, 4].Value = row.PeriodCredit;
                    ws2.Cells[r2, 5].Value = row.ClosingDebt;
                    ws2.Cells[r2, 6].Value = row.InvoiceCount;
                    r2++;
                }

                pkg.SaveAs(new System.IO.FileInfo(dlg.FileName));
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                MessageBox.Show("Đã xuất báo cáo bán hàng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class DebtSummaryRow
    {
        public string CustomerName { get; set; } = "";
        public double OpeningDebt { get; set; }
        public double PeriodDebt { get; set; }
        public double PeriodCredit { get; set; }
        public double ClosingDebt { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class SalesLedgerRow
    {
        public DateTime InvoiceDate { get; set; }
        public string InvoiceNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public double SubTotal { get; set; }
        public double VatAmount { get; set; }
        public double TotalInvoice { get; set; }
        public double PaidAmount { get; set; }
        public double Remaining { get; set; }
    }
}