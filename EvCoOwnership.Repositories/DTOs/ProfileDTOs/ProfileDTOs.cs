using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.ProfileDTOs
{
    #region Profile Request DTOs

    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(50)
                .WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .MaximumLength(50)
                .WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters");

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today)
                .When(x => x.DateOfBirth.HasValue)
                .WithMessage("Date of birth must be in the past");

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage("Bio cannot exceed 500 characters");

            RuleFor(x => x.AvatarUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
                .WithMessage("Invalid avatar URL format");
        }
    }

    public class UpdateNotificationSettingsRequest
    {
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool BookingReminders { get; set; }
        public bool MaintenanceAlerts { get; set; }
        public bool PaymentNotifications { get; set; }
        public bool SystemAnnouncements { get; set; }
    }

    public class UpdatePrivacySettingsRequest
    {
        public bool ProfileVisibility { get; set; }
        public bool ShowEmail { get; set; }
        public bool ShowPhone { get; set; }
        public bool ShareUsageData { get; set; }
        public bool AllowDataAnalytics { get; set; }
    }

    #endregion

    #region Profile Response DTOs

    public class ProfileResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public ProfileStatistics Statistics { get; set; } = new();
        public NotificationSettings NotificationSettings { get; set; } = new();
        public PrivacySettings PrivacySettings { get; set; } = new();
    }

    public class ProfileStatistics
    {
        public int TotalBookings { get; set; }
        public int CompletedTrips { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal TotalSpent { get; set; }
        public int VehiclesCoOwned { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int MaintenanceContributions { get; set; }
        public DateTime? LastActivity { get; set; }
        public int DaysActive { get; set; }
        public decimal CarbonFootprintSaved { get; set; }
    }

    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool BookingReminders { get; set; } = true;
        public bool MaintenanceAlerts { get; set; } = true;
        public bool PaymentNotifications { get; set; } = true;
        public bool SystemAnnouncements { get; set; } = true;
    }

    public class PrivacySettings
    {
        public bool ProfileVisibility { get; set; } = true;
        public bool ShowEmail { get; set; } = false;
        public bool ShowPhone { get; set; } = false;
        public bool ShareUsageData { get; set; } = true;
        public bool AllowDataAnalytics { get; set; } = true;
    }

    public class ActivityLogResponse
    {
        public List<ActivityLogItem> Activities { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ActivityLogItem
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SecurityLogResponse
    {
        public List<SecurityLogItem> SecurityEvents { get; set; } = new();
        public int TotalCount { get; set; }
        public SecuritySummary Summary { get; set; } = new();
    }

    public class SecurityLogItem
    {
        public int Id { get; set; }
        public string Event { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public string? Device { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FailureReason { get; set; }
    }

    public class SecuritySummary
    {
        public int SuccessfulLogins { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int PasswordChanges { get; set; }
        public int SuspiciousActivities { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<string> RecentDevices { get; set; } = new();
        public List<string> RecentLocations { get; set; } = new();
    }

    #endregion
}