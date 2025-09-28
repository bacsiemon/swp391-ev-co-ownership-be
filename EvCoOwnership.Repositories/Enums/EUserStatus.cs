namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for users in the system
    /// </summary>
    public enum EUserStatus
    {
        /// <summary>
        /// User is active and can use the system
        /// </summary>
        Active = 0,

        /// <summary>
        /// User is inactive
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// User is suspended from using the system
        /// </summary>
        Suspended = 2
    }
}