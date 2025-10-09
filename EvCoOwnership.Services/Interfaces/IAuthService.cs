using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse> LoginAsync(LoginRequest request);
        Task<BaseResponse> RegisterAsync(RegisterRequest request);
        Task<BaseResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<BaseResponse> ForgotPasswordAsync(string email);
        BaseResponse GetForgotPasswordOtpAsync(string email);
        Task<BaseResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<BaseResponse> VerifyLicenseAsync(VerifyLicenseRequest request);
    }
}