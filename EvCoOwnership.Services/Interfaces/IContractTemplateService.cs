using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ContractTemplateDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for contract template management
    /// </summary>
    public interface IContractTemplateService
    {
        /// <summary>
        /// Create a new contract template
        /// </summary>
        /// <param name="request">Template creation request</param>
        /// <param name="createdBy">ID of the user creating the template</param>
        /// <returns>Created template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> CreateTemplateAsync(CreateContractTemplateRequest request, int createdBy);

        /// <summary>
        /// Update an existing contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="request">Update request</param>
        /// <param name="userId">ID of the user making the update</param>
        /// <returns>Updated template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> UpdateTemplateAsync(int id, UpdateContractTemplateRequest request, int userId);

        /// <summary>
        /// Get contract template by ID
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> GetTemplateByIdAsync(int id, int userId);

        /// <summary>
        /// Get contract templates with pagination and filtering
        /// </summary>
        /// <param name="request">Filter and pagination request</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>Paginated templates list</returns>
        Task<BaseResponse<ContractTemplatesListResponse>> GetTemplatesAsync(GetContractTemplatesRequest request, int userId);

        /// <summary>
        /// Approve/activate a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="request">Approval request</param>
        /// <param name="approvedBy">ID of the user approving the template</param>
        /// <returns>Approved template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> ApproveTemplateAsync(int id, ApproveContractTemplateRequest request, int approvedBy);

        /// <summary>
        /// Deactivate a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user deactivating the template</param>
        /// <returns>Deactivated template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> DeactivateTemplateAsync(int id, int userId);

        /// <summary>
        /// Archive a contract template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user archiving the template</param>
        /// <returns>Archived template response</returns>
        Task<BaseResponse<ContractTemplateResponse>> ArchiveTemplateAsync(int id, int userId);

        /// <summary>
        /// Delete a contract template (only if not used)
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="userId">ID of the user deleting the template</param>
        /// <returns>Success response</returns>
        Task<BaseResponse<object>> DeleteTemplateAsync(int id, int userId);

        /// <summary>
        /// Get active contract templates for selection
        /// </summary>
        /// <returns>List of active templates</returns>
        Task<BaseResponse<List<ContractTemplateSummary>>> GetActiveTemplatesAsync();

        /// <summary>
        /// Get templates created by a specific user
        /// </summary>
        /// <param name="creatorId">Creator user ID</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <returns>List of templates created by the user</returns>
        Task<BaseResponse<List<ContractTemplateSummary>>> GetTemplatesByCreatorAsync(int creatorId, int userId);
    }
}