using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string FullName { get; set; }
        public string Role { get; set; } // Admin, NhanVien
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        /// <summary>Buộc user đổi mật khẩu khi đăng nhập (do Admin reset) — không lưu DB.</summary>
        [NotMapped]
        public bool BatBuocDoiMatKhau { get; set; } = false;

        /// <summary>Số phiếu thu do nhân viên này lập (khớp Họ tên hoặc tên đăng nhập với Payment.StaffName) — không lưu DB.</summary>
        [NotMapped]
        public int PaymentReceiptCount { get; set; }

        /// <summary>Tổng tiền đã thu (VNĐ) — không lưu DB.</summary>
        [NotMapped]
        public double PaymentTotalAmount { get; set; }
    }
}
