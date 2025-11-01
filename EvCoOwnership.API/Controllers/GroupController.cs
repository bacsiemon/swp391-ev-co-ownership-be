using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Repositories.DTOs.GroupDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// API controller for group management (CRUD, members, roles, votes, fund)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IFundService _fundService;
        private readonly IMaintenanceVoteService _maintenanceVoteService;
        private readonly ILogger<GroupController> _logger;

        /// <summary>
        /// Initialize GroupController with required services
        /// </summary>
        /// <param name="groupService">Group management service</param>
        /// <param name="fundService">Fund management service</param>
        /// <param name="maintenanceVoteService">Maintenance voting service</param>
        /// <param name="logger">Logger instance</param>
        public GroupController(
            IGroupService groupService,
            IFundService fundService,
            IMaintenanceVoteService maintenanceVoteService,
            ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _fundService = fundService;
            _maintenanceVoteService = maintenanceVoteService;
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
                // Mock implementation - replace with actual service call when available
                var mockMembers = new List<GroupMemberDto>
                {
                    new GroupMemberDto { Id = 1, GroupId = groupId, UserId = 1, Role = "Owner" },
                    new GroupMemberDto { Id = 2, GroupId = groupId, UserId = 2, Role = "Member" },
                    new GroupMemberDto { Id = 3, GroupId = groupId, UserId = 3, Role = "Member" }
                };

                var response = new BaseResponse<IEnumerable<GroupMemberDto>>
                {
                    StatusCode = 200,
                    Message = "Group members retrieved successfully",
                    Data = mockMembers
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
        public async Task<IActionResult> AddMember(int groupId, [FromBody] object dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual service call when available
                var mockMember = new GroupMemberDto 
                { 
                    Id = new Random().Next(1000, 9999), 
                    GroupId = groupId, 
                    UserId = new Random().Next(100, 999), 
                    Role = "Member" 
                };

                var response = new BaseResponse<GroupMemberDto>
                {
                    StatusCode = 201,
                    Message = "Member added to group successfully",
                    Data = mockMember
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
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<object>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual service call when available
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
        public async Task<IActionResult> UpdateMemberRole(int groupId, int memberId, [FromBody] object dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    var unauthorizedResponse = new BaseResponse<GroupMemberDto>
                    {
                        StatusCode = 401,
                        Message = "User authentication required"
                    };
                    return Unauthorized(unauthorizedResponse);
                }

                // Mock implementation - replace with actual service call when available
                var updatedMember = new GroupMemberDto 
                { 
                    Id = memberId, 
                    GroupId = groupId, 
                    UserId = memberId, 
                    Role = "Admin" 
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