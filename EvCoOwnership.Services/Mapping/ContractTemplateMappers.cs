using EvCoOwnership.Repositories.DTOs.ContractTemplateDTOs;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Mapping
{
    /// <summary>
    /// Mappers for ContractTemplate entity and DTOs
    /// </summary>
    public static class ContractTemplateMappers
    {
        #region DTO to Entity

        /// <summary>
        /// Map CreateContractTemplateRequest to ContractTemplate entity
        /// </summary>
        /// <param name="request">Create request</param>
        /// <param name="createdBy">ID of the user creating the template</param>
        /// <returns>ContractTemplate entity</returns>
        public static ContractTemplate ToEntity(this CreateContractTemplateRequest request, int createdBy)
        {
            return new ContractTemplate
            {
                Name = request.Name,
                Version = request.Version,
                Description = request.Description,
                Content = request.Content,
                TermsAndConditions = request.TermsAndConditions,
                StatusEnum = request.Status,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Update ContractTemplate entity from UpdateContractTemplateRequest
        /// </summary>
        /// <param name="entity">Existing entity to update</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated entity</returns>
        public static ContractTemplate UpdateFromRequest(this ContractTemplate entity, UpdateContractTemplateRequest request)
        {
            if (!string.IsNullOrEmpty(request.Name))
                entity.Name = request.Name;
                
            if (!string.IsNullOrEmpty(request.Version))
                entity.Version = request.Version;
                
            if (!string.IsNullOrEmpty(request.Description))
                entity.Description = request.Description;
                
            if (!string.IsNullOrEmpty(request.Content))
                entity.Content = request.Content;
                
            if (!string.IsNullOrEmpty(request.TermsAndConditions))
                entity.TermsAndConditions = request.TermsAndConditions;
                
            if (request.Status.HasValue)
                entity.StatusEnum = request.Status.Value;

            entity.UpdatedAt = DateTime.UtcNow;
            
            return entity;
        }

        #endregion

        #region Entity to DTO

        /// <summary>
        /// Map ContractTemplate entity to response DTO
        /// </summary>
        /// <param name="entity">ContractTemplate entity</param>
        /// <param name="usageCount">Usage count (optional)</param>
        /// <param name="canEdit">Whether current user can edit this template</param>
        /// <param name="canApprove">Whether current user can approve this template</param>
        /// <returns>ContractTemplateResponse</returns>
        public static ContractTemplateResponse ToResponse(this ContractTemplate entity, int usageCount = 0, bool canEdit = false, bool canApprove = false)
        {
            return new ContractTemplateResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Version = entity.Version,
                Description = entity.Description,
                Content = entity.Content,
                TermsAndConditions = entity.TermsAndConditions,
                Status = entity.StatusEnum ?? Repositories.Enums.EContractTemplateStatus.Draft,
                StatusText = GetStatusText(entity.StatusEnum),
                CreatedBy = entity.CreatedBy,
                CreatedByName = entity.CreatedByNavigation != null ? 
                    $"{entity.CreatedByNavigation.FirstName} {entity.CreatedByNavigation.LastName}".Trim() : null,
                ApprovedBy = entity.ApprovedBy,
                ApprovedByName = entity.ApprovedByNavigation != null ? 
                    $"{entity.ApprovedByNavigation.FirstName} {entity.ApprovedByNavigation.LastName}".Trim() : null,
                ApprovedAt = entity.ApprovedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                UsageCount = usageCount,
                IsActive = entity.StatusEnum == Repositories.Enums.EContractTemplateStatus.Active,
                CanEdit = canEdit,
                CanApprove = canApprove
            };
        }

        /// <summary>
        /// Map ContractTemplate entity to summary DTO
        /// </summary>
        /// <param name="entity">ContractTemplate entity</param>
        /// <param name="usageCount">Usage count (optional)</param>
        /// <returns>ContractTemplateSummary</returns>
        public static ContractTemplateSummary ToSummary(this ContractTemplate entity, int usageCount = 0)
        {
            return new ContractTemplateSummary
            {
                Id = entity.Id,
                Name = entity.Name,
                Version = entity.Version,
                Description = entity.Description,
                Status = entity.StatusEnum ?? Repositories.Enums.EContractTemplateStatus.Draft,
                StatusText = GetStatusText(entity.StatusEnum),
                CreatedByName = entity.CreatedByNavigation != null ? 
                    $"{entity.CreatedByNavigation.FirstName} {entity.CreatedByNavigation.LastName}".Trim() : null,
                ApprovedByName = entity.ApprovedByNavigation != null ? 
                    $"{entity.ApprovedByNavigation.FirstName} {entity.ApprovedByNavigation.LastName}".Trim() : null,
                CreatedAt = entity.CreatedAt,
                ApprovedAt = entity.ApprovedAt,
                UsageCount = usageCount,
                IsActive = entity.StatusEnum == Repositories.Enums.EContractTemplateStatus.Active
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get human-readable status text
        /// </summary>
        /// <param name="status">Template status</param>
        /// <returns>Status text</returns>
        private static string GetStatusText(Repositories.Enums.EContractTemplateStatus? status)
        {
            return status switch
            {
                Repositories.Enums.EContractTemplateStatus.Draft => "Draft",
                Repositories.Enums.EContractTemplateStatus.Active => "Active",
                Repositories.Enums.EContractTemplateStatus.Inactive => "Inactive",
                Repositories.Enums.EContractTemplateStatus.Archived => "Archived",
                _ => "Unknown"
            };
        }

        #endregion
    }
}