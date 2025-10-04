using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        private string RESET_PASSWORD_SUFFIX = "_RESET_PASS";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>BaseResponse with status 200, 404</returns>
        public async Task<BaseResponse> ForgotPasswordAsync(string email)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (user == null)
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                };
            OtpHelper.GenerateOtp(email + RESET_PASSWORD_SUFFIX);
            return new BaseResponse
            {
                StatusCode = 200,
                Message = "SUCCESS",
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>BaseResponse with status 200, 400, 404</returns>
        public async Task<BaseResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new BaseResponse
                {
                    StatusCode = 404,
                    Message = "USER_NOT_FOUND",
                };
            if (!OtpHelper.VerifyOtp(request.Email + RESET_PASSWORD_SUFFIX, request.Otp))
            {
                return new BaseResponse
                {
                    StatusCode = 400,
                    Message = "INVALID_OTP",
                };
            }
            user.PasswordHash = StringHasher.HashWithSalt(request.NewPassword, user.PasswordSalt);
            OtpHelper.RemoveOtp(request.Email + RESET_PASSWORD_SUFFIX);
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return new BaseResponse
            {
                StatusCode = 200,
                Message = "PASSWORD_RESET_SUCCESS",
            };
        }

        #region Development Only
        public BaseResponse GetForgotPasswordOtpAsync(string email)
        {
            var otp = OtpHelper.GetOtpData(email + RESET_PASSWORD_SUFFIX);
            return new BaseResponse
            {
                StatusCode = otp != null ? 200 : 404,
                Message = otp != null ? "OTP_FOUND" : "OTP_NOT_FOUND",
                Data = otp
            };
        }
        #endregion
    }
}
