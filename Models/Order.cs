using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int SellerId { get; set; }

        public int? DeliveryPersonId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellerEarnings { get; set; }

        public string Status { get; set; } = "Pending";

        public bool AdminVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [ForeignKey("DeliveryPersonId")]
        public virtual DeliveryPerson? DeliveryPerson { get; set; }
    }
}