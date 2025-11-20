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
        Open = 0,

        /// <summary>
        /// Dispute is under investigation
        /// </summary>
        UnderReview = 1,

        /// <summary>
        /// Dispute requires mediation from admin or third party
        /// </summary>
        InMediation = 2,

        /// <summary>
        /// Dispute has been resolved successfully
        /// </summary>
        Resolved = 3,

        /// <summary>
        /// Dispute was rejected/dismissed
        /// </summary>
        Rejected = 4,

        /// <summary>
        /// Dispute was withdrawn by the initiator
        /// </summary>
        Withdrawn = 5,

        /// <summary>
        /// Dispute requires escalation to higher authority
        /// </summary>
        Escalated = 6
    }
}
