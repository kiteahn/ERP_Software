using System;

namespace PhanMemInAnERP.Models
{
    public class DebtReportItem
    {
        public string CustomerName { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public double TotalAmount { get; set; }
        public double PaidAmount { get; set; }
        public double RemainingDebt { get; set; }
        public string Status { get; set; } 
    }
}