using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.PaymentDTOs
{
    /// <summary>
    /// Invoice for payment - replaces direct payment approach
    /// </summary>
    public class InvoiceResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        public EInvoiceType InvoiceType { get; set; }
        public string InvoiceTypeName { get; set; } = string.Empty;
        
        public EInvoiceStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        
        public string Description { get; set; } = string.Empty;
        public string? Notes { get; set; }
        
        // Related entities
        public int? BookingId { get; set; }
        public int? FundAdditionId { get; set; }
        public int? MaintenanceId { get; set; }
        public int? VehicleId { get; set; }
        
        // Payment information
        public int? PaymentId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create invoice
    /// </summary>
    public class CreateInvoiceRequest
    {
        public decimal Amount { get; set; }
        public EInvoiceType InvoiceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Notes { get; set; }
        
        // Related entity (one of these should be provided)
        public int? BookingId { get; set; }
        public int? FundAdditionId { get; set; }
        public int? MaintenanceId { get; set; }
        public int? VehicleId { get; set; }
        
        public int? DueDays { get; set; } = 7; // Default 7 days to pay
    }

    /// <summary>
    /// Request to pay an invoice
    /// </summary>
    public class PayInvoiceRequest
    {
        public int InvoiceId { get; set; }
        public EPaymentGateway PaymentGateway { get; set; } = EPaymentGateway.VNPay;
        public EPaymentMethod? PaymentMethod { get; set; }
        public string? BankCode { get; set; }
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// Invoice payment result
    /// </summary>
    public class InvoicePaymentResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    /// <summary>
    /// Receipt data for invoice
    /// </summary>
    public class ReceiptResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        
        // Company information
        public CompanyInfo Company { get; set; } = new();
        
        // Customer information
        public CustomerInfo Customer { get; set; } = new();
        
        // Invoice details
        public List<ReceiptLineItem> LineItems { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Payment information
        public DateTime IssueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        
        public string Notes { get; set; } = string.Empty;
    }

    public class CompanyInfo
    {
        public string Name { get; set; } = "EV Co-Ownership Platform";
        public string Address { get; set; } = "123 EV Street, Ho Chi Minh City";
        public string Phone { get; set; } = "1900-xxxx";
        public string Email { get; set; } = "support@evcoownership.com";
        public string TaxCode { get; set; } = "0123456789";
    }

    public class CustomerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class ReceiptLineItem
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Payment reminder request
    /// </summary>
    public class PaymentReminderRequest
    {
        public int? InvoiceId { get; set; }
        public int? UserId { get; set; }
        public List<int>? InvoiceIds { get; set; }
        public string? CustomMessage { get; set; }
    }

    /// <summary>
    /// Group finance summary
    /// </summary>
    public class GroupFinanceSummary
    {
        // Maintenance fund
        public FundSummary MaintenanceFund { get; set; } = new();
        
        // Common fund (general operations)
        public FundSummary CommonFund { get; set; } = new();
        
        // Total statistics
        public decimal TotalBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        
        // Recent transactions
        public List<FundTransaction> RecentTransactions { get; set; } = new();
        
        // Pending payments
        public List<InvoiceResponse> PendingInvoices { get; set; } = new();
        public decimal TotalPendingAmount { get; set; }
    }

    public class FundSummary
    {
        public string FundName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public int VehicleCount { get; set; }
        public List<VehicleFundBreakdown> VehicleBreakdown { get; set; } = new();
    }

    public class VehicleFundBreakdown
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal MonthlyContribution { get; set; }
        public int CoOwnerCount { get; set; }
    }

    public class FundTransaction
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty; // Income/Expense
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? VehicleId { get; set; }
        public string? VehicleName { get; set; }
    }
}

namespace EvCoOwnership.Repositories.Enums
{
    public enum EInvoiceType
    {
        Booking = 0,
        FundContribution = 1,
        Maintenance = 2,
        MonthlyFee = 3,
        Other = 4
    }

    public enum EInvoiceStatus
    {
        Draft = 0,
        Pending = 1,
        Paid = 2,
        Overdue = 3,
        Cancelled = 4,
        Refunded = 5
    }
}
