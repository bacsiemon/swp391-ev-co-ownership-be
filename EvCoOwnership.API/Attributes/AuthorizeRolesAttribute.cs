using EvCoOwnership.Repositories.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Attributes
{
    /// <summary>
    /// Attribute to specify required roles for controller actions/endpoints
    /// If no roles are specified, only authentication is required
    /// If roles are specified, the user must have at least one of the specified roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRolesAttribute : Attribute
    {
        /// <summary>
        /// Required roles for accessing the endpoint
        /// If empty, only authentication is required
        /// </summary>
        public EUserRole[] Roles { get; }

        /// <summary>
        /// Creates an AuthorizeRoles attribute with no specific role requirements (authentication only)
        /// </summary>
        public AuthorizeRolesAttribute()
        {
            Roles = Array.Empty<EUserRole>();
        }

        /// <summary>
        /// Creates an AuthorizeRoles attribute with specific role requirements
        /// </summary>
        /// <param name="roles">Required roles (user must have at least one)</param>
        public AuthorizeRolesAttribute(params EUserRole[] roles)
        {
            Roles = roles ?? Array.Empty<EUserRole>();
        }

        /// <summary>
        /// Checks if the user has any of the required roles
        /// </summary>
        /// <param name="userRoles">User's roles from the JWT token (as strings)</param>
        /// <returns>True if user has required roles or no roles are specified</returns>
        public bool HasRequiredRoles(IEnumerable<string> userRoles)
        {
            // If no roles specified, only authentication is required
            if (Roles.Length == 0)
                return true;

            // Convert enum roles to strings and check if user has any of the required roles
            var requiredRoleStrings = Roles.Select(r => r.ToString());
            return requiredRoleStrings.Any(requiredRole => 
                userRoles.Any(userRole => 
                    string.Equals(userRole, requiredRole, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Checks if the user has any of the required roles using enum comparison
        /// </summary>
        /// <param name="userRoles">User's roles as enums</param>
        /// <returns>True if user has required roles or no roles are specified</returns>
        public bool HasRequiredRoles(IEnumerable<EUserRole> userRoles)
        {
            // If no roles specified, only authentication is required
            if (Roles.Length == 0)
                return true;

            // Check if user has any of the required roles using enum comparison
            return Roles.Any(requiredRole => userRoles.Contains(requiredRole));
        }

        /// <summary>
        /// Gets a formatted string of required roles for error messages
        /// </summary>
        /// <returns>Comma-separated list of required roles</returns>
        public string GetRequiredRolesString()
        {
            return Roles.Length == 0 ? "Authentication only" : string.Join(", ", Roles.Select(r => r.ToString()));
        }
    }
}