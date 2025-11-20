using System.ComponentModel.DataAnnotations;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs
{
    public class AddFundsRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VehicleId must be a positive integer")]
        public int VehicleId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}