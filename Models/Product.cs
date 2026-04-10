using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ThumbImage { get; set; } = string.Empty;

        public string BannerImage { get; set; } = string.Empty;

        [MaxLength(200)]
        public string MetaTitle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string MetaKeywords { get; set; } = string.Empty;

        [MaxLength(500)]
        public string MetaDescription { get; set; } = string.Empty;

        public string Weight { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        public string ProductType { get; set; } = "Physical";

        public bool HasVariants { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public bool IsFlashSale { get; set; } = false;

        public DateTime? FlashSaleStart { get; set; }

        public DateTime? FlashSaleEnd { get; set; }

        public int FlashSaleDiscount { get; set; } = 0;

        public bool AdminApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}