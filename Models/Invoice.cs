using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string InvoiceNo { get; set; } // Số hoá đơn
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public string RefOrderNo { get; set; } // Số đơn hàng tham chiếu
        public int SalesOrderId { get; set; }
        [ForeignKey("SalesOrderId")]
        public virtual SalesOrder SalesOrder { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        // Thông tin khách hàng (copy từ đơn hàng)
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }

        // Tổng tiền đơn hàng
        public double TotalAmount { get; set; }
        public double SubTotal { get; set; }
        public double VATPercent { get; set; }
        public double VATAmount { get; set; }
        public double GrandTotal { get; set; }

        // Tài khoản kế toán (từ đơn hàng, có thể chỉnh sửa)
        public string? TK_No { get; set; } // Tài khoản nợ
        public string? TK_Co { get; set; } // Tài khoản có

        /// <summary>
        /// Loại tài khoản: Công nợ (131), Tiền mặt (111), Ngân hàng (112)
        /// </summary>
        public string? AccountType { get; set; }

        public string Status { get; set; } = "Chưa thu"; // Chưa thu, Đã thu, Đã hủy
        public DateTime? PaymentDate { get; set; }
        public string Notes { get; set; }

        // Thông tin bổ sung
        public string TemplateNo { get; set; }
        public string Symbol { get; set; }
        public int UserId { get; set; }
        public DateTime DueDate { get; set; }
    }
}
