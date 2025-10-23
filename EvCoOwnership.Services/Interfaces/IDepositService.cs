using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.DepositDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service for managing deposit transactions
    /// </summary>
    public interface IDepositService
    {
        /// <summary>
        /// Creates a new deposit transaction and returns payment URL
        /// </summary>
        /// <param name="userId">User ID making the deposit</param>
        /// <param name="request">Deposit creation request</param>
        /// <returns>Payment URL and deposit details</returns>
        Task<BaseResponse<DepositPaymentUrlResponse>> CreateDepositAsync(int userId, CreateDepositRequest request);

        /// <summary>
        /// Gets deposit transaction by ID
        /// </summary>
        /// <param name="depositId">Deposit ID</param>
        /// <param name="userId">User ID (for authorization check)</param>
        /// <returns>Deposit details</returns>
        Task<BaseResponse<DepositResponse>> GetDepositByIdAsync(int depositId, int userId);

        /// <summary>
        /// Gets user's deposit history with filters
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Filter and pagination request</param>
        /// <returns>Paginated list of deposits</returns>
        Task<BaseResponse<DepositListResponse>> GetUserDepositsAsync(int userId, GetDepositsRequest request);

        /// <summary>
        /// Gets all deposits (admin only)
        /// </summary>
        /// <param name="request">Filter and pagination request</param>
        /// <returns>Paginated list of all deposits</returns>
        Task<BaseResponse<DepositListResponse>> GetAllDepositsAsync(GetDepositsRequest request);

        /// <summary>
        /// Verifies and processes deposit callback from payment gateway
        /// </summary>
        /// <param name="request">Callback verification request</param>
        /// <returns>Processing result</returns>
        Task<BaseResponse<DepositResponse>> VerifyDepositCallbackAsync(VerifyDepositCallbackRequest request);

        /// <summary>
        /// Cancels a pending deposit
        /// </summary>
        /// <param name="depositId">Deposit ID</param>
        /// <param name="userId">User ID (for authorization check)</param>
        /// <returns>Success message</returns>
        Task<BaseResponse<string>> CancelDepositAsync(int depositId, int userId);

        /// <summary>
        /// Gets deposit statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Deposit statistics</returns>
        Task<BaseResponse<DepositStatistics>> GetUserDepositStatisticsAsync(int userId);

        /// <summary>
        /// Gets available payment methods information
        /// </summary>
        /// <returns>List of supported payment methods with details</returns>
        Task<BaseResponse<PaymentMethodsResponse>> GetAvailablePaymentMethodsAsync();
    }
}
