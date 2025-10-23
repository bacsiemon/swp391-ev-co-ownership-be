using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.DTOs.VehicleDTOs
{
    /// <summary>
    /// Request DTO for adding a co-owner to a vehicle
    /// </summary>
    public class AddCoOwnerRequest
    {
        /// <summary>
        /// User ID of the person to be invited as co-owner
        /// </summary>
        [Required(ErrorMessage = "USER_ID_REQUIRED")]
        public int UserId { get; set; }

        /// <summary>
        /// Ownership percentage to be assigned (must not exceed available percentage)
        /// </summary>
        [Required(ErrorMessage = "OWNERSHIP_PERCENTAGE_REQUIRED")]
        [Range(0.1, 99.9, ErrorMessage = "OWNERSHIP_PERCENTAGE_MUST_BE_BETWEEN_0_1_AND_99_9")]
        public decimal OwnershipPercentage { get; set; }

        /// <summary>
        /// Investment amount the new co-owner needs to contribute
        /// </summary>
        [Required(ErrorMessage = "INVESTMENT_AMOUNT_REQUIRED")]
        [Range(100000, 10000000000, ErrorMessage = "INVESTMENT_AMOUNT_MUST_BE_BETWEEN_100K_AND_10B_VND")]
        public decimal InvestmentAmount { get; set; }

        /// <summary>
        /// Optional message to the invited user
        /// </summary>
        [StringLength(500, ErrorMessage = "MESSAGE_MAX_500_CHARACTERS")]
        public string? InvitationMessage { get; set; }
    }

    /// <summary>
    /// Request DTO for responding to co-ownership invitation
    /// </summary>
    public class RespondToInvitationRequest
    {
        /// <summary>
        /// Accept (true) or reject (false) the invitation
        /// </summary>
        [Required(ErrorMessage = "RESPONSE_REQUIRED")]
        public bool Accept { get; set; }

        /// <summary>
        /// Optional response message
        /// </summary>
        [StringLength(500, ErrorMessage = "MESSAGE_MAX_500_CHARACTERS")]
        public string? ResponseMessage { get; set; }
    }

    /// <summary>
    /// Response DTO for vehicle information
    /// </summary>
    public class VehicleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public string Vin { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public string Color { get; set; } = null!;
        public decimal? BatteryCapacity { get; set; }
        public int? RangeKm { get; set; }
        public DateOnly PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateOnly? WarrantyUntil { get; set; }
        public int? DistanceTravelled { get; set; }
        public string? Status { get; set; }
        public string? VerificationStatus { get; set; }
        public decimal? LocationLatitude { get; set; }
        public decimal? LocationLongitude { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<VehicleCoOwnerResponse> CoOwners { get; set; } = new List<VehicleCoOwnerResponse>();
    }

    /// <summary>
    /// Detailed response DTO for vehicle information including fund details
    /// </summary>
    public class VehicleDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        
        // Vehicle Specifications
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public string Vin { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public string Color { get; set; } = null!;
        public decimal? BatteryCapacity { get; set; }
        public int? RangeKm { get; set; }
        
        // Purchase & Financial Info
        public DateOnly PurchaseDate { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateOnly? WarrantyUntil { get; set; }
        
        // Current Status
        public int? DistanceTravelled { get; set; }
        public string? Status { get; set; }
        public string? VerificationStatus { get; set; }
        
        // Location
        public decimal? LocationLatitude { get; set; }
        public decimal? LocationLongitude { get; set; }
        
        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Co-ownership Information
        public List<VehicleCoOwnerResponse> CoOwners { get; set; } = new List<VehicleCoOwnerResponse>();
        public decimal TotalOwnershipPercentage { get; set; }
        public decimal AvailableOwnershipPercentage { get; set; }
        
        // Fund Information
        public VehicleFundInfo? Fund { get; set; }
        
        // Creator Information
        public CreatorInfo? CreatedBy { get; set; }
    }

    /// <summary>
    /// Fund information for a vehicle
    /// </summary>
    public class VehicleFundInfo
    {
        public int FundId { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalUsages { get; set; }
        public decimal TotalAddedAmount { get; set; }
        public decimal TotalUsedAmount { get; set; }
    }

    /// <summary>
    /// Creator information
    /// </summary>
    public class CreatorInfo
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// Response DTO for vehicle co-owner information
    /// </summary>
    public class VehicleCoOwnerResponse
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? InvitationMessage { get; set; }
        public string? ResponseMessage { get; set; }
    }

    /// <summary>
    /// Response DTO for co-ownership invitation
    /// </summary>
    public class CoOwnershipInvitationResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string VehicleBrand { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public int VehicleYear { get; set; }
        public string LicensePlate { get; set; } = null!;
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string? Status { get; set; }
        public string? InvitationMessage { get; set; }
        public DateTime? CreatedAt { get; set; }
        public InviterInfo Inviter { get; set; } = null!;
    }

    /// <summary>
    /// Information about the person who sent the invitation
    /// </summary>
    public class InviterInfo
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}