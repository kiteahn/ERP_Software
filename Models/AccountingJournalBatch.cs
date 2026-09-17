using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhanMemInAnERP.Models
{
    /// <summary>Lô bút toán tự động sau khi kế toán xác nhận từ màn hình xem trước.</summary>
    public class AccountingJournalBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string EventType { get; set; } = "";

        [MaxLength(200)]
        public string? TriggerDocument { get; set; }

        [MaxLength(80)]
        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Posted { get; set; }
        public DateTime? PostedAt { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<AccountingJournalLine> Lines { get; set; } = new List<AccountingJournalLine>();
    }
}
