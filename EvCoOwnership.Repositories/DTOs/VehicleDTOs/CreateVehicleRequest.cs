using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.DTOs.VehicleDTOs
{
    /// <summary>
    /// Request DTO for creating a new vehicle
    /// </summary>
    public class CreateVehicleRequest
    {
        /// <summary>
        /// Vehicle name/title
        /// </summary>
        [Required(ErrorMessage = "VEHICLE_NAME_REQUIRED")]
        [StringLength(100, ErrorMessage = "VEHICLE_NAME_MAX_100_CHARACTERS")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Vehicle description
        /// </summary>
        [StringLength(500, ErrorMessage = "DESCRIPTION_MAX_500_CHARACTERS")]
        public string? Description { get; set; }

        /// <summary>
        /// Vehicle brand (e.g., VinFast, Tesla)
        /// </summary>
        [Required(ErrorMessage = "BRAND_REQUIRED")]
        [StringLength(50, ErrorMessage = "BRAND_MAX_50_CHARACTERS")]
        public string Brand { get; set; } = null!;

        /// <summary>
        /// Vehicle model (e.g., VF8, Model 3)
        /// </summary>
        [Required(ErrorMessage = "MODEL_REQUIRED")]
        [StringLength(50, ErrorMessage = "MODEL_MAX_50_CHARACTERS")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// Manufacturing year
        /// </summary>
        [Required(ErrorMessage = "YEAR_REQUIRED")]
        [Range(2015, 2030, ErrorMessage = "YEAR_MUST_BE_BETWEEN_2015_AND_2030")]
        public int Year { get; set; }

        /// <summary>
        /// Vehicle Identification Number
        /// </summary>
        [Required(ErrorMessage = "VIN_REQUIRED")]
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN_MUST_BE_17_CHARACTERS")]
        [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "VIN_INVALID_FORMAT")]
        public string Vin { get; set; } = null!;

        /// <summary>
        /// License plate number (Vietnamese format)
        /// </summary>
        [Required(ErrorMessage = "LICENSE_PLATE_REQUIRED")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{1,2}-[0-9]{3}\.[0-9]{2}$|^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}$",
            ErrorMessage = "LICENSE_PLATE_INVALID_FORMAT")]
        public string LicensePlate { get; set; } = null!;

        /// <summary>
        /// Vehicle color
        /// </summary>
        [Required(ErrorMessage = "COLOR_REQUIRED")]
        [StringLength(30, ErrorMessage = "COLOR_MAX_30_CHARACTERS")]
        public string Color { get; set; } = null!;        /// <summary>
                                                          /// Battery capacity in kWh
                                                          /// </summary>
        [Range(0.1, 200, ErrorMessage = "BATTERY_CAPACITY_MUST_BE_BETWEEN_0_1_AND_200")]
        public decimal? BatteryCapacity { get; set; }

        /// <summary>
        /// Range in kilometers
        /// </summary>
        [Range(1, 1000, ErrorMessage = "RANGE_MUST_BE_BETWEEN_1_AND_1000")]
        public int? RangeKm { get; set; }

        /// <summary>
        /// Purchase date
        /// </summary>
        [Required(ErrorMessage = "PURCHASE_DATE_REQUIRED")]
        public DateOnly PurchaseDate { get; set; }

        /// <summary>
        /// Purchase price in VND
        /// </summary>
        [Required(ErrorMessage = "PURCHASE_PRICE_REQUIRED")]
        [Range(100000000, 10000000000, ErrorMessage = "PURCHASE_PRICE_MUST_BE_BETWEEN_100M_AND_10B_VND")]
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Warranty expiration date
        /// </summary>
        public DateOnly? WarrantyUntil { get; set; }

        /// <summary>
        /// Distance travelled in kilometers
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "DISTANCE_MUST_BE_BETWEEN_0_AND_1000000")]
        public int? DistanceTravelled { get; set; }

        /// <summary>
        /// Current location latitude
        /// </summary>
        [Range(-90, 90, ErrorMessage = "LATITUDE_MUST_BE_BETWEEN_MINUS_90_AND_90")]
        public decimal? LocationLatitude { get; set; }

        /// <summary>
        /// Current location longitude
        /// </summary>
        [Range(-180, 180, ErrorMessage = "LONGITUDE_MUST_BE_BETWEEN_MINUS_180_AND_180")]
        public decimal? LocationLongitude { get; set; }

        /// <summary>
        /// Initial ownership percentage for the creator
        /// </summary>
        [Range(1, 100, ErrorMessage = "OWNERSHIP_PERCENTAGE_MUST_BE_BETWEEN_1_AND_100")]
        public decimal InitialOwnershipPercentage { get; set; } = 100;

        /// <summary>
        /// Initial investment amount by the creator
        /// </summary>
        [Required(ErrorMessage = "INITIAL_INVESTMENT_REQUIRED")]
        [Range(1000000, 10000000000, ErrorMessage = "INITIAL_INVESTMENT_MUST_BE_BETWEEN_1M_AND_10B_VND")]
        public decimal InitialInvestmentAmount { get; set; }
    }
}