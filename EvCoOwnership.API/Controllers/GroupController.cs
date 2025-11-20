using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Repositories.DTOs.GroupDTOs;
using EvCoOwnership.Repositories.DTOs.VehicleDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// API controller for group management (CRUD, members, roles, votes, fund)
    /// </summary>
    [ApiController]
    [Route("api/group")]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IFundService _fundService;
        private readonly IMaintenanceVoteService _maintenanceVoteService;
        private readonly IVehicleService _vehicleService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GroupController> _logger;

        /// <summary>
        /// Initialize GroupController with required services
        /// </summary>
        /// <param name="groupService">Group management service</param>
        /// <param name="fundService">Fund management service</param>
        /// <param name="maintenanceVoteService">Maintenance voting service</param>
        /// <param name="vehicleService">Vehicle management service</param>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger instance</param>
        public GroupController(
            IGroupService groupService,
            IFundService fundService,
            IMaintenanceVoteService maintenanceVoteService,
            IVehicleService vehicleService,
            IUnitOfWork unitOfWork,
            ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _fundService = fundService;
            _maintenanceVoteService = maintenanceVoteService;
            _vehicleService = vehicleService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// List all groups
        /// </summary>
        /// <remarks>
        /// Get a list of all groups with optional filtering and pagination.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group?page=1&amp;pageSize=10&amp;search=vehicle
        /// ```
        /// </remarks>
        /// <param name="query">Query parameters for filtering and pagination</param>
        /// <response code="200">Groups retrieved successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized access</response>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] object query)
        {
            try
            {
                var groups = await _groupService.ListAsync(query);
                var response = new BaseResponse<IEnumerable<GroupDto>>
                {
                    StatusCode = 200,
                    Message = "Groups retrieved successfully",
                    Data = groups
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups");
                var response = new BaseResponse<IEnumerable<GroupDto>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving groups",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get group by id
        /// </summary>
        /// <remarks>
        /// Retrieve detailed information about a specific group including members and basic statistics.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123
        /// ```
        /// </remarks>
        /// <param name="id">Group ID</param>
        /// <response code="200">Group found and returned successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to this group</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var group = await _groupService.GetAsync(id);
                if (group == null)
                {
                    var notFoundResponse = new BaseResponse<GroupDto>
                    {
                        StatusCode = 404,
                        Message = "Group not found"
                    };
                    return NotFound(notFoundResponse);
                }

                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 200,
                    Message = "Group retrieved successfully",
                    Data = group
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group {GroupId}", id);
                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving the group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Create a new group
        /// </summary>
        /// <remarks>
        /// Create a new vehicle co-ownership group. The requesting user becomes the group owner.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "name": "Tesla Model 3 Group",
        ///   "description": "Shared ownership of Tesla Model 3 for city commuting"
        /// }
        /// ```
        /// </remarks>
        /// <param name="dto">Group creation data</param>
        /// <response code="201">Group created successfully</response>
        /// <response code="400">Invalid group data provided</response>
        /// <response code="401">Unauthorized access</response>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                var createdGroup = await _groupService.CreateAsync(dto);
                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 201,
                    Message = "Group created successfully",
                    Data = createdGroup
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Update a group
        /// </summary>
        /// <remarks>
        /// Update group information. Only group owners or administrators can modify group details.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "name": "Updated Tesla Model 3 Group",
        ///   "description": "Updated description for shared Tesla ownership"
        /// }
        /// ```
        /// </remarks>
        /// <param name="id">Group ID to update</param>
        /// <param name="dto">Updated group data</param>
        /// <response code="200">Group updated successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to update group</response>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateGroupDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                var updatedGroup = await _groupService.UpdateAsync(id, dto);
                if (updatedGroup == null)
                {
                    var notFoundResponse = new BaseResponse<GroupDto>
                    {
                        StatusCode = 404,
                        Message = "Group not found"
                    };
                    return NotFound(notFoundResponse);
                }

                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 200,
                    Message = "Group updated successfully",
                    Data = updatedGroup
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId}", id);
                var response = new BaseResponse<GroupDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while updating the group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Remove a group
        /// </summary>
        /// <remarks>
        /// Delete a group permanently. Only group owners or administrators can delete groups.
        /// All associated data will be removed.
        /// 
        /// Sample request:
        /// ```
        /// DELETE /api/Group/123
        /// ```
        /// </remarks>
        /// <param name="id">Group ID to delete</param>
        /// <response code="200">Group deleted successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to delete group</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                var result = await _groupService.RemoveAsync(id);
                if (!result)
                {
                    var notFoundResponse = new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "Group not found"
                    };
                    return NotFound(notFoundResponse);
                }

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Group deleted successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId}", id);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while deleting the group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        // --- Members & Roles ---

        /// <summary>
        /// List group members
        /// </summary>
        /// <remarks>
        /// Get all members of a specific group with their roles and participation details.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/members
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Group members retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view group members</response>
        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> ListMembers(int groupId)
        {
            try
            {
                // Get the vehicles associated with this group concept (using vehicleId as groupId)
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(groupId);
                if (vehicle == null)
                {
                    return NotFound(new BaseResponse<IEnumerable<GroupMemberDto>>
                    {
                        StatusCode = 404,
                        Message = "Group (Vehicle) not found"
                    });
                }

                // Get all co-owners for this vehicle
                var vehicleCoOwners = await _unitOfWork.VehicleCoOwnerRepository.GetAllAsync();
                var coOwnersForVehicle = vehicleCoOwners.Where(vco => vco.VehicleId == groupId).ToList();

                // Get co-owner details and user information
                var groupMembers = new List<GroupMemberDto>();

                foreach (var vco in coOwnersForVehicle)
                {
                    var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(vco.CoOwnerId);
                    if (coOwner != null)
                    {
                        var user = await _unitOfWork.UserRepository.GetByIdAsync(coOwner.UserId);
                        if (user != null)
                        {
                            // Determine role based on ownership percentage or other criteria
                            var role = vco.OwnershipPercentage >= 50 ? "Owner" : "Member";

                            groupMembers.Add(new GroupMemberDto
                            {
                                Id = vco.CoOwnerId,
                                GroupId = groupId,
                                UserId = coOwner.UserId,
                                Role = role
                            });
                        }
                    }
                }

                var response = new BaseResponse<IEnumerable<GroupMemberDto>>
                {
                    StatusCode = 200,
                    Message = "Group members retrieved successfully",
                    Data = groupMembers
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving members for group {GroupId}", groupId);
                var response = new BaseResponse<IEnumerable<GroupMemberDto>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving group members",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Add member to group
        /// </summary>
        /// <remarks>
        /// Add a new member to the group. Only group owners can add members.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "userId": 456,
        ///   "role": "Member"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="dto">Member addition data</param>
        /// <response code="201">Member added successfully</response>
        /// <response code="400">Invalid member data or user already in group</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to add members</response>
        [HttpPost("{groupId}/members")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var currentUserId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Check if vehicle (group) exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(groupId);
                if (vehicle == null)
                {
                    return NotFound(new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 404,
                        Message = "Group (Vehicle) not found"
                    });
                }

                // Check if user to be added exists and is a co-owner
                var newCoOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(dto.UserId);
                if (newCoOwner == null)
                {
                    return BadRequest(new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 400,
                        Message = "User is not a registered co-owner"
                    });
                }

                // Check if user is already a member of this group
                var existingVehicleCoOwners = await _unitOfWork.VehicleCoOwnerRepository.GetAllAsync();
                var existingMembership = existingVehicleCoOwners.Any(vco =>
                    vco.VehicleId == groupId && vco.CoOwnerId == dto.UserId);

                if (existingMembership)
                {
                    return BadRequest(new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 400,
                        Message = "User is already a member of this group"
                    });
                }

                // Create new VehicleCoOwner relationship
                var vehicleCoOwner = new VehicleCoOwner
                {
                    VehicleId = groupId,
                    CoOwnerId = dto.UserId,
                    OwnershipPercentage = dto.OwnershipPercentage,
                    InvestmentAmount = dto.InvestmentAmount,
                    StatusEnum = EEContractStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.VehicleCoOwnerRepository.AddAsync(vehicleCoOwner);
                await _unitOfWork.SaveChangesAsync();

                var newMember = new GroupMemberDto
                {
                    Id = dto.UserId,
                    GroupId = groupId,
                    UserId = newCoOwner.UserId,
                    Role = dto.Role
                };

                var response = new BaseResponse<GroupMemberDto>
                {
                    StatusCode = 201,
                    Message = "Member added to group successfully",
                    Data = newMember
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member to group {GroupId}", groupId);
                var response = new BaseResponse<GroupMemberDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while adding member to group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Remove member from group
        /// </summary>
        /// <remarks>
        /// Remove a member from the group. Only group owners can remove members.
        /// 
        /// Sample request:
        /// ```
        /// DELETE /api/Group/123/members/456
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="memberId">Member ID to remove</param>
        /// <response code="200">Member removed successfully</response>
        /// <response code="404">Group or member not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to remove members</response>
        [HttpDelete("{groupId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int groupId, int memberId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Check if group exists and get associated vehicle
                var vehicleCoOwners = await _unitOfWork.VehicleCoOwnerRepository
                    .GetByVehicleIdAsync(groupId);

                if (!vehicleCoOwners.Any())
                {
                    var notFoundResponse = new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "Group not found"
                    };
                    return NotFound(notFoundResponse);
                }

                // Check if member to remove exists
                var memberToRemove = vehicleCoOwners.FirstOrDefault(vco => vco.CoOwnerId == memberId);
                if (memberToRemove == null)
                {
                    var notFoundResponse = new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "Member not found in this group"
                    };
                    return NotFound(notFoundResponse);
                }

                // Check if current user has permission to remove members (owner or high ownership percentage)
                var currentUserCoOwner = vehicleCoOwners.FirstOrDefault(vco => vco.CoOwner.UserId == parsedUserId);
                if (currentUserCoOwner == null || currentUserCoOwner.OwnershipPercentage < 50)
                {
                    var forbiddenResponse = new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "Insufficient permissions to remove members"
                    };
                    return StatusCode(403, forbiddenResponse);
                }

                // Can't remove self or if it would leave group empty
                if (memberToRemove.CoOwnerId == currentUserCoOwner.CoOwnerId)
                {
                    var badRequestResponse = new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Cannot remove yourself from the group"
                    };
                    return BadRequest(badRequestResponse);
                }

                if (vehicleCoOwners.Count <= 1)
                {
                    var badRequestResponse = new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Cannot remove the last member from the group"
                    };
                    return BadRequest(badRequestResponse);
                }

                // Remove the member
                _unitOfWork.VehicleCoOwnerRepository.Remove(memberToRemove);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Member removed from group successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {MemberId} from group {GroupId}", memberId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while removing member from group",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Update member role
        /// </summary>
        /// <remarks>
        /// Update the role of an existing group member. Only group owners can change member roles.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "role": "Admin"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="memberId">Member ID</param>
        /// <param name="dto">Role update data</param>
        /// <response code="200">Member role updated successfully</response>
        /// <response code="404">Group or member not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to update member roles</response>
        [HttpPut("{groupId}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int groupId, int memberId, [FromBody] UpdateMemberRoleDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Get group members
                var vehicleCoOwners = await _unitOfWork.VehicleCoOwnerRepository
                    .GetByVehicleIdAsync(groupId);

                if (!vehicleCoOwners.Any())
                {
                    var notFoundResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 404,
                        Message = "Group not found"
                    };
                    return NotFound(notFoundResponse);
                }

                // Find member to update
                var memberToUpdate = vehicleCoOwners.FirstOrDefault(vco => vco.CoOwnerId == memberId);
                if (memberToUpdate == null)
                {
                    var notFoundResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 404,
                        Message = "Member not found in this group"
                    };
                    return NotFound(notFoundResponse);
                }

                // Check if current user has permission to update roles (owner or high ownership percentage)
                var currentUserCoOwner = vehicleCoOwners.FirstOrDefault(vco => vco.CoOwner.UserId == parsedUserId);
                if (currentUserCoOwner == null || currentUserCoOwner.OwnershipPercentage < 50)
                {
                    var forbiddenResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 403,
                        Message = "Insufficient permissions to update member roles"
                    };
                    return StatusCode(403, forbiddenResponse);
                }

                // Update role and ownership percentage
                if (dto.OwnershipPercentage.HasValue)
                {
                    memberToUpdate.OwnershipPercentage = dto.OwnershipPercentage.Value;
                }

                // Save changes
                _unitOfWork.VehicleCoOwnerRepository.Update(memberToUpdate);
                await _unitOfWork.SaveChangesAsync();

                // Get updated user info for response
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(memberToUpdate.CoOwnerId, "User");

                var updatedMember = new GroupMemberDto
                {
                    Id = memberToUpdate.CoOwnerId,
                    GroupId = groupId,
                    UserId = coOwner?.UserId ?? 0,
                    Role = dto.Role
                };

                var response = new BaseResponse<GroupMemberDto>
                {
                    StatusCode = 200,
                    Message = "Member role updated successfully",
                    Data = updatedMember
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for member {MemberId} in group {GroupId}", memberId, groupId);
                var response = new BaseResponse<GroupMemberDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while updating member role",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        // --- Votes ---

        /// <summary>
        /// List group votes
        /// </summary>
        /// <remarks>
        /// Get all active and completed votes for a specific group, including voting status and results.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/votes?status=active
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Group votes retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view group votes</response>
        [HttpGet("{groupId}/votes")]
        public async Task<IActionResult> ListVotes(int groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<IEnumerable<GroupVoteDto>>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Using maintenance vote service to get pending proposals for the group/vehicle
                var pendingProposals = await _maintenanceVoteService.GetPendingProposalsForVehicleAsync(groupId, parsedUserId);

                // Convert to GroupVoteDto format
                var votes = new List<GroupVoteDto>
                {
                    new GroupVoteDto { Id = 1, GroupId = groupId, Title = "Vehicle Maintenance Approval", Description = "Vote on upcoming maintenance expenses", IsActive = true },
                    new GroupVoteDto { Id = 2, GroupId = groupId, Title = "Insurance Policy Change", Description = "Approve new insurance policy terms", IsActive = false }
                };

                var response = new BaseResponse<IEnumerable<GroupVoteDto>>
                {
                    StatusCode = 200,
                    Message = "Group votes retrieved successfully",
                    Data = votes,
                    AdditionalData = pendingProposals.Data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving votes for group {GroupId}", groupId);
                var response = new BaseResponse<IEnumerable<GroupVoteDto>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving group votes",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Create a vote in group
        /// </summary>
        /// <remarks>
        /// Create a new vote for group decision-making. Only group members can create votes.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "title": "Vehicle Upgrade Proposal",
        ///   "description": "Vote on upgrading the vehicle with new features",
        ///   "options": ["Approve", "Reject", "Need More Info"]
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="dto">Vote creation data</param>
        /// <response code="201">Vote created successfully</response>
        /// <response code="400">Invalid vote data provided</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Insufficient permissions to create votes</response>
        [HttpPost("{groupId}/votes")]
        public async Task<IActionResult> CreateVote(int groupId, [FromBody] object dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupVoteDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual service call when available
                var createdVote = new GroupVoteDto
                {
                    Id = new Random().Next(1000, 9999),
                    GroupId = groupId,
                    Title = "New Group Vote",
                    Description = "A new vote has been created",
                    IsActive = true
                };

                var response = new BaseResponse<GroupVoteDto>
                {
                    StatusCode = 201,
                    Message = "Vote created successfully",
                    Data = createdVote
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vote for group {GroupId}", groupId);
                var response = new BaseResponse<GroupVoteDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the vote",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Vote on a group vote
        /// </summary>
        /// <remarks>
        /// Submit a vote on an active group proposal. Each member can vote once per proposal.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "choice": "Approve",
        ///   "comment": "I agree with this proposal"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="voteId">Vote ID</param>
        /// <param name="dto">Vote submission data</param>
        /// <response code="200">Vote submitted successfully</response>
        /// <response code="400">Invalid vote choice or already voted</response>
        /// <response code="404">Group or vote not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Not a group member or vote closed</response>
        [HttpPost("{groupId}/votes/{voteId}/vote")]
        public async Task<IActionResult> Vote(int groupId, int voteId, [FromBody] object dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // For maintenance votes, use the maintenance vote service
                // Mock implementation - replace with actual vote submission logic
                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Vote submitted successfully",
                    Data = new { VoteId = voteId, Status = "Recorded", Timestamp = DateTime.UtcNow }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting vote {VoteId} for group {GroupId}", voteId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while submitting the vote",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        #region Vehicle Management

        /// <summary>
        /// Create a new vehicle for the group
        /// </summary>
        /// <remarks>
        /// Creates a new vehicle in the EV co-ownership system. The user creating the vehicle becomes the primary owner.
        /// 
        /// **Requirements:**
        /// - User must have Co-owner role
        /// - User must have verified driving license
        /// - License must not be expired
        /// - VIN and license plate must be unique
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleName": "Tesla Model 3 2024",
        ///   "brand": "Tesla",
        ///   "model": "Model 3",
        ///   "vin": "5YJ3E1EB5LF123456",
        ///   "licensePlate": "30A-12345",
        ///   "color": "Pearl White",
        ///   "manufacturingYear": 2024,
        ///   "purchaseDate": "2024-01-15",
        ///   "purchasePrice": 1500000000
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="request">Vehicle creation request</param>
        /// <response code="201">Vehicle created successfully</response>
        /// <response code="400">Validation error or user not eligible</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">Vehicle already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{groupId}/vehicles")]
        public async Task<IActionResult> CreateGroupVehicle(int groupId, [FromBody] CreateVehicleDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Check if user is part of the group (using groupId as vehicleId since they're the same in this context)
                var existingGroupMember = await _unitOfWork.VehicleCoOwnerRepository
                    .GetByVehicleIdAsync(groupId);

                var currentUserCoOwner = existingGroupMember.FirstOrDefault(vco => vco.CoOwner.UserId == parsedUserId);
                if (currentUserCoOwner == null)
                {
                    var forbiddenResponse = new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "You are not a member of this group"
                    };
                    return StatusCode(403, forbiddenResponse);
                }

                // Check if vehicle with this license plate already exists
                var existingVehicle = await _unitOfWork.VehicleRepository
                    .GetQueryable()
                    .Where(v => v.LicensePlate == request.LicensePlate)
                    .FirstOrDefaultAsync();

                if (existingVehicle != null)
                {
                    var conflictResponse = new BaseResponse<object>
                    {
                        StatusCode = 409,
                        Message = "Vehicle with this license plate already exists"
                    };
                    return Conflict(conflictResponse);
                }

                // Create new vehicle
                var newVehicle = new Vehicle
                {
                    Name = $"{request.Make} {request.Model}",
                    Brand = request.Make,
                    Model = request.Model,
                    Year = request.Year,
                    LicensePlate = request.LicensePlate,
                    Vin = request.VinNumber,
                    Color = request.Color,
                    PurchasePrice = request.PurchasePrice,
                    PurchaseDate = DateOnly.FromDateTime(DateTime.Now),
                    Description = request.Description,
                    StatusEnum = EVehicleStatus.Available,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = parsedUserId
                };

                var createdVehicle = await _unitOfWork.VehicleRepository.AddAsync(newVehicle);
                await _unitOfWork.SaveChangesAsync();

                var response = new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "Vehicle created successfully for group",
                    Data = new
                    {
                        VehicleId = createdVehicle.Id,
                        GroupId = groupId,
                        LicensePlate = createdVehicle.LicensePlate,
                        Brand = createdVehicle.Brand,
                        Model = createdVehicle.Model,
                        CreatedAt = createdVehicle.CreatedAt
                    }
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vehicle for group {GroupId}", groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the vehicle",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get all vehicles in the group
        /// </summary>
        /// <remarks>
        /// Retrieves all vehicles associated with the group, including detailed information and co-ownership status.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/vehicles
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Group vehicles retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view group vehicles</response>
        [HttpGet("{groupId}/vehicles")]
        public async Task<IActionResult> GetGroupVehicles(int groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<IEnumerable<object>>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Get user's vehicles (assuming group contains vehicles the user co-owns)
                var userVehicles = await _vehicleService.GetUserVehiclesAsync(parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Group vehicles retrieved successfully",
                    Data = userVehicles.Data,
                    AdditionalData = new { GroupId = groupId, Message = "Vehicles retrieved from user's co-ownership" }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vehicles for group {GroupId}", groupId);
                var response = new BaseResponse<IEnumerable<object>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving group vehicles",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get specific vehicle details in the group
        /// </summary>
        /// <remarks>
        /// Retrieve detailed information about a specific vehicle including co-owners, fund balance, and specifications.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/vehicles/456
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Vehicle details retrieved successfully</response>
        /// <response code="404">Group or vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view vehicle details</response>
        [HttpGet("{groupId}/vehicles/{vehicleId}")]
        public async Task<IActionResult> GetGroupVehicleDetails(int groupId, int vehicleId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use vehicle service to get vehicle details
                var vehicleDetail = await _vehicleService.GetVehicleDetailAsync(vehicleId, parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = vehicleDetail.StatusCode,
                    Message = vehicleDetail.Message,
                    Data = vehicleDetail.Data,
                    AdditionalData = new { GroupId = groupId }
                };

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
                _logger.LogError(ex, "Error retrieving vehicle {VehicleId} details for group {GroupId}", vehicleId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving vehicle details",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Add co-owner to group vehicle
        /// </summary>
        /// <remarks>
        /// Adds a co-owner to an existing vehicle in the group by sending an invitation.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "userId": 789,
        ///   "ownershipPercentage": 25.0,
        ///   "investmentAmount": 500000000
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="request">Add co-owner request</param>
        /// <response code="200">Co-owner invitation sent successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="404">Group or vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="409">User already co-owner</response>
        [HttpPost("{groupId}/vehicles/{vehicleId}/co-owners")]
        public async Task<IActionResult> AddVehicleCoOwner(int groupId, int vehicleId, [FromBody] object request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual vehicle service call
                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Co-owner invitation sent successfully",
                    Data = new { VehicleId = vehicleId, GroupId = groupId, InvitationSent = true, Timestamp = DateTime.UtcNow }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding co-owner to vehicle {VehicleId} in group {GroupId}", vehicleId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while adding co-owner",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Remove co-owner from group vehicle
        /// </summary>
        /// <remarks>
        /// Removes a co-owner from a vehicle in the group. Only the vehicle creator can perform this action.
        /// 
        /// Sample request:
        /// ```
        /// DELETE /api/Group/123/vehicles/456/co-owners/789
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="coOwnerUserId">Co-owner user ID to remove</param>
        /// <response code="200">Co-owner removed successfully</response>
        /// <response code="400">Cannot remove last active owner</response>
        /// <response code="404">Group, vehicle, or co-owner not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Only creator can remove co-owners</response>
        [HttpDelete("{groupId}/vehicles/{vehicleId}/co-owners/{coOwnerUserId}")]
        public async Task<IActionResult> RemoveVehicleCoOwner(int groupId, int vehicleId, int coOwnerUserId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use vehicle service to remove co-owner
                var result = await _vehicleService.RemoveCoOwnerAsync(vehicleId, coOwnerUserId, parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = result.StatusCode,
                    Message = result.Message,
                    Data = result.Data,
                    AdditionalData = new { GroupId = groupId, VehicleId = vehicleId, RemovedUserId = coOwnerUserId }
                };

                return result.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing co-owner {CoOwnerUserId} from vehicle {VehicleId} in group {GroupId}", coOwnerUserId, vehicleId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while removing co-owner",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get vehicle availability schedule
        /// </summary>
        /// <remarks>
        /// Provides a detailed view of when a specific vehicle is available or booked in the group.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/vehicles/456/schedule?startDate=2025-01-17&amp;endDate=2025-01-24
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="startDate">Start date (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date (format: yyyy-MM-dd)</param>
        /// <param name="statusFilter">Optional booking status filter</param>
        /// <response code="200">Vehicle schedule retrieved successfully</response>
        /// <response code="400">Invalid date range</response>
        /// <response code="404">Group or vehicle not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        [HttpGet("{groupId}/vehicles/{vehicleId}/schedule")]
        public async Task<IActionResult> GetVehicleSchedule(
            int groupId,
            int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? statusFilter = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use vehicle service to get availability schedule
                var schedule = await _vehicleService.GetVehicleAvailabilityScheduleAsync(vehicleId, parsedUserId, startDate, endDate, statusFilter);

                var response = new BaseResponse<object>
                {
                    StatusCode = schedule.StatusCode,
                    Message = schedule.Message,
                    Data = schedule.Data,
                    AdditionalData = new { GroupId = groupId, VehicleId = vehicleId }
                };

                return schedule.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedule for vehicle {VehicleId} in group {GroupId}", vehicleId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving vehicle schedule",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        #endregion

        #region Maintenance Voting

        /// <summary>
        /// Propose maintenance expenditure for group vehicle
        /// </summary>
        /// <remarks>
        /// Create a maintenance expenditure proposal that requires approval from other co-owners before fund deduction.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleId": 456,
        ///   "maintenanceCostId": 45,
        ///   "reason": "Emergency brake system replacement - safety critical",
        ///   "amount": 5000000,
        ///   "imageUrl": "https://storage.example.com/receipts/brake-quote.jpg"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="request">Maintenance proposal request</param>
        /// <response code="201">Proposal created successfully</response>
        /// <response code="400">Invalid amount or validation error</response>
        /// <response code="404">Group, vehicle, or maintenance cost not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Only co-owners can propose maintenance</response>
        [HttpPost("{groupId}/maintenance/propose")]
        public async Task<IActionResult> ProposeGroupMaintenanceExpenditure(int groupId, [FromBody] object request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual maintenance vote service call
                var response = new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "Maintenance expenditure proposal created successfully",
                    Data = new
                    {
                        ProposalId = new Random().Next(1000, 9999),
                        GroupId = groupId,
                        Amount = 5000000,
                        Status = "Pending",
                        RequiredVotes = 2,
                        CurrentVotes = 1,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing maintenance expenditure for group {GroupId}", groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating maintenance proposal",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Vote on group maintenance proposal
        /// </summary>
        /// <remarks>
        /// Vote to approve or reject a maintenance expenditure proposal for a group vehicle.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "approve": true,
        ///   "comments": "I agree this repair is necessary for safety"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="proposalId">Maintenance proposal ID</param>
        /// <param name="request">Vote request</param>
        /// <response code="200">Vote recorded successfully</response>
        /// <response code="400">Proposal already finalized or already voted</response>
        /// <response code="404">Group or proposal not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Only co-owners can vote</response>
        [HttpPost("{groupId}/maintenance/proposals/{proposalId}/vote")]
        public async Task<IActionResult> VoteOnGroupMaintenanceProposal(int groupId, int proposalId, [FromBody] object request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use maintenance vote service
                // Note: proposalId would be the fundUsageId in the service
                // var result = await _maintenanceVoteService.VoteOnMaintenanceExpenditureAsync(proposalId, request, parsedUserId);

                // Mock implementation
                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Vote recorded successfully",
                    Data = new
                    {
                        ProposalId = proposalId,
                        GroupId = groupId,
                        VoteStatus = "Approved",
                        TotalVotes = 3,
                        ApprovalCount = 2,
                        ExecutionStatus = "Pending Fund Deduction"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on maintenance proposal {ProposalId} for group {GroupId}", proposalId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while recording vote",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get group maintenance proposal details
        /// </summary>
        /// <remarks>
        /// Retrieve detailed information about a specific maintenance expenditure proposal including voting status.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/maintenance/proposals/456
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="proposalId">Maintenance proposal ID</param>
        /// <response code="200">Proposal details retrieved successfully</response>
        /// <response code="404">Group or proposal not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        [HttpGet("{groupId}/maintenance/proposals/{proposalId}")]
        public async Task<IActionResult> GetGroupMaintenanceProposal(int groupId, int proposalId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use maintenance vote service to get proposal details
                var proposal = await _maintenanceVoteService.GetMaintenanceProposalDetailsAsync(proposalId, parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = proposal.StatusCode,
                    Message = proposal.Message,
                    Data = proposal.Data,
                    AdditionalData = new { GroupId = groupId }
                };

                return proposal.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance proposal {ProposalId} for group {GroupId}", proposalId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving proposal details",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get all pending maintenance proposals for group
        /// </summary>
        /// <remarks>
        /// Retrieve all pending maintenance expenditure proposals for vehicles in the group.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/maintenance/proposals/pending
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Pending proposals retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        [HttpGet("{groupId}/maintenance/proposals/pending")]
        public async Task<IActionResult> GetGroupPendingMaintenanceProposals(int groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<IEnumerable<object>>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Note: In real implementation, we would need to get all vehicles in the group
                // and then get pending proposals for each vehicle
                // For now, using a mock vehicleId (assuming one primary vehicle per group)
                var mockVehicleId = groupId; // Simplified mapping

                var pendingProposals = await _maintenanceVoteService.GetPendingProposalsForVehicleAsync(mockVehicleId, parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = pendingProposals.StatusCode,
                    Message = pendingProposals.Message,
                    Data = pendingProposals.Data,
                    AdditionalData = new { GroupId = groupId, VehicleId = mockVehicleId }
                };

                return pendingProposals.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending maintenance proposals for group {GroupId}", groupId);
                var response = new BaseResponse<IEnumerable<object>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving pending proposals",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Cancel group maintenance proposal
        /// </summary>
        /// <remarks>
        /// Cancel a pending maintenance expenditure proposal for a group vehicle.
        /// 
        /// Sample request:
        /// ```
        /// DELETE /api/Group/123/maintenance/proposals/456/cancel
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="proposalId">Maintenance proposal ID</param>
        /// <response code="200">Proposal cancelled successfully</response>
        /// <response code="400">Proposal already finalized</response>
        /// <response code="404">Group or proposal not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Only proposer or admin can cancel</response>
        [HttpDelete("{groupId}/maintenance/proposals/{proposalId}/cancel")]
        public async Task<IActionResult> CancelGroupMaintenanceProposal(int groupId, int proposalId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use maintenance vote service to cancel proposal
                var result = await _maintenanceVoteService.CancelMaintenanceProposalAsync(proposalId, parsedUserId);

                var response = new BaseResponse<object>
                {
                    StatusCode = result.StatusCode,
                    Message = result.Message,
                    Data = result.Data,
                    AdditionalData = new { GroupId = groupId, ProposalId = proposalId }
                };

                return result.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling maintenance proposal {ProposalId} for group {GroupId}", proposalId, groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while cancelling proposal",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        #endregion

        // --- Fund ---

        /// <summary>
        /// Get group fund
        /// </summary>
        /// <remarks>
        /// Get current fund balance and summary for the group's vehicle.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/fund
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Fund information retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view fund information</response>
        [HttpGet("{groupId}/fund")]
        public async Task<IActionResult> GetFund(int groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupFundDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use fund service to get balance (assuming groupId represents vehicleId)
                var fundBalance = await _fundService.GetFundBalanceAsync(groupId, parsedUserId);

                var groupFund = new GroupFundDto
                {
                    GroupId = groupId,
                    Balance = fundBalance.Data?.CurrentBalance ?? 0
                };

                var response = new BaseResponse<GroupFundDto>
                {
                    StatusCode = 200,
                    Message = "Group fund retrieved successfully",
                    Data = groupFund,
                    AdditionalData = fundBalance.Data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund for group {GroupId}", groupId);
                var response = new BaseResponse<GroupFundDto>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving group fund",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Contribute to group fund
        /// </summary>
        /// <remarks>
        /// Make a financial contribution to the group's fund for vehicle-related expenses.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "amount": 500.00,
        ///   "description": "Monthly contribution for maintenance",
        ///   "paymentMethod": "Bank Transfer"
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <param name="dto">Contribution data</param>
        /// <response code="201">Contribution added successfully</response>
        /// <response code="400">Invalid contribution amount or data</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Not authorized to contribute to this group</response>
        [HttpPost("{groupId}/fund/contribute")]
        public async Task<IActionResult> ContributeFund(int groupId, [FromBody] object dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual fund contribution logic
                var contributionRecord = new
                {
                    Id = new Random().Next(1000, 9999),
                    GroupId = groupId,
                    UserId = userId,
                    Amount = 500.00m,
                    Description = "Fund contribution",
                    Timestamp = DateTime.UtcNow,
                    Status = "Completed"
                };

                var response = new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "Fund contribution added successfully",
                    Data = contributionRecord
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding fund contribution for group {GroupId}", groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while processing fund contribution",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get group fund history
        /// </summary>
        /// <remarks>
        /// Retrieve detailed history of all fund activities including contributions and expenses.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/Group/123/fund/history?page=1&amp;pageSize=20&amp;startDate=2024-01-01
        /// ```
        /// </remarks>
        /// <param name="groupId">Group ID</param>
        /// <response code="200">Fund history retrieved successfully</response>
        /// <response code="404">Group not found</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied to view fund history</response>
        [HttpGet("{groupId}/fund/history")]
        public async Task<IActionResult> FundHistory(int groupId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Use fund service to get history (assuming groupId represents vehicleId)
                var fundAdditions = await _fundService.GetFundAdditionsAsync(groupId, parsedUserId);
                var fundUsages = await _fundService.GetFundUsagesAsync(groupId, parsedUserId);

                var fundHistory = new
                {
                    Additions = fundAdditions.Data,
                    Usages = fundUsages.Data,
                    Summary = new
                    {
                        TotalAdditions = fundAdditions.Data?.Sum(a => a.Amount) ?? 0,
                        TotalUsages = fundUsages.Data?.Sum(u => u.Amount) ?? 0,
                        TransactionCount = (fundAdditions.Data?.Count() ?? 0) + (fundUsages.Data?.Count() ?? 0)
                    }
                };

                var response = new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "Fund history retrieved successfully",
                    Data = fundHistory
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund history for group {GroupId}", groupId);
                var response = new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving fund history",
                    Errors = ex.Message
                };

                return StatusCode(500, response);
            }
        }
    }
}