using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.PaymentDTOs
{
    public class CreatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentGateway { get; set; } = "VNPay";
        public int? FundAdditionId { get; set; }
        public string? Description { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public int PaymentId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }

    public class PaymentResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string PaymentGateway { get; set; } = string.Empty;
        public EPaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FundAdditionId { get; set; }
    }

    public class PaymentStatisticsResponse
    {
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CompletedAmount { get; set; }
        public decimal PendingAmount { get; set; }
    }

    public class PaymentUrlResponse
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
    }
}
