using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.LicenseDTOs
{
    /// <summary>
    /// Request to approve a driving license
    /// </summary>
    public class ApproveLicenseRequest
    {
        /// <summary>
        /// ID of the license to approve
        /// </summary>
        public int LicenseId { get; set; }

        /// <summary>
        /// Optional notes from the admin/staff
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to reject a driving license with reason
    /// </summary>
    public class RejectLicenseRequest
    {
        /// <summary>
        /// ID of the license to reject
        /// </summary>
        public int LicenseId { get; set; }

        /// <summary>
        /// Reason for rejecting the license (required)
        /// </summary>
        public string RejectReason { get; set; } = string.Empty;

        /// <summary>
        /// Additional notes from the admin/staff
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Response for license approval/rejection operations
    /// </summary>
    public class LicenseApprovalResponse
    {
        /// <summary>
        /// ID of the license
        /// </summary>
        public int LicenseId { get; set; }

        /// <summary>
        /// License number
        /// </summary>
        public string LicenseNumber { get; set; } = string.Empty;

        /// <summary>
        /// New verification status
        /// </summary>
        public EDrivingLicenseVerificationStatus? VerificationStatus { get; set; }

        /// <summary>
        /// Reason for rejection (if applicable)
        /// </summary>
        public string? RejectReason { get; set; }

        /// <summary>
        /// Name of the user who verified the license
        /// </summary>
        public string VerifiedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// Date and time when the license was verified
        /// </summary>
        public DateTime VerifiedAt { get; set; }
    }

    /// <summary>
    /// Response for getting license list with verification status
    /// </summary>
    public class LicenseListResponse
    {
        /// <summary>
        /// ID of the license
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// License number
        /// </summary>
        public string LicenseNumber { get; set; } = string.Empty;

        /// <summary>
        /// Issued by authority
        /// </summary>
        public string IssuedBy { get; set; } = string.Empty;

        /// <summary>
        /// Issue date
        /// </summary>
        public DateOnly IssueDate { get; set; }

        /// <summary>
        /// Expiry date
        /// </summary>
        public DateOnly? ExpiryDate { get; set; }

        /// <summary>
        /// License image URL
        /// </summary>
        public string LicenseImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Verification status
        /// </summary>
        public EDrivingLicenseVerificationStatus? VerificationStatus { get; set; }

        /// <summary>
        /// Reason for rejection (if applicable)
        /// </summary>
        public string? RejectReason { get; set; }

        /// <summary>
        /// Name of the license holder
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User ID of the license holder
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Date when license was submitted
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// Name of the user who verified the license
        /// </summary>
        public string? VerifiedByUserName { get; set; }

        /// <summary>
        /// Date when license was verified
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Whether the license is expired
        /// </summary>
        public bool IsExpired { get; set; }
    }

    /// <summary>
    /// Validator for RejectLicenseRequest
    /// </summary>
    public class RejectLicenseRequestValidator : AbstractValidator<RejectLicenseRequest>
    {
        public RejectLicenseRequestValidator()
        {
            RuleFor(x => x.LicenseId)
                .GreaterThan(0)
                .WithMessage("LICENSE_ID_MUST_BE_GREATER_THAN_ZERO");

            RuleFor(x => x.RejectReason)
                .NotEmpty()
                .WithMessage("REJECT_REASON_REQUIRED")
                .MaximumLength(500)
                .WithMessage("REJECT_REASON_MAX_500_CHARACTERS");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("NOTES_MAX_1000_CHARACTERS");
        }
    }

    /// <summary>
    /// Validator for ApproveLicenseRequest
    /// </summary>
    public class ApproveLicenseRequestValidator : AbstractValidator<ApproveLicenseRequest>
    {
        public ApproveLicenseRequestValidator()
        {
            RuleFor(x => x.LicenseId)
                .GreaterThan(0)
                .WithMessage("LICENSE_ID_MUST_BE_GREATER_THAN_ZERO");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("NOTES_MAX_1000_CHARACTERS");
        }
    }
}