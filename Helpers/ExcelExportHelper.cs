using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhanMemInAnERP.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PhanMemInAnERP.Helpers
{
    public static class ExcelExportHelper
    {
        public static void XuatSoChiTietVatTu(
            List<SoChiTietVatTuItem> items,
            double tonDauKy,
            double tongNhap,
            double tongXuat,
            double tonCuoiKy,
            Material selectedMaterial,
            DateTime tuNgay,
            DateTime denNgay,
            string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("So Chi Tiet Vat Tu");

                string tieuDe = selectedMaterial == null
                    ? "SỔ CHI TIẾT VẬT TƯ - TẤT CẢ VẬT TƯ"
                    : $"SỔ CHI TIẾT VẬT TƯ - {selectedMaterial.Name}";

                ws.Cells["A1:K1"].Merge = true;
                ws.Cells["A1"].Value = "CÔNG TY TNHH SX THƯƠNG MẠI DỊCH VỤ AN LÂM";
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Size = 12;

                ws.Cells["A2:K2"].Merge = true;
                ws.Cells["A2"].Value = tieuDe;
                ws.Cells["A2"].Style.Font.Bold = true;
                ws.Cells["A2"].Style.Font.Size = 14;
                ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["A3:K3"].Merge = true;
                ws.Cells["A3"].Value = $"Từ ngày: {tuNgay:dd/MM/yyyy}   Đến ngày: {denNgay:dd/MM/yyyy}";
                ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Tổng hợp
                int row = 5;
                ws.Cells[$"A{row}"].Value = "TỒN ĐẦU KỲ:";
                ws.Cells[$"B{row}"].Value = tonDauKy;
                ws.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";

                row++;
                ws.Cells[$"A{row}"].Value = "TỔNG NHẬP TRONG KỲ:";
                ws.Cells[$"B{row}"].Value = tongNhap;
                ws.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";

                row++;
                ws.Cells[$"A{row}"].Value = "TỔNG XUẤT TRONG KỲ:";
                ws.Cells[$"B{row}"].Value = tongXuat;
                ws.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";

                row++;
                ws.Cells[$"A{row}"].Value = "TỒN CUỐI KỲ:";
                ws.Cells[$"A{row}"].Style.Font.Bold = true;
                ws.Cells[$"B{row}"].Value = tonCuoiKy;
                ws.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";
                ws.Cells[$"B{row}"].Style.Font.Bold = true;

                // Header bảng chi tiết
                row = 8;
                string[] headers = { "STT", "Ngày", "Số Phiếu", "Loại", "Mã VT", "Tên Vật Tư", "ĐVT", "Tồn ĐK", "SL Nhập", "SL Xuất", "Tồn CK" };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[row, i + 1].Value = headers[i];
                    ws.Cells[row, i + 1].Style.Font.Bold = true;
                    ws.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ws.Cells[row, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    ws.Cells[row, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                }

                // Data rows
                row++;
                int stt = 1;
                double lastTonDauKy = tonDauKy;

                foreach (var item in items)
                {
                    ws.Cells[row, 1].Value = stt++;
                    ws.Cells[row, 2].Value = item.Ngay;
                    ws.Cells[row, 2].Style.Numberformat.Format = "dd/MM/yyyy";
                    ws.Cells[row, 3].Value = item.SoPhieu;
                    ws.Cells[row, 4].Value = item.LoaiPhieu;
                    ws.Cells[row, 5].Value = item.MaVatTu;
                    ws.Cells[row, 6].Value = item.TenVatTu;
                    ws.Cells[row, 7].Value = item.DonViTinh;
                    ws.Cells[row, 8].Value = item.TonDauKy;
                    ws.Cells[row, 9].Value = item.SoLuongNhap;
                    ws.Cells[row, 10].Value = item.SoLuongXuat;
                    ws.Cells[row, 11].Value = item.TonCuoiKy;

                    // Format số
                    ws.Cells[row, 8, row, 11].Style.Numberformat.Format = "#,##0.##";

                    // Màu dòng theo loại
                    if (item.LoaiPhieu == "NHẬP")
                    {
                        for (int i = 1; i <= 11; i++)
                            ws.Cells[row, i].Style.Font.Color.SetColor(Color.DarkGreen);
                    }
                    else
                    {
                        for (int i = 1; i <= 11; i++)
                            ws.Cells[row, i].Style.Font.Color.SetColor(Color.DarkRed);
                    }

                    // Đóng khung
                    for (int i = 1; i <= 11; i++)
                        ws.Cells[row, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    row++;
                }

                // Dòng tổng cộng
                row++;
                ws.Cells[row, 1, row, 7].Merge = true;
                ws.Cells[row, 1].Value = "TỔNG CỘNG";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 8].Value = tonDauKy;
                ws.Cells[row, 9].Value = tongNhap;
                ws.Cells[row, 10].Value = tongXuat;
                ws.Cells[row, 11].Value = tonCuoiKy;

                ws.Cells[row, 8, row, 11].Style.Font.Bold = true;
                ws.Cells[row, 8, row, 11].Style.Numberformat.Format = "#,##0.##";

                for (int i = 1; i <= 11; i++)
                {
                    ws.Cells[row, i].Style.Border.BorderAround(ExcelBorderStyle.Double);
                    ws.Cells[row, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[row, i].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // Ngày in
                row += 2;
                ws.Cells[$"A{row}"].Value = $"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}";

                // Auto fit columns
                ws.Cells.AutoFitColumns();

                // Đặt độ rộng cột
                ws.Column(1).Width = 6;
                ws.Column(2).Width = 12;
                ws.Column(3).Width = 14;
                ws.Column(4).Width = 10;
                ws.Column(5).Width = 10;
                ws.Column(6).Width = 25;
                ws.Column(7).Width = 8;

                // Lưu file
                FileInfo fi = new FileInfo(filePath);
                package.SaveAs(fi);
            }
        }

        public static void XuatPhieuXuatKho(PhieuXuatKhoModel data, string filePath)
        {
            // Yêu cầu bản quyền phi thương mại của EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("PhieuXuatKho");

              
                ws.Cells["A1:D1"].Merge = true;
                ws.Cells["A1"].Value = "CÔNG TY TNHH SX THƯƠNG MẠI DỊCH VỤ AN LÂM";
                ws.Cells["A1"].Style.Font.Bold = true;

                ws.Cells["E1:H1"].Merge = true;
                ws.Cells["E1"].Value = "Mẫu số: 02 - VT";
                ws.Cells["E1"].Style.Font.Bold = true;
                ws.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["E2:H2"].Merge = true;
                ws.Cells["E2"].Value = "(Ban hành theo Thông tư số 200/2014/TT-BTC)";
                ws.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells["E2"].Style.Font.Italic = true;

             
                ws.Cells["A4:H4"].Merge = true;
                ws.Cells["A4"].Value = "PHIẾU XUẤT KHO";
                ws.Cells["A4"].Style.Font.Bold = true;
                ws.Cells["A4"].Style.Font.Size = 16;
                ws.Cells["A4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["A5:H5"].Merge = true;
                ws.Cells["A5"].Value = $"Ngày {data.NgayXuat:dd} tháng {data.NgayXuat:MM} năm {data.NgayXuat:yyyy}";
                ws.Cells["A5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells["A5"].Style.Font.Italic = true;

                ws.Cells["A6:H6"].Merge = true;
                ws.Cells["A6"].Value = $"Số: {data.SoPhieu}";
                ws.Cells["A6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

             
                ws.Cells["A8"].Value = $"- Họ và tên người nhận hàng: {data.NguoiNhan}";
                ws.Cells["A9"].Value = $"- Địa chỉ (bộ phận): {data.DiaChiBoPhan}";
                ws.Cells["A10"].Value = $"- Lý do xuất kho: {data.LyDoXuat}";
                ws.Cells["A11"].Value = $"- Xuất tại kho (ngăn lô): {data.XuatTaiKho}";

              
                int row = 13;
                string[] headers = { "STT", "Tên, nhãn hiệu, quy cách vật tư", "Mã số", "Đơn vị tính", "Số lượng Yêu cầu", "Số lượng Thực xuất", "Đơn giá", "Thành tiền" };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[row, i + 1].Value = headers[i];
                    ws.Cells[row, i + 1].Style.Font.Bold = true;
                    ws.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ws.Cells[row, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

             
                row++;
                string[] subHeaders = { "A", "B", "C", "D", "1", "2", "3", "4" };
                for (int i = 0; i < subHeaders.Length; i++)
                {
                    ws.Cells[row, i + 1].Value = subHeaders[i];
                    ws.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

             
                row++;
                int stt = 1;
                foreach (var item in data.ChiTietHangHoa)
                {
                    ws.Cells[row, 1].Value = stt++;
                    ws.Cells[row, 2].Value = item.TenVatTu;
                    ws.Cells[row, 3].Value = item.MaSo;
                    ws.Cells[row, 4].Value = item.DVT;
                    ws.Cells[row, 5].Value = item.SoLuongYeuCau;
                    ws.Cells[row, 6].Value = item.SoLuongThucXuat;
                    ws.Cells[row, 7].Value = item.DonGia;
                    ws.Cells[row, 8].Value = item.ThanhTien;

                    // Định dạng số
                    ws.Cells[row, 5, row, 8].Style.Numberformat.Format = "#,##0";

                    // Đóng khung
                    for (int i = 1; i <= 8; i++)
                        ws.Cells[row, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    row++;
                }

               
                ws.Cells[row, 1, row, 7].Merge = true;
                ws.Cells[row, 1].Value = "Cộng";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 8].Value = data.TongTien;
                ws.Cells[row, 8].Style.Font.Bold = true;
                ws.Cells[row, 8].Style.Numberformat.Format = "#,##0";

                for (int i = 1; i <= 8; i++)
                    ws.Cells[row, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);

              
                row += 2;
                ws.Cells[row, 1].Value = $"- Tổng số tiền (Viết bằng chữ): {data.TongTienBangChu}";
                ws.Cells[row + 1, 1].Value = $"- Số chứng từ gốc kèm theo: {data.ChungTuGoc}";

                row += 3;
                ws.Cells[row, 7, row, 8].Merge = true;
                ws.Cells[row, 7].Value = $"Ngày {data.NgayXuat:dd} tháng {data.NgayXuat:MM} năm {data.NgayXuat:yyyy}";
                ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 7].Style.Font.Italic = true;

                row++;
                ws.Cells[row, 1].Value = "Người lập biểu";
                ws.Cells[row, 3].Value = "Người nhận hàng";
                ws.Cells[row, 5].Value = "Thủ kho";
                ws.Cells[row, 6].Value = "Kế toán trưởng";
                ws.Cells[row, 7, row, 8].Merge = true;
                ws.Cells[row, 7].Value = "Giám đốc";

                ws.Cells[row, 1, row, 8].Style.Font.Bold = true;
                ws.Cells[row, 1, row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                row++;
                ws.Cells[row, 1].Value = "(Ký, họ tên)";
                ws.Cells[row, 3].Value = "(Ký, họ tên)";
                ws.Cells[row, 5].Value = "(Ký, họ tên)";
                ws.Cells[row, 6].Value = "(Ký, họ tên)";
                ws.Cells[row, 7, row, 8].Merge = true;
                ws.Cells[row, 7].Value = "(Ký, họ tên, đóng dấu)";
                ws.Cells[row, 1, row, 8].Style.Font.Italic = true;
                ws.Cells[row, 1, row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Tự động giãn cột
                ws.Cells.AutoFitColumns();

                // Lưu file
                FileInfo fi = new FileInfo(filePath);
                package.SaveAs(fi);
            }
        }
    }
}