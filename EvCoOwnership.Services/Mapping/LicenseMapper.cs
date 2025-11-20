using EvCoOwnership.Repositories.DTOs.AuthDTOs;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Mapping
{
    /// <summary>
    /// Extension methods for mapping between License entities and DTOs
    /// </summary>
    public static class LicenseMapper
    {
        /// <summary>
        /// Maps DrivingLicense entity to LicenseDetails DTO
        /// </summary>
        /// <param name="license">DrivingLicense entity</param>
        /// <returns>LicenseDetails DTO</returns>
        public static LicenseDetails ToLicenseDetails(this DrivingLicense license)
        {
            if (license == null)
                throw new ArgumentNullException(nameof(license));

            var today = DateOnly.FromDateTime(DateTime.Now);
            var status = "ACTIVE";
            var restrictions = new List<string>();

            // Determine status based on expiry date
            if (license.ExpiryDate.HasValue && license.ExpiryDate.Value < today)
            {
                status = "EXPIRED";
                restrictions.Add("LICENSE_EXPIRED");
            }

            // Get holder name from CoOwner if available
            var holderName = license.CoOwner?.User != null
                ? $"{license.CoOwner.User.FirstName} {license.CoOwner.User.LastName}"
                : "Unknown Holder";

            return new LicenseDetails
            {
                LicenseNumber = license.LicenseNumber,
                HolderName = holderName,
                IssueDate = license.IssueDate,
                ExpiryDate = license.ExpiryDate,
                IssuedBy = license.IssuedBy,
                Status = status,
                LicenseClass = ExtractLicenseClass(license.LicenseNumber),
                Restrictions = restrictions.Any() ? restrictions : null
            };
        }

        /// <summary>
        /// Maps VerifyLicenseRequest to DrivingLicense entity
        /// </summary>
        /// <param name="request">VerifyLicenseRequest DTO</param>
        /// <param name="coOwnerId">ID of the co-owner this license belongs to</param>
        /// <param name="licenseImageUrl">URL of the uploaded license image</param>
        /// <returns>DrivingLicense entity</returns>
        public static DrivingLicense ToEntity(this VerifyLicenseRequest request, int? coOwnerId, string? licenseImageUrl = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Calculate expiry date (typically 10 years from issue date for Vietnam driving licenses)
            var expiryDate = request.IssueDate.AddYears(10);

            return new DrivingLicense
            {
                CoOwnerId = coOwnerId,
                LicenseNumber = request.LicenseNumber,
                IssuedBy = request.IssuedBy,
                IssueDate = request.IssueDate,
                ExpiryDate = expiryDate,
                LicenseImageUrl = licenseImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Maps DrivingLicense entity to a summary object
        /// </summary>
        /// <param name="license">DrivingLicense entity</param>
        /// <returns>License summary object</returns>
        public static object ToSummary(this DrivingLicense license)
        {
            if (license == null)
                throw new ArgumentNullException(nameof(license));

            var today = DateOnly.FromDateTime(DateTime.Now);
            var isExpired = license.ExpiryDate.HasValue && license.ExpiryDate.Value < today;

            return new
            {
                Id = license.Id,
                LicenseNumber = license.LicenseNumber,
                IssuedBy = license.IssuedBy,
                IssueDate = license.IssueDate,
                ExpiryDate = license.ExpiryDate,
                Status = isExpired ? "EXPIRED" : "ACTIVE",
                DaysUntilExpiry = license.ExpiryDate.HasValue
                    ? (license.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days
                    : (int?)null,
                CreatedAt = license.CreatedAt,
                UpdatedAt = license.UpdatedAt
            };
        }

        /// <summary>
        /// Creates a verification response for successful license registration
        /// </summary>
        /// <param name="license">DrivingLicense entity that was created</param>
        /// <returns>VerifyLicenseResponse DTO</returns>
        public static VerifyLicenseResponse ToVerificationResponse(this DrivingLicense license)
        {
            if (license == null)
                throw new ArgumentNullException(nameof(license));

            return new VerifyLicenseResponse
            {
                IsValid = true,
                Status = "REGISTERED",
                Message = "License has been successfully verified and registered",
                LicenseDetails = license.ToLicenseDetails(),
                Issues = null,
                VerifiedAt = license.CreatedAt ?? DateTime.UtcNow
            };
        }

        /// <summary>
        /// Extracts license class from license number format
        /// </summary>
        /// <param name="licenseNumber">License number</param>
        /// <returns>License class (A, B, C, etc.)</returns>
        private static string? ExtractLicenseClass(string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
                return null;

            // For Vietnamese license format, try to extract class from first character
            if (char.IsLetter(licenseNumber[0]))
            {
                return licenseNumber[0].ToString().ToUpperInvariant();
            }

            // Default to class B for numeric licenses (most common)
            return "B";
        }

        /// <summary>
        /// Validates license number format for Vietnamese licenses
        /// </summary>
        /// <param name="licenseNumber">License number to validate</param>
        /// <returns>True if format is valid</returns>
        public static bool IsValidVietnameseLicenseFormat(string licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
                return false;

            // Vietnamese license formats:
            // - 9 digits: 123456789
            // - Letter + 8 digits: A12345678
            // - 12 digits: 123456789012

            var patterns = new[]
            {
                @"^[0-9]{9}$",           // 9 digits
                @"^[A-Z][0-9]{8}$",     // 1 letter + 8 digits
                @"^[0-9]{12}$"          // 12 digits (new format)
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(licenseNumber, pattern));
        }

        /// <summary>
        /// Calculates days until license expiry
        /// </summary>
        /// <param name="license">DrivingLicense entity</param>
        /// <returns>Number of days until expiry, null if no expiry date</returns>
        public static int? GetDaysUntilExpiry(this DrivingLicense license)
        {
            if (license?.ExpiryDate == null)
                return null;

            var today = DateOnly.FromDateTime(DateTime.Now);
            var daysUntilExpiry = license.ExpiryDate.Value.DayNumber - today.DayNumber;

            return daysUntilExpiry;
        }

        /// <summary>
        /// Checks if license is expired
        /// </summary>
        /// <param name="license">DrivingLicense entity</param>
        /// <returns>True if license is expired</returns>
        public static bool IsExpired(this DrivingLicense license)
        {
            if (license?.ExpiryDate == null)
                return false;

            var today = DateOnly.FromDateTime(DateTime.Now);
            return license.ExpiryDate.Value < today;
        }

        /// <summary>
        /// Checks if license is expiring soon (within specified days)
        /// </summary>
        /// <param name="license">DrivingLicense entity</param>
        /// <param name="daysThreshold">Number of days to consider as "soon" (default: 30)</param>
        /// <returns>True if license is expiring soon</returns>
        public static bool IsExpiringSoon(this DrivingLicense license, int daysThreshold = 30)
        {
            var daysUntilExpiry = license.GetDaysUntilExpiry();
            return daysUntilExpiry.HasValue && daysUntilExpiry.Value <= daysThreshold && daysUntilExpiry.Value > 0;
        }
    }
}