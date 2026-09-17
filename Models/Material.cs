using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class Material : ViewModels.BaseViewModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } // Mã vật tư (VD: VT-001)

        [Required, MaxLength(200)]
        public string Name { get; set; } // Tên vật tư

        [MaxLength(50)]
        public string Unit { get; set; } // Đơn vị tính (Kg, Ram...)

        [MaxLength(100)]
        public string Category { get; set; } // Phân loại (Giấy, Mực...)

        public double MinStock { get; set; } // Tồn tối thiểu cảnh báo

        /// <summary>Tồn kho tối thiểu — alias cho MinStock, dùng cho báo cáo. Không lưu DB.</summary>
        [NotMapped]
        public decimal TonKhoToiThieu { get; set; } = 0;

        /// <summary>Số lượng nên đặt thêm = TonKhoToiThieu * 2 - TonKho. Không lưu DB.</summary>
        [NotMapped]
        public decimal DeXuatNhap { get; set; } = 0;

        public string Notes { get; set; }

        // Các cột tự động cập nhật khi Nhập/Xuất
        private double _stockQuantity = 0;
        public double StockQuantity
        {
            get => _stockQuantity;
            set { _stockQuantity = value; OnPropertyChanged(); }
        }

        private double _averagePrice = 0;
        public double AveragePrice
        {
            get => _averagePrice;
            set { _averagePrice = value; OnPropertyChanged(); }
        }

        /// <summary>Không lưu CSDL — giao dịch nhập/xuất gần nhất (tính khi tải kho).</summary>
        [NotMapped]
        public DateTime? LastMovementTime { get; set; }
    }
}
