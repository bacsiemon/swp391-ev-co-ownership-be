namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the type/purpose of payment transaction
    /// </summary>
    public enum EPaymentType
    {
        /// <summary>
        /// Payment for vehicle booking
        /// </summary>
        Booking = 0,

        /// <summary>
        /// Payment for vehicle maintenance
        /// </summary>
        Maintenance = 1,

        /// <summary>
        /// Payment for fund addition (deposit to group fund)
        /// </summary>
        FundAddition = 2,

        /// <summary>
        /// Payment for fuel costs
        /// </summary>
        Fuel = 3,

        /// <summary>
        /// Payment for insurance
        /// </summary>
        Insurance = 4,

        /// <summary>
        /// Payment for parking fees
        /// </summary>
        Parking = 5,

        /// <summary>
        /// Payment for toll fees
        /// </summary>
        Toll = 6,

        /// <summary>
        /// Payment for vehicle upgrade/modification
        /// </summary>
        Upgrade = 7,

        /// <summary>
        /// Payment for dispute resolution
        /// </summary>
        Dispute = 8,

        /// <summary>
        /// Payment for contract fees
        /// </summary>
        Contract = 9,

        /// <summary>
        /// Other miscellaneous payments
        /// </summary>
        Other = 99
    }
}
