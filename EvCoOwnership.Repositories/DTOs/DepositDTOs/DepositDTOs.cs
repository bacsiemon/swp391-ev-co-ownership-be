using EvCoOwnership.Repositories.Enums;
using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.DepositDTOs
{
    #region Request DTOs

    /// <summary>
    /// Request to create a new deposit transaction
    /// </summary>
    public class CreateDepositRequest
    {
        /// <summary>
        /// Amount to deposit (VND)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Deposit method (CreditCard, EWallet, OnlineBanking)
        /// </summary>
        public EDepositMethod DepositMethod { get; set; }

        /// <summary>
        /// Optional: Specific bank code for online banking
        /// </summary>
        public string? BankCode { get; set; }

        /// <summary>
        /// Optional: E-wallet provider (MOMO, ZALOPAY)
        /// </summary>
        public string? EWalletProvider { get; set; }

        /// <summary>
        /// Optional description for the deposit
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Return URL after payment completion (optional, system default if not provided)
        /// </summary>
        public string? ReturnUrl { get; set; }
    }

    public class CreateDepositValidator : AbstractValidator<CreateDepositRequest>
    {
        public CreateDepositValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("DEPOSIT_AMOUNT_MUST_BE_POSITIVE")
                .LessThanOrEqualTo(1000000000).WithMessage("DEPOSIT_AMOUNT_TOO_LARGE"); // Max 1 billion VND

            RuleFor(x => x.DepositMethod)
                .IsInEnum().WithMessage("INVALID_DEPOSIT_METHOD");

            RuleFor(x => x.BankCode)
                .MaximumLength(50).WithMessage("BANK_CODE_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.BankCode));

            RuleFor(x => x.EWalletProvider)
                .MaximumLength(50).WithMessage("EWALLET_PROVIDER_TOO_LONG")
                .Must(provider => provider == null || provider.ToUpper() == "MOMO" || provider.ToUpper() == "ZALOPAY")
                .WithMessage("INVALID_EWALLET_PROVIDER")
                .When(x => !string.IsNullOrEmpty(x.EWalletProvider));

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("DESCRIPTION_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.ReturnUrl)
                .MaximumLength(1000).WithMessage("RETURN_URL_TOO_LONG")
                .When(x => !string.IsNullOrEmpty(x.ReturnUrl));
        }
    }

    /// <summary>
    /// Request to get deposit history with filters
    /// </summary>
    public class GetDepositsRequest
    {
        /// <summary>
        /// Filter by deposit method
        /// </summary>
        public EDepositMethod? DepositMethod { get; set; }

        /// <summary>
        /// Filter by deposit status
        /// </summary>
        public EDepositStatus? Status { get; set; }

        /// <summary>
        /// Filter deposits created from this date
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter deposits created to this date
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Minimum deposit amount
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum deposit amount
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Page number (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size (default: 20)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by field (CreatedAt, Amount, Status)
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Sort order (asc/desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }

    public class GetDepositsValidator : AbstractValidator<GetDepositsRequest>
    {
        public GetDepositsValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PAGE_NUMBER_MUST_BE_POSITIVE");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PAGE_SIZE_MUST_BE_BETWEEN_1_AND_100");

            RuleFor(x => x.SortBy)
                .Must(sortBy => sortBy == "CreatedAt" || sortBy == "Amount" || sortBy == "Status" || sortBy == "CompletedAt")
                .WithMessage("INVALID_SORT_BY");

            RuleFor(x => x.SortOrder)
                .Must(order => order.ToLower() == "asc" || order.ToLower() == "desc")
                .WithMessage("INVALID_SORT_ORDER");

            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(0).WithMessage("MIN_AMOUNT_MUST_BE_NON_NEGATIVE")
                .When(x => x.MinAmount.HasValue);

            RuleFor(x => x.MaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("MAX_AMOUNT_MUST_BE_NON_NEGATIVE")
                .GreaterThanOrEqualTo(x => x.MinAmount ?? 0).WithMessage("MAX_AMOUNT_MUST_BE_GREATER_THAN_MIN_AMOUNT")
                .When(x => x.MaxAmount.HasValue);
        }
    }

    /// <summary>
    /// Request to verify deposit callback from payment gateway
    /// </summary>
    public class VerifyDepositCallbackRequest
    {
        /// <summary>
        /// Deposit transaction ID
        /// </summary>
        public int DepositId { get; set; }

        /// <summary>
        /// Payment gateway transaction ID
        /// </summary>
        public string GatewayTransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Payment status from gateway
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Payment gateway response code
        /// </summary>
        public string? ResponseCode { get; set; }

        /// <summary>
        /// Secure hash for verification
        /// </summary>
        public string? SecureHash { get; set; }
    }

    public class VerifyDepositCallbackValidator : AbstractValidator<VerifyDepositCallbackRequest>
    {
        public VerifyDepositCallbackValidator()
        {
            RuleFor(x => x.DepositId)
                .GreaterThan(0).WithMessage("DEPOSIT_ID_REQUIRED");

            RuleFor(x => x.GatewayTransactionId)
                .NotEmpty().WithMessage("GATEWAY_TRANSACTION_ID_REQUIRED")
                .MaximumLength(200).WithMessage("GATEWAY_TRANSACTION_ID_TOO_LONG");
        }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response containing deposit payment URL
    /// </summary>
    public class DepositPaymentUrlResponse
    {
        /// <summary>
        /// URL to redirect user for payment
        /// </summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// Deposit transaction ID
        /// </summary>
        public int DepositId { get; set; }

        /// <summary>
        /// Transaction reference number
        /// </summary>
        public string TransactionRef { get; set; } = string.Empty;

        /// <summary>
        /// Deposit method used
        /// </summary>
        public EDepositMethod DepositMethod { get; set; }

        /// <summary>
        /// Amount to be deposited
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment expiry time (UTC)
        /// </summary>
        public DateTime ExpiryTime { get; set; }
    }

    /// <summary>
    /// Detailed deposit transaction information
    /// </summary>
    public class DepositResponse
    {
        /// <summary>
        /// Deposit ID
        /// </summary>
        public int DepositId { get; set; }

        /// <summary>
        /// User ID who made the deposit
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User email
        /// </summary>
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Deposit amount (VND)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Deposit method
        /// </summary>
        public EDepositMethod DepositMethod { get; set; }

        /// <summary>
        /// Deposit method display name
        /// </summary>
        public string DepositMethodName { get; set; } = string.Empty;

        /// <summary>
        /// Deposit status
        /// </summary>
        public EDepositStatus Status { get; set; }

        /// <summary>
        /// Deposit status display name
        /// </summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>
        /// Payment gateway used
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Gateway transaction ID
        /// </summary>
        public string? GatewayTransactionId { get; set; }

        /// <summary>
        /// Bank code (for online banking)
        /// </summary>
        public string? BankCode { get; set; }

        /// <summary>
        /// E-wallet provider (MOMO, ZALOPAY)
        /// </summary>
        public string? EWalletProvider { get; set; }

        /// <summary>
        /// Transaction reference number
        /// </summary>
        public string TransactionRef { get; set; } = string.Empty;

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Created date (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Completed date (UTC)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Failed date (UTC)
        /// </summary>
        public DateTime? FailedAt { get; set; }

        /// <summary>
        /// Cancellation date (UTC)
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Expiry time for payment (UTC)
        /// </summary>
        public DateTime? ExpiryTime { get; set; }

        /// <summary>
        /// Failure reason
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// IP address of user
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Related payment ID (if created)
        /// </summary>
        public int? PaymentId { get; set; }
    }

    /// <summary>
    /// Summary of deposit transaction for list view
    /// </summary>
    public class DepositSummary
    {
        public int DepositId { get; set; }
        public decimal Amount { get; set; }
        public EDepositMethod DepositMethod { get; set; }
        public string DepositMethodName { get; set; } = string.Empty;
        public EDepositStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
    }

    /// <summary>
    /// Paginated list of deposits with statistics
    /// </summary>
    public class DepositListResponse
    {
        /// <summary>
        /// List of deposit summaries
        /// </summary>
        public List<DepositSummary> Deposits { get; set; } = new List<DepositSummary>();

        /// <summary>
        /// Deposit statistics
        /// </summary>
        public DepositStatistics Statistics { get; set; } = new DepositStatistics();

        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }

    /// <summary>
    /// Deposit statistics
    /// </summary>
    public class DepositStatistics
    {
        /// <summary>
        /// Total number of deposits
        /// </summary>
        public int TotalDeposits { get; set; }

        /// <summary>
        /// Number of pending deposits
        /// </summary>
        public int PendingDeposits { get; set; }

        /// <summary>
        /// Number of completed deposits
        /// </summary>
        public int CompletedDeposits { get; set; }

        /// <summary>
        /// Number of failed deposits
        /// </summary>
        public int FailedDeposits { get; set; }

        /// <summary>
        /// Number of cancelled deposits
        /// </summary>
        public int CancelledDeposits { get; set; }

        /// <summary>
        /// Total amount of all deposits
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Total amount of completed deposits
        /// </summary>
        public decimal CompletedAmount { get; set; }

        /// <summary>
        /// Total amount of pending deposits
        /// </summary>
        public decimal PendingAmount { get; set; }

        /// <summary>
        /// Breakdown by deposit method
        /// </summary>
        public Dictionary<string, decimal> ByMethod { get; set; } = new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Pagination metadata
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
    /// Supported payment methods information
    /// </summary>
    public class PaymentMethodsResponse
    {
        /// <summary>
        /// Available deposit methods
        /// </summary>
        public List<DepositMethodInfo> Methods { get; set; } = new List<DepositMethodInfo>();
    }

    public class DepositMethodInfo
    {
        public EDepositMethod Method { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string? Icon { get; set; }
        public List<string>? SupportedBanks { get; set; }
        public List<string>? SupportedEWallets { get; set; }
    }

    #endregion
}
