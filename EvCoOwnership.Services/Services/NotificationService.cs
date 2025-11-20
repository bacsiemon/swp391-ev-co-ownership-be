using EvCoOwnership.Repositories.DTOs.Notifications;
using EvCoOwnership.Helpers.BaseClasses;
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
        /// <param name="request">Create notification request containing notification details</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<int>> SendNotificationToUsersAsync(CreateNotificationRequest request)
        {
            _logger.LogInformation("Sending notification to {UserCount} users. Type: {NotificationType}", 
                request.UserIds.Count, request.NotificationType);

            // Validate that users exist
            var existingUserIds = new List<int>();
            foreach (var userId in request.UserIds)
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
                    Message = "NO_VALID_USERS_FOUND",
                    Data = 0
                };
            }

            // Create notification entity
            var notification = new NotificationEntity
            {
                NotificationType = request.NotificationType,
                AdditionalDataJson = request.AdditionalData,
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
                    NotificationType = request.NotificationType,
                    AdditionalData = request.AdditionalData,
                    CreatedAt = notification.CreatedAt
                };

                NotificationEventPublisher.PublishNotificationCreated(eventData);
            }

            _logger.LogInformation("Successfully sent notification {NotificationId} to {UserCount} users", 
                notification.Id, existingUserIds.Count);

            return new BaseResponse<int>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = notification.Id
            };
        }

        /// <summary>
        /// Sends a notification to a single user and fires notification event
        /// </summary>
        /// <param name="request">Send notification request containing user and notification details</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<int>> SendNotificationToUserAsync(SendNotificationRequestDto request)
        {
            var createRequest = new CreateNotificationRequest
            {
                NotificationType = request.NotificationType,
                UserIds = new List<int> { request.UserId },
                AdditionalData = request.AdditionalData
            };

            return await SendNotificationToUsersAsync(createRequest);
        }

        /// <summary>
        /// Gets paginated notifications for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageIndex">Page index (starts from 1)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeRead">Include read notifications</param>
        /// <returns>Paginated list of user notification response DTOs</returns>
        public async Task<BaseResponse<PaginatedList<UserNotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 10, bool includeRead = true)
        {
            _logger.LogInformation("Getting notifications for user {UserId}, Page: {Page}, Size: {Size}", 
                userId, pageIndex, pageSize);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<PaginatedList<UserNotificationResponseDto>>
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                    Data = new PaginatedList<UserNotificationResponseDto>()
                };
            }

            var skip = (pageIndex - 1) * pageSize;
            var (notifications, totalCount) = await _unitOfWork.UserNotificationRepository.GetByUserIdAsync(userId, skip, pageSize, includeRead);

            var notificationDtos = notifications.Select(un => new UserNotificationResponseDto
            {
                Id = un.Id,
                NotificationId = un.NotificationId,
                UserId = un.UserId,
                ReadAt = un.ReadAt,
                NotificationType = un.Notification?.NotificationType ?? "System",
                AdditionalData = un.Notification?.AdditionalDataJson,
                CreatedAt = un.Notification?.CreatedAt
            }).ToList();

            // Create a paginated result manually since we already have the data
            var paginatedResult = new PaginatedList<UserNotificationResponseDto>
            {
                Items = notificationDtos,
                Page = pageIndex,
                Size = pageSize,
                Total = totalCount
            };

            return new BaseResponse<PaginatedList<UserNotificationResponseDto>>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = paginatedResult
            };
        }

        /// <summary>
        /// Marks a single notification as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Mark notification as read request containing notification ID</param>
        /// <returns>Base response indicating success or failure</returns>
        public async Task<BaseResponse<bool>> MarkNotificationAsReadAsync(int userId, MarkNotificationAsReadRequest request)
        {
            _logger.LogInformation("Marking notification {UserNotificationId} as read for user {UserId}", 
                request.UserNotificationId, userId);

            var userNotification = await _unitOfWork.UserNotificationRepository.GetByIdAsync(request.UserNotificationId);
            if (userNotification == null || userNotification.UserId != userId)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 404,
                    Message = "NOTIFICATION_NOT_FOUND",
                    Data = false
                };
            }

            if (userNotification.ReadAt.HasValue)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = true
                };
            }

            var success = await _unitOfWork.UserNotificationRepository.MarkAsReadAsync(request.UserNotificationId);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                StatusCode = success ? 200 : 404,
                Message = success ? "SUCCESS" : "NOTIFICATION_NOT_FOUND",
                Data = success
            };
        }

        /// <summary>
        /// Marks multiple notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Mark multiple notifications as read request containing notification IDs</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        public async Task<BaseResponse<int>> MarkMultipleNotificationsAsReadAsync(int userId, MarkMultipleNotificationsAsReadRequest request)
        {
            _logger.LogInformation("Marking {Count} notifications as read for user {UserId}", 
                request.UserNotificationIds.Count, userId);

            // Verify all notifications belong to the user
            var userNotifications = await _unitOfWork.UserNotificationRepository.GetByNotificationIdsForUserAsync(userId, request.UserNotificationIds);
            var validIds = userNotifications.Select(un => un.Id).ToList();

            if (!validIds.Any())
            {
                return new BaseResponse<int>
                {
                    StatusCode = 404,
                    Message = "NO_VALID_NOTIFICATIONS_FOUND",
                    Data = 0
                };
            }

            var count = await _unitOfWork.UserNotificationRepository.MarkMultipleAsReadAsync(validIds);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse<int>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = count
            };
        }

        /// <summary>
        /// Marks all unread notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with count of notifications marked as read</returns>
        public async Task<BaseResponse<int>> MarkAllNotificationsAsReadAsync(int userId)
        {
            _logger.LogInformation("Marking all unread notifications as read for user {UserId}", userId);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<int>
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                    Data = 0
                };
            }

            var count = await _unitOfWork.UserNotificationRepository.MarkAllAsReadForUserAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse<int>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = count
            };
        }

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Base response with unread notifications count</returns>
        public async Task<BaseResponse<int>> GetUnreadCountAsync(int userId)
        {
            _logger.LogInformation("Getting unread count for user {UserId}", userId);

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<int>
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                    Data = 0
                };
            }

            var count = await _unitOfWork.UserNotificationRepository.GetUnreadCountAsync(userId);

            return new BaseResponse<int>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = count
            };
        }
    }
}