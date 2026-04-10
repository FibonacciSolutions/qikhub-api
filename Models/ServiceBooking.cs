using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class ServiceBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public DateTime BookingDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public bool AdminVerified { get; set; } = false;

        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [ForeignKey("ProviderId")]
        public virtual ServicePro? Provider { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }
    }
}