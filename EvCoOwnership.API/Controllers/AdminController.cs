using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Helpers.BaseClasses;

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
        private readonly ILogger<AdminController> _logger;

        /// <summary>
        /// Constructor for AdminController
        /// </summary>
        /// <param name="userService">User service for user management</param>
        /// <param name="licenseService">License service for license management</param>
        /// <param name="groupService">Group service for group management</param>
        /// <param name="logger">Logger for logging</param>
        public AdminController(
            IUserService userService,
            ILicenseVerificationService licenseService,
            IGroupService groupService,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _licenseService = licenseService;
            _groupService = groupService;
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
    }
}
