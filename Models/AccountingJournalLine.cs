using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    public class AccountingJournalLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BatchId { get; set; }
        [ForeignKey("BatchId")]
        public virtual AccountingJournalBatch Batch { get; set; } = null!;

        public int LineNo { get; set; }

        [Required, MaxLength(20)]
        public string AccountCode { get; set; } = "";

        [MaxLength(200)]
        public string AccountName { get; set; } = "";

        public double Debit { get; set; }
        public double Credit { get; set; }

        [MaxLength(500)]
        public string? Explanation { get; set; }
    }
}
