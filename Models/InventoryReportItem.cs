using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhanMemInAnERP.Models
{
    public class InventoryReportItem
    {
        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Unit { get; set; } = "";
        public double TotalImport { get; set; }
        public double TotalExport { get; set; }
        public double CurrentStock { get; set; }
        public double MinStock { get; set; }
        public double AvgPrice { get; set; }
        public double TotalValue => CurrentStock * AvgPrice;
        public bool IsLowStock => CurrentStock <= MinStock;

        /// <summary>Thời điểm phiếu nhập hoặc xuất gần nhất (lấy max).</summary>
        public DateTime? LastMovementTime { get; set; }
    }
}
