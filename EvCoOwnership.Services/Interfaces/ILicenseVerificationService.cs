using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Interface for driving license verification services
    /// </summary>
    public interface ILicenseVerificationService
    {
        /// <summary>
        /// Verifies a driving license with the provided information
        /// </summary>
        /// <param name="request">License verification request containing license details</param>
        /// <returns>BaseResponse with verification result</returns>
        Task<BaseResponse> VerifyLicenseAsync(VerifyLicenseRequest request);

        /// <summary>
        /// Checks if a license number is already registered in the system
        /// </summary>
        /// <param name="licenseNumber">License number to check</param>
        /// <returns>BaseResponse indicating if license exists</returns>
        Task<BaseResponse> CheckLicenseExistsAsync(string licenseNumber);

        /// <summary>
        /// Gets license information by license number (for authorized users)
        /// </summary>
        /// <param name="licenseNumber">License number to lookup</param>
        /// <param name="userId">ID of the user making the request</param>
        /// <returns>BaseResponse with license information</returns>
        Task<BaseResponse> GetLicenseInfoAsync(string licenseNumber, int userId);

        /// <summary>
        /// Updates license status (for admin use)
        /// </summary>
        /// <param name="licenseNumber">License number to update</param>
        /// <param name="status">New status</param>
        /// <param name="adminUserId">ID of the admin user</param>
        /// <returns>BaseResponse with update result</returns>
        Task<BaseResponse> UpdateLicenseStatusAsync(string licenseNumber, string status, int adminUserId);
    }
}