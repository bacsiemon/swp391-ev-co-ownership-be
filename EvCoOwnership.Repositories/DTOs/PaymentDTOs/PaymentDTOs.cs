using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.PaymentDTOs
{
    /// <summary>
    /// Request to create a new payment transaction
    /// </summary>
    public class CreatePaymentRequest
    {
        /// <summary>
        /// Amount to pay (VND)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment gateway to use
        /// </summary>
        public EPaymentGateway PaymentGateway { get; set; } = EPaymentGateway.VNPay;

        /// <summary>
        /// Payment method (for VNPay: CreditCard, OnlineBanking, EWallet, QRCode)
        /// </summary>
        public EPaymentMethod? PaymentMethod { get; set; }

        /// <summary>
        /// Payment type/purpose
        /// </summary>
        public EPaymentType PaymentType { get; set; }

        /// <summary>
        /// Related fund addition ID (if applicable)
        /// </summary>
        public int? FundAdditionId { get; set; }

        /// <summary>
        /// Related booking ID (if payment for booking)
        /// </summary>
        public int? BookingId { get; set; }

        /// <summary>
        /// Related maintenance ID (if payment for maintenance)
        /// </summary>
        public int? MaintenanceId { get; set; }

        /// <summary>
        /// Bank code for online banking (e.g., VIETCOMBANK, BIDV)
        /// </summary>
        public string? BankCode { get; set; }

        /// <summary>
        /// E-wallet provider (for non-VNPay e-wallets)
        /// </summary>
        public string? EWalletProvider { get; set; }

        /// <summary>
        /// Payment description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Return URL after payment completion
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Cancel URL if user cancels payment
        /// </summary>
        public string? CancelUrl { get; set; }
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
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public EPaymentGateway PaymentGateway { get; set; }
        public string PaymentGatewayName { get; set; } = string.Empty;
        public EPaymentMethod? PaymentMethod { get; set; }
        public string? PaymentMethodName { get; set; }
        public EPaymentType PaymentType { get; set; }
        public string PaymentTypeName { get; set; } = string.Empty;
        public EPaymentStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FundAdditionId { get; set; }
        public int? BookingId { get; set; }
        public int? MaintenanceId { get; set; }
        public string? BankCode { get; set; }
        public string? EWalletProvider { get; set; }
        public string? Description { get; set; }
        public string? FailureReason { get; set; }
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
        public EPaymentGateway Gateway { get; set; }
        public string GatewayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    /// <summary>
    /// Response containing available payment gateways
    /// </summary>
    public class PaymentGatewaysResponse
    {
        public List<PaymentGatewayInfo> Gateways { get; set; } = new List<PaymentGatewayInfo>();
    }

    public class PaymentGatewayInfo
    {
        public EPaymentGateway Gateway { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string? Logo { get; set; }
        public List<string>? SupportedMethods { get; set; }
        public List<string>? SupportedBanks { get; set; }
    }
}

