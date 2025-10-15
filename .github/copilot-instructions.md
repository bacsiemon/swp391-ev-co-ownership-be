# EV Co-Ownership System - AI Coding Instructions

## Architecture Overview

This is a **clean architecture** ASP.NET Core Web API for an electric vehicle co-ownership and cost-sharing system using **PostgreSQL** with **Entity Framework Core**. The solution follows a **layered approach** with clear separation of concerns:

```
├── EvCoOwnership.API (Presentation Layer)
├── EvCoOwnership.Services (Business Logic Layer) 
├── EvCoOwnership.Repositories (Data Access Layer)
├── EvCoOwnership.DTOs (Data Transfer Objects)
└── EvCoOwnership.Helpers (Utilities & Base Classes)
```

## Key Architectural Patterns

### Dependency Injection Structure
Each layer has its own configuration class for DI registration:
- `ApiConfigurations.AddApiConfigurations()` - JWT auth, Swagger setup
- `ServiceConfigurations.AddServiceConfigurations()` - Business services
- `RepositoryConfigurations.AddRepositoryConfigurations()` - Data layer, DbContext
- `HelperConfigurations.AddHelperConfigurations()` - Utilities

### Unit of Work Pattern
The system uses **UoW pattern** with repository interfaces. All repositories are accessed through `IUnitOfWork`:
```csharp
// Example usage in services
var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
await _unitOfWork.SaveChangesAsync();
```

### Response Standardization
All API responses use `BaseResponse<T>` with consistent structure:
```csharp
public class BaseResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public object? AdditionalData { get; set; }
    public object? Errors { get; set; }
}
```

## Critical Development Workflows

### Database Operations
- **Scaffold**: Use `dotnet ef dbcontext scaffold` with Npgsql provider
- **Connection string**: Located in `EvCoOwnership.API/appsettings.json`
- **Post-scaffold**: Convert `int` enum fields to proper enum types manually
- **Migrations**: Run from `EvCoOwnership.Repositories` project

### Authentication Flow
- **JWT + Refresh Token** system implemented
- Use `[Authorize]` for protected endpoints
- Bearer token configuration in `ApiConfigurations.AddJwtAuthentication()`
- All auth endpoints return consistent `BaseResponse` format

### Validation System
**Custom FluentValidation** implementation (NOT using deprecated AspNetCore package):
- Validators auto-registered from DTOs assembly
- `ValidationActionFilter` intercepts requests automatically
- Returns `BaseResponse` format on validation errors
- Use `[SkipValidation]` attribute to bypass validation

## Project-Specific Conventions

### API Documentation Standards
Follow the XML comment pattern in `README_coding_conventions.md`:
- `<summary>` contains **role requirements only** (Admin/User/blank)
- `<remarks>` contains parameter descriptions and sample requests
- `<response>` lists ALL possible status codes and messages
- Use **Markdown syntax** in XML comments

### Controller Patterns
Return appropriate status codes using switch expressions:
```csharp
return response.StatusCode switch
{
    200 => Ok(response),
    400 => BadRequest(response),
    403 => StatusCode(403, response),
    _ => NoContent()
};
```

### Logging with Serilog
- Structured logging configured with **console + file output**
- Request logging middleware automatically captures HTTP details  
- Use `ILogger<T>` for service-level logging
- Log files rotate daily in `/logs` directory

### File Structure Standards
- Controllers: Single responsibility, thin controllers
- Services: Business logic implementation
- Repositories: Data access only, inherit from generic base
- DTOs: Separate request/response models with FluentValidation validators
- Models: EF Core entities (generated via scaffold)

## Integration Points

### External Dependencies
- **PostgreSQL** via Npgsql.EntityFrameworkCore.PostgreSQL
- **JWT** authentication with SymmetricSecurityKey
- **Serilog** for structured logging
- **FluentValidation** for DTO validation

### SignalR (Current Branch: feat-signalr-notification)
The system appears to be implementing real-time notification features using SignalR.

## Development Commands

### Build & Run
```bash
# From solution root
dotnet build
dotnet run --project EvCoOwnership.API

# Database scaffold (from Repositories project)
dotnet ef dbcontext scaffold "ConnectionString" Npgsql.EntityFrameworkCore.PostgreSQL -o Models -f
```

### Testing Database Connection
Use the PowerShell script: `test-db-connection.ps1` (configure as needed)

## Common Gotchas

1. **Enum Handling**: After EF scaffold, manually convert enum fields from `int` to proper enum types
2. **Validation**: Custom FluentValidation setup - don't use the deprecated AspNetCore package
3. **Configuration**: Each layer has its own DI configuration method - follow the pattern
4. **Responses**: Always return `BaseResponse<T>` for consistency
5. **Auth**: JWT configuration requires all three: SecretKey, Issuer, and Audience in appsettings