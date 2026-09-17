using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// [DEPRECATED] Dùng PhieuXuatKho thay thế.
    /// Class này giữ lại để tương thích với code cũ.
    /// </summary>
    [Obsolete("Dùng PhieuXuatKho thay thế")]
    public class ExportTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string TicketNo { get; set; }
        public DateTime ExportDate { get; set; }
        public string Purpose { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int? ProductionOrderId { get; set; }
        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder ProductionOrder { get; set; }

        public int? QuotationId { get; set; }
        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; }

        public int? MaterialId { get; set; } // Cho phép null cho sản phẩm hoàn thành
        [ForeignKey("MaterialId")]
        public virtual Material Material { get; set; }

        public double Quantity { get; set; }
        public string Notes { get; set; }
    }
}