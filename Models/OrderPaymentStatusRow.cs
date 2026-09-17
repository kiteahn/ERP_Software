using System;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Một dòng tổng hợp: đơn hàng → hóa đơn → đã thu / còn nợ (chỉ hiển thị).
    /// </summary>
    public class OrderPaymentStatusRow
    {
        public string OrderNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public string InvoiceNo { get; set; } = "";
        public double OrderTotal { get; set; }
        public double InvoiceTotal { get; set; }
        public double PaidAmount { get; set; }
        public double Remaining { get; set; }
        /// <summary>Đã thanh toán / Thanh toán một phần / Chưa thanh toán / Chưa có hóa đơn</summary>
        public string PaymentStatus { get; set; } = "";
    }
}
