using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        public int Stock { get; set; }

        public int MinimumReorderLevel { get; set; } = 5;

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}