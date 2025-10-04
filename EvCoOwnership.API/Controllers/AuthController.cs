using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Generates an OTP and sends it to the user's email for password reset.
        /// </summary>
        /// <remarks>
        /// Possible messages:  
        /// >SUCCESS  
        /// >USER_NOT_FOUND
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="404">Email not found</response>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var response = await _authService.ForgotPasswordAsync(request.Email);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => NoContent()
            };
        }

        /// <summary>
        /// Resets the user's password using the provided OTP.
        /// </summary>
        /// <remarks>
        /// Possible messages:  
        /// >SUCCESS  
        /// >EMAIL_REQUIRED  
        /// >INVALID_EMAIL_FORMAT  
        /// >OTP_MIN_6_CHARACTERS  
        /// >NEW_PASSWORD_MIN_8_CHARACTERS  
        /// >USER_NOT_FOUND  
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">Validation Error</response>
        /// <response code="404">Email not found</response>
        [HttpPatch("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var response = await _authService.ResetPasswordAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => NoContent()
            };
        }

        #region Development only
        /// <summary>
        /// Gets the generated OTP for the given email
        /// </summary>
        /// <remarks>
        /// This endpoint is for testing purposes only.
        /// </remarks>
        /// <param name="email"></param>
        /// <response code="200">Success</response>
        /// <response code="404">OTP not found</response>
        [HttpGet("test/get-forgot-password-otp")]
        public IActionResult TestGetForgotPasswordOtp([FromQuery] string email)
        {
            var response  = _authService.GetForgotPasswordOtpAsync(email);
            return response switch
            {
                { StatusCode: 200 } => Ok(response),
                { StatusCode: 404 } => NotFound(response),
                _ => NoContent()
            };
        }
        #endregion
    }
}
