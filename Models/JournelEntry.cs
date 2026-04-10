using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public string AccountDebit { get; set; } = string.Empty;

        [Required]
        public string AccountCredit { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string Narration { get; set; } = string.Empty;

        public int? OrderId { get; set; }

        public int? ServiceBookingId { get; set; }
    }
}