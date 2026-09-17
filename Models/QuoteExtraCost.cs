using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace PhanMemInAnERP.Models
{
    public class QuoteExtraCost : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        private string _itemName = "";
        public string ItemName
        {
            get => _itemName;
            set { if (_itemName == value) return; _itemName = value; OnPropertyChanged(); }
        }

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set { if (_quantity == value) return; _quantity = value; OnPropertyChanged(); }
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get => _unitPrice;
            set { if (_unitPrice == value) return; _unitPrice = value; OnPropertyChanged(); }
        }

        public double TotalPrice { get; set; }

        public int QuotationId { get; set; }
        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
