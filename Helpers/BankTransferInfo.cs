using System.IO;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>
    /// Thông tin tài khoản nhận chuyển khoản (chỉnh sửa tại đây nếu đổi STK/NH).
    /// </summary>
    public static class BankTransferInfo
    {
        public const string AccountNumber = "1031899056";
        public const string BankName = "Vietcombank (VietQR / Napas 247)";
        public const string AccountHolder = "PHAM TIEN DAT";

        /// <summary>Đặt file QR_CK.jpg trong thư mục Assets (cùng thư mục exe) hoặc cạnh file exe.</summary>
        public static string? ResolveQrImagePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Assets", "QR_CK.jpg"),
                Path.Combine(baseDir, "QR_CK.jpg"),
                Path.Combine(baseDir, "Assets", "QR_CK.png"),
                Path.Combine(baseDir, "QR_CK.png")
            };
            foreach (var p in candidates)
            {
                if (File.Exists(p))
                    return p;
            }
            return null;
        }
    }
}
