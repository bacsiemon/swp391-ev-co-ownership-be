using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.Repositories.DTOs.VehicleDTOs
{
    public class CreateVehicleDto
    {
        [Required]
        public string Make { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        [Required]
        public string LicensePlate { get; set; } = string.Empty;

        public string? VinNumber { get; set; }

        public string? Color { get; set; }

        public decimal PurchasePrice { get; set; }

        public decimal CurrentValue { get; set; }

        public string? Description { get; set; }
    }
}