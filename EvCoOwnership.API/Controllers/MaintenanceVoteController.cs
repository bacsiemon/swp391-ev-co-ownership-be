using EvCoOwnership.Repositories.DTOs.MaintenanceVoteDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing maintenance expenditure voting system
    /// </summary>
    [ApiController]
    [Route("api/maintenance-vote")]
    [Authorize]
    public class MaintenanceVoteController : ControllerBase
    {
        private readonly IMaintenanceVoteService _maintenanceVoteService;
        private readonly ILogger<MaintenanceVoteController> _logger;

        public MaintenanceVoteController(
            IMaintenanceVoteService maintenanceVoteService,
            ILogger<MaintenanceVoteController> logger)
        {
            _maintenanceVoteService = maintenanceVoteService;
            _logger = logger;
        }

        /// <summary>
        /// Proposes a maintenance expenditure that requires co-owner voting approval
        /// </summary>
        /// <param name="request">Proposal details</param>
        /// <response code="201">Proposal created successfully. Possible messages:  
        /// - MAINTENANCE_EXPENDITURE_PROPOSAL_CREATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - INVALID_AMOUNT  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ONLY_CO_OWNERS_CAN_PROPOSE_MAINTENANCE_EXPENDITURE  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - MAINTENANCE_COST_NOT_FOUND  
        /// - FUND_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **PROPOSE MAINTENANCE EXPENDITURE FOR VOTING**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle only
        /// 
        /// **Purpose:**
        /// Create a maintenance expenditure proposal that requires approval from other co-owners before fund deduction.
        /// 
        /// **Voting Logic:**
        /// - Proposer automatically approves (counted as 1 vote)
        /// - Other co-owners can vote approve/reject
        /// - Requires majority approval (> 50% of co-owners)
        /// - If approved: Amount deducted from fund automatically
        /// - If rejected by anyone: Proposal immediately rejected
        /// 
        /// **Use Cases:**
        /// - Large maintenance expenses requiring consensus
        /// - Major repairs or upgrades
        /// - Emergency maintenance over budget threshold
        /// 
        /// **Sample Request:**  
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "maintenanceCostId": 45,
        ///   "reason": "Emergency brake system replacement - safety critical",
        ///   "amount": 5000000,
        ///   "imageUrl": "https://storage.example.com/receipts/brake-quote.jpg"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("propose")]
        public async Task<IActionResult> ProposeMaintenanceExpenditure([FromBody] ProposeMaintenanceExpenditureRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.ProposeMaintenanceExpenditureAsync(request, userId);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(GetProposalDetails), new { fundUsageId = response.Data?.FundUsageId }, response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProposeMaintenanceExpenditure");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Votes (approve/reject) on a proposed maintenance expenditure
        /// </summary>
        /// <param name="fundUsageId">ID of the fund usage proposal</param>
        /// <param name="request">Vote decision</param>
        /// <response code="200">Vote recorded successfully. Possible messages:  
        /// - VOTE_RECORDED_WAITING_FOR_MORE_APPROVALS  
        /// - PROPOSAL_APPROVED_AND_EXECUTED  
        /// - VOTE_RECORDED_PROPOSAL_REJECTED  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - PROPOSAL_ALREADY_FINALIZED  
        /// - ALREADY_VOTED  
        /// - PROPOSAL_APPROVED_BUT_INSUFFICIENT_FUND_BALANCE  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ONLY_CO_OWNERS_CAN_VOTE  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - MAINTENANCE_PROPOSAL_NOT_FOUND  
        /// - FUND_NOT_FOUND  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **VOTE ON MAINTENANCE EXPENDITURE PROPOSAL**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle only
        /// 
        /// **Voting Rules:**
        /// - Each co-owner can vote once
        /// - Vote can be approve (true) or reject (false)
        /// - Rejection by any co-owner → Proposal immediately rejected
        /// - Approval by majority (> 50%) → Proposal automatically executed
        /// 
        /// **Execution on Approval:**
        /// - Amount deducted from vehicle fund
        /// - FundUsage record updated with actual amount
        /// - Description marked as [APPROVED]
        /// - Fund balance updated
        /// 
        /// **Sample Request (Approve):**  
        /// ```json
        /// {
        ///   "approve": true,
        ///   "comments": "I agree this repair is necessary for safety"
        /// }
        /// ```
        /// 
        /// **Sample Request (Reject):**  
        /// ```json
        /// {
        ///   "approve": false,
        ///   "comments": "Too expensive, please get a second quote"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{fundUsageId}/vote")]
        public async Task<IActionResult> VoteOnProposal(int fundUsageId, [FromBody] VoteMaintenanceExpenditureRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.VoteOnMaintenanceExpenditureAsync(fundUsageId, request, userId);

                return response.StatusCode switch
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
                _logger.LogError(ex, "Error in VoteOnProposal for fundUsageId {FundUsageId}", fundUsageId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets details of a specific maintenance expenditure proposal
        /// </summary>
        /// <param name="fundUsageId">ID of the fund usage proposal</param>
        /// <response code="200">Proposal details retrieved successfully. Possible messages:  
        /// - PROPOSAL_DETAILS_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - MAINTENANCE_PROPOSAL_NOT_FOUND  
        /// - FUND_NOT_FOUND  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **GET PROPOSAL DETAILS**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Provides:**
        /// - Proposal information (amount, reason, maintenance details)
        /// - Voting statistics (approvals, rejections, percentage)
        /// - Individual vote details for all co-owners
        /// - Voting status (Pending/Approved/Rejected/Cancelled)
        /// - Proposer information
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "PROPOSAL_DETAILS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "fundUsageId": 123,
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "maintenanceCostId": 45,
        ///     "maintenanceDescription": "Brake pad replacement",
        ///     "maintenanceType": "Repair",
        ///     "amount": 5000000,
        ///     "reason": "Emergency brake system replacement",
        ///     "proposedByUserName": "John Doe",
        ///     "proposedAt": "2024-10-23T10:00:00Z",
        ///     "totalCoOwners": 3,
        ///     "requiredApprovals": 2,
        ///     "currentApprovals": 1,
        ///     "currentRejections": 0,
        ///     "approvalPercentage": 33.33,
        ///     "votingStatus": "Pending",
        ///     "votes": [
        ///       {
        ///         "userId": 10,
        ///         "userName": "John Doe",
        ///         "hasVoted": true,
        ///         "isAgree": true,
        ///         "votedAt": "2024-10-23T10:00:00Z"
        ///       },
        ///       {
        ///         "userId": 11,
        ///         "userName": "Jane Smith",
        ///         "hasVoted": false
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpGet("{fundUsageId}")]
        public async Task<IActionResult> GetProposalDetails(int fundUsageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.GetMaintenanceProposalDetailsAsync(fundUsageId, userId);

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
                _logger.LogError(ex, "Error in GetProposalDetails for fundUsageId {FundUsageId}", fundUsageId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets all pending maintenance expenditure proposals for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <response code="200">Pending proposals retrieved successfully. Possible messages:  
        /// - PENDING_PROPOSALS_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **GET ALL PENDING PROPOSALS FOR VEHICLE**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Provides:**
        /// - List of all proposals awaiting votes
        /// - Total pending proposals count
        /// - Total pending amount (sum of all proposals)
        /// - Full details for each proposal including voting status
        /// 
        /// **Use Cases:**
        /// - Dashboard view of pending actions
        /// - Notification reminders for co-owners
        /// - Financial planning and forecasting
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/maintenance-vote/vehicle/1/pending
        /// ```
        /// </remarks>
        [HttpGet("vehicle/{vehicleId}/pending")]
        public async Task<IActionResult> GetPendingProposals(int vehicleId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.GetPendingProposalsForVehicleAsync(vehicleId, userId);

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
                _logger.LogError(ex, "Error in GetPendingProposals for vehicleId {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets voting history for the authenticated user
        /// </summary>
        /// <response code="200">Voting history retrieved successfully. Possible messages:  
        /// - VOTING_HISTORY_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **GET USER VOTING HISTORY**
        /// 
        /// **Access Control:**
        /// - Authenticated users only
        /// 
        /// **Provides:**
        /// - Total votes cast
        /// - Number of approvals given
        /// - Number of rejections given
        /// - Pending votes count
        /// - Complete voting history with proposal details
        /// 
        /// **Use Cases:**
        /// - User profile/dashboard
        /// - Voting participation tracking
        /// - Personal voting analytics
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/maintenance-vote/my-voting-history
        /// ```
        /// </remarks>
        [HttpGet("my-voting-history")]
        public async Task<IActionResult> GetMyVotingHistory()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.GetUserVotingHistoryAsync(userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyVotingHistory");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Cancels a pending maintenance expenditure proposal
        /// </summary>
        /// <param name="fundUsageId">ID of the fund usage proposal</param>
        /// <response code="200">Proposal cancelled successfully. Possible messages:  
        /// - PROPOSAL_CANCELLED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - PROPOSAL_ALREADY_FINALIZED_CANNOT_CANCEL  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ONLY_PROPOSER_OR_ADMIN_CAN_CANCEL  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - MAINTENANCE_PROPOSAL_NOT_FOUND  
        /// - PROPOSAL_DATA_INCOMPLETE  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **CANCEL MAINTENANCE EXPENDITURE PROPOSAL**
        /// 
        /// **Access Control:**
        /// - Original proposer only
        /// - Admin/Staff
        /// 
        /// **Restrictions:**
        /// - Can only cancel proposals still in "Pending" status
        /// - Cannot cancel approved or rejected proposals
        /// 
        /// **Effects:**
        /// - Proposal marked as [CANCELLED]
        /// - No fund deduction occurs
        /// - All votes preserved for audit trail
        /// 
        /// **Use Cases:**
        /// - Proposer changes mind
        /// - Found cheaper alternative
        /// - Maintenance no longer needed
        /// - Admin intervention
        /// 
        /// **Sample Request:**  
        /// ```
        /// DELETE /api/maintenance-vote/123/cancel
        /// ```
        /// </remarks>
        [HttpDelete("{fundUsageId}/cancel")]
        public async Task<IActionResult> CancelProposal(int fundUsageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _maintenanceVoteService.CancelMaintenanceProposalAsync(fundUsageId, userId);

                return response.StatusCode switch
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
                _logger.LogError(ex, "Error in CancelProposal for fundUsageId {FundUsageId}", fundUsageId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }
    }
}
