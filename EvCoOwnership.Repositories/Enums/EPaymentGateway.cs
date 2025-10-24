namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents payment gateway providers
    /// </summary>
    public enum EPaymentGateway
    {
        /// <summary>
        /// VNPay payment gateway (default)
        /// </summary>
        VNPay = 0,

        /// <summary>
        /// Momo e-wallet
        /// </summary>
        Momo = 1,

        /// <summary>
        /// ZaloPay e-wallet
        /// </summary>
        ZaloPay = 2,

        /// <summary>
        /// ShopeePay e-wallet
        /// </summary>
        ShopeePay = 3,

        /// <summary>
        /// ViettelPay e-wallet
        /// </summary>
        ViettelPay = 4,

        /// <summary>
        /// Direct bank transfer
        /// </summary>
        BankTransfer = 5
    }
}
