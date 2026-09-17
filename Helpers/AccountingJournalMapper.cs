using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>Nghiệp vụ kích hoạt sinh bút toán (PHẦN 3).</summary>
    public enum AccountingTrigger
    {
        PurchaseOrderApprovedNoEntry,
        GoodsReceiptToWarehouse,
        SupplierPaymentCash,
        SupplierPaymentBank,
        IssueMaterialToProduction,
        AccrueDirectLabor,
        AllocateMachineOverhead,
        FinishedGoodsReceipt,
        SalesInvoiceAndDelivery,
        CostOfGoodsSold,
        CustomerPaymentCash,
        CustomerPaymentBank,
        QcRejectScrapToReceivable,
        QcRejectScrapToExpense,
        MaterialLossOverNorm,
        SalesReturn,
        SalesReturnRestock,
        EarlyPaymentDiscount,
        VolumeRebate,
        PeriodCloseRevenue,
        PeriodCloseCogs,
        PeriodCloseSellingExpense,
        PeriodCloseAdminExpense,
        PeriodCloseProfit,
        PeriodCloseLoss
    }

    /// <summary>Tham số số tiền cho bút toán (đơn vị VND).</summary>
    public class AccountingAmounts
    {
        /// <summary>Giá trị chính (NVL, công nợ, doanh thu chưa thuế…).</summary>
        public double Amount { get; set; }
        /// <summary>Thuế GTGT đầu ra / phụ trợ.</summary>
        public double VatAmount { get; set; }
        /// <summary>Giá vốn / giá trị hoàn trả kho.</summary>
        public double CogsAmount { get; set; }
        /// <summary>Chiết khấu / phụ.</summary>
        public double DiscountAmount { get; set; }
        public string? Note { get; set; }
    }

    public record JournalPreviewLine(int LineNo, string AccountCode, string AccountName, double Debit, double Credit, string Explanation);

    public static class AccountingJournalMapper
    {
        private static readonly Dictionary<string, string> Tk = new(StringComparer.Ordinal)
        {
            ["111"] = "Tiền mặt",
            ["112"] = "Tiền gửi ngân hàng",
            ["131"] = "Phải thu khách hàng",
            ["138"] = "Phải thu khác",
            ["152"] = "Nguyên vật liệu",
            ["154"] = "Chi phí SX dở dang",
            ["155"] = "Thành phẩm",
            ["214"] = "Hao mòn TSCĐ",
            ["331"] = "Phải trả người bán",
            ["3331"] = "Thuế GTGT đầu ra",
            ["334"] = "Phải trả người lao động",
            ["421"] = "Lợi nhuận sau thuế chưa phân phối",
            ["511"] = "Doanh thu bán hàng",
            ["621"] = "Chi phí NVL trực tiếp",
            ["622"] = "Chi phí nhân công trực tiếp",
            ["627"] = "Chi phí SX chung",
            ["632"] = "Giá vốn hàng bán",
            ["635"] = "Chi phí tài chính",
            ["641"] = "Chi phí bán hàng",
            ["642"] = "Chi phí quản lý doanh nghiệp",
            ["911"] = "Xác định kết quả kinh doanh"
        };

        private static string N(string code) => Tk.TryGetValue(code, out var n) ? n : code;

        private static JournalPreviewLine L(int no, string code, double dr, double cr, string? ex = null)
            => new(no, code, N(code), Round(dr), Round(cr), ex ?? "");

        private static double Round(double v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

        public static IReadOnlyList<JournalPreviewLine> Build(AccountingTrigger trigger, AccountingAmounts a)
        {
            var note = string.IsNullOrWhiteSpace(a.Note) ? "" : a.Note.Trim();
            return trigger switch
            {
                AccountingTrigger.PurchaseOrderApprovedNoEntry => Array.Empty<JournalPreviewLine>(),

                AccountingTrigger.GoodsReceiptToWarehouse => new[]
                {
                    L(1, "152", a.Amount, 0, $"Nhập kho NVL. {note}"),
                    L(2, "331", 0, a.Amount, $"Công nợ NCC. {note}")
                },

                AccountingTrigger.SupplierPaymentCash => new[]
                {
                    L(1, "331", a.Amount, 0, $"Thanh toán NCC. {note}"),
                    L(2, "111", 0, a.Amount, "Chi tiền mặt")
                },

                AccountingTrigger.SupplierPaymentBank => new[]
                {
                    L(1, "331", a.Amount, 0, $"Thanh toán NCC. {note}"),
                    L(2, "112", 0, a.Amount, "Chi qua ngân hàng")
                },

                AccountingTrigger.IssueMaterialToProduction => new[]
                {
                    L(1, "154", a.Amount, 0, $"Xuất NVL lệnh SX (621→154). {note}"),
                    L(2, "152", 0, a.Amount, "Giảm kho NVL")
                },

                AccountingTrigger.AccrueDirectLabor => new[]
                {
                    L(1, "154", a.Amount, 0, $"Nhân công trực tiếp (622→154). {note}"),
                    L(2, "334", 0, a.Amount, "Phải trả công nhân")
                },

                AccountingTrigger.AllocateMachineOverhead => AllocateOh(a, note),

                AccountingTrigger.FinishedGoodsReceipt => new[]
                {
                    L(1, "155", a.Amount, 0, $"Nhập kho thành phẩm. {note}"),
                    L(2, "154", 0, a.Amount, "Kết chuyển CPSX dở dang")
                },

                AccountingTrigger.SalesInvoiceAndDelivery => SalesInvoice(a, note),

                AccountingTrigger.CostOfGoodsSold => new[]
                {
                    L(1, "632", a.CogsAmount, 0, $"Ghi nhận giá vốn. {note}"),
                    L(2, "155", 0, a.CogsAmount, "Xuất kho thành phẩm")
                },

                AccountingTrigger.CustomerPaymentCash => new[]
                {
                    L(1, "111", a.Amount, 0, $"Thu tiền mặt KH. {note}"),
                    L(2, "131", 0, a.Amount, "Giảm phải thu")
                },

                AccountingTrigger.CustomerPaymentBank => new[]
                {
                    L(1, "112", a.Amount, 0, $"Thu chuyển khoản KH. {note}"),
                    L(2, "131", 0, a.Amount, "Giảm phải thu")
                },

                AccountingTrigger.QcRejectScrapToReceivable => new[]
                {
                    L(1, "138", a.Amount, 0, $"Hàng hỏng QC — bồi thường NCC/NV. {note}"),
                    L(2, "154", 0, a.Amount, "Giảm CPSX dở dang")
                },

                AccountingTrigger.QcRejectScrapToExpense => new[]
                {
                    L(1, "632", a.Amount, 0, $"Hàng hỏng QC — ghi nhận chi phí. {note}"),
                    L(2, "154", 0, a.Amount, "Giảm CPSX dở dang")
                },

                AccountingTrigger.MaterialLossOverNorm => new[]
                {
                    L(1, "632", a.Amount, 0, $"Hao hụt vượt định mức. {note}"),
                    L(2, "154", 0, a.Amount, "Giảm CPSX dở dang")
                },

                AccountingTrigger.SalesReturn => SalesReturnRevenue(a, note),

                AccountingTrigger.SalesReturnRestock => new[]
                {
                    L(1, "155", a.CogsAmount, 0, $"Nhập lại kho hàng trả. {note}"),
                    L(2, "632", 0, a.CogsAmount, "Hoàn nhập giá vốn")
                },

                AccountingTrigger.EarlyPaymentDiscount => new[]
                {
                    L(1, "635", a.DiscountAmount, 0, $"Chiết khấu thanh toán sớm. {note}"),
                    L(2, "131", 0, a.DiscountAmount, "Giảm phải thu")
                },

                AccountingTrigger.VolumeRebate => new[]
                {
                    L(1, "511", a.DiscountAmount, 0, $"Chiết khấu thương mại theo sản lượng. {note}"),
                    L(2, "131", 0, a.DiscountAmount, "Giảm phải thu")
                },

                AccountingTrigger.PeriodCloseRevenue => new[]
                {
                    L(1, "511", a.Amount, 0, "Kết chuyển doanh thu cuối kỳ"),
                    L(2, "911", 0, a.Amount, "911")
                },

                AccountingTrigger.PeriodCloseCogs => new[]
                {
                    L(1, "911", a.Amount, 0, "Kết chuyển giá vốn"),
                    L(2, "632", 0, a.Amount, "632")
                },

                AccountingTrigger.PeriodCloseSellingExpense => new[]
                {
                    L(1, "911", a.Amount, 0, "Kết chuyển chi phí bán hàng"),
                    L(2, "641", 0, a.Amount, "641")
                },

                AccountingTrigger.PeriodCloseAdminExpense => new[]
                {
                    L(1, "911", a.Amount, 0, "Kết chuyển chi phí QLDN"),
                    L(2, "642", 0, a.Amount, "642")
                },

                AccountingTrigger.PeriodCloseProfit => new[]
                {
                    L(1, "911", a.Amount, 0, "Kết chuyển lãi"),
                    L(2, "421", 0, a.Amount, "421")
                },

                AccountingTrigger.PeriodCloseLoss => new[]
                {
                    L(1, "421", a.Amount, 0, "Kết chuyển lỗ"),
                    L(2, "911", 0, a.Amount, "911")
                },

                _ => Array.Empty<JournalPreviewLine>()
            };
        }

        private static IReadOnlyList<JournalPreviewLine> AllocateOh(AccountingAmounts a, string note)
        {
            var dep = a.Amount;
            var elec = a.VatAmount;
            var other331 = a.CogsAmount;
            var lines = new List<JournalPreviewLine>();
            int n = 1;
            if (dep > 0)
            {
                lines.Add(L(n++, "154", dep, 0, $"Khấu hao máy (627→154). {note}"));
                lines.Add(L(n++, "214", 0, dep, "Phân bổ khấu hao TSCĐ"));
            }
            if (elec > 0)
            {
                lines.Add(L(n++, "154", elec, 0, $"Điện SX (627→154). {note}"));
                lines.Add(L(n++, "331", 0, elec, "Công nợ điện/NVL phụ trợ"));
            }
            if (other331 > 0)
            {
                lines.Add(L(n++, "154", other331, 0, $"CPQL nhà máy phân bổ (627→154). {note}"));
                lines.Add(L(n++, "331", 0, other331, "Phải trả chi phí chung"));
            }
            return lines;
        }

        private static IReadOnlyList<JournalPreviewLine> SalesInvoice(AccountingAmounts a, string note)
        {
            var net = a.Amount;
            var vat = a.VatAmount;
            var total = net + vat;
            return new[]
            {
                L(1, "131", total, 0, $"Ghi nhận công nợ bán hàng. {note}"),
                L(2, "511", 0, net, "Doanh thu chưa thuế"),
                L(3, "3331", 0, vat, "Thuế GTGT đầu ra")
            };
        }

        private static IReadOnlyList<JournalPreviewLine> SalesReturnRevenue(AccountingAmounts a, string note)
        {
            var net = a.Amount;
            var vat = a.VatAmount;
            var total = net + vat;
            return new[]
            {
                L(1, "511", net, 0, $"Hàng bán bị trả — giảm DT. {note}"),
                L(2, "3331", vat, 0, "Giảm thuế GTGT đầu ra"),
                L(3, "131", 0, total, "Giảm phải thu")
            };
        }

        public static string TriggerDisplayName(AccountingTrigger t) => t switch
        {
            AccountingTrigger.PurchaseOrderApprovedNoEntry => "Duyệt đơn mua — chưa hạch toán",
            AccountingTrigger.GoodsReceiptToWarehouse => "Nhận hàng nhập kho",
            AccountingTrigger.SupplierPaymentCash => "Thanh toán NCC (tiền mặt)",
            AccountingTrigger.SupplierPaymentBank => "Thanh toán NCC (chuyển khoản)",
            AccountingTrigger.IssueMaterialToProduction => "Xuất kho NVL cho lệnh SX",
            AccountingTrigger.AccrueDirectLabor => "Ghi nhận nhân công trực tiếp",
            AccountingTrigger.AllocateMachineOverhead => "Phân bổ chi phí máy / SX chung",
            AccountingTrigger.FinishedGoodsReceipt => "Nhập kho thành phẩm",
            AccountingTrigger.SalesInvoiceAndDelivery => "Giao hàng + xuất hóa đơn",
            AccountingTrigger.CostOfGoodsSold => "Ghi nhận giá vốn (đồng thời với bán)",
            AccountingTrigger.CustomerPaymentCash => "Khách trả tiền mặt",
            AccountingTrigger.CustomerPaymentBank => "Khách chuyển khoản",
            AccountingTrigger.QcRejectScrapToReceivable => "Hàng hỏng QC — bồi thường (138)",
            AccountingTrigger.QcRejectScrapToExpense => "Hàng hỏng QC — chi phí (632)",
            AccountingTrigger.MaterialLossOverNorm => "Hao hụt vượt định mức",
            AccountingTrigger.SalesReturn => "Khách trả hàng — điều chỉnh DT & thuế",
            AccountingTrigger.SalesReturnRestock => "Khách trả hàng — nhập lại kho",
            AccountingTrigger.EarlyPaymentDiscount => "Chiết khấu thanh toán sớm",
            AccountingTrigger.VolumeRebate => "Chiết khấu thương mại theo sản lượng",
            AccountingTrigger.PeriodCloseRevenue => "Cuối kỳ: kết chuyển doanh thu",
            AccountingTrigger.PeriodCloseCogs => "Cuối kỳ: kết chuyển giá vốn",
            AccountingTrigger.PeriodCloseSellingExpense => "Cuối kỳ: kết chuyển CP bán hàng",
            AccountingTrigger.PeriodCloseAdminExpense => "Cuối kỳ: kết chuyển CP QLDN",
            AccountingTrigger.PeriodCloseProfit => "Cuối kỳ: lãi → 421",
            AccountingTrigger.PeriodCloseLoss => "Cuối kỳ: lỗ → 421",
            _ => t.ToString()
        };

        public static string FormatMoney(double v) => v.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    }
}
