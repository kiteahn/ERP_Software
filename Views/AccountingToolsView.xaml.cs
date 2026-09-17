using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhanMemInAnERP.Helpers;

namespace PhanMemInAnERP.Views
{
    public partial class AccountingToolsView : Window
    {
        private sealed class TriggerItem
        {
            public AccountingTrigger Trigger { get; init; }
            public string Display => AccountingJournalMapper.TriggerDisplayName(Trigger);
        }

        private sealed class MapRow
        {
            public string Action { get; init; } = "";
            public string Department { get; init; } = "";
            public string Document { get; init; } = "";
            public string Journal { get; init; } = "";
            public string Condition { get; init; } = "";
        }

        public AccountingToolsView()
        {
            InitializeComponent();
            cbTrigger.ItemsSource = Enum.GetValues<AccountingTrigger>()
                .Select(t => new TriggerItem { Trigger = t })
                .ToList();
            cbTrigger.SelectedIndex = 0;
            BuildMappingGrid();
        }

        private void BuildMappingGrid()
        {
            gridMap.Columns.Clear();
            gridMap.Columns.Add(new DataGridTextColumn { Header = "Hành động trong phần mềm", Binding = new System.Windows.Data.Binding("Action"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            gridMap.Columns.Add(new DataGridTextColumn { Header = "Bộ phận thực hiện", Binding = new System.Windows.Data.Binding("Department"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            gridMap.Columns.Add(new DataGridTextColumn { Header = "Chứng từ sinh ra", Binding = new System.Windows.Data.Binding("Document"), Width = new DataGridLength(1.3, DataGridLengthUnitType.Star) });
            gridMap.Columns.Add(new DataGridTextColumn { Header = "Bút toán tự động", Binding = new System.Windows.Data.Binding("Journal"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });
            gridMap.Columns.Add(new DataGridTextColumn { Header = "Điều kiện kích hoạt", Binding = new System.Windows.Data.Binding("Condition"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });

            var rows = new List<MapRow>
            {
                new() { Action = "Duyệt đơn mua hàng", Department = "Mua hàng / Kế toán", Document = "Phiếu duyệt PO (trạng thái)", Journal = "Không hạch toán", Condition = "Chỉ khi chưa nhập kho / chưa ghi nhận công nợ hàng về" },
                new() { Action = "Nhận hàng nhập kho", Department = "Kho / Mua hàng", Document = "Phiếu nhập kho, hóa đơn NCC (nếu có)", Journal = "Nợ 152 / Có 331", Condition = "Hoàn tất kiểm đếm, xác nhận giá trị NVL và công nợ NCC" },
                new() { Action = "Thanh toán nhà cung cấp (TM)", Department = "Kế toán", Document = "Phiếu chi, ủy nhiệm chi", Journal = "Nợ 331 / Có 111", Condition = "Thanh toán bằng tiền mặt" },
                new() { Action = "Thanh toán nhà cung cấp (CK)", Department = "Kế toán", Document = "Ủy nhiệm chi, sao kê", Journal = "Nợ 331 / Có 112", Condition = "Thanh toán chuyển khoản" },
                new() { Action = "Xuất kho NVL cho lệnh SX", Department = "Kho / Sản xuất", Document = "Phiếu xuất kho NVL theo lệnh SX", Journal = "Nợ 154 / Có 152", Condition = "Xuất đủ/xuất theo định mức cho WO; tập hợp TK 621 vào 154" },
                new() { Action = "Ghi nhận nhân công trực tiếp", Department = "Sản xuất / Nhân sự", Document = "Bảng chấm công máy / lệnh SX", Journal = "Nợ 154 / Có 334", Condition = "Khóa kỳ công hoặc xác nhận chi phí NC gắn lệnh; TK 622 → 154" },
                new() { Action = "Phân bổ chi phí máy (KH, điện, SC)", Department = "Sản xuất / Kế toán", Document = "Bảng phân bổ giờ máy", Journal = "Nợ 154 / Có 214; Nợ 154 / Có 331 (điện, CP chung)", Condition = "Theo giờ chạy máy & định mức PHẦN 2 (627 → 154)" },
                new() { Action = "Nhập kho thành phẩm", Department = "Kho / SX", Document = "Phiếu nhập TP, báo cáo đóng lệnh", Journal = "Nợ 155 / Có 154", Condition = "HOàn thành SX, xác định giá thành lệnh" },
                new() { Action = "Giao hàng + xuất hóa đơn", Department = "Bán hàng / Kế toán", Document = "Hóa đơn GTGT, biên bản giao hàng", Journal = "Nợ 131 / Có 511 / Có 3331", Condition = "Ghi nhận doanh thu & thuế đầu ra khi đủ điều kiện ghi nhận" },
                new() { Action = "Ghi nhận giá vốn bán hàng", Department = "Kế toán / Kho", Document = "Phiếu xuất kho bán hàng (âm kho)", Journal = "Nợ 632 / Có 155", Condition = "Đồng thời hoặc ngay sau ghi nhận DT; có giá vốn TP" },
                new() { Action = "Khách trả tiền mặt", Department = "Kế toán / Thu ngân", Document = "Phiếu thu", Journal = "Nợ 111 / Có 131", Condition = "Đã thu tiền, bù trừ công nợ KH" },
                new() { Action = "Khách chuyển khoản", Department = "Kế toán", Document = "Sao kê NH, đối chiếu", Journal = "Nợ 112 / Có 131", Condition = "Tiền về tài khoản công ty" },
                new() { Action = "Hàng hỏng QC (bồi thường)", Department = "QC / Kế toán", Document = "Biên bản hỏng, quyết định xử lý", Journal = "Nợ 138 / Có 154", Condition = "Có đối tượng bồi thường (NCC/NV); nếu không → 632" },
                new() { Action = "Hàng hỏng QC (ghi chi phí)", Department = "QC / Kế toán", Document = "Biên bản hủy / xử lý nội bộ", Journal = "Nợ 632 / Có 154", Condition = "Không thu hồi được; hạ giá trị dở dang" },
                new() { Action = "Hao hụt vượt định mức", Department = "SX / Kế toán", Document = "Bảng đối chiếu định mức vs thực tế", Journal = "Nợ 632 / Có 154", Condition = "Chênh lệch vượt định mức được phê duyệt (PHẦN 2.3–2.4)" },
                new() { Action = "Khách trả hàng (điều chỉnh DT)", Department = "Bán hàng / Kế toán", Document = "Hóa đơn điều chỉnh giảm", Journal = "Nợ 511, Nợ 3331 / Có 131", Condition = "Trả hàng làm giảm DT & thuế GTGT đầu ra" },
                new() { Action = "Khách trả hàng (nhập lại kho)", Department = "Kho / Kế toán", Document = "Phiếu nhập kho hàng trả", Journal = "Nợ 155 / Có 632", Condition = "Hàng đạt chất lượng nhập lại; hoàn nhập GV" },
                new() { Action = "Chiết khấu thanh toán sớm", Department = "Kế toán", Document = "Thỏa thuận CKTT, ghi nhận điều chỉnh", Journal = "Nợ 635 / Có 131", Condition = "KH thanh toán trước hạn theo hợp đồng" },
                new() { Action = "Chiết khấu thương mại theo sản lượng", Department = "Bán hàng / Kế toán", Document = "Phụ lục CK sau kỳ", Journal = "Nợ 511 / Có 131", Condition = "Đạt mốc sản lượng / doanh thu theo hợp đồng" },
                new() { Action = "Kết chuyển doanh thu cuối kỳ", Department = "Kế toán", Document = "Bút kết chuyển cuối tháng/quý", Journal = "Nợ 511 / Có 911", Condition = "Khóa sổ kỳ kế toán" },
                new() { Action = "Kết chuyển giá vốn cuối kỳ", Department = "Kế toán", Document = "Bút kết chuyển cuối kỳ", Journal = "Nợ 911 / Có 632", Condition = "Khóa sổ kỳ kế toán" },
                new() { Action = "Kết chuyển chi phí bán hàng", Department = "Kế toán", Document = "Bút kết chuyển cuối kỳ", Journal = "Nợ 911 / Có 641", Condition = "Khóa sổ kỳ kế toán" },
                new() { Action = "Kết chuyển chi phí quản lý", Department = "Kế toán", Document = "Bút kết chuyển cuối kỳ", Journal = "Nợ 911 / Có 642", Condition = "Khóa sổ kỳ kế toán" },
                new() { Action = "Xác định lãi (911 → 421)", Department = "Kế toán", Document = "Bút kết quả KD", Journal = "Nợ 911 / Có 421", Condition = "911 có bên Có sau khi kết chuyển đủ TK chi phí/DT" },
                new() { Action = "Xác định lỗ (421 → 911)", Department = "Kế toán", Document = "Bút kết quả KD", Journal = "Nợ 421 / Có 911", Condition = "911 có bên Nợ (lỗ) sau kết chuyển" }
            };
            gridMap.ItemsSource = rows;
        }

        private static double ParseMoney(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim().Replace(".", "").Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0] == ',' ? "." : ",");
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            double.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out v);
            return v;
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (cbTrigger.SelectedValue is not AccountingTrigger trigger)
            {
                MessageBox.Show("Chọn nghiệp vụ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var amounts = new AccountingAmounts
            {
                Amount = ParseMoney(tbAmount.Text),
                VatAmount = ParseMoney(tbVat.Text),
                CogsAmount = ParseMoney(tbCogs.Text),
                DiscountAmount = ParseMoney(tbDiscount.Text),
                Note = tbNote.Text?.Trim()
            };

            var lines = AccountingJournalMapper.Build(trigger, amounts);
            if (trigger == AccountingTrigger.AllocateMachineOverhead && lines.Count == 0)
            {
                MessageBox.Show("Phân bổ máy: nhập ít nhất một trong ba ô — Amount (khấu hao), VAT (điện), COGS (331 khác).", "Thiếu số liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new JournalPreviewWindow(trigger, lines, tbNote.Text, "DemoAccountingTools", null)
            {
                Owner = this
            };
            dlg.ShowDialog();
        }

        private void OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.GetFullPath(Path.Combine(baseDir, "docs", "ERP_Accounting_Auto_Mapping.html"));
            if (!File.Exists(path))
            {
                var alt = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "docs", "ERP_Accounting_Auto_Mapping.html"));
                path = File.Exists(alt) ? alt : path;
            }
            if (!File.Exists(path))
            {
                MessageBox.Show("Không tìm thấy file:\n" + path, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không mở được file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
