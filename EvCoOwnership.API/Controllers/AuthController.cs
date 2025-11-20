using EvCoOwnership.Repositories.DTOs.AuthDTOs;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Authentication controller for user login, registration and password management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the AuthController
        /// </summary>
        /// <param name="authService">Authentication service</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <param name="request">Login credentials</param>
        /// <response code="200">Login successful. Possible messages:  
        /// - LOGIN_SUCCESS  
        /// </response>
        /// <response code="400">Invalid credentials or validation error. Possible messages:  
        /// - INVALID_EMAIL_OR_PASSWORD  
        /// </response>
        /// <response code="403">Account suspended or inactive. Possible messages:  
        /// - ACCOUNT_SUSPENDED  
        /// - ACCOUNT_INACTIVE  
        /// </response>
        /// <remarks>
        /// Authenticates user with email and password
        /// </remarks>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                _ => NoContent()
            };
        }

        /// <response code="201">Registration successful. Possible messages:  
        /// - REGISTRATION_SUCCESS  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - EMAIL_REQUIRED  
        /// - INVALID_EMAIL_FORMAT  
        /// - PASSWORD_REQUIRED  
        /// - PASSWORD_MIN_8_CHARACTERS  
        /// - PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL  
        /// - CONFIRM_PASSWORD_MUST_MATCH  
        /// - FIRST_NAME_REQUIRED  
        /// - LAST_NAME_REQUIRED  
        /// </response>
        /// <response code="409">Email already exists. Possible messages:  
        /// - EMAIL_ALREADY_EXISTS  
        /// </response>
        /// <remarks>
        /// Registers a new user account
        /// </remarks>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                409 => Conflict(response),
                _ => NoContent()
            };
        }

        /// <response code="200">Token refresh successful. Possible messages:  
        /// - TOKEN_REFRESH_SUCCESS  
        /// </response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Invalid or expired refresh token. Possible messages:  
        /// - INVALID_OR_EXPIRED_REFRESH_TOKEN  
        /// </response>
        /// <response code="403">Account suspended or inactive. Possible messages:  
        /// - ACCOUNT_SUSPENDED  
        /// - ACCOUNT_INACTIVE  
        /// </response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Refreshes the access token using a valid refresh token
        /// </remarks>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => NoContent()
            };
        }

        /// <response code="200">Success. Possible messages:  
        /// - SUCCESS  
        /// </response>
        /// <response code="404">Email not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Generates an OTP and sends it to the user's email for password reset.
        /// </remarks>
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

        /// <response code="200">Success. Possible messages:  
        /// - SUCCESS  
        /// </response>
        /// <response code="400">Validation Error. Possible messages:  
        /// - EMAIL_REQUIRED  
        /// - INVALID_EMAIL_FORMAT  
        /// - OTP_MIN_6_CHARACTERS  
        /// - NEW_PASSWORD_MIN_8_CHARACTERS  
        /// </response>
        /// <response code="404">Email not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Resets the user's password using the provided OTP.
        /// </remarks>
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

        /// <summary>
        /// Verifies a driving license (basic version through AuthService)
        /// </summary>
        /// <param name="request">License verification request</param>
        /// <response code="200">License verification successful. Possible messages:  
        /// - LICENSE_VERIFICATION_SUCCESS  
        /// </response>
        /// <response code="400">Validation error or license verification failed. Possible messages:  
        /// - INVALID_LICENSE_FORMAT  
        /// - LICENSE_NUMBER_REQUIRED  
        /// - ISSUE_DATE_REQUIRED  
        /// - FIRST_NAME_REQUIRED  
        /// - LAST_NAME_REQUIRED  
        /// </response>
        /// <response code="409">License already registered. Possible messages:  
        /// - LICENSE_ALREADY_REGISTERED  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Basic license verification through AuthService. For advanced features, use the dedicated License controller.
        /// </remarks>
        [HttpPost("verify-license")]
        public async Task<IActionResult> VerifyLicense([FromBody] VerifyLicenseRequest request)
        {
            var response = await _authService.VerifyLicenseAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
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
            var response = _authService.GetForgotPasswordOtpAsync(email);
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
