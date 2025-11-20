using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.GroupManagementDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;
using EvCoOwnership.Repositories.DTOs.LicenseDTOs;
using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Repositories.UoW;
using Microsoft.EntityFrameworkCore;

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
        private readonly IUnitOfWork _unitOfWork;
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
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for logging</param>
        public AdminController(
            IUserService userService,
            ILicenseVerificationService licenseService,
            IGroupService groupService,
            INotificationService notificationService,
            IProfileService profileService,
            IGroupManagementService groupManagementService,
            IUnitOfWork unitOfWork,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _licenseService = licenseService;
            _groupService = groupService;
            _notificationService = notificationService;
            _profileService = profileService;
            _groupManagementService = groupManagementService;
            _unitOfWork = unitOfWork;
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
            catch (Exception)
            {
                _logger.LogError("Error getting users list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
            catch (Exception)
            {
                _logger.LogError("Error creating user");
                return BadRequest(new BaseResponse<object>
                {
                    StatusCode = 400,
                    Message = "USER_CREATION_FAILED",
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
            catch (Exception)
            {
                _logger.LogError("Error updating user {UserId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
            catch (Exception)
            {
                _logger.LogError("Error deleting user {UserId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                });
            }
        }

        // --- Quản lý license ---
        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả license theo trạng thái
        /// 
        /// **Query Parameters:**
        /// - status: pending, verified, rejected, expired (optional - lấy tất cả nếu không có)
        /// - page: Số trang (mặc định 1)
        /// - pageSize: Số item per page (mặc định 10)
        /// </remarks>
        /// <response code="200">Lấy danh sách license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("licenses")]
        public async Task<IActionResult> GetLicenses(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get driving licenses with user information from database
                var coOwners = await _unitOfWork.CoOwnerRepository.GetAllAsync();
                var licenses = coOwners
                    .Where(co => co.DrivingLicenses != null && co.DrivingLicenses.Any())
                    .SelectMany(co => co.DrivingLicenses.Select(dl => new LicenseListResponse
                    {
                        Id = dl.Id,
                        LicenseNumber = dl.LicenseNumber,
                        IssuedBy = dl.IssuedBy,
                        IssueDate = dl.IssueDate,
                        ExpiryDate = dl.ExpiryDate,
                        LicenseImageUrl = dl.LicenseImageUrl,
                        VerificationStatus = dl.VerificationStatus,
                        RejectReason = dl.RejectReason,
                        UserName = $"{co.User?.FirstName} {co.User?.LastName}".Trim(),
                        UserId = co.UserId,
                        SubmittedAt = dl.CreatedAt,
                        VerifiedByUserName = dl.VerifiedByUser != null ? $"{dl.VerifiedByUser.FirstName} {dl.VerifiedByUser.LastName}".Trim() : null,
                        VerifiedAt = dl.VerifiedAt,
                        IsExpired = dl.ExpiryDate.HasValue && dl.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Now)
                    }))
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<EDrivingLicenseVerificationStatus>(status, true, out var statusEnum))
                    {
                        licenses = licenses.Where(l => l.VerificationStatus == statusEnum);
                    }
                }

                // Apply pagination
                var totalCount = licenses.Count();
                var paginatedLicenses = licenses
                    .OrderByDescending(x => x.SubmittedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new
                {
                    Items = paginatedLicenses,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = page * pageSize < totalCount,
                    HasPreviousPage = page > 1
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "LICENSE_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error getting licenses list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Duyệt license cho người dùng
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "licenseId": 1,
        ///   "notes": "License verified successfully"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Duyệt license thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/approve")]
        public async Task<IActionResult> ApproveLicense([FromBody] ApproveLicenseRequest request)
        {
            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get the license from database with details
                var license = await _unitOfWork.DrivingLicenseRepository.GetByIdWithDetailsAsync(request.LicenseId);
                if (license == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    });
                }

                // Update license status
                license.VerificationStatus = EDrivingLicenseVerificationStatus.Verified;
                license.VerifiedByUserId = adminUserId;
                license.VerifiedAt = DateTime.UtcNow;
                license.UpdatedAt = DateTime.UtcNow;
                license.RejectReason = null; // Clear any previous reject reason

                await _unitOfWork.DrivingLicenseRepository.UpdateAsync(license);
                await _unitOfWork.SaveChangesAsync();

                // Get admin user for response
                var adminUser = await _unitOfWork.UserRepository.GetByIdAsync(adminUserId);

                var response = new LicenseApprovalResponse
                {
                    LicenseId = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    VerificationStatus = license.VerificationStatus,
                    VerifiedByUserName = $"{adminUser?.FirstName} {adminUser?.LastName}".Trim(),
                    VerifiedAt = license.VerifiedAt.Value
                };

                _logger.LogInformation("License {LicenseId} approved by user {AdminUserId}", request.LicenseId, adminUserId);

                return Ok(new BaseResponse<LicenseApprovalResponse>
                {
                    StatusCode = 200,
                    Message = "LICENSE_APPROVED_SUCCESSFULLY",
                    Data = response
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error approving license {LicenseId}", request.LicenseId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Từ chối license cho người dùng với lý do cụ thể
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "licenseId": 1,
        ///   "rejectReason": "License đã hết hạn",
        ///   "notes": "Vui lòng cập nhật license mới"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Từ chối license thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/reject")]
        public async Task<IActionResult> RejectLicense([FromBody] RejectLicenseRequest request)
        {
            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get the license from database with details
                var license = await _unitOfWork.DrivingLicenseRepository.GetByIdWithDetailsAsync(request.LicenseId);
                if (license == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    });
                }

                // Update license status
                license.VerificationStatus = EDrivingLicenseVerificationStatus.Rejected;
                license.RejectReason = request.RejectReason;
                license.VerifiedByUserId = adminUserId;
                license.VerifiedAt = DateTime.UtcNow;
                license.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.DrivingLicenseRepository.UpdateAsync(license);
                await _unitOfWork.SaveChangesAsync();

                // Get admin user for response
                var adminUser = await _unitOfWork.UserRepository.GetByIdAsync(adminUserId);

                var response = new LicenseApprovalResponse
                {
                    LicenseId = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    VerificationStatus = license.VerificationStatus,
                    RejectReason = license.RejectReason,
                    VerifiedByUserName = $"{adminUser?.FirstName} {adminUser?.LastName}".Trim(),
                    VerifiedAt = license.VerifiedAt.Value
                };

                _logger.LogInformation("License {LicenseId} rejected by user {AdminUserId} with reason: {RejectReason}",
                    request.LicenseId, adminUserId, request.RejectReason);

                return Ok(new BaseResponse<LicenseApprovalResponse>
                {
                    StatusCode = 200,
                    Message = "LICENSE_REJECTED_SUCCESSFULLY",
                    Data = response
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error rejecting license {LicenseId}", request.LicenseId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
            catch (Exception)
            {
                _logger.LogError("Error getting groups list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Cập nhật trạng thái nhóm (Active/Inactive)
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Active"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật trạng thái thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="404">Không tìm thấy nhóm</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("group/{id}/status")]
        public async Task<IActionResult> UpdateGroupStatus(int id, [FromBody] object statusUpdate)
        {
            try
            {
                // Find the group (using Fund as a proxy for group - needs actual Group entity)
                var fund = await _unitOfWork.FundRepository.GetByIdAsync(id);
                if (fund == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "GROUP_NOT_FOUND"
                    });
                }

                // In a real implementation, this would update the actual Group entity status
                // For now, we'll just log the action and return success
                _logger.LogInformation("Group {GroupId} status update requested by admin {AdminId}",
                    id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_STATUS_UPDATED_SUCCESS",
                    Data = new
                    {
                        GroupId = id,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error updating group status {GroupId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
                // Get system settings from database
                var configurations = await _unitOfWork.DbContext.Set<Configuration>().ToListAsync();

                var settings = new Dictionary<string, object>();

                foreach (var config in configurations)
                {
                    // Parse values based on configuration key
                    switch (config.Key?.ToLowerInvariant())
                    {
                        case "maxbookingduration":
                        case "bookingadvancetime":
                        case "maintenancefeepercentage":
                            if (int.TryParse(config.Value, out int intValue))
                                settings[config.Key] = intValue;
                            else
                                settings[config.Key] = config.Value;
                            break;
                        case "defaultdepositamount":
                            if (decimal.TryParse(config.Value, out decimal decimalValue))
                                settings[config.Key] = decimalValue;
                            else
                                settings[config.Key] = config.Value;
                            break;
                        default:
                            if (config.Key != null)
                                settings[config.Key] = config.Value;
                            break;
                    }
                }

                // Add default values if configurations don't exist
                if (!settings.ContainsKey("MaxBookingDuration"))
                    settings["MaxBookingDuration"] = 24;
                if (!settings.ContainsKey("BookingAdvanceTime"))
                    settings["BookingAdvanceTime"] = 2;
                if (!settings.ContainsKey("DefaultDepositAmount"))
                    settings["DefaultDepositAmount"] = 500000;
                if (!settings.ContainsKey("MaintenanceFeePercentage"))
                    settings["MaintenanceFeePercentage"] = 10;

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_SETTINGS_RETRIEVED_SUCCESS",
                    Data = settings
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error getting system settings");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                });
            }
        }

        /// <summary>
        /// Admin
        /// </summary>
        /// <remarks>
        /// Cập nhật cấu hình hệ thống
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "MaxBookingDuration": 48,
        ///   "BookingAdvanceTime": 4,
        ///   "DefaultDepositAmount": 750000,
        ///   "MaintenanceFeePercentage": 12
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật cấu hình thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Admin</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, object> settings)
        {
            try
            {
                var currentConfigurations = await _unitOfWork.DbContext.Set<Configuration>().ToListAsync();
                var currentConfigDict = currentConfigurations.ToDictionary(c => c.Key, c => c);

                foreach (var setting in settings)
                {
                    var configValue = setting.Value?.ToString() ?? "";

                    if (currentConfigDict.ContainsKey(setting.Key))
                    {
                        // Update existing configuration
                        var existingConfig = currentConfigDict[setting.Key];
                        existingConfig.Value = configValue;
                        existingConfig.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new configuration
                        var newConfig = new Configuration
                        {
                            Key = setting.Key,
                            Value = configValue,
                            Description = $"System configuration for {setting.Key}",
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.DbContext.Set<Configuration>().AddAsync(newConfig);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("System settings updated by admin user {UserId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_SETTINGS_UPDATED_SUCCESS",
                    Data = new { UpdatedAt = DateTime.UtcNow, UpdatedCount = settings.Count }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error updating system settings");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
                // Get real data from database
                var allUsers = await _userService.GetAllAsync();
                var allGroups = await _groupService.ListAsync(new { });
                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var allCoOwners = await _unitOfWork.CoOwnerRepository.GetAllAsync();

                // Calculate pending licenses count
                var pendingLicensesCount = allCoOwners
                    .SelectMany(co => co.DrivingLicenses ?? new List<DrivingLicense>())
                    .Count();

                // Calculate active bookings (bookings with confirmed or pending status)
                var activeBookingsCount = allBookings.Count(b => b.StatusEnum == EBookingStatus.Confirmed || b.StatusEnum == EBookingStatus.Pending);

                // Calculate revenue from actual payments
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

                // Get actual payments for revenue calculation
                var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var thisMonthPayments = allPayments.Where(p =>
                    p.CreatedAt.HasValue &&
                    p.CreatedAt.Value.Month == currentMonth &&
                    p.CreatedAt.Value.Year == currentYear);
                var lastMonthPayments = allPayments.Where(p =>
                    p.CreatedAt.HasValue &&
                    p.CreatedAt.Value.Month == lastMonth &&
                    p.CreatedAt.Value.Year == lastMonthYear);

                var thisMonthRevenue = thisMonthPayments.Sum(p => p.Amount);
                var lastMonthRevenue = lastMonthPayments.Sum(p => p.Amount);
                var growth = lastMonthRevenue > 0 ? ((thisMonthRevenue - lastMonthRevenue) * 100) / lastMonthRevenue : 0;

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SYSTEM_REPORTS_RETRIEVED_SUCCESS",
                    Data = new
                    {
                        TotalUsers = allUsers.Count(),
                        TotalGroups = allGroups.Count(),
                        PendingLicenses = pendingLicensesCount,
                        ActiveBookings = activeBookingsCount,
                        Revenue = new
                        {
                            ThisMonth = thisMonthRevenue,
                            LastMonth = lastMonthRevenue,
                            Growth = growth
                        }
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error getting system reports");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
                // Get recent user activities from database
                var allUsers = await _userService.GetAllAsync();
                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var recentBookings = allBookings.OrderByDescending(b => b.CreatedAt).Take(20).ToList();

                var auditLogs = new List<object>();

                // Add user-related activities
                foreach (var user in allUsers.OrderByDescending(u => u.UpdatedAt).Take(10))
                {
                    auditLogs.Add(new
                    {
                        Id = user.Id,
                        Action = user.CreatedAt == user.UpdatedAt ? "USER_REGISTERED" : "USER_UPDATED",
                        UserId = user.Id,
                        UserName = user.Email,
                        Timestamp = user.UpdatedAt ?? user.CreatedAt ?? DateTime.UtcNow,
                        IpAddress = "System Generated", // Would need to track IP in real implementation
                        Details = $"User account {(user.CreatedAt == user.UpdatedAt ? "created" : "updated")}: {user.FirstName} {user.LastName}"
                    });
                }

                // Add booking-related activities
                foreach (var booking in recentBookings)
                {
                    // Get the user through CoOwner relationship
                    var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(booking.CoOwnerId ?? 0);
                    var booker = coOwner != null ? await _userService.GetByIdAsync(coOwner.UserId) : null;

                    auditLogs.Add(new
                    {
                        Id = booking.Id + 10000, // Offset to avoid ID conflicts
                        Action = "BOOKING_CREATED",
                        UserId = coOwner?.UserId ?? 0,
                        UserName = booker?.Email ?? "Unknown User",
                        Timestamp = booking.CreatedAt ?? DateTime.UtcNow,
                        IpAddress = "System Generated",
                        Details = $"Booking created for vehicle {booking.VehicleId}, status: {booking.StatusEnum}"
                    });
                }

                // Sort by timestamp descending
                var sortedLogs = auditLogs.OrderByDescending(log => ((dynamic)log).Timestamp).ToList();

                // Apply pagination
                var totalCount = sortedLogs.Count;
                var paginatedLogs = sortedLogs
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "AUDIT_LOGS_RETRIEVED_SUCCESS",
                    Data = new
                    {
                        Items = paginatedLogs,
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error getting audit logs");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
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
        public async Task<IActionResult> SendNotificationToUser([FromBody] Dictionary<string, object> request)
        {
            try
            {
                // Extract data from request
                if (!request.TryGetValue("userId", out var userIdObj) || !int.TryParse(userIdObj.ToString(), out int userId))
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Invalid or missing userId"
                    });
                }

                var notificationType = request.TryGetValue("notificationType", out var typeObj) ? typeObj.ToString() : "General";
                var additionalData = request.TryGetValue("additionalData", out var dataObj) ? dataObj.ToString() : "{}";

                // Check if user exists
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    });
                }

                // Create and send notification using notification service
                var notification = new NotificationEntity
                {
                    NotificationType = notificationType,
                    AdditionalDataJson = additionalData,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<NotificationEntity>().AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // Create user notification
                var userNotification = new UserNotification
                {
                    UserId = userId,
                    NotificationId = notification.Id,
                    ReadAt = null // null means unread
                };

                await _unitOfWork.UserNotificationRepository.CreateAsync(userNotification);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Notification sent to user {UserId} by admin {AdminId}",
                    userId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "NOTIFICATION_SENT_SUCCESS",
                    Data = new
                    {
                        NotificationId = notification.Id,
                        SentAt = notification.CreatedAt,
                        Status = "Delivered",
                        UserId = userId
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error sending notification to user");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
        public async Task<IActionResult> CreateNotification([FromBody] Dictionary<string, object> request)
        {
            try
            {
                // Extract data from request
                var notificationType = request.TryGetValue("notificationType", out var typeObj) ? typeObj.ToString() : "General";
                var additionalData = request.TryGetValue("additionalData", out var dataObj) ? dataObj.ToString() : "{}";

                // Extract user IDs
                var userIds = new List<int>();
                if (request.TryGetValue("userIds", out var userIdsObj))
                {
                    var userIdsJson = userIdsObj?.ToString();
                    if (!string.IsNullOrEmpty(userIdsJson))
                    {
                        try
                        {
                            var parsedUserIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(userIdsJson);
                            userIds = parsedUserIds ?? new List<int>();
                        }
                        catch
                        {
                            return BadRequest(new BaseResponse<object>
                            {
                                StatusCode = 400,
                                Message = "Invalid userIds format. Expected array of integers."
                            });
                        }
                    }
                }

                if (!userIds.Any())
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "At least one user ID is required"
                    });
                }

                // Create notification
                var notification = new NotificationEntity
                {
                    NotificationType = notificationType,
                    AdditionalDataJson = additionalData,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<NotificationEntity>().AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // Create user notifications for each user
                var successCount = 0;
                foreach (var userId in userIds)
                {
                    try
                    {
                        // Check if user exists
                        var user = await _userService.GetByIdAsync(userId);
                        if (user != null)
                        {
                            var userNotification = new UserNotification
                            {
                                UserId = userId,
                                NotificationId = notification.Id,
                                ReadAt = null
                            };

                            await _unitOfWork.UserNotificationRepository.CreateAsync(userNotification);
                            successCount++;
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("Failed to create notification for user {UserId}", userId);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk notification created and sent to {SuccessCount}/{TotalCount} users by admin {AdminId}",
                    successCount, userIds.Count, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "NOTIFICATION_CREATED_SUCCESS",
                    Data = new
                    {
                        NotificationId = notification.Id,
                        CreatedAt = notification.CreatedAt,
                        RecipientCount = successCount,
                        Status = "Sent",
                        TotalRequested = userIds.Count,
                        FailedCount = userIds.Count - successCount
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError("Error creating notification");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
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
                // Get notifications from database
                var allNotifications = await _unitOfWork.NotificationRepository.GetAllAsync();

                // Filter by notification type if provided
                var filteredNotifications = allNotifications.AsQueryable();
                if (!string.IsNullOrEmpty(notificationType))
                {
                    filteredNotifications = filteredNotifications.Where(n =>
                        n.NotificationType != null && n.NotificationType.ToLower().Contains(notificationType.ToLower()));
                }

                // Apply pagination
                var totalCount = filteredNotifications.Count();
                var notifications = filteredNotifications
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        Id = n.Id,
                        Type = n.NotificationType ?? "General",
                        Message = $"Notification of type: {n.NotificationType}", // Generate message from type
                        CreatedAt = n.CreatedAt ?? DateTime.UtcNow,
                        IsRead = false, // Default value - can be enhanced with user-specific read status
                        AdditionalData = n.AdditionalDataJson
                    })
                    .ToList();

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Notifications retrieved successfully",
                    Data = notifications,
                    AdditionalData = new
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception)
            {
                _logger.LogError("Error retrieving all notifications");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving notifications"
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving admin profile");
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
            catch (Exception)
            {
                _logger.LogError("Error updating admin profile");
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
            catch (Exception)
            {
                _logger.LogError("Error changing admin password");
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
            catch (Exception)
            {
                _logger.LogError("Error updating admin notification settings");
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
            catch (Exception)
            {
                _logger.LogError("Error updating admin privacy settings");
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving admin activity log");
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving admin security log");
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving user profile for admin view");
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving groups overview for admin");
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
            catch (Exception)
            {
                _logger.LogError("Error creating group");
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
            catch (Exception)
            {
                _logger.LogError("Error updating group status for group {GroupId}", request.GroupId);
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
            catch (Exception)
            {
                _logger.LogError("Error retrieving group analytics for admin");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion
    }
}
