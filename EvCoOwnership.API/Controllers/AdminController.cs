using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.GroupManagementDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for admin-specific operations in the EV Co-ownership system
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILicenseVerificationService _licenseService;
        private readonly IGroupService _groupService;
        private readonly INotificationService _notificationService;
        private readonly IProfileService _profileService;
        private readonly IGroupManagementService _groupManagementService;
        private readonly ILogger<AdminController> _logger;

        /// <summary>
        /// Constructor for AdminController
        /// </summary>
        /// <param name="userService">User service for user management</param>
        /// <param name="licenseService">License service for license management</param>
        /// <param name="groupService">Group service for group management</param>
        /// <param name="notificationService">Notification service for notification management</param>
        /// <param name="profileService">Profile service for user profile management</param>
        /// <param name="groupManagementService">Group management service for group administration</param>
        /// <param name="logger">Logger for logging</param>
        public AdminController(
            IUserService userService,
            ILicenseVerificationService licenseService,
            IGroupService groupService,
            INotificationService notificationService,
            IProfileService profileService,
            IGroupManagementService groupManagementService,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _licenseService = licenseService;
            _groupService = groupService;
            _notificationService = notificationService;
            _profileService = profileService;
            _groupManagementService = groupManagementService;
            _logger = logger;
        }

        // --- Quản lý người dùng ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả người dùng trong hệ thống với phân trang
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/users?pageIndex=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Lấy danh sách người dùng thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _userService.GetPagingAsync(pageIndex, pageSize);
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "USER_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Tạo người dùng mới
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "email": "user@example.com",
        ///   "fullName": "John Doe",
        ///   "phoneNumber": "0123456789",
        ///   "password": "SecurePassword123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Tạo người dùng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("user")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                var createdUser = await _userService.CreateAsync(user);
                return StatusCode(201, new BaseResponse<User>
                {
                    StatusCode = 201,
                    Message = "USER_CREATED_SUCCESS",
                    Data = createdUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return BadRequest(new BaseResponse<object>
                {
                    StatusCode = 400,
                    Message = "USER_CREATION_FAILED",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Cập nhật thông tin người dùng
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "fullName": "John Doe Updated",
        ///   "phoneNumber": "0987654321",
        ///   "isActive": true
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            try
            {
                var updatedUser = await _userService.UpdateAsync(id, user);
                if (updatedUser == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    });
                }

                return Ok(new BaseResponse<User>
                {
                    StatusCode = 200,
                    Message = "USER_UPDATED_SUCCESS",
                    Data = updatedUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Xóa người dùng khỏi hệ thống
        /// </remarks>
        /// <response code="200">Xóa thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy người dùng</response>
        /// <response code="500">Lỗi server</response>
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    });
                }

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "USER_DELETED_SUCCESS"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Quản lý license ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả license cần duyệt
        /// </remarks>
        /// <response code="200">Lấy danh sách license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("licenses")]
        public async Task<IActionResult> GetLicenses()
        {
            try
            {
                // Tạm thời trả về mock data, có thể implement service sau
                var licenses = new[]
                {
                    new { Id = 1, LicenseNumber = "123456789", Status = "Pending", UserName = "John Doe" },
                    new { Id = 2, LicenseNumber = "987654321", Status = "Pending", UserName = "Jane Smith" }
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "LICENSE_LIST_RETRIEVED_SUCCESS",
                    Data = licenses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Duyệt license cho người dùng
        /// </remarks>
        /// <response code="200">Duyệt license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/{id}/approve")]
        public async Task<IActionResult> ApproveLicense(int id)
        {
            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _licenseService.UpdateLicenseStatusAsync(id.ToString(), "Approved", adminUserId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving license {LicenseId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Từ chối license cho người dùng
        /// </remarks>
        /// <response code="200">Từ chối license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/{id}/reject")]
        public async Task<IActionResult> RejectLicense(int id)
        {
            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _licenseService.UpdateLicenseStatusAsync(id.ToString(), "Rejected", adminUserId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting license {LicenseId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Quản lý nhóm & hệ thống ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả nhóm trong hệ thống
        /// </remarks>
        /// <response code="200">Lấy danh sách nhóm thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            try
            {
                var groups = await _groupService.ListAsync(new { });
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_LIST_RETRIEVED_SUCCESS",
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Cập nhật trạng thái nhóm (Active/Inactive)
        /// </remarks>
        /// <response code="200">Cập nhật trạng thái thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy nhóm</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("group/{id}/status")]
        public async Task<IActionResult> UpdateGroupStatus(int id)
        {
            try
            {
                // Logic cập nhật trạng thái nhóm - cần implement sau khi có service method
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_STATUS_UPDATED_SUCCESS"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group status {GroupId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Cấu hình hệ thống ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy cấu hình hệ thống
        /// </remarks>
        /// <response code="200">Lấy cấu hình thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = new
                {
                    MaxBookingDuration = 24, // hours
                    BookingAdvanceTime = 2, // hours
                    DefaultDepositAmount = 500000, // VND
                    MaintenanceFeePercentage = 10 // %
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_SETTINGS_RETRIEVED_SUCCESS",
                    Data = settings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system settings");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Cập nhật cấu hình hệ thống
        /// </remarks>
        /// <response code="200">Cập nhật cấu hình thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] object settings)
        {
            try
            {
                // Logic cập nhật cấu hình - cần implement service
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_SETTINGS_UPDATED_SUCCESS"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system settings");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Báo cáo & giám sát ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy báo cáo tổng quan hệ thống
        /// </remarks>
        /// <response code="200">Lấy báo cáo thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                var reports = new
                {
                    TotalUsers = await _userService.GetAllAsync(),
                    TotalGroups = (await _groupService.ListAsync(new { })).Count(),
                    PendingLicenses = 5, // Mock data
                    ActiveBookings = 12, // Mock data
                    Revenue = new
                    {
                        ThisMonth = 15000000, // VND
                        LastMonth = 12000000,
                        Growth = 25 // %
                    }
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_REPORTS_RETRIEVED_SUCCESS",
                    Data = new
                    {
                        TotalUsers = reports.TotalUsers.Count(),
                        TotalGroups = reports.TotalGroups,
                        PendingLicenses = reports.PendingLicenses,
                        ActiveBookings = reports.ActiveBookings,
                        Revenue = reports.Revenue
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system reports");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy log hoạt động của hệ thống
        /// </remarks>
        /// <response code="200">Lấy audit logs thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var auditLogs = new[]
                {
                    new {
                        Id = 1,
                        Action = "USER_LOGIN",
                        UserId = 123,
                        UserName = "john.doe@example.com",
                        Timestamp = DateTime.UtcNow.AddHours(-2),
                        IpAddress = "192.168.1.100",
                        Details = "Successful login"
                    },
                    new {
                        Id = 2,
                        Action = "LICENSE_APPROVED",
                        UserId = 456,
                        UserName = "admin@system.com",
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        IpAddress = "192.168.1.101",
                        Details = "License #ABC123 approved for user #789"
                    }
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "AUDIT_LOGS_RETRIEVED_SUCCESS",
                    Data = new
                    {
                        Items = auditLogs,
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalCount = 2
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- NOTIFICATION MANAGEMENT ---

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Manually sends a notification to a specific user.
        /// 
        /// Parameters:
        /// - request: Notification data including user ID, message, and type
        /// 
        /// Sample request:
        /// 
        /// POST /api/admin/notifications/send-to-user
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "userId": 123,
        ///   "notificationType": "Booking",
        ///   "additionalData": "{\"bookingId\": 456, \"vehicleId\": 789}"
        /// }
        /// </remarks>
        /// <response code="200">Notification sent successfully</response>
        /// <response code="400">Bad request - Invalid user ID or missing data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("notifications/send-to-user")]
        public async Task<IActionResult> SendNotificationToUser([FromBody] object request)
        {
            try
            {
                // Mock implementation - replace with actual service call when DTO is available
                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Notification sent successfully",
                    Data = new
                    {
                        NotificationId = new Random().Next(1000, 9999),
                        SentAt = DateTime.UtcNow,
                        Status = "Delivered"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while sending notification",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Manually creates and sends a notification to multiple users.
        /// 
        /// Parameters:
        /// - request: Notification data including user IDs, type, and priority
        /// 
        /// Sample request:
        /// 
        /// POST /api/admin/notifications/create-notification
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "notificationType": "System",
        ///   "userIds": [1, 2, 3, 4, 5],
        ///   "additionalData": "{\"maintenanceWindow\": \"2025-10-15T02:00:00Z\"}"
        /// }
        /// </remarks>
        /// <response code="200">Notification created and sent successfully</response>
        /// <response code="400">Bad request - Invalid user IDs or missing data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("notifications/create-notification")]
        public async Task<IActionResult> CreateNotification([FromBody] object request)
        {
            try
            {
                // Mock implementation - replace with actual service call when DTO is available
                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Notification created and sent successfully",
                    Data = new
                    {
                        NotificationId = new Random().Next(1000, 9999),
                        CreatedAt = DateTime.UtcNow,
                        RecipientCount = 5,
                        Status = "Sent"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating notification",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Get all notifications in the system with pagination and filtering
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/notifications?pageIndex=1&amp;pageSize=20&amp;notificationType=System
        /// ```
        /// </remarks>
        /// <response code="200">Notifications retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("notifications")]
        public async Task<IActionResult> GetAllNotifications(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? notificationType = null)
        {
            try
            {
                // Mock implementation - replace with actual service call
                var notifications = new List<object>
                {
                    new { Id = 1, Type = "Booking", Message = "Your booking has been confirmed", CreatedAt = DateTime.UtcNow.AddHours(-2), IsRead = true },
                    new { Id = 2, Type = "Maintenance", Message = "Vehicle maintenance required", CreatedAt = DateTime.UtcNow.AddHours(-5), IsRead = false },
                    new { Id = 3, Type = "System", Message = "System maintenance scheduled", CreatedAt = DateTime.UtcNow.AddDays(-1), IsRead = false }
                };

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Notifications retrieved successfully",
                    Data = notifications,
                    AdditionalData = new
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalCount = notifications.Count,
                        TotalPages = (int)Math.Ceiling(notifications.Count / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all notifications");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving notifications",
                    Errors = ex.Message
                });
            }
        }

        #region Profile Management

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Retrieve your complete admin profile with system management statistics and settings.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/profile
        /// ```
        /// </remarks>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetProfileAsync(userId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin profile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Update your admin profile information including personal details and contact information.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "firstName": "Admin",
        ///   "lastName": "User",
        ///   "phone": "+1-555-0123",
        ///   "address": "123 Admin St, City, Country",
        ///   "dateOfBirth": "1985-01-15",
        ///   "bio": "System Administrator for EV Co-ownership Platform"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Profile update request</param>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdateProfileAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin profile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Change your admin account password for enhanced security.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "currentPassword": "currentAdminPassword123!",
        ///   "newPassword": "newAdminPassword456@",
        ///   "confirmPassword": "newAdminPassword456@"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Password change request</param>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid current password or validation error</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("profile/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.ChangePasswordAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing admin password");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Configure your admin notification preferences and alert settings.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "emailNotifications": true,
        ///   "pushNotifications": true,
        ///   "bookingReminders": false,
        ///   "maintenanceAlerts": true,
        ///   "paymentNotifications": true,
        ///   "systemAnnouncements": true
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Notification settings request</param>
        /// <response code="200">Notification settings updated successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile/notification-settings")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdateNotificationSettingsAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin notification settings");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Configure your admin privacy preferences and system access settings.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "profileVisibility": true,
        ///   "showEmail": false,
        ///   "showPhone": false,
        ///   "shareUsageData": true,
        ///   "allowDataAnalytics": true
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Privacy settings request</param>
        /// <response code="200">Privacy settings updated successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile/privacy-settings")]
        public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdatePrivacySettingsAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin privacy settings");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// View your admin account activity history and administrative actions performed.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/profile/activity-log?page=1&amp;pageSize=50&amp;category=user-management
        /// ```
        /// </remarks>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50)</param>
        /// <param name="category">Optional activity category filter</param>
        /// <response code="200">Activity log retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile/activity-log")]
        public async Task<IActionResult> GetActivityLog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? category = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetActivityLogAsync(userId, page, pageSize, category);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin activity log");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// View security events and login history for your admin account with enhanced monitoring.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/profile/security-log?days=30
        /// ```
        /// </remarks>
        /// <param name="days">Number of days to look back (default: 30)</param>
        /// <response code="200">Security log retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile/security-log")]
        public async Task<IActionResult> GetSecurityLog([FromQuery] int days = 30)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetSecurityLogAsync(userId, days);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin security log");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// View another user's profile (admin privilege for user management and support).
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/profile/user/123
        /// ```
        /// </remarks>
        /// <param name="userId">Target user ID to view profile</param>
        /// <response code="200">User profile retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile/user/{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetProfileAsync(userId, adminId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for admin view");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Group Management Administration

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Get comprehensive overview of all groups in the system with statistics and trends.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/groups/overview
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "GROUPS_OVERVIEW_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "groups": [
        ///       {
        ///         "groupId": 1,
        ///         "groupName": "EV Enthusiasts Group",
        ///         "memberCount": 5,
        ///         "vehicleCount": 2,
        ///         "status": "Active",
        ///         "createdDate": "2024-01-01T00:00:00Z",
        ///         "totalFunds": 50000.00,
        ///         "activeDisputeCount": 1,
        ///         "utilizationRate": 75.5,
        ///         "healthScore": "Good"
        ///       }
        ///     ],
        ///     "statistics": {
        ///       "totalGroups": 25,
        ///       "newGroupsThisMonth": 3,
        ///       "activeDisputes": 5,
        ///       "averageGroupSize": 4.2,
        ///       "averageVehiclesPerGroup": 1.8,
        ///       "systemUtilizationRate": 68.5,
        ///       "totalSystemFunds": 1250000.00
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Groups overview retrieved successfully</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("groups/overview")]
        public async Task<IActionResult> GetGroupsOverview()
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.GetGroupsOverviewAsync(adminId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => Forbid(),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups overview for admin");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Create a new group with initial members and settings.
        /// 
        /// Sample request:
        /// ```
        /// POST /api/admin/group
        /// Content-Type: application/json
        /// 
        /// {
        ///   "groupName": "Green Commuters",
        ///   "description": "A group focused on green commuting solutions",
        ///   "createdByUserId": 101,
        ///   "initialMembers": [
        ///     {
        ///       "userId": 101,
        ///       "ownershipPercentage": 50.0,
        ///       "role": "Owner"
        ///     },
        ///     {
        ///       "userId": 102,
        ///       "ownershipPercentage": 30.0,
        ///       "role": "CoOwner"
        ///     },
        ///     {
        ///       "userId": 103,
        ///       "ownershipPercentage": 20.0,
        ///       "role": "CoOwner"
        ///     }
        ///   ],
        ///   "settings": {
        ///     "autoApproveBookings": false,
        ///     "maxBookingDays": 7,
        ///     "minimumFundBalance": 5000.00,
        ///     "allowMemberInvites": true,
        ///     "requireUnanimousVoting": false
        ///   }
        /// }
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 201,
        ///   "message": "GROUP_CREATED_SUCCESSFULLY",
        ///   "data": {
        ///     "groupId": 26,
        ///     "groupName": "Green Commuters",
        ///     "memberCount": 3,
        ///     "vehicleCount": 0,
        ///     "status": "Active",
        ///     "createdDate": "2024-11-01T16:00:00Z",
        ///     "totalFunds": 0.00,
        ///     "activeDisputeCount": 0,
        ///     "utilizationRate": 0.0,
        ///     "healthScore": "New"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Group creation details including members and settings</param>
        /// <response code="201">Group created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="409">Group name already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.CreateGroupAsync(request, adminId);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(CreateGroup), response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    409 => Conflict(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Update the status of a group (activate, suspend, or terminate).
        /// 
        /// Sample request:
        /// ```
        /// PUT /api/admin/group/status
        /// Content-Type: application/json
        /// 
        /// {
        ///   "groupId": 26,
        ///   "newStatus": "Suspended",
        ///   "reason": "Financial irregularities detected - pending investigation",
        ///   "notifyMembers": true,
        ///   "effectiveDate": "2024-11-01T18:00:00Z"
        /// }
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "GROUP_STATUS_UPDATED_SUCCESSFULLY",
        ///   "data": {
        ///     "groupId": 26,
        ///     "groupName": "Green Commuters",
        ///     "memberCount": 3,
        ///     "vehicleCount": 0,
        ///     "status": "Suspended",
        ///     "createdDate": "2024-11-01T16:00:00Z",
        ///     "totalFunds": 15000.00,
        ///     "activeDisputeCount": 0,
        ///     "utilizationRate": 45.0,
        ///     "healthScore": "Fair"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Group status update details including reason and notification settings</param>
        /// <response code="200">Group status updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("group/status")]
        public async Task<IActionResult> UpdateGroupStatus([FromBody] UpdateGroupStatusRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.UpdateGroupStatusAsync(request, adminId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group status for group {GroupId}", request.GroupId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Get comprehensive analytics about groups including performance metrics, financial trends, and utilization data.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/admin/groups/analytics?startDate=2024-10-01&amp;endDate=2024-11-01
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "GROUP_ANALYTICS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "totalGroups": 25,
        ///     "activeGroups": 22,
        ///     "suspendedGroups": 2,
        ///     "terminatedGroups": 1,
        ///     "totalMembers": 105,
        ///     "totalVehicles": 45,
        ///     "totalFundsAmount": 1250000.00,
        ///     "topPerformingGroups": [
        ///       {
        ///         "groupId": 1,
        ///         "groupName": "EV Enthusiasts Group",
        ///         "performanceScore": 95.5,
        ///         "utilizationRate": 87.5,
        ///         "revenueGenerated": 125000.00,
        ///         "memberSatisfactionScore": 92,
        ///         "disputeCount": 0
        ///       }
        ///     ],
        ///     "financialTrends": [
        ///       {
        ///         "month": "2024-10-01T00:00:00Z",
        ///         "totalRevenue": 125000.00,
        ///         "totalExpenses": 75000.00,
        ///         "netProfit": 50000.00,
        ///         "activeGroups": 24
        ///       }
        ///     ],
        ///     "vehicleUtilization": [
        ///       {
        ///         "vehicleId": 201,
        ///         "vehicleName": "Tesla Model 3",
        ///         "groupId": 1,
        ///         "groupName": "EV Enthusiasts Group",
        ///         "utilizationRate": 85.0,
        ///         "totalBookings": 45,
        ///         "revenueGenerated": 22500.00
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="startDate">Start date for analytics period (defaults to 30 days ago)</param>
        /// <param name="endDate">End date for analytics period (defaults to today)</param>
        /// <response code="200">Group analytics retrieved successfully</response>
        /// <response code="400">Invalid date range</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("groups/analytics")]
        public async Task<IActionResult> GetGroupAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Default to last 30 days if dates not provided
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { message = "START_DATE_CANNOT_BE_AFTER_END_DATE" });
                }

                var response = await _groupManagementService.GetGroupAnalyticsAsync(start, end, adminId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group analytics for admin");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion
    }
}
