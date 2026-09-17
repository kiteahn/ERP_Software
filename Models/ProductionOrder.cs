using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class ProductionOrder : ViewModels.BaseViewModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? SalesOrderId { get; set; } // Thuộc đơn hàng nào
        [ForeignKey("SalesOrderId")]
        public virtual SalesOrder SalesOrder { get; set; }

        public int UserId { get; set; } // Người lập lệnh
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required, MaxLength(50)]
        public string OrderNo { get; set; } = ""; // Số lệnh SX

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);

        [Required, MaxLength(200)]
        public string CustomerName { get; set; } = "";

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = "";

        public int Quantity { get; set; }

        [MaxLength(100)]
        public string? Dimensions { get; set; } // Kích thước DxRxC

        [MaxLength(200)]
        public string? Material { get; set; } // Chất liệu

        // ===== CÁC CÔNG ĐOẠN SẢN XUẤT (chọn từ báo giá) =====
        public bool HasPrinting { get; set; } = true;
        public bool HasPlateMaking { get; set; } = true;
        public bool HasLamination { get; set; } = true;
        public bool HasDieCutting { get; set; } = true;
        public bool HasGluing { get; set; } = true;
        public bool HasStringing { get; set; }
        public bool HasButtoning { get; set; }
        public bool HasPackaging { get; set; } = true;

        // ===== THÔNG SỐ IN ĐẦY ĐỦ (copy từ báo giá) =====
        public double PrintLength { get; set; }
        public double PrintWidth { get; set; }
        public int ColorCount { get; set; }
        public bool IsLargeMachine { get; set; }
        public double PlatePricePerColorLarge { get; set; }
        public double PlatePricePerColorSmall { get; set; }
        public string LaminationType { get; set; } = "";
        public int LaminationSides { get; set; }
        public double LaminationPrice { get; set; }
        public double PaperPricePerTon { get; set; }
        public int SoCon { get; set; }
        public int BuHao { get; set; }

        public string? Notes { get; set; }

        [MaxLength(50)]
        private string _status = "Chờ SX";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public virtual ICollection<PhieuXuatKho> PhieuXuatKhos { get; set; }
    }
}