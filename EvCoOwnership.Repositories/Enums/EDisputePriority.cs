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
        Low = 0,

        /// <summary>
        /// Medium priority - moderate impact
        /// </summary>
        Medium = 1,

        /// <summary>
        /// High priority - significant impact
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority - requires immediate attention
        /// </summary>
        Critical = 3
    }
}
