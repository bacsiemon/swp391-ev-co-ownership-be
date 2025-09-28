namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for vehicles in the system
    /// </summary>
    public enum EVehicleStatus
    {
        /// <summary>
        /// Vehicle is available for booking
        /// </summary>
        Available = 0,

        /// <summary>
        /// Vehicle is currently in use
        /// </summary>
        InUse = 1,

        /// <summary>
        /// Vehicle is under maintenance
        /// </summary>
        Maintenance = 2,

        /// <summary>
        /// Vehicle is unavailable for any reason
        /// </summary>
        Unavailable = 3
    }
}