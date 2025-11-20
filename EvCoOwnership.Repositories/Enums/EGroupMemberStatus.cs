namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for group members
    /// </summary>
    public enum EGroupMemberStatus
    {
        /// <summary>
        /// Member is active in the group
        /// </summary>
        Active = 0,

        /// <summary>
        /// Member invitation is pending
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Member has been removed from the group
        /// </summary>
        Removed = 2
    }
}
