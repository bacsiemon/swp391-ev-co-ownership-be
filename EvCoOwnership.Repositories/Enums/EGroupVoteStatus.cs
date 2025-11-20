namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for group votes
    /// </summary>
    public enum EGroupVoteStatus
    {
        /// <summary>
        /// Vote is active and accepting responses
        /// </summary>
        Active = 0,

        /// <summary>
        /// Vote has been completed
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Vote has been cancelled
        /// </summary>
        Cancelled = 2
    }
}
