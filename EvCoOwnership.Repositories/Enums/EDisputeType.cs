namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Types of disputes that can be raised
    /// </summary>
    public enum EDisputeType
    {
        /// <summary>
        /// Dispute related to booking issues (cancellation, unauthorized usage, etc.)
        /// </summary>
        Booking = 0,

        /// <summary>
        /// Dispute related to cost sharing and payments
        /// </summary>
        CostSharing = 1,

        /// <summary>
        /// Dispute related to group decisions (voting, proposals, etc.)
        /// </summary>
        GroupDecision = 2,

        /// <summary>
        /// Dispute related to vehicle damage
        /// </summary>
        VehicleDamage = 3,

        /// <summary>
        /// Dispute related to ownership changes
        /// </summary>
        OwnershipChange = 4,

        /// <summary>
        /// Other types of disputes
        /// </summary>
        Other = 99
    }
}
