using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PhanMemInAnERP.Data;
using PhanMemInAnERP.Helpers;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Services
{
    /// <summary>
    /// Service xử lý các thao tác liên quan đến reset mật khẩu.
    /// Bao gồm: gửi email reset, xác thực token, đổi mật khẩu, admin reset.
    /// </summary>
    public class PasswordResetService
    {
        private readonly AppDbContext _context;

        // Thời gian hết hạn token (15 phút)
        private static readonly TimeSpan TokenExpiration = TimeSpan.FromMinutes(15);

        // SMTP config - nên lưu vào appsettings hoặc database
        private const string SmtpHost = "smtp.example.com";
        private const int SmtpPort = 587;
        private const string SmtpUsername = "noreply@example.com";
        private const string SmtpPassword = "your-smtp-password";
        private const string FromEmail = "noreply@example.com";
        private const string FromDisplayName = "ERP System";

        // Domain cho link reset
        private const string ResetPasswordDomain = "https://your-domain.com";

        public PasswordResetService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gửi email reset mật khẩu.
        /// Tìm user theo email, sinh token và gửi link reset qua email.
        /// Nếu email không tồn tại vẫn trả "Đã gửi" để tránh leak thông tin.
        /// </summary>
        /// <param name="email">Email của user cần reset</param>
        /// <returns>True nếu email được gửi thành công</returns>
        public bool GuiEmailReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            email = email.Trim().ToLowerInvariant();

            // Tìm user theo email
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

            // Nếu không tìm thấy user, vẫn trả "Đã gửi" để tránh leak thông tin
            if (user == null)
            {
                // Ghi log để admin biết có ai cố reset email không tồn tại
                System.Diagnostics.Debug.WriteLine($"[PasswordReset] Không tìm thấy user với email: {email}");
                return true; // Vẫn trả "Đã gửi"
            }

            // Sinh token ngẫu nhiên 48 bytes (64 ký tự base64)
            string token = GenerateSecureToken();

            // Tính thời hạn token
            DateTime ngayTao = DateTime.Now;
            DateTime ngayHetHan = ngayTao.Add(TokenExpiration);

            // Xóa các token cũ chưa sử dụng của user này
            var oldTokens = _context.PasswordResets
                .Where(t => t.UserId == user.Id && !t.DaSuDung)
                .ToList();

            _context.PasswordResets.RemoveRange(oldTokens);

            // Tạo mới token
            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                Token = token,
                NgayTao = ngayTao,
                NgayHetHan = ngayHetHan,
                DaSuDung = false
            };

            _context.PasswordResets.Add(passwordReset);
            _context.SaveChanges();

            // Gửi email
            string resetLink = $"{ResetPasswordDomain}/reset-password?token={token}";
            string emailBody = $@"
Chào {user.FullName},

Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản trên hệ thống ERP.

Vui lòng nhấn vào link sau ��ể đặt lại mật khẩu:
{resetLink}

Link này sẽ hết hạn sau 15 phút.

Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.

Trân trọng,
Hệ thống ERP
";

            try
            {
                SendEmail(user.Email, user.FullName, "Đặt lại mật khẩu", emailBody);
                System.Diagnostics.Debug.WriteLine($"[PasswordReset] Đã gửi email reset cho: {email}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PasswordReset] Lỗi gửi email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xác thực token reset mật khẩu.
        /// </summary>
        /// <param name="token">Token cần xác thực</param>
        /// <returns>True nếu token hợp lệ (tồn tại, chưa sử dụng, chưa hết hạn)</returns>
        public bool XacThucToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var passwordReset = _context.PasswordResets
                .FirstOrDefault(t => t.Token == token);

            if (passwordReset == null)
            {
                return false;
            }

            // Kiểm tra điều kiện
            return passwordReset.ConHieuLuc();
        }

        /// <summary>
        /// Đổi mật khẩu sau khi xác thực token thành công.
        /// </summary>
        /// <param name="token">Token đã được xác thực</param>
        /// <param name="matKhauMoi">Mật khẩu mới (sẽ được hash)</param>
        /// <returns>True nếu đổi mật khẩu thành công</returns>
        /// <exception cref="ArgumentException">Token không hợp lệ</exception>
        public bool DoiMatKhau(string token, string matKhauMoi)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token không được để trống", nameof(token));
            }

            if (string.IsNullOrWhiteSpace(matKhauMoi))
            {
                throw new ArgumentException("Mật khẩu mới không được để trống", nameof(matKhauMoi));
            }

            // Xác thực token trước
            if (!XacThucToken(token))
            {
                throw new ArgumentException("Token không hợp lệ hoặc đã hết hạn");
            }

            var passwordReset = _context.PasswordResets
                .Include(t => t.User)
                .FirstOrDefault(t => t.Token == token);

            if (passwordReset?.User == null)
            {
                throw new ArgumentException("Không tìm thấy thông tin user");
            }

            var user = passwordReset.User;

            // Hash mật khẩu mới bằng BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);

            // Update mật khẩu
            user.Password = hashedPassword;
            // Bỏ cờ bắt buộc đổi mật khẩu
            user.BatBuocDoiMatKhau = false;

            // Đánh dấu token đã sử dụng
            passwordReset.DaSuDung = true;

            // Xóa toàn bộ token cũ của user đó
            var oldTokens = _context.PasswordResets
                .Where(t => t.UserId == user.Id && !t.DaSuDung && t.Id != passwordReset.Id)
                .ToList();

            _context.PasswordResets.RemoveRange(oldTokens);

            _context.SaveChanges();

            System.Diagnostics.Debug.WriteLine($"[PasswordReset] User {user.Username} đã đổi mật khẩu thành công");
            return true;
        }

        /// <summary>
        /// Admin reset mật khẩu cho user.
        /// Chỉ Admin mới có quyền gọi method này.
        /// </summary>
        /// <param name="userId">ID của user cần reset</param>
        /// <param name="adminId">ID của admin thực hiện reset</param>
        /// <returns>Mật khẩu tạm thời (để admin thông báo cho user)</returns>
        /// <exception cref="UnauthorizedAccessException">Khi không có quyền</exception>
        /// <exception cref="ArgumentException">Khi user không tồn tại</exception>
        public string AdminResetMatKhau(int userId, int adminId)
        {
            // Kiểm tra quyền Admin
            if (!AppSession.IsAdmin)
            {
                throw new UnauthorizedAccessException("Chỉ Admin mới có quyền reset mật khẩu");
            }

            var targetUser = _context.Users.Find(userId);
            if (targetUser == null)
            {
                throw new ArgumentException($"Không tìm thấy user với ID: {userId}");
            }

            // Sinh mật khẩu tạm: "In@" + năm hiện tại + 4 số cuối SĐT
            string year = DateTime.Now.Year.ToString();
            string last4Phone = "0000";
            
            if (!string.IsNullOrWhiteSpace(targetUser.PhoneNumber))
            {
                // Lấy 4 số cuối của SĐT (chỉ số)
                string digitsOnly = new string(targetUser.PhoneNumber.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length >= 4)
                {
                    last4Phone = digitsOnly.Substring(digitsOnly.Length - 4);
                }
                else
                {
                    last4Phone = digitsOnly.PadLeft(4, '0');
                }
            }

            string tempPassword = $"In@{year}{last4Phone}";

            // Hash mật khẩu tạm
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            // Update mật khẩu
            targetUser.Password = hashedPassword;
            // Bật cờ bắt buộc đổi mật khẩu
            targetUser.BatBuocDoiMatKhau = true;

            _context.SaveChanges();

            // Ghi AuditLog
            var auditLog = new AuditLog
            {
                ThoiGian = DateTime.Now,
                NguoiThucHien = AppSession.CurrentUser?.FullName ?? "System",
                UserId = adminId,
                HanhDong = "UPDATE",
                TenBang = "Users",
                IdBanGhi = userId,
                DuLieuCu = $"Mật khẩu của user {targetUser.Username} đã được reset bởi Admin",
                DuLieuMoi = $"Mật khẩu đã reset. User phải đổi mật khẩu khi đăng nhập.",
                DiaChiIP = null
            };
            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();

            System.Diagnostics.Debug.WriteLine($"[PasswordReset] Admin {adminId} đã reset mật khẩu cho user {userId}");
            return tempPassword;
        }

        /// <summary>
        /// Sinh token ngẫu nhiên bảo mật.
        /// </summary>
        private string GenerateSecureToken()
        {
            byte[] tokenBytes = new byte[48]; // 48 bytes = 64 ký tự base64
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }

        /// <summary>
        /// Gửi email qua SMTP.
        /// </summary>
        private void SendEmail(string toEmail, string toName, string subject, string body)
        {
            using var client = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(FromEmail, FromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(new MailAddress(toEmail, toName));

            client.Send(mailMessage);
        }

        /// <summary>
        /// Dọn dẹp các token đã hết hạn.
        /// Nên chạy định kỳ ( VD: mỗi ngày ).
        /// </summary>
        /// <returns>Số lượng token đã xóa</returns>
        public int CleanupExpiredTokens()
        {
            var expiredTokens = _context.PasswordResets
                .Where(t => t.NgayHetHan <= DateTime.Now || t.DaSuDung)
                .ToList();

            int count = expiredTokens.Count;

            _context.PasswordResets.RemoveRange(expiredTokens);
            _context.SaveChanges();

            System.Diagnostics.Debug.WriteLine($"[PasswordReset] Đã dọn dẹp {count} token hết hạn");
            return count;
        }

        /// <summary>
        /// Lấy thông tin user từ token.
        /// </summary>
        /// <param name="token">Token reset</param>
        /// <returns>User hoặc null</returns>
        public User GetUserByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var passwordReset = _context.PasswordResets
                .Include(t => t.User)
                .FirstOrDefault(t => t.Token == token);

            return passwordReset?.User;
        }
    }
}