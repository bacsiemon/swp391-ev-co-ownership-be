namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the type of group votes
    /// </summary>
    public enum EGroupVoteType
    {
        /// <summary>
        /// Vote for maintenance decisions
        /// </summary>
        Maintenance = 0,

        /// <summary>
        /// Vote for purchase decisions
        /// </summary>
        Purchase = 1,

        /// <summary>
        /// Vote for upgrade decisions
        /// </summary>
        Upgrade = 2,

        /// <summary>
        /// General vote for other decisions
        /// </summary>
        General = 3
    }
}
