namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the usage types for funds in the system
    /// </summary>
    public enum EUsageType
    {
        /// <summary>
        /// Fund used for maintenance purposes
        /// </summary>
        Maintenance = 0,

        /// <summary>
        /// Fund used for insurance purposes
        /// </summary>
        Insurance = 1,

        /// <summary>
        /// Fund used for fuel expenses
        /// </summary>
        Fuel = 2,

        /// <summary>
        /// Fund used for parking expenses
        /// </summary>
        Parking = 3,

        /// <summary>
        /// Fund used for other miscellaneous purposes
        /// </summary>
        Other = 4
    }
}