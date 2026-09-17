using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class Quotation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // ========== THÔNG TIN NGƯỜI TẠO ==========
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required, MaxLength(50)]
        public string QuoteNo { get; set; } = "BG-" + DateTime.Now.ToString("yyMMddHHmm");
        public DateTime QuoteDate { get; set; } = DateTime.Now;

        // ========== CỘT CÓ TRONG DB ==========
        /// <summary>Số ngày hiệu lực của báo giá (mặc định 30)</summary>
        public int ValidityDays { get; set; } = 30;

        /// <summary>Thời gian giao hàng dự kiến (ngày)</summary>
        public int DeliveryDays { get; set; } = 7;

        // ========== THÔNG TIN KHÁCH HÀNG ==========
        [Required, MaxLength(200)]
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerTaxCode { get; set; }

        // ========== THÔNG TIN SẢN PHẨM ==========
        [Required, MaxLength(200)]
        public string ProductName { get; set; }
        public string ProductDimensions { get; set; }
        public int SoCon { get; set; } = 1;
        public int BuHao { get; set; } = 500;

        /// <summary>Số lượng mặc định</summary>
        public int Quantity { get; set; }

        // ========== THÔNG TIN VẬT LIỆU ==========
        public string PaperType { get; set; }
        public double PaperGsm { get; set; }
        public double PaperPricePerTon { get; set; }
        public double PrintLength { get; set; }
        public double PrintWidth { get; set; }
        public int ColorCount { get; set; }

        public bool IsLargeMachine { get; set; }
        public double PlatePricePerColorLargeMachine { get; set; } = 0;
        public double PlatePricePerColorSmallMachine { get; set; } = 0;
        public string LaminationType { get; set; }
        public int LaminationSides { get; set; }
        public double LaminationPrice { get; set; }

        // ========== CHI PHÍ GIA CÔNG KHÁC ==========
        public double DieCutMoldPrice { get; set; }
        public double StringPricePerItem { get; set; }
        public double ButtonPricePerItem { get; set; }
        public double BoxPrice { get; set; }
        public double DeliveryFee { get; set; }
        public double PrintProofFee { get; set; } = 0;

        /// <summary>% Lợi nhuận</summary>
        public double ProfitMargin { get; set; }

        // ========== KẾT QUẢ ==========
        /// <summary>Tổng giá thành sản xuất</summary>
        public double TotalProductionCost { get; set; }
        /// <summary>Giá mỗi cái của mức chính</summary>
        public double QuotedUnitPrice { get; set; }
        /// <summary>Tổng giá báo khách của mức chính</summary>
        public double TotalOrderValue { get; set; }

        // ========== CỘT BỔ SUNG (không có trong DB - đánh dấu NotMapped) ==========
        /// <summary>Người tạo báo giá — không lưu DB.</summary>
        [NotMapped]
        public string NguoiTao { get; set; }

        /// <summary>Ngày tạo — không lưu DB.</summary>
        [NotMapped]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Ghi chú — không lưu DB.</summary>
        [NotMapped]
        public string GhiChu { get; set; }

        /// <summary>Trạng thái báo giá — không lưu DB.</summary>
        [NotMapped]
        public string TrangThai { get; set; } = "Chờ duyệt";

        /// <summary>Thời gian hiệu lực (alias cho ValidityDays) — không lưu DB.</summary>
        [NotMapped]
        public int HieuLucNgay { get; set; } = 30;

        /// <summary>Thời gian giao hàng dự kiến (text) — không lưu DB.</summary>
        [NotMapped]
        public string ThoiGianGiaoHangDuKien { get; set; }

        /// <summary>ID mức chính — không lưu DB.</summary>
        [NotMapped]
        public int? IdMucChinh { get; set; }

        // ========== QUAN HỆ ==========
        /// <summary>Danh sách chi tiết báo giá (các mức số lượng)</summary>
        public virtual ICollection<QuotationDetail> ChiTiet { get; set; }

        /// <summary>Danh sách chi phí phát sinh</summary>
        public virtual ICollection<QuoteExtraCost> ExtraCosts { get; set; }

        public virtual SalesOrder SalesOrder { get; set; }
    }
}
