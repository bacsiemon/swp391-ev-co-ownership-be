using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.MaintenanceDTOs
{
    public class CreateMaintenanceRequest
    {
        public int VehicleId { get; set; }
        public int? BookingId { get; set; }
        public EMaintenanceType MaintenanceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string ServiceProvider { get; set; } = string.Empty;
        public DateOnly ServiceDate { get; set; }
        public int? OdometerReading { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateMaintenanceRequest
    {
        public EMaintenanceType? MaintenanceType { get; set; }
        public string? Description { get; set; }
        public decimal? Cost { get; set; }
        public string? ServiceProvider { get; set; }
        public DateOnly? ServiceDate { get; set; }
        public int? OdometerReading { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPaid { get; set; }
    }

    public class MaintenanceResponse
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int? BookingId { get; set; }
        public EMaintenanceType MaintenanceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public bool IsPaid { get; set; }
        public string ServiceProvider { get; set; } = string.Empty;
        public DateOnly ServiceDate { get; set; }
        public int? OdometerReading { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MaintenanceStatisticsResponse
    {
        public int TotalMaintenances { get; set; }
        public int PaidMaintenances { get; set; }
        public int UnpaidMaintenances { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
        public Dictionary<EMaintenanceType, int> MaintenanceTypeCount { get; set; } = new();
        public Dictionary<EMaintenanceType, decimal> MaintenanceTypeCost { get; set; } = new();
    }

    public class VehicleMaintenanceHistoryResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public List<MaintenanceResponse> MaintenanceHistory { get; set; } = new();
        public decimal TotalMaintenanceCost { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextScheduledMaintenance { get; set; }
    }
}
