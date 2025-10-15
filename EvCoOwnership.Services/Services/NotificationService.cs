using EvCoOwnership.DTOs.Notifications;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Events;
using EvCoOwnership.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for notification operations
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Sends a notification to multiple users and fires notification event
        /// </summary>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="priority">Priority level of notification</param>
        /// <param name="userIds">List of user IDs to send notification to</param>
        /// <param name="additionalData">Optional additional data in JSON format</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<int>> SendNotificationToUsersAsync(string notificationType, ESeverityType priority, List<int> userIds, string? additionalData = null)
        {
            try
            {
                _logger.LogInformation("Sending notification to {UserCount} users. Type: {NotificationType}, Priority: {Priority}", 
                    userIds.Count, notificationType, priority);

                // Validate that users exist
                var existingUserIds = new List<int>();
                foreach (var userId in userIds)
                {
                    var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        existingUserIds.Add(userId);
                    }
                    else
                    {
                        _logger.LogWarning("User with ID {UserId} does not exist", userId);
                    }
                }

                if (!existingUserIds.Any())
                {
                    return new BaseResponse<int>
                    {
                        StatusCode = 400,
                        Message = "No valid users found",
                        Data = 0
                    };
                }

                // Create notification entity
                var notification = new NotificationEntity
                {
                    NotificationType = notificationType,
                    PriorityEnum = priority,
                    AdditionalData = additionalData,
                    CreatedAt = DateTime.UtcNow
                };

                // Save notification to get ID
                await _unitOfWork.NotificationRepository.CreateAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // Create user notifications for each user
                await _unitOfWork.UserNotificationRepository.CreateBulkAsync(notification.Id, existingUserIds);
                await _unitOfWork.SaveChangesAsync();

                // Fire notification events for each user
                foreach (var userId in existingUserIds)
                {
                    var eventData = new NotificationEventData
                    {
                        NotificationId = notification.Id,
                        UserId = userId,
                        NotificationType = notificationType,
                        AdditionalData = additionalData,
                        CreatedAt = notification.CreatedAt ?? DateTime.UtcNow
                    };

                    NotificationEventPublisher.PublishNotificationCreated(eventData);
                }

                _logger.LogInformation("Successfully sent notification {NotificationId} to {UserCount} users", 
                    notification.Id, existingUserIds.Count);

                return new BaseResponse<int>
                {
                    StatusCode = 200,
                    Message = $"Notification sent successfully to {existingUserIds.Count} users",
                    Data = notification.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to users");
                return new BaseResponse<int>
                {
                    StatusCode = 500,
                    Message = "An error occurred while sending notification",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Sends a notification to a single user and fires notification event
        /// </summary>
        /// <param name="userId">User ID to send notification to</param>
        /// <param name="notificationType">Type of notification</param>
        /// <param name="priority">Priority level of notification</param>
        /// <param name="additionalData">Optional additional data in JSON format</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<int>> SendNotificationToUserAsync(int userId, string notificationType, ESeverityType priority, string? additionalData = null)
        {
            return await SendNotificationToUsersAsync(notificationType, priority, new List<int> { userId }, additionalData);
        }

        /// <summary>
        /// Gets paginated notifications for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageIndex">Page index (starts from 1)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>Paginated list of notification response DTOs</returns>
        public async Task<BaseResponse<PaginatedList<NotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 10, bool includeRead = true)
        {
            try
            {
                _logger.LogInformation("Getting notifications for user {UserId}, Page: {Page}, Size: {Size}", 
                    userId, pageIndex, pageSize);

                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<PaginatedList<NotificationResponseDto>>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = new PaginatedList<NotificationResponseDto>(new List<NotificationResponseDto>(), 0, pageIndex, pageSize)
                    };
                }

                var skip = (pageIndex - 1) * pageSize;
                var (notifications, totalCount) = await _unitOfWork.UserNotificationRepository.GetByUserIdAsync(userId, skip, pageSize, includeRead);

                var notificationDtos = notifications.Select(un => new NotificationResponseDto
                {
                    Id = un.Id,
                    NotificationType = un.Notification?.NotificationType ?? string.Empty,
                    Priority = un.Notification?.PriorityEnum ?? ESeverityType.Low,
                    AdditionalData = un.Notification?.AdditionalData,
                    CreatedAt = un.Notification?.CreatedAt ?? DateTime.MinValue,
                    IsRead = un.ReadAt.HasValue,
                    ReadAt = un.ReadAt
                }).ToList();

                var paginatedResult = new PaginatedList<NotificationResponseDto>(notificationDtos, totalCount, pageIndex, pageSize);

                return new BaseResponse<PaginatedList<NotificationResponseDto>>
                {
                    StatusCode = 200,
                    Message = "Notifications retrieved successfully",
                    Data = paginatedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new BaseResponse<PaginatedList<NotificationResponseDto>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving notifications",
                    Data = new PaginatedList<NotificationResponseDto>(new List<NotificationResponseDto>(), 0, pageIndex, pageSize)
                };
            }
        }

        /// <summary>
        /// Marks a single notification as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userNotificationId">User notification ID to mark as read</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int userId, int userNotificationId)
        {
            try
            {
                _logger.LogInformation("Marking notification {UserNotificationId} as read for user {UserId}", 
                    userNotificationId, userId);

                var userNotification = await _unitOfWork.UserNotificationRepository.GetByIdAsync(userNotificationId);
                if (userNotification == null || userNotification.UserId != userId)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 404,
                        Message = "Notification not found",
                        Data = false
                    };
                }

                if (userNotification.ReadAt.HasValue)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 200,
                        Message = "Notification already marked as read",
                        Data = true
                    };
                }

                var success = await _unitOfWork.UserNotificationRepository.MarkAsReadAsync(userNotificationId);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<bool>
                {
                    StatusCode = success ? 200 : 404,
                    Message = success ? "Notification marked as read successfully" : "Notification not found",
                    Data = success
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return new BaseResponse<bool>
                {
                    StatusCode = 500,
                    Message = "An error occurred while marking notification as read",
                    Data = false
                };
            }
        }

        /// <summary>
        /// Marks multiple notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userNotificationIds">List of user notification IDs to mark as read</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        public async Task<BaseResponse<int>> MarkMultipleNotificationsAsReadAsync(int userId, List<int> userNotificationIds)
        {
            try
            {
                _logger.LogInformation("Marking {Count} notifications as read for user {UserId}", 
                    userNotificationIds.Count, userId);

                // Verify all notifications belong to the user
                var userNotifications = await _unitOfWork.UserNotificationRepository.GetByNotificationIdsForUserAsync(userId, userNotificationIds);
                var validIds = userNotifications.Select(un => un.Id).ToList();

                if (!validIds.Any())
                {
                    return new BaseResponse<int>
                    {
                        StatusCode = 404,
                        Message = "No valid notifications found for this user",
                        Data = 0
                    };
                }

                var count = await _unitOfWork.UserNotificationRepository.MarkMultipleAsReadAsync(validIds);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<int>
                {
                    StatusCode = 200,
                    Message = $"{count} notifications marked as read successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking multiple notifications as read for user {UserId}", userId);
                return new BaseResponse<int>
                {
                    StatusCode = 500,
                    Message = "An error occurred while marking notifications as read",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Marks all unread notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        public async Task<BaseResponse<int>> MarkAllNotificationsAsReadAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Marking all unread notifications as read for user {UserId}", userId);

                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<int>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = 0
                    };
                }

                var count = await _unitOfWork.UserNotificationRepository.MarkAllAsReadForUserAsync(userId);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<int>
                {
                    StatusCode = 200,
                    Message = $"{count} notifications marked as read successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return new BaseResponse<int>
                {
                    StatusCode = 500,
                    Message = "An error occurred while marking all notifications as read",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with unread notifications count</returns>
        public async Task<BaseResponse<int>> GetUnreadCountAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting unread count for user {UserId}", userId);

                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<int>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = 0
                    };
                }

                var count = await _unitOfWork.UserNotificationRepository.GetUnreadCountAsync(userId);

                return new BaseResponse<int>
                {
                    StatusCode = 200,
                    Message = "Unread count retrieved successfully",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return new BaseResponse<int>
                {
                    StatusCode = 500,
                    Message = "An error occurred while getting unread count",
                    Data = 0
                };
            }
        }
    }
}