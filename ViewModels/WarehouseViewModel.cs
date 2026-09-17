using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using Microsoft.Win32;

namespace PhanMemInAnERP.ViewModels
{
    public class WarehouseViewModel : BaseViewModel
    {
        // 1. COLLECTIONS CHO DATAGRID
        private ObservableCollection<Material> _materials;
        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set { _materials = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ImportTransaction> ImportList { get; set; }
        public ObservableCollection<ExportTransaction> ExportList { get; set; }
        public ObservableCollection<InventoryReportItem> InventoryReports { get; set; }

        private readonly List<InventoryReportItem> _inventoryReportSource = new List<InventoryReportItem>();
        public ObservableCollection<string> InventoryReportSearchHints { get; } = new ObservableCollection<string>();

        private string _inventoryReportSearchText = "";
        public string InventoryReportSearchText
        {
            get => _inventoryReportSearchText;
            set
            {
                var v = value ?? "";
                if (_inventoryReportSearchText == v) return;
                _inventoryReportSearchText = v;
                OnPropertyChanged();
                ApplyInventoryReportFilter();
            }
        }

        /// <summary>Chọn nhanh từ ComboBox (đồng bộ Text).</summary>
        public void SetInventoryReportSearchFromCombo(string? selected)
        {
            if (string.IsNullOrEmpty(selected) || selected == "(Tất cả)")
                InventoryReportSearchText = "";
            else
                InventoryReportSearchText = selected;
        }

        public ISnackbarMessageQueue MyMessageQueue { get; set; } = new SnackbarMessageQueue();

        public ObservableCollection<string> OperationLogs { get; set; } = new ObservableCollection<string>();

        // Private log helper
        private void Log(string message, bool isError = false)
        {
            string icon = isError ? "❌" : "✅";
            string time = DateTime.Now.ToString("HH:mm:ss");
            OperationLogs.Insert(0, $"[{time}] {icon} {message}");
        }

        // Public AddLog cho binding - signature khác với BaseViewModel
        public void AddLog(string message)
        {
            Log(message, false);
        }

        // 2. PROPERTIES TAB VẬT TƯ
        private Material _currentMaterial = new Material();
        public Material CurrentMaterial { get => _currentMaterial; set { _currentMaterial = value; OnPropertyChanged(); } }

        // 3. PROPERTIES TAB NHẬP KHO
        public string ImpTicketNo { get; set; } = "NK-" + DateTime.Now.ToString("yyMMddHHmm");
        public DateTime ImpDate { get; set; } = DateTime.Now;
        public string ImpSupplier { get; set; }
        /// <summary>NCC đã từng dùng trên phiếu nhập — gợi ý trong ComboBox.</summary>
        public ObservableCollection<string> KnownSuppliers { get; } = new ObservableCollection<string>();
        public Material ImpSelectedMaterial { get; set; }
        private double _impQuantity;
        public double ImpQuantity { get => _impQuantity; set { _impQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImpTotal)); } }
        private double _impPrice;
        public double ImpPrice { get => _impPrice; set { _impPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImpTotal)); } }
        public double ImpTotal => ImpQuantity * ImpPrice; // Tự động tính thành tiền
        public string ImpNotes { get; set; }

        // 4. PROPERTIES TAB XUẤT KHO
        private string _expTicketNo = "XK-" + DateTime.Now.ToString("yyMMddHHmm");
        public string ExpTicketNo
        {
            get => _expTicketNo;
            set { _expTicketNo = value ?? ""; OnPropertyChanged(); }
        }

        public DateTime ExpDate { get; set; } = DateTime.Now;
        public string ExpPurpose { get; set; } = "Sản xuất";
        private Material _expSelectedMaterial;
        public Material ExpSelectedMaterial
        {
            get => _expSelectedMaterial;
            set { _expSelectedMaterial = value; OnPropertyChanged(); }
        }
        public double ExpQuantity { get; set; }
        public string ExpNotes { get; set; }

        public ObservableCollection<ProductionOrder> PendingProductionOrders { get; set; }

        private ProductionOrder _expSelectedProductionOrder;
        public ProductionOrder ExpSelectedProductionOrder
        {
            get => _expSelectedProductionOrder;
            set { _expSelectedProductionOrder = value; OnPropertyChanged(); }
        }

        // ===== XUẤT KHO TỪ SẢN PHẨM HOÀN THÀNH (NHẬP KHO SX) =====
        public ObservableCollection<ProductionImport> AvailableProductionImports { get; set; }
        private ProductionImport _expSelectedProductionImport;
        public ProductionImport ExpSelectedProductionImport
        {
            get => _expSelectedProductionImport;
            set
            {
                _expSelectedProductionImport = value;
                OnPropertyChanged();
                if (_expSelectedProductionImport != null)
                {
                    LoadProductionImportForExport(_expSelectedProductionImport);
                }
            }
        }

        // ===== CHỨC NĂNG NHẬP KHO SẢN XUẤT =====
        private ObservableCollection<ProductionImport> _productionImports;
        public ObservableCollection<ProductionImport> ProductionImports
        {
            get => _productionImports;
            set { _productionImports = value; OnPropertyChanged(); }
        }

        private ProductionImport _selectedProductionImport;
        public ProductionImport SelectedProductionImport
        {
            get => _selectedProductionImport;
            set { _selectedProductionImport = value; OnPropertyChanged(); }
        }

        // Form nhập kho sản xuất
        private string _prodImportTicketNo = "NK-SX-" + DateTime.Now.ToString("yyMMddHHmm");
        public string ProdImportTicketNo
        {
            get => _prodImportTicketNo;
            set { _prodImportTicketNo = value ?? ""; OnPropertyChanged(); }
        }

        public DateTime ProdImportDate { get; set; } = DateTime.Now;

        private ProductionOrder _prodImportSelectedOrder;
        public ProductionOrder ProdImportSelectedOrder
        {
            get => _prodImportSelectedOrder;
            set { _prodImportSelectedOrder = value; OnPropertyChanged(); }
        }

        private string _prodImportProductName = "";
        public string ProdImportProductName
        {
            get => _prodImportProductName;
            set { _prodImportProductName = value ?? ""; OnPropertyChanged(); }
        }

        private string _prodImportCustomerName = "";
        public string ProdImportCustomerName
        {
            get => _prodImportCustomerName;
            set { _prodImportCustomerName = value ?? ""; OnPropertyChanged(); }
        }

        private int _prodImportQuantity = 0;
        public int ProdImportQuantity
        {
            get => _prodImportQuantity;
            set { _prodImportQuantity = value; OnPropertyChanged(); }
        }

        private string _prodImportUnit = "Cái";
        public string ProdImportUnit
        {
            get => _prodImportUnit;
            set { _prodImportUnit = value ?? "Cái"; OnPropertyChanged(); }
        }

        public string ProdImportNotes { get; set; } = "";

        // ===== CHO XUẤT KHO TỪ BÁO GIÁ =====
        public ObservableCollection<Quotation> AvailableQuotations { get; set; }
        private Quotation _expSelectedQuotation;
        public Quotation ExpSelectedQuotation
        {
            get => _expSelectedQuotation;
            set
            {
                _expSelectedQuotation = value;
                OnPropertyChanged();
                if (_expSelectedQuotation != null)
                {
                    LoadQuotationMaterials(_expSelectedQuotation);
                }
            }
        }

        // Danh sách vật tư từ báo giá đã chọn
        public ObservableCollection<QuotationMaterialItem> QuotationMaterials { get; set; } = new ObservableCollection<QuotationMaterialItem>();

        private QuotationMaterialItem _selectedQuotationMaterial;
        public QuotationMaterialItem SelectedQuotationMaterial
        {
            get => _selectedQuotationMaterial;
            set
            {
                _selectedQuotationMaterial = value;
                OnPropertyChanged();
                if (_selectedQuotationMaterial != null)
                {
                    // Tự động điền thông tin khi chọn vật tư từ báo giá
                    ExpSelectedMaterial = Materials.FirstOrDefault(m => m.Name == _selectedQuotationMaterial.MaterialName);
                    ExpQuantity = _selectedQuotationMaterial.RequiredQuantity;
                    ExpNotes = $"Xuất từ Báo giá: {_expSelectedQuotation?.CustomerName} - {_expSelectedQuotation?.ProductName}";
                }
            }
        }

        public ICommand ImportFromQuotationCommand { get; }

        private int _totalMaterialTypes; public int TotalMaterialTypes { get => _totalMaterialTypes; set { _totalMaterialTypes = value; OnPropertyChanged(); } }
        private double _totalInventoryValue; public double TotalInventoryValue { get => _totalInventoryValue; set { _totalInventoryValue = value; OnPropertyChanged(); } }
        private int _lowStockCount; public int LowStockCount { get => _lowStockCount; set { _lowStockCount = value; OnPropertyChanged(); } }

        // COMMANDS
        public ICommand SaveMaterialCommand { get; }
        public ICommand DeleteMaterialCommand { get; }
        public ICommand SaveImportCommand { get; }
        public ICommand DeleteImportCommand { get; }
        public ICommand DeleteExportCommand { get; }
        public ICommand SaveExportCommand { get; }
        public ICommand SaveProductionImportCommand { get; }
        public ICommand ExportProductionToSalesCommand { get; }

        public ICommand ExportExcelCommand { get; }

        // ================== TAB SỔ CHI TIẾT VẬT TƯ ==================
        private DateTime _scTuNgay = DateTime.Now.AddMonths(-1);
        public DateTime SCTuNgay
        {
            get => _scTuNgay;
            set { _scTuNgay = value; OnPropertyChanged(); }
        }

        private DateTime _scDenNgay = DateTime.Now;
        public DateTime SCDenNgay
        {
            get => _scDenNgay;
            set { _scDenNgay = value; OnPropertyChanged(); }
        }

        private Material _scSelectedMaterial;
        public Material SCSelectedMaterial
        {
            get => _scSelectedMaterial;
            set { _scSelectedMaterial = value; OnPropertyChanged(); }
        }

        private ObservableCollection<SoChiTietVatTuItem> _soChiTietItems;
        public ObservableCollection<SoChiTietVatTuItem> SoChiTietItems
        {
            get => _soChiTietItems;
            set { _soChiTietItems = value; OnPropertyChanged(); }
        }

        private double _scTongNhap;
        public double SCTongNhap
        {
            get => _scTongNhap;
            set { _scTongNhap = value; OnPropertyChanged(); }
        }

        private double _scTongXuat;
        public double SCTongXuat
        {
            get => _scTongXuat;
            set { _scTongXuat = value; OnPropertyChanged(); }
        }

        private double _scTonDauKy;
        public double SCTonDauKy
        {
            get => _scTonDauKy;
            set { _scTonDauKy = value; OnPropertyChanged(); }
        }

        private double _scTonCuoiKy;
        public double SCTonCuoiKy
        {
            get => _scTonCuoiKy;
            set { _scTonCuoiKy = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> SCMaterialHints { get; } = new ObservableCollection<string>();

        public ICommand SCTimKiemCommand { get; }
        public ICommand SCXuatExcelCommand { get; }
        public ICommand SCClearFilterCommand { get; }

        // Danh sách tất cả vật tư cho filter (để load nhanh)
        private List<Material> _allMaterialsForFilter = new List<Material>();

        /// <summary>Lệnh SX để lập phiếu bàn giao hàng (sau khi đã SX xong, giao cho khách).</summary>
        public ObservableCollection<ProductionOrder> HandoverProductionOrders { get; set; } = new ObservableCollection<ProductionOrder>();

        private ProductionOrder _selectedHandoverPo;
        public ProductionOrder SelectedHandoverPo
        {
            get => _selectedHandoverPo;
            set
            {
                _selectedHandoverPo = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _handoverPhieuSo = "PBG-" + DateTime.Now.ToString("yyMMddHHmm");
        public string HandoverPhieuSo
        {
            get => _handoverPhieuSo;
            set { _handoverPhieuSo = value ?? ""; OnPropertyChanged(); }
        }

        private DateTime _handoverNgayLap = DateTime.Now;
        public DateTime HandoverNgayLap
        {
            get => _handoverNgayLap;
            set { _handoverNgayLap = value; OnPropertyChanged(); }
        }

        private string _handoverNguoiBanGiao = "";
        public string HandoverNguoiBanGiao
        {
            get => _handoverNguoiBanGiao;
            set { _handoverNguoiBanGiao = value ?? ""; OnPropertyChanged(); }
        }

        public ICommand PrintHandoverCommand { get; }

        public WarehouseViewModel()
        {
            ExportExcelCommand = new RelayCommand<ExportTransaction>(ExportPhieuXuat);
            PrintHandoverCommand = new RelayCommand(PrintHandover, () => SelectedHandoverPo != null);
            SaveMaterialCommand = new RelayCommand(SaveMaterial);
            DeleteMaterialCommand = new RelayCommand<Material>(DeleteMaterial);
            SaveImportCommand = new RelayCommand(SaveImport);
            DeleteImportCommand = new RelayCommand<ImportTransaction>(DeleteImport);
            DeleteExportCommand = new RelayCommand<ExportTransaction>(DeleteExport);
            SaveExportCommand = new RelayCommand(SaveExport);
            SaveProductionImportCommand = new RelayCommand(SaveProductionImport);
            ExportProductionToSalesCommand = new RelayCommand<ProductionImport>(ExportProductionToSales, CanExportProductionToSales);
            ImportFromQuotationCommand = new RelayCommand<Quotation>(ImportExportFromQuotation);

            // Commands cho Sổ Chi Tiết Vật Tư
            SCTimKiemCommand = new RelayCommand(LoadSoChiTietVatTu);
            SCXuatExcelCommand = new RelayCommand(ExportSoChiTietExcel, () => SoChiTietItems != null && SoChiTietItems.Count > 0);
            SCClearFilterCommand = new RelayCommand(ClearSoChiTietFilter);

            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không tải được dữ liệu kho. Kiểm tra SQL Server và chạy: dotnet ef database update\n\nChi tiết: " + ex.Message,
                    "Lỗi CSDL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                InitEmptyWarehouseState();
            }
        }

        private void InitEmptyWarehouseState()
        {
            Materials = new ObservableCollection<Material>();
            ImportList = new ObservableCollection<ImportTransaction>();
            ExportList = new ObservableCollection<ExportTransaction>();
            PendingProductionOrders = new ObservableCollection<ProductionOrder>();
            HandoverProductionOrders = new ObservableCollection<ProductionOrder>();
            ProductionImports = new ObservableCollection<ProductionImport>();
            AvailableQuotations = new ObservableCollection<Quotation>();
            InventoryReports = new ObservableCollection<InventoryReportItem>();
            SoChiTietItems = new ObservableCollection<SoChiTietVatTuItem>();
            _inventoryReportSource.Clear();
            InventoryReportSearchText = "";
            InventoryReportSearchHints.Clear();
            InventoryReportSearchHints.Add("(Tất cả)");
            SCMaterialHints.Clear();
            SCMaterialHints.Add("(Tất cả vật tư)");
            KnownSuppliers.Clear();
            OnPropertyChanged(nameof(Materials));
            OnPropertyChanged(nameof(ImportList));
            OnPropertyChanged(nameof(ExportList));
            OnPropertyChanged(nameof(PendingProductionOrders));
            OnPropertyChanged(nameof(HandoverProductionOrders));
            OnPropertyChanged(nameof(ProductionImports));
            OnPropertyChanged(nameof(AvailableQuotations));
            OnPropertyChanged(nameof(InventoryReports));
            OnPropertyChanged(nameof(SoChiTietItems));
        }

        private void ExportPhieuXuat(ExportTransaction phieuThucTe)
        {
            if (phieuThucTe == null) return;

            double unitPrice = phieuThucTe.Material?.AveragePrice ?? 0;
            var data = new Models.PhieuXuatKhoModel
            {
                SoPhieu = phieuThucTe.TicketNo,
                NgayXuat = phieuThucTe.ExportDate,
                NguoiNhan = "Nhân viên nhận",
                DiaChiBoPhan = phieuThucTe.Purpose ?? "",
                LyDoXuat = phieuThucTe.Notes ?? "Xuất kho vật tư",
                XuatTaiKho = phieuThucTe.Material?.Category ?? "",
                ChungTuGoc = "",
                TongTien = phieuThucTe.Quantity * unitPrice,
                TongTienBangChu = "(Chưa lập hàm đọc số)"
            };

            data.ChiTietHangHoa.Add(new Models.PhieuXuatKhoChiTiet
            {
                TenVatTu = phieuThucTe.Material?.Name ?? "(Không rõ)",
                MaSo = phieuThucTe.Material?.Code ?? "",
                DVT = phieuThucTe.Material?.Unit ?? "",
                SoLuongYeuCau = phieuThucTe.Quantity,
                SoLuongThucXuat = phieuThucTe.Quantity,
                DonGia = unitPrice
            });

            // Mở hộp thoại chọn nơi lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Lưu Phiếu Xuất Kho",
                FileName = $"PhieuXuatKho_{data.SoPhieu}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Gọi class Helper mà chúng ta đã tạo ở bước trước
                    Helpers.ExcelExportHelper.XuatPhieuXuatKho(data, saveFileDialog.FileName);

                    MessageBox.Show("Xuất file Excel thành công!", "Thông báo");

                    // Tự động mở file lên xem luôn
                    Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Có lỗi xảy ra khi xuất Excel: " + ex.Message, "Lỗi");
                }
            }
        }



        private void LoadData()
        {
            using (var db = new AppDbContext())
            {
                int? selectedImpId = ImpSelectedMaterial?.Id;
                int? selectedExpId = ExpSelectedMaterial?.Id;

                var list = db.Materials.AsNoTracking().ToList();
                Materials = new ObservableCollection<Material>(list);

                if (selectedImpId != null)
                    ImpSelectedMaterial = Materials.FirstOrDefault(x => x.Id == selectedImpId);
                if (selectedExpId != null)
                    ExpSelectedMaterial = Materials.FirstOrDefault(x => x.Id == selectedExpId);

                OnPropertyChanged(nameof(Materials));
                OnPropertyChanged(nameof(ImpSelectedMaterial));
                OnPropertyChanged(nameof(ExpSelectedMaterial));

                ImportList = new ObservableCollection<ImportTransaction>(db.ImportTransactions.Include(x => x.Material).OrderByDescending(x => x.ImportDate).ToList());
                ExportList = new ObservableCollection<ExportTransaction>(db.ExportTransactions.Include(x => x.Material).OrderByDescending(x => x.ExportDate).ToList());

                // Nạp danh sách lệnh sản xuất cho dropdown "Thuộc lệnh"
                var prodOrders = db.ProductionOrders.OrderByDescending(x => x.OrderDate).Take(50).ToList();
                PendingProductionOrders = new ObservableCollection<ProductionOrder>(prodOrders);

                var handoverList = db.ProductionOrders.AsNoTracking().OrderByDescending(x => x.OrderDate).Take(100).ToList();
                HandoverProductionOrders = new ObservableCollection<ProductionOrder>(handoverList);
                OnPropertyChanged(nameof(HandoverProductionOrders));

                // Nạp danh sách sản phẩm hoàn thành (Nhập kho sản xuất)
                var prodImports = db.ProductionImports
                    .Include(p => p.ProductionOrder)
                    .Include(p => p.SalesOrder)
                    .OrderByDescending(x => x.ImportDate)
                    .Take(200)
                    .ToList();
                ProductionImports = new ObservableCollection<ProductionImport>(prodImports);
                OnPropertyChanged(nameof(ProductionImports));

                // Nạp danh sách báo giá cho xuất kho
                var quotations = db.Quotations.OrderByDescending(q => q.QuoteDate).Take(100).ToList();
                AvailableQuotations = new ObservableCollection<Quotation>(quotations);

                // Nạp danh sách sản phẩm hoàn thành cho xuất kho bán hàng
                var availableProdImports = db.ProductionImports
                    .Include(p => p.ProductionOrder)
                    .Include(p => p.SalesOrder)
                    .Where(p => p.Status == "Chưa xuất") // Chưa xuất kho bán hàng
                    .OrderByDescending(x => x.ImportDate)
                    .Take(100)
                    .ToList();
                AvailableProductionImports = new ObservableCollection<ProductionImport>(availableProdImports);

                OnPropertyChanged(nameof(ImportList));
                OnPropertyChanged(nameof(ExportList));
                OnPropertyChanged(nameof(PendingProductionOrders));
                OnPropertyChanged(nameof(ProductionImports));
                OnPropertyChanged(nameof(AvailableQuotations));
                OnPropertyChanged(nameof(AvailableProductionImports));

                KnownSuppliers.Clear();
                foreach (var s in db.ImportTransactions
                             .Select(i => i.Supplier)
                             .AsEnumerable()
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Select(x => x.Trim())
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    KnownSuppliers.Add(s);

                LoadInventoryReport(db);
            }
        }

        /// <summary>
        /// Tải danh sách vật tư từ báo giá đã chọn
        /// </summary>
        private void LoadQuotationMaterials(Quotation quote)
        {
            QuotationMaterials.Clear();
            if (quote == null) return;

            using (var db = new AppDbContext())
            {
                // Tính toán số lượng giấy cần thiết dựa trên báo giá
                double pricePerRam = ((quote.PrintLength * quote.PrintWidth) / 10000.0)
                                     * (quote.PaperGsm / 1000.0)
                                     * (quote.PaperPricePerTon / 1000.0)
                                     * 500.0;
                double totalSheets = (quote.Quantity / (double)Math.Max(1, quote.SoCon)) + quote.BuHao;
                double totalRams = Math.Ceiling(totalSheets / 500.0);
                double paperWeightNeeded = totalRams * pricePerRam / 1000; // Chuyển sang Kg

                // Tìm vật tư giấy trong kho
                var paperType = quote.PaperType ?? "";
                var paperMaterial = string.IsNullOrEmpty(paperType)
                    ? null
                    : db.Materials.FirstOrDefault(m => m.Name != null && m.Name.Contains(paperType));
                double paperStock = paperMaterial?.StockQuantity ?? 0;

                QuotationMaterials.Add(new Models.QuotationMaterialItem
                {
                    MaterialName = paperMaterial?.Name ?? (string.IsNullOrEmpty(paperType) ? "(Chưa rõ giấy)" : paperType),
                    MaterialInfo = $"{paperType} {quote.PaperGsm} gsm",
                    RequiredQuantity = paperWeightNeeded,
                    Unit = "Kg",
                    AvailableStock = paperStock
                });

                // Các vật tư khác từ chi phí phát sinh (nếu có)
                if (quote.ExtraCosts != null)
                {
                    foreach (var extra in quote.ExtraCosts)
                    {
                        var itemName = extra.ItemName ?? "";
                        var extraMaterial = string.IsNullOrEmpty(itemName)
                            ? null
                            : db.Materials.FirstOrDefault(m => m.Name != null && m.Name.Contains(itemName));
                        QuotationMaterials.Add(new Models.QuotationMaterialItem
                        {
                            MaterialName = extraMaterial?.Name ?? itemName,
                            MaterialInfo = itemName,
                            RequiredQuantity = extra.Quantity,
                            Unit = extraMaterial?.Unit ?? "Cái",
                            AvailableStock = extraMaterial?.StockQuantity ?? 0
                        });
                    }
                }

                OnPropertyChanged(nameof(QuotationMaterials));
            }
        }

        /// <summary>
        /// Import xuất kho từ báo giá
        /// </summary>
        private void ImportExportFromQuotation(Quotation quote)
        {
            if (quote == null) return;
            ExpSelectedQuotation = quote;
            MessageBox.Show($"Đã tải thông tin từ Báo giá của {quote.CustomerName}\nVui lòng chọn vật tư cần xuất từ danh sách bên dưới.", "Thông báo");
        }

        private void SaveMaterial()
        {
            if (string.IsNullOrEmpty(CurrentMaterial.Code) || string.IsNullOrEmpty(CurrentMaterial.Name))
            {
                MessageBox.Show("Vui lòng nhập Mã vật tư và Tên vật tư!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Đảm bảo cột NOT NULL trong DB không bị null
            CurrentMaterial.Notes = CurrentMaterial.Notes ?? "";
            CurrentMaterial.Unit = CurrentMaterial.Unit ?? "";
            CurrentMaterial.Category = CurrentMaterial.Category ?? "";

            using (var db = new AppDbContext())
            {
                if (CurrentMaterial.Id == 0)
                {
                    db.Materials.Add(CurrentMaterial);
                }
                else
                {
                    db.Materials.Update(CurrentMaterial);
                }
                db.SaveChanges();
            }
            MessageBox.Show("Lưu vật tư thành công!");
            CurrentMaterial = new Material();
            LoadData();
        }

        private void DeleteMaterial(Material mat)
        {
            if (mat == null) return;

            if (MessageBox.Show($"Xóa vật tư \"{mat.Name}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new AppDbContext())
            {
                db.Materials.Remove(mat);
                db.SaveChanges();
            }
            LoadData();
        }

        private void SaveImport()
        {
            if (ImpSelectedMaterial == null || ImpQuantity <= 0) return;

            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!", "Lỗi");
                return;
            }

            using (var db = new AppDbContext())
            {
                var importTx = new ImportTransaction
                {
                    TicketNo = ImpTicketNo,
                    ImportDate = ImpDate,
                    Supplier = ImpSupplier ?? "",
                    MaterialId = ImpSelectedMaterial.Id,
                    Quantity = ImpQuantity,
                    UnitPrice = ImpPrice,
                    TotalPrice = ImpTotal,
                    Notes = ImpNotes ?? "",
                    UserId = AppSession.CurrentUser.Id
                };
                db.ImportTransactions.Add(importTx);

                var material = db.Materials.Find(ImpSelectedMaterial.Id);
                double oldTotalValue = material.StockQuantity * material.AveragePrice;
                double newTotalValue = oldTotalValue + ImpTotal;

                material.StockQuantity += ImpQuantity;
                material.AveragePrice = newTotalValue / material.StockQuantity;

                db.SaveChanges();
            }

            ImpTicketNo = "NK-" + DateTime.Now.ToString("yyMMddHHmm");
            ImpQuantity = 0; ImpPrice = 0; ImpSupplier = ""; ImpNotes = "";
            OnPropertyChanged(nameof(ImpTicketNo)); OnPropertyChanged(nameof(ImpQuantity));
            OnPropertyChanged(nameof(ImpPrice)); OnPropertyChanged(nameof(ImpSupplier));

            LoadData();
            MessageBox.Show("Lưu phiếu nhập & Cập nhật tồn kho thành công!");
        }

        private void DeleteImport(ImportTransaction tx)
        {
            if (tx == null) return;

            if (MessageBox.Show(
                    $"Xóa phiếu nhập \"{tx.TicketNo}\"?\nTồn kho sẽ được trừ lại {tx.Quantity:N2} ({tx.Material?.Name}).",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var entity = db.ImportTransactions.Include(i => i.Material).FirstOrDefault(i => i.Id == tx.Id);
                    if (entity == null)
                    {
                        MessageBox.Show("Không tìm thấy phiếu nhập trong CSDL.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                        return;
                    }

                    var material = db.Materials.Find(entity.MaterialId);
                    if (material == null)
                    {
                        db.ImportTransactions.Remove(entity);
                        db.SaveChanges();
                        LoadData();
                        MessageBox.Show("Đã xóa phiếu (vật tư không còn trong danh mục).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    double q = entity.Quantity;
                    double importValue = entity.TotalPrice;
                    double stock = material.StockQuantity;
                    double avg = material.AveragePrice;

                    if (stock + 1e-9 < q)
                    {
                        MessageBox.Show(
                            "Tồn kho hiện tại nhỏ hơn số lượng trên phiếu nhập — không thể xóa an toàn (có thể đã xuất bớt).",
                            "Không thể xóa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    double newStock = stock - q;
                    if (newStock <= 1e-9)
                    {
                        material.StockQuantity = 0;
                        material.AveragePrice = 0;
                    }
                    else
                    {
                        double newAvg = (stock * avg - importValue) / newStock;
                        material.StockQuantity = newStock;
                        material.AveragePrice = newAvg < 0 ? 0 : newAvg;
                    }

                    db.ImportTransactions.Remove(entity);
                    db.SaveChanges();
                }

                LoadData();
                MessageBox.Show("Đã xóa phiếu nhập và cập nhật tồn kho.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa: {ex.InnerException?.Message ?? ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteExport(ExportTransaction tx)
        {
            if (tx == null) return;

            if (MessageBox.Show(
                    $"Xóa phiếu xuất \"{tx.TicketNo}\"?\nTồn kho sẽ được cộng lại {tx.Quantity:N2} ({tx.Material?.Name}).",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var entity = db.ExportTransactions.Include(e => e.Material).FirstOrDefault(e => e.Id == tx.Id);
                    if (entity == null)
                    {
                        MessageBox.Show("Không tìm thấy phiếu xuất trong CSDL.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                        return;
                    }

                    var material = db.Materials.Find(entity.MaterialId);
                    if (material == null)
                    {
                        db.ExportTransactions.Remove(entity);
                        db.SaveChanges();
                        LoadData();
                        MessageBox.Show("Đã xóa phiếu (vật tư không còn trong danh mục).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    material.StockQuantity += entity.Quantity;
                    db.ExportTransactions.Remove(entity);
                    db.SaveChanges();
                }

                LoadData();
                MessageBox.Show("Đã xóa phiếu xuất và cập nhật tồn kho.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa: {ex.InnerException?.Message ?? ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveExport()
        {
            if (ExpSelectedMaterial == null || ExpQuantity <= 0) return;
            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!", "Lỗi");
                return;
            }

            var ticket = (ExpTicketNo ?? "").Trim();
            if (string.IsNullOrEmpty(ticket))
            {
                MessageBox.Show("Vui lòng nhập số phiếu.", "Thiếu số phiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                if (db.ExportTransactions.Any(e => e.TicketNo == ticket))
                {
                    MessageBox.Show(
                        "Số phiếu này đã tồn tại. Bấm \"Số mới\" hoặc đổi số trước khi lưu.",
                        "Trùng số phiếu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var material = db.Materials.Find(ExpSelectedMaterial.Id);
                if (material == null)
                {
                    MessageBox.Show("Không tìm thấy vật tư trong kho. Vui lòng chọn lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (material.StockQuantity < ExpQuantity)
                {
                    MessageBox.Show("Lỗi: Số lượng xuất lớn hơn Tồn kho hiện tại!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var exportTx = new ExportTransaction
                {
                    TicketNo = ticket,
                    ExportDate = ExpDate,
                    Purpose = ExpPurpose,
                    MaterialId = ExpSelectedMaterial.Id,
                    Quantity = ExpQuantity,
                    Notes = ExpNotes ?? "",
                    UserId = AppSession.CurrentUser.Id,
                    ProductionOrderId = (ExpPurpose == "Sản xuất" && ExpSelectedProductionOrder != null) ? ExpSelectedProductionOrder.Id : (int?)null,
                    QuotationId = ExpSelectedQuotation?.Id // Liên kết với báo giá nếu có
                };
                db.ExportTransactions.Add(exportTx);

                // 2. Cập nhật tồn kho
                material.StockQuantity -= ExpQuantity;

                db.SaveChanges();

                if (material.StockQuantity <= material.MinStock)
                {
                    // Chỉ truyền vào nội dung thông báo, không truyền Action
                    MyMessageQueue.Enqueue($"⚠️ CẢNH BÁO: {material.Name} đã chạm mức tồn tối thiểu!");
                }
                else
                {
                    MyMessageQueue.Enqueue("✅ Xuất kho thành công!");
                }
            }

            // Reset form xuất
            ExpTicketNo = "XK-" + DateTime.Now.ToString("yyMMddHHmm");
            ExpQuantity = 0; ExpNotes = "";
            ExpSelectedQuotation = null;
            QuotationMaterials.Clear();
            OnPropertyChanged(nameof(ExpTicketNo));
            OnPropertyChanged(nameof(ExpQuantity));
            OnPropertyChanged(nameof(ExpSelectedQuotation));

            LoadData();
            MessageBox.Show("Lưu phiếu xuất thành công!");
        }

        private void LoadInventoryReport(AppDbContext db)
        {
            var allMaterials = db.Materials.ToList();
            var allImports = db.ImportTransactions.ToList();
            var allExports = db.ExportTransactions.ToList();

            var impMax = allImports
                .GroupBy(i => i.MaterialId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.ImportDate));
            var expMax = allExports
                .Where(e => e.MaterialId.HasValue)
                .GroupBy(e => e.MaterialId.Value!)
                .ToDictionary(g => g.Key, g => g.Max(x => x.ExportDate));

            var reportList = allMaterials.Select(m => new InventoryReportItem
            {
                MaterialCode = m.Code,
                MaterialName = m.Name,
                Unit = m.Unit,
                TotalImport = allImports.Where(i => i.MaterialId == m.Id).Sum(i => i.Quantity),
                TotalExport = allExports.Where(e => e.MaterialId == m.Id).Sum(e => e.Quantity),
                CurrentStock = m.StockQuantity,
                MinStock = m.MinStock,
                AvgPrice = m.AveragePrice,
                LastMovementTime = LatestMovement(m.Id, impMax, expMax)
            }).ToList();

            _inventoryReportSource.Clear();
            _inventoryReportSource.AddRange(reportList);
            RebuildInventorySearchHints();
            ApplyInventoryReportFilter();

                // Cập nhật danh sách vật tư cho filter Sổ Chi Tiết
            _allMaterialsForFilter = allMaterials;
            RebuildSCMaterialHints();

            // Load data for Sổ Chi Tiết Vật Tư
            LoadSoChiTietVatTu();

            if (Materials != null)
            {
                foreach (var mat in Materials)
                    mat.LastMovementTime = LatestMovement(mat.Id, impMax, expMax);
                OnPropertyChanged(nameof(Materials));
            }

            // Cập nhật Dashboard thống kê
            TotalMaterialTypes = allMaterials.Count;
            TotalInventoryValue = allMaterials.Sum(m => m.StockQuantity * m.AveragePrice);
            LowStockCount = allMaterials.Count(m => m.StockQuantity <= m.MinStock);
        }

        private void RebuildInventorySearchHints()
        {
            InventoryReportSearchHints.Clear();
            InventoryReportSearchHints.Add("(Tất cả)");
            foreach (var x in _inventoryReportSource.OrderBy(r => r.MaterialName ?? ""))
                InventoryReportSearchHints.Add($"{x.MaterialCode} | {x.MaterialName}");
            OnPropertyChanged(nameof(InventoryReportSearchHints));
        }

        private void ApplyInventoryReportFilter()
        {
            IEnumerable<InventoryReportItem> q = _inventoryReportSource;
            var s = (InventoryReportSearchText ?? "").Trim();
            if (s.Length > 0)
            {
                var low = s.ToLowerInvariant();
                q = q.Where(x =>
                    (!string.IsNullOrEmpty(x.MaterialCode) && x.MaterialCode.ToLowerInvariant().Contains(low)) ||
                    (!string.IsNullOrEmpty(x.MaterialName) && x.MaterialName.ToLowerInvariant().Contains(low)));
            }

            InventoryReports = new ObservableCollection<InventoryReportItem>(q.ToList());
            OnPropertyChanged(nameof(InventoryReports));
        }

        // ================== SỔ CHI TIẾT VẬT TƯ ==================

        private void RebuildSCMaterialHints()
        {
            SCMaterialHints.Clear();
            SCMaterialHints.Add("(Tất cả vật tư)");
            foreach (var m in _allMaterialsForFilter.OrderBy(x => x.Name ?? ""))
                SCMaterialHints.Add($"{m.Code} | {m.Name}");
            OnPropertyChanged(nameof(SCMaterialHints));
        }

        private void LoadSoChiTietVatTu()
        {
            using var db = new AppDbContext();

            // Lấy danh sách tất cả vật tư
            var allMaterials = db.Materials.AsNoTracking().ToList();
            var allImports = db.ImportTransactions.AsNoTracking().ToList();
            var allExports = db.ExportTransactions.AsNoTracking().ToList();

            // Lọc theo vật tư được chọn
            var filterMaterialId = SCSelectedMaterial?.Id;

            // Lấy tất cả giao dịch trong khoảng thời gian
            var fromDate = SCTuNgay.Date;
            var toDate = SCDenNgay.Date.AddDays(1).AddSeconds(-1);

            // Tính tồn đầu kỳ
            double tongDauKy = 0;
            if (filterMaterialId.HasValue)
            {
                // Tồn đầu kỳ cho 1 vật tư
                var mat = allMaterials.FirstOrDefault(m => m.Id == filterMaterialId.Value);
                var importsBefore = allImports.Where(i => i.MaterialId == filterMaterialId.Value && i.ImportDate < fromDate).Sum(i => i.Quantity);
                var exportsBefore = allExports.Where(e => e.MaterialId == filterMaterialId.Value && e.ExportDate < fromDate).Sum(e => e.Quantity);
                tongDauKy = mat != null ? mat.StockQuantity + exportsBefore - importsBefore : 0;
            }
            else
            {
                // Tổng tồn đầu kỳ tất cả vật tư
                tongDauKy = allMaterials.Sum(m => m.StockQuantity);
                foreach (var m in allMaterials)
                {
                    var importsBefore = allImports.Where(i => i.MaterialId == m.Id && i.ImportDate < fromDate).Sum(i => i.Quantity);
                    var exportsBefore = allExports.Where(e => e.MaterialId == m.Id && e.ExportDate < fromDate).Sum(e => e.Quantity);
                    tongDauKy += exportsBefore - importsBefore;
                }
            }

            SCTonDauKy = tongDauKy;

            // Tạo danh sách các dòng chi tiết
            var items = new List<SoChiTietVatTuItem>();

            // Lấy các giao dịch nhập kho trong khoảng thời gian
            var importTxs = allImports
                .Where(i => i.ImportDate >= fromDate && i.ImportDate <= toDate)
                .Where(i => !filterMaterialId.HasValue || i.MaterialId == filterMaterialId.Value)
                .OrderBy(i => i.ImportDate)
                .ToList();

            foreach (var imp in importTxs)
            {
                var mat = allMaterials.FirstOrDefault(m => m.Id == imp.MaterialId);
                items.Add(new SoChiTietVatTuItem
                {
                    Ngay = imp.ImportDate,
                    SoPhieu = imp.TicketNo,
                    LoaiPhieu = "NHẬP",
                    MaVatTu = mat?.Code ?? "",
                    TenVatTu = mat?.Name ?? "",
                    DonViTinh = mat?.Unit ?? "",
                    SoLuongNhap = imp.Quantity,
                    DonGia = imp.UnitPrice,
                    GhiChu = $"Nhận từ: {imp.Supplier}"
                });
            }

            // Lấy các giao dịch xuất kho trong khoảng thời gian
            var exportTxs = allExports
                .Where(e => e.ExportDate >= fromDate && e.ExportDate <= toDate)
                .Where(e => !filterMaterialId.HasValue || e.MaterialId == filterMaterialId.Value)
                .OrderBy(e => e.ExportDate)
                .ToList();

            foreach (var exp in exportTxs)
            {
                var mat = allMaterials.FirstOrDefault(m => m.Id == exp.MaterialId);
                items.Add(new SoChiTietVatTuItem
                {
                    Ngay = exp.ExportDate,
                    SoPhieu = exp.TicketNo,
                    LoaiPhieu = "XUẤT",
                    MaVatTu = mat?.Code ?? "",
                    TenVatTu = mat?.Name ?? "",
                    DonViTinh = mat?.Unit ?? "",
                    SoLuongXuat = exp.Quantity,
                    DonGia = mat?.AveragePrice ?? 0,
                    GhiChu = exp.Purpose ?? ""
                });
            }

            // Sắp xếp theo ngày
            items = items.OrderBy(x => x.Ngay).ThenBy(x => x.LoaiPhieu == "NHẬP" ? 0 : 1).ToList();

            // Tính tồn tức thời sau mỗi giao dịch
            double runningStock = tongDauKy;
            foreach (var item in items)
            {
                if (item.LoaiPhieu == "NHẬP")
                    runningStock += item.SoLuongNhap;
                else
                    runningStock -= item.SoLuongXuat;

                item.TonDauKy = tongDauKy;
                item.TonCuoiKy = runningStock;
            }

            // Tính tổng hợp
            double tongNhap = items.Sum(i => i.SoLuongNhap);
            double tongXuat = items.Sum(i => i.SoLuongXuat);
            SCTongNhap = tongNhap;
            SCTongXuat = tongXuat;
            SCTonCuoiKy = runningStock;

            SoChiTietItems = new ObservableCollection<SoChiTietVatTuItem>(items);
            OnPropertyChanged(nameof(SoChiTietItems));
        }

        private void ClearSoChiTietFilter()
        {
            SCSelectedMaterial = null;
            SCTimKiemCommand.Execute(null);
        }

        private void ExportSoChiTietExcel()
        {
            if (SoChiTietItems == null || SoChiTietItems.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất Excel!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Lưu Sổ Chi Tiết Vật Tư",
                FileName = $"SoChiTietVatTu_{SCTuNgay:yyyyMMdd}_{SCDenNgay:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExportHelper.XuatSoChiTietVatTu(SoChiTietItems.ToList(), SCTonDauKy, SCTongNhap, SCTongXuat, SCTonCuoiKy, SCSelectedMaterial, SCTuNgay, SCDenNgay, saveFileDialog.FileName);
                    MessageBox.Show("Xuất file Excel thành công!", "Thông báo");
                    Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Có lỗi xảy ra khi xuất Excel: " + ex.Message, "Lỗi");
                }
            }
        }

        private void PrintHandover()
        {
            if (SelectedHandoverPo == null) return;
            var phieu = (HandoverPhieuSo ?? "").Trim();
            if (string.IsNullOrEmpty(phieu))
            {
                MessageBox.Show("Vui lòng nhập số phiếu bàn giao.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = BuildHandoverPrintData(SelectedHandoverPo.Id);
            if (data == null)
            {
                MessageBox.Show("Không tải được thông tin lệnh sản xuất.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DeliveryHandoverPrintHelper.ShowPreviewThenPrint(data);
                HandoverPhieuSo = "PBG-" + DateTime.Now.ToString("yyMMddHHmm");
                OnPropertyChanged(nameof(HandoverPhieuSo));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không in được: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DeliveryHandoverPrintData? BuildHandoverPrintData(int productionOrderId)
        {
            using var db = new AppDbContext();
            var po = db.ProductionOrders.AsNoTracking().FirstOrDefault(p => p.Id == productionOrderId);
            if (po == null) return null;

            SalesOrder? so = null;
            if (po.SalesOrderId.HasValue)
                so = db.SalesOrders.AsNoTracking().FirstOrDefault(s => s.Id == po.SalesOrderId.Value);

            Quotation? q = null;
            if (so?.QuotationId != null)
                q = db.Quotations.AsNoTracking().FirstOrDefault(x => x.Id == so.QuotationId.Value);

            string invLabel = "—";
            if (so != null && !string.IsNullOrWhiteSpace(so.OrderNo))
            {
                var inv = db.Invoices.AsNoTracking().FirstOrDefault(i => i.RefOrderNo == so.OrderNo);
                if (inv != null) invLabel = inv.InvoiceNo ?? "—";
            }

            var lines = new List<string>();
            var mat = string.IsNullOrWhiteSpace(po.Material) ? "—" : po.Material.Trim();
            var materialLine = $"Loại giấy / vật liệu: {mat}";
            if (q != null && !string.IsNullOrWhiteSpace(q.PaperType))
                materialLine += $" ({q.PaperType.Trim()}, {q.PaperGsm} gsm)";
            lines.Add(materialLine);

            lines.Add($"Kích thước SP (khuôn): {(string.IsNullOrWhiteSpace(po.Dimensions) ? "—" : po.Dimensions.Trim())}");
            if (q != null && q.PrintLength > 0 && q.PrintWidth > 0)
                lines.Add($"Khổ in: {q.PrintLength} × {q.PrintWidth} cm — {q.SoCon} con/tờ, bù hao {q.BuHao} tờ");

            if (q != null)
            {
                lines.Add($"In: {q.ColorCount} màu");
                if (!string.IsNullOrWhiteSpace(q.LaminationType) && q.LaminationPrice > 0)
                    lines.Add($"Cán màng: {q.LaminationType.Trim()}");
            }

            var stages = new List<string>();
            if (po.HasPrinting) stages.Add("In");
            if (po.HasPlateMaking) stages.Add("Làm kẽm");
            if (po.HasLamination) stages.Add("Cán màng");
            if (po.HasDieCutting) stages.Add("Bế");
            if (po.HasGluing) stages.Add("Dán");
            if (po.HasStringing) stages.Add("Xỏ dây");
            if (po.HasButtoning) stages.Add("Đóng nút");
            if (po.HasPackaging) stages.Add("Đóng thùng");
            if (stages.Count > 0)
                lines.Add("Công đoạn thực hiện: " + string.Join(", ", stages));

            return new DeliveryHandoverPrintData
            {
                PhieuSo = (HandoverPhieuSo ?? "").Trim(),
                NgayLap = HandoverNgayLap,
                KhachHang = po.CustomerName ?? "",
                DiaChi = so?.Address ?? "",
                DienThoai = so?.Phone ?? "",
                SoDonHang = string.IsNullOrWhiteSpace(so?.OrderNo) ? "—" : so!.OrderNo!,
                SoLenhSx = po.OrderNo ?? "",
                SoHoaDon = invLabel,
                SanPham = po.ProductName ?? "",
                SoLuong = po.Quantity,
                NoiDungSanXuat = lines,
                GhiChu = string.IsNullOrWhiteSpace(po.Notes) ? "" : po.Notes.Trim(),
                NguoiBanGiao = (HandoverNguoiBanGiao ?? "").Trim()
            };
        }

        private static DateTime? LatestMovement(int materialId, Dictionary<int, DateTime> impMax, Dictionary<int, DateTime> expMax)
        {
            bool hi = impMax.TryGetValue(materialId, out var di);
            bool he = expMax.TryGetValue(materialId, out var de);
            if (!hi && !he) return null;
            if (!hi) return de;
            if (!he) return di;
            return di >= de ? di : de;
        }

        // ===== PHƯƠNG THỨC CHO NHẬP KHO SẢN XUẤT =====

        private void SaveProductionImport()
        {
            if (ProdImportSelectedOrder == null)
            {
                MessageBox.Show("Vui lòng chọn lệnh sản xuất!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProdImportQuantity <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!", "Lỗi");
                return;
            }

            using (var db = new AppDbContext())
            {
                // Kiểm tra lệnh sản xuất tồn tại
                var po = db.ProductionOrders.Find(ProdImportSelectedOrder.Id);
                if (po == null)
                {
                    MessageBox.Show("Lệnh sản xuất không tồn tại!", "Lỗi");
                    return;
                }

                if (po.Status != "Đang SX")
                {
                    MessageBox.Show($"Lệnh {po.OrderNo} chưa ở trạng thái 'Đang SX'!", "Lỗi");
                    return;
                }

                // Kiểm tra xem lệnh này đã được nhập kho chưa
                bool alreadyImported = db.ProductionImports.Any(pi => pi.ProductionOrderId == po.Id && pi.Status != "Đã xuất");
                if (alreadyImported)
                {
                    MessageBox.Show($"Lệnh {po.OrderNo} đã được nhập kho trước đó!", "Lỗi");
                    return;
                }

                // Tạo phiếu nhập kho sản xuất
                var import = new ProductionImport
                {
                    TicketNo = ProdImportTicketNo,
                    ImportDate = ProdImportDate,
                    ProductionOrderId = po.Id,
                    SalesOrderId = po.SalesOrderId,
                    ProductName = po.ProductName,
                    CustomerName = po.CustomerName,
                    Quantity = ProdImportQuantity,
                    Unit = ProdImportUnit,
                    UnitPrice = 0,
                    TotalPrice = 0,
                    UserId = AppSession.CurrentUser.Id,
                    Notes = ProdImportNotes ?? "",
                    Status = "Chưa xuất"
                };

                db.ProductionImports.Add(import);

                // Cập nhật trạng thái lệnh sản xuất thành "Hoàn thành"
                po.Status = "Hoàn thành";

                db.SaveChanges();

                AddLog($"✅ Hoàn thành lệnh {po.OrderNo}. Đã nhập kho sản phẩm với số phiếu {ProdImportTicketNo}.");
                MessageBox.Show($"Hoàn thành sản xuất thành công! Phiếu nhập: {ProdImportTicketNo}", "Thành công");

                // Reset form
                ProdImportTicketNo = "NK-SX-" + DateTime.Now.ToString("yyMMddHHmm");
                ProdImportSelectedOrder = null;
                ProdImportProductName = "";
                ProdImportCustomerName = "";
                ProdImportQuantity = 0;
                ProdImportUnit = "Cái";
                ProdImportNotes = "";
                OnPropertyChanged(nameof(ProdImportTicketNo));
                OnPropertyChanged(nameof(ProdImportSelectedOrder));

                LoadData();
            }
        }

        /// <summary>
        /// Kiểm tra có thể xuất sản phẩm hoàn thành bán hàng không
        /// </summary>
        private bool CanExportProductionToSales(ProductionImport pi)
        {
            return pi != null && pi.Status == "Chưa xuất";
        }

        /// <summary>
        /// Xuất sản phẩm hoàn thành để bán hàng - tự động lấy đơn hàng mới nhất chưa giao
        /// </summary>
        private void ExportProductionToSales(ProductionImport pi)
        {
            if (pi == null) return;

            using (var db = new AppDbContext())
            {
                // Tìm đơn hàng bán hàng mới nhất chưa giao (Chưa giao hoặc Đang sản xuất)
                var targetSalesOrder = db.SalesOrders
                    .Where(so => so.Status == "Chưa giao" || so.Status == "Đang sản xuất")
                    .OrderByDescending(so => so.Id)
                    .FirstOrDefault();

                if (targetSalesOrder == null)
                {
                    MessageBox.Show("Không tìm thấy đơn hàng bán hàng nào (Chưa giao/Đang sản xuất) để xuất kho!", "Cảnh báo");
                    return;
                }

                // Tạo phiếu xuất kho bán hàng
                var exportTicketNo = "XK-BH-" + DateTime.Now.ToString("yyMMddHHmm");
                var exportTx = new ExportTransaction
                {
                    TicketNo = exportTicketNo,
                    ExportDate = DateTime.Now,
                    Purpose = "Bán hàng",
                    MaterialId = (int?)null,
                    Quantity = pi.Quantity,
                    Notes = $"Xuất bán hàng từ lệnh {(pi.ProductionOrder?.OrderNo ?? "N/A")}. Đơn hàng: {targetSalesOrder.OrderNo}",
                    UserId = AppSession.CurrentUser?.Id ?? 1,
                    ProductionOrderId = pi.ProductionOrderId,
                    QuotationId = targetSalesOrder.QuotationId
                };
                db.ExportTransactions.Add(exportTx);

                // Cập nhật trạng thái sản phẩm hoàn thành
                pi.Status = "Đã xuất";
                pi.ExportDate = DateTime.Now;
                pi.ExportTicketNo = exportTicketNo;
                pi.SalesOrderId = targetSalesOrder.Id;

                // Cập nhật trạng thái đơn hàng bán hàng
                var so = db.SalesOrders.Find(targetSalesOrder.Id);
                if (so != null)
                {
                    so.Status = "Đã giao";
                }

                // === TẠO HÓA ĐƠN TỰ ĐỘNG ===
                var invoice = new Invoice
                {
                    InvoiceNo = "HD-" + DateTime.Now.ToString("yyMMddHHmm"),
                    InvoiceDate = DateTime.Now,
                    SalesOrderId = targetSalesOrder.Id,
                    CustomerId = targetSalesOrder.Id,
                    CustomerName = targetSalesOrder.CustomerName ?? "",
                    Phone = targetSalesOrder.Phone ?? "",
                    Address = string.IsNullOrWhiteSpace(targetSalesOrder.Address) ? "Chưa nhập địa chỉ" : targetSalesOrder.Address,
                    TaxCode = targetSalesOrder.TaxCode ?? "",
                    TotalAmount = targetSalesOrder.TotalAmount,
                    SubTotal = targetSalesOrder.TotalAmount,
                    VATPercent = 0.08,
                    VATAmount = targetSalesOrder.TotalAmount * 0.08,
                    GrandTotal = targetSalesOrder.TotalAmount * 1.08,
                    RefOrderNo = targetSalesOrder.OrderNo ?? "",
                    TemplateNo = "1/001",
                    Symbol = "KT/26E",
                    UserId = AppSession.CurrentUser?.Id ?? 1,
                    DueDate = DateTime.Now.AddDays(30),
                    Status = "Chưa thu",
                    Notes = $"Tạo tự động từ đơn hàng {targetSalesOrder.OrderNo}",
                    TK_No = targetSalesOrder.TK_No ?? "131",
                    TK_Co = targetSalesOrder.TK_Co ?? "511",
                    AccountType = "Công nợ (131)"
                };
                db.Invoices.Add(invoice);
                // === KẾT THÚC TẠO HÓA ĐƠN ===

                db.SaveChanges();

                AddLog($"📤 Xuất bán hàng thành công! Sản phẩm từ lệnh {pi.ProductionOrder?.OrderNo} đã xuất cho đơn hàng {targetSalesOrder.OrderNo}. Phiếu xuất: {exportTicketNo}. Hoá đơn: {invoice.InvoiceNo}");
                MessageBox.Show($"Đã xuất kho bán hàng thành công!\nPhiếu xuất: {exportTicketNo}\nHoá đơn: {invoice.InvoiceNo}", "Thành công");

                LoadData();
            }
        }

        private void LoadProductionImportForExport(ProductionImport pi)
        {
            // Auto-load thông tin khi chọn sản phẩm hoàn thành
        }

        // ===== PHƯƠNG THỨC CHO NHẬP KHO SẢN XUẤT =====

        /// <summary>
        /// Tự động điền thông tin khi chọn lệnh sản xuất để nhập kho
        /// </summary>
        private void OnProdImportSelectedOrderChanged()
        {
            if (ProdImportSelectedOrder == null) return;

            ProdImportProductName = ProdImportSelectedOrder.ProductName;
            ProdImportCustomerName = ProdImportSelectedOrder.CustomerName;
            ProdImportQuantity = ProdImportSelectedOrder.Quantity;
            ProdImportUnit = "Cái"; // Mặc định
            OnPropertyChanged(nameof(ProdImportProductName));
            OnPropertyChanged(nameof(ProdImportCustomerName));
            OnPropertyChanged(nameof(ProdImportQuantity));
            OnPropertyChanged(nameof(ProdImportUnit));
        }

    }
}