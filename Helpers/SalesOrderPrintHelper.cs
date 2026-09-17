using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Helpers
{
    public static class SalesOrderPrintHelper
    {
        /// <summary>Mở cửa sổ xem trước FlowDocument; từ đó người dùng chọn «In…» để mở hộp thoại máy in.</summary>
        public static void ShowPreviewThenPrint(SalesOrder order, string customerName, string customerPhone, string customerAddress, string staffName)
        {
            var previewDoc = BuildFlowDocument(order, customerName, customerPhone, customerAddress, staffName);
            previewDoc.PagePadding = new Thickness(40);
            previewDoc.ColumnWidth = 680;
            previewDoc.PageWidth = 720;

            var win = new Window
            {
                Title = "Xem trước — Đơn hàng bán",
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

            btnPrint.Click += (_, _) => PrintToDevice(order, customerName, customerPhone, customerAddress, staffName);
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

        /// <summary>In thẳng (không xem trước).</summary>
        public static void Print(SalesOrder order, string customerName, string customerPhone, string customerAddress, string staffName)
        {
            PrintToDevice(order, customerName, customerPhone, customerAddress, staffName);
        }

        private static void PrintToDevice(SalesOrder order, string customerName, string customerPhone, string customerAddress, string staffName)
        {
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            var doc = BuildFlowDocument(order, customerName, customerPhone, customerAddress, staffName);
            doc.PageHeight = pd.PrintableAreaHeight;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.PagePadding = new Thickness(40);
            doc.ColumnGap = 0;
            doc.ColumnWidth = pd.PrintableAreaWidth;

            if (doc is IDocumentPaginatorSource idps)
                pd.PrintDocument(idps.DocumentPaginator, $"DonHang_{order.OrderNo}");
        }

        private static FlowDocument BuildFlowDocument(SalesOrder o, string customerName, string customerPhone, string customerAddress, string staffName)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PagePadding = new Thickness(40),
                TextAlignment = TextAlignment.Left
            };

            // Tiêu đề
            doc.Blocks.Add(new Paragraph(new Run("ĐƠN HÀNG BÁN"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Số đơn + ngày
            doc.Blocks.Add(P($"Số đơn: {o.OrderNo}          Ngày đặt: {o.OrderDate:dd/MM/yyyy HH:mm}"));
            doc.Blocks.Add(P($"Nhân viên: {staffName ?? "N/A"}"));

            // Thông tin khách
            doc.Blocks.Add(new Paragraph(new Run("Thông tin khách hàng"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            });
            doc.Blocks.Add(P($"  Tên: {customerName}"));
            doc.Blocks.Add(P($"  ĐT: {customerPhone}"));
            doc.Blocks.Add(P($"  Địa chỉ: {customerAddress}"));
            doc.Blocks.Add(P($"  Mã số thuế: {o.TaxCode}"));

            // Sản phẩm
            doc.Blocks.Add(new Paragraph(new Run("Sản phẩm đặt mua"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            });
            doc.Blocks.Add(P($"  Tên sản phẩm: {o.ProductName}"));
            doc.Blocks.Add(P($"  Kích thước: {o.Dimensions}"));
            doc.Blocks.Add(P($"  Số lượng: {o.Quantity:N0}"));
            doc.Blocks.Add(P($"  Đơn giá: {o.UnitPrice:N0} VNĐ"));
            doc.Blocks.Add(P($"  Tổng cộng: {o.TotalAmount:N0} VNĐ"));

            // Hạch toán kế toán
            doc.Blocks.Add(new Paragraph(new Run("Thông tin hạch toán kế toán"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            });
            doc.Blocks.Add(P($"  Tài khoản Nợ: {o.TK_No ?? "(chưa chọn)"}"));
            doc.Blocks.Add(P($"  Tài khoản Có: {o.TK_Co ?? "(chưa chọn)"}"));

            // Ghi chú
            if (!string.IsNullOrWhiteSpace(o.PaymentTerm))
                doc.Blocks.Add(P($"  Ghi chú: {o.PaymentTerm}"));

            // Ký tên
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
            cellLeft.Blocks.Add(P("Người lập đơn", FontWeights.SemiBold, null, null));
            cellLeft.Blocks.Add(P("(Ký, ghi rõ họ tên)", null, FontStyles.Italic, 10));
            cellLeft.Blocks.Add(P(staffName ?? ""));
            cellLeft.Blocks.Add(P(".............................................."));

            var cellRight = new TableCell();
            cellRight.Blocks.Add(P("Người nhận đơn", FontWeights.SemiBold, null, null));
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
