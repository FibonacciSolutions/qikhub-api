using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerHour { get; set; }

        public int DurationMinutes { get; set; }

        public bool AdminApproved { get; set; } = false;

        [ForeignKey("ProviderId")]
        public virtual ServicePro? Provider { get; set; }
    }
}