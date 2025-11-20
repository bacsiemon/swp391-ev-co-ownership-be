namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for contract templates
    /// </summary>
    public enum EContractTemplateStatus
    {
        /// <summary>
        /// Template is in draft state
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Template is active and can be used
        /// </summary>
        Active = 1,

        /// <summary>
        /// Template is inactive and cannot be used
        /// </summary>
        Inactive = 2,

        /// <summary>
        /// Template has been archived
        /// </summary>
        Archived = 3
    }
}
