using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Helpers
{
    public static class DeliveryHandoverPrintHelper
    {
        /// <summary>Mở cửa sổ xem trước FlowDocument; từ đó người dùng chọn «In…» để mở hộp thoại máy in.</summary>
        public static void ShowPreviewThenPrint(DeliveryHandoverPrintData data)
        {
            var previewDoc = BuildFlowDocument(data);
            previewDoc.PagePadding = new Thickness(40);
            previewDoc.ColumnWidth = 680;
            previewDoc.PageWidth = 720;

            var win = new Window
            {
                Title = "Xem trước — Phiếu bàn giao hàng",
                Width = 780,
                Height = 820,
                MinWidth = 440,
                MinHeight = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                ShowInTaskbar = false
            };

            var viewer = new FlowDocumentScrollViewer
            {
                Document = previewDoc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsToolBarVisible = false
            };

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var btnPrint = new Button
            {
                Content = "🖨️ In…",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(18, 8, 18, 8),
                MinWidth = 120,
                IsDefault = true
            };
            var btnClose = new Button
            {
                Content = "Đóng",
                Padding = new Thickness(18, 8, 18, 8),
                MinWidth = 96,
                IsCancel = true
            };

            btnPrint.Click += (_, _) => PrintToDevice(data);
            btnClose.Click += (_, _) => win.Close();

            btnPanel.Children.Add(btnPrint);
            btnPanel.Children.Add(btnClose);

            var root = new DockPanel();
            DockPanel.SetDock(btnPanel, Dock.Bottom);
            root.Children.Add(btnPanel);
            root.Children.Add(viewer);
            win.Content = root;
            win.ShowDialog();
        }

        /// <summary>In thẳng (không xem trước) — giữ cho tương thích nếu cần gọi từ nơi khác.</summary>
        public static void Print(DeliveryHandoverPrintData data) => PrintToDevice(data);

        private static void PrintToDevice(DeliveryHandoverPrintData data)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            var doc = BuildFlowDocument(data);
            doc.PageHeight = pd.PrintableAreaHeight;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.PagePadding = new Thickness(40);
            doc.ColumnGap = 0;
            doc.ColumnWidth = pd.PrintableAreaWidth;

            if (doc is IDocumentPaginatorSource idps)
                pd.PrintDocument(idps.DocumentPaginator, $"PhieuBanGiao_{data.PhieuSo}");
        }

        private static FlowDocument BuildFlowDocument(DeliveryHandoverPrintData d)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PagePadding = new Thickness(40),
                TextAlignment = TextAlignment.Left
            };

            doc.Blocks.Add(new Paragraph(new Run("PHIẾU BÀN GIAO HÀNG"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            doc.Blocks.Add(P($"Số phiếu: {d.PhieuSo}          Ngày lập: {d.NgayLap:dd/MM/yyyy HH:mm}"));
            doc.Blocks.Add(P($"Khách hàng: {d.KhachHang}"));
            if (!string.IsNullOrWhiteSpace(d.DienThoai))
                doc.Blocks.Add(P($"Điện thoại: {d.DienThoai}"));
            if (!string.IsNullOrWhiteSpace(d.DiaChi))
                doc.Blocks.Add(P($"Địa chỉ giao: {d.DiaChi}"));
            doc.Blocks.Add(P($"Tham chiếu — Đơn hàng: {d.SoDonHang}  |  Hóa đơn: {d.SoHoaDon}"));

            doc.Blocks.Add(new Paragraph(new Run("Nội dung đã sản xuất & bàn giao"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            });

            doc.Blocks.Add(P($"• Sản phẩm: {d.SanPham}"));
            doc.Blocks.Add(P($"• Số lượng giao: {d.SoLuong:N0} {d.DonVi}"));

            if (!string.IsNullOrWhiteSpace(d.TongDonHang) && d.TongDonHang != "—")
                doc.Blocks.Add(P($"• Tổng đơn hàng: {d.TongDonHang}"));

            foreach (var line in d.NoiDungSanXuat.Where(x => !string.IsNullOrWhiteSpace(x)))
                doc.Blocks.Add(P($"• {line.Trim()}"));

            doc.Blocks.Add(new Paragraph(new Run("Xác nhận"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 28, 0, 8)
            });

            var table = new Table { CellSpacing = 0 };
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var rg = new TableRowGroup();
            var row = new TableRow();

            var cellLeft = new TableCell { Padding = new Thickness(0, 0, 16, 0) };
            cellLeft.Blocks.Add(P("Người giao hàng", FontWeights.SemiBold, null, null));
            cellLeft.Blocks.Add(P("(Ký, ghi rõ họ tên)", null, FontStyles.Italic, 10));
            if (!string.IsNullOrWhiteSpace(d.NguoiBanGiao))
                cellLeft.Blocks.Add(P(d.NguoiBanGiao));
            cellLeft.Blocks.Add(P(".............................................."));

            var cellRight = new TableCell();
            cellRight.Blocks.Add(P("Người nhận hàng", FontWeights.SemiBold, null, null));
            cellRight.Blocks.Add(P("(Ký, ghi rõ họ tên)", null, FontStyles.Italic, 10));
            cellRight.Blocks.Add(new Paragraph { Margin = new Thickness(0, 36, 0, 0) });
            cellRight.Blocks.Add(P(".............................................."));

            row.Cells.Add(cellLeft);
            row.Cells.Add(cellRight);
            rg.Rows.Add(row);
            table.RowGroups.Add(rg);
            doc.Blocks.Add(table);

            return doc;
        }

        private static Paragraph P(string text) => P(text, null, null, null);

        private static Paragraph P(string text, FontWeight? weight, FontStyle? style, double? fontSize)
        {
            var p = new Paragraph(new Run(text ?? "")) { Margin = new Thickness(0, 2, 0, 0) };
            if (weight != null) p.FontWeight = weight.Value;
            if (style != null) p.FontStyle = style.Value;
            if (fontSize != null) p.FontSize = fontSize.Value;
            return p;
        }
    }
}
