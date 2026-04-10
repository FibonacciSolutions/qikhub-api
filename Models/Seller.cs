using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Seller
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        public string TaxId { get; set; } = string.Empty;

        [Required]
        public string BankAccount { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionRate { get; set; } = 10;

        public string Status { get; set; } = "Pending";

        public DateTime? ApprovedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}