namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for group fund operations
    /// </summary>
    public enum EGroupFundStatus
    {
        /// <summary>
        /// Fund operation is active and in progress
        /// </summary>
        Active = 0,

        /// <summary>
        /// Fund operation has been completed
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Fund operation has been cancelled
        /// </summary>
        Cancelled = 2
    }
}
