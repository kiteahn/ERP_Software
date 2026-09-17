using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class SalesOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? QuotationId { get; set; }
        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; }
        public int UserId { get; set; } 
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        /// <summary>Ngày (dự kiến) giao hàng cho khách.</summary>
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);
        /// <summary>Thời gian (giờ:phút) giao hàng dự kiến.</summary>
        public TimeSpan? DeliveryTime { get; set; }

        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }

        public string ProductName { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalAmount { get; set; }

        public string PaymentTerm { get; set; } // Tiền mặt, CK, COD, Công nợ
        public int CreditDays { get; set; }
        public string Status { get; set; } = "Chưa giao";

        public string Dimensions { get; set; }
        public string Material { get; set; }

        /// <summary>Loại bán hàng: Nội địa / Xuất khẩu / Dịch vụ.</summary>
        public string SaleType { get; set; } = "Nội địa";

        /// <summary>Tài khoản nợ (TK_No) - chuỗi 20 ký tự, cho phép null.</summary>
        public string? TK_No { get; set; }

        /// <summary>Tài khoản có (TK_Co) - chuỗi 20 ký tự, cho phép null.</summary>
        public string? TK_Co { get; set; }

        public virtual ProductionOrder ProductionOrder { get; set; }
    }
}