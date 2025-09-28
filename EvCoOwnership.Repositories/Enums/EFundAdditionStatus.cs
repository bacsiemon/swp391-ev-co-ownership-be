namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for fund additions in the system
    /// </summary>
    public enum EFundAdditionStatus
    {
        /// <summary>
        /// Fund addition is pending processing
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Fund addition has been completed successfully
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Fund addition has failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Fund addition has been refunded
        /// </summary>
        Refunded = 3
    }
}