using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse> ForgotPasswordAsync(string email);
        BaseResponse GetForgotPasswordOtpAsync(string email);
        Task<BaseResponse> ResetPasswordAsync(ResetPasswordRequest request);
    }
}