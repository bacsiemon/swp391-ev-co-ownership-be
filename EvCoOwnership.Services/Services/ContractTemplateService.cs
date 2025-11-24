using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ContractTemplateDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Services.Mapping;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for contract template management
    /// </summary>
    public class ContractTemplateService : IContractTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ContractTemplateService> _logger;

        public ContractTemplateService(IUnitOfWork unitOfWork, ILogger<ContractTemplateService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Create a new contract template
        /// </summary>
        /// <param name="request">Template creation request</param>
        /// <param name="createdBy">ID of the user creating the template</param>
        /// <returns>Created template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> CreateTemplateAsync(CreateContractTemplateRequest request, int createdBy)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(createdBy);
                if (user == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                if (user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Check if template name already exists
                var nameExists = await _unitOfWork.ContractTemplateRepository.NameExistsAsync(request.Name);
                if (nameExists)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 409,
                        Message = "TEMPLATE_NAME_ALREADY_EXISTS",
                        Data = null
                    };
                }

                // Create template entity
                var template = request.ToEntity(createdBy);

                // Save to database
                await _unitOfWork.ContractTemplateRepository.AddAsync(template);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contract template {TemplateName} created by user {UserId}", request.Name, createdBy);

                // Get the created template with navigation properties
                var createdTemplate = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(template.Id);
                var response = createdTemplate?.ToResponse(0, true, false);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 201,
                    Message = "CONTRACT_TEMPLATE_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract template");
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Update an existing contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="request">Update request</param>
        /// <param name="userId">ID of the user making the update</param>
        /// <returns>Updated template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> UpdateTemplateAsync(int id, UpdateContractTemplateRequest request, int userId)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                if (user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get existing template
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                if (template == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if template is being used and prevent major changes
                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);
                if (usageCount > 0 && (request.Content != null || request.TermsAndConditions != null))
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_MODIFY_CONTENT_TEMPLATE_IN_USE",
                        Data = null,
                        Errors = new { UsageCount = usageCount }
                    };
                }

                // Check name uniqueness if name is being changed
                if (!string.IsNullOrEmpty(request.Name) && request.Name != template.Name)
                {
                    var nameExists = await _unitOfWork.ContractTemplateRepository.NameExistsAsync(request.Name, id);
                    if (nameExists)
                    {
                        return new BaseResponse<ContractTemplateResponse>
                        {
                            StatusCode = 409,
                            Message = "TEMPLATE_NAME_ALREADY_EXISTS",
                            Data = null
                        };
                    }
                }

                // Update template
                template.UpdateFromRequest(request);

                await _unitOfWork.ContractTemplateRepository.UpdateAsync(template);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contract template {TemplateId} updated by user {UserId}", id, userId);

                // Get updated template with navigation properties
                var updatedTemplate = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                var response = updatedTemplate?.ToResponse(usageCount, true, false);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TEMPLATE_UPDATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract template {TemplateId}", id);
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get contract template by ID
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> GetTemplateByIdAsync(int id, int userId) 
        {
            try
            {
                // Verify user exists and is Admin (only admins can view templates)
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                if (user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get template with details
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                if (template == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Get usage count
                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);

                // Determine permissions
                var canEdit = true; // Admin can always edit
                var canApprove = template.StatusEnum == EContractTemplateStatus.Draft;

                var response = template.ToResponse(usageCount, canEdit, canApprove);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract template {TemplateId}", id);
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get contract templates with pagination and filtering
        /// </summary>
        /// <param name="request">Filter and pagination request</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Paginated templates list</returns>
        public async Task<BaseResponse<ContractTemplatesListResponse>> GetTemplatesAsync(GetContractTemplatesRequest request, int userId)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<ContractTemplatesListResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                if (user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplatesListResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get paginated templates
                var (templates, totalCount) = await _unitOfWork.ContractTemplateRepository.GetPaginatedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.Status,
                    request.NameSearch,
                    request.SortBy,
                    request.SortOrder);

                // Convert to summaries with usage counts
                var templateSummaries = new List<ContractTemplateSummary>();
                foreach (var template in templates)
                {
                    var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(template.Id);
                    templateSummaries.Add(template.ToSummary(usageCount));
                }

                // Calculate statistics
                var allTemplates = await _unitOfWork.ContractTemplateRepository.GetAllAsync();
                var statistics = new ContractTemplateStatistics
                {
                    TotalTemplates = allTemplates.Count(),
                    DraftTemplates = allTemplates.Count(t => t.StatusEnum == EContractTemplateStatus.Draft),
                    ActiveTemplates = allTemplates.Count(t => t.StatusEnum == EContractTemplateStatus.Active),
                    InactiveTemplates = allTemplates.Count(t => t.StatusEnum == EContractTemplateStatus.Inactive),
                    ArchivedTemplates = allTemplates.Count(t => t.StatusEnum == EContractTemplateStatus.Archived),
                    TotalUsage = 0, // Would need to calculate from all templates
                    MostUsedTemplate = "N/A" // Would need to calculate
                };

                // Calculate total usage
                var totalUsage = 0;
                var mostUsedTemplate = "N/A";
                var maxUsage = 0;
                
                foreach (var template in allTemplates)
                {
                    var usage = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(template.Id);
                    totalUsage += usage;
                    
                    if (usage > maxUsage)
                    {
                        maxUsage = usage;
                        mostUsedTemplate = template.Name;
                    }
                }
                
                statistics.TotalUsage = totalUsage;
                statistics.MostUsedTemplate = maxUsage > 0 ? mostUsedTemplate : "N/A";

                var response = new ContractTemplatesListResponse
                {
                    Templates = templateSummaries,
                    Statistics = statistics,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                        TotalItems = totalCount,
                        HasPreviousPage = request.PageNumber > 1,
                        HasNextPage = request.PageNumber < (int)Math.Ceiling(totalCount / (double)request.PageSize)
                    }
                };

                return new BaseResponse<ContractTemplatesListResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract templates");
                return new BaseResponse<ContractTemplatesListResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Approve/activate a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="request">Approval request</param>
        /// <param name="approvedBy">ID of the user approving the template</param>
        /// <returns>Approved template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> ApproveTemplateAsync(int id, ApproveContractTemplateRequest request, int approvedBy)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(approvedBy);
                if (user == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                if (user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get template
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                if (template == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if template can be approved
                if (template.StatusEnum != EContractTemplateStatus.Draft)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 400,
                        Message = "TEMPLATE_NOT_IN_DRAFT_STATUS",
                        Data = null
                    };
                }

                // Approve template
                template.StatusEnum = EContractTemplateStatus.Active;
                template.ApprovedBy = approvedBy;
                template.ApprovedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.ContractTemplateRepository.UpdateAsync(template);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contract template {TemplateId} approved by user {UserId}", id, approvedBy);

                // Get updated template
                var approvedTemplate = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);
                var response = approvedTemplate?.ToResponse(usageCount, true, false);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TEMPLATE_APPROVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving contract template {TemplateId}", id);
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Deactivate a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user deactivating the template</param>
        /// <returns>Deactivated template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> DeactivateTemplateAsync(int id, int userId)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get template
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                if (template == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Deactivate template
                template.StatusEnum = EContractTemplateStatus.Inactive;
                template.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.ContractTemplateRepository.UpdateAsync(template);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contract template {TemplateId} deactivated by user {UserId}", id, userId);

                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);
                var response = template.ToResponse(usageCount, true, false);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TEMPLATE_DEACTIVATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating contract template {TemplateId}", id);
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Archive a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user archiving the template</param>
        /// <returns>Archived template response</returns>
        public async Task<BaseResponse<ContractTemplateResponse>> ArchiveTemplateAsync(int id, int userId)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get template
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdWithDetailsAsync(id);
                if (template == null)
                {
                    return new BaseResponse<ContractTemplateResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Archive template
                template.StatusEnum = EContractTemplateStatus.Archived;
                template.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.ContractTemplateRepository.UpdateAsync(template);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contract template {TemplateId} archived by user {UserId}", id, userId);

                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);
                var response = template.ToResponse(usageCount, true, false);

                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TEMPLATE_ARCHIVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving contract template {TemplateId}", id);
                return new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Delete a contract template (only if not used)
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user deleting the template</param>
        /// <returns>Success response</returns>
        public async Task<BaseResponse<object>> DeleteTemplateAsync(int id, int userId)
        {
            try
            {
                // Verify user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                // Get template
                var template = await _unitOfWork.ContractTemplateRepository.GetByIdAsync(id);
                if (template == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_TEMPLATE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if template is being used
                var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(id);
                if (usageCount > 0)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_DELETE_USED_TEMPLATE",
                        Data = null,
                        Errors = new { UsageCount = usageCount }
                    };
                }

                // Delete template
                var result = await _unitOfWork.ContractTemplateRepository.DeleteAsync(template);
                if (result)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Contract template {TemplateId} deleted by user {UserId}", id, userId);

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TEMPLATE_DELETED_SUCCESSFULLY",
                    Data = new { DeletedId = id, DeletedAt = DateTime.UtcNow }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract template {TemplateId}", id);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get active contract templates for selection
        /// </summary>
        /// <returns>List of active templates</returns>
        public async Task<BaseResponse<List<ContractTemplateSummary>>> GetActiveTemplatesAsync()
        {
            try
            {
                var activeTemplates = await _unitOfWork.ContractTemplateRepository.GetActiveTemplatesAsync();
                
                var summaries = new List<ContractTemplateSummary>();
                foreach (var template in activeTemplates)
                {
                    var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(template.Id);
                    summaries.Add(template.ToSummary(usageCount));
                }

                return new BaseResponse<List<ContractTemplateSummary>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active contract templates");
                return new BaseResponse<List<ContractTemplateSummary>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get templates created by a specific user
        /// </summary>
        /// <param name="creatorId">Creator user ID</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>List of templates created by the user</returns>
        public async Task<BaseResponse<List<ContractTemplateSummary>>> GetTemplatesByCreatorAsync(int creatorId, int userId)
        {
            try
            {
                // Verify requesting user exists and is Admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<List<ContractTemplateSummary>>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ADMIN_ONLY",
                        Data = null
                    };
                }

                var templates = await _unitOfWork.ContractTemplateRepository.GetByCreatorAsync(creatorId);
                
                var summaries = new List<ContractTemplateSummary>();
                foreach (var template in templates)
                {
                    var usageCount = await _unitOfWork.ContractTemplateRepository.GetUsageCountAsync(template.Id);
                    summaries.Add(template.ToSummary(usageCount));
                }

                return new BaseResponse<List<ContractTemplateSummary>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates by creator {CreatorId}", creatorId);
                return new BaseResponse<List<ContractTemplateSummary>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }
    }
}