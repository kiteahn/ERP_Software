using System;
using System.IO;
using System.Text.Json;

namespace PhanMemInAnERP.Helpers
{
    /// <summary>Lưu đơn giá kẽm/màu mặc định (máy lớn / máy nhỏ) vào %LocalAppData%\PhanMemInAnERP\</summary>
    public static class PlatePriceDefaultsStore
    {
        public const double FallbackLargePerColor = 100_000;
        public const double FallbackSmallPerColor = 60_000;

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhanMemInAnERP",
            "plate_price_defaults.json");

        public static (double LargePerColor, double SmallPerColor) Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return (FallbackLargePerColor, FallbackSmallPerColor);
                var json = File.ReadAllText(FilePath);
                var dto = JsonSerializer.Deserialize<Dto>(json);
                if (dto == null)
                    return (FallbackLargePerColor, FallbackSmallPerColor);
                double l = dto.LargePerColor > 0 ? dto.LargePerColor : FallbackLargePerColor;
                double s = dto.SmallPerColor > 0 ? dto.SmallPerColor : FallbackSmallPerColor;
                return (l, s);
            }
            catch
            {
                return (FallbackLargePerColor, FallbackSmallPerColor);
            }
        }

        public static void Save(double largePerColor, double smallPerColor)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                var dto = new Dto
                {
                    LargePerColor = largePerColor > 0 ? largePerColor : FallbackLargePerColor,
                    SmallPerColor = smallPerColor > 0 ? smallPerColor : FallbackSmallPerColor
                };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(dto, JsonOpts));
            }
            catch
            {
                // bỏ qua — form vẫn dùng giá đã nhập
            }
        }

        private sealed class Dto
        {
            public double LargePerColor { get; set; }
            public double SmallPerColor { get; set; }
        }
    }
}
