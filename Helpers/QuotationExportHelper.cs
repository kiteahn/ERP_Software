using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhanMemInAnERP.ViewModels;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Globalization;
using System.IO;

namespace PhanMemInAnERP.Helpers
{
    public static class QuotationExportHelper
    {
        private static readonly CultureInfo VnCulture = CultureInfo.GetCultureInfo("vi-VN");

        private static string FormatMoney0(double value) =>
            Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("N0", VnCulture);

        public static void ExportToExcel(QuotationViewModel vm, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("BaoGia");

                // Tiêu đề
                ws.Cells["A1:D1"].Merge = true;
                ws.Cells["A1"].Value = "BẢNG BÁO GIÁ IN ẤN";
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 16;
                ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Số báo giá + Ngày
                ws.Cells["A2"].Value = "Số báo giá:"; ws.Cells["B2"].Value = vm.QuoteNo ?? "";
                ws.Cells["C2"].Value = "Ngày báo giá:"; ws.Cells["D2"].Value = vm.QuotationDate.ToString("dd/MM/yyyy");
                ws.Cells["A2:B2"].Style.Font.Bold = true;

                // Hiệu lực + Giao hàng
                ws.Cells["A3"].Value = "Hiệu lực:"; ws.Cells["B3"].Value = $"{vm.ValidityDays} ngày";
                ws.Cells["C3"].Value = "Giao hàng:"; ws.Cells["D3"].Value = $"{vm.DeliveryDays} ngày";
                ws.Cells["A3:B3"].Style.Font.Bold = true;

                // Khách hàng
                ws.Cells["A5"].Value = "Tên khách hàng:"; ws.Cells["B5"].Value = vm.CustomerName;
                ws.Cells["A6"].Value = "Địa chỉ:"; ws.Cells["B6"].Value = vm.CustomerAddress ?? "";
                ws.Cells["A7"].Value = "Sản phẩm:"; ws.Cells["B7"].Value = vm.ProductName;
                ws.Cells["A8"].Value = "Số lượng:"; ws.Cells["B8"].Value = $"{vm.Quantity:N0} cái";
                ws.Cells["A5:A8"].Style.Font.Bold = true;

                // I. QUY CÁCH
                ws.Cells["A10:D10"].Merge = true;
                ws.Cells["A10"].Value = "I. QUY CÁCH SẢN PHẨM";
                ws.Cells["A10"].Style.Font.Bold = true;

                ws.Cells["A11"].Value = "- Khổ in:"; ws.Cells["B11"].Value = $"{vm.PrintLength} x {vm.PrintWidth} cm";
                ws.Cells["A12"].Value = "- Chất liệu giấy:"; ws.Cells["B12"].Value = vm.SelectedPaper?.Name ?? "Chưa chọn";
                ws.Cells["A13"].Value = "- Số con/tờ:"; ws.Cells["B13"].Value = $"{vm.SoCon} con/tờ";
                ws.Cells["A14"].Value = "- Bù hao:"; ws.Cells["B14"].Value = $"{vm.BuHao:N0} tờ";
                ws.Cells["A15"].Value = "- Tiền giấy (cách tính):";
                ws.Cells["B15"].Value = vm.PaperCostBreakdownText ?? "";
                ws.Cells["B15:D15"].Merge = true;
                ws.Cells["B15"].Style.WrapText = true;
                string mayKem = vm.IsLargeMachine ? "máy lớn" : "máy nhỏ";
                double platePer = vm.IsLargeMachine ? vm.PlatePricePerColorLarge : vm.PlatePricePerColorSmall;
                ws.Cells["A16"].Value = "- Số màu in:"; ws.Cells["B16"].Value = $"{vm.ColorCount} màu — Tiền kẽm ({mayKem}): {FormatMoney0(platePer)} đ/kẽm; tổng kẽm: {FormatMoney0(vm.ColorCount * platePer)} đ";
                ws.Cells["A17"].Value = "- Gia công:"; ws.Cells["B17"].Value = $"Cán màng {vm.LaminationType}, {vm.LaminationSides} mặt";

                // Không xuất bảng II. chi tiết giá thành (chỉ giữ tổng kết báo giá)
                const int summaryStartRow = 19;
                int r = summaryStartRow;
                ws.Cells[$"A{r}"].Value = "TỔNG TRỊ GIÁ ĐƠN HÀNG:"; ws.Cells[$"B{r}"].Value = vm.TotalOrderValue;
                ws.Cells[$"A{r}:B{r}"].Style.Font.Bold = true;
                ws.Cells[$"B{r}"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                ws.Cells[$"B{summaryStartRow}:B{r}"].Style.Numberformat.Format = "#,##0";
                ws.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }

        public static void ExportToPdf(QuotationViewModel vm, string filePath)
        {
            if (vm == null)
            {
                System.Windows.MessageBox.Show("Không có dữ liệu báo giá để xuất.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var document = new PdfDocument();
                document.Info.Title = "Báo giá in ấn";

                var page = document.AddPage();
                page.Size = PageSize.A4;

                const double margin = 40;
                double y = margin;
                double pageWidthPt = page.Width.Point;
                double contentRight = pageWidthPt - margin;
                // Cột giống Excel: nhãn trái ~ A:D merge feel, số căn phải cột B
                double labelX = margin;
                double valueRightX = contentRight;

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                    var bodyFont = new XFont("Arial", 11, XFontStyleEx.Regular);
                    var sectionFont = new XFont("Arial", 11, XFontStyleEx.Bold);
                    var totalFont = new XFont("Arial", 11, XFontStyleEx.Bold);

                    double RowHeight(XFont f) => gfx.MeasureString("Mg", f).Height;
                    void Skip(double extraPt) => y += extraPt;
                    // DrawString dùng y là baseline; khoảng sau gạch ngang phải đủ lớn để chữ không “cắt” qua đường kẻ
                    const double sepBeforeLine = 10;
                    const double sepAfterLine = 18;

                    void DrawLeft(string text, XFont font, XBrush brush)
                    {
                        gfx.DrawString(text, font, brush, labelX, y);
                        y += RowHeight(font) + 3;
                    }

                    void DrawLabelValue(string label, string value, XFont labelFont, XFont valueFont, XBrush valueBrush)
                    {
                        double rowTop = y;
                        gfx.DrawString(label, labelFont, XBrushes.Black, labelX, rowTop);
                        XSize vs = gfx.MeasureString(value, valueFont);
                        gfx.DrawString(value, valueFont, valueBrush, valueRightX - vs.Width, rowTop);
                        y += Math.Max(RowHeight(labelFont), RowHeight(valueFont)) + 3;
                    }

                    // ===== HEADER =====
                    string title = "BẢNG BÁO GIÁ IN ẤN";
                    XSize titleSz = gfx.MeasureString(title, titleFont);
                    gfx.DrawString(title, titleFont, XBrushes.Black, (pageWidthPt - titleSz.Width) / 2, y);
                    y += titleSz.Height + 12;

                    // Số báo giá + Ngày — trên 1 dòng
                    string soBg = $"Số báo giá: {vm.QuoteNo ?? ""}";
                    string ngayBg = $"Ngày báo giá: {vm.QuotationDate:dd/MM/yyyy}";
                    gfx.DrawString(soBg, bodyFont, XBrushes.Black, labelX, y);
                    XSize ngayBgSz = gfx.MeasureString(ngayBg, bodyFont);
                    gfx.DrawString(ngayBg, bodyFont, XBrushes.Black, valueRightX - ngayBgSz.Width, y);
                    y += RowHeight(bodyFont) + 5;

                    // Hiệu lực + Giao hàng — trên 1 dòng
                    string hieuLuc = $"Hiệu lực báo giá: {vm.ValidityDays} ngày";
                    string giaoHang = $"Thời gian giao hàng: {vm.DeliveryDays} ngày";
                    gfx.DrawString(hieuLuc, bodyFont, XBrushes.Black, labelX, y);
                    XSize ghSz = gfx.MeasureString(giaoHang, bodyFont);
                    gfx.DrawString(giaoHang, bodyFont, XBrushes.Black, valueRightX - ghSz.Width, y);
                    y += RowHeight(bodyFont) + 4;
                    y += sepBeforeLine;

                    // Gạch ngang phân cách (khách hàng)
                    gfx.DrawLine(XPens.Gray, labelX, y, valueRightX, y);
                    y += sepAfterLine;

                    // ===== THÔNG TIN KHÁCH =====
                    DrawLabelValue("Khách hàng:", vm.CustomerName ?? "", sectionFont, bodyFont, XBrushes.Black);
                    if (!string.IsNullOrWhiteSpace(vm.CustomerAddress))
                        DrawLabelValue("Địa chỉ:", vm.CustomerAddress, bodyFont, bodyFont, XBrushes.Black);
                    DrawLabelValue("Sản phẩm:", vm.ProductName ?? "", sectionFont, bodyFont, XBrushes.Black);
                    DrawLabelValue("Số lượng:", $"{FormatMoney0(vm.Quantity)} cái", sectionFont, bodyFont, XBrushes.Black);
                    y += 6;

                    // ===== I. QUY CÁCH =====
                    DrawLeft("I. QUY CÁCH SẢN PHẨM", sectionFont, XBrushes.Black);
                    DrawLabelValue("- Khổ in:", $"{vm.PrintLength} x {vm.PrintWidth} cm", bodyFont, bodyFont, XBrushes.Black);
                    DrawLabelValue("- Chất liệu giấy:", vm.SelectedPaper?.Name ?? "Chưa chọn", bodyFont, bodyFont, XBrushes.Black);
                    DrawLabelValue("- Số con:", $"{vm.SoCon} con/tờ", bodyFont, bodyFont, XBrushes.Black);
                    DrawLabelValue("- Bù hao:", $"{FormatMoney0(vm.BuHao)} tờ", bodyFont, bodyFont, XBrushes.Black);
                    string mayIn = vm.IsLargeMachine ? "Máy lớn" : "Máy nhỏ";
                    DrawLabelValue("- Số màu in:", $"{vm.ColorCount} màu ({mayIn})", bodyFont, bodyFont, XBrushes.Black);

                    string giaCong = "Cán màng";
                    if (vm.LaminationPrice > 0) giaCong += $" {vm.LaminationType} {vm.LaminationSides} mặt";
                    if (vm.DieCutMoldPrice > 0) giaCong += ", Khuôn bế";
                    if (vm.StringPricePerItem > 0) giaCong += ", Xỏ dây";
                    if (vm.ButtonPricePerItem > 0) giaCong += ", Đóng nút";
                    DrawLabelValue("- Gia công:", giaCong, bodyFont, bodyFont, XBrushes.Black);
                    y += 10;

                    // Không in mục II / bảng chi tiết giá thành — không hiển thị tiền giấy, kẽm, in…; chỉ tổng đơn hàng
                    y += sepBeforeLine;
                    gfx.DrawLine(XPens.Gray, labelX, y, valueRightX, y);
                    y += sepAfterLine;

                    DrawLabelValue("TỔNG TRỊ GIÁ ĐƠN HÀNG:", $"{FormatMoney0(vm.TotalOrderValue)} VNĐ", totalFont, totalFont, XBrushes.Red);
                }

                document.Save(filePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Không xuất được PDF.\n\n" + ex.Message,
                    "Lỗi PDF",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
