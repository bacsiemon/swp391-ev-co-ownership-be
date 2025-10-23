using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.DisputeDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing disputes (booking, cost sharing, group decisions)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;
        private readonly ILogger<DisputeController> _logger;

        public DisputeController(
            IDisputeService disputeService,
            ILogger<DisputeController> logger)
        {
            _disputeService = disputeService;
            _logger = logger;
        }

        /// <summary>
        /// **[CoOwner]** Raise a booking dispute
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Raise a dispute related to a booking issue such as unauthorized usage, 
        /// cancellation problems, damage during booking, no-show, etc.
        /// 
        /// **Parameters:**
        /// - `bookingId` (body): ID of the booking in dispute
        /// - `vehicleId` (body): ID of the vehicle
        /// - `title` (body): Title/subject of the dispute (max 200 chars)
        /// - `description` (body): Detailed description (20-2000 chars)
        /// - `priority` (body): Low, Medium, High, Critical (default: Medium)
        /// - `category` (body): Unauthorized, Cancellation, Damage, NoShow, etc.
        /// - `respondentUserIds` (body): IDs of users being disputed against
        /// - `evidenceUrls` (body): URLs to evidence (images, documents)
        /// - `requestedResolution` (body): What outcome you want
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "vehicleId": 5,
        ///   "title": "Unauthorized vehicle usage",
        ///   "description": "User took the vehicle without proper booking confirmation and returned it 3 hours late causing me to miss my appointment.",
        ///   "priority": "High",
        ///   "category": "Unauthorized",
        ///   "respondentUserIds": [15],
        ///   "evidenceUrls": ["https://storage.com/evidence1.jpg"],
        ///   "requestedResolution": "Compensation for missed appointment and warning to the user"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Dispute raised successfully</response>
        /// <response code="403">NOT_AUTHORIZED - User is not a co-owner</response>
        /// <response code="404">BOOKING_NOT_FOUND - Booking does not exist</response>
        [HttpPost("booking")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RaiseBookingDispute([FromBody] RaiseBookingDisputeRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.RaiseBookingDisputeAsync(userId, request);

            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Raise a cost sharing dispute
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Raise a dispute about cost sharing, payments, or unfair charges.
        /// Can relate to payments, maintenance costs, or fund usage.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (body): ID of the vehicle
        /// - `paymentId` (body, optional): Related payment ID
        /// - `maintenanceCostId` (body, optional): Related maintenance cost ID
        /// - `fundUsageId` (body, optional): Related fund usage ID
        /// - `title` (body): Dispute title
        /// - `description` (body): Detailed description (20-2000 chars)
        /// - `disputedAmount` (body): Amount being disputed
        /// - `expectedAmount` (body, optional): What you think is fair
        /// - `priority` (body): Low, Medium, High, Critical
        /// - `category` (body): Overcharge, UnfairSplit, Unauthorized, InvalidCost, etc.
        /// - `respondentUserIds` (body): IDs of users involved
        /// - `evidenceUrls` (body): Evidence (receipts, invoices)
        /// - `requestedResolution` (body): Desired outcome
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "vehicleId": 5,
        ///   "maintenanceCostId": 45,
        ///   "title": "Unfair maintenance cost split",
        ///   "description": "The recent tire replacement cost was split equally among all co-owners, but the damage was caused by one user's reckless driving during their booking.",
        ///   "disputedAmount": 2000000,
        ///   "expectedAmount": 500000,
        ///   "priority": "High",
        ///   "category": "UnfairSplit",
        ///   "respondentUserIds": [15, 20],
        ///   "evidenceUrls": ["https://storage.com/tire-damage-report.pdf"],
        ///   "requestedResolution": "Cost should be split 80% by responsible user, 20% shared equally"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Dispute raised successfully</response>
        /// <response code="403">NOT_AUTHORIZED - User is not a co-owner</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        [HttpPost("cost-sharing")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RaiseCostSharingDispute([FromBody] RaiseCostSharingDisputeRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.RaiseCostSharingDisputeAsync(userId, request);

            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Raise a group decision dispute
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Raise a dispute about voting results, proposals, or group decisions.
        /// Can relate to fund usage votes, vehicle upgrade proposals, or ownership changes.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (body): ID of the vehicle
        /// - `fundUsageVoteId` (body, optional): Related fund usage vote ID
        /// - `vehicleUpgradeProposalId` (body, optional): Related upgrade proposal ID
        /// - `ownershipChangeId` (body, optional): Related ownership change ID
        /// - `title` (body): Dispute title
        /// - `description` (body): Detailed description (20-2000 chars)
        /// - `priority` (body): Low, Medium, High, Critical
        /// - `category` (body): VotingIrregularity, UnfairProcess, PolicyViolation, etc.
        /// - `violatedPolicy` (body, optional): Specific policy violated
        /// - `respondentUserIds` (body): IDs of users involved
        /// - `evidenceUrls` (body): Supporting evidence
        /// - `requestedResolution` (body): Desired outcome
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "vehicleId": 5,
        ///   "vehicleUpgradeProposalId": 12,
        ///   "title": "Voting irregularity in battery upgrade decision",
        ///   "description": "The battery upgrade vote was closed prematurely before all co-owners had a chance to vote. Two co-owners were traveling and explicitly requested extension but were denied.",
        ///   "priority": "High",
        ///   "category": "VotingIrregularity",
        ///   "violatedPolicy": "Voting period must be at least 7 days",
        ///   "respondentUserIds": [10],
        ///   "evidenceUrls": ["https://storage.com/vote-timeline.png"],
        ///   "requestedResolution": "Reopen the vote with proper 7-day period"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Dispute raised successfully</response>
        /// <response code="403">NOT_AUTHORIZED - User is not a co-owner</response>
        /// <response code="404">VEHICLE_NOT_FOUND - Vehicle does not exist</response>
        [HttpPost("group-decision")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RaiseGroupDecisionDispute([FromBody] RaiseGroupDecisionDisputeRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.RaiseGroupDecisionDisputeAsync(userId, request);

            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get dispute details by ID
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Retrieve full details of a specific dispute including all responses,
        /// evidence, and current status. User must be initiator, respondent, or co-owner.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/dispute/5
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Dispute retrieved successfully</response>
        /// <response code="403">ACCESS_DENIED - User not authorized to view this dispute</response>
        /// <response code="404">DISPUTE_NOT_FOUND</response>
        [HttpGet("{disputeId}")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDisputeById(int disputeId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.GetDisputeByIdAsync(disputeId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get list of disputes with filters
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get paginated list of disputes with various filters. Shows disputes for
        /// user's vehicles where they are initiator, respondent, or co-owner.
        /// 
        /// **Query Parameters:**
        /// - `vehicleId` (optional): Filter by vehicle
        /// - `disputeType` (optional): Booking, CostSharing, GroupDecision, VehicleDamage, OwnershipChange, Other
        /// - `status` (optional): Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated
        /// - `priority` (optional): Low, Medium, High, Critical
        /// - `isInitiator` (optional): true/false - disputes you initiated
        /// - `isRespondent` (optional): true/false - disputes where you are respondent
        /// - `startDate` (optional): Filter from date
        /// - `endDate` (optional): Filter to date
        /// - `pageNumber` (optional): Page number (default: 1)
        /// - `pageSize` (optional): Items per page (default: 20, max: 100)
        /// - `sortBy` (optional): CreatedDate, UpdatedDate, Priority
        /// - `sortOrder` (optional): asc, desc (default: desc)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/dispute?vehicleId=5&amp;status=Open&amp;priority=High&amp;pageNumber=1&amp;pageSize=20
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Disputes retrieved successfully</response>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<DisputeListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDisputes([FromQuery] GetDisputesRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.GetDisputesAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Respond to a dispute
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Add a response to an existing dispute. Can be used by respondents to 
        /// provide their side of the story or by any co-owner to add information.
        /// 
        /// **Parameters:**
        /// - `message` (body): Response message (10-2000 chars)
        /// - `evidenceUrls` (body, optional): Counter-evidence URLs
        /// - `agreesWithDispute` (body): true if you agree with the dispute
        /// - `proposedSolution` (body, optional): Your suggested resolution
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "message": "I apologize for the delay. I had a family emergency and tried to contact the booking owner but they didn't respond. I'm willing to compensate for the inconvenience.",
        ///   "evidenceUrls": ["https://storage.com/hospital-receipt.jpg"],
        ///   "agreesWithDispute": true,
        ///   "proposedSolution": "I will pay 50% of the missed appointment cost"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Response added successfully</response>
        /// <response code="403">ACCESS_DENIED - Not authorized to respond</response>
        /// <response code="404">DISPUTE_NOT_FOUND</response>
        [HttpPost("{disputeId}/respond")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RespondToDispute(
            int disputeId,
            [FromBody] RespondToDisputeRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.RespondToDisputeAsync(disputeId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[Admin]** Update dispute status
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Update the status of a dispute. Only administrators can update dispute status.
        /// Use this to move disputes through the resolution process.
        /// 
        /// **Parameters:**
        /// - `status` (body): Open, UnderReview, InMediation, Resolved, Rejected, Withdrawn, Escalated
        /// - `resolutionNotes` (body, required for Resolved/Rejected): Decision notes
        /// - `actionsRequired` (body, optional): Actions to be taken
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "status": "Resolved",
        ///   "resolutionNotes": "After reviewing all evidence and responses, the dispute is resolved in favor of the initiator. The respondent will compensate 70% of the missed appointment cost.",
        ///   "actionsRequired": "Payment of 350,000 VND within 7 days"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Status updated successfully</response>
        /// <response code="403">ACCESS_DENIED - Only admins can update status</response>
        /// <response code="404">DISPUTE_NOT_FOUND</response>
        [HttpPut("{disputeId}/status")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDisputeStatus(
            int disputeId,
            [FromBody] UpdateDisputeStatusRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.UpdateDisputeStatusAsync(disputeId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Withdraw a dispute
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Withdraw a dispute you initiated. Only the initiator can withdraw.
        /// Cannot withdraw resolved or rejected disputes.
        /// 
        /// **Parameters:**
        /// - `reason` (body): Reason for withdrawal
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "reason": "Issue has been resolved through direct communication with the other party"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Dispute withdrawn successfully</response>
        /// <response code="400">CANNOT_WITHDRAW - Dispute already resolved/rejected</response>
        /// <response code="403">ACCESS_DENIED - Only initiator can withdraw</response>
        /// <response code="404">DISPUTE_NOT_FOUND</response>
        [HttpPost("{disputeId}/withdraw")]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<DisputeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> WithdrawDispute(
            int disputeId,
            [FromBody] WithdrawDisputeRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _disputeService.WithdrawDisputeAsync(disputeId, userId, request.Reason);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }

    /// <summary>
    /// Request to withdraw a dispute
    /// </summary>
    public class WithdrawDisputeRequest
    {
        /// <summary>
        /// Reason for withdrawal
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
