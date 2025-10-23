using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.OwnershipDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing vehicle ownership percentage changes with group consensus
    /// </summary>
    [Route("api/ownership-change")]
    [ApiController]
    [AuthorizeRoles] // Requires authentication
    public class OwnershipChangeController : ControllerBase
    {
        private readonly IOwnershipChangeService _ownershipChangeService;

        public OwnershipChangeController(IOwnershipChangeService ownershipChangeService)
        {
            _ownershipChangeService = ownershipChangeService;
        }

        /// <summary>
        /// Proposes a change to vehicle ownership percentages (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="request">Ownership change proposal details</param>
        /// <response code="201">Ownership change request created successfully. Message: OWNERSHIP_CHANGE_REQUEST_CREATED_SUCCESSFULLY</response>
        /// <response code="400">Bad request. Possible messages:  
        /// - INVALID_CO_OWNER_IDS_IN_PROPOSED_CHANGES  
        /// - ALL_CO_OWNERS_MUST_BE_INCLUDED_IN_OWNERSHIP_CHANGE  
        /// - TOTAL_PROPOSED_OWNERSHIP_MUST_EQUAL_100_PERCENT  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ONLY_CO_OWNERS_CAN_PROPOSE_OWNERSHIP_CHANGES  
        /// </response>
        /// <response code="404">Vehicle not found. Message: VEHICLE_NOT_FOUND</response>
        /// <response code="409">Conflict. Message: VEHICLE_HAS_PENDING_OWNERSHIP_CHANGE_REQUEST</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "vehicleId": 5,
        ///   "reason": "Adjusting ownership after new co-owner investment. User A increases stake, User B decreases proportionally.",
        ///   "proposedChanges": [
        ///     {
        ///       "coOwnerId": 10,
        ///       "proposedPercentage": 60.0,
        ///       "proposedInvestment": 600000000
        ///     },
        ///     {
        ///       "coOwnerId": 11,
        ///       "proposedPercentage": 40.0,
        ///       "proposedInvestment": 400000000
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **Business Rules:**
        /// - Only co-owners of the vehicle can propose ownership changes
        /// - All current co-owners must be included in the proposal
        /// - Total proposed ownership must equal exactly 100%
        /// - Only one pending request allowed per vehicle at a time
        /// - All co-owners must approve before changes are applied
        /// - Notifications sent to all co-owners (except proposer)
        /// </remarks>
        [HttpPost("propose")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> ProposeOwnershipChange([FromBody] ProposeOwnershipChangeRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.ProposeOwnershipChangeAsync(request, userId);
            return response.StatusCode switch
            {
                201 => Created($"/api/ownership-change/{response.Data?.Id}", response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets details of a specific ownership change request (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="requestId">Ownership change request ID</param>
        /// <response code="200">Ownership change request retrieved successfully. Message: OWNERSHIP_CHANGE_REQUEST_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Message: NOT_AUTHORIZED_TO_VIEW_THIS_REQUEST</response>
        /// <response code="404">Request not found. Message: OWNERSHIP_CHANGE_REQUEST_NOT_FOUND</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Returns detailed information about an ownership change request including:
        /// - Proposed changes for each co-owner
        /// - Approval status from each co-owner
        /// - Current approval progress
        /// 
        /// Only co-owners involved in the request can view it.
        /// </remarks>
        [HttpGet("{requestId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetOwnershipChangeRequest(int requestId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.GetOwnershipChangeRequestAsync(requestId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets all ownership change requests for a specific vehicle (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="includeCompleted">Include completed requests (approved/rejected/cancelled)</param>
        /// <response code="200">Ownership change requests retrieved. Message: FOUND_X_OWNERSHIP_CHANGE_REQUESTS</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Message: NOT_AUTHORIZED_TO_VIEW_VEHICLE_REQUESTS</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Returns all ownership change requests for a vehicle.
        /// By default, only pending requests are returned.
        /// Set `includeCompleted=true` to see historical requests.
        /// 
        /// Only co-owners of the vehicle can view its requests.
        /// </remarks>
        [HttpGet("vehicle/{vehicleId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetVehicleOwnershipChangeRequests(
            int vehicleId,
            [FromQuery] bool includeCompleted = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.GetVehicleOwnershipChangeRequestsAsync(
                vehicleId, userId, includeCompleted);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets all pending ownership change requests requiring approval from current user (CoOwner/Staff/Admin)
        /// </summary>
        /// <response code="200">Pending approvals retrieved. Message: FOUND_X_PENDING_APPROVALS</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Returns all ownership change requests that are:
        /// - Currently pending (not yet approved/rejected)
        /// - Awaiting the current user's approval
        /// 
        /// Use this endpoint to show users what decisions they need to make.
        /// </remarks>
        [HttpGet("pending-approvals")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.GetPendingApprovalsAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Approves or rejects an ownership change request (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="requestId">Ownership change request ID</param>
        /// <param name="request">Approval decision and optional comments</param>
        /// <response code="200">Decision recorded. Possible messages:  
        /// - APPROVAL_RECORDED_WAITING_FOR_OTHER_CO_OWNERS  
        /// - OWNERSHIP_CHANGE_APPROVED_AND_APPLIED  
        /// - OWNERSHIP_CHANGE_REQUEST_REJECTED  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - REQUEST_ALREADY_APPROVED  
        /// - REQUEST_ALREADY_REJECTED  
        /// - ALREADY_APPROVED  
        /// - ALREADY_REJECTED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Message: NOT_AUTHORIZED_TO_APPROVE_THIS_REQUEST</response>
        /// <response code="404">Request not found. Message: OWNERSHIP_CHANGE_REQUEST_NOT_FOUND</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// **Sample Request (Approve):**
        /// ```json
        /// {
        ///   "approve": true,
        ///   "comments": "I agree with this ownership adjustment"
        /// }
        /// ```
        /// 
        /// **Sample Request (Reject):**
        /// ```json
        /// {
        ///   "approve": false,
        ///   "comments": "I disagree with the proposed percentage split"
        /// }
        /// ```
        /// 
        /// **Group Consensus Logic:**
        /// - **If Approved:** Approval count increments. When ALL co-owners approve, changes are automatically applied.
        /// - **If Rejected:** Request is immediately marked as rejected. Changes are NOT applied.
        /// - All co-owners are notified of the decision
        /// - Only co-owners involved in the request can approve/reject
        /// - Each co-owner can only respond once
        /// </remarks>
        [HttpPost("{requestId:int}/respond")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> ApproveOrRejectOwnershipChange(
            int requestId,
            [FromBody] ApproveOwnershipChangeRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.ApproveOrRejectOwnershipChangeAsync(
                requestId, request, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Cancels a pending ownership change request (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="requestId">Ownership change request ID to cancel</param>
        /// <response code="200">Request cancelled successfully. Message: OWNERSHIP_CHANGE_REQUEST_CANCELLED</response>
        /// <response code="400">Bad request. Message: CANNOT_CANCEL_REQUEST_WITH_STATUS_X</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied. Message: ONLY_PROPOSER_CAN_CANCEL_REQUEST</response>
        /// <response code="404">Request not found. Message: OWNERSHIP_CHANGE_REQUEST_NOT_FOUND</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Allows the proposer to cancel a pending ownership change request.
        /// 
        /// **Restrictions:**
        /// - Only the original proposer can cancel
        /// - Can only cancel requests with "Pending" status
        /// - Cannot cancel approved/rejected/completed requests
        /// </remarks>
        [HttpDelete("{requestId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> CancelOwnershipChangeRequest(int requestId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.CancelOwnershipChangeRequestAsync(requestId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets ownership change statistics (Admin/Staff only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully. Message: OWNERSHIP_CHANGE_STATISTICS_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - Admin/Staff role required</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Returns comprehensive statistics about ownership change requests:
        /// - Total requests by status (pending/approved/rejected/cancelled/expired)
        /// - Average approval time (in hours)
        /// - Last request created timestamp
        /// 
        /// Admin/Staff only endpoint for system monitoring.
        /// </remarks>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetOwnershipChangeStatistics()
        {
            var response = await _ownershipChangeService.GetOwnershipChangeStatisticsAsync();
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets all ownership change requests for current user (as proposer or approver) (CoOwner/Staff/Admin)
        /// </summary>
        /// <param name="includeCompleted">Include completed requests</param>
        /// <response code="200">Ownership change requests retrieved. Message: FOUND_X_OWNERSHIP_CHANGE_REQUESTS</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error. Message: INTERNAL_SERVER_ERROR</response>
        /// <remarks>
        /// Returns all ownership change requests where the current user is:
        /// - The proposer of the request, OR
        /// - One of the co-owners who needs to approve
        /// 
        /// By default, only pending requests are shown.
        /// Set `includeCompleted=true` to see historical requests.
        /// </remarks>
        [HttpGet("my-requests")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetMyOwnershipChangeRequests([FromQuery] bool includeCompleted = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _ownershipChangeService.GetUserOwnershipChangeRequestsAsync(userId, includeCompleted);
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
