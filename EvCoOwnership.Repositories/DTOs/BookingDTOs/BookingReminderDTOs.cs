using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.BookingDTOs
{
    /// <summary>
    /// Request to configure booking reminder settings
    /// </summary>
    public class ConfigureReminderRequest
    {
        /// <summary>
        /// Hours before booking to send reminder (e.g., 24 = 1 day before)
        /// </summary>
        public int HoursBeforeBooking { get; set; } = 24;

        /// <summary>
        /// Whether to enable reminders for this user
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// User's reminder preferences
    /// </summary>
    public class ReminderPreferencesResponse
    {
        public int UserId { get; set; }
        public int HoursBeforeBooking { get; set; }
        public bool Enabled { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Details about an upcoming booking that needs reminder
    /// </summary>
    public class UpcomingBookingReminderResponse
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public double HoursUntilStart { get; set; }
        public bool ReminderSent { get; set; }
        public DateTime? ReminderSentAt { get; set; }
    }

    /// <summary>
    /// List of upcoming bookings with reminders
    /// </summary>
    public class UpcomingBookingsWithRemindersResponse
    {
        public int UserId { get; set; }
        public int TotalUpcomingBookings { get; set; }
        public List<UpcomingBookingReminderResponse> UpcomingBookings { get; set; } = new();
    }

    /// <summary>
    /// Reminder notification data for JSON storage
    /// </summary>
    public class BookingReminderNotificationData
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public double HoursUntilStart { get; set; }
    }

    /// <summary>
    /// Statistics about booking reminders
    /// </summary>
    public class BookingReminderStatisticsResponse
    {
        public int TotalRemindersScheduled { get; set; }
        public int RemindersSentToday { get; set; }
        public int RemindersScheduledNext24Hours { get; set; }
        public int RemindersScheduledNext7Days { get; set; }
        public int UsersWithRemindersEnabled { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
        public DateTime StatisticsGeneratedAt { get; set; }
    }
}
