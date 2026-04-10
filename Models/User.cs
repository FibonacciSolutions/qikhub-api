using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QikHubAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        public int? VerifiedByAdminId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditBalance { get; set; } = 0;

        // Navigation properties
        public virtual Seller? Seller { get; set; }
        public virtual DeliveryPerson? DeliveryPerson { get; set; }
    }
}