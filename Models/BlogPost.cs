using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class BlogPost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Excerpt { get; set; } = string.Empty;

        public string FeaturedImage { get; set; } = string.Empty;

        [MaxLength(200)]
        public string MetaTitle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string MetaKeywords { get; set; } = string.Empty;

        [MaxLength(500)]
        public string MetaDescription { get; set; } = string.Empty;

        public int ViewCount { get; set; } = 0;

        public bool IsPublished { get; set; } = false;

        public DateTime PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CategoryId")]
        public virtual BlogCategory? Category { get; set; }
    }
}