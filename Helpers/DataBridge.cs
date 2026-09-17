using System;
using PhanMemInAnERP.Models;

namespace PhanMemInAnERP.Helpers
{
    public static class DataBridge
    {
        
        public static event Action<Quotation> OnRequestLoadQuote;
        public static event Action<Quotation> OnRequestCreateOrder;
        public static event Action OnDataChanged;  // Thêm event để thông báo khi dữ liệu thay đổi
        public static Quotation SelectedQuotation { get; set; }


        public static void RaiseLoadQuote(Quotation quote)
        {
            OnRequestLoadQuote?.Invoke(quote);
        }

        public static void RaiseCreateOrder(Quotation quote)
        {
            OnRequestCreateOrder?.Invoke(quote);
        }

        /// <summary>
        /// Thông báo cho tất cả các ViewModel rằng dữ liệu đã thay đổi.
        /// Gọi sau khi xóa, thêm hoặc sửa dữ liệu.
        /// </summary>
        public static void RaiseDataChanged()
        {
            OnDataChanged?.Invoke();
        }
    }
}