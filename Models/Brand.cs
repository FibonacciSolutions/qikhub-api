using System;
using System.ComponentModel.DataAnnotations;

namespace QikHubAPI.Models
{
    public class Brand
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

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}