using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.ContractTemplateDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to create a new contract template
    /// </summary>
    public class CreateContractTemplateRequest
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Template version (e.g., "1.0", "2.1")
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Template description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Template content/body
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Terms and conditions
        /// </summary>
        public string TermsAndConditions { get; set; } = string.Empty;

        /// <summary>
        /// Initial status (default: Draft)
        /// </summary>
        public EContractTemplateStatus Status { get; set; } = EContractTemplateStatus.Draft;
    }

    public class CreateContractTemplateRequestValidator : AbstractValidator<CreateContractTemplateRequest>
    {
        public CreateContractTemplateRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("TEMPLATE_NAME_REQUIRED")
                .Length(3, 200).WithMessage("TEMPLATE_NAME_LENGTH_INVALID");

            RuleFor(x => x.Version)
                .NotEmpty().WithMessage("TEMPLATE_VERSION_REQUIRED")
                .Length(1, 10).WithMessage("TEMPLATE_VERSION_LENGTH_INVALID")
                .Matches(@"^[0-9]+\.[0-9]+(\.[0-9]+)?$").WithMessage("TEMPLATE_VERSION_FORMAT_INVALID");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("TEMPLATE_DESCRIPTION_REQUIRED")
                .Length(10, 1000).WithMessage("TEMPLATE_DESCRIPTION_LENGTH_INVALID");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("TEMPLATE_CONTENT_REQUIRED")
                .MinimumLength(50).WithMessage("TEMPLATE_CONTENT_TOO_SHORT");

            RuleFor(x => x.TermsAndConditions)
                .NotEmpty().WithMessage("TEMPLATE_TERMS_REQUIRED")
                .MinimumLength(20).WithMessage("TEMPLATE_TERMS_TOO_SHORT");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("TEMPLATE_STATUS_INVALID");
        }
    }

    /// <summary>
    /// Request to update an existing contract template
    /// </summary>
    public class UpdateContractTemplateRequest
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Template version
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Template description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Template content/body
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Terms and conditions
        /// </summary>
        public string? TermsAndConditions { get; set; }

        /// <summary>
        /// Template status
        /// </summary>
        public EContractTemplateStatus? Status { get; set; }
    }

    public class UpdateContractTemplateRequestValidator : AbstractValidator<UpdateContractTemplateRequest>
    {
        public UpdateContractTemplateRequestValidator()
        {
            RuleFor(x => x.Name)
                .Length(3, 200).WithMessage("TEMPLATE_NAME_LENGTH_INVALID")
                .When(x => x.Name != null);

            RuleFor(x => x.Version)
                .Length(1, 10).WithMessage("TEMPLATE_VERSION_LENGTH_INVALID")
                .Matches(@"^[0-9]+\.[0-9]+(\.[0-9]+)?$").WithMessage("TEMPLATE_VERSION_FORMAT_INVALID")
                .When(x => x.Version != null);

            RuleFor(x => x.Description)
                .Length(10, 1000).WithMessage("TEMPLATE_DESCRIPTION_LENGTH_INVALID")
                .When(x => x.Description != null);

            RuleFor(x => x.Content)
                .MinimumLength(50).WithMessage("TEMPLATE_CONTENT_TOO_SHORT")
                .When(x => x.Content != null);

            RuleFor(x => x.TermsAndConditions)
                .MinimumLength(20).WithMessage("TEMPLATE_TERMS_TOO_SHORT")
                .When(x => x.TermsAndConditions != null);

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("TEMPLATE_STATUS_INVALID")
                .When(x => x.Status != null);
        }
    }

    /// <summary>
    /// Request to get contract templates with filters
    /// </summary>
    public class GetContractTemplatesRequest
    {
        /// <summary>
        /// Filter by status
        /// </summary>
        public EContractTemplateStatus? Status { get; set; }

        /// <summary>
        /// Filter by name (partial match)
        /// </summary>
        public string? NameSearch { get; set; }

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

    public class GetContractTemplatesRequestValidator : AbstractValidator<GetContractTemplatesRequest>
    {
        public GetContractTemplatesRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PAGE_NUMBER_INVALID");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PAGE_SIZE_INVALID");

            RuleFor(x => x.SortBy)
                .Must(x => new[] { "CreatedAt", "UpdatedAt", "Name", "Version", "Status" }.Contains(x))
                .WithMessage("SORT_BY_INVALID");

            RuleFor(x => x.SortOrder)
                .Must(x => new[] { "asc", "desc" }.Contains(x.ToLower()))
                .WithMessage("SORT_ORDER_INVALID");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("STATUS_FILTER_INVALID")
                .When(x => x.Status.HasValue);
        }
    }

    /// <summary>
    /// Request to approve/activate a contract template
    /// </summary>
    public class ApproveContractTemplateRequest
    {
        /// <summary>
        /// Optional approval notes
        /// </summary>
        public string? Notes { get; set; }
    }

    public class ApproveContractTemplateRequestValidator : AbstractValidator<ApproveContractTemplateRequest>
    {
        public ApproveContractTemplateRequestValidator()
        {
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("APPROVAL_NOTES_TOO_LONG")
                .When(x => x.Notes != null);
        }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Contract template response
    /// </summary>
    public class ContractTemplateResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TermsAndConditions { get; set; } = string.Empty;
        public EContractTemplateStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        
        // Creator information
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        
        // Approval information
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Usage statistics
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
        public bool CanEdit { get; set; }
        public bool CanApprove { get; set; }
    }

    /// <summary>
    /// Contract template summary for list views
    /// </summary>
    public class ContractTemplateSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EContractTemplateStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Paginated contract templates list response
    /// </summary>
    public class ContractTemplatesListResponse
    {
        public List<ContractTemplateSummary> Templates { get; set; } = new();
        public ContractTemplateStatistics Statistics { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Contract template statistics
    /// </summary>
    public class ContractTemplateStatistics
    {
        public int TotalTemplates { get; set; }
        public int DraftTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int InactiveTemplates { get; set; }
        public int ArchivedTemplates { get; set; }
        public int TotalUsage { get; set; }
        public string? MostUsedTemplate { get; set; }
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

    #endregion
}