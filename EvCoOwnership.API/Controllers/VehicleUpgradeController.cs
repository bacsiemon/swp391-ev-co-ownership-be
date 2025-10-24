using EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    [Route("api/upgrade-vote")]
    [ApiController]
    [Authorize]
    public class VehicleUpgradeController : ControllerBase
    {
        private readonly IVehicleUpgradeVoteService _upgradeVoteService;
        private readonly ILogger<VehicleUpgradeController> _logger;

        public VehicleUpgradeController(IVehicleUpgradeVoteService upgradeVoteService, ILogger<VehicleUpgradeController> logger)
        {
            _upgradeVoteService = upgradeVoteService;
            _logger = logger;
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Propose a new vehicle upgrade**
        /// 
        /// Sample request:
        /// ```json
        /// POST /api/upgrade-vote/propose
        /// {
        ///   "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "upgradeType": 0,
        ///   "title": "Battery Upgrade to 100kWh",
        ///   "description": "Upgrade to new generation battery for extended range",
        ///   "estimatedCost": 15000.00,
        ///   "justification": "Current battery capacity degraded by 20%, affecting daily operations",
        ///   "imageUrl": "https://example.com/battery-specs.jpg",
        ///   "vendorName": "Tesla Battery Solutions",
        ///   "vendorContact": "+1-555-0123",
        ///   "proposedInstallationDate": "2024-12-01T00:00:00Z",
        ///   "estimatedDurationDays": 3
        /// }
        /// ```
        /// 
        /// **Upgrade Types:**
        /// - 0: BatteryUpgrade
        /// - 1: InsurancePackage
        /// - 2: TechnologyUpgrade
        /// - 3: InteriorUpgrade
        /// - 4: PerformanceUpgrade
        /// - 5: SafetyUpgrade
        /// - 6: Other
        /// </remarks>
        /// <response code="201">Proposal created successfully - includes auto-approval from proposer</response>
        /// <response code="400">Invalid request data (e.g., negative cost)</response>
        /// <response code="403">User is not a co-owner of the vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("propose")]
        public async Task<IActionResult> ProposeUpgrade([FromBody] ProposeVehicleUpgradeRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.ProposeVehicleUpgradeAsync(request, userId);
            return response.StatusCode switch
            {
                201 => Created($"/api/upgrade-vote/{response.Data?.ProposalId}", response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Vote on a vehicle upgrade proposal**
        /// 
        /// Sample request:
        /// ```json
        /// POST /api/upgrade-vote/{proposalId}/vote
        /// {
        ///   "isApprove": true,
        ///   "comments": "Great idea! This will significantly improve our vehicle's performance"
        /// }
        /// ```
        /// 
        /// **Voting Rules:**
        /// - Each co-owner can vote once per proposal
        /// - If ANY co-owner rejects, proposal is instantly rejected
        /// - If > 50% of co-owners approve, proposal is approved
        /// - Proposer is auto-approved when creating proposal
        /// </remarks>
        /// <response code="200">Vote recorded successfully</response>
        /// <response code="400">Already voted, or proposal status doesn't allow voting</response>
        /// <response code="403">User is not a co-owner of the vehicle</response>
        /// <response code="404">Proposal not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{proposalId}/vote")]
        public async Task<IActionResult> VoteOnUpgrade(Guid proposalId, [FromBody] VoteVehicleUpgradeRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.VoteOnUpgradeAsync(proposalId, request, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Get detailed information about a specific upgrade proposal**
        /// 
        /// Sample request:
        /// ```
        /// GET /api/upgrade-vote/{proposalId}
        /// ```
        /// 
        /// Response includes:
        /// - Full proposal details (upgrade type, costs, vendor info, dates)
        /// - Voting statistics (total votes, approvals, rejections)
        /// - Individual vote details with comments
        /// - Execution status and actual costs (if executed)
        /// </remarks>
        /// <response code="200">Proposal details retrieved successfully</response>
        /// <response code="403">User is not a co-owner of the vehicle</response>
        /// <response code="404">Proposal not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{proposalId}")]
        public async Task<IActionResult> GetProposalDetails(Guid proposalId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.GetUpgradeProposalDetailsAsync(proposalId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Get all pending upgrade proposals for a vehicle**
        /// 
        /// Sample request:
        /// ```
        /// GET /api/upgrade-vote/vehicle/{vehicleId}/pending
        /// ```
        /// 
        /// Returns summary including:
        /// - List of all pending proposals
        /// - Total estimated cost across all pending upgrades
        /// - Voting progress for each proposal
        /// </remarks>
        /// <response code="200">Pending proposals retrieved successfully</response>
        /// <response code="403">User is not a co-owner of the vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/pending")]
        public async Task<IActionResult> GetPendingUpgrades(Guid vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.GetPendingUpgradesForVehicleAsync(vehicleId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User (Admin or Proposer)
        /// </summary>
        /// <remarks>
        /// **Mark an approved upgrade proposal as executed**
        /// 
        /// Sample request:
        /// ```json
        /// POST /api/upgrade-vote/{proposalId}/execute
        /// {
        ///   "actualCost": 14500.00,
        ///   "executionNotes": "Installation completed successfully. Battery performing above expected specifications.",
        ///   "invoiceImageUrl": "https://example.com/invoice-12345.pdf"
        /// }
        /// ```
        /// 
        /// **Process:**
        /// 1. Validates proposal is approved (not pending/rejected/cancelled)
        /// 2. Checks fund has sufficient balance
        /// 3. Creates fund usage record
        /// 4. Deducts actual cost from vehicle fund
        /// 5. Marks proposal as executed with timestamp
        /// 
        /// **Permissions:** Only admin or original proposer can execute
        /// </remarks>
        /// <response code="200">Upgrade marked as executed, fund deducted</response>
        /// <response code="400">Proposal not approved, already executed, or insufficient funds</response>
        /// <response code="403">User is not admin or proposer</response>
        /// <response code="404">Proposal or fund not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{proposalId}/execute")]
        public async Task<IActionResult> MarkAsExecuted(Guid proposalId, [FromBody] MarkUpgradeExecutedRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.MarkUpgradeAsExecutedAsync(proposalId, request, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User (Admin or Proposer)
        /// </summary>
        /// <remarks>
        /// **Cancel an upgrade proposal**
        /// 
        /// Sample request:
        /// ```
        /// DELETE /api/upgrade-vote/{proposalId}/cancel
        /// ```
        /// 
        /// **Cancellation Rules:**
        /// - Can only cancel Pending or Approved proposals
        /// - Cannot cancel executed proposals
        /// - No fund refund (funds are only deducted on execution)
        /// 
        /// **Permissions:** Only admin or original proposer can cancel
        /// </remarks>
        /// <response code="200">Proposal cancelled successfully</response>
        /// <response code="400">Proposal already executed or in non-cancellable status</response>
        /// <response code="403">User is not admin or proposer</response>
        /// <response code="404">Proposal not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{proposalId}/cancel")]
        public async Task<IActionResult> CancelProposal(Guid proposalId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.CancelUpgradeProposalAsync(proposalId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Get the user's upgrade voting history**
        /// 
        /// Sample request:
        /// ```
        /// GET /api/upgrade-vote/my-history
        /// ```
        /// 
        /// Returns all proposals the user has voted on, including:
        /// - Proposal details (type, title, cost)
        /// - User's vote (Approve/Reject) and comments
        /// - Current proposal status
        /// - Execution status
        /// </remarks>
        /// <response code="200">Voting history retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyVotingHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.GetUserUpgradeVotingHistoryAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// User
        /// </summary>
        /// <remarks>
        /// **Get upgrade statistics for a vehicle**
        /// 
        /// Sample request:
        /// ```
        /// GET /api/upgrade-vote/vehicle/{vehicleId}/statistics
        /// ```
        /// 
        /// Returns comprehensive statistics:
        /// - Total proposals (by status: pending, approved, rejected, cancelled)
        /// - Executed upgrades count
        /// - Total costs (estimated vs actual)
        /// - Breakdown by upgrade type (battery, insurance, technology, etc.)
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="403">User is not a co-owner of the vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/statistics")]
        public async Task<IActionResult> GetVehicleStatistics(Guid vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _upgradeVoteService.GetVehicleUpgradeStatisticsAsync(vehicleId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
