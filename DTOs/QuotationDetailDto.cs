using System;
using System.ComponentModel.DataAnnotations;

namespace PhanMemInAnERP.DTOs
{
    /// <summary>
    /// DTO cho chi tiết báo giá (mỗi mức số lượng).
    /// </summary>
    public class QuotationDetailDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Số lượng không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        /// <summary>Tiền giấy (VNĐ)</summary>
        public decimal TienGiay { get; set; }

        /// <summary>Tiền kẽm/màu máy (VNĐ)</summary>
        public decimal TienMuc { get; set; }

        /// <summary>Tiền kẽm (VNĐ)</summary>
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

        /// <summary>Đánh dấu đây có phải là mức số lượng chính không</summary>
        public bool LaNucMucChinh { get; set; } = false;

        /// <summary>
        /// Tính tổng giá thành sản xuất cho detail này.
        /// </summary>
        public decimal TinhTongGiaThanhSanXuat()
        {
            return TienGiay + TienMuc + TienKem + TienCanMang + TienMetalize
                   + TienUV + TienBe + TienKhuonBe + TienDan + TienDay
                   + TienNut + TienThung + TienXeGiao + TienProof;
        }
    }
}
