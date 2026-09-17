using System;

namespace PhanMemInAnERP.Models
{
    public class CustomerDebtSummary
    {
        public string CustomerName { get; set; }
        public double TotalInvoiced { get; set; }
        /// <summary>Chỉ phiếu thu gắn đúng số HĐ của khách (khớp với tổng phát sinh).</summary>
        public double TotalPaid { get; set; }
        /// <summary>Số nợ còn phải thu (không âm — không hiển thị «trả dư» như nợ âm).</summary>
        public double CurrentBalance => Math.Max(0, TotalInvoiced - TotalPaid);
        
        public string FormattedBalance => CurrentBalance.ToString("N0") + " VNĐ";
        public string FormattedPaid => TotalPaid.ToString("N0") + " VNĐ";
        public string FormattedInvoiced => TotalInvoiced.ToString("N0") + " VNĐ";
    }
}
