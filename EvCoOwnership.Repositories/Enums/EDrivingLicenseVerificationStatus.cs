namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the verification status types for driving licenses in the system
    /// </summary>
    public enum EDrivingLicenseVerificationStatus
    {
        /// <summary>
        /// License is pending verification
        /// </summary>
        Pending = 0,

        /// <summary>
        /// License has been verified and is valid
        /// </summary>
        Verified = 1,

        /// <summary>
        /// License verification has been rejected
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// License has expired
        /// </summary>
        Expired = 3
    }
}