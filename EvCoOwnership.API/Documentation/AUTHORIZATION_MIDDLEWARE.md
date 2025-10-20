# Custom Authorization Middleware

This document explains the custom `AuthorizationMiddleware` and `AuthorizeRoles` attribute that provides bearer token validation and role-based authorization for the EV Co-ownership API.

## Overview

The custom authorization system consists of two main components:

1. **`AuthorizeRoles` Attribute**: Applied to controllers or actions to specify authorization requirements using strongly-typed `EUserRole` enum
2. **`AuthorizationMiddleware`**: Middleware that validates bearer tokens and checks user roles, using the standard `BaseResponse` class for consistent API responses

## Features

- ? **Bearer Token Validation**: Validates JWT tokens in Authorization headers
- ? **Type-Safe Role-Based Authorization**: Uses `EUserRole` enum for compile-time safety
- ? **Flexible Access Control**: Endpoints can require authentication only or specific roles
- ? **Public Endpoints**: Endpoints without the attribute are publicly accessible
- ? **Comprehensive Logging**: Detailed logging for debugging and monitoring
- ? **Standard HTTP Responses**: Returns appropriate 401/403/500 status codes
- ? **IntelliSense Support**: Full IDE support with autocomplete for role values

## How It Works

### 1. Endpoint Detection
The middleware checks each incoming request to see if the target controller action has the `[AuthorizeRoles]` attribute.

### 2. Authorization Logic
- **No Attribute**: Endpoint is publicly accessible (no authentication required)
- **Attribute with No Roles**: Authentication required, any valid token is accepted
- **Attribute with Roles**: Authentication required + user must have at least one of the specified roles

### 3. Token Validation
- Extracts bearer token from Authorization header
- Validates JWT signature, issuer, audience, and expiration
- Extracts user claims (ID, email, roles) from token

## Usage Examples

### 1. Public Endpoint (No Authentication Required)
```csharp
[HttpGet("public-info")]
public IActionResult GetPublicInfo()
{
    // No [AuthorizeRoles] attribute = publicly accessible
    return Ok("This is public information");
}
```

### 2. Authentication Only (Any Valid Token)
```csharp
[HttpGet("profile")]
[AuthorizeRoles] // No roles specified = authentication only
public IActionResult GetProfile()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Ok($"Profile for user {userId}");
}
```

### 3. Single Role Required
```csharp
[HttpDelete("users/{id}")]
[AuthorizeRoles(EUserRole.Admin)]
public IActionResult DeleteUser(int id)
{
    // Only Admin role can access this endpoint
    return Ok($"User {id} deleted");
}
```

### 4. Multiple Roles (OR Logic)
```csharp
[HttpGet("reports")]
[AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
public IActionResult GetReports()
{
    // Admin OR Staff role can access this endpoint
    return Ok("Reports data");
}
```

### 5. Controller-Level Authorization
```csharp
[Route("api/[controller]")]
[ApiController]
[AuthorizeRoles(EUserRole.Admin)] // Applies to all actions in controller
public class AdminController : ControllerBase
{
    [HttpGet("stats")]
    public IActionResult GetStats() 
    {
        // Inherits Admin role requirement from controller
        return Ok("Admin stats");
    }
    
    [HttpPost("backup")]
    [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)] // Overrides controller requirement
    public IActionResult CreateBackup()
    {
        // Admin OR Staff can access (overrides controller-level Admin-only)
        return Ok("Backup created");
    }
}
```

## Available Roles

The system supports the following roles based on the `EUserRole` enum from `EvCoOwnership.Repositories.Enums`:

- **`EUserRole.CoOwner`**: Regular users who can participate in vehicle co-ownership
- **`EUserRole.Staff`**: Staff members with elevated permissions
- **`EUserRole.Admin`**: Administrators with full system access

When using the `AuthorizeRoles` attribute, you must use the enum values instead of strings:

```csharp
// Correct usage with enum
[AuthorizeRoles(EUserRole.Admin)]
[AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]

// Don't forget to add the using statement
using EvCoOwnership.Repositories.Enums;
```

## Benefits of Using EUserRole Enum

### 1. Type Safety
```csharp
// ? Compile-time safety - invalid roles won't compile
[AuthorizeRoles(EUserRole.Admin)]

// ? This would cause a compilation error:
// [AuthorizeRoles(EUserRole.InvalidRole)] // Compilation error!

// ? vs. string-based approach where typos are runtime errors:
// [AuthorizeRoles("Adminn")] // Typo! Runtime error
```

### 2. IntelliSense Support
When typing `EUserRole.`, your IDE will show available options:
- `EUserRole.Admin`
- `EUserRole.Staff` 
- `EUserRole.CoOwner`

### 3. Refactoring Safety
If role names change in the enum, all usages are automatically updated during refactoring.

