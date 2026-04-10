using System;
using System.ComponentModel.DataAnnotations;

namespace QikHubAPI.Models
{
    public class Advertise
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string LinkUrl { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Position { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}