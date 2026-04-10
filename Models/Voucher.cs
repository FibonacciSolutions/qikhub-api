using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string DiscountType { get; set; } = string.Empty; // Percentage, Fixed

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int CreatedByAdminId { get; set; }
    }
}