using System;
using System.ComponentModel.DataAnnotations;

namespace QikHubAPI.Models
{
    public class BlogCategory
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

        public int? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}