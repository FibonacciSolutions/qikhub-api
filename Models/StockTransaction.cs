using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class StockTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public string Notes { get; set; } = string.Empty;

        public int? OrderId { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}