using FluentValidation;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.VehicleDTOs
{
    public class VehicleVerificationRequest
    {
        public int VehicleId { get; set; }
        public EVehicleVerificationStatus Status { get; set; }
        public string? Notes { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class VehicleVerificationRequestValidator : AbstractValidator<VehicleVerificationRequest>
    {
        public VehicleVerificationRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("VEHICLE_ID_REQUIRED");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("INVALID_VERIFICATION_STATUS")
                .Must(status => status != EVehicleVerificationStatus.Pending)
                .WithMessage("CANNOT_SET_PENDING_STATUS");

            When(x => x.Status == EVehicleVerificationStatus.Rejected, () =>
            {
                RuleFor(x => x.Notes)
                    .NotEmpty().WithMessage("REJECTION_NOTES_REQUIRED")
                    .MinimumLength(10).WithMessage("REJECTION_NOTES_MIN_10_CHARACTERS");
            });

            When(x => x.ImageUrls != null, () =>
            {
                RuleForEach(x => x.ImageUrls)
                    .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    .WithMessage("INVALID_IMAGE_URL_FORMAT");
            });
        }
    }

    public class VehicleVerificationResponse
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public int StaffId { get; set; }
        public string StaffName { get; set; } = null!;
        public EVehicleVerificationStatus Status { get; set; }
        public string? Notes { get; set; }
        public List<string>? ImageUrls { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VehicleDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public string Vin { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public string? Color { get; set; }
        public decimal? BatteryCapacity { get; set; }
        public int? RangeKm { get; set; }
        public DateOnly PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateOnly? WarrantyUntil { get; set; }
        public int? DistanceTravelled { get; set; }
        public EVehicleStatus? Status { get; set; }
        public EVehicleVerificationStatus? VerificationStatus { get; set; }
        public decimal? LocationLatitude { get; set; }
        public decimal? LocationLongitude { get; set; }
        public List<VehicleVerificationResponse> VerificationHistory { get; set; } = new();
    }

    public class VehicleCreateRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public string Vin { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public string? Color { get; set; }
        public decimal? BatteryCapacity { get; set; }
        public int? RangeKm { get; set; }
        public DateOnly PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateOnly? WarrantyUntil { get; set; }
        public int? FundId { get; set; }
    }

    public class VehicleCreateRequestValidator : AbstractValidator<VehicleCreateRequest>
    {
        public VehicleCreateRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("VEHICLE_NAME_REQUIRED")
                .MaximumLength(200).WithMessage("VEHICLE_NAME_MAX_200_CHARACTERS");

            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("VEHICLE_BRAND_REQUIRED")
                .MaximumLength(100).WithMessage("VEHICLE_BRAND_MAX_100_CHARACTERS");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("VEHICLE_MODEL_REQUIRED")
                .MaximumLength(100).WithMessage("VEHICLE_MODEL_MAX_100_CHARACTERS");

            RuleFor(x => x.Year)
                .GreaterThan(1900).WithMessage("INVALID_VEHICLE_YEAR")
                .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage("FUTURE_VEHICLE_YEAR_NOT_ALLOWED");

            RuleFor(x => x.Vin)
                .NotEmpty().WithMessage("VIN_REQUIRED")
                .Length(17).WithMessage("VIN_MUST_BE_17_CHARACTERS")
                .Matches("^[A-HJ-NPR-Z0-9]{17}$").WithMessage("INVALID_VIN_FORMAT");

            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("LICENSE_PLATE_REQUIRED")
                .MaximumLength(20).WithMessage("LICENSE_PLATE_MAX_20_CHARACTERS")
                .Matches(@"^[0-9]{2}[A-Z]{1}-[0-9]{3}\.[0-9]{2}$|^[0-9]{2}[A-Z]{2}-[0-9]{4}$")
                .WithMessage("INVALID_VIETNAM_LICENSE_PLATE_FORMAT");

            RuleFor(x => x.BatteryCapacity)
                .GreaterThan(0).WithMessage("BATTERY_CAPACITY_MUST_BE_POSITIVE")
                .When(x => x.BatteryCapacity.HasValue);

            RuleFor(x => x.RangeKm)
                .GreaterThan(0).WithMessage("RANGE_MUST_BE_POSITIVE")
                .When(x => x.RangeKm.HasValue);

            RuleFor(x => x.PurchaseDate)
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("PURCHASE_DATE_CANNOT_BE_FUTURE");

            RuleFor(x => x.PurchasePrice)
                .GreaterThan(0).WithMessage("PURCHASE_PRICE_MUST_BE_POSITIVE");

            RuleFor(x => x.WarrantyUntil)
                .GreaterThan(x => x.PurchaseDate)
                .WithMessage("WARRANTY_DATE_MUST_BE_AFTER_PURCHASE_DATE")
                .When(x => x.WarrantyUntil.HasValue);

            RuleFor(x => x.FundId)
                .GreaterThan(0).WithMessage("INVALID_FUND_ID")
                .When(x => x.FundId.HasValue);
        }
    }
}