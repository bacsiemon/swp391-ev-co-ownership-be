using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.Repositories.DTOs.UserDTOs
{
    /// <summary>
    /// Response DTO for user profile information
    /// </summary>
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Additional profile stats
        public UserProfileStats Stats { get; set; } = new();
    }

    /// <summary>
    /// User profile statistics
    /// </summary>
    public class UserProfileStats
    {
        public int TotalVehiclesOwned { get; set; }
        public int TotalVehiclesCoOwned { get; set; }
        public int TotalBookings { get; set; }
        public int PendingInvitations { get; set; }
        public bool HasValidDrivingLicense { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalInvestmentAmount { get; set; }
    }

    /// <summary>
    /// Request DTO for updating user profile
    /// </summary>
    public class UpdateUserProfileRequest
    {
        [Required(ErrorMessage = "FIRST_NAME_REQUIRED")]
        [StringLength(50, ErrorMessage = "FIRST_NAME_MAX_50_CHARACTERS")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "FIRST_NAME_ONLY_LETTERS_AND_SPACES")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LAST_NAME_REQUIRED")]
        [StringLength(50, ErrorMessage = "LAST_NAME_MAX_50_CHARACTERS")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "LAST_NAME_ONLY_LETTERS_AND_SPACES")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "INVALID_PHONE_FORMAT")]
        [RegularExpression(@"^(\+84|0)[3-9]\d{8}$", ErrorMessage = "INVALID_VIETNAM_PHONE_FORMAT")]
        public string? Phone { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "ADDRESS_MAX_200_CHARACTERS")]
        public string? Address { get; set; }

        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// Request DTO for changing password
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "CURRENT_PASSWORD_REQUIRED")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "NEW_PASSWORD_REQUIRED")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "NEW_PASSWORD_MIN_8_CHARACTERS")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
            ErrorMessage = "NEW_PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "CONFIRM_PASSWORD_REQUIRED")]
        [Compare("NewPassword", ErrorMessage = "CONFIRM_PASSWORD_MUST_MATCH")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for user's vehicles summary
    /// </summary>
    public class UserVehiclesSummary
    {
        public List<UserVehicleInfo> OwnedVehicles { get; set; } = new();
        public List<UserVehicleInfo> CoOwnedVehicles { get; set; } = new();
        public List<UserVehicleInvitation> PendingInvitations { get; set; } = new();
    }

    /// <summary>
    /// Brief vehicle information for profile
    /// </summary>
    public class UserVehicleInfo
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>
    /// Vehicle invitation information for profile
    /// </summary>
    public class UserVehicleInvitation
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string InviterName { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }
        public DateTime InvitedAt { get; set; }
    }

    /// <summary>
    /// User activity summary for profile
    /// </summary>
    public class UserActivitySummary
    {
        public List<RecentBooking> RecentBookings { get; set; } = new();
        public List<RecentPayment> RecentPayments { get; set; } = new();
        public DrivingLicenseInfo? DrivingLicense { get; set; }
    }

    /// <summary>
    /// Recent booking information
    /// </summary>
    public class RecentBooking
    {
        public int BookingId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Recent payment information
    /// </summary>
    public class RecentPayment
    {
        public int PaymentId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    /// <summary>
    /// Driving license information for profile
    /// </summary>
    public class DrivingLicenseInfo
    {
        public int LicenseId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string IssuedBy { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public bool IsValid { get; set; }
        public string? LicenseImageUrl { get; set; }
    }

    /// <summary>
    /// Profile completeness validation result
    /// </summary>
    public class UserProfileCompleteness
    {
        public decimal Completeness { get; set; }
        public List<string> MissingFields { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}