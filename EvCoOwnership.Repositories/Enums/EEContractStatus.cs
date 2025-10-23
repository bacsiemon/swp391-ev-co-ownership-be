namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Overall status of an e-contract
    /// </summary>
    public enum EEContractStatus
    {
        /// <summary>
        /// Contract is in draft state
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Contract is pending signatures from all parties
        /// </summary>
        PendingSignatures = 1,

        /// <summary>
        /// Contract is partially signed (some but not all parties signed)
        /// </summary>
        PartiallySigned = 2,

        /// <summary>
        /// Contract is fully signed by all required parties
        /// </summary>
        FullySigned = 3,

        /// <summary>
        /// Contract is active and in effect
        /// </summary>
        Active = 4,

        /// <summary>
        /// Contract has expired
        /// </summary>
        Expired = 5,

        /// <summary>
        /// Contract was terminated/cancelled
        /// </summary>
        Terminated = 6,

        /// <summary>
        /// Contract was rejected by one or more parties
        /// </summary>
        Rejected = 7,

        /// <summary>
        /// Contract is under dispute
        /// </summary>
        UnderDispute = 8
    }
}
