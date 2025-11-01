using EvCoOwnership.API.Attributes;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Manager Account Controller - Quản lý tài khoản Admin/Staff
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Admin)]
    public class ManagerAccountController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ManagerAccountController> _logger;

        public ManagerAccountController(IUnitOfWork unitOfWork, ILogger<ManagerAccountController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tài khoản Manager với phân trang
        /// </summary>
        /// <param name="page">Số trang (mặc định: 1)</param>
        /// <param name="size">Kích thước trang (mặc định: 10)</param>
        /// <param name="search">Từ khóa tìm kiếm theo tên hoặc email</param>
        /// <param name="role">Lọc theo role (Admin/Staff)</param>
        /// <param name="status">Lọc theo trạng thái (Active/Inactive/Suspended)</param>
        /// <returns>Danh sách tài khoản Manager</returns>
        [HttpGet]
        public async Task<IActionResult> GetManagerAccounts(
            int page = 1, 
            int size = 10, 
            string? search = null, 
            EUserRole? role = null, 
            EUserStatus? status = null)
        {
            try
            {
                var query = _unitOfWork.UserRepository.GetQueryable()
                    .Where(u => u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(u => 
                        (u.FirstName + " " + u.LastName).ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower));
                }

                if (role.HasValue)
                {
                    query = query.Where(u => u.RoleEnum == role.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(u => u.StatusEnum == status.Value);
                }

                query = query.OrderByDescending(u => u.CreatedAt);
                var paginatedResult = new PaginatedList<User>(
                    query, 
                    page, 
                    size, 
                    1
                );

                var response = new BaseResponse<PaginatedList<User>>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNTS_RETRIEVED_SUCCESSFULLY",
                    Data = paginatedResult
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manager accounts");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết tài khoản Manager theo ID
        /// </summary>
        /// <param name="id">ID tài khoản Manager</param>
        /// <returns>Thông tin chi tiết tài khoản Manager</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetManagerAccount(int id)
        {
            try
            {
                var manager = await _unitOfWork.UserRepository.GetByIdAsync(id);
                
                if (manager == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                if (manager.RoleEnum != EUserRole.Admin && manager.RoleEnum != EUserRole.Staff)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                var response = new BaseResponse<User>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNT_RETRIEVED_SUCCESSFULLY",
                    Data = manager
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manager account with ID: {Id}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo tài khoản Manager mới
        /// </summary>
        /// <param name="request">Thông tin tài khoản Manager cần tạo</param>
        /// <returns>Tài khoản Manager đã được tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateManagerAccount([FromBody] CreateManagerAccountRequest request)
        {
            try
            {
                var emailExists = await _unitOfWork.UserRepository.EmailExistsAsync(request.Email);
                if (emailExists)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "EMAIL_ALREADY_EXISTS"
                    });
                }
                var manager = new User
                {
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpperInvariant(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Address = request.Address,
                    DateOfBirth = request.DateOfBirth,
                    RoleEnum = request.Role,
                    StatusEnum = EUserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    manager.PasswordHash = "HASHED_PASSWORD_PLACEHOLDER";
                    manager.PasswordSalt = "SALT_PLACEHOLDER";
                }

                await _unitOfWork.UserRepository.AddAsync(manager);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<User>
                {
                    StatusCode = 201,
                    Message = "MANAGER_ACCOUNT_CREATED_SUCCESSFULLY",
                    Data = manager
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manager account");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản Manager
        /// </summary>
        /// <param name="id">ID tài khoản Manager</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Tài khoản Manager đã được cập nhật</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateManagerAccount(int id, [FromBody] UpdateManagerAccountRequest request)
        {
            try
            {
                var manager = await _unitOfWork.UserRepository.GetByIdAsync(id);
                
                if (manager == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                if (manager.RoleEnum != EUserRole.Admin && manager.RoleEnum != EUserRole.Staff)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    manager.FirstName = request.FirstName;
                
                if (!string.IsNullOrWhiteSpace(request.LastName))
                    manager.LastName = request.LastName;
                
                if (!string.IsNullOrWhiteSpace(request.Phone))
                    manager.Phone = request.Phone;
                
                if (!string.IsNullOrWhiteSpace(request.Address))
                    manager.Address = request.Address;
                
                if (request.DateOfBirth.HasValue)
                    manager.DateOfBirth = request.DateOfBirth.Value;
                
                if (request.Role.HasValue)
                    manager.RoleEnum = request.Role.Value;
                
                if (request.Status.HasValue)
                    manager.StatusEnum = request.Status.Value;

                manager.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.UserRepository.UpdateAsync(manager);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<User>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNT_UPDATED_SUCCESSFULLY",
                    Data = manager
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manager account with ID: {Id}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa tài khoản Manager
        /// </summary>
        /// <param name="id">ID tài khoản Manager</param>
        /// <returns>Kết quả xóa tài khoản</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteManagerAccount(int id)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                {
                    return Unauthorized(new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "INVALID_TOKEN"
                    });
                }

                if (id == currentUserId)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_DELETE_OWN_ACCOUNT"
                    });
                }

                var manager = await _unitOfWork.UserRepository.GetByIdAsync(id);
                
                if (manager == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                if (manager.RoleEnum != EUserRole.Admin && manager.RoleEnum != EUserRole.Staff)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                if (manager.RoleEnum == EUserRole.Admin)
                {
                    var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(currentUserId);
                    if (currentUser?.RoleEnum != EUserRole.Admin)
                    {
                        return BadRequest(new BaseResponse<object>
                        {
                            StatusCode = 400,
                            Message = "INSUFFICIENT_PERMISSIONS_TO_DELETE_ADMIN"
                        });
                    }
                }

                await _unitOfWork.UserRepository.DeleteAsync(manager);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNT_DELETED_SUCCESSFULLY"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting manager account with ID: {Id}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Thay đổi trạng thái tài khoản Manager
        /// </summary>
        /// <param name="id">ID tài khoản Manager</param>
        /// <param name="request">Trạng thái mới</param>
        /// <returns>Tài khoản Manager với trạng thái đã cập nhật</returns>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeManagerAccountStatus(int id, [FromBody] ChangeManagerStatusRequest request)
        {
            try
            {
                var manager = await _unitOfWork.UserRepository.GetByIdAsync(id);
                
                if (manager == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                if (manager.RoleEnum != EUserRole.Admin && manager.RoleEnum != EUserRole.Staff)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MANAGER_ACCOUNT_NOT_FOUND"
                    });
                }

                manager.StatusEnum = request.Status;
                manager.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.UserRepository.UpdateAsync(manager);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<User>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNT_STATUS_UPDATED_SUCCESSFULLY",
                    Data = manager
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing manager account status with ID: {Id}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thống kê tài khoản Manager
        /// </summary>
        /// <returns>Thống kê số lượng Manager theo role và status</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetManagerAccountStatistics()
        {
            try
            {
                var totalManagers = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff);

                var totalAdmins = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => u.RoleEnum == EUserRole.Admin);

                var totalStaff = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => u.RoleEnum == EUserRole.Staff);

                var activeManagers = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => (u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff) && u.StatusEnum == EUserStatus.Active);

                var inactiveManagers = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => (u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff) && u.StatusEnum == EUserStatus.Inactive);

                var suspendedManagers = await _unitOfWork.UserRepository.GetQueryable()
                    .CountAsync(u => (u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff) && u.StatusEnum == EUserStatus.Suspended);

                var statistics = new
                {
                    TotalManagers = totalManagers,
                    TotalAdmins = totalAdmins,
                    TotalStaff = totalStaff,
                    ActiveManagers = activeManagers,
                    InactiveManagers = inactiveManagers,
                    SuspendedManagers = suspendedManagers,
                    ActivePercentage = totalManagers > 0 ? Math.Round((double)activeManagers / totalManagers * 100, 2) : 0
                };

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MANAGER_ACCOUNT_STATISTICS_RETRIEVED_SUCCESSFULLY",
                    Data = statistics
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manager account statistics");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// Request DTO cho việc tạo tài khoản Manager
    /// </summary>
    public class CreateManagerAccountRequest
    {
        /// <summary>
        /// Email của Manager
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên của Manager
        /// </summary>
        public string FirstName { get; set; } = string.Empty;
        
        /// <summary>
        /// Họ của Manager
        /// </summary>
        public string LastName { get; set; } = string.Empty;
        
        /// <summary>
        /// Số điện thoại
        /// </summary>
        public string? Phone { get; set; }
        
        /// <summary>
        /// Địa chỉ
        /// </summary>
        public string? Address { get; set; }
        
        /// <summary>
        /// Ngày sinh
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }
        
        /// <summary>
        /// Vai trò (Admin/Staff)
        /// </summary>
        public EUserRole Role { get; set; } = EUserRole.Staff;
        
        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// Request DTO cho việc cập nhật tài khoản Manager
    /// </summary>
    public class UpdateManagerAccountRequest
    {
        /// <summary>
        /// Tên mới
        /// </summary>
        public string? FirstName { get; set; }
        
        /// <summary>
        /// Họ mới
        /// </summary>
        public string? LastName { get; set; }
        
        /// <summary>
        /// Số điện thoại mới
        /// </summary>
        public string? Phone { get; set; }
        
        /// <summary>
        /// Địa chỉ mới
        /// </summary>
        public string? Address { get; set; }
        
        /// <summary>
        /// Ngày sinh mới
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }
        
        /// <summary>
        /// Vai trò mới
        /// </summary>
        public EUserRole? Role { get; set; }
        
        /// <summary>
        /// Trạng thái mới
        /// </summary>
        public EUserStatus? Status { get; set; }
    }

    /// <summary>
    /// Request DTO cho việc thay đổi trạng thái Manager
    /// </summary>
    public class ChangeManagerStatusRequest
    {
        /// <summary>
        /// Trạng thái mới (Active/Inactive/Suspended)
        /// </summary>
        public EUserStatus Status { get; set; }
    }
}
