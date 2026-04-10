using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}