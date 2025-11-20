namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the role types for group members
    /// </summary>
    public enum EGroupMemberRole
    {
        /// <summary>
        /// Regular member role
        /// </summary>
        Member = 0,

        /// <summary>
        /// Admin role with elevated permissions
        /// </summary>
        Admin = 1,

        /// <summary>
        /// Owner role with full control
        /// </summary>
        Owner = 2
    }
}
