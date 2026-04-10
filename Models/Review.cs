using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int? ProductId { get; set; }

        public int? ServiceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}