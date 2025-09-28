namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the usage status types for services in the system
    /// </summary>
    public enum EServiceUsageStatus
    {
        /// <summary>
        /// Service usage is scheduled
        /// </summary>
        Scheduled = 0,

        /// <summary>
        /// Service usage is currently in progress
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Service usage has been completed
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Service usage has been cancelled
        /// </summary>
        Cancelled = 3
    }
}