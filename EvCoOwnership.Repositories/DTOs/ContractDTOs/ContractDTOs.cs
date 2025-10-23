using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.ContractDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to create a new e-contract
    /// </summary>
    public class CreateContractRequest
    {
        /// <summary>
        /// Vehicle ID for the contract
        /// </summary>
        public int VehicleId { get; set; }

        /// <summary>
        /// Contract template type
        /// </summary>
        public string TemplateType { get; set; } = string.Empty;

        /// <summary>
        /// Contract title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Contract description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Custom contract terms (JSON format)
        /// </summary>
        public string? CustomTerms { get; set; }

        /// <summary>
        /// List of party user IDs who need to sign (excluding creator)
        /// </summary>
        public List<int> SignatoryUserIds { get; set; } = new();

        /// <summary>
        /// Contract effective date
        /// </summary>
        public DateTime? EffectiveDate { get; set; }

        /// <summary>
        /// Contract expiry date
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// URLs to attached documents
        /// </summary>
        public List<string>? AttachmentUrls { get; set; }

        /// <summary>
        /// Auto-activate when fully signed
        /// </summary>
        public bool AutoActivate { get; set; } = true;

        /// <summary>
        /// Signature deadline
        /// </summary>
        public DateTime? SignatureDeadline { get; set; }
    }

    public class CreateContractRequestValidator : AbstractValidator<CreateContractRequest>
    {
        public CreateContractRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("Vehicle ID is required");

            RuleFor(x => x.TemplateType)
                .NotEmpty().WithMessage("Template type is required")
                .Must(BeValidTemplateType).WithMessage("Invalid template type");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .Length(5, 200).WithMessage("Title must be 5-200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description cannot exceed 2000 characters");

            RuleFor(x => x.SignatoryUserIds)
                .NotEmpty().WithMessage("At least one signatory is required")
                .Must(x => x != null && x.Count > 0).WithMessage("At least one signatory is required");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(DateTime.UtcNow).When(x => x.ExpiryDate.HasValue)
                .WithMessage("Expiry date must be in the future");

            RuleFor(x => x.EffectiveDate)
                .LessThan(x => x.ExpiryDate).When(x => x.EffectiveDate.HasValue && x.ExpiryDate.HasValue)
                .WithMessage("Effective date must be before expiry date");

            RuleFor(x => x.SignatureDeadline)
                .GreaterThan(DateTime.UtcNow).When(x => x.SignatureDeadline.HasValue)
                .WithMessage("Signature deadline must be in the future");
        }

        private bool BeValidTemplateType(string templateType)
        {
            return Enum.TryParse<EContractTemplateType>(templateType, true, out _);
        }
    }

    /// <summary>
    /// Request to sign an e-contract
    /// </summary>
    public class SignContractRequest
    {
        /// <summary>
        /// Electronic signature (could be encrypted hash, biometric data, etc.)
        /// </summary>
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the signer
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Device information
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Geolocation (latitude, longitude)
        /// </summary>
        public string? Geolocation { get; set; }

        /// <summary>
        /// Optional agreement confirmation text
        /// </summary>
        public string? AgreementConfirmation { get; set; }

        /// <summary>
        /// Notes from the signer
        /// </summary>
        public string? SignerNotes { get; set; }
    }

    public class SignContractRequestValidator : AbstractValidator<SignContractRequest>
    {
        public SignContractRequestValidator()
        {
            RuleFor(x => x.Signature)
                .NotEmpty().WithMessage("Signature is required")
                .MinimumLength(10).WithMessage("Invalid signature format");

            RuleFor(x => x.SignerNotes)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.SignerNotes))
                .WithMessage("Signer notes cannot exceed 500 characters");
        }
    }

    /// <summary>
    /// Request to decline/reject a contract
    /// </summary>
    public class DeclineContractRequest
    {
        /// <summary>
        /// Reason for declining
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Suggested changes
        /// </summary>
        public string? SuggestedChanges { get; set; }
    }

    public class DeclineContractRequestValidator : AbstractValidator<DeclineContractRequest>
    {
        public DeclineContractRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .Length(10, 1000).WithMessage("Reason must be 10-1000 characters");

            RuleFor(x => x.SuggestedChanges)
                .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.SuggestedChanges))
                .WithMessage("Suggested changes cannot exceed 2000 characters");
        }
    }

    /// <summary>
    /// Request to get contracts with filters
    /// </summary>
    public class GetContractsRequest
    {
        /// <summary>
        /// Filter by vehicle ID
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Filter by template type
        /// </summary>
        public string? TemplateType { get; set; }

        /// <summary>
        /// Filter by contract status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter contracts where user is creator
        /// </summary>
        public bool? IsCreator { get; set; }

        /// <summary>
        /// Filter contracts where user is signatory
        /// </summary>
        public bool? IsSignatory { get; set; }

        /// <summary>
        /// Filter by signature status for current user
        /// </summary>
        public string? MySignatureStatus { get; set; }

        /// <summary>
        /// Filter by creation date from
        /// </summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>
        /// Filter by creation date to
        /// </summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>
        /// Show only active contracts
        /// </summary>
        public bool? ActiveOnly { get; set; }

        /// <summary>
        /// Show only contracts pending my signature
        /// </summary>
        public bool? PendingMySignature { get; set; }

        /// <summary>
        /// Page number (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by field
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Sort order (asc/desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }

    public class GetContractsRequestValidator : AbstractValidator<GetContractsRequest>
    {
        public GetContractsRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.SortBy)
                .Must(x => new[] { "CreatedAt", "UpdatedAt", "EffectiveDate", "ExpiryDate", "Title" }.Contains(x))
                .WithMessage("Invalid sort field");

            RuleFor(x => x.SortOrder)
                .Must(x => new[] { "asc", "desc" }.Contains(x.ToLower()))
                .WithMessage("Sort order must be 'asc' or 'desc'");
        }
    }

    /// <summary>
    /// Request to terminate a contract
    /// </summary>
    public class TerminateContractRequest
    {
        /// <summary>
        /// Reason for termination
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Termination effective date
        /// </summary>
        public DateTime? EffectiveDate { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string? Notes { get; set; }
    }

    public class TerminateContractRequestValidator : AbstractValidator<TerminateContractRequest>
    {
        public TerminateContractRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .Length(10, 1000).WithMessage("Reason must be 10-1000 characters");

            RuleFor(x => x.Notes)
                .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Notes))
                .WithMessage("Notes cannot exceed 2000 characters");
        }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Complete contract response with all details
    /// </summary>
    public class ContractResponse
    {
        public int ContractId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContractContent { get; set; }
        public string? CustomTerms { get; set; }

        // Creator information
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;

        // Dates
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? SignatureDeadline { get; set; }
        public DateTime? FullySignedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? TerminatedAt { get; set; }

        // Signature information
        public List<ContractSignature> Signatures { get; set; } = new();
        public int TotalSignatories { get; set; }
        public int SignedCount { get; set; }
        public int PendingCount { get; set; }
        public int DeclinedCount { get; set; }
        public bool IsFullySigned { get; set; }
        public bool RequiresMySignature { get; set; }
        public string? MySignatureStatus { get; set; }

        // Attachments
        public List<string> AttachmentUrls { get; set; } = new();

        // Additional info
        public bool AutoActivate { get; set; }
        public string? TerminationReason { get; set; }
        public int? TerminatedByUserId { get; set; }
        public string? TerminatedByUserName { get; set; }
    }

    /// <summary>
    /// Contract signature details
    /// </summary>
    public class ContractSignature
    {
        public int SignatureId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SignedAt { get; set; }
        public DateTime? DeclinedAt { get; set; }
        public string? DeclineReason { get; set; }
        public string? SignerNotes { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Geolocation { get; set; }
        public bool IsRequired { get; set; }
        public int SignatureOrder { get; set; }
    }

    /// <summary>
    /// Summary of a contract for list view
    /// </summary>
    public class ContractSummary
    {
        public int ContractId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int TotalSignatories { get; set; }
        public int SignedCount { get; set; }
        public bool IsFullySigned { get; set; }
        public bool RequiresMySignature { get; set; }
        public string? MySignatureStatus { get; set; }
        public int DaysUntilExpiry { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    /// <summary>
    /// Paginated contract list response
    /// </summary>
    public class ContractListResponse
    {
        public List<ContractSummary> Contracts { get; set; } = new();
        public ContractStatistics Statistics { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Contract statistics
    /// </summary>
    public class ContractStatistics
    {
        public int TotalContracts { get; set; }
        public int DraftContracts { get; set; }
        public int PendingSignatures { get; set; }
        public int ActiveContracts { get; set; }
        public int ExpiredContracts { get; set; }
        public int TerminatedContracts { get; set; }
        public int PendingMySignature { get; set; }
        public int SignedByMe { get; set; }
        public int CreatedByMe { get; set; }
        public int ExpiringWithin30Days { get; set; }
    }

    /// <summary>
    /// Pagination information
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    /// <summary>
    /// Contract template information
    /// </summary>
    public class ContractTemplateResponse
    {
        public string TemplateType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
        public bool RequiresAllCoOwnersSignature { get; set; }
        public int MinimumSignatories { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    #endregion
}
