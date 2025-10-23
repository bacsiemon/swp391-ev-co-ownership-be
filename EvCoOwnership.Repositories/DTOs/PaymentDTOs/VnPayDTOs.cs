namespace EvCoOwnership.Repositories.DTOs.PaymentDTOs
{
    /// <summary>
    /// VNPay callback/return request
    /// </summary>
    public class VnPayCallbackRequest
    {
        public string vnp_TmnCode { get; set; } = string.Empty;
        public string vnp_Amount { get; set; } = string.Empty;
        public string vnp_BankCode { get; set; } = string.Empty;
        public string vnp_BankTranNo { get; set; } = string.Empty;
        public string vnp_CardType { get; set; } = string.Empty;
        public string vnp_PayDate { get; set; } = string.Empty;
        public string vnp_OrderInfo { get; set; } = string.Empty;
        public string vnp_TransactionNo { get; set; } = string.Empty;
        public string vnp_ResponseCode { get; set; } = string.Empty;
        public string vnp_TransactionStatus { get; set; } = string.Empty;
        public string vnp_TxnRef { get; set; } = string.Empty;
        public string vnp_SecureHashType { get; set; } = string.Empty;
        public string vnp_SecureHash { get; set; } = string.Empty;
        
        // Additional fields that VNPay may return
        public string vnp_Version { get; set; } = string.Empty;
        public string vnp_Command { get; set; } = string.Empty;
        public string vnp_CurrCode { get; set; } = string.Empty;
        public string vnp_Locale { get; set; } = string.Empty;
        public string vnp_IpAddr { get; set; } = string.Empty;
        public string vnp_CreateDate { get; set; } = string.Empty;
    }

    /// <summary>
    /// VNPay callback response
    /// </summary>
    public class VnPayCallbackResponse
    {
        public string RspCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string BankTranNo { get; set; } = string.Empty;
        public string PayDate { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
