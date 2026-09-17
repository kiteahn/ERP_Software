using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ReceiptNo { get; set; } // Số phiếu thu
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public int? InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        public string PaymentMethod { get; set; } // Tiền mặt / Chuyển khoản
        public string BankAccount { get; set; }
        public string BankName { get; set; }

        public string CustomerName { get; set; }
        public string RefInvoiceNo { get; set; } // Tham chiếu Hóa đơn để trừ nợ

        public double Amount { get; set; }
        public string Notes { get; set; }
        public string StaffName { get; set; }

        // Loại tài khoản được chọn từ hoá đơn
        public string? AccountType { get; set; }
    }
}