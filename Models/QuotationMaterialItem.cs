using System;
using System.ComponentModel.DataAnnotations;

namespace PhanMemInAnERP.Models
{
    /// <summary>
    /// Item vật tư được tính toán từ Báo giá để xuất kho
    /// </summary>
    public class QuotationMaterialItem
    {
        public string MaterialName { get; set; }
        public string MaterialInfo { get; set; } // Chi tiết: "Giấy 250 gsm"
        public double RequiredQuantity { get; set; } // Số lượng cần xuất
        public string Unit { get; set; } = "Kg";
        public double AvailableStock { get; set; } // Tồn kho hiện tại
        
        public string DisplayText => $"{MaterialName} - Cần: {RequiredQuantity:N0} {Unit} (Tồn: {AvailableStock:N0})";
    }
}
