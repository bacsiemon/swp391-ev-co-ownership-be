# FluentValidation Auto-Validation Implementation

This implementation provides automatic FluentValidation for DTOs without using the deprecated `FluentValidation.AspNetCore` package. The solution uses a custom action filter approach that integrates seamlessly with the existing project structure.

## Features

? **Automatic Validation**: All DTOs with corresponding validators are automatically validated
? **Custom Error Format**: Returns consistent `BaseResponse` format with validation errors
? **Selective Validation**: Use attributes to skip validation on specific actions/controllers
? **Comprehensive Logging**: Detailed logging of validation process and results
? **Performance Optimized**: Skips validation for primitive types and framework types
? **Easy Integration**: Simple registration in `Program.cs`

## How It Works

### 1. Custom Action Filter
- `ValidationActionFilter` intercepts all action executions
- Automatically detects DTOs that have FluentValidation validators
- Validates request objects before action execution
- Returns `BaseResponse` with validation errors if validation fails

### 2. Dependency Injection
- All validators are automatically registered from the DTOs assembly
- Uses `FluentValidation.DependencyInjectionExtensions` for registration
- Filter is registered globally via MVC options

### 3. Error Handling
- Validation errors are returned as `400 Bad Request`
- Uses the existing `BaseResponse` class for consistent API responses
- Includes both summary message and detailed field-level errors

## Installation & Setup

### 1. Packages Added
```xml
<!-- In EvCoOwnership.DTOs -->
<PackageReference Include="FluentValidation" Version="12.0.0" />

<!-- In EvCoOwnership.API -->
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.0.0" />
```

### 2. Service Registration
```csharp
// In Program.cs
builder.Services.AddFluentValidationServices();
```

## Usage Examples

### 1. Create a DTO with Validator

```csharp
// DTO
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

// Validator
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("NAME_REQUIRED")
            .MinimumLength(2).WithMessage("NAME_MIN_2_CHARACTERS");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("EMAIL_REQUIRED")
            .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("AGE_MUST_BE_POSITIVE");
    }
}
```

### 2. Controller Action (No Additional Code Needed)

```csharp
[HttpPost]
public IActionResult CreateUser([FromBody] CreateUserRequest request)
{
    // Validation happens automatically before this code runs
    // If validation fails, a 400 response is returned automatically
    
    // Your business logic here
    return Ok("User created successfully");
}
```

### 3. Skip Validation (When Needed)

```csharp
[HttpPost("skip-validation")]
[SkipValidation]
public IActionResult SomeAction([FromBody] CreateUserRequest request)
{
    // Validation is skipped for this action
    return Ok("Validation skipped");
}
```

## Response Format

### Successful Validation
```json
{
  "statusCode": 200,
  "isSuccess": true,
  "message": "Success",
  "data": { ... }
}
```

### Validation Failure
```json
{
  "statusCode": 400,
  "isSuccess": false,
  "message": "NAME_REQUIRED, INVALID_EMAIL_FORMAT",
  "data": [
    {
      "field": "Name",
      "error": "NAME_REQUIRED"
    },
    {
      "field": "Email", 
      "error": "INVALID_EMAIL_FORMAT"
    }
  ]
}
```

## Test Endpoints

The following test endpoints are available to verify the implementation:

1. **`POST /api/test/test-fluentvalidation`**
   - Tests custom validation DTO
   - Try invalid data to see validation errors

2. **`POST /api/test/test-auth-validation`** 
   - Tests existing `ForgotPasswordRequest` validation
   - Validates email format

3. **`POST /api/test/test-skip-validation`**
   - Demonstrates validation skipping
   - Accepts invalid data without errors

## File Structure

```
EvCoOwnership.API/
??? Attributes/
?   ??? ValidationAttributes.cs      # [SkipValidation] attribute
??? Extensions/
?   ??? FluentValidationExtensions.cs # Service registration
??? Filters/
?   ??? ValidationActionFilter.cs    # Main validation logic
??? Controllers/
    ??? TestController.cs            # Test endpoints

EvCoOwnership.DTOs/
??? AuthDTOs/
?   ??? ForgotPasswordRequest.cs     # Existing DTO with validator
?   ??? ResetPasswordRequest.cs      # Existing DTO with validator
??? TestDTOs/
    ??? TestValidationRequest.cs     # Test DTO with validator
```

## Benefits Over FluentValidation.AspNetCore

1. **No Deprecated Dependencies**: Uses only supported packages
2. **Full Control**: Custom error handling and response format
3. **Better Integration**: Works seamlessly with existing `BaseResponse` pattern
4. **Selective Validation**: Easy to disable validation when needed
5. **Comprehensive Logging**: Built-in logging for debugging and monitoring
6. **Performance Optimized**: Smart filtering of types that don't need validation

## Migration Notes

- Existing validators (`ForgotPasswordRequestValidator`, `ResetPasswordRequestValidator`) work without changes
- No changes needed to existing controller actions
- All validation is now automatic and consistent
- Error messages maintain the same format as before

## Troubleshooting

### Validation Not Working
- Ensure validator is in the DTOs assembly
- Verify validator class naming convention: `{DTO}Validator`
- Check that the DTO is being passed as a parameter (not primitive type)

### Performance Issues
- The filter automatically skips primitive types
- Complex validation rules should be optimized in the validator itself
- Consider using `[SkipValidation]` for high-frequency endpoints that don't need validation

### Custom Error Messages
- Use `.WithMessage()` in validators for custom error messages
- Error messages support placeholders: `.WithMessage("'{PropertyName}' must be between {MinLength} and {MaxLength} characters")`