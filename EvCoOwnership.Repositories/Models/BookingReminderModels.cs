using System;

namespace EvCoOwnership.Repositories.Models
{
    /// <summary>
    /// User preferences for booking reminders
    /// </summary>
    public class UserReminderPreference
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Hours before booking start time to send reminder
        /// </summary>
        public int HoursBeforeBooking { get; set; } = 24;

        /// <summary>
        /// Whether reminders are enabled for this user
        /// </summary>
        public bool Enabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; } = null!;
    }

    /// <summary>
    /// Log of sent booking reminders
    /// </summary>
    public class BookingReminderLog
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime BookingStartTime { get; set; }
        public double HoursBeforeBooking { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public virtual Booking Booking { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
