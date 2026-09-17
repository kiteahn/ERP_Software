using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng lưu lịch sử thay đổi dữ liệu (Audit Log).
    /// Theo dõi mọi thao tác CREATE, UPDATE, DELETE trên các bảng được giám sát.
    /// </summary>
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Thời gian thực hiện thao tác (mặc định GETDATE())</summary>
        public DateTime ThoiGian { get; set; } = DateTime.Now;

        /// <summary>Tên người thực hiện thao tác</summary>
        [MaxLength(100)]
        public string NguoiThucHien { get; set; }

        /// <summary>ID của user đang đăng nhập (nullable nếu không có user)</summary>
        public int? UserId { get; set; }

        /// <summary>Hành động: CREATE / UPDATE / DELETE</summary>
        [MaxLength(20)]
        public string HanhDong { get; set; }

        /// <summary>Tên bảng bị tác động</summary>
        [MaxLength(100)]
        public string TenBang { get; set; }

        /// <summary>ID của bản ghi bị tác động</summary>
        public int IdBanGhi { get; set; }

        /// <summary>Dữ liệu cũ trước khi thay đổi (JSON format)</summary>
        public string DuLieuCu { get; set; }

        /// <summary>Dữ liệu mới sau khi thay đổi (JSON format)</summary>
        public string DuLieuMoi { get; set; }

        /// <summary>Địa chỉ IP của máy thực hiện (null cho WPF app)</summary>
        [MaxLength(50)]
        public string DiaChiIP { get; set; }

        /// <summary>Tên đầy đủ của người dùng (navigation property)</summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
