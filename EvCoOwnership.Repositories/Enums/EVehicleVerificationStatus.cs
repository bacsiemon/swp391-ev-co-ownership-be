namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the verification status types for vehicles in the system
    /// </summary>
    public enum EVehicleVerificationStatus
    {
        /// <summary>
        /// Vehicle verification is pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Vehicle has been verified
        /// </summary>
        Verified = 1,

        /// <summary>
        /// Vehicle verification was rejected
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Vehicle requires recheck
        /// </summary>
        RequiresRecheck = 3
    }
}