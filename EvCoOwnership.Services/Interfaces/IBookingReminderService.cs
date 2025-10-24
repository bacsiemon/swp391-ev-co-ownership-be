using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for booking reminder operations
    /// </summary>
    public interface IBookingReminderService
    {
        /// <summary>
        /// Configure reminder preferences for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Reminder configuration request</param>
        /// <returns>Updated reminder preferences</returns>
        Task<BaseResponse<ReminderPreferencesResponse>> ConfigureReminderPreferencesAsync(int userId, ConfigureReminderRequest request);

        /// <summary>
        /// Get reminder preferences for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User's reminder preferences</returns>
        Task<BaseResponse<ReminderPreferencesResponse>> GetReminderPreferencesAsync(int userId);

        /// <summary>
        /// Get upcoming bookings that will trigger reminders for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="daysAhead">Number of days to look ahead (default: 7)</param>
        /// <returns>List of upcoming bookings with reminder information</returns>
        Task<BaseResponse<UpcomingBookingsWithRemindersResponse>> GetUpcomingBookingsWithRemindersAsync(int userId, int daysAhead = 7);

        /// <summary>
        /// Process and send reminders for bookings that are due
        /// Called by background service
        /// </summary>
        /// <returns>Number of reminders sent</returns>
        Task<int> ProcessPendingRemindersAsync();

        /// <summary>
        /// Get statistics about booking reminders (Admin only)
        /// </summary>
        /// <returns>Reminder statistics</returns>
        Task<BaseResponse<BookingReminderStatisticsResponse>> GetReminderStatisticsAsync();

        /// <summary>
        /// Manually send a reminder for a specific booking (for testing)
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Success indicator</returns>
        Task<BaseResponse<bool>> SendManualReminderAsync(int bookingId, int userId);
    }
}
