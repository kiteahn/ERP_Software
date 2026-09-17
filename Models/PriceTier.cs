namespace PhanMemInAnERP.Models
{
    public class PriceTier
    {
        public int Quantity { get; set; }
        public double CostPerItem { get; set; }
        public double QuotedUnitPrice { get; set; }
        public double TotalValue { get; set; }

        /// <summary>Tooltip đơn giản: công thức tính giá vốn và đơn giá.</summary>
        public string DetailTooltip { get; set; } = "";
    }
}
