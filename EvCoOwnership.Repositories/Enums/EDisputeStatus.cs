namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Status of a dispute
    /// </summary>
    public enum EDisputeStatus
    {
        /// <summary>
        /// Dispute has been opened and awaiting review
        /// </summary>
        Open = 1,

        /// <summary>
        /// Dispute is under investigation
        /// </summary>
        UnderReview = 2,

        /// <summary>
        /// Dispute requires mediation from admin or third party
        /// </summary>
        InMediation = 3,

        /// <summary>
        /// Dispute has been resolved successfully
        /// </summary>
        Resolved = 4,

        /// <summary>
        /// Dispute was rejected/dismissed
        /// </summary>
        Rejected = 5,

        /// <summary>
        /// Dispute was withdrawn by the initiator
        /// </summary>
        Withdrawn = 6,

        /// <summary>
        /// Dispute requires escalation to higher authority
        /// </summary>
        Escalated = 7
    }
}
