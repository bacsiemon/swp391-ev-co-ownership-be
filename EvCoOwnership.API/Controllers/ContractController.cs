using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ContractDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing e-contracts (view, create, sign)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IContractService contractService,
            ILogger<ContractController> logger)
        {
            _contractService = contractService;
            _logger = logger;
        }

        /// <summary>
        /// **[CoOwner]** Create a new e-contract
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Create a new electronic contract for vehicle co-ownership, usage agreement, cost sharing, etc.
        /// Creator is automatically added as the first signatory.
        /// 
        /// **Parameters:**
        /// - `vehicleId` (body): ID of the vehicle
        /// - `templateType` (body): CoOwnershipAgreement, VehicleUsageAgreement, CostSharingAgreement, MaintenanceAgreement, etc.
        /// - `title` (body): Contract title (5-200 chars)
        /// - `description` (body, optional): Detailed description
        /// - `customTerms` (body, optional): Custom terms in JSON format
        /// - `signatoryUserIds` (body): List of user IDs who need to sign (excluding yourself)
        /// - `effectiveDate` (body, optional): When contract becomes effective
        /// - `expiryDate` (body, optional): When contract expires
        /// - `signatureDeadline` (body, optional): Deadline for all signatures (default: 30 days)
        /// - `autoActivate` (body): Auto-activate when fully signed (default: true)
        /// - `attachmentUrls` (body, optional): URLs to supporting documents
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "vehicleId": 5,
        ///   "templateType": "CoOwnershipAgreement",
        ///   "title": "2025 Tesla Model 3 Co-Ownership Agreement",
        ///   "description": "This agreement establishes the co-ownership terms for our shared Tesla Model 3",
        ///   "signatoryUserIds": [10, 15, 20],
        ///   "effectiveDate": "2025-11-01T00:00:00Z",
        ///   "expiryDate": "2027-10-31T23:59:59Z",
        ///   "signatureDeadline": "2025-11-15T23:59:59Z",
        ///   "autoActivate": true,
        ///   "attachmentUrls": ["https://storage.com/vehicle-inspection-report.pdf"]
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Contract created successfully</response>
        /// <response code="403">NOT_AUTHORIZED - User is not a co-owner</response>
        /// <response code="404">VEHICLE_NOT_FOUND</response>
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.CreateContractAsync(userId, request);

            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get contract by ID
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Retrieve full details of a specific e-contract including all signatures,
        /// content, and status. User must be creator, signatory, or co-owner of the vehicle.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/5
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Contract retrieved successfully</response>
        /// <response code="403">ACCESS_DENIED - User not authorized to view this contract</response>
        /// <response code="404">CONTRACT_NOT_FOUND</response>
        [HttpGet("{contractId}")]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractById(int contractId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.GetContractByIdAsync(contractId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get list of contracts with filters
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get paginated list of e-contracts with various filters. Shows contracts for
        /// user's vehicles where they are creator, signatory, or co-owner.
        /// 
        /// **Query Parameters:**
        /// - `vehicleId` (optional): Filter by specific vehicle
        /// - `templateType` (optional): CoOwnershipAgreement, VehicleUsageAgreement, CostSharingAgreement, etc.
        /// - `status` (optional): Draft, PendingSignatures, PartiallySigned, FullySigned, Active, Expired, Terminated, Rejected
        /// - `isCreator` (optional): true/false - contracts you created
        /// - `isSignatory` (optional): true/false - contracts where you are a signatory
        /// - `mySignatureStatus` (optional): Pending, Signed, Declined
        /// - `createdFrom` (optional): Filter from creation date
        /// - `createdTo` (optional): Filter to creation date
        /// - `activeOnly` (optional): true - show only active contracts
        /// - `pendingMySignature` (optional): true - show only contracts waiting for your signature
        /// - `pageNumber` (optional): Page number (default: 1)
        /// - `pageSize` (optional): Items per page (default: 20, max: 100)
        /// - `sortBy` (optional): CreatedAt, UpdatedAt, EffectiveDate, ExpiryDate, Title
        /// - `sortOrder` (optional): asc, desc (default: desc)
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract?vehicleId=5&amp;status=Active&amp;pageNumber=1&amp;pageSize=20
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Contracts retrieved successfully</response>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<ContractListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetContracts([FromQuery] GetContractsRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.GetContractsAsync(userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Sign an e-contract
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Electronically sign a contract. This records your signature with metadata
        /// (IP address, device info, timestamp, geolocation). Contract becomes active
        /// when all required signatures are collected (if auto-activate is enabled).
        /// 
        /// **Parameters:**
        /// - `signature` (body): Electronic signature (encrypted hash, min 10 chars)
        /// - `ipAddress` (body, optional): Your IP address
        /// - `deviceInfo` (body, optional): Device information
        /// - `geolocation` (body, optional): GPS coordinates
        /// - `agreementConfirmation` (body, optional): Confirmation text
        /// - `signerNotes` (body, optional): Your notes on signing (max 500 chars)
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "signature": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        ///   "ipAddress": "192.168.1.100",
        ///   "deviceInfo": "Chrome 120.0 on Windows 11",
        ///   "geolocation": "21.0285,105.8542",
        ///   "agreementConfirmation": "I agree to all terms and conditions",
        ///   "signerNotes": "Reviewed and approved all terms"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Contract signed successfully or fully signed</response>
        /// <response code="403">NOT_A_SIGNATORY - User is not listed as signatory</response>
        /// <response code="400">ALREADY_SIGNED, ALREADY_DECLINED, CONTRACT_NOT_PENDING_SIGNATURES, SIGNATURE_DEADLINE_EXPIRED</response>
        /// <response code="404">CONTRACT_NOT_FOUND</response>
        [HttpPost("{contractId}/sign")]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SignContract(
            int contractId,
            [FromBody] SignContractRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.SignContractAsync(contractId, userId, request);

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
        /// **[CoOwner]** Decline/reject a contract
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Decline to sign a contract. This records your rejection with reason
        /// and optional suggested changes. Contract status becomes "Rejected".
        /// 
        /// **Parameters:**
        /// - `reason` (body): Reason for declining (10-1000 chars)
        /// - `suggestedChanges` (body, optional): Your suggestions for improvement (max 2000 chars)
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "reason": "The cost sharing ratio doesn't reflect my actual usage pattern. I use the vehicle 40% of the time but the agreement splits costs equally.",
        ///   "suggestedChanges": "Suggest changing to usage-based cost sharing: 40% me, 35% John, 25% Sarah based on actual booking hours"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Contract declined successfully</response>
        /// <response code="403">NOT_A_SIGNATORY</response>
        /// <response code="400">ALREADY_SIGNED</response>
        /// <response code="404">CONTRACT_NOT_FOUND</response>
        [HttpPost("{contractId}/decline")]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeclineContract(
            int contractId,
            [FromBody] DeclineContractRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.DeclineContractAsync(contractId, userId, request);

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
        /// **[CoOwner/Admin]** Terminate an active contract
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Terminate an active contract early. Only contract creator or admin can terminate.
        /// This action is permanent and changes contract status to "Terminated".
        /// 
        /// **Parameters:**
        /// - `reason` (body): Reason for termination (10-1000 chars)
        /// - `effectiveDate` (body, optional): When termination takes effect (default: now)
        /// - `notes` (body, optional): Additional notes (max 2000 chars)
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "reason": "Vehicle has been sold. All co-owners have agreed to terminate the co-ownership agreement.",
        ///   "effectiveDate": "2025-12-31T23:59:59Z",
        ///   "notes": "Final settlement completed. All parties satisfied."
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Contract terminated successfully</response>
        /// <response code="403">NOT_AUTHORIZED - Only creator or admin can terminate</response>
        /// <response code="400">ALREADY_TERMINATED</response>
        /// <response code="404">CONTRACT_NOT_FOUND</response>
        [HttpPost("{contractId}/terminate")]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ContractResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TerminateContract(
            int contractId,
            [FromBody] TerminateContractRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.TerminateContractAsync(contractId, userId, request);

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
        /// **[CoOwner]** Get available contract templates
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get list of all available contract templates with their descriptions,
        /// required fields, and usage information.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/templates
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Templates retrieved successfully</response>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(BaseResponse<List<ContractTemplateResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetContractTemplates()
        {
            var response = await _contractService.GetContractTemplatesAsync();

            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get a specific contract template
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get detailed information about a specific contract template including
        /// the template content and placeholders.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/templates/CoOwnershipAgreement
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Template retrieved successfully</response>
        /// <response code="400">INVALID_TEMPLATE_TYPE</response>
        [HttpGet("templates/{templateType}")]
        [ProducesResponseType(typeof(BaseResponse<ContractTemplateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ContractTemplateResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetContractTemplate(string templateType)
        {
            var response = await _contractService.GetContractTemplateAsync(templateType);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Download contract as PDF
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Download a signed contract as PDF document for offline storage or printing.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/5/download
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">PDF generated successfully</response>
        /// <response code="403">ACCESS_DENIED</response>
        /// <response code="404">CONTRACT_NOT_FOUND</response>
        [HttpGet("{contractId}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<byte[]>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<byte[]>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadContractPdf(int contractId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.DownloadContractPdfAsync(contractId, userId);

            if (response.StatusCode == 200 && response.Data != null)
            {
                return File(response.Data, "application/pdf", $"contract-{contractId}.pdf");
            }

            return response.StatusCode switch
            {
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get contracts pending your signature
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get all contracts that are waiting for your signature, ordered by
        /// signature deadline (most urgent first).
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/pending-signature
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Pending contracts retrieved successfully</response>
        [HttpGet("pending-signature")]
        [ProducesResponseType(typeof(BaseResponse<List<ContractSummary>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingSignatureContracts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.GetPendingSignatureContractsAsync(userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// **[CoOwner]** Get contracts you have signed
        /// </summary>
        /// <remarks>
        /// **Description:**
        /// Get all contracts that you have already signed, optionally filtered by vehicle.
        /// Useful for viewing your contract history.
        /// 
        /// **Query Parameters:**
        /// - `vehicleId` (optional): Filter by specific vehicle
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/contract/signed?vehicleId=5
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <response code="200">Signed contracts retrieved successfully</response>
        [HttpGet("signed")]
        [ProducesResponseType(typeof(BaseResponse<List<ContractSummary>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSignedContracts([FromQuery] int? vehicleId = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _contractService.GetSignedContractsAsync(userId, vehicleId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
