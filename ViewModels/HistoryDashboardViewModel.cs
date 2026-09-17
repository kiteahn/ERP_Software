using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;

namespace PhanMemInAnERP.ViewModels
{
    public class HistoryDashboardViewModel : BaseViewModel
    {

        public Func<double, string> Formatter { get; set; } = value => value.ToString("N0") + " đ";

        private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

        /// <summary>Trục X biểu đồ Top KH: gọn hơn — ví dụ 2.000.000 → "2 triệu", 200.000.000 → "200 triệu".</summary>
        public Func<double, string> KpiAxisFormatter { get; } = value =>
        {
            double trieu = value / 1_000_000.0;
            if (double.IsNaN(trieu) || double.IsInfinity(trieu))
                return string.Empty;
            if (Math.Abs(trieu) < 1e-9)
                return "0";

            decimal d = (decimal)trieu;
            decimal r = Math.Round(d, 2, MidpointRounding.AwayFromZero);
            if (r == decimal.Truncate(r))
                return ((long)r).ToString("N0", ViCulture) + " triệu";
            return r.ToString("N1", ViCulture) + " triệu";
        };
        // 1. THỐNG KÊ CƠ BẢN
        private int _totalQuotes; public int TotalQuotes { get => _totalQuotes; set { _totalQuotes = value; OnPropertyChanged(); } }
        private int _totalProducts; public int TotalProducts { get => _totalProducts; set { _totalProducts = value; OnPropertyChanged(); } }
        private double _estimatedRevenue; public double EstimatedRevenue { get => _estimatedRevenue; set { _estimatedRevenue = value; OnPropertyChanged(); } }
        private double _averageOrderValue; public double AverageOrderValue { get => _averageOrderValue; set { _averageOrderValue = value; OnPropertyChanged(); } }

        // 2. DỮ LIỆU TÌM KIẾM
        private DateTime _fromDate = DateTime.Now.AddDays(-30);
        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged();
                LoadDashboardCharts();
                LoadHistory();
            }
        }

        private DateTime _toDate = DateTime.Now;
        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                _toDate = value;
                OnPropertyChanged();
                LoadDashboardCharts();
                LoadHistory();
            }
        }

        public ObservableCollection<Quotation> QuoteHistory { get; set; }

        public SeriesCollection BarSeries { get; set; }        // Biểu đồ Cột
        public string[] BarLabels { get; set; }

        public SeriesCollection LineSeries { get; set; }       // Biểu đồ Đường
        public string[] LineLabels { get; set; }

        public SeriesCollection AreaSeries { get; set; }       // Biểu đồ Area (Vùng)
        public string[] AreaLabels { get; set; }

        public SeriesCollection StackedSeries { get; set; }    // Biểu đồ Cột Xếp Chồng
        public string[] StackedLabels { get; set; }

        public SeriesCollection PieSeries { get; set; }        // Biểu đồ Tròn

        public SeriesCollection KpiSeries { get; set; }
        public string[] KpiLabels { get; set; }

        // ===== THỐNG KÊ THEO THÁNG =====
        public SeriesCollection MonthlyQuoteSeries { get; set; }
        public string[] MonthlyQuoteLabels { get; set; }
        public int[] MonthlyQuoteCounts { get; set; }

        // ===== THỐNG KÊ THEO KHÁCH HÀNG =====
        public ObservableCollection<QuotationCountByCustomer> CustomerQuoteCounts { get; set; } = new ObservableCollection<QuotationCountByCustomer>();

        // ===== TỔNG HỢP =====
        private int _totalQuotesAllTime; public int TotalQuotesAllTime { get => _totalQuotesAllTime; set { _totalQuotesAllTime = value; OnPropertyChanged(); } }
        private double _totalRevenueAllTime; public double TotalRevenueAllTime { get => _totalRevenueAllTime; set { _totalRevenueAllTime = value; OnPropertyChanged(); } }

        // Tìm kiếm theo tên khách hàng
        private string _searchCustomerName;
        public string SearchCustomerName { get => _searchCustomerName; set { _searchCustomerName = value; OnPropertyChanged(); FilterByCustomer(); } }

        // Lọc theo khách hàng
        public const string AllCustomersFilterLabel = "(Tất cả khách hàng)";

        public ObservableCollection<string> AvailableCustomerNames { get; set; } = new ObservableCollection<string>();

        private string _selectedCustomerFilter = AllCustomersFilterLabel;
        public string SelectedCustomerFilter
        {
            get => _selectedCustomerFilter;
            set
            {
                _selectedCustomerFilter = string.IsNullOrWhiteSpace(value) ? AllCustomersFilterLabel : value;
                OnPropertyChanged();
                LoadDashboardCharts();
                LoadHistory();
            }
        }

        public double GaugeValue { get; set; }                 // Biểu đồ Gauge (Kim đo mục tiêu)
        public double GaugeTarget { get; set; }

        // COMMANDS & EVENTS
        public ICommand FilterCommand { get; }
        public ICommand DeleteQuoteCommand { get; }
        public ICommand LoadQuoteCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public Action<Quotation> OnLoadQuoteRequested { get; set; }

        public HistoryDashboardViewModel()
        {
            QuoteHistory = new ObservableCollection<Quotation>();

            FilterCommand = new RelayCommand(() => { LoadHistory(); LoadDashboardCharts(); });
            DeleteQuoteCommand = new RelayCommand<Quotation>(DeleteQuote);
            LoadQuoteCommand = new RelayCommand<Quotation>(LoadQuoteToCalculator);
            CreateOrderCommand = new RelayCommand<Quotation>(CreateOrder);

            LoadCustomerNames();
            LoadHistory();
            LoadDashboardCharts();
        }

        private void LoadCustomerNames()
        {
            using (var db = new AppDbContext())
            {
                var names = db.Quotations
                    .Select(q => q.CustomerName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                AvailableCustomerNames.Clear();
                AvailableCustomerNames.Add(AllCustomersFilterLabel);
                foreach (var n in names)
                    AvailableCustomerNames.Add(n);
            }
        }

        private void FilterByCustomer()
        {
            if (string.IsNullOrWhiteSpace(SearchCustomerName))
            {
                LoadDashboardCharts();
                return;
            }

            using (var db = new AppDbContext())
            {
                var quotes = db.Quotations
                    .Where(q => q.CustomerName.Contains(SearchCustomerName))
                    .ToList();

                if (quotes.Count == 0)
                {
                    TotalQuotes = 0;
                    TotalProducts = 0;
                    EstimatedRevenue = 0;
                    AverageOrderValue = 0;
                    OnPropertyChanged(nameof(TotalQuotes));
                    OnPropertyChanged(nameof(TotalProducts));
                    OnPropertyChanged(nameof(EstimatedRevenue));
                    OnPropertyChanged(nameof(AverageOrderValue));
                    return;
                }

                TotalQuotes = quotes.Count;
                TotalProducts = quotes.Sum(q => q.Quantity);
                EstimatedRevenue = quotes.Sum(q => q.TotalOrderValue);
                AverageOrderValue = TotalQuotes > 0 ? (EstimatedRevenue / TotalQuotes) : 0;
                OnPropertyChanged(nameof(TotalQuotes));
                OnPropertyChanged(nameof(TotalProducts));
                OnPropertyChanged(nameof(EstimatedRevenue));
                OnPropertyChanged(nameof(AverageOrderValue));
            }
        }

        private void LoadDashboardCharts()
        {
            using (var db = new AppDbContext())
            {
                var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);

                // Lọc theo khoảng ngày + khách hàng
                IQueryable<Quotation> query = db.Quotations.Where(q => q.QuoteDate >= FromDate.Date && q.QuoteDate <= endOfDay);
                if (!string.IsNullOrWhiteSpace(SelectedCustomerFilter) && SelectedCustomerFilter != AllCustomersFilterLabel)
                {
                    query = query.Where(q => q.CustomerName == SelectedCustomerFilter);
                }
                var quotes = query.ToList();

                // Tổng hợp tất cả thời gian
                var allQuotes = db.Quotations.ToList();
                TotalQuotesAllTime = allQuotes.Count;
                TotalRevenueAllTime = allQuotes.Sum(q => q.TotalOrderValue);

                // Cập nhật KPI card — phải gọi TRƯỚC khi reset chart, nếu không UI sẽ hiển thị giá trị cũ
                TotalQuotes = quotes.Count;
                TotalProducts = quotes.Sum(q => q.Quantity);
                EstimatedRevenue = quotes.Sum(q => q.TotalOrderValue);
                AverageOrderValue = TotalQuotes > 0 ? (EstimatedRevenue / TotalQuotes) : 0;
                OnPropertyChanged(nameof(TotalQuotes));
                OnPropertyChanged(nameof(TotalProducts));
                OnPropertyChanged(nameof(EstimatedRevenue));
                OnPropertyChanged(nameof(AverageOrderValue));

                if (quotes.Count == 0)
                {
                    // Reset TẤT CẢ chart + gauge về trạng thái rỗng thay vì return sớm
                    GaugeValue = 0;
                    OnPropertyChanged(nameof(GaugeValue));
                    MonthlyQuoteSeries = new SeriesCollection(); OnPropertyChanged(nameof(MonthlyQuoteSeries));
                    MonthlyQuoteLabels = Array.Empty<string>(); OnPropertyChanged(nameof(MonthlyQuoteLabels));
                    BarSeries = new SeriesCollection(); OnPropertyChanged(nameof(BarSeries));
                    BarLabels = Array.Empty<string>(); OnPropertyChanged(nameof(BarLabels));
                    LineSeries = new SeriesCollection(); OnPropertyChanged(nameof(LineSeries));
                    LineLabels = Array.Empty<string>(); OnPropertyChanged(nameof(LineLabels));
                    AreaSeries = new SeriesCollection(); OnPropertyChanged(nameof(AreaSeries));
                    AreaLabels = Array.Empty<string>(); OnPropertyChanged(nameof(AreaLabels));
                    StackedSeries = new SeriesCollection(); OnPropertyChanged(nameof(StackedSeries));
                    StackedLabels = Array.Empty<string>(); OnPropertyChanged(nameof(StackedLabels));
                    PieSeries = new SeriesCollection(); OnPropertyChanged(nameof(PieSeries));
                    KpiSeries = new SeriesCollection(); OnPropertyChanged(nameof(KpiSeries));
                    KpiLabels = Array.Empty<string>(); OnPropertyChanged(nameof(KpiLabels));
                    return;
                }

                // ===== THỐNG KÊ THEO THÁNG =====
                var monthlyStats = quotes
                    .GroupBy(q => new { q.QuoteDate.Year, q.QuoteDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Month = $"Tháng {g.Key.Month}/{g.Key.Year}",
                        Count = g.Count(),
                        Revenue = g.Sum(q => q.TotalOrderValue)
                    })
                    .ToList();

                MonthlyQuoteLabels = monthlyStats.Select(m => m.Month).ToArray();
                MonthlyQuoteSeries = new SeriesCollection {
                    new ColumnSeries {
                        Title = "Số Báo Giá",
                        Values = new ChartValues<int>(monthlyStats.Select(m => m.Count)),
                        DataLabels = true,
                        Fill = System.Windows.Media.Brushes.SteelBlue
                    },
                    new LineSeries {
                        Title = "Doanh Thu",
                        Values = new ChartValues<double>(monthlyStats.Select(m => m.Revenue)),
                        StrokeThickness = 3,
                        Fill = System.Windows.Media.Brushes.Transparent
                    }
                };
                OnPropertyChanged(nameof(MonthlyQuoteSeries));
                OnPropertyChanged(nameof(MonthlyQuoteLabels));

                // ===== THỐNG KÊ THEO KHÁCH HÀNG =====
                var customerStats = quotes
                    .GroupBy(q => q.CustomerName)
                    .Select(g => new QuotationCountByCustomer
                    {
                        CustomerName = g.Key,
                        QuoteCount = g.Count(),
                        TotalValue = g.Sum(q => q.TotalOrderValue),
                        LastQuoteDate = g.Max(q => q.QuoteDate)
                    })
                    .OrderByDescending(c => c.QuoteCount)
                    .ToList();
                CustomerQuoteCounts.Clear();
                foreach (var cs in customerStats)
                {
                    CustomerQuoteCounts.Add(cs);
                }
                OnPropertyChanged(nameof(CustomerQuoteCounts));

                var last7Days = quotes.GroupBy(q => q.QuoteDate.Date)
                                      .OrderByDescending(g => g.Key).Take(7).Reverse().ToList();
                var labels = last7Days.Select(g => g.Key.ToString("dd/MM")).ToArray();

                // Biểu đồ Cột (Bar)
                BarLabels = labels;
                BarSeries = new SeriesCollection {
            new ColumnSeries { Title = "Doanh Thu", Values = new ChartValues<double>(last7Days.Select(g => g.Sum(q => q.TotalOrderValue))) }
        };

                // Biểu đồ Đường (Line)
                LineLabels = labels;
                LineSeries = new SeriesCollection {
            new LineSeries { Title = "Sản Lượng", Values = new ChartValues<double>(last7Days.Select(g => (double)g.Sum(q => q.Quantity))), Fill = System.Windows.Media.Brushes.Transparent }
        };

                // Biểu đồ Vùng (Area) - Giá vốn vs Lợi nhuận
                AreaLabels = labels;
                AreaSeries = new SeriesCollection {
            new LineSeries { Title = "Giá Vốn", Values = new ChartValues<double>(last7Days.Select(g => g.Sum(q => q.TotalProductionCost))) },
            new LineSeries { Title = "Lợi Nhuận", Values = new ChartValues<double>(last7Days.Select(g => g.Sum(q => q.TotalOrderValue - q.TotalProductionCost))) }
        };

                StackedLabels = labels;
                StackedSeries = new SeriesCollection {
            new StackedColumnSeries { Title = "Giá Vốn", Values = new ChartValues<double>(last7Days.Select(g => g.Sum(q => q.TotalProductionCost))) },
            new StackedColumnSeries { Title = "Lợi Nhuận", Values = new ChartValues<double>(last7Days.Select(g => g.Sum(q => q.TotalOrderValue - q.TotalProductionCost))) }
        };

                // Biểu đồ Tròn (Pie) - Số báo giá ưu tiên máy lớn vs máy nhỏ (theo IsLargeMachine)
                int mayLon = quotes.Count(q => q.IsLargeMachine);
                int mayNho = quotes.Count(q => !q.IsLargeMachine);
                PieSeries = new SeriesCollection {
            new PieSeries { Title = "Máy Lớn", Values = new ChartValues<int> { mayLon }, DataLabels = true },
            new PieSeries { Title = "Máy Nhỏ", Values = new ChartValues<int> { mayNho }, DataLabels = true }
        };

                // Top 5 theo doanh thu — đảo thứ tự để khách lớn nhất nằm phía trên (LiveCharts: index 0 ở dưới)
                var topCustomers = quotes.GroupBy(q => q.CustomerName)
                    .Select(g => new { Name = g.Key ?? "", Total = g.Sum(q => q.TotalOrderValue) })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToList();
                while (topCustomers.Count < 5)
                    topCustomers.Add(new { Name = "", Total = 0.0 });
                topCustomers.Reverse();

                const int kpiLabelMax = 42;
                KpiLabels = topCustomers.Select(c =>
                {
                    if (string.IsNullOrWhiteSpace(c.Name))
                        return "—";
                    var n = c.Name.Trim();
                    return n.Length <= kpiLabelMax ? n : n.Substring(0, kpiLabelMax - 1) + "…";
                }).ToArray();

                KpiSeries = new SeriesCollection {
                    new RowSeries
                    {
                        Title = "Doanh thu (VNĐ)",
                        Values = new ChartValues<double>(topCustomers.Select(c => c.Total)),
                        RowPadding = 10,
                        MaxRowHeigth = 34
                    }
                };

                // Biểu đồ Kim đo (Gauge) - Mục tiêu 1 tỷ/tháng
                GaugeTarget = 1000000000;
                GaugeValue = EstimatedRevenue;

                // Cập nhật UI
                OnPropertyChanged(nameof(BarSeries)); OnPropertyChanged(nameof(BarLabels));
                OnPropertyChanged(nameof(LineSeries)); OnPropertyChanged(nameof(LineLabels));
                OnPropertyChanged(nameof(AreaSeries)); OnPropertyChanged(nameof(AreaLabels));
                OnPropertyChanged(nameof(StackedSeries)); OnPropertyChanged(nameof(StackedLabels));
                OnPropertyChanged(nameof(PieSeries));
                OnPropertyChanged(nameof(KpiSeries)); OnPropertyChanged(nameof(KpiLabels));
                OnPropertyChanged(nameof(GaugeValue)); OnPropertyChanged(nameof(GaugeTarget));
            }
        }

        // ---------- PHẦN LỊCH SỬ ----------
        private void LoadHistory()
        {
            using (var db = new AppDbContext())
            {
                var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);
                var historyQuery = db.Quotations.Include(q => q.ExtraCosts)
                                .Where(q => q.QuoteDate >= FromDate.Date && q.QuoteDate <= endOfDay);
                if (!string.IsNullOrWhiteSpace(SelectedCustomerFilter) && SelectedCustomerFilter != AllCustomersFilterLabel)
                    historyQuery = historyQuery.Where(q => q.CustomerName == SelectedCustomerFilter);

                var history = historyQuery.OrderByDescending(q => q.QuoteDate).ToList();
                QuoteHistory = new ObservableCollection<Quotation>(history);
                OnPropertyChanged(nameof(QuoteHistory));
            }
        }

        private void DeleteQuote(Quotation quote)
        {
            if (quote == null) return;

            if (MessageBox.Show($"Xóa báo giá của {quote.CustomerName}?", "Cảnh báo", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    // Load the tracked quotation
                    var dbQuote = db.Quotations.Find(quote.Id);
                    if (dbQuote == null) return;

                    // 1. Xóa phiếu xuất kho liên quan (FK: QuotationId)
                    var exports = db.ExportTransactions.Where(x => x.QuotationId == dbQuote.Id).ToList();
                    if (exports.Any())
                    {
                        var result = MessageBox.Show(
                            $"Báo giá này có {exports.Count} phiếu xuất kho liên quan. Xóa báo giá sẽ xóa luôn các phiếu xuất kho này.\n\nTiếp tục xóa?",
                            "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result != MessageBoxResult.Yes) return;
                        db.ExportTransactions.RemoveRange(exports);
                    }

                    // 2. Xóa chi phí phát sinh
                    var extraCosts = db.QuoteExtraCosts.Where(e => e.QuotationId == dbQuote.Id).ToList();
                    if (extraCosts.Any())
                        db.QuoteExtraCosts.RemoveRange(extraCosts);

                    // 3. Xóa đơn hàng liên quan (nếu có)
                    var salesOrders = db.SalesOrders.Where(s => s.QuotationId == dbQuote.Id).ToList();
                    if (salesOrders.Any())
                    {
                        foreach (var so in salesOrders)
                        {
                            // Xóa hóa đơn + phiếu thu gắn với đơn hàng
                            var invs = db.Invoices.Where(i => i.RefOrderNo == so.OrderNo).ToList();
                            foreach (var inv in invs)
                            {
                                db.Payments.RemoveRange(db.Payments.Where(p => p.RefInvoiceNo == inv.InvoiceNo));
                                db.Invoices.Remove(inv);
                            }
                            // Xóa lệnh sản xuất
                            db.ProductionOrders.RemoveRange(db.ProductionOrders.Where(p => p.SalesOrderId == so.Id));
                            db.SalesOrders.Remove(so);
                        }
                    }

                    // 4. Xóa báo giá
                    db.Quotations.Remove(dbQuote);
                    db.SaveChanges();
                }
                LoadCustomerNames();
                LoadHistory(); LoadDashboardCharts();

                // Thông báo cho các ViewModel khác biết dữ liệu đã thay đổi
                PhanMemInAnERP.Helpers.DataBridge.RaiseDataChanged();

                MessageBox.Show("Đã xóa báo giá và các dữ liệu liên quan.", "Thành công");
            }
        }

        private void LoadQuoteToCalculator(Quotation quote)
        {
            PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation = quote;
            OnLoadQuoteRequested?.Invoke(quote);
            PhanMemInAnERP.Helpers.DataBridge.RaiseLoadQuote(quote);
            MessageBox.Show("Đang chuyển dữ liệu sang màn hình tính giá...");
        }

        private void CreateOrder(Quotation quote)
        {
            if (quote == null) return;
            PhanMemInAnERP.Helpers.DataBridge.SelectedQuotation = quote;
            PhanMemInAnERP.Helpers.DataBridge.RaiseCreateOrder(quote);
            MessageBox.Show("Đang chuyển dữ liệu và mở tab Bán Hàng...");
        }
    }
}