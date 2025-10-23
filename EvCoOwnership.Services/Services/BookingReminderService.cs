using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for booking reminder operations
    /// </summary>
    public class BookingReminderService : IBookingReminderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingReminderService> _logger;

        public BookingReminderService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<BookingReminderService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Configure reminder preferences for a user
        /// </summary>
        public async Task<BaseResponse<ReminderPreferencesResponse>> ConfigureReminderPreferencesAsync(
            int userId, ConfigureReminderRequest request)
        {
            _logger.LogInformation("Configuring reminder preferences for user {UserId}", userId);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<ReminderPreferencesResponse>
                {
                    StatusCode = 404,
                    Message = "User not found"
                };
            }

            // Validate hours before booking
            if (request.HoursBeforeBooking < 1 || request.HoursBeforeBooking > 168) // Max 7 days
            {
                return new BaseResponse<ReminderPreferencesResponse>
                {
                    StatusCode = 400,
                    Message = "Hours before booking must be between 1 and 168 (7 days)"
                };
            }

            // Check if preference already exists (using in-memory operations since we don't have repository)
            var existingPreferences = await _unitOfWork.DbContext.Set<UserReminderPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingPreferences != null)
            {
                // Update existing
                existingPreferences.HoursBeforeBooking = request.HoursBeforeBooking;
                existingPreferences.Enabled = request.Enabled;
                existingPreferences.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var newPreference = new UserReminderPreference
                {
                    UserId = userId,
                    HoursBeforeBooking = request.HoursBeforeBooking,
                    Enabled = request.Enabled,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.DbContext.Set<UserReminderPreference>().AddAsync(newPreference);
                existingPreferences = newPreference;
            }

            await _unitOfWork.SaveChangesAsync();

            var response = new ReminderPreferencesResponse
            {
                UserId = existingPreferences.UserId,
                HoursBeforeBooking = existingPreferences.HoursBeforeBooking,
                Enabled = existingPreferences.Enabled,
                UpdatedAt = existingPreferences.UpdatedAt ?? existingPreferences.CreatedAt
            };

            _logger.LogInformation("Reminder preferences configured for user {UserId}: {Hours}h before, Enabled: {Enabled}",
                userId, request.HoursBeforeBooking, request.Enabled);

            return new BaseResponse<ReminderPreferencesResponse>
            {
                StatusCode = 200,
                Message = "Reminder preferences updated successfully",
                Data = response
            };
        }

        /// <summary>
        /// Get reminder preferences for a user
        /// </summary>
        public async Task<BaseResponse<ReminderPreferencesResponse>> GetReminderPreferencesAsync(int userId)
        {
            _logger.LogInformation("Getting reminder preferences for user {UserId}", userId);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<ReminderPreferencesResponse>
                {
                    StatusCode = 404,
                    Message = "User not found"
                };
            }

            var preference = await _unitOfWork.DbContext.Set<UserReminderPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preference == null)
            {
                // Return default preferences
                return new BaseResponse<ReminderPreferencesResponse>
                {
                    StatusCode = 200,
                    Message = "Using default reminder preferences (not yet configured)",
                    Data = new ReminderPreferencesResponse
                    {
                        UserId = userId,
                        HoursBeforeBooking = 24,
                        Enabled = true,
                        UpdatedAt = null
                    }
                };
            }

            var response = new ReminderPreferencesResponse
            {
                UserId = preference.UserId,
                HoursBeforeBooking = preference.HoursBeforeBooking,
                Enabled = preference.Enabled,
                UpdatedAt = preference.UpdatedAt ?? preference.CreatedAt
            };

            return new BaseResponse<ReminderPreferencesResponse>
            {
                StatusCode = 200,
                Message = "Reminder preferences retrieved successfully",
                Data = response
            };
        }

        /// <summary>
        /// Get upcoming bookings that will trigger reminders for a user
        /// </summary>
        public async Task<BaseResponse<UpcomingBookingsWithRemindersResponse>> GetUpcomingBookingsWithRemindersAsync(
            int userId, int daysAhead = 7)
        {
            _logger.LogInformation("Getting upcoming bookings with reminders for user {UserId}, {Days} days ahead",
                userId, daysAhead);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<UpcomingBookingsWithRemindersResponse>
                {
                    StatusCode = 404,
                    Message = "User not found"
                };
            }

            // Get user's reminder preferences
            var preference = await _unitOfWork.DbContext.Set<UserReminderPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var hoursBeforeBooking = preference?.HoursBeforeBooking ?? 24;
            var enabled = preference?.Enabled ?? true;

            if (!enabled)
            {
                return new BaseResponse<UpcomingBookingsWithRemindersResponse>
                {
                    StatusCode = 200,
                    Message = "Reminders are disabled for this user",
                    Data = new UpcomingBookingsWithRemindersResponse
                    {
                        UserId = userId,
                        TotalUpcomingBookings = 0,
                        UpcomingBookings = new List<UpcomingBookingReminderResponse>()
                    }
                };
            }

            // Get upcoming bookings
            var now = DateTime.UtcNow;
            var endDate = now.AddDays(daysAhead);

            var upcomingBookings = await _unitOfWork.DbContext.Set<Booking>()
                .Include(b => b.Vehicle)
                .Include(b => b.CoOwner)
                .Where(b =>
                    b.CoOwner != null && b.CoOwner.UserId == userId &&
                    b.StartTime > now &&
                    b.StartTime <= endDate &&
                    (b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Active))
                .ToListAsync();

            // Get reminder logs to check which reminders have been sent
            var bookingIds = upcomingBookings.Select(b => b.Id).ToList();
            var reminderLogs = await _unitOfWork.DbContext.Set<BookingReminderLog>()
                .Where(log => bookingIds.Contains(log.BookingId) && log.UserId == userId)
                .ToListAsync();

            var reminderResponses = upcomingBookings.Select(booking =>
            {
                var hoursUntilStart = (booking.StartTime - now).TotalHours;
                var reminderLog = reminderLogs.FirstOrDefault(log => log.BookingId == booking.Id);

                return new UpcomingBookingReminderResponse
                {
                    BookingId = booking.Id,
                    VehicleId = booking.VehicleId ?? 0,
                    VehicleName = $"{booking.Vehicle?.Brand} {booking.Vehicle?.Model}",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Purpose = booking.Purpose ?? "",
                    HoursUntilStart = hoursUntilStart,
                    ReminderSent = reminderLog != null,
                    ReminderSentAt = reminderLog?.SentAt
                };
            }).OrderBy(r => r.StartTime).ToList();

            var response = new UpcomingBookingsWithRemindersResponse
            {
                UserId = userId,
                TotalUpcomingBookings = reminderResponses.Count,
                UpcomingBookings = reminderResponses
            };

            return new BaseResponse<UpcomingBookingsWithRemindersResponse>
            {
                StatusCode = 200,
                Message = $"Found {reminderResponses.Count} upcoming bookings",
                Data = response
            };
        }

        /// <summary>
        /// Process and send reminders for bookings that are due
        /// Called by background service
        /// </summary>
        public async Task<int> ProcessPendingRemindersAsync()
        {
            _logger.LogInformation("Processing pending booking reminders");

            try
            {
                var now = DateTime.UtcNow;
                var remindersSent = 0;

                // Get all users with enabled reminders
                var usersWithReminders = await _unitOfWork.DbContext.Set<UserReminderPreference>()
                    .Where(p => p.Enabled)
                    .ToListAsync();

                foreach (var userPref in usersWithReminders)
                {
                    // Calculate time window for this user's bookings
                    var reminderWindowStart = now;
                    var reminderWindowEnd = now.AddHours(userPref.HoursBeforeBooking + 1); // +1 hour buffer

                    // Get bookings that need reminders for this user
                    var upcomingBookings = await _unitOfWork.DbContext.Set<Booking>()
                        .Include(b => b.Vehicle)
                        .Include(b => b.CoOwner)
                            .ThenInclude(co => co.User)
                        .Where(b =>
                            b.CoOwner != null &&
                            b.CoOwner.UserId == userPref.UserId &&
                            b.StartTime > reminderWindowStart &&
                            b.StartTime <= reminderWindowEnd &&
                            (b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Active))
                        .ToListAsync();

                    foreach (var booking in upcomingBookings)
                    {
                        var hoursUntilStart = (booking.StartTime - now).TotalHours;

                        // Check if we should send reminder based on user preference
                        if (hoursUntilStart <= userPref.HoursBeforeBooking)
                        {
                            // Check if reminder already sent
                            var existingLog = await _unitOfWork.DbContext.Set<BookingReminderLog>()
                                .FirstOrDefaultAsync(log =>
                                    log.BookingId == booking.Id &&
                                    log.UserId == userPref.UserId);

                            if (existingLog == null)
                            {
                                // Send reminder
                                var sent = await SendReminderNotificationAsync(booking, userPref.UserId, hoursUntilStart);
                                if (sent)
                                {
                                    remindersSent++;
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Processed pending reminders: {Count} reminders sent", remindersSent);
                return remindersSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending reminders");
                return 0;
            }
        }

        /// <summary>
        /// Get statistics about booking reminders
        /// </summary>
        public async Task<BaseResponse<BookingReminderStatisticsResponse>> GetReminderStatisticsAsync()
        {
            _logger.LogInformation("Getting booking reminder statistics");

            try
            {
                var now = DateTime.UtcNow;

                // Total reminders scheduled (upcoming bookings with enabled reminders)
                var usersWithEnabledReminders = await _unitOfWork.DbContext.Set<UserReminderPreference>()
                    .Where(p => p.Enabled)
                    .Select(p => p.UserId)
                    .ToListAsync();

                var totalScheduled = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CoOwner)
                    .Where(b =>
                        b.CoOwner != null &&
                        usersWithEnabledReminders.Contains(b.CoOwner.UserId) &&
                        b.StartTime > now &&
                        (b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Active))
                    .CountAsync();

                // Reminders sent today
                var todayStart = now.Date;
                var sentToday = await _unitOfWork.DbContext.Set<BookingReminderLog>()
                    .CountAsync(log => log.SentAt >= todayStart && log.Success);

                // Reminders scheduled next 24 hours
                var next24Hours = now.AddHours(24);
                var scheduledNext24Hours = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CoOwner)
                    .Where(b =>
                        b.CoOwner != null &&
                        usersWithEnabledReminders.Contains(b.CoOwner.UserId) &&
                        b.StartTime > now &&
                        b.StartTime <= next24Hours &&
                        (b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Active))
                    .CountAsync();

                // Reminders scheduled next 7 days
                var next7Days = now.AddDays(7);
                var scheduledNext7Days = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CoOwner)
                    .Where(b =>
                        b.CoOwner != null &&
                        usersWithEnabledReminders.Contains(b.CoOwner.UserId) &&
                        b.StartTime > now &&
                        b.StartTime <= next7Days &&
                        (b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Active))
                    .CountAsync();

                // Users with reminders enabled
                var usersCount = usersWithEnabledReminders.Count;

                // Last reminder sent
                var lastReminder = await _unitOfWork.DbContext.Set<BookingReminderLog>()
                    .Where(log => log.Success)
                    .OrderByDescending(log => log.SentAt)
                    .FirstOrDefaultAsync();

                var stats = new BookingReminderStatisticsResponse
                {
                    TotalRemindersScheduled = totalScheduled,
                    RemindersSentToday = sentToday,
                    RemindersScheduledNext24Hours = scheduledNext24Hours,
                    RemindersScheduledNext7Days = scheduledNext7Days,
                    UsersWithRemindersEnabled = usersCount,
                    LastReminderSentAt = lastReminder?.SentAt,
                    StatisticsGeneratedAt = now
                };

                return new BaseResponse<BookingReminderStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "Reminder statistics retrieved successfully",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminder statistics");
                return new BaseResponse<BookingReminderStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "Error retrieving statistics"
                };
            }
        }

        /// <summary>
        /// Manually send a reminder for a specific booking
        /// </summary>
        public async Task<BaseResponse<bool>> SendManualReminderAsync(int bookingId, int userId)
        {
            _logger.LogInformation("Manually sending reminder for booking {BookingId} to user {UserId}",
                bookingId, userId);

            // Get booking
            var bookings = await _unitOfWork.DbContext.Set<Booking>()
                .Include(b => b.Vehicle)
                .Include(b => b.CoOwner)
                    .ThenInclude(co => co.User)
                .Where(b => b.Id == bookingId)
                .ToListAsync();

            var booking = bookings.FirstOrDefault();
            if (booking == null)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 404,
                    Message = "Booking not found"
                };
            }

            // Verify user owns this booking
            if (booking.CoOwner?.UserId != userId)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 403,
                    Message = "You are not authorized to access this booking"
                };
            }

            var now = DateTime.UtcNow;
            var hoursUntilStart = (booking.StartTime - now).TotalHours;

            if (hoursUntilStart < 0)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 400,
                    Message = "Cannot send reminder for past bookings"
                };
            }

            var sent = await SendReminderNotificationAsync(booking, userId, hoursUntilStart, isManual: true);

            return new BaseResponse<bool>
            {
                StatusCode = sent ? 200 : 500,
                Message = sent ? "Reminder sent successfully" : "Failed to send reminder",
                Data = sent
            };
        }

        /// <summary>
        /// Internal method to send reminder notification
        /// </summary>
        private async Task<bool> SendReminderNotificationAsync(
            Booking booking, int userId, double hoursUntilStart, bool isManual = false)
        {
            try
            {
                // Prepare notification data
                var reminderData = new BookingReminderNotificationData
                {
                    BookingId = booking.Id,
                    VehicleId = booking.VehicleId ?? 0,
                    VehicleName = $"{booking.Vehicle?.Brand} {booking.Vehicle?.Model}",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Purpose = booking.Purpose ?? "",
                    HoursUntilStart = hoursUntilStart
                };

                var additionalDataJson = JsonSerializer.Serialize(reminderData);

                // Send notification
                var notificationRequest = new EvCoOwnership.DTOs.Notifications.SendNotificationRequestDto
                {
                    UserId = userId,
                    NotificationType = isManual ? "BookingReminderManual" : "BookingReminderAutomatic",
                    AdditionalData = additionalDataJson
                };

                var result = await _notificationService.SendNotificationToUserAsync(notificationRequest);

                // Log reminder
                var log = new BookingReminderLog
                {
                    BookingId = booking.Id,
                    UserId = userId,
                    SentAt = DateTime.UtcNow,
                    BookingStartTime = booking.StartTime,
                    HoursBeforeBooking = hoursUntilStart,
                    Success = result.StatusCode == 200,
                    ErrorMessage = result.StatusCode != 200 ? result.Message : null
                };

                await _unitOfWork.DbContext.Set<BookingReminderLog>().AddAsync(log);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Reminder sent for booking {BookingId} to user {UserId}. Hours until start: {Hours:F1}",
                    booking.Id, userId, hoursUntilStart);

                return result.StatusCode == 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for booking {BookingId}", booking.Id);

                // Log failed attempt
                try
                {
                    var log = new BookingReminderLog
                    {
                        BookingId = booking.Id,
                        UserId = userId,
                        SentAt = DateTime.UtcNow,
                        BookingStartTime = booking.StartTime,
                        HoursBeforeBooking = hoursUntilStart,
                        Success = false,
                        ErrorMessage = ex.Message
                    };

                    await _unitOfWork.DbContext.Set<BookingReminderLog>().AddAsync(log);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch { /* Ignore logging errors */ }

                return false;
            }
        }
    }
}
