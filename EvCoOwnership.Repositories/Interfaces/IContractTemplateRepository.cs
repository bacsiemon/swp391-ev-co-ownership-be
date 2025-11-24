using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for ContractTemplate operations
    /// </summary>
    public interface IContractTemplateRepository : IGenericRepository<ContractTemplate>
    {
        /// <summary>
        /// Get contract template with navigation properties
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Contract template with navigation properties or null</returns>
        Task<ContractTemplate?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Get contract templates by status
        /// </summary>
        /// <param name="status">Template status</param>
        /// <returns>List of templates with the specified status</returns>
        Task<IEnumerable<ContractTemplate>> GetByStatusAsync(EContractTemplateStatus status);

        /// <summary>
        /// Get active contract templates
        /// </summary>
        /// <returns>List of active templates</returns>
        Task<IEnumerable<ContractTemplate>> GetActiveTemplatesAsync();

        /// <summary>
        /// Search contract templates by name
        /// </summary>
        /// <param name="searchText">Search text</param>
        /// <returns>List of matching templates</returns>
        Task<IEnumerable<ContractTemplate>> SearchByNameAsync(string searchText);

        /// <summary>
        /// Get contract templates with pagination and filtering
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="nameSearch">Optional name search</param>
        /// <param name="sortBy">Sort field</param>
        /// <param name="sortOrder">Sort order (asc/desc)</param>
        /// <returns>Paginated list of templates</returns>
        Task<(IEnumerable<ContractTemplate> Templates, int TotalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            EContractTemplateStatus? status = null,
            string? nameSearch = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc");

        /// <summary>
        /// Check if template name exists (excluding specific ID for updates)
        /// </summary>
        /// <param name="name">Template name</param>
        /// <param name="excludeId">ID to exclude from check</param>
        /// <returns>True if name exists</returns>
        Task<bool> NameExistsAsync(string name, int? excludeId = null);

        /// <summary>
        /// Get templates created by a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of templates created by the user</returns>
        Task<IEnumerable<ContractTemplate>> GetByCreatorAsync(int userId);

        /// <summary>
        /// Get usage count for a template (from GroupContracts)
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <returns>Usage count</returns>
        Task<int> GetUsageCountAsync(int templateId);
    }
}