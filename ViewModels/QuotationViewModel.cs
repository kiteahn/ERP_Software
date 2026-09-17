using PhanMemInAnERP.Data;
using PhanMemInAnERP.Models;
using PhanMemInAnERP.Services;
using System;
using System.Globalization;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using PhanMemInAnERP.Helpers;

namespace PhanMemInAnERP.ViewModels
{
    public class QuotationViewModel : BaseViewModel
    {
        /// <summary>Id ảo — không tồn tại trong DB, dùng cho mục "Không chọn giấy".</summary>
        private const int NoPaperMaterialId = -1;

        /// <summary>Giá giấy mặc định (VNĐ/tấn) — khi chưa có giá kho hoặc dữ liệu = 0.</summary>
        public const double DefaultPaperPricePerTon = 0;

        private static Material CreateNoPaperMaterial() => new Material
        {
            Id = NoPaperMaterialId,
            Code = "__NONE__",
            Name = "(Không chọn)",
            Category = "Giấy",
            Unit = "",
            Notes = "",
            AveragePrice = 0,
            MinStock = 0,
            StockQuantity = 0
        };

        private string _customerName = "TỔNG CÔNG TY CỔ PHẦN MAY VIỆT TIẾN";
        public string CustomerName { get => _customerName; set { _customerName = value; OnPropertyChanged(); } }

        private string _productName = "Túi Việt Tiến Lớn";
        public string ProductName { get => _productName; set { _productName = value; OnPropertyChanged(); } }

        private string _productDimensions = "39x27x9";
        public string ProductDimensions
        {
            get => _productDimensions;
            set { _productDimensions = value ?? ""; OnPropertyChanged(); UpdatePrintSizeMismatchWarning(); }
        }

        /// <summary>Cảnh báo khi DxR sản phẩm (mục 1) khác Khổ in (mục 2) — tiền giấy/cán màng tính theo Khổ in.</summary>
        private string _printSizeMismatchWarning = "";
        public string PrintSizeMismatchWarning => _printSizeMismatchWarning;
        public bool HasPrintSizeMismatch => !string.IsNullOrWhiteSpace(_printSizeMismatchWarning);

        private string _quoteNo = "BG-" + DateTime.Now.ToString("yyMMddHHmm");
        public string QuoteNo { get => _quoteNo; set { _quoteNo = value; OnPropertyChanged(); } }

        private DateTime _quotationDate = DateTime.Now.Date;
        public DateTime QuotationDate { get => _quotationDate; set { _quotationDate = value; OnPropertyChanged(); } }

        private int _validityDays = 15;
        public int ValidityDays { get => _validityDays; set { _validityDays = value; OnPropertyChanged(); } }

        private int _deliveryDays = 7;
        public int DeliveryDays { get => _deliveryDays; set { _deliveryDays = value; OnPropertyChanged(); } }

        private string _customerAddress = "";
        public string CustomerAddress
        {
            get => _customerAddress;
            set { _customerAddress = value; OnPropertyChanged(); }
        }

        /// <summary>Mã số thuế khách hàng — tự điền khi chọn khách hoặc nhập thủ công.</summary>
        private string _customerTaxCode = "";
        public string CustomerTaxCode
        {
            get => _customerTaxCode;
            set { _customerTaxCode = value ?? ""; OnPropertyChanged(); }
        }

        public ObservableCollection<Material> PaperMaterials { get; set; }

        /// <summary>Text nhập tay để chọn giấy — gợi ý khi gõ.</summary>
        private string _paperNameText = "";
        public string PaperNameText
        {
            get => _paperNameText;
            set
            {
                if (_paperNameText == value) return;
                _paperNameText = value ?? "";
                OnPropertyChanged();
                BuildPaperSuggestions();
                // Nếu khớp chính xác một loại giấy → chọn luôn
                var exactMatch = PaperMaterials.FirstOrDefault(p =>
                    p.Id != NoPaperMaterialId &&
                    (p.Name ?? "").Equals(_paperNameText, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                    SelectedPaper = exactMatch;
            }
        }

        /// <summary>Danh sách gợi ý cho TextBox chọn giấy (lọc theo text nhập).</summary>
        public ObservableCollection<string> PaperSuggestions { get; } = new ObservableCollection<string>();

        private void BuildPaperSuggestions()
        {
            PaperSuggestions.Clear();
            var filter = (_paperNameText ?? "").Trim().ToLowerInvariant();
            foreach (var p in PaperMaterials.Where(p => p.Id != NoPaperMaterialId))
            {
                if (string.IsNullOrEmpty(filter) || (p.Name ?? "").ToLowerInvariant().Contains(filter))
                    PaperSuggestions.Add(p.Name ?? "");
            }
            OnPropertyChanged(nameof(PaperSuggestions));
        }

        /// <summary>Danh sách tất cả giấy cho ComboBox hiển thị đầy đủ khi click.</summary>
        public ObservableCollection<Material> PaperMaterialsForSelection { get; set; }

        private Material _selectedPaper;
        public Material SelectedPaper
        {
            get => _selectedPaper;
            set
            {
                _selectedPaper = value;
                OnPropertyChanged();

                // Đồng bộ PaperNameText khi chọn từ ComboBox
                PaperNameText = _selectedPaper?.Name ?? "";

                if (_selectedPaper != null && _selectedPaper.Id != NoPaperMaterialId)
                {
                    var ap = _selectedPaper.AveragePrice;
                    PaperPricePerTon = ap > 0 ? ap : DefaultPaperPricePerTon;

                    var match = Regex.Match(_selectedPaper.Name, @"(\d+)\s*gsm", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        PaperGsm = double.Parse(match.Groups[1].Value);
                    }
                }
                else
                {
                    // "(Không chọn)" hoặc null: giá xuất kho về 0 (người dùng nhập tay hoặc chọn giấy lại sau).
                    PaperPricePerTon = DefaultPaperPricePerTon;
                }
            }
        }


        /// <summary>Mỗi lần bấm nút + bên số lượng — cộng thêm bấy nhiêu cái.</summary>
        public const int QuantityPlusStep = 500;

        /// <summary>Quy ước 1 thùng = bấy nhiêu cái (dùng tính số thùng = ceil(SL / PcsPerBox)).</summary>
        public const int PcsPerBox = 500;

        public ObservableCollection<QuantityListItem> QuantityChoices { get; } = new ObservableCollection<QuantityListItem>();

        private static void FillDefaultQuantityChoices(ObservableCollection<QuantityListItem> list)
        {
            foreach (var n in new[] { 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000 })
                list.Add(new QuantityListItem(n));
        }

        private int _quantity = 20000;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var v = value < 1 ? 1 : value;
                if (_quantity == v) return;
                _quantity = v;
                ClearMatrixSidebarSelection();
                EnsureQuantityInChoices();
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(QuantityChoices)); // bắt buộc ComboBox refresh lại ItemsSource
                CalculatePrice();
            }
        }

        private void EnsureQuantityInChoices()
        {
            if (QuantityChoices.Any(q => q.Value == _quantity)) return;
            QuantityChoices.Add(new QuantityListItem(_quantity));
        }

        private void IncrementQuantity()
        {
            Quantity = _quantity + QuantityPlusStep;
        }

        private int _soCon = 1;
        public int SoCon { get => _soCon; set { _soCon = value; OnPropertyChanged(); CalculatePrice(); } }

        private int _buHao = 500;
        public int BuHao { get => _buHao; set { _buHao = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _printLength = 79;
        public double PrintLength { get => _printLength; set { _printLength = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _printWidth = 51;
        public double PrintWidth { get => _printWidth; set { _printWidth = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _paperGsm = 250;
        public double PaperGsm { get => _paperGsm; set { _paperGsm = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _paperPricePerTon = 0;
        public double PaperPricePerTon { get => _paperPricePerTon; set { _paperPricePerTon = value; OnPropertyChanged(); CalculatePrice(); } }

        private int _colorCount = 6;
        public int ColorCount { get => _colorCount; set { _colorCount = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _platePricePerColorLarge;
        public double PlatePricePerColorLarge
        {
            get => _platePricePerColorLarge;
            set
            {
                if (Math.Abs(_platePricePerColorLarge - value) < 1e-9) return;
                _platePricePerColorLarge = value;
                OnPropertyChanged();
                if (_isLargeMachine)
                    OnPropertyChanged(nameof(SelectedPlatePricePerColor));
                CalculatePrice();
            }
        }

        private double _platePricePerColorSmall;
        public double PlatePricePerColorSmall
        {
            get => _platePricePerColorSmall;
            set
            {
                if (Math.Abs(_platePricePerColorSmall - value) < 1e-9) return;
                _platePricePerColorSmall = value;
                OnPropertyChanged();
                if (!_isLargeMachine)
                    OnPropertyChanged(nameof(SelectedPlatePricePerColor));
                CalculatePrice();
            }
        }

        /// <summary>Chọn một trong hai: máy lớn (100k/kẽm/màu mặc định) hoặc máy nhỏ (60k) — tổng kẽm = số màu × đơn giá đang chọn.</summary>
        private bool _isLargeMachine = true;
        public bool IsLargeMachine
        {
            get => _isLargeMachine;
            set
            {
                if (_isLargeMachine == value) return;
                _isLargeMachine = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPlatePricePerColor));
                CalculatePrice();
            }
        }

        /// <summary>Đơn giá kẽm/màu của loại máy đang chọn — bind ô nhập chính.</summary>
        public double SelectedPlatePricePerColor
        {
            get => _isLargeMachine ? _platePricePerColorLarge : _platePricePerColorSmall;
            set
            {
                if (_isLargeMachine)
                    PlatePricePerColorLarge = value;
                else
                    PlatePricePerColorSmall = value;
            }
        }

        /// <summary>Đơn giá cán màng m² (VNĐ) — bảng mặc định khi đổi &quot;Loại màng cán&quot;.</summary>
        public const double DefaultLaminationPriceGlossy = 1900;
        public const double DefaultLaminationPriceMatte = 2000;

        private double _laminationPrice = DefaultLaminationPriceGlossy;
        public double LaminationPrice { get => _laminationPrice; set { _laminationPrice = value; OnPropertyChanged(); CalculatePrice(); } }

        private int _laminationSides = 1;
        public int LaminationSides { get => _laminationSides; set { _laminationSides = value; OnPropertyChanged(); CalculatePrice(); } }

        private string _laminationType = "Bóng";
        public string LaminationType
        {
            get => _laminationType;
            set
            {
                var v = string.IsNullOrWhiteSpace(value) ? "Bóng" : value.Trim();
                if (_laminationType == v) return;
                _laminationType = v;
                OnPropertyChanged();
                if (!_suppressLaminationAutoPrice)
                    LaminationPrice = GetDefaultLaminationUnitPrice(v);
                else
                    CalculatePrice();
            }
        }

        /// <summary>Khi nạp báo giá từ lịch sử: giữ đơn giá đã lưu, không ghi đè theo loại màng.</summary>
        private bool _suppressLaminationAutoPrice;

        public ObservableCollection<string> LaminationTypeOptions { get; } = new ObservableCollection<string> { "Bóng", "Mờ", "Không cán" };

        private static double GetDefaultLaminationUnitPrice(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return DefaultLaminationPriceGlossy;
            if (type.StartsWith("Không", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(type, "Mờ", StringComparison.OrdinalIgnoreCase)) return DefaultLaminationPriceMatte;
            if (string.Equals(type, "Bóng", StringComparison.OrdinalIgnoreCase)) return DefaultLaminationPriceGlossy;
            return DefaultLaminationPriceGlossy;
        }

        private static bool IsNoLamination(string? type) =>
            !string.IsNullOrWhiteSpace(type) && type.Trim().StartsWith("Không", StringComparison.OrdinalIgnoreCase);

        private double _dieCutMoldPrice = 400000;
        public double DieCutMoldPrice { get => _dieCutMoldPrice; set { _dieCutMoldPrice = value; OnPropertyChanged(); CalculatePrice(); } }

        /// <summary>Đơn giá dây mặc định (đ/cái) — theo bảng giá chuẩn (vd 550đ × SL).</summary>
        public const double DefaultStringPricePerItem = 550;

        /// <summary>Đơn giá nút mặc định (đ/cái).</summary>
        public const double DefaultButtonPricePerItem = 360;

        /// <summary>Giá 1 thùng (500 cái/thùng) mặc định — vd 40 thùng × 20.000 = 800.000 cho 20k cái.</summary>
        public const double DefaultBoxPrice = 20000;

        /// <summary>Tiền in proof (in thử) mặc định một lần/đơn.</summary>
        public const double DefaultPrintProofFee = 100000;

        private double _stringPricePerItem = DefaultStringPricePerItem;
        public double StringPricePerItem
        {
            get => _stringPricePerItem;
            set
            {
                _stringPricePerItem = value;
                OnPropertyChanged();
                CalculatePrice();
            }
        }

        private double _buttonPricePerItem = DefaultButtonPricePerItem;
        public double ButtonPricePerItem
        {
            get => _buttonPricePerItem;
            set
            {
                _buttonPricePerItem = value;
                OnPropertyChanged();
                CalculatePrice();
            }
        }

        /// <summary>Tổng tiền dây — TextBox bind chuỗi đã đồng bộ (có PropertyChanged khi đổi SL).</summary>
        private string _stringCostTotalText = "";
        public string StringCostTotalText
        {
            get => _stringCostTotalText;
            set
            {
                if (value == _stringCostTotalText) return;
                _stringCostTotalText = value ?? "0";
                OnPropertyChanged();
                if (!TryParseVnMoney(value, out double total)) return;
                if (_quantity <= 0) return;
                double newUnit = total / _quantity;
                if (Math.Abs(newUnit - _stringPricePerItem) < 1e-9) return;
                _stringPricePerItem = newUnit;
                OnPropertyChanged(nameof(StringPricePerItem));
                CalculatePrice();
            }
        }

        /// <summary>Tổng tiền nút — TextBox bind chuỗi đã đồng bộ (có PropertyChanged khi đổi SL).</summary>
        private string _buttonCostTotalText = "";
        public string ButtonCostTotalText
        {
            get => _buttonCostTotalText;
            set
            {
                if (value == _buttonCostTotalText) return;
                _buttonCostTotalText = value ?? "0";
                OnPropertyChanged();
                if (!TryParseVnMoney(value, out double total)) return;
                if (_quantity <= 0) return;
                double newUnit = total / _quantity;
                if (Math.Abs(newUnit - _buttonPricePerItem) < 1e-9) return;
                _buttonPricePerItem = newUnit;
                OnPropertyChanged(nameof(ButtonPricePerItem));
                CalculatePrice();
            }
        }

        /// <summary>Tổng tiền thùng = ceil(SL/PcsPerBox)×giá 1 thùng — tự cập nhật khi đổi SL; sửa ô → đổi giá 1 thùng (BoxPrice).</summary>
        private string _boxCostTotalText = "";
        public string BoxCostTotalText
        {
            get => _boxCostTotalText;
            set
            {
                if (value == _boxCostTotalText) return;
                _boxCostTotalText = value ?? "0";
                OnPropertyChanged();
                if (!TryParseVnMoney(value, out double total)) return;
                int boxes = BoxCountForQuantity(_quantity);
                if (boxes <= 0) return;
                double newUnit = total / boxes;
                if (Math.Abs(newUnit - _boxPrice) < 1e-9) return;
                _boxPrice = newUnit;
                OnPropertyChanged(nameof(BoxPrice));
                CalculatePrice();
            }
        }

        private static int BoxCountForQuantity(int q)
        {
            if (q <= 0) return 0;
            return (int)Math.Ceiling(q / (double)PcsPerBox);
        }

        /// <summary>Cập nhật ô tổng dây/nút/thùng + báo PropertyChanged — gọi mỗi lần tính giá / đổi SL.</summary>
        private void SyncWireButtonTotalDisplayStrings()
        {
            int q = _quantity < 0 ? 0 : _quantity;
            _stringCostTotalText = FormatVnMoney0(_stringPricePerItem * q);
            _buttonCostTotalText = FormatVnMoney0(_buttonPricePerItem * q);
            int boxes = BoxCountForQuantity(q);
            _boxCostTotalText = FormatVnMoney0(boxes * _boxPrice);
            OnPropertyChanged(nameof(StringCostTotalText));
            OnPropertyChanged(nameof(ButtonCostTotalText));
            OnPropertyChanged(nameof(BoxCostTotalText));
        }

        private static readonly CultureInfo ViMoneyCulture = new CultureInfo("vi-VN");

        private static string FormatVnMoney0(double x) =>
            Math.Round(x, 0, MidpointRounding.AwayFromZero).ToString("N0", ViMoneyCulture);

        private static bool TryParseVnMoney(string? s, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return true;
            var t = new string(s.Trim().Where(ch => !char.IsWhiteSpace(ch) && ch != '\u00a0').ToArray());
            if (t.Length == 0) return true;
            t = t.Replace(".", "");
            return double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private double _boxPrice = DefaultBoxPrice;
        public double BoxPrice { get => _boxPrice; set { _boxPrice = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _deliveryFee = 500000;
        public double DeliveryFee { get => _deliveryFee; set { _deliveryFee = value; OnPropertyChanged(); CalculatePrice(); } }

        private double _printProofFee = DefaultPrintProofFee;
        public double PrintProofFee
        {
            get => _printProofFee;
            set
            {
                if (Math.Abs(_printProofFee - value) < 1e-9) return;
                _printProofFee = value < 0 ? 0 : value;
                OnPropertyChanged();
                CalculatePrice();
            }
        }

        private double _profitMargin = 20; // % Lợi nhuận
        public double ProfitMargin { get => _profitMargin; set { _profitMargin = value; OnPropertyChanged(); CalculatePrice(); } }

       
        private double _paperCost; public double PaperCost { get => _paperCost; set { _paperCost = value; OnPropertyChanged(); } }

        /// <summary>Chi tiết tính tiền giấy (theo SL đang xem ở cột phải — form hoặc dòng bảng SL).</summary>
        private string _paperCostBreakdownText = "";
        public string PaperCostBreakdownText
        {
            get => _paperCostBreakdownText;
            set { _paperCostBreakdownText = value ?? ""; OnPropertyChanged(); }
        }
        private double _plateCost; public double PlateCost { get => _plateCost; set { _plateCost = value; OnPropertyChanged(); } }
        private double _printCost; public double PrintCost { get => _printCost; set { _printCost = value; OnPropertyChanged(); } }
        private double _laminationCost; public double LaminationCost { get => _laminationCost; set { _laminationCost = value; OnPropertyChanged(); } }
        private double _dieCutCost; public double DieCutCost { get => _dieCutCost; set { _dieCutCost = value; OnPropertyChanged(); } }
        private double _gluingCost; public double GluingCost { get => _gluingCost; set { _gluingCost = value; OnPropertyChanged(); } }
        private double _otherCostTotal; public double OtherCostTotal { get => _otherCostTotal; set { _otherCostTotal = value; OnPropertyChanged(); } }

        /// <summary>Dây, nút, thùng, xe, khuôn, phát sinh (không gồm tiền in proof).</summary>
        private double _otherCostExcludingProof; public double OtherCostExcludingProof { get => _otherCostExcludingProof; set { _otherCostExcludingProof = value; OnPropertyChanged(); } }

        private double _totalProductionCost; public double TotalProductionCost { get => _totalProductionCost; set { _totalProductionCost = value; OnPropertyChanged(); } }
        private double _quotedUnitPrice; public double QuotedUnitPrice { get => _quotedUnitPrice; set { _quotedUnitPrice = value; OnPropertyChanged(); } }
        private double _totalOrderValue; public double TotalOrderValue { get => _totalOrderValue; set { _totalOrderValue = value; OnPropertyChanged(); } }

        public ObservableCollection<QuoteExtraCost> ExtraCosts { get; set; } = new ObservableCollection<QuoteExtraCost>();
        public ObservableCollection<PriceTier> PriceMatrix { get; set; } = new ObservableCollection<PriceTier>();

        /// <summary>SL dùng cho bảng chi tiết bên phải khi chọn dòng bảng giá theo số lượng; null = theo SL form.</summary>
        private int? _sidebarQuantityOverride;

        private bool _restoringMatrixSelection;
        private PriceTier _selectedPriceMatrixRow;

        public PriceTier SelectedPriceMatrixRow
        {
            get => _selectedPriceMatrixRow;
            set
            {
                if (ReferenceEquals(_selectedPriceMatrixRow, value)) return;
                _selectedPriceMatrixRow = value;
                OnPropertyChanged();
                if (_restoringMatrixSelection) return;
                _sidebarQuantityOverride = value?.Quantity;
                NotifySidebarCostHint();
                ApplySidebarCosts();
            }
        }

        /// <summary>Gợi ý dưới tiêu đề bảng chi tiết — theo SL form hay dòng đã chọn.</summary>
        public string SidebarCostQuantityHint
        {
            get
            {
                if (_sidebarQuantityOverride.HasValue)
                    return $"Đang xem chi tiết theo {string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", _sidebarQuantityOverride.Value)} cái (từ bảng giá theo SL).";
                return $"Theo số lượng trên form: {string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", Quantity)} cái.";
            }
        }

        private void NotifySidebarCostHint() => OnPropertyChanged(nameof(SidebarCostQuantityHint));

        private void ClearMatrixSidebarSelection()
        {
            _sidebarQuantityOverride = null;
            if (_selectedPriceMatrixRow != null)
            {
                _restoringMatrixSelection = true;
                try
                {
                    _selectedPriceMatrixRow = null;
                    OnPropertyChanged(nameof(SelectedPriceMatrixRow));
                }
                finally
                {
                    _restoringMatrixSelection = false;
                }
            }
            NotifySidebarCostHint();
        }

        // --- CUSTOMER MANAGEMENT ---
        public ObservableCollection<Customer> AvailableCustomers { get; set; } = new ObservableCollection<Customer>();
        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                if (_selectedCustomer != null)
                {
                    CustomerName = _selectedCustomer.Name;
                    CustomerAddress = _selectedCustomer.Address ?? "";
                    CustomerTaxCode = _selectedCustomer.TaxCode ?? "";
                }
            }
        }

        private bool _isAddCustomerPopupOpen;
        public bool IsAddCustomerPopupOpen { get => _isAddCustomerPopupOpen; set { _isAddCustomerPopupOpen = value; OnPropertyChanged(); } }

        private string _newCustomerName; public string NewCustomerName { get => _newCustomerName; set { _newCustomerName = value; OnPropertyChanged(); } }
        private string _newCustomerPhone; public string NewCustomerPhone { get => _newCustomerPhone; set { _newCustomerPhone = value; OnPropertyChanged(); } }
        private string _newCustomerAddress; public string NewCustomerAddress { get => _newCustomerAddress; set { _newCustomerAddress = value; OnPropertyChanged(); } }
        private string _newCustomerTaxCode; public string NewCustomerTaxCode { get => _newCustomerTaxCode; set { _newCustomerTaxCode = value; OnPropertyChanged(); } }

        public ICommand ShowAddCustomerCommand { get; }
        public ICommand SaveNewCustomerCommand { get; }
        public ICommand CancelAddCustomerCommand { get; }

        private bool _isAddPaperPopupOpen;
        public bool IsAddPaperPopupOpen { get => _isAddPaperPopupOpen; set { _isAddPaperPopupOpen = value; OnPropertyChanged(); } }

        private string _newPaperName = "";
        public string NewPaperName { get => _newPaperName; set { _newPaperName = value ?? ""; OnPropertyChanged(); } }

        private double _newPaperGsm = 250;
        public double NewPaperGsm { get => _newPaperGsm; set { _newPaperGsm = value; OnPropertyChanged(); } }

        private double _newPaperExportPrice;
        public double NewPaperExportPrice { get => _newPaperExportPrice; set { _newPaperExportPrice = value; OnPropertyChanged(); } }

        public ICommand ShowAddPaperCommand { get; }
        public ICommand SaveNewPaperCommand { get; }
        public ICommand CancelAddPaperCommand { get; }
        public ICommand DeletePaperCommand { get; }

        public ICommand AddExtraCostCommand { get; }
        public ICommand RemoveExtraCostCommand { get; }
        public ICommand RecalculateCommand { get; }

        public ICommand SaveQuoteCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand IncrementQuantityCommand { get; }
        public ICommand EditPlatePricesCommand { get; }
        public ICommand ShowCostDetailCommand { get; }

        public QuotationViewModel()
        {
            AddExtraCostCommand = new RelayCommand(AddExtraCost);
            RemoveExtraCostCommand = new RelayCommand<QuoteExtraCost>(RemoveExtraCost);
            RecalculateCommand = new RelayCommand(CalculatePrice);
            SaveQuoteCommand = new RelayCommand(SaveQuote);
            ExportExcelCommand = new RelayCommand(ExportExcel);
            ExportPdfCommand = new RelayCommand(ExportPdf);
            IncrementQuantityCommand = new RelayCommand(IncrementQuantity);
            EditPlatePricesCommand = new RelayCommand(OpenEditPlatePricesDialog);
            ShowCostDetailCommand = new RelayCommand(ShowCostDetail);

            ShowAddCustomerCommand = new RelayCommand(() => IsAddCustomerPopupOpen = true);
            CancelAddCustomerCommand = new RelayCommand(() => IsAddCustomerPopupOpen = false);
            SaveNewCustomerCommand = new RelayCommand(SaveNewCustomer);

            ShowAddPaperCommand = new RelayCommand(OpenAddPaperPopup);
            CancelAddPaperCommand = new RelayCommand(() => IsAddPaperPopupOpen = false);
            SaveNewPaperCommand = new RelayCommand(SaveNewPaper);
            DeletePaperCommand = new RelayCommand<Material>(DeletePaper);

            ExtraCosts.CollectionChanged += ExtraCosts_CollectionChanged;

            FillDefaultQuantityChoices(QuantityChoices);

            var (lg, sm) = PlatePriceDefaultsStore.Load();
            _platePricePerColorLarge = lg;
            _platePricePerColorSmall = sm;
            OnPropertyChanged(nameof(PlatePricePerColorLarge));
            OnPropertyChanged(nameof(PlatePricePerColorSmall));
            OnPropertyChanged(nameof(SelectedPlatePricePerColor));

            CalculatePrice(); // Tính ngay khi mở form
            LoadCustomers();

            RefreshPaperMaterialsFromDb();
            SelectedPaper = PaperMaterials.Count > 0 ? PaperMaterials[0] : null;
            if (_paperPricePerTon <= 0)
                PaperPricePerTon = DefaultPaperPricePerTon;

            // ComboBox SelectedValue cần refresh sau khi đã có QuantityChoices
            OnPropertyChanged(nameof(Quantity));
        }

        private void LoadCustomers()
        {
            using (var db = new AppDbContext())
            {
                var customerService = new CustomerService(db);
                var list = customerService.GetDanhSachChonKhach();
                AvailableCustomers.Clear();
                foreach (var c in list) AvailableCustomers.Add(c);
            }
        }

        private void RefreshPaperMaterialsFromDb(int? selectMaterialId = null)
        {
            using (var db = new AppDbContext())
            {
                PaperMaterials = new ObservableCollection<Material> { CreateNoPaperMaterial() };
                PaperMaterialsForSelection = new ObservableCollection<Material>();
                foreach (var m in db.Materials.Where(x => x.Category == "Giấy").OrderBy(x => x.Name))
                {
                    PaperMaterials.Add(m);
                    PaperMaterialsForSelection.Add(m);
                }
            }

            OnPropertyChanged(nameof(PaperMaterials));
            OnPropertyChanged(nameof(PaperMaterialsForSelection));
            BuildPaperSuggestions();

            if (!selectMaterialId.HasValue)
                return;
            var pick = PaperMaterials.FirstOrDefault(x => x.Id == selectMaterialId.Value);
            if (pick != null)
                SelectedPaper = pick;
        }

        private void OpenAddPaperPopup()
        {
            NewPaperName = "";
            NewPaperGsm = PaperGsm > 0 ? PaperGsm : 250;
            NewPaperExportPrice = PaperPricePerTon >= 0 ? PaperPricePerTon : 0;
            IsAddPaperPopupOpen = true;
        }

        private static string BuildPaperMaterialDisplayName(string baseName, double gsm)
        {
            var t = (baseName ?? "").Trim();
            if (t.Length == 0)
                return "";
            if (Regex.IsMatch(t, @"\d+\s*gsm", RegexOptions.IgnoreCase))
                return t;
            var g = Math.Round(gsm, 0, MidpointRounding.AwayFromZero);
            return $"{t} {g}gsm";
        }

        private void SaveNewPaper()
        {
            if (string.IsNullOrWhiteSpace(NewPaperName))
            {
                MessageBox.Show("Vui lòng nhập tên giấy!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPaperGsm <= 0)
            {
                MessageBox.Show("Định lượng (gsm) phải lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var displayName = BuildPaperMaterialDisplayName(NewPaperName, NewPaperGsm);
            var code = "GJ-" + DateTime.Now.ToString("yyMMddHHmmss");

            using (var db = new AppDbContext())
            {
                if (db.Materials.Any(m => m.Category == "Giấy" && m.Name == displayName))
                {
                    MessageBox.Show($"Đã có loại giấy \"{displayName}\" trong kho.", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var mat = new Material
                {
                    Code = code,
                    Name = displayName,
                    Unit = "Kg",
                    Category = "Giấy",
                    Notes = "",
                    AveragePrice = NewPaperExportPrice >= 0 ? NewPaperExportPrice : 0,
                    MinStock = 0,
                    StockQuantity = 0
                };
                db.Materials.Add(mat);
                db.SaveChanges();
                var newId = mat.Id;
                IsAddPaperPopupOpen = false;
                NewPaperName = "";
                RefreshPaperMaterialsFromDb(newId);
            }
        }

        private void DeletePaper(Material paper)
        {
            if (paper == null || paper.Id == NoPaperMaterialId) return;

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa loại giấy \"{paper.Name}\"?\n\nLưu ý: Nếu giấy này đã được sử dụng trong báo giá hoặc phiếu nhập kho, việc xóa có thể ảnh hưởng đến dữ liệu cũ.",
                "Xác nhận xóa giấy",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            using (var db = new AppDbContext())
            {
                var mat = db.Materials.Find(paper.Id);
                if (mat != null)
                {
                    db.Materials.Remove(mat);
                    db.SaveChanges();
                }
            }

            // Nếu đang chọn giấy đó → reset về "Không chọn"
            if (SelectedPaper?.Id == paper.Id)
                SelectedPaper = PaperMaterials.FirstOrDefault(p => p.Id == NoPaperMaterialId);

            RefreshPaperMaterialsFromDb();
            MessageBox.Show($"Đã xóa loại giấy \"{paper.Name}\".", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveNewCustomer()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName))
            {
                MessageBox.Show("Vui lòng nhập tên khách hàng!");
                return;
            }

            using (var db = new AppDbContext())
            {
                if (db.Customers.Any(c => c.Name == NewCustomerName))
                {
                    MessageBox.Show($"Khách hàng \"{NewCustomerName}\" đã tồn tại!", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var c = new Customer
                {
                    Name = NewCustomerName.Trim(),
                    Phone = NewCustomerPhone ?? "",
                    Address = NewCustomerAddress ?? "",
                    TaxCode = NewCustomerTaxCode ?? ""
                };
                c.NormalizeForDatabase();
                db.Customers.Add(c);
                db.SaveChanges();

                LoadCustomers();
                SelectedCustomer = c;
            }

            IsAddCustomerPopupOpen = false;
            NewCustomerName = NewCustomerPhone = NewCustomerAddress = NewCustomerTaxCode = "";
        }

        /// <summary>Nạp đầy đủ thông tin báo giá đã lưu (lịch sử) — không tự đổi đơn giá cán theo loại.</summary>
        public void ApplyHistoryQuote(Quotation quote)
        {
            if (quote == null) return;

            _suppressLaminationAutoPrice = true;
            try
            {
                CustomerName = quote.CustomerName ?? "";
                CustomerAddress = quote.CustomerAddress ?? "";
                ProductName = quote.ProductName ?? "";
                ProductDimensions = quote.ProductDimensions ?? "";
                QuoteNo = quote.QuoteNo ?? "";
                QuotationDate = quote.QuoteDate.Date;
                ValidityDays = quote.ValidityDays;
                DeliveryDays = quote.DeliveryDays;
                Quantity = quote.Quantity;
                SoCon = quote.SoCon;
                BuHao = quote.BuHao;
                PrintLength = quote.PrintLength;
                PrintWidth = quote.PrintWidth;
                ColorCount = quote.ColorCount;
                var pl = quote.PlatePricePerColorLargeMachine;
                var ps = quote.PlatePricePerColorSmallMachine;
                bool useLarge;
                if (pl > 0 && ps <= 0) useLarge = true;
                else if (ps > 0 && pl <= 0) useLarge = false;
                else useLarge = quote.IsLargeMachine;

                if (pl <= 0 && ps <= 0)
                {
                    var (defL, defS) = PlatePriceDefaultsStore.Load();
                    pl = defL;
                    ps = defS;
                }

                _isLargeMachine = useLarge;
                OnPropertyChanged(nameof(IsLargeMachine));
                PlatePricePerColorLarge = pl;
                PlatePricePerColorSmall = ps;
                OnPropertyChanged(nameof(SelectedPlatePricePerColor));

                var lt = string.IsNullOrWhiteSpace(quote.LaminationType) ? "Bóng" : quote.LaminationType.Trim();
                _laminationType = lt;
                OnPropertyChanged(nameof(LaminationType));
                LaminationSides = quote.LaminationSides;
                LaminationPrice = quote.LaminationPrice;

                DieCutMoldPrice = quote.DieCutMoldPrice;
                StringPricePerItem = quote.StringPricePerItem;
                ButtonPricePerItem = quote.ButtonPricePerItem;
                BoxPrice = quote.BoxPrice;
                DeliveryFee = quote.DeliveryFee;
                PrintProofFee = quote.PrintProofFee;
                ProfitMargin = quote.ProfitMargin;

                if (!string.IsNullOrWhiteSpace(quote.PaperType) && PaperMaterials != null)
                {
                    var p = PaperMaterials.FirstOrDefault(m => m.Name == quote.PaperType);
                    if (p != null)
                        SelectedPaper = p;
                }

                // Sau khi chọn giấy (nếu có) — ghi đè lại định lượng & giá đã lưu trong báo giá
                PaperGsm = quote.PaperGsm;
                PaperPricePerTon = quote.PaperPricePerTon;
                if (PaperPricePerTon <= 0)
                {
                    if (SelectedPaper != null && SelectedPaper.Id != NoPaperMaterialId && SelectedPaper.AveragePrice > 0)
                        PaperPricePerTon = SelectedPaper.AveragePrice;
                    else
                        PaperPricePerTon = DefaultPaperPricePerTon;
                }
            }
            finally
            {
                _suppressLaminationAutoPrice = false;
            }

            CalculatePrice();
        }

        private void OpenEditPlatePricesDialog()
        {
            var dlg = new Views.PlatePriceEditDialog(PlatePricePerColorLarge, PlatePricePerColorSmall)
            {
                Owner = Application.Current?.MainWindow
            };
            if (dlg.ShowDialog() != true) return;
            PlatePriceDefaultsStore.Save(dlg.LargePerColor, dlg.SmallPerColor);
            PlatePricePerColorLarge = dlg.LargePerColor;
            PlatePricePerColorSmall = dlg.SmallPerColor;
            OnPropertyChanged(nameof(SelectedPlatePricePerColor));
        }

        private void SaveQuote()
        {
            if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(ProductName))
            {
                MessageBox.Show("Vui lòng nhập Tên khách hàng và Sản phẩm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra trạng thái khách hàng trước khi tạo báo giá
            using (var dbCheck = new AppDbContext())
            {
                var customerService = new CustomerService(dbCheck);
                
                // Tìm customer theo tên
                var customer = dbCheck.Customers
                    .AsEnumerable()
                    .FirstOrDefault(c => c.Name == CustomerName.Trim() && c.NgayXoa == null);
                
                if (customer != null)
                {
                    // Kiểm tra khách nợ xấu
                    try
                    {
                        customerService.ValidateKhachHangCoTheThanhToan(customer.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Không thể tạo báo giá", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    // Khách không có trong danh sách Customers (khách lẻ)
                    // Kiểm tra xem có khách nào trùng tên nhưng ở trạng thái nợ xấu không
                    var badCustomer = dbCheck.Customers
                        .AsEnumerable()
                        .FirstOrDefault(c => c.Name == CustomerName.Trim() && c.TrangThai == "Nợ xấu");
                    
                    if (badCustomer != null)
                    {
                        MessageBox.Show("Khách hàng đang nợ xấu, không thể tạo báo giá.", "Không thể tạo báo giá", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(QuoteNo) || QuoteNo.StartsWith("BG-"))
                QuoteNo = "BG-" + DateTime.Now.ToString("yyMMddHHmm");

            using (var db = new AppDbContext())
            {
                var quote = new Quotation
                {
                    UserId = AppSession.CurrentUser?.Id ?? 1,
                    QuoteNo = this.QuoteNo,
                    // DatePicker chỉ có phần ngày (00:00); ghép giờ thực tế lúc lưu để lịch sử hiển thị đúng
                    QuoteDate = QuotationDate.Date.Add(DateTime.Now.TimeOfDay),
                    ValidityDays = this.ValidityDays,
                    DeliveryDays = this.DeliveryDays,
                    CustomerName = this.CustomerName,
                    CustomerAddress = this.CustomerAddress ?? "",
                    CustomerTaxCode = this.CustomerTaxCode ?? "",
                    ProductName = this.ProductName,
                    ProductDimensions = this.ProductDimensions ?? "",
                    Quantity = this.Quantity,
                    SoCon = this.SoCon,
                    BuHao = this.BuHao,
                    PaperType = (this.SelectedPaper == null || this.SelectedPaper.Id == NoPaperMaterialId)
                        ? "Không chọn"
                        : this.SelectedPaper.Name,
                    PaperGsm = this.PaperGsm,
                    PaperPricePerTon = this.PaperPricePerTon,
                    PrintLength = this.PrintLength,
                    PrintWidth = this.PrintWidth,
                    ColorCount = this.ColorCount,
                    IsLargeMachine = this.IsLargeMachine,
                    PlatePricePerColorLargeMachine = this.PlatePricePerColorLarge,
                    PlatePricePerColorSmallMachine = this.PlatePricePerColorSmall,
                    LaminationType = this.LaminationType,
                    LaminationSides = this.LaminationSides,
                    LaminationPrice = this.LaminationPrice,
                    DieCutMoldPrice = this.DieCutMoldPrice,
                    StringPricePerItem = this.StringPricePerItem,
                    ButtonPricePerItem = this.ButtonPricePerItem,
                    BoxPrice = this.BoxPrice,
                    DeliveryFee = this.DeliveryFee,
                    PrintProofFee = this.PrintProofFee,
                    ProfitMargin = this.ProfitMargin,
                    TotalProductionCost = this.TotalProductionCost,
                    QuotedUnitPrice = this.QuotedUnitPrice,
                    TotalOrderValue = this.TotalOrderValue
                };

                db.Quotations.Add(quote);
                db.SaveChanges();

                foreach (var extra in this.ExtraCosts)
                {
                    extra.QuotationId = quote.Id;
                    db.QuoteExtraCosts.Add(extra);
                }
                db.SaveChanges();

                MessageBox.Show($"✅ Đã lưu Báo giá số [{QuoteNo}] thành công!");
                QuoteNo = "BG-" + DateTime.Now.ToString("yyMMddHHmm");
                QuotationDate = DateTime.Now.Date;
            }
        }

        private void ExportExcel()
        {
            string safeName = System.Text.RegularExpressions.Regex.Replace(CustomerName, @"[/\\:*?""<>|]", "_");
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"BaoGia_{safeName}.xlsx" };
            if (sfd.ShowDialog() == true)
            {
                // Gọi helper xuất Excel
                Helpers.QuotationExportHelper.ExportToExcel(this, sfd.FileName);
                MessageBox.Show("✅ Xuất Excel thành công!");
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
        }

        private void ExportPdf()
        {
            string safeName = System.Text.RegularExpressions.Regex.Replace(CustomerName, @"[/\\:*?""<>|]", "_");
            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"BaoGia_{safeName}.pdf" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // 1. Xuất file PDF
                    Helpers.QuotationExportHelper.ExportToPdf(this, sfd.FileName);
                    MessageBox.Show("✅ Xuất PDF thành công!\n\nFile được lưu tại:\n" + sfd.FileName, "Thông báo");

                    // 2. Thử mở file PDF
                    try
                    {
                        Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        string folderPath = Path.GetDirectoryName(sfd.FileName);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folderPath}\"") { UseShellExecute = true });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Có lỗi xảy ra trong quá trình xuất file:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private readonly record struct PriceCalcResult(
            double PaperCost,
            double PaperPricePerRam,
            double PaperTotalSheets,
            int PaperTotalRams,
            double PlateCost,
            double PrintCost,
            double LaminationCost,
            double DieCutCost,
            double GluingCost,
            double OtherCostDisplayed,
            double TotalProductionCost,
            double QuotedUnitPrice,
            double TotalOrderValue);

        public void CalculatePrice()
        {
            SyncWireButtonTotalDisplayStrings();
            if (Quantity <= 0 || SoCon <= 0) return;

            ApplySidebarCosts();
            UpdatePriceMatrix();
        }

        /// <summary>Cập nhật các dòng tiền bên phải theo SL form hoặc theo dòng đang chọn ở bảng mục 5.</summary>
        private void ApplySidebarCosts()
        {
            if (Quantity <= 0 || SoCon <= 0) return;
            int q = _sidebarQuantityOverride ?? Quantity;
            var result = CalculateResultForQuantity(q);

            PaperCost = result.PaperCost;
            PaperCostBreakdownText = BuildPaperCostBreakdownText(result, q);
            PlateCost = result.PlateCost;
            PrintCost = result.PrintCost;
            LaminationCost = result.LaminationCost;
            DieCutCost = result.DieCutCost;
            GluingCost = result.GluingCost;
            OtherCostTotal = result.OtherCostDisplayed;
            OtherCostExcludingProof = result.OtherCostDisplayed - PrintProofFee;
            TotalProductionCost = result.TotalProductionCost;
            QuotedUnitPrice = result.QuotedUnitPrice;
            TotalOrderValue = result.TotalOrderValue;
            UpdatePrintSizeMismatchWarning();
        }

        private void UpdatePrintSizeMismatchWarning()
        {
            var hint = BuildPrintSizeMismatchWarning();
            if (_printSizeMismatchWarning == hint) return;
            _printSizeMismatchWarning = hint;
            OnPropertyChanged(nameof(PrintSizeMismatchWarning));
            OnPropertyChanged(nameof(HasPrintSizeMismatch));
        }

        /// <summary>Lấy hai số đầu từ chuỗi dạng "39x27x9" hoặc "40 x 60".</summary>
        private static (double? w, double? h) TryParseProductDimensionsFirstPlane(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return (null, null);
            var m = Regex.Match(raw.Trim(), @"^\s*(\d+(?:[.,]\d+)?)\s*[xX×]\s*(\d+(?:[.,]\d+)?)");
            if (!m.Success) return (null, null);
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double a)) return (null, null);
            if (!double.TryParse(m.Groups[2].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double b)) return (null, null);
            return (a, b);
        }

        private string BuildPrintSizeMismatchWarning()
        {
            var (pdW, pdH) = TryParseProductDimensionsFirstPlane(ProductDimensions);
            if (pdW == null || pdH == null) return "";
            const double tolCm = 0.5;
            if (Math.Abs(PrintLength - pdW.Value) < tolCm && Math.Abs(PrintWidth - pdH.Value) < tolCm)
                return "";
            return $"Lưu ý: Kích thước sản phẩm ghi {pdW:0.##}×{pdH:0.##} cm khác Khổ in đang dùng ({PrintLength}×{PrintWidth} cm). Tiền giấy và cán màng tính theo Khổ in — nếu so sánh với bảng tay, hãy dùng cùng khổ.";
        }

        /// <summary>Tránh cộng trùng: ô «Tiền in proof» + dòng phát sinh tên chứa proof/in thử.</summary>
        private static bool IsProofLikeExtraLine(QuoteExtraCost? x)
        {
            var n = (x?.ItemName ?? "").Trim();
            if (n.Length == 0) return false;
            if (n.Contains("proof", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.Contains("in thử", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.Contains("in thu", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void UpdatePriceMatrix()
        {
            _restoringMatrixSelection = true;
            try
            {
                PriceMatrix.Clear();

                // Lọc các tier chỉ hiện mức <= Quantity hiện tại
                int currentQty = Quantity > 0 ? Quantity : 0;
                int[] allTiers = { 500, 1000, 2000, 5000, 10000, 20000, 50000 };

                foreach (int qty in allTiers)
                {
                    // Chỉ thêm tier nếu <= Quantity hiện tại
                    if (qty > currentQty) continue;

                    var res = CalculateResultForQuantity(qty);
                    double costPerItem = qty > 0 ? res.TotalProductionCost / qty : 0;
                    PriceMatrix.Add(new PriceTier
                    {
                        Quantity = qty,
                        CostPerItem = costPerItem,
                        QuotedUnitPrice = res.QuotedUnitPrice,
                        TotalValue = res.TotalOrderValue,
                        DetailTooltip = $"Giá một SP (gồm LN): {res.QuotedUnitPrice:N0} đ/cái\nTổng đơn: {res.TotalOrderValue:N0} đ\nTiền giấy: {res.PaperTotalRams} ram × {FormatVnMoney0(res.PaperPricePerRam)} đ/ram ≈ {FormatVnMoney0(res.PaperCost)} đ"
                    });
                }

                // Nếu Quantity không trùng tier nào → thêm dòng cho Quantity hiện tại (nếu > 0)
                if (currentQty > 0 && !PriceMatrix.Any(t => t.Quantity == currentQty))
                {
                    var res = CalculateResultForQuantity(currentQty);
                    double costPerItem = res.TotalProductionCost / currentQty;
                    PriceMatrix.Add(new PriceTier
                    {
                        Quantity = currentQty,
                        CostPerItem = costPerItem,
                        QuotedUnitPrice = res.QuotedUnitPrice,
                        TotalValue = res.TotalOrderValue,
                        DetailTooltip = $"Giá một SP (gồm LN): {res.QuotedUnitPrice:N0} đ/cái\nTổng đơn: {res.TotalOrderValue:N0} đ\nTiền giấy: {res.PaperTotalRams} ram × {FormatVnMoney0(res.PaperPricePerRam)} đ/ram ≈ {FormatVnMoney0(res.PaperCost)} đ"
                    });
                }

                _selectedPriceMatrixRow = _sidebarQuantityOverride.HasValue
                    ? PriceMatrix.FirstOrDefault(t => t.Quantity == _sidebarQuantityOverride.Value)
                    : null;
                OnPropertyChanged(nameof(SelectedPriceMatrixRow));
            }
            finally
            {
                _restoringMatrixSelection = false;
            }
        }

        private string BuildPaperCostBreakdownText(PriceCalcResult r, int qtyForCalc)
        {
            if (qtyForCalc <= 0)
                return "Nhập số lượng > 0 và số con/tờ để tính tiền giấy.";
            if (r.PaperPricePerRam <= 0 && r.PaperCost <= 0)
                return "Nhập giá giấy (VNĐ/tấn) ở mục 2 — hệ thống tính đơn giá 1 ram (500 tờ) và tiền giấy theo công thức:\n" +
                       "• Đơn giá 1 ram = (Dài/100)×(Rộng/100)×(định lượng/1000)×(Giá tấn/1000)×500\n" +
                       "• Số tờ = (Số lượng ÷ Số con/tờ) + Bù hao\n" +
                       "• Số ram = làm tròn lên (Số tờ ÷ 500)\n" +
                       "• Tiền giấy = Số ram × Đơn giá 1 ram";
            return
                $"Đơn giá 1 ram (500 tờ): {FormatVnMoney0(r.PaperPricePerRam)} đ\n" +
                $"Số tờ: ({FormatVnMoney0(qtyForCalc)} ÷ {SoCon}) + {BuHao} = {FormatVnMoney0(r.PaperTotalSheets)} tờ\n" +
                $"Số ram (làm tròn lên): {r.PaperTotalRams} ram\n" +
                $"→ Tiền giấy: {r.PaperTotalRams} × {FormatVnMoney0(r.PaperPricePerRam)} = {FormatVnMoney0(r.PaperCost)} đ";
        }

        private void ShowCostDetail()
        {
            if (Quantity <= 0 || SoCon <= 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng > 0 và số con/tờ > 0.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int q = _sidebarQuantityOverride ?? Quantity;
            string text = BuildFullCostDetailReport(q);
            var w = new Views.CostDetailWindow(text) { Owner = Application.Current?.MainWindow };
            w.ShowDialog();
        }

        /// <summary>Báo cáo đầy đủ từng khoản + tổng — cùng số lượng với bảng chi tiết bên phải.</summary>
        private string BuildFullCostDetailReport(int q)
        {
            var r = CalculateResultForQuantity(q);
            double soCon = SoCon <= 0 ? 1 : SoCon;
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            sb.AppendLine("========== CHI TIẾT GIÁ VỐN IN ==========");
            sb.AppendLine($"Khách hàng: {CustomerName}");
            sb.AppendLine($"Sản phẩm: {ProductName}");
            sb.AppendLine(_sidebarQuantityOverride.HasValue
                ? $"Số lượng tính: {FormatVnMoney0(q)} cái (theo dòng đã chọn ở «Bảng giá theo SL»)."
                : $"Số lượng tính: {FormatVnMoney0(q)} cái (theo ô Số lượng trên form).");
            sb.AppendLine($"Số con/tờ: {soCon}");
            sb.AppendLine($"Bù hao (tờ giấy): {BuHao}");
            sb.AppendLine($"Khổ in: {PrintLength} × {PrintWidth} cm | Định lượng: {PaperGsm} gsm | Giá giấy: {FormatVnMoney0(PaperPricePerTon)} đ/tấn");
            if (!string.IsNullOrWhiteSpace(PrintSizeMismatchWarning))
                sb.AppendLine(PrintSizeMismatchWarning);
            sb.AppendLine();

            double lenDm = PrintLength / 100.0;
            double widDm = PrintWidth / 100.0;
            double gsmK = PaperGsm / 1000.0;
            double tonK = PaperPricePerTon / 1000.0;

            sb.AppendLine("1) TIỀN GIẤY (NVL)");
            sb.AppendLine("  Công thức 1 ram (500 tờ) khi nhà cung cấp báo giá theo tấn:");
            sb.AppendLine("  Giá 1 ram = (Dài/100) × (Rộng/100) × (định lượng/1000) × (giá tấn/1000) × 500");
            sb.AppendLine($"  = ({PrintLength}/100)×({PrintWidth}/100)×({PaperGsm}/1000)×({FormatVnMoney0(PaperPricePerTon)}/1000)×500");
            sb.AppendLine($"  = {lenDm.ToString("0.########", inv)} × {widDm.ToString("0.########", inv)} × {gsmK.ToString("0.########", inv)} × {tonK.ToString("0.########", inv)} × 500");
            sb.AppendLine($"  = {FormatVnMoney0(r.PaperPricePerRam)} đ/ram");
            sb.AppendLine($"  Số tờ = ({FormatVnMoney0(q)} ÷ {soCon}) + {BuHao} = {FormatVnMoney0(r.PaperTotalSheets)} tờ");
            sb.AppendLine($"  Số ram (làm tròn lên) = {r.PaperTotalRams} ram");
            sb.AppendLine($"  → Tiền giấy = {r.PaperTotalRams} × {FormatVnMoney0(r.PaperPricePerRam)} = {FormatVnMoney0(r.PaperCost)} đ");
            sb.AppendLine();

            double platePerColor = IsLargeMachine ? PlatePricePerColorLarge : PlatePricePerColorSmall;
            sb.AppendLine("2) TIỀN KẼM");
            sb.AppendLine($"  {ColorCount} màu × {FormatVnMoney0(platePerColor)} đ/màu ({(IsLargeMachine ? "máy lớn" : "máy nhỏ")}) = {FormatVnMoney0(r.PlateCost)} đ");
            sb.AppendLine();

            sb.AppendLine("3) TIỀN IN");
            if (q < 5000)
                sb.AppendLine($"  SL < 5.000: {ColorCount} × 300.000 = {FormatVnMoney0(r.PrintCost)} đ");
            else
                sb.AppendLine($"  SL ≥ 5.000: {ColorCount} × 70 × {FormatVnMoney0(q)} = {FormatVnMoney0(r.PrintCost)} đ");
            sb.AppendLine();

            sb.AppendLine("4) TIỀN CÁN MÀNG");
            if (IsNoLamination(LaminationType))
                sb.AppendLine($"  Loại: {LaminationType} → 0 đ");
            else
            {
                double areaM2 = lenDm * widDm;
                sb.AppendLine($"  Loại: {LaminationType} | Đơn giá: {FormatVnMoney0(LaminationPrice)} đ (theo m² in) × {LaminationSides} mặt");
                sb.AppendLine($"  Diện tích 1 tờ (m²) = (Dài/100)×(Rộng/100) = {areaM2.ToString("0.######", inv)}");
                sb.AppendLine($"  → diện_tích × đơn_giá × mặt ÷ số_con × SL = {FormatVnMoney0(r.LaminationCost)} đ");
            }
            sb.AppendLine();

            sb.AppendLine("5) TIỀN BẾ");
            sb.AppendLine($"  {soCon} × 150 × {FormatVnMoney0(q)} = {FormatVnMoney0(r.DieCutCost)} đ");
            sb.AppendLine();

            sb.AppendLine("6) TIỀN DÁN");
            if (q < 5000)
                sb.AppendLine($"  SL < 5.000: cố định 300.000 đ = {FormatVnMoney0(r.GluingCost)} đ");
            else
            {
                double extraThousands = Math.Floor((q - 5000) / 1000.0);
                sb.AppendLine($"  SL ≥ 5.000: 300.000 + ⌊(SL−5000)/1000⌋×100.000");
                sb.AppendLine($"  = 300.000 + {extraThousands:N0}×100.000 = {FormatVnMoney0(r.GluingCost)} đ");
            }
            sb.AppendLine();

            sb.AppendLine("7) CHI PHÍ KHÁC");
            int boxCount = (int)Math.Ceiling(q / (double)PcsPerBox);
            double boxCostTotal = boxCount * BoxPrice;
            double stringCostTotal = StringPricePerItem * q;
            double buttonCostTotal = ButtonPricePerItem * q;
            sb.AppendLine($"  • Thùng: ⌈{FormatVnMoney0(q)}/{PcsPerBox}⌉ × {FormatVnMoney0(BoxPrice)} = {FormatVnMoney0(boxCostTotal)} đ");
            sb.AppendLine($"  • Dây: {FormatVnMoney0(StringPricePerItem)} đ/cái × {FormatVnMoney0(q)} = {FormatVnMoney0(stringCostTotal)} đ");
            sb.AppendLine($"  • Nút: {FormatVnMoney0(ButtonPricePerItem)} đ/cái × {FormatVnMoney0(q)} = {FormatVnMoney0(buttonCostTotal)} đ");
            sb.AppendLine($"  • Xe / giao hàng: {FormatVnMoney0(DeliveryFee)} đ");
            sb.AppendLine($"  • Khuôn bế: {FormatVnMoney0(DieCutMoldPrice)} đ");
            sb.AppendLine($"  • Tiền in proof: {FormatVnMoney0(PrintProofFee)} đ");
            if (ExtraCosts != null)
            {
                foreach (var x in ExtraCosts)
                {
                    double lineTotal = x.Quantity * x.UnitPrice;
                    if (PrintProofFee > 1e-9 && IsProofLikeExtraLine(x))
                    {
                        sb.AppendLine($"  • Phát sinh «{x.ItemName}»: {FormatVnMoney0(lineTotal)} đ — bỏ qua (trùng «Tiền in proof»)");
                        continue;
                    }
                    sb.AppendLine($"  • Phát sinh «{x.ItemName}»: {FormatVnMoney0(x.Quantity)} × {FormatVnMoney0(x.UnitPrice)} = {FormatVnMoney0(lineTotal)} đ");
                }
            }
            sb.AppendLine($"  → Cộng mục 7 (vào tổng CP SX): {FormatVnMoney0(r.OtherCostDisplayed)} đ");
            sb.AppendLine();

            sb.AppendLine("──────── TỔNG HỢP ────────");
            sb.AppendLine($"  Tiền giấy:      {FormatVnMoney0(r.PaperCost)} đ");
            sb.AppendLine($"  Tiền kẽm:       {FormatVnMoney0(r.PlateCost)} đ");
            sb.AppendLine($"  Tiền in:        {FormatVnMoney0(r.PrintCost)} đ");
            sb.AppendLine($"  Cán màng:       {FormatVnMoney0(r.LaminationCost)} đ");
            sb.AppendLine($"  Tiền bế:        {FormatVnMoney0(r.DieCutCost)} đ");
            sb.AppendLine($"  Tiền dán:       {FormatVnMoney0(r.GluingCost)} đ");
            sb.AppendLine($"  Chi phí khác:   {FormatVnMoney0(r.OtherCostDisplayed)} đ");
            sb.AppendLine($"  TỔNG CP SX:     {FormatVnMoney0(r.TotalProductionCost)} đ");
            sb.AppendLine();
            sb.AppendLine($"  Giá vốn / cái:  {FormatVnMoney0(r.TotalProductionCost / q)} đ");
            sb.AppendLine($"  Lợi nhuận:      {ProfitMargin}%");
            sb.AppendLine($"  Giá báo / cái (gồm LN): {FormatVnMoney0(r.QuotedUnitPrice)} đ");
            sb.AppendLine($"  TỔNG ĐƠN HÀNG:  {FormatVnMoney0(r.TotalOrderValue)} đ");
            sb.AppendLine("==========================================");
            return sb.ToString();
        }

        private PriceCalcResult CalculateResultForQuantity(int qty)
        {
            if (qty <= 0)
                return new PriceCalcResult(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            // 1. TIỀN GIẤY — chỉ NVL (ram × đơn giá ram). Không gộp tiền in proof (ô riêng mục 3).
            double soCon = SoCon <= 0 ? 1 : SoCon;
            // Giá 1 ram (500 tờ) = (khổ cm/100)×(khổ cm/100)×(gsm/1000)×(giá tấn/1000)×500 — tương đương (Dài×Rộng/10000)×…
            double pricePerRam = (PrintLength / 100.0) * (PrintWidth / 100.0) * (PaperGsm / 1000.0) * (PaperPricePerTon / 1000.0) * 500.0;
            double totalSheets = (qty / (double)soCon) + BuHao;
            int totalRams = (int)Math.Ceiling(totalSheets / 500.0);
            double paperCost = totalRams * pricePerRam;

            // 2. TIỀN KẼM — một loại máy: số màu × đơn giá/màu (máy lớn hoặc máy nhỏ)
            double platePerColor = IsLargeMachine ? PlatePricePerColorLarge : PlatePricePerColorSmall;
            double plateCost = ColorCount * platePerColor;

            // 3. TIỀN IN — SL dưới 5.000: số màu × 300.000đ; SL từ 5.000: số màu × 70 × SL
            double printCost = qty < 5000
                ? ColorCount * 300000.0
                : ColorCount * 70.0 * qty;

            // 4. TIỀN CÁN MÀNG
            double laminationCost = IsNoLamination(LaminationType)
                ? 0
                : (PrintLength / 100.0) * (PrintWidth / 100.0) * LaminationPrice * LaminationSides / soCon * qty;

            // 5. TIỀN BẾ
            double dieCutCost = soCon * 150 * qty;

            // 6. TIỀN DÁN
            double gluingCost;
            if (qty < 5000)
                gluingCost = 300000;
            else
            {
                double extraThousands = Math.Floor((qty - 5000) / 1000.0);
                gluingCost = 300000 + (extraThousands * 100000);
            }

            // 7. CÁC CHI PHÍ KHÁC
            double boxCostTotal = Math.Ceiling(qty / (double)PcsPerBox) * BoxPrice;
            double stringCostTotal = StringPricePerItem * qty;
            double buttonCostTotal = ButtonPricePerItem * qty;
            double otherCostTotal = boxCostTotal + stringCostTotal + buttonCostTotal + DeliveryFee + DieCutMoldPrice + PrintProofFee;

            // Khi đã nhập «Tiền in proof» ở mục 3, không cộng thêm dòng phát sinh trùng (proof / in thử).
            double dynamicExtraCost = 0;
            if (ExtraCosts != null)
            {
                foreach (var x in ExtraCosts)
                {
                    if (PrintProofFee > 1e-9 && IsProofLikeExtraLine(x))
                        continue;
                    dynamicExtraCost += x.Quantity * x.UnitPrice;
                }
            }
            double otherCostDisplayed = otherCostTotal + dynamicExtraCost;

            double totalProductionCost = paperCost + plateCost + printCost + laminationCost + dieCutCost + gluingCost + otherCostDisplayed;
            double unitCost = totalProductionCost / qty;
            double quotedUnitPrice = unitCost * (1 + (ProfitMargin / 100.0));
            double totalOrderValue = quotedUnitPrice * qty;

            return new PriceCalcResult(
                paperCost, pricePerRam, totalSheets, totalRams,
                plateCost, printCost, laminationCost, dieCutCost, gluingCost,
                otherCostDisplayed, totalProductionCost, quotedUnitPrice, totalOrderValue);
        }

        private void AddExtraCost()
        {
            ExtraCosts.Add(new QuoteExtraCost { ItemName = "", Quantity = 1, UnitPrice = 0 });
        }

        private void RemoveExtraCost(QuoteExtraCost item)
        {
            if (item == null) return;
            ExtraCosts.Remove(item);
        }

        private void ExtraCosts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (QuoteExtraCost c in e.NewItems)
                    c.PropertyChanged += ExtraCostItem_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (QuoteExtraCost c in e.OldItems)
                    c.PropertyChanged -= ExtraCostItem_PropertyChanged;
            }

            CalculatePrice();
        }

        private void ExtraCostItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(QuoteExtraCost.Quantity) or nameof(QuoteExtraCost.UnitPrice) or nameof(QuoteExtraCost.ItemName))
                CalculatePrice();
        }
    }
}