using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Bảng lưu thông tin yêu cầu đặt lại mật khẩu.
    /// Mỗi yêu cầu có token duy nhất với thời hạn 15 phút.
    /// </summary>
    [Table("PasswordReset")]
    public class PasswordReset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>ID của user yêu cầu reset</summary>
        public int UserId { get; set; }

        /// <summary>Token duy nhất để xác thực yêu cầu</summary>
        [Required]
        [MaxLength(128)]
        public string Token { get; set; }

        /// <summary>Thời gian tạo yêu cầu (mặc định GETDATE())</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Thời gian hết hạn của token (NgayTao + 15 phút)</summary>
        public DateTime NgayHetHan { get; set; }

        /// <summary>Token đã được sử dụng chưa</summary>
        public bool DaSuDung { get; set; } = false;

        /// <summary>User liên kết với yêu cầu reset (navigation property)</summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /// <summary>
        /// Kiểm tra xem token có còn hiệu lực không.
        /// </summary>
        /// <returns>True nếu token còn hiệu lực (chưa sử dụng và chưa hết hạn)</returns>
        public bool ConHieuLuc()
        {
            return !DaSuDung && NgayHetHan > DateTime.Now;
        }

        /// <summary>
        /// Kiểm tra xem token có hết hạn không.
        /// </summary>
        /// <returns>True nếu đã hết hạn</returns>
        public bool DaHetHan()
        {
            return NgayHetHan <= DateTime.Now;
        }
    }
}
