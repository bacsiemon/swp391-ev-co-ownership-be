namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Priority level of a dispute
    /// </summary>
    public enum EDisputePriority
    {
        /// <summary>
        /// Low priority - minor issues
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium priority - moderate impact
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High priority - significant impact
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical priority - requires immediate attention
        /// </summary>
        Critical = 4
    }
}
