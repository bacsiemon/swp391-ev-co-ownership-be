# Authentication API Documentation

## Overview

The EV Co-Ownership authentication system provides secure user registration, login, and token management functionality using JWT tokens with refresh token support.

## Features

- ✅ User Registration with validation
- ✅ User Login with email/password
- ✅ JWT Access Token & Refresh Token
- ✅ Password Reset with OTP
- ✅ Account status management (Active/Inactive/Suspended)
- ✅ Role-based authentication
- ✅ Automatic FluentValidation

## API Endpoints

### 1. Register User
**POST** `/api/auth/register`

Registers a new user account with CoOwner role by default.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+1234567890",
  "dateOfBirth": "1990-01-01",
  "address": "123 Main Street, City, Country"
}
```

**Response (201 Created):**
```json
{
  "statusCode": 201,
  "message": "REGISTRATION_SUCCESS",
  "data": {
    "userId": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

**Validation Rules:**
- Email: Required, valid email format, max 255 characters
- Password: Required, min 8 characters, must contain uppercase, lowercase, number, and special character
- ConfirmPassword: Must match password
- FirstName/LastName: Required, max 50 characters, letters and spaces only
- Phone: Optional, valid international format
- DateOfBirth: Optional, must be at least 16 years old
- Address: Optional, max 500 characters

### 2. Login User
**POST** `/api/auth/login`

Authenticates user with email and password, returns JWT tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "LOGIN_SUCCESS",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64RefreshToken...",
    "accessTokenExpiresAt": "2025-10-04T15:30:00Z",
    "refreshTokenExpiresAt": "2025-12-02T14:30:00Z",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "phone": "+1234567890",
      "dateOfBirth": "1990-01-01",
      "address": "123 Main Street, City, Country",
      "profileImageUrl": null,
      "status": "Active",
      "roles": ["CoOwner"]
    }
  }
}
```

### 3. Refresh Token
**POST** `/api/auth/refresh-token`

Refreshes the access token using a valid refresh token.

**Request Body:**
```json
{
  "refreshToken": "base64RefreshToken..."
}
```

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "TOKEN_REFRESH_SUCCESS",
  "data": {
    "accessToken": "newAccessToken...",
    "refreshToken": "newRefreshToken...",
    "accessTokenExpiresAt": "2025-10-04T16:30:00Z",
    "refreshTokenExpiresAt": "2025-12-02T15:30:00Z",
    "user": { ... }
  }
}
```

### 4. Forgot Password
**POST** `/api/auth/forgot-password`

Generates and sends OTP for password reset.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "SUCCESS"
}
```

### 5. Reset Password
**PATCH** `/api/auth/reset-password`

Resets user password using OTP verification.

**Request Body:**
```json
{
  "email": "user@example.com",
  "otp": "123456",
  "newPassword": "NewSecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "PASSWORD_RESET_SUCCESS"
}
```

### 6. Get OTP (Development Only)
**GET** `/api/auth/test/get-forgot-password-otp?email=user@example.com`

Gets the generated OTP for testing purposes.

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "OTP_FOUND",
  "data": {
    "otp": "123456"
  }
}
```

## Error Responses

### Validation Errors (400 Bad Request)
```json
{
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "errors": {
    "Email": ["INVALID_EMAIL_FORMAT"],
    "Password": ["PASSWORD_MIN_8_CHARACTERS"]
  }
}
```

### Business Logic Errors
```json
{
  "statusCode": 409,
  "message": "EMAIL_ALREADY_EXISTS"
}
```

## JWT Token Usage

### Authorization Header
Include the access token in the Authorization header for protected endpoints:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Claims
The JWT access token contains the following claims:
- `nameid`: User ID
- `email`: User email
- `name`: Full name
- `FirstName`: First name
- `LastName`: Last name
- `role`: User roles (CoOwner, Staff, Admin)

### Token Expiration
- **Access Token**: 564 minutes (configurable)
- **Refresh Token**: 564 days (configurable)

## Security Features

### Password Security
- Passwords are hashed using SHA256 with random salt
- Salt is generated using cryptographically secure random number generator
- Each user has a unique salt stored separately

### Token Security
- JWT tokens are signed with HMAC-SHA256
- Refresh tokens are cryptographically secure random strings
- Expired refresh tokens are automatically cleaned up

### Account Security
- Account status checking (Active/Inactive/Suspended)
- Failed login attempts protection (through invalid credentials response)
- OTP-based password reset

## Database Schema

### Users Table
- `Id`: Primary key
- `Email`: Unique email address
- `NormalizedEmail`: Uppercase email for case-insensitive search
- `PasswordHash`: Hashed password
- `PasswordSalt`: Unique salt for password hashing
- `FirstName`, `LastName`: User names
- `Phone`, `DateOfBirth`, `Address`: Optional profile data
- `ProfileImageUrl`: Optional profile image
- `StatusEnum`: Account status (Active/Inactive/Suspended)
- `CreatedAt`, `UpdatedAt`: Timestamps

### UserRefreshTokens Table
- `UserId`: Foreign key to Users
- `RefreshToken`: Unique refresh token
- `ExpiresAt`: Token expiration timestamp

### Roles Table
- `Id`: Primary key
- `RoleNameEnum`: Role type (CoOwner/Staff/Admin)

### CoOwners Table
- `UserId`: Foreign key to Users (one-to-one)
- `CreatedAt`, `UpdatedAt`: Timestamps

## Configuration

### JWT Settings (appsettings.json)
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

## Testing

### Sample Test Cases

1. **Successful Registration**
   ```bash
   curl -X POST "https://localhost:7000/api/auth/register" \
   -H "Content-Type: application/json" \
   -d '{
     "email": "test@example.com",
     "password": "SecurePass123!",
     "confirmPassword": "SecurePass123!",
     "firstName": "Test",
     "lastName": "User"
   }'
   ```

2. **Successful Login**
   ```bash
   curl -X POST "https://localhost:7000/api/auth/login" \
   -H "Content-Type: application/json" \
   -d '{
     "email": "test@example.com",
     "password": "SecurePass123!"
   }'
   ```

3. **Refresh Token**
   ```bash
   curl -X POST "https://localhost:7000/api/auth/refresh-token" \
   -H "Content-Type: application/json" \
   -d '{
     "refreshToken": "your-refresh-token-here"
   }'
   ```

## Error Messages Reference

### Authentication Errors
- `INVALID_EMAIL_OR_PASSWORD`: Login credentials are incorrect
- `ACCOUNT_SUSPENDED`: User account is suspended
- `ACCOUNT_INACTIVE`: User account is inactive
- `INVALID_OR_EXPIRED_REFRESH_TOKEN`: Refresh token is invalid or expired

### Registration Errors
- `EMAIL_ALREADY_EXISTS`: Email is already registered
- `EMAIL_REQUIRED`: Email field is required
- `INVALID_EMAIL_FORMAT`: Email format is invalid
- `PASSWORD_MIN_8_CHARACTERS`: Password must be at least 8 characters
- `PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL`: Password complexity requirement
- `CONFIRM_PASSWORD_MUST_MATCH`: Password confirmation doesn't match

### Password Reset Errors
- `USER_NOT_FOUND`: Email not found in system
- `INVALID_OTP`: OTP is incorrect or expired
- `OTP_MIN_6_CHARACTERS`: OTP must be 6 characters

## Best Practices

1. **Always use HTTPS** in production
2. **Store refresh tokens securely** on client side
3. **Implement token refresh logic** before access token expires
4. **Handle 401 responses** by redirecting to login
5. **Validate all user inputs** on client side before sending
6. **Use environment variables** for JWT secret keys
7. **Implement rate limiting** for authentication endpoints
8. **Log authentication events** for security monitoring

## Development Notes

- FluentValidation is automatically applied to all DTOs
- Use `[SkipValidation]` attribute to bypass validation for specific endpoints
- OTP helper generates 6-digit codes valid for 5 minutes
- Default user role is CoOwner upon registration
- Email normalization ensures case-insensitive email matching