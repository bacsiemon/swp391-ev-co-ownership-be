namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for contracts in the system
    /// </summary>
    public enum EContractStatus
    {
        /// <summary>
        /// Contract is pending approval
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Contract is active
        /// </summary>
        Active = 1,

        /// <summary>
        /// Contract has been rejected
        /// </summary>
        Rejected = 2
    }
}