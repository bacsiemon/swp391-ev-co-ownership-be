namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents deposit methods for adding funds to user wallet/account
    /// </summary>
    public enum EDepositMethod
    {
        /// <summary>
        /// Credit card deposit via VNPay gateway
        /// </summary>
        CreditCard = 0,

        /// <summary>
        /// E-wallet deposit (Momo, ZaloPay, etc.)
        /// </summary>
        EWallet = 1,

        /// <summary>
        /// Online banking/internet banking deposit via VNPay
        /// </summary>
        OnlineBanking = 2,

        /// <summary>
        /// QR code payment via VNPay
        /// </summary>
        QRCode = 3
    }
}
