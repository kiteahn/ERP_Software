using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhanMemInAnERP.DTOs
{
    /// <summary>
    /// DTO để tạo mới báo giá.
    /// </summary>
    public class CreateQuotationDto
    {
        // ========== THÔNG TIN NGƯỜI TẠO ==========
        public int UserId { get; set; }

        // ========== THÔNG TIN KHÁCH HÀNG ==========
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerTaxCode { get; set; }

        // ========== THÔNG TIN SẢN PHẨM ==========
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [MaxLength(200)]
        public string ProductName { get; set; }

        public string ProductDimensions { get; set; }
        public int SoCon { get; set; } = 1;
        public int BuHao { get; set; } = 500;

        // ========== THÔNG TIN VẬT LIỆU ==========
        public string PaperType { get; set; }
        public double PaperGsm { get; set; }
        public double PaperPricePerTon { get; set; }
        public double PrintLength { get; set; }
        public double PrintWidth { get; set; }
        public int ColorCount { get; set; }

        public bool IsLargeMachine { get; set; }
        public double PlatePricePerColorLargeMachine { get; set; }
        public double PlatePricePerColorSmallMachine { get; set; }
        public string LaminationType { get; set; }
        public int LaminationSides { get; set; }
        public double LaminationPrice { get; set; }

        // ========== CHI PHÍ GIA CÔNG KHÁC ==========
        public double DieCutMoldPrice { get; set; }
        public double StringPricePerItem { get; set; }
        public double ButtonPricePerItem { get; set; }
        public double BoxPrice { get; set; }
        public double DeliveryFee { get; set; }
        public double PrintProofFee { get; set; }

        /// <summary>% Lợi nhuận</summary>
        [Range(0, 1000, ErrorMessage = "Lợi nhuận phải từ 0 đến 1000%")]
        public double ProfitMargin { get; set; }

        // ========== THÔNG TIN BÁO GIÁ ==========
        /// <summary>Số ngày hiệu lực</summary>
        public int HieuLucNgay { get; set; } = 30;

        /// <summary>Số ngày giao hàng dự kiến</summary>
        public int DeliveryDays { get; set; } = 7;

        /// <summary>Thời gian giao hàng dự kiến</summary>
        [MaxLength(100)]
        public string ThoiGianGiaoHangDuKien { get; set; }

        /// <summary>Ghi chú</summary>
        public string GhiChu { get; set; }

        // ========== DANH SÁCH CHI TIẾT (CÁC MỨC SỐ LƯỢNG) ==========
        /// <summary>Danh sách chi tiết báo giá - phải có ít nhất 1 mức số lượng</summary>
        [Required(ErrorMessage = "Phải có ít nhất 1 mức số lượng")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 mức số lượng")]
        public List<QuotationDetailDto> ChiTiet { get; set; } = new List<QuotationDetailDto>();
    }

    /// <summary>
    /// DTO để cập nhật trạng thái báo giá.
    /// </summary>
    public class UpdateTrangThaiDto
    {
        [Required]
        public int QuotationId { get; set; }

        [Required]
        public string TrangThaiMoi { get; set; }

        [Required]
        public string NguoiDung { get; set; }
    }

    /// <summary>
    /// DTO để đặt mức số lượng chính.
    /// </summary>
    public class SetMucChinhDto
    {
        [Required]
        public int QuotationId { get; set; }

        [Required]
        public int DetailId { get; set; }
    }

    /// <summary>
    /// Kết quả trả về sau khi tạo báo giá thành công.
    /// </summary>
    public class CreateQuotationResult
    {
        public int QuotationId { get; set; }
        public string QuoteNo { get; set; }
        public int IdMucChinh { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
