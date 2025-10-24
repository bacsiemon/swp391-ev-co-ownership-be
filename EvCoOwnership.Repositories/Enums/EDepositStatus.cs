namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status of a deposit transaction
    /// </summary>
    public enum EDepositStatus
    {
        /// <summary>
        /// Deposit is pending payment
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Deposit is being processed by payment gateway
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Deposit completed successfully and funds added to account
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Deposit failed due to payment error
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Deposit cancelled by user
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Deposit expired (user didn't complete payment)
        /// </summary>
        Expired = 5,

        /// <summary>
        /// Deposit refunded (reversal of completed transaction)
        /// </summary>
        Refunded = 6
    }
}
