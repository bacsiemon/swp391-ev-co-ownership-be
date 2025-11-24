using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Repositories
{
    /// <summary>
    /// Repository implementation for ContractTemplate operations
    /// </summary>
    public class ContractTemplateRepository : GenericRepository<ContractTemplate>, IContractTemplateRepository
    {
        public ContractTemplateRepository(EvCoOwnershipDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get contract template with navigation properties
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Contract template with navigation properties or null</returns>
        public async Task<ContractTemplate?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Set<ContractTemplate>()
                .Include(ct => ct.CreatedByNavigation)
                .Include(ct => ct.ApprovedByNavigation)
                .Include(ct => ct.GroupContracts)
                .FirstOrDefaultAsync(ct => ct.Id == id);
        }

        /// <summary>
        /// Get contract templates by status
        /// </summary>
        /// <param name="status">Template status</param>
        /// <returns>List of templates with the specified status</returns>
        public async Task<IEnumerable<ContractTemplate>> GetByStatusAsync(EContractTemplateStatus status)
        {
            return await _context.Set<ContractTemplate>()
                .Include(ct => ct.CreatedByNavigation)
                .Include(ct => ct.ApprovedByNavigation)
                .Where(ct => ct.StatusEnum == status)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get active contract templates
        /// </summary>
        /// <returns>List of active templates</returns>
        public async Task<IEnumerable<ContractTemplate>> GetActiveTemplatesAsync()
        {
            return await GetByStatusAsync(EContractTemplateStatus.Active);
        }

        /// <summary>
        /// Search contract templates by name
        /// </summary>
        /// <param name="searchText">Search text</param>
        /// <returns>List of matching templates</returns>
        public async Task<IEnumerable<ContractTemplate>> SearchByNameAsync(string searchText)
        {
            return await _context.Set<ContractTemplate>()
                .Include(ct => ct.CreatedByNavigation)
                .Include(ct => ct.ApprovedByNavigation)
                .Where(ct => ct.Name.Contains(searchText))
                .OrderBy(ct => ct.Name)
                .ToListAsync();
        }

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
        public async Task<(IEnumerable<ContractTemplate> Templates, int TotalCount)> GetPaginatedAsync(
            int pageNumber,
            int pageSize,
            EContractTemplateStatus? status = null,
            string? nameSearch = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc")
        {
            var query = _context.Set<ContractTemplate>()
                .Include(ct => ct.CreatedByNavigation)
                .Include(ct => ct.ApprovedByNavigation)
                .AsQueryable();

            // Apply filters
            if (status.HasValue)
            {
                query = query.Where(ct => ct.StatusEnum == status.Value);
            }

            if (!string.IsNullOrEmpty(nameSearch))
            {
                query = query.Where(ct => ct.Name.Contains(nameSearch) || 
                                         ct.Description.Contains(nameSearch));
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(ct => ct.Name)
                    : query.OrderByDescending(ct => ct.Name),
                "version" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(ct => ct.Version)
                    : query.OrderByDescending(ct => ct.Version),
                "status" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(ct => ct.StatusEnum)
                    : query.OrderByDescending(ct => ct.StatusEnum),
                "updatedat" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(ct => ct.UpdatedAt)
                    : query.OrderByDescending(ct => ct.UpdatedAt),
                _ => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(ct => ct.CreatedAt)
                    : query.OrderByDescending(ct => ct.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var templates = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (templates, totalCount);
        }

        /// <summary>
        /// Check if template name exists (excluding specific ID for updates)
        /// </summary>
        /// <param name="name">Template name</param>
        /// <param name="excludeId">ID to exclude from check</param>
        /// <returns>True if name exists</returns>
        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Set<ContractTemplate>()
                .Where(ct => ct.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(ct => ct.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get templates created by a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of templates created by the user</returns>
        public async Task<IEnumerable<ContractTemplate>> GetByCreatorAsync(int userId)
        {
            return await _context.Set<ContractTemplate>()
                .Include(ct => ct.CreatedByNavigation)
                .Include(ct => ct.ApprovedByNavigation)
                .Where(ct => ct.CreatedBy == userId)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get usage count for a template (from GroupContracts)
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <returns>Usage count</returns>
        public async Task<int> GetUsageCountAsync(int templateId)
        {
            return await _context.Set<GroupContract>()
                .CountAsync(gc => gc.TemplateId == templateId);
        }
    }
}