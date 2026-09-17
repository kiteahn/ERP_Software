using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Chi tiết báo giá - lưu các mức số lượng khác nhau với giá tương ứng.
    /// Mỗi báo giá có thể có nhiều mức số lượng.
    /// </summary>
    public class QuotationDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int QuotationId { get; set; }

        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; }

        /// <summary>Số lượng cho mức này</summary>
        [Required]
        public int SoLuong { get; set; }

        /// <summary>Tiền giấy (VNĐ)</summary>
        public decimal TienGiay { get; set; }

        /// <summary>Tiền kẽm/màu máy (VNĐ)</summary>
        public decimal TienMuc { get; set; }

        /// <summary>Tiền kẽm (VNĐ) - alias cho TienMuc</summary>
        public decimal TienKem { get; set; }

        /// <summary>Tiền cán màng (VNĐ)</summary>
        public decimal TienCanMang { get; set; }

        /// <summary>Tiền Metalize (VNĐ)</summary>
        public decimal TienMetalize { get; set; }

        /// <summary>Tiền UV (VNĐ)</summary>
        public decimal TienUV { get; set; }

        /// <summary>Tiền bế (VNĐ)</summary>
        public decimal TienBe { get; set; }

        /// <summary>Tiền khuôn bế (VNĐ)</summary>
        public decimal TienKhuonBe { get; set; }

        /// <summary>Tiền dán (VNĐ)</summary>
        public decimal TienDan { get; set; }

        /// <summary>Tiền dây/cái (VNĐ)</summary>
        public decimal TienDay { get; set; }

        /// <summary>Tiền nút/cái (VNĐ)</summary>
        public decimal TienNut { get; set; }

        /// <summary>Tiền thùng (VNĐ)</summary>
        public decimal TienThung { get; set; }

        /// <summary>Tiền xe giao (VNĐ)</summary>
        public decimal TienXeGiao { get; set; }

        /// <summary>Tiền in proof (VNĐ)</summary>
        public decimal TienProof { get; set; }

        /// <summary>Tổng giá thành sản xuất = tổng tất cả khoản tiền</summary>
        public decimal TongGiaThanhSanXuat { get; set; }

        /// <summary>Giá mỗi cái = TongGiaThanhSanXuat / SoLuong * (1 + LoiNhuan/100)</summary>
        public decimal GiaMoiCai { get; set; }

        /// <summary>Giá báo khách (VNĐ)</summary>
        public decimal GiaBaoKhach { get; set; }

        /// <summary>Tổng giá báo khách = GiaMoiCai * SoLuong</summary>
        public decimal TongGiaBaoKhach { get; set; }

        /// <summary>Đánh dấu đây có phải là mức số lượng chính không</summary>
        public bool LaNucMucChinh { get; set; } = false;

        // ========== CỘT THỰC TẾ SAU SẢN XUẤT ==========
        /// <summary>Số lượng thực tế sản xuất (nullable vì có thể chưa hoàn thành)</summary>
        public int? SoLuongThucTe { get; set; }

        /// <summary>Phí gia công thực tế phát sinh (VNĐ)</summary>
        public decimal? PhiGiaCongThucTe { get; set; }

        /// <summary>Lợi nhuận thực tế sau khi hoàn thành sản xuất</summary>
        public decimal? LoiNhuanThucTe { get; set; }

        /// <summary>Ghi chú về lý do chênh lệch giữa dự kiến và thực tế</summary>
        public string GhiChuGiaCong { get; set; }
    }
}
