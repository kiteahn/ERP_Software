using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Lưu trữ sản phẩm hoàn thành sau khi sản xuất - tương đương "Nhập kho sản xuất"
    /// </summary>
    public class ProductionImport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã phiếu nhập kho sản xuất</summary>
        [Required, MaxLength(50)]
        public string TicketNo { get; set; } = "";

        /// <summary>Ngày nhập kho</summary>
        public DateTime ImportDate { get; set; } = DateTime.Now;

        /// <summary>Thuộc lệnh sản xuất nào</summary>
        public int ProductionOrderId { get; set; }
        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder ProductionOrder { get; set; }

        /// <summary>Thuộc đơn hàng bán hàng nào (nếu có)</summary>
        public int? SalesOrderId { get; set; }
        [ForeignKey("SalesOrderId")]
        public virtual SalesOrder SalesOrder { get; set; }

        /// <summary>Tên sản phẩm</summary>
        [MaxLength(200)]
        public string ProductName { get; set; } = "";

        /// <summary>Khách hàng</summary>
        [MaxLength(200)]
        public string CustomerName { get; set; } = "";

        /// <summary>Số lượng nhập kho</summary>
        public int Quantity { get; set; }

        /// <summary>Đơn vị tính</summary>
        [MaxLength(50)]
        public string Unit { get; set; } = "Cái";

        /// <summary>Đơn giá</summary>
        public double UnitPrice { get; set; }

        /// <summary>Tổng tiền</summary>
        public double TotalPrice { get; set; }

        /// <summary>Người tạo phiếu</summary>
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /// <summary>Ghi chú</summary>
        public string Notes { get; set; } = "";

        /// <summary>Trạng thái: Chưa xuất / Đã xuất</summary>
        public string Status { get; set; } = "Chưa xuất";

        /// <summary>Ngày xuất kho (nếu đã xuất)</summary>
        public DateTime? ExportDate { get; set; }

        /// <summary>Số phiếu xuất kho bán hàng</summary>
        [MaxLength(50)]
        public string ExportTicketNo { get; set; } = "";
    }
}
