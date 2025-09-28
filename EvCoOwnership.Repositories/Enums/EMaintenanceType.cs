namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the maintenance types for vehicles in the system
    /// </summary>
    public enum EMaintenanceType
    {
        /// <summary>
        /// Routine maintenance activities
        /// </summary>
        Routine = 0,

        /// <summary>
        /// Repair maintenance activities
        /// </summary>
        Repair = 1,

        /// <summary>
        /// Emergency maintenance activities
        /// </summary>
        Emergency = 2,

        /// <summary>
        /// Upgrade maintenance activities
        /// </summary>
        Upgrade = 3
    }
}