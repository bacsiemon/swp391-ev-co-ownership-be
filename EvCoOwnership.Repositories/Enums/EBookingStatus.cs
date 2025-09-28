namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the status types for bookings in the system
    /// </summary>
    public enum EBookingStatus
    {
        /// <summary>
        /// Booking is pending approval
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Booking has been confirmed
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Booking is currently active (in progress)
        /// </summary>
        Active = 2,

        /// <summary>
        /// Booking has been completed
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Booking has been cancelled
        /// </summary>
        Cancelled = 4
    }
}