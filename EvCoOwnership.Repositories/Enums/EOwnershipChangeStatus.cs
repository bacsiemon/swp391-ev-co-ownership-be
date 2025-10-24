namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status of an ownership change request
    /// </summary>
    public enum EOwnershipChangeStatus
    {
        /// <summary>
        /// Request is pending approval from co-owners
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Request has been approved by all required co-owners and applied
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Request has been rejected by one or more co-owners
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Request was cancelled by the proposer
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Request has expired without receiving all approvals
        /// </summary>
        Expired = 4
    }

    /// <summary>
    /// Represents the approval status of an individual co-owner
    /// </summary>
    public enum EApprovalStatus
    {
        /// <summary>
        /// Co-owner has not yet responded
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Co-owner has approved the change
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Co-owner has rejected the change
        /// </summary>
        Rejected = 2
    }
}
