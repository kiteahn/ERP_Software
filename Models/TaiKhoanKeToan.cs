using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Danh mục tài khoản kế toán theo Thông tư 200.
    /// </summary>
    public class TaiKhoanKeToan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã tài khoản (111, 112, 131, 152, 511, 5111, 621, 911...)</summary>
        [Required, MaxLength(20)]
        public string MaTK { get; set; } = "";

        /// <summary>Tên tài khoản</summary>
        [Required, MaxLength(200)]
        public string TenTK { get; set; } = "";

        /// <summary>Loại tài khoản: TÀI SẢN | NGUỒN VỐN | DOANH THU | CHI PHÍ | XÁC ĐỊNH KQ</summary>
        [MaxLength(50)]
        public string LoaiTK { get; set; } = "";

        /// <summary>Mã TK cấp cha (VD: "511" là cha của "5111", "5112")</summary>
        [MaxLength(20)]
        public string? MaTKCha { get; set; }

        /// <summary>
        /// True = TK tăng bên Nợ (Tài sản, Chi phí).
        /// False = TK tăng bên Có (Nguồn vốn, Doanh thu).
        /// </summary>
        public bool LoTang { get; set; }

        /// <summary>
        /// False = TK không có số dư cuối kỳ (511, 632, 641, 642, 911...).
        /// True = TK có số dư (111, 112, 131, 152, 154, 155, 331, 334...).
        /// </summary>
        public bool CoSoDu { get; set; } = true;

        /// <summary>TK có đang hoạt động không</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Ghi chú</summary>
        public string? GhiChu { get; set; }
    }

    /// <summary>
    /// Sổ cái - ghi nhận phát sinh từng tài khoản theo thời gian.
    /// </summary>
    public class SoCai
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Mã tài khoản</summary>
        [Required, MaxLength(20)]
        public string MaTK { get; set; } = "";

        /// <summary>Ngày hạch toán</summary>
        public DateTime NgayHachToan { get; set; }

        /// <summary>Số chứng từ</summary>
        [MaxLength(50)]
        public string? SoChungTu { get; set; }

        /// <summary>Diễn giải</summary>
        [MaxLength(500)]
        public string? DienGiai { get; set; }

        /// <summary>Số tiền Nợ</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienNo { get; set; } = 0;

        /// <summary>Số tiền Có</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienCo { get; set; } = 0;

        /// <summary>Lô bút toán liên quan</summary>
        public int? JournalBatchId { get; set; }

        [ForeignKey("JournalBatchId")]
        public virtual AccountingJournalBatch? JournalBatch { get; set; }

        /// <summary>Đối tượng (mã KH, mã NCC...)</summary>
        [MaxLength(100)]
        public string? DoiTuong { get; set; }

        /// <summary>Người tạo</summary>
        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        /// <summary>Ngày tạo</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
