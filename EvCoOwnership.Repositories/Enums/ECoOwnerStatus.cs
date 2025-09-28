namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for co-owners in the system
    /// </summary>
    public enum ECoOwnerStatus
    {
        /// <summary>
        /// Co-owner is active in the group
        /// </summary>
        Active = 0,

        /// <summary>
        /// Co-owner application is pending approval
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Co-owner has left the group
        /// </summary>
        Left = 2
    }
}