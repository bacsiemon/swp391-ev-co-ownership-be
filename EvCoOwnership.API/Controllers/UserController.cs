using EvCoOwnership.API.Attributes;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for user management operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Initializes a new instance of the UserController
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="logger">Logger</param>
        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Gets paginated list of users (Admin/Staff only)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetUsers(int pageIndex = 1, int pageSize = 10)
        {
            return Ok(await _userService.GetPagingAsync(pageIndex, pageSize));
        }

        /// <summary>
        /// Gets user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <response code="200">User retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - can only view own profile unless admin/staff</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id:int}")]
        [AuthorizeRoles]
        public async Task<IActionResult> GetUserById(int id)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Check if user can access this profile
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var isAdmin = userRoles.Contains("Admin") || userRoles.Contains("Staff");

            if (id != currentUserId && !isAdmin)
            {
                return Forbid("ACCESS_DENIED");
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "USER_NOT_FOUND" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Updates user profile
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">Update user request</param>
        /// <response code="200">User updated successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - can only update own profile</response>
        /// <response code="404">User not found</response>
        [HttpPut("{id:int}")]
        [AuthorizeRoles]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Users can only update their own profile
            if (id != currentUserId)
            {
                return Forbid("ACCESS_DENIED");
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "USER_NOT_FOUND" });
            }

            // Update user fields
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                var nameParts = request.FullName.Split(' ', 2);
                user.FirstName = nameParts[0];
                user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            }
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.Phone = request.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(request.Address))
                user.Address = request.Address;
            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;
            // Note: CitizenId and ProfileImageId are not in User model currently
            // if (!string.IsNullOrWhiteSpace(request.CitizenId))
            //     user.CitizenId = request.CitizenId;
            // if (request.ProfileImageId.HasValue)
            //     user.ProfileImageId = request.ProfileImageId.Value;

            var updatedUser = await _userService.UpdateAsync(id, user);
            return Ok(new { Message = "USER_UPDATED_SUCCESSFULLY", Data = updatedUser });
        }

        /// <summary>
        /// Deletes user (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <response code="200">User deleted successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="404">User not found</response>
        [HttpDelete("{id:int}")]
        [AuthorizeRoles(EUserRole.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { Message = "USER_NOT_FOUND" });
            }

            return Ok(new { Message = "USER_DELETED_SUCCESSFULLY" });
        }
    }
}
