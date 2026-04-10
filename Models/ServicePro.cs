using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class ServicePro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        public DateTime LicenseExpiry { get; set; }

        public string InsuranceDoc { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionRate { get; set; } = 15;

        public string Status { get; set; } = "Pending";

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}