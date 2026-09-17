using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PhanMemInAnERP.ViewModels
{
    public class ProductionOrderViewModel : BaseViewModel
    {
        private ObservableCollection<ProductionOrder> _allOrders;

        private ObservableCollection<ProductionOrder> _filteredOrders;
        public ObservableCollection<ProductionOrder> FilteredOrders
        {
            get => _filteredOrders;
            set { _filteredOrders = value; OnPropertyChanged(); }
        }

        private ProductionOrder _currentOrder;
        public ProductionOrder CurrentOrder
        {
            get => _currentOrder;
            set { _currentOrder = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Quotation> _savedQuotations;
        public ObservableCollection<Quotation> SavedQuotations
        {
            get => _savedQuotations;
            set { _savedQuotations = value; OnPropertyChanged(); }
        }

        private Quotation _selectedQuotation;
        public Quotation SelectedQuotation
        {
            get => _selectedQuotation;
            set
            {
                _selectedQuotation = value;
                OnPropertyChanged();
                if (_selectedQuotation != null)
                {
                    ImportFromQuotation(_selectedQuotation);
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterOrders(); 
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand ClearFormCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ObservableCollection<Material> AvailableMaterials { get; set; }

        public ObservableCollection<Customer> OrderFormCustomers { get; } = new ObservableCollection<Customer>();

        private Customer? _selectedProductionCustomer;
        public Customer? SelectedProductionCustomer
        {
            get => _selectedProductionCustomer;
            set
            {
                if (ReferenceEquals(_selectedProductionCustomer, value)) return;
                _selectedProductionCustomer = value;
                OnPropertyChanged();
                if (value != null)
                    ApplyCustomerFromCrm(value);
            }
        }

        private const string CrmNotesLinePrefix = "[Thông tin KH] ";

        private void ApplyCustomerFromCrm(Customer c)
        {
            CurrentOrder.CustomerName = (c.Name ?? "").Trim();
            var bits = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(c.Phone)) bits.Add($"ĐT {c.Phone.Trim()}");
            if (!string.IsNullOrWhiteSpace(c.TaxCode)) bits.Add($"MST {c.TaxCode.Trim()}");
            if (!string.IsNullOrWhiteSpace(c.Address)) bits.Add(c.Address.Trim());
            if (bits.Count > 0)
            {
                string line = CrmNotesLinePrefix + string.Join(" | ", bits);
                string raw = (CurrentOrder.Notes ?? "").Replace("\r\n", "\n");
                var lines = raw.Split('\n').ToList();
                int idx = lines.FindIndex(l => l.TrimStart().StartsWith(CrmNotesLinePrefix, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) lines[idx] = line;
                else lines.Insert(0, line);
                CurrentOrder.Notes = string.Join("\n", lines).TrimEnd();
            }
            OnPropertyChanged(nameof(CurrentOrder));
        }

        private void SyncProductionCustomerCombo()
        {
            var name = (CurrentOrder.CustomerName ?? "").Trim();
            Customer? match = string.IsNullOrEmpty(name)
                ? null
                : OrderFormCustomers.FirstOrDefault(x =>
                    string.Equals((x.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (!ReferenceEquals(_selectedProductionCustomer, match))
            {
                _selectedProductionCustomer = match;
                OnPropertyChanged(nameof(SelectedProductionCustomer));
            }
        }

        private void LoadCustomers()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    OrderFormCustomers.Clear();
                    foreach (var c in db.Customers.OrderBy(x => x.Name))
                        OrderFormCustomers.Add(c);
                }
            }
            catch (Exception ex)
            {
                OrderFormCustomers.Clear();
                AddLog($"Lỗi tải khách hàng: {ex.Message}", true);
            }
        }

        public ICommand StartProductionCommand { get; }
        public ICommand CompleteProductionCommand { get; }

        public ProductionOrderViewModel()
        {
            CurrentOrder = new ProductionOrder { OrderNo = GenerateOrderNo() };

            SaveCommand = new RelayCommand(SaveOrder);
            ClearFormCommand = new RelayCommand(ClearForm);
            EditCommand = new RelayCommand<ProductionOrder>(EditOrder);
            DeleteCommand = new RelayCommand<ProductionOrder>(DeleteOrder);

            LoadOrdersFromDatabase();
            LoadMaterials();
            LoadCustomers();
            LoadQuotations();

            StartProductionCommand = new RelayCommand<ProductionOrder>(StartProduction, CanStartProduction);
            CompleteProductionCommand = new RelayCommand<ProductionOrder>(CompleteProduction, CanCompleteProduction);
        }

        private void LoadQuotations()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var list = db.Quotations.OrderByDescending(q => q.QuoteDate).ToList();
                    SavedQuotations = new ObservableCollection<Quotation>(list);
                }
            }
            catch (Exception ex)
            {
                SavedQuotations = new ObservableCollection<Quotation>();
                AddLog($"Lỗi tải báo giá: {ex.Message}", true);
            }
        }

        private const string QuoteTechNotesHeader = "Thông số kỹ thuật (từ báo giá)";

        /// <summary>Ghi chú kỹ thuật sinh từ báo giá — không ghi giá tiền.</summary>
        private static string BuildTechnicalNotesFromQuotation(Quotation quote)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                QuoteTechNotesHeader + ":",
                $"• In: {quote.ColorCount} màu — Tiền kẽm ({(quote.IsLargeMachine ? "máy lớn" : "máy nhỏ")}): {(quote.IsLargeMachine ? quote.PlatePricePerColorLargeMachine : quote.PlatePricePerColorSmallMachine):N0} đ/kẽm"
            };

            if (quote.LaminationPrice > 0)
            {
                string lamType = string.IsNullOrWhiteSpace(quote.LaminationType) ? "chưa ghi loại" : quote.LaminationType.Trim();
                string sides = quote.LaminationSides <= 0 ? "?" : quote.LaminationSides.ToString();
                lines.Add($"• Cán màng: {lamType}, {sides} mặt");
            }

            if (quote.DieCutMoldPrice > 0) lines.Add("• Có khuôn bế riêng");
            if (quote.StringPricePerItem > 0) lines.Add("• Có xỏ dây túi");
            if (quote.ButtonPricePerItem > 0) lines.Add("• Có đóng nút túi");
            if (quote.BoxPrice > 0) lines.Add("• Đóng thùng: 500 cái/thùng (theo quy ước báo giá)");
            lines.Add($"• Quy cách: {quote.SoCon} con/tờ, bù hao {quote.BuHao} tờ");

            return string.Join("\n", lines);
        }

        /// <summary>Bỏ khối ghi chú cũ do nhập từ báo giá để tránh chồng lặp khi chọn báo giá khác.</summary>
        private static string RemovePreviousQuoteTechnicalBlock(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return "";

            var lines = notes.Replace("\r\n", "\n").Split('\n');
            int blockStart = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith("---", StringComparison.Ordinal) && trimmed.Contains("BÁO GIÁ", StringComparison.OrdinalIgnoreCase))
                {
                    blockStart = i;
                    break;
                }

                if (trimmed.StartsWith(QuoteTechNotesHeader, StringComparison.OrdinalIgnoreCase))
                {
                    blockStart = i;
                    break;
                }
            }

            if (blockStart < 0)
                return notes.Trim();

            int blockEnd = blockStart;
            for (int i = blockStart; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    blockEnd = i;
                    continue;
                }

                if (trimmed.StartsWith(QuoteTechNotesHeader, StringComparison.OrdinalIgnoreCase))
                {
                    blockEnd = i;
                    continue;
                }

                if (trimmed.StartsWith("•", StringComparison.Ordinal) ||
                    trimmed.StartsWith("-", StringComparison.Ordinal) ||
                    (trimmed.StartsWith("---", StringComparison.Ordinal) && trimmed.Contains("BÁO GIÁ", StringComparison.OrdinalIgnoreCase)))
                {
                    blockEnd = i;
                    continue;
                }

                break;
            }

            var before = blockStart > 0 ? string.Join("\n", lines.Take(blockStart)).TrimEnd() : "";
            var afterStart = blockEnd + 1;
            var after = afterStart < lines.Length ? string.Join("\n", lines.Skip(afterStart)).Trim() : "";

            if (string.IsNullOrEmpty(before)) return after;
            if (string.IsNullOrEmpty(after)) return before;
            return before + "\n\n" + after;
        }

        private void ImportFromQuotation(Quotation quote)
        {
            if (quote == null) return;

            CurrentOrder.CustomerName = quote.CustomerName;
            CurrentOrder.ProductName = quote.ProductName;
            CurrentOrder.Quantity = quote.Quantity;
            CurrentOrder.Dimensions = quote.ProductDimensions;
            CurrentOrder.Material = $"{quote.PaperType} {quote.PaperGsm} gsm";

            // ===== TỰ ĐỘNG CHỌN CÁC CÔNG ĐOẠN TỪ BÁO GIÁ =====
            // Dựa trên các thông số đã nhập ở báo giá, tự động xác định công đoạn cần làm
            CurrentOrder.HasPrinting = true;             // Luôn có in
            CurrentOrder.HasPlateMaking = quote.ColorCount > 0;  // Có làm kẽm nếu có số màu

            // Cán màng: có giá và > 0
            CurrentOrder.HasLamination = quote.LaminationPrice > 0;

            // Bế: luôn có (khuôn bế)
            CurrentOrder.HasDieCutting = true;

            // Dán: có số lượng
            CurrentOrder.HasGluing = quote.Quantity > 0;

            // Xỏ dây: có giá dây
            CurrentOrder.HasStringing = quote.StringPricePerItem > 0;

            // Đóng nút: có giá nút
            CurrentOrder.HasButtoning = quote.ButtonPricePerItem > 0;

            // ===== COPY ĐẦY ĐỦ THÔNG SỐ IN TỪ BÁO GIÁ =====
            CurrentOrder.PrintLength = quote.PrintLength;
            CurrentOrder.PrintWidth = quote.PrintWidth;
            CurrentOrder.ColorCount = quote.ColorCount;
            CurrentOrder.IsLargeMachine = quote.IsLargeMachine;
            CurrentOrder.PlatePricePerColorLarge = quote.PlatePricePerColorLargeMachine;
            CurrentOrder.PlatePricePerColorSmall = quote.PlatePricePerColorSmallMachine;
            CurrentOrder.LaminationType = quote.LaminationType ?? "";
            CurrentOrder.LaminationSides = quote.LaminationSides;
            CurrentOrder.LaminationPrice = quote.LaminationPrice;
            CurrentOrder.PaperPricePerTon = quote.PaperPricePerTon;
            CurrentOrder.SoCon = quote.SoCon;
            CurrentOrder.BuHao = quote.BuHao;

            // Đóng gói/đóng thùng: luôn có
            CurrentOrder.HasPackaging = quote.BoxPrice > 0 || quote.Quantity > 0;

            string techNotes = BuildTechnicalNotesFromQuotation(quote);
            string keptManual = RemovePreviousQuoteTechnicalBlock(CurrentOrder.Notes);
            CurrentOrder.Notes = string.IsNullOrWhiteSpace(keptManual)
                ? techNotes
                : techNotes + "\n\n" + keptManual.Trim();
            
            // Thông báo cho UI cập nhật
            OnPropertyChanged(nameof(CurrentOrder));
            SyncProductionCustomerCombo();
        }

        private void LoadMaterials()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var list = db.Materials.ToList();
                    AvailableMaterials = new ObservableCollection<Material>(list);
                    OnPropertyChanged(nameof(AvailableMaterials));
                }
            }
            catch (Exception ex)
            {
                AvailableMaterials = new ObservableCollection<Material>();
                OnPropertyChanged(nameof(AvailableMaterials));
                AddLog($"Lỗi tải vật tư: {ex.Message}", true);
            }
        }

        private bool CanStartProduction(ProductionOrder order)
        {
            return order != null && order.Status == "Chờ SX";
        }

        private void StartProduction(ProductionOrder order)
        {
            if (order == null) return;

            using (var context = new AppDbContext())
            {
                var dbOrder = context.ProductionOrders.Find(order.Id);
                if (dbOrder == null) return;

                var material = context.Materials.FirstOrDefault(m => m.Name == dbOrder.Material);

                if (material == null)
                {
                    AddLog($"❌ LỖI: Không tìm thấy vật tư '{dbOrder.Material}' trong kho để trừ hàng!", true);
                    MessageBox.Show($"Không tìm thấy vật tư có tên '{dbOrder.Material}' trong danh mục vật tư!", "Lỗi trừ kho");
                    return;
                }

                double amountNeeded = dbOrder.Quantity;

                if (material.StockQuantity < amountNeeded)
                {
                    AddLog($"❌ THẤT BẠI: Lệnh {dbOrder.OrderNo} cần {amountNeeded} {material.Unit}, nhưng kho chỉ còn {material.StockQuantity}.", true);
                    MessageBox.Show("Số lượng vật tư trong kho không đủ để sản xuất!", "Thiếu hàng");
                    return;
                }

                try
                {
                    material.StockQuantity -= amountNeeded;

                    var autoExport = new ExportTransaction
                    {
                        TicketNo = "AUTO-" + DateTime.Now.ToString("yyMMddHHmm"),
                        ExportDate = DateTime.Now,
                        Purpose = "Sản xuất (Tự động)",
                        MaterialId = material.Id,
                        Quantity = amountNeeded,
                        ProductionOrderId = dbOrder.Id,
                        UserId = AppSession.CurrentUser?.Id ?? 1,
                        Notes = $"Xuất tự động cho lệnh {dbOrder.OrderNo}"
                    };
                    context.ExportTransactions.Add(autoExport);

                    dbOrder.Status = "Đang SX";

                    context.SaveChanges();

                    AddLog($"🚀 Lệnh {dbOrder.OrderNo} BẮT ĐẦU. Đã tự động xuất {amountNeeded} {material.Unit} {material.Name} từ kho.");
                    MessageBox.Show($"Lệnh {dbOrder.OrderNo} đã lên máy in thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    AddLog($"❌ LỖI HỆ THỐNG: {ex.Message}", true);
                }
            }

            LoadOrdersFromDatabase();
        }

        private bool CanCompleteProduction(ProductionOrder order)
        {
            return order != null && order.Status == "Đang SX";
        }

        private void CompleteProduction(ProductionOrder order)
        {
            if (order == null) return;

            using (var context = new AppDbContext())
            {
                var dbOrder = context.ProductionOrders.Find(order.Id);
                if (dbOrder == null) return;

                // Kiểm tra vật tư đầu vào
                var material = context.Materials.FirstOrDefault(m => m.Name == dbOrder.Material);
                if (material == null)
                {
                    AddLog($"❌ LỖI: Không tìm thấy vật tư '{dbOrder.Material}' trong danh mục vật tư!", true);
                    MessageBox.Show($"Không tìm thấy vật tư '{dbOrder.Material}'!", "Lỗi");
                    return;
                }

                // Tạo phiếu nhập kho sản xuất
                var importTicketNo = "NK-SX-" + DateTime.Now.ToString("yyMMddHHmm");
                var productionImport = new ProductionImport
                {
                    TicketNo = importTicketNo,
                    ImportDate = DateTime.Now,
                    ProductionOrderId = dbOrder.Id,
                    SalesOrderId = dbOrder.SalesOrderId,
                    ProductName = dbOrder.ProductName,
                    CustomerName = dbOrder.CustomerName,
                    Quantity = dbOrder.Quantity,
                    Unit = material.Unit,
                    UnitPrice = 0, // Sản phẩm hoàn thành có giá 0 (sẽ tính khi bán)
                    TotalPrice = 0,
                    UserId = AppSession.CurrentUser?.Id ?? 1,
                    Notes = $"Nhập kho sản phẩm hoàn thành từ lệnh {dbOrder.OrderNo}",
                    Status = "Chưa xuất",
                    ExportDate = null,
                    ExportTicketNo = ""
                };

                context.ProductionImports.Add(productionImport);

                // Cập nhật trạng thái lệnh sản xuất
                dbOrder.Status = "Hoàn thành";

                context.SaveChanges();

                AddLog($"✅ Lệnh {dbOrder.OrderNo} đã HOÀN THÀNH. Sản phẩm đã được đưa vào kho (phiếu: {importTicketNo}).");
                MessageBox.Show($"Lệnh {dbOrder.OrderNo} đã hoàn thành! Sản phẩm đã được nhập kho với số phiếu {importTicketNo}.", "Thành công");
            }

            LoadOrdersFromDatabase();
        }

        private void LoadOrdersFromDatabase()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var list = context.ProductionOrders
                                      .AsNoTracking()
                                      .OrderByDescending(o => o.OrderDate)
                                      .ToList();
                    _allOrders = new ObservableCollection<ProductionOrder>(list);
                    FilteredOrders = new ObservableCollection<ProductionOrder>(_allOrders);
                }
            }
            catch (Exception ex)
            {
                _allOrders = new ObservableCollection<ProductionOrder>();
                FilteredOrders = new ObservableCollection<ProductionOrder>(_allOrders);
                AddLog($"Lỗi tải dữ liệu: {ex.Message}", true);
            }
        }

        private void FilterOrders()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredOrders = new ObservableCollection<ProductionOrder>(_allOrders);
            }
            else
            {
                var lowerSearch = SearchText.ToLower();
                FilteredOrders = new ObservableCollection<ProductionOrder>(
                    _allOrders.Where(o =>
                        (o.OrderNo != null && o.OrderNo.ToLower().Contains(lowerSearch)) ||
                        (o.CustomerName != null && o.CustomerName.ToLower().Contains(lowerSearch)) ||
                        (o.ProductName != null && o.ProductName.ToLower().Contains(lowerSearch))
                    )
                );
            }
        }

        private void SaveOrder()
        {
            CurrentOrder.OrderNo = (CurrentOrder.OrderNo ?? "").Trim();
            CurrentOrder.CustomerName = (CurrentOrder.CustomerName ?? "").Trim();
            CurrentOrder.ProductName = (CurrentOrder.ProductName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(CurrentOrder.OrderNo)
                || string.IsNullOrWhiteSpace(CurrentOrder.CustomerName)
                || string.IsNullOrWhiteSpace(CurrentOrder.ProductName))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ: Số lệnh, Khách hàng và Sản phẩm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentOrder.Material))
            {
                MessageBox.Show("Vui lòng chọn Chất liệu!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại!");
                return;
            }

            using (var context = new AppDbContext())
            {
                CurrentOrder.UserId = AppSession.CurrentUser.Id;
                if (CurrentOrder.Id == 0)
                {
                    context.ProductionOrders.Add(CurrentOrder);
                }
                context.SaveChanges();
            }

            MessageBox.Show("Lưu lệnh sản xuất thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadOrdersFromDatabase();
            LoadCustomers();
            ClearForm();
        }

        private void EditOrder(ProductionOrder order)
        {
          
            CurrentOrder = new ProductionOrder
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderNo = order.OrderNo,
                OrderDate = order.OrderDate,
                Deadline = order.Deadline,
                CustomerName = order.CustomerName,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                Dimensions = order.Dimensions,
                Material = order.Material,
                Notes = order.Notes,
                Status = order.Status,
                // Các công đoạn sản xuất
                HasPrinting = order.HasPrinting,
                HasPlateMaking = order.HasPlateMaking,
                HasLamination = order.HasLamination,
                HasDieCutting = order.HasDieCutting,
                HasGluing = order.HasGluing,
                HasStringing = order.HasStringing,
                HasButtoning = order.HasButtoning,
                HasPackaging = order.HasPackaging
            };
            OnPropertyChanged(nameof(CurrentOrder));
            SyncProductionCustomerCombo();
        }

        private void DeleteOrder(ProductionOrder order)
{
    if (order == null) return;

    using (var context = new AppDbContext())
    {
        bool hasExport = context.ExportTransactions.Any(x => x.ProductionOrderId == order.Id);

        if (hasExport)
        {
            AddLog($"❌ LỖI: Không thể xóa lệnh {order.OrderNo} vì đã có phiếu xuất kho liên quan.");
            MessageBox.Show("Không thể xóa lệnh đã phát sinh dữ liệu xuất kho!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa lệnh {order.OrderNo} không?", 
            "Xác nhận xóa", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var dbOrder = context.ProductionOrders.Find(order.Id);
            if (dbOrder != null) 
            { 
                context.ProductionOrders.Remove(dbOrder); 
                context.SaveChanges(); 
            }
            
            AddLog($"🗑️ Đã xóa lệnh: {order.OrderNo}");
   
            LoadOrdersFromDatabase();
        }
    } 
}

        public ObservableCollection<string> OperationLogs { get; set; } = new ObservableCollection<string>();

        // Helper method cho log - dùng riêng trong class này
        private void Log(string message, bool isError = false)
        {
            string icon = isError ? "❌" : "✅";
            string time = DateTime.Now.ToString("HH:mm:ss");
            OperationLogs.Insert(0, $"[{time}] {icon} {message}");
        }

        // Public AddLog cho binding - khác signature với BaseViewModel nên không warning
        public void AddLog(string message)
        {
            Log(message, false);
        }

        private void ClearForm()
        {
            _selectedProductionCustomer = null;
            OnPropertyChanged(nameof(SelectedProductionCustomer));
            CurrentOrder = new ProductionOrder
            {
                OrderNo = GenerateOrderNo(),
                UserId = AppSession.CurrentUser?.Id ?? 1
            };
            OnPropertyChanged(nameof(CurrentOrder));
        }

        private string GenerateOrderNo()
        {
            return "LSX-" + DateTime.Now.ToString("yyMMddHHmm");
        }
    }
}