using EvCoOwnership.API.Attributes;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Helpers.Helpers;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

namespace EvCoOwnership.API.Middlewares
{
    /// <summary>
    /// Middleware for handling custom authorization based on AuthorizeRoles attribute
    /// Checks bearer tokens and validates user roles against endpoint requirements
    /// </summary>
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public AuthorizationMiddleware(
            RequestDelegate next, 
            ILogger<AuthorizationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authorization for certain paths
            if (ShouldSkipAuthorization(context))
            {
                await _next(context);
                return;
            }

            // Get the endpoint and check for AuthorizeRoles attribute
            var endpoint = context.GetEndpoint();
            var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            
            if (controllerActionDescriptor == null)
            {
                await _next(context);
                return;
            }

            // Check for AuthorizeRoles attribute on action first, then controller
            var authorizeAttribute = GetAuthorizeRolesAttribute(controllerActionDescriptor);
            
            if (authorizeAttribute == null)
            {
                // No AuthorizeRoles attribute found, endpoint doesn't need authentication
                await _next(context);
                return;
            }

            // Extract and validate bearer token
            var (authorizationResponse, principal) = await ValidateAuthorizationAsync(context, authorizeAttribute);
            
            if (authorizationResponse.StatusCode != 200)
            {
                await WriteUnauthorizedResponseAsync(context, authorizationResponse);
                return;
            }

            // Add user claims to context for downstream middleware/controllers
            if (principal != null)
            {
                context.User = principal;
            }

            await _next(context);
        }

        /// <summary>
        /// Determines if authorization should be skipped for certain paths
        /// </summary>
        private static bool ShouldSkipAuthorization(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            
            // Skip for certain paths
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register", 
                "/api/auth/refresh-token",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/test/test-api",
                "/api/test/test-exception-middleware",
                "/api/test/test-base-response",
                "/swagger",
                "/health",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
        }

        /// <summary>
        /// Gets the AuthorizeRoles attribute from action or controller
        /// </summary>
        private static AuthorizeRolesAttribute? GetAuthorizeRolesAttribute(ControllerActionDescriptor actionDescriptor)
        {
            // Check action method first
            var actionAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AuthorizeRolesAttribute>();
            if (actionAttribute != null)
                return actionAttribute;

            // Check controller if not found on action
            var controllerAttribute = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<AuthorizeRolesAttribute>();
            return controllerAttribute;
        }

        /// <summary>
        /// Validates the authorization header and user roles
        /// </summary>
        private async Task<(BaseResponse Response, ClaimsPrincipal? Principal)> ValidateAuthorizationAsync(
            HttpContext context, 
            AuthorizeRolesAttribute authorizeAttribute)
        {
            try
            {
                // Extract bearer token
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Missing or invalid Authorization header for {Path}", context.Request.Path);
                    return (new BaseResponse
                    {
                        StatusCode = 401,
                        Message = "UNAUTHORIZED",
                        Errors = new { Details = "Bearer token is required" }
                    }, null);
                }

                var token = authHeader["Bearer ".Length..].Trim();
                
                // Validate JWT token
                var principal = JwtHelper.ValidateToken(token, _configuration);
                if (principal == null)
                {
                    _logger.LogWarning("Invalid JWT token for {Path}", context.Request.Path);
                    return (new BaseResponse
                    {
                        StatusCode = 401,
                        Message = "INVALID_OR_EXPIRED_TOKEN",
                        Errors = new { Details = "The provided token is invalid or expired" }
                    }, null);
                }

                // Extract user roles from token
                var userRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;

                _logger.LogDebug("User {UserId} ({Email}) with roles [{Roles}] accessing {Path}", 
                    userId, userEmail, string.Join(", ", userRoles), context.Request.Path);

                // Check if user has required roles
                if (!authorizeAttribute.HasRequiredRoles(userRoles))
                {
                    _logger.LogWarning("User {UserId} does not have required roles [{RequiredRoles}] for {Path}. User roles: [{UserRoles}]",
                        userId, authorizeAttribute.GetRequiredRolesString(), context.Request.Path, string.Join(", ", userRoles));
                    
                    return (new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "INSUFFICIENT_PERMISSIONS",
                        Errors = new { Details = $"Required roles: {authorizeAttribute.GetRequiredRolesString()}" }
                    }, null);
                }

                return (new BaseResponse
                {
                    StatusCode = 200,
                    Message = "AUTHORIZATION_SUCCESS",
                    Errors = new { UserId = userId, Email = userEmail, Roles = userRoles }
                }, principal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authorization validation for {Path}", context.Request.Path);
                return (new BaseResponse
                {
                    StatusCode = 500,
                    Message = "AUTHORIZATION_ERROR",
                    Errors = new { Details = "An error occurred during authorization validation" }
                }, null);
            }
        }

        /// <summary>
        /// Writes unauthorized response to the HTTP context
        /// </summary>
        private static async Task WriteUnauthorizedResponseAsync(HttpContext context, BaseResponse response)
        {
            context.Response.StatusCode = response.StatusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}