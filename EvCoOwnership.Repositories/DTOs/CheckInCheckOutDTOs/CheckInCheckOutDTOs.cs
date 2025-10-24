using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.CheckInCheckOutDTOs
{
    #region QR Code DTOs

    /// <summary>
    /// QR code data for vehicle pickup/return
    /// </summary>
    public class VehicleQRCodeData
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public DateTime BookingStartTime { get; set; }
        public DateTime BookingEndTime { get; set; }
        public int VehicleStationId { get; set; }
        public string VehicleStationName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string QRCodeHash { get; set; } = string.Empty; // For verification
    }

    /// <summary>
    /// QR scan check-in request (CoOwner self-service pickup)
    /// </summary>
    public class QRScanCheckInRequest
    {
        /// <summary>
        /// Scanned QR code data (JSON string or hash)
        /// </summary>
        public string QRCodeData { get; set; } = string.Empty;

        /// <summary>
        /// Vehicle condition report at pickup
        /// </summary>
        public VehicleConditionReport? ConditionReport { get; set; }

        /// <summary>
        /// Optional notes from co-owner
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Location latitude at check-in
        /// </summary>
        public decimal? LocationLatitude { get; set; }

        /// <summary>
        /// Location longitude at check-in
        /// </summary>
        public decimal? LocationLongitude { get; set; }
    }

    /// <summary>
    /// QR scan check-out request (CoOwner self-service return)
    /// </summary>
    public class QRScanCheckOutRequest
    {
        /// <summary>
        /// Scanned QR code data (JSON string or hash)
        /// </summary>
        public string QRCodeData { get; set; } = string.Empty;

        /// <summary>
        /// Vehicle condition report at return (REQUIRED)
        /// </summary>
        public VehicleConditionReport ConditionReport { get; set; } = new();

        /// <summary>
        /// Final odometer reading
        /// </summary>
        public int? OdometerReading { get; set; }

        /// <summary>
        /// Fuel/battery level percentage (0-100)
        /// </summary>
        public int? BatteryLevel { get; set; }

        /// <summary>
        /// Optional notes from co-owner
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Photos of vehicle condition (FileUpload IDs)
        /// </summary>
        public List<int>? ConditionPhotoIds { get; set; }

        /// <summary>
        /// Location latitude at check-out
        /// </summary>
        public decimal? LocationLatitude { get; set; }

        /// <summary>
        /// Location longitude at check-out
        /// </summary>
        public decimal? LocationLongitude { get; set; }
    }

    #endregion

    #region Manual Check-In/Out DTOs

    /// <summary>
    /// Manual check-in request (Staff verification)
    /// </summary>
    public class ManualCheckInRequest
    {
        /// <summary>
        /// Booking ID to check-in
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Vehicle station ID where pickup occurs
        /// </summary>
        public int VehicleStationId { get; set; }

        /// <summary>
        /// Vehicle condition report at pickup
        /// </summary>
        public VehicleConditionReport ConditionReport { get; set; } = new();

        /// <summary>
        /// Initial odometer reading
        /// </summary>
        public int? OdometerReading { get; set; }

        /// <summary>
        /// Initial battery level percentage (0-100)
        /// </summary>
        public int? BatteryLevel { get; set; }

        /// <summary>
        /// Staff notes/observations
        /// </summary>
        public string? StaffNotes { get; set; }

        /// <summary>
        /// Photos of vehicle condition (FileUpload IDs)
        /// </summary>
        public List<int>? ConditionPhotoIds { get; set; }

        /// <summary>
        /// Override time (if check-in not at booking start time)
        /// </summary>
        public DateTime? OverrideCheckInTime { get; set; }
    }

    /// <summary>
    /// Manual check-out request (Staff verification)
    /// </summary>
    public class ManualCheckOutRequest
    {
        /// <summary>
        /// Booking ID to check-out
        /// </summary>
        public int BookingId { get; set; }

        /// <summary>
        /// Vehicle station ID where return occurs
        /// </summary>
        public int VehicleStationId { get; set; }

        /// <summary>
        /// Vehicle condition report at return (REQUIRED)
        /// </summary>
        public VehicleConditionReport ConditionReport { get; set; } = new();

        /// <summary>
        /// Final odometer reading
        /// </summary>
        public int? OdometerReading { get; set; }

        /// <summary>
        /// Final battery level percentage (0-100)
        /// </summary>
        public int? BatteryLevel { get; set; }

        /// <summary>
        /// Staff notes/observations
        /// </summary>
        public string? StaffNotes { get; set; }

        /// <summary>
        /// Photos of vehicle condition (FileUpload IDs)
        /// </summary>
        public List<int>? ConditionPhotoIds { get; set; }

        /// <summary>
        /// Damages found during inspection
        /// </summary>
        public List<DamageReport>? DamagesFound { get; set; }

        /// <summary>
        /// Override time (if check-out not at booking end time)
        /// </summary>
        public DateTime? OverrideCheckOutTime { get; set; }
    }

    #endregion

    #region Shared DTOs

    /// <summary>
    /// Vehicle condition report
    /// </summary>
    public class VehicleConditionReport
    {
        /// <summary>
        /// Overall condition type
        /// </summary>
        public EVehicleConditionType ConditionType { get; set; }

        /// <summary>
        /// Cleanliness level (1-5, 5 is cleanest)
        /// </summary>
        public int CleanlinessLevel { get; set; } = 5;

        /// <summary>
        /// Has visible damages?
        /// </summary>
        public bool HasDamages { get; set; }

        /// <summary>
        /// Damage descriptions
        /// </summary>
        public List<DamageReport>? Damages { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Damage report detail
    /// </summary>
    public class DamageReport
    {
        /// <summary>
        /// Damage type/category
        /// </summary>
        public string DamageType { get; set; } = string.Empty;

        /// <summary>
        /// Severity (Minor, Moderate, Severe)
        /// </summary>
        public ESeverityType Severity { get; set; }

        /// <summary>
        /// Location on vehicle
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Photo evidence (FileUpload IDs)
        /// </summary>
        public List<int>? PhotoIds { get; set; }

        /// <summary>
        /// Estimated repair cost
        /// </summary>
        public decimal? EstimatedCost { get; set; }
    }

    /// <summary>
    /// Check-in response
    /// </summary>
    public class CheckInResponse
    {
        public int CheckInId { get; set; }
        public int BookingId { get; set; }
        public string BookingPurpose { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public int VehicleStationId { get; set; }
        public string VehicleStationName { get; set; } = string.Empty;
        public string VehicleStationAddress { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public EVehicleConditionType VehicleCondition { get; set; }
        public VehicleConditionReport? ConditionReport { get; set; }
        public bool WasQRScanned { get; set; }
        public string? Notes { get; set; }
        public CheckInStatus Status { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Check-out response
    /// </summary>
    public class CheckOutResponse
    {
        public int CheckOutId { get; set; }
        public int BookingId { get; set; }
        public string BookingPurpose { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public int VehicleStationId { get; set; }
        public string VehicleStationName { get; set; } = string.Empty;
        public string VehicleStationAddress { get; set; } = string.Empty;
        public DateTime CheckOutTime { get; set; }
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public EVehicleConditionType VehicleCondition { get; set; }
        public VehicleConditionReport? ConditionReport { get; set; }
        public bool WasQRScanned { get; set; }
        public int? OdometerReading { get; set; }
        public int? BatteryLevel { get; set; }
        public List<DamageReport>? DamagesFound { get; set; }
        public bool HasNewDamages { get; set; }
        public decimal? DamageCharges { get; set; }
        public string? Notes { get; set; }
        public CheckOutStatus Status { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public UsageSummary? UsageSummary { get; set; }
    }

    /// <summary>
    /// Usage summary after check-out
    /// </summary>
    public class UsageSummary
    {
        public DateTime CheckInTime { get; set; }
        public DateTime CheckOutTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public int TotalHours { get; set; }
        public int? DistanceTraveled { get; set; }
        public int? BatteryUsed { get; set; }
        public decimal BookingCost { get; set; }
        public decimal? LateFee { get; set; }
        public decimal? DamageFee { get; set; }
        public decimal TotalCost { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Check-in status
    /// </summary>
    public enum CheckInStatus
    {
        Success = 0,
        SuccessWithWarnings = 1,
        Failed = 2,
        PendingStaffVerification = 3,
        AlreadyCheckedIn = 4
    }

    /// <summary>
    /// Check-out status
    /// </summary>
    public enum CheckOutStatus
    {
        Success = 0,
        SuccessWithDamages = 1,
        SuccessWithLateFee = 2,
        Failed = 3,
        PendingDamageInspection = 4,
        AlreadyCheckedOut = 5
    }

    #endregion

    #region Validators

    public class QRScanCheckInRequestValidator : AbstractValidator<QRScanCheckInRequest>
    {
        public QRScanCheckInRequestValidator()
        {
            RuleFor(x => x.QRCodeData)
                .NotEmpty().WithMessage("QR code data is required");

            When(x => x.LocationLatitude.HasValue || x.LocationLongitude.HasValue, () =>
            {
                RuleFor(x => x.LocationLatitude)
                    .InclusiveBetween(-90, 90).WithMessage("Invalid latitude");
                RuleFor(x => x.LocationLongitude)
                    .InclusiveBetween(-180, 180).WithMessage("Invalid longitude");
            });
        }
    }

    public class QRScanCheckOutRequestValidator : AbstractValidator<QRScanCheckOutRequest>
    {
        public QRScanCheckOutRequestValidator()
        {
            RuleFor(x => x.QRCodeData)
                .NotEmpty().WithMessage("QR code data is required");

            RuleFor(x => x.ConditionReport)
                .NotNull().WithMessage("Vehicle condition report is required at check-out");

            RuleFor(x => x.BatteryLevel)
                .InclusiveBetween(0, 100).When(x => x.BatteryLevel.HasValue)
                .WithMessage("Battery level must be between 0-100%");

            RuleFor(x => x.OdometerReading)
                .GreaterThan(0).When(x => x.OdometerReading.HasValue)
                .WithMessage("Odometer reading must be positive");

            When(x => x.LocationLatitude.HasValue || x.LocationLongitude.HasValue, () =>
            {
                RuleFor(x => x.LocationLatitude)
                    .InclusiveBetween(-90, 90).WithMessage("Invalid latitude");
                RuleFor(x => x.LocationLongitude)
                    .InclusiveBetween(-180, 180).WithMessage("Invalid longitude");
            });
        }
    }

    public class ManualCheckInRequestValidator : AbstractValidator<ManualCheckInRequest>
    {
        public ManualCheckInRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0).WithMessage("Valid booking ID is required");

            RuleFor(x => x.VehicleStationId)
                .GreaterThan(0).WithMessage("Valid vehicle station ID is required");

            RuleFor(x => x.ConditionReport)
                .NotNull().WithMessage("Vehicle condition report is required");

            RuleFor(x => x.BatteryLevel)
                .InclusiveBetween(0, 100).When(x => x.BatteryLevel.HasValue)
                .WithMessage("Battery level must be between 0-100%");

            RuleFor(x => x.OdometerReading)
                .GreaterThan(0).When(x => x.OdometerReading.HasValue)
                .WithMessage("Odometer reading must be positive");
        }
    }

    public class ManualCheckOutRequestValidator : AbstractValidator<ManualCheckOutRequest>
    {
        public ManualCheckOutRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .GreaterThan(0).WithMessage("Valid booking ID is required");

            RuleFor(x => x.VehicleStationId)
                .GreaterThan(0).WithMessage("Valid vehicle station ID is required");

            RuleFor(x => x.ConditionReport)
                .NotNull().WithMessage("Vehicle condition report is required at check-out");

            RuleFor(x => x.BatteryLevel)
                .InclusiveBetween(0, 100).When(x => x.BatteryLevel.HasValue)
                .WithMessage("Battery level must be between 0-100%");

            RuleFor(x => x.OdometerReading)
                .GreaterThan(0).When(x => x.OdometerReading.HasValue)
                .WithMessage("Odometer reading must be positive");
        }
    }

    #endregion
}
