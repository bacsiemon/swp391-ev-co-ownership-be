namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the type of ownership change
    /// </summary>
    public enum EOwnershipChangeType
    {
        /// <summary>
        /// Initial ownership allocation when co-owner joins
        /// </summary>
        Initial = 0,

        /// <summary>
        /// Ownership adjustment through group consensus
        /// </summary>
        Adjustment = 1,

        /// <summary>
        /// Ownership transfer between co-owners
        /// </summary>
        Transfer = 2,

        /// <summary>
        /// Co-owner exits and ownership redistributed
        /// </summary>
        Exit = 3,

        /// <summary>
        /// New co-owner joins and ownership redistributed
        /// </summary>
        NewMember = 4,

        /// <summary>
        /// Manual admin adjustment
        /// </summary>
        AdminAdjustment = 5
    }
}
