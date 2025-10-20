# Conventions

## Variable Naming
- Use `camelCase`for variables and parameters.
- Use `PascalCase` for public properties and methods.
- Use `ALL_CAPS` for constants.

## Api Commenting
- XML Content must be written using Markdown syntax.
- `summary` only includes roles
    - If the enpoint doesn't require authentication, leave it blank.
    - If the endpoint only requires authentication, use `User`.
- `param` section is used to describe parameters' & their constraints.
	- For parameters in request body, put parameter descriptions in the `remarks` section.
- `response` section is used for all possible responses. Each response code should include all possible messages

``` csharp
/// <summary>Admin</summary>
/// <remarks>
///
/// Create a new account.  
///
/// Parameters:
/// email: Required, valid email format.  
/// password: Required, minimum 8 characters, must contain uppercase, lowercase, number, and special character.  
/// confirmPassword: Must match the password.  
/// firstName: Required.  
/// lastName: Required.  
///
/// Sample request:
///
/// POST /api/accounts  
/// {  
/// "email": "user@example.com",  
/// "password": "Password123!",  
/// "confirmPassword": "Password123!",  
/// "firstName": "John",  
/// "lastName": "Doe"  
/// }
/// </remarks>
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
```

## Mapping

Put all mappings between DTOs & Entities in the `Mapping` folder of the Services project. Separate mappers by entity. For example, for `Account` entity, create `AccountMappers.cs` file.

```csharp
  public static class AccountMappers
    {
        #region DTO to Entity

        #endregion

        #region Entity to DTO
        public static AccountResponse ToResponse(this Account entity)
        {
            return new AccountResponse
            {
                Id = entity.Id,
                Email = entity.Email,
                Role = entity.RoleEnum,
                FullName = entity.FullName,
                Phone = entity.Phone,
                DateOfBirth = entity.DateOfBirth,
                AvatarUrl = entity.AvatarUrl,
                IsActive = entity.IsActive,
                EmailVerified = entity.EmailVerified
            };
        }
        #endregion
    }
```

## Validation

Create FluentValidation validators under the Request DTOs.
```csharp

public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("EMAIL_REQUIRED")
                .EmailAddress().WithMessage("INVALID_EMAIL_FORMAT");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("PASSWORD_REQUIRED")
                .MinimumLength(8).WithMessage("PASSWORD_MIN_8_CHARACTERS");
        }
    }

```

## Exception handling
Do not catch generic exceptions (e.g `Exception`). Catch specific exceptions only when necessary.
`ExceptionMiddleware` handles all unhandled exceptions and logs them.`

