using System;

namespace PhanMemInAnERP.DTOs
{
    /// <summary>
    /// DTO để lưu bản nháp báo giá.
    /// </summary>
    public class SaveDraftDto
    {
        /// <summary>ID khách hàng đang nhập (nullable)</summary>
        public int? CustomerId { get; set; }

        /// <summary>Tên bản nháp do người dùng đặt</summary>
        public string TenNhap { get; set; }

        /// <summary>ID bản nháp cũ nếu muốn cập nhật (null = tạo mới)</summary>
        public int? DraftId { get; set; }
    }

    /// <summary>
    /// DTO trả về thông tin bản nháp.
    /// </summary>
    public class DraftInfoDto
    {
        public int Id { get; set; }
        public string TenNhap { get; set; }
        public string CustomerName { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string ThoiGianNhap { get; set; } // ví dụ: "5 phút trước", "2 giờ trước"
    }

    /// <summary>
    /// DTO trả về kết quả lưu bản nháp.
    /// </summary>
    public class SaveDraftResult
    {
        public int DraftId { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}