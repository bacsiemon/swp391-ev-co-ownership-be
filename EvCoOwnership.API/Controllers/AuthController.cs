using EvCoOwnership.DTOs.AuthDTOs;
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

        /// <summary>
        /// Authenticates user with email and password
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <remarks>
        /// Possible messages:  
        /// >LOGIN_SUCCESS  
        /// >INVALID_EMAIL_OR_PASSWORD  
        /// >ACCOUNT_SUSPENDED  
        /// >ACCOUNT_INACTIVE
        /// </remarks>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid credentials or validation error</response>
        /// <response code="403">Account suspended or inactive</response>
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

        /// <summary>
        /// Registers a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <remarks>
        /// Possible messages:  
        /// >REGISTRATION_SUCCESS  
        /// >EMAIL_ALREADY_EXISTS  
        /// >EMAIL_REQUIRED  
        /// >INVALID_EMAIL_FORMAT  
        /// >PASSWORD_REQUIRED  
        /// >PASSWORD_MIN_8_CHARACTERS  
        /// >PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL  
        /// >CONFIRM_PASSWORD_MUST_MATCH  
        /// >FIRST_NAME_REQUIRED  
        /// >LAST_NAME_REQUIRED
        /// </remarks>
        /// <response code="201">Registration successful</response>
        /// <response code="400">Validation error</response>
        /// <response code="409">Email already exists</response>
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

        /// <summary>
        /// Refreshes the access token using a valid refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <remarks>
        /// Possible messages:  
        /// >TOKEN_REFRESH_SUCCESS  
        /// >INVALID_OR_EXPIRED_REFRESH_TOKEN  
        /// >USER_NOT_FOUND  
        /// >ACCOUNT_SUSPENDED  
        /// >ACCOUNT_INACTIVE
        /// </remarks>
        /// <response code="200">Token refresh successful</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Invalid or expired refresh token</response>
        /// <response code="403">Account suspended or inactive</response>
        /// <response code="404">User not found</response>
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