### 4. Centralized Role Definition
All roles are defined in one place: `EvCoOwnership.Repositories.Enums.EUserRole`

## Request/Response Examples

### Successful Request
```http
GET /api/coowner/eligibility
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

HTTP/1.1 200 OK
Content-Type: application/json

{
  "statusCode": 200,
  "message": "ELIGIBLE_FOR_CO_OWNERSHIP",
  "data": { ... }
}
```

### Missing Token
```http
GET /api/coowner/statistics

HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "statusCode": 401,
  "message": "AUTHORIZATION_HEADER_MISSING_OR_INVALID",
  "data": {
    "details": "Bearer token is required"
  }
}
```

### Invalid/Expired Token
```http
GET /api/coowner/statistics
Authorization: Bearer invalid.jwt.token

HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "statusCode": 401,
  "message": "INVALID_OR_EXPIRED_TOKEN",
  "data": {
    "details": "The provided token is invalid or expired"
  }
}
```

### Insufficient Permissions
```http
GET /api/coowner/statistics
Authorization: Bearer valid.coowner.token

HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "statusCode": 403,
  "message": "INSUFFICIENT_PERMISSIONS",
  "data": {
    "details": "Required roles: Admin, Staff"
  }
}
```

## Configuration

### 1. Middleware Registration
The middleware is registered in `Program.cs`:

```csharp
// Custom authorization middleware (must be before UseAuthentication and UseAuthorization)
app.UseMiddleware<AuthorizationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
```

### 2. JWT Settings
Configure JWT settings in `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "GoldShip",
    "Audience": "Trainer", 
    "SecretKey": "YourSecretKeyHere",
    "AccessTokenExpirationMinutes": 564,
    "RefreshTokenExpirationDays": 564
  }
}
```

## Skipped Paths

The middleware automatically skips authorization for certain paths:

- `/api/auth/*` (authentication endpoints)
- `/api/test/test-*` (basic test endpoints)
- `/swagger` (API documentation)
- `/health` (health check)
- `/favicon.ico`

## Testing Endpoints

Use these test endpoints to verify the authorization system:

### Test Authentication Only
```http
GET /api/test/test-auth-only
Authorization: Bearer your.jwt.token
```

### Test Admin Only
```http
GET /api/test/test-admin-only
Authorization: Bearer admin.jwt.token
```

### Test Admin or Staff
```http
GET /api/test/test-admin-or-staff
Authorization: Bearer admin.or.staff.token
```

### Test All Roles
```http
GET /api/test/test-all-roles
Authorization: Bearer any.valid.token
```

### Test Public Access
```http
GET /api/test/test-public
# No authorization header needed
```

## Error Handling

The middleware provides comprehensive error handling:

- **401 Unauthorized**: Missing, invalid, or expired tokens
- **403 Forbidden**: Valid token but insufficient permissions
- **500 Internal Server Error**: Unexpected errors during authorization

All errors include descriptive messages and additional details for debugging.

## Logging

The middleware logs authorization events at different levels:

- **Debug**: Successful authorization with user details
- **Warning**: Missing/invalid tokens, insufficient permissions
- **Error**: Unexpected errors during authorization process

Example log entries:
```
[Debug] User 123 (user@example.com) with roles [CoOwner] accessing /api/coowner/eligibility
[Warning] User 456 does not have required roles [Admin, Staff] for /api/coowner/statistics. User roles: [CoOwner]
[Error] Error during authorization validation for /api/coowner/statistics: Invalid JWT token
```

## Best Practices

### 1. Use Appropriate Authorization Levels
- **Public**: Information that doesn't require authentication
- **Authentication Only**: User-specific data that any authenticated user can access
- **Role-Based**: Administrative functions, privileged operations

### 2. Token Management
- Always use HTTPS in production
- Implement token refresh logic in client applications
- Store refresh tokens securely on client side
- Handle 401 responses by redirecting to login

### 3. Error Handling
- Check response status codes before processing data
- Implement retry logic for temporary failures
- Display user-friendly error messages for authorization failures

### 4. Security Considerations
- Validate tokens on both client and server side
- Use short-lived access tokens with refresh token rotation
- Implement rate limiting for authentication endpoints
- Monitor and log all authorization events for security auditing

## Migration from Built-in Authorization

To migrate from ASP.NET Core's built-in `[Authorize]` attribute:

### Before:
```csharp
[Authorize] // Authentication only
[Authorize(Roles = "Admin")] // Single role
[Authorize(Roles = "Admin,Staff")] // Multiple roles (comma-separated)
```

### After:
```csharp
[AuthorizeRoles] // Authentication only
[AuthorizeRoles(EUserRole.Admin)] // Single role
[AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)] // Multiple roles (separate parameters)

// Don't forget to add the using statement
using EvCoOwnership.Repositories.Enums;
```

The custom system provides the same functionality with additional logging and error handling capabilities, plus type safety with enums.