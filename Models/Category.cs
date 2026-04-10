using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

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

        public int? ParentId { get; set; }

        public int Level { get; set; } = 1;

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
    }
}