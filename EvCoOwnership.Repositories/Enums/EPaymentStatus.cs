namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for payments in the system
    /// </summary>
    public enum EPaymentStatus
    {
        /// <summary>
        /// Payment is pending processing
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Payment has been completed successfully
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Payment has failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Payment has been refunded
        /// </summary>
        Refunded = 3
    }
}