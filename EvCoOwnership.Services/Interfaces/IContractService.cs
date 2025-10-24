using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ContractDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for e-contract management
    /// </summary>
    public interface IContractService
    {
        /// <summary>
        /// Create a new e-contract
        /// </summary>
        /// <param name="userId">User creating the contract</param>
        /// <param name="request">Contract details</param>
        /// <returns>Created contract</returns>
        Task<BaseResponse<ContractResponse>> CreateContractAsync(
            int userId,
            CreateContractRequest request);

        /// <summary>
        /// Get contract by ID
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <param name="userId">Requesting user ID (for authorization)</param>
        /// <returns>Contract details</returns>
        Task<BaseResponse<ContractResponse>> GetContractByIdAsync(
            int contractId,
            int userId);

        /// <summary>
        /// Get list of contracts with filters
        /// </summary>
        /// <param name="userId">Requesting user ID</param>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Paginated contract list</returns>
        Task<BaseResponse<ContractListResponse>> GetContractsAsync(
            int userId,
            GetContractsRequest request);

        /// <summary>
        /// Sign an e-contract
        /// </summary>
        /// <param name="contractId">Contract ID to sign</param>
        /// <param name="userId">User signing the contract</param>
        /// <param name="request">Signature details</param>
        /// <returns>Updated contract</returns>
        Task<BaseResponse<ContractResponse>> SignContractAsync(
            int contractId,
            int userId,
            SignContractRequest request);

        /// <summary>
        /// Decline/reject a contract
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <param name="userId">User declining the contract</param>
        /// <param name="request">Decline details</param>
        /// <returns>Updated contract</returns>
        Task<BaseResponse<ContractResponse>> DeclineContractAsync(
            int contractId,
            int userId,
            DeclineContractRequest request);

        /// <summary>
        /// Terminate an active contract
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <param name="userId">User requesting termination</param>
        /// <param name="request">Termination details</param>
        /// <returns>Updated contract</returns>
        Task<BaseResponse<ContractResponse>> TerminateContractAsync(
            int contractId,
            int userId,
            TerminateContractRequest request);

        /// <summary>
        /// Get available contract templates
        /// </summary>
        /// <returns>List of templates</returns>
        Task<BaseResponse<List<ContractTemplateResponse>>> GetContractTemplatesAsync();

        /// <summary>
        /// Get a specific contract template
        /// </summary>
        /// <param name="templateType">Template type</param>
        /// <returns>Template details</returns>
        Task<BaseResponse<ContractTemplateResponse>> GetContractTemplateAsync(string templateType);

        /// <summary>
        /// Download contract as PDF
        /// </summary>
        /// <param name="contractId">Contract ID</param>
        /// <param name="userId">Requesting user ID</param>
        /// <returns>PDF byte array</returns>
        Task<BaseResponse<byte[]>> DownloadContractPdfAsync(
            int contractId,
            int userId);

        /// <summary>
        /// Get contracts pending user's signature
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of contracts pending signature</returns>
        Task<BaseResponse<List<ContractSummary>>> GetPendingSignatureContractsAsync(int userId);

        /// <summary>
        /// Get signed contracts for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="vehicleId">Optional vehicle ID filter</param>
        /// <returns>List of signed contracts</returns>
        Task<BaseResponse<List<ContractSummary>>> GetSignedContractsAsync(
            int userId,
            int? vehicleId = null);
    }
}
