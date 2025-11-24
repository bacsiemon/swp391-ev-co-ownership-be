using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.ContractTemplateDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for contract template management
    /// </summary>
    [Route("api/admin/contract-templates")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Admin)]
    public class ContractTemplateController : ControllerBase
    {
        private readonly IContractTemplateService _contractTemplateService;
        private readonly ILogger<ContractTemplateController> _logger;

        public ContractTemplateController(
            IContractTemplateService contractTemplateService,
            ILogger<ContractTemplateController> logger)
        {
            _contractTemplateService = contractTemplateService;
            _logger = logger;
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Create a new contract template for use in the system.
        /// 
        /// **Parameters:**
        /// - name: Required, template name (3-200 characters)
        /// - version: Required, version format like "1.0" or "2.1.3"
        /// - description: Required, template description (10-1000 characters)
        /// - content: Required, template content/body (minimum 50 characters)
        /// - termsAndConditions: Required, terms and conditions (minimum 20 characters)
        /// - status: Optional, initial status (default: Draft)
        /// 
        /// **Sample request:**
        /// 
        /// POST /api/admin/contract-templates
        /// ```json
        /// {
        ///   "name": "Vehicle Co-Ownership Agreement V2",
        ///   "version": "2.0",
        ///   "description": "Standard co-ownership agreement template for electric vehicles with enhanced terms",
        ///   "content": "VEHICLE CO-OWNERSHIP AGREEMENT\n\nThis agreement establishes the terms and conditions for co-ownership of electric vehicles...",
        ///   "termsAndConditions": "1. All parties agree to shared responsibility...\n2. Maintenance costs will be split proportionally...",
        ///   "status": "Draft"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Template created successfully</response>
        /// <response code="400">Validation error. Possible messages:
        /// - TEMPLATE_NAME_REQUIRED
        /// - TEMPLATE_NAME_LENGTH_INVALID
        /// - TEMPLATE_VERSION_REQUIRED
        /// - TEMPLATE_VERSION_FORMAT_INVALID
        /// - TEMPLATE_DESCRIPTION_REQUIRED
        /// - TEMPLATE_CONTENT_REQUIRED
        /// - TEMPLATE_TERMS_REQUIRED
        /// </response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="409">Template name already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateContractTemplateRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.CreateTemplateAsync(request, userId);

                return response.StatusCode switch
                {
                    201 => StatusCode(201, response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    409 => Conflict(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateTemplate endpoint");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Update an existing contract template.
        /// 
        /// **Parameters:**
        /// - All parameters are optional - only provide fields you want to update
        /// - name: Template name (3-200 characters)
        /// - version: Version format like "1.0" or "2.1.3"
        /// - description: Template description (10-1000 characters)
        /// - content: Template content/body (minimum 50 characters) - Cannot be changed if template is in use
        /// - termsAndConditions: Terms and conditions (minimum 20 characters) - Cannot be changed if template is in use
        /// - status: Template status
        /// 
        /// **Sample request:**
        /// 
        /// PUT /api/admin/contract-templates/1
        /// ```json
        /// {
        ///   "name": "Vehicle Co-Ownership Agreement V2.1",
        ///   "version": "2.1",
        ///   "description": "Updated co-ownership agreement template with latest legal requirements",
        ///   "status": "Active"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Template updated successfully</response>
        /// <response code="400">Validation error or template cannot be modified. Possible messages:
        /// - TEMPLATE_NAME_LENGTH_INVALID
        /// - TEMPLATE_VERSION_FORMAT_INVALID
        /// - CANNOT_MODIFY_CONTENT_TEMPLATE_IN_USE
        /// </response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="409">Template name already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateContractTemplateRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.UpdateTemplateAsync(id, request, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    409 => Conflict(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Get a specific contract template by ID with full details.
        /// 
        /// **Sample request:**
        /// 
        /// GET /api/admin/contract-templates/1
        /// </remarks>
        /// <response code="200">Template retrieved successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.GetTemplateByIdAsync(id, userId);

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
                _logger.LogError(ex, "Error in GetTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Get contract templates with pagination and filtering options.
        /// 
        /// **Query Parameters:**
        /// - status: Filter by status (Draft, Active, Inactive, Archived)
        /// - nameSearch: Search in template names and descriptions
        /// - pageNumber: Page number (default: 1)
        /// - pageSize: Items per page (default: 20, max: 100)
        /// - sortBy: Sort field (CreatedAt, UpdatedAt, Name, Version, Status)
        /// - sortOrder: Sort order (asc, desc)
        /// 
        /// **Sample request:**
        /// 
        /// GET /api/admin/contract-templates?status=Active&pageNumber=1&pageSize=10&sortBy=Name&sortOrder=asc
        /// </remarks>
        /// <response code="200">Templates retrieved successfully</response>
        /// <response code="400">Invalid query parameters. Possible messages:
        /// - PAGE_NUMBER_INVALID
        /// - PAGE_SIZE_INVALID
        /// - SORT_BY_INVALID
        /// - SORT_ORDER_INVALID
        /// - STATUS_FILTER_INVALID
        /// </response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        public async Task<IActionResult> GetTemplates([FromQuery] GetContractTemplatesRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.GetTemplatesAsync(request, userId);

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
                _logger.LogError(ex, "Error in GetTemplates endpoint");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Approve and activate a draft contract template.
        /// 
        /// **Parameters:**
        /// - notes: Optional approval notes (max 500 characters)
        /// 
        /// **Sample request:**
        /// 
        /// POST /api/admin/contract-templates/1/approve
        /// ```json
        /// {
        ///   "notes": "Template reviewed and approved for use in the system"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Template approved successfully</response>
        /// <response code="400">Template cannot be approved. Possible messages:
        /// - TEMPLATE_NOT_IN_DRAFT_STATUS
        /// - APPROVAL_NOTES_TOO_LONG
        /// </response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveTemplate(int id, [FromBody] ApproveContractTemplateRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.ApproveTemplateAsync(id, request, userId);

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
                _logger.LogError(ex, "Error in ApproveTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Deactivate a contract template to prevent new usage while keeping existing contracts valid.
        /// 
        /// **Sample request:**
        /// 
        /// POST /api/admin/contract-templates/1/deactivate
        /// </remarks>
        /// <response code="200">Template deactivated successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTemplate(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.DeactivateTemplateAsync(id, userId);

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
                _logger.LogError(ex, "Error in DeactivateTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Archive a contract template for long-term storage while preventing new usage.
        /// 
        /// **Sample request:**
        /// 
        /// POST /api/admin/contract-templates/1/archive
        /// </remarks>
        /// <response code="200">Template archived successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveTemplate(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.ArchiveTemplateAsync(id, userId);

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
                _logger.LogError(ex, "Error in ArchiveTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Permanently delete a contract template. Only templates that have never been used can be deleted.
        /// 
        /// **Sample request:**
        /// 
        /// DELETE /api/admin/contract-templates/1
        /// </remarks>
        /// <response code="200">Template deleted successfully</response>
        /// <response code="400">Template cannot be deleted. Possible messages:
        /// - CANNOT_DELETE_USED_TEMPLATE
        /// </response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="404">Template not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.DeleteTemplateAsync(id, userId);

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
                _logger.LogError(ex, "Error in DeleteTemplate endpoint for template {TemplateId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Get all active contract templates available for use in contract creation.
        /// 
        /// **Sample request:**
        /// 
        /// GET /api/admin/contract-templates/active
        /// </remarks>
        /// <response code="200">Active templates retrieved successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveTemplates()
        {
            try
            {
                var response = await _contractTemplateService.GetActiveTemplatesAsync();

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveTemplates endpoint");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>Admin</summary>
        /// <remarks>
        /// Get contract templates created by a specific user.
        /// 
        /// **Sample request:**
        /// 
        /// GET /api/admin/contract-templates/creator/123
        /// </remarks>
        /// <response code="200">Templates retrieved successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Access denied - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("creator/{creatorId}")]
        public async Task<IActionResult> GetTemplatesByCreator(int creatorId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _contractTemplateService.GetTemplatesByCreatorAsync(creatorId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTemplatesByCreator endpoint for creator {CreatorId}", creatorId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }
    }
}