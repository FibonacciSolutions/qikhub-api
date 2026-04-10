using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class DeliveryPerson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string VehicleType { get; set; } = string.Empty;

        public string Zone { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}