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
                var query = _unitOfWork.UserRepository.GetAll()
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

        [HttpGet("statistics")]
        public async Task<IActionResult> GetManagerAccountStatistics()
        {
            try
            {
                var totalManagers = await _unitOfWork.UserRepository.GetAll()
                    .CountAsync(u => u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff);

                var totalAdmins = await _unitOfWork.UserRepository.GetAll()
                    .CountAsync(u => u.RoleEnum == EUserRole.Admin);

                var totalStaff = await _unitOfWork.UserRepository.GetAll()
                    .CountAsync(u => u.RoleEnum == EUserRole.Staff);

                var activeManagers = await _unitOfWork.UserRepository.GetAll()
                    .CountAsync(u => (u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff) && u.StatusEnum == EUserStatus.Active);

                var inactiveManagers = await _unitOfWork.UserRepository.GetAll()
                    .CountAsync(u => (u.RoleEnum == EUserRole.Admin || u.RoleEnum == EUserRole.Staff) && u.StatusEnum == EUserStatus.Inactive);

                var suspendedManagers = await _unitOfWork.UserRepository.GetAll()
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

    public class CreateManagerAccountRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public EUserRole Role { get; set; } = EUserRole.Staff;
        public string? Password { get; set; }
    }

    public class UpdateManagerAccountRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public EUserRole? Role { get; set; }
        public EUserStatus? Status { get; set; }
    }

    public class ChangeManagerStatusRequest
    {
        public EUserStatus Status { get; set; }
    }
}
