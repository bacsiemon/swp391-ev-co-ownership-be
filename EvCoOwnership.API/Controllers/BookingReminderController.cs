using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing booking reminders
    /// </summary>
    [ApiController]
    [Route("api/booking-reminder")]
    [Authorize]
    public class BookingReminderController : ControllerBase
    {
        private readonly IBookingReminderService _bookingReminderService;
        private readonly ILogger<BookingReminderController> _logger;

        public BookingReminderController(
            IBookingReminderService bookingReminderService,
            ILogger<BookingReminderController> logger)
        {
            _bookingReminderService = bookingReminderService;
            _logger = logger;
        }

        /// <summary>
        /// Configure booking reminder preferences for current user
        /// </summary>
        /// <remarks>
        /// **CONFIGURE BOOKING REMINDER PREFERENCES**
        /// 
        /// **Role Required:** CoOwner, Staff, Admin
        /// 
        /// **Purpose:**
        /// Allows users to customize when they receive booking reminders and enable/disable the feature.
        /// 
        /// **How It Works:**
        /// - Users can set how many hours before a booking they want to be reminded (1-168 hours / 1-7 days)
        /// - Users can enable or disable reminders entirely
        /// - Reminders are sent automatically by a background service
        /// - Default: 24 hours before booking, enabled
        /// 
        /// **Use Cases:**
        /// 1. **Enable Reminders:**
        ///    - "Nhắc tôi 24 giờ trước khi đặt xe"
        ///    - Set HoursBeforeBooking = 24, Enabled = true
        /// 
        /// 2. **Custom Timing:**
        ///    - "Tôi muốn được nhắc 2 giờ trước khi đặt xe"
        ///    - Set HoursBeforeBooking = 2, Enabled = true
        /// 
        /// 3. **Disable Reminders:**
        ///    - "Tắt thông báo nhắc lịch đặt xe"
        ///    - Set Enabled = false
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "hoursBeforeBooking": 24,
        ///   "enabled": true
        /// }
        /// ```
        /// 
        /// **Validation:**
        /// - Hours must be between 1 and 168 (7 days)
        /// - User must exist
        /// </remarks>
        /// <response code="200">Reminder preferences updated successfully</response>
        /// <response code="400">Invalid hours before booking value</response>
        /// <response code="401">Unauthorized - invalid token</response>
        /// <response code="404">User not found</response>
        [HttpPost("configure")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> ConfigureReminderPreferences([FromBody] ConfigureReminderRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingReminderService.ConfigureReminderPreferencesAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get current user's reminder preferences
        /// </summary>
        /// <remarks>
        /// **GET REMINDER PREFERENCES**
        /// 
        /// **Role Required:** CoOwner, Staff, Admin
        /// 
        /// **Purpose:**
        /// Retrieve the current user's booking reminder settings.
        /// 
        /// **Returns:**
        /// - Hours before booking to send reminder
        /// - Whether reminders are enabled
        /// - Last update timestamp
        /// - If not configured, returns default settings (24 hours, enabled)
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Reminder preferences retrieved successfully",
        ///   "data": {
        ///     "userId": 5,
        ///     "hoursBeforeBooking": 24,
        ///     "enabled": true,
        ///     "updatedAt": "2025-10-23T10:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Preferences retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">User not found</response>
        [HttpGet("preferences")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetReminderPreferences()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingReminderService.GetReminderPreferencesAsync(userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get upcoming bookings with reminder information
        /// </summary>
        /// <param name="daysAhead">Number of days to look ahead (default: 7, max: 30)</param>
        /// <remarks>
        /// **GET UPCOMING BOOKINGS WITH REMINDERS**
        /// 
        /// **Role Required:** CoOwner, Staff, Admin
        /// 
        /// **Purpose:**
        /// View all upcoming bookings and see which ones will receive reminders.
        /// Helps users track when they'll be reminded about their bookings.
        /// 
        /// **Information Provided:**
        /// - All upcoming bookings in the specified period
        /// - Hours until each booking starts
        /// - Whether reminder has already been sent
        /// - When reminder was sent (if applicable)
        /// - Vehicle details for each booking
        /// 
        /// **Use Cases:**
        /// 
        /// 1. **Check Next Week's Bookings:**
        ///    - GET /api/booking-reminder/upcoming?daysAhead=7
        ///    - See all bookings in the next 7 days
        /// 
        /// 2. **Check This Month:**
        ///    - GET /api/booking-reminder/upcoming?daysAhead=30
        ///    - See all bookings in the next 30 days
        /// 
        /// 3. **Today's Bookings:**
        ///    - GET /api/booking-reminder/upcoming?daysAhead=1
        ///    - See bookings scheduled for today/tomorrow
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Found 3 upcoming bookings",
        ///   "data": {
        ///     "userId": 5,
        ///     "totalUpcomingBookings": 3,
        ///     "upcomingBookings": [
        ///       {
        ///         "bookingId": 101,
        ///         "vehicleId": 5,
        ///         "vehicleName": "VinFast VF8",
        ///         "licensePlate": "30A-12345",
        ///         "startTime": "2025-10-24T08:00:00Z",
        ///         "endTime": "2025-10-24T18:00:00Z",
        ///         "purpose": "Đi công tác",
        ///         "hoursUntilStart": 21.5,
        ///         "reminderSent": true,
        ///         "reminderSentAt": "2025-10-23T11:00:00Z"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Upcoming bookings retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">User not found</response>
        [HttpGet("upcoming")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetUpcomingBookingsWithReminders([FromQuery] int daysAhead = 7)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Validate daysAhead
            if (daysAhead < 1 || daysAhead > 30)
            {
                return BadRequest(new { Message = "Days ahead must be between 1 and 30" });
            }

            var response = await _bookingReminderService.GetUpcomingBookingsWithRemindersAsync(userId, daysAhead);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Send a manual reminder for a specific booking
        /// </summary>
        /// <param name="bookingId">ID of the booking to send reminder for</param>
        /// <remarks>
        /// **SEND MANUAL REMINDER**
        /// 
        /// **Role Required:** CoOwner, Staff, Admin
        /// 
        /// **Purpose:**
        /// Manually trigger a reminder notification for a specific booking.
        /// Useful for testing or if user wants immediate reminder.
        /// 
        /// **How It Works:**
        /// - Sends a notification immediately for the specified booking
        /// - Logs the reminder as "manual" (different from automatic)
        /// - Checks that booking belongs to the requesting user
        /// - Cannot send reminders for past bookings
        /// 
        /// **Use Cases:**
        /// 
        /// 1. **Test Reminder System:**
        ///    - POST /api/booking-reminder/send/101
        ///    - Test if reminders are working correctly
        /// 
        /// 2. **Immediate Reminder:**
        ///    - "Tôi muốn nhận nhắc nhở ngay bây giờ"
        ///    - Send reminder even if not scheduled yet
        /// 
        /// 3. **Re-send Reminder:**
        ///    - Send another reminder if user missed the first one
        /// 
        /// **Example Request:**
        /// POST /api/booking-reminder/send/101
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Reminder sent successfully",
        ///   "data": true
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Reminder sent successfully</response>
        /// <response code="400">Cannot send reminder for past bookings</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Not authorized to access this booking</response>
        /// <response code="404">Booking not found</response>
        /// <response code="500">Failed to send reminder</response>
        [HttpPost("send/{bookingId}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> SendManualReminder(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingReminderService.SendManualReminderAsync(bookingId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get booking reminder statistics (Admin only)
        /// </summary>
        /// <remarks>
        /// **GET REMINDER STATISTICS - Admin Only**
        /// 
        /// **Role Required:** Admin
        /// 
        /// **Purpose:**
        /// View system-wide statistics about booking reminders.
        /// Helps administrators monitor the reminder system.
        /// 
        /// **Statistics Included:**
        /// 1. **Total Reminders Scheduled:** Upcoming bookings with enabled reminders
        /// 2. **Reminders Sent Today:** Count of reminders sent today
        /// 3. **Scheduled Next 24 Hours:** Bookings starting in next 24 hours
        /// 4. **Scheduled Next 7 Days:** Bookings starting in next week
        /// 5. **Users with Reminders Enabled:** Total users using the feature
        /// 6. **Last Reminder Sent At:** Timestamp of most recent reminder
        /// 7. **Statistics Generated At:** When these stats were calculated
        /// 
        /// **Use Cases:**
        /// 
        /// 1. **System Monitoring:**
        ///    - Check if background service is working
        ///    - Monitor reminder delivery
        /// 
        /// 2. **Usage Analytics:**
        ///    - How many users are using reminders?
        ///    - How many reminders are being sent?
        /// 
        /// 3. **Troubleshooting:**
        ///    - When was the last reminder sent?
        ///    - Are reminders being processed?
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Reminder statistics retrieved successfully",
        ///   "data": {
        ///     "totalRemindersScheduled": 45,
        ///     "remindersSentToday": 12,
        ///     "remindersScheduledNext24Hours": 8,
        ///     "remindersScheduledNext7Days": 32,
        ///     "usersWithRemindersEnabled": 15,
        ///     "lastReminderSentAt": "2025-10-23T11:45:00Z",
        ///     "statisticsGeneratedAt": "2025-10-23T12:00:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Error retrieving statistics</response>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin)]
        public async Task<IActionResult> GetReminderStatistics()
        {
            var response = await _bookingReminderService.GetReminderStatisticsAsync();

            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
