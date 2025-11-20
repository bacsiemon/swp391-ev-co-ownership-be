namespace EvCoOwnership.Repositories.DTOs.AuthDTOs
{
    /// <summary>
    /// Response DTO for license verification result
    /// </summary>
    public class VerifyLicenseResponse
    {
        /// <summary>
        /// Whether the license verification was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// License verification status code
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Detailed verification message
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// License details if verification is successful
        /// </summary>
        public LicenseDetails? LicenseDetails { get; set; }

        /// <summary>
        /// List of verification issues if any
        /// </summary>
        public List<string>? Issues { get; set; }

        /// <summary>
        /// Timestamp when verification was performed
        /// </summary>
        public DateTime VerifiedAt { get; set; }
    }

    /// <summary>
    /// Detailed information about the verified license
    /// </summary>
    public class LicenseDetails
    {
        /// <summary>
        /// License number
        /// </summary>
        public string LicenseNumber { get; set; } = null!;

        /// <summary>
        /// License holder's full name
        /// </summary>
        public string HolderName { get; set; } = null!;

        /// <summary>
        /// Date when the license was issued
        /// </summary>
        public DateOnly IssueDate { get; set; }

        /// <summary>
        /// License expiry date (if available)
        /// </summary>
        public DateOnly? ExpiryDate { get; set; }

        /// <summary>
        /// Authority that issued the license
        /// </summary>
        public string IssuedBy { get; set; } = null!;

        /// <summary>
        /// License status (Active, Expired, Suspended, etc.)
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// License class/category (if available)
        /// </summary>
        public string? LicenseClass { get; set; }

        /// <summary>
        /// Any restrictions on the license
        /// </summary>
        public List<string>? Restrictions { get; set; }
    }
}