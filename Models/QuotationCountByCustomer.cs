using System;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Thống kê số lượng báo giá theo khách hàng
    /// </summary>
    public class QuotationCountByCustomer
    {
        public string CustomerName { get; set; }
        public int QuoteCount { get; set; }
        public double TotalValue { get; set; }
        public DateTime? LastQuoteDate { get; set; }

        public string FormattedCount => $"{QuoteCount} báo giá";
        public string FormattedValue => $"{TotalValue:N0} đ";
    }
}
