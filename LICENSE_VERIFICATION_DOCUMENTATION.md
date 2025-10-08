# License Verification System Documentation

## Overview

The License Verification System provides comprehensive functionality for verifying, managing, and validating driving licenses in the EV Co-Ownership platform. This system supports Vietnamese driving license formats and integrates with the existing authentication infrastructure.

## Features

- ✅ **License Verification**: Verify driving licenses against mock government databases
- ✅ **Format Validation**: Support for Vietnamese driving license formats
- ✅ **Duplicate Prevention**: Prevent registration of already existing licenses
- ✅ **Image Upload**: Support license image uploads for verification
- ✅ **Age Validation**: Ensure license holders meet minimum age requirements
- ✅ **Expiry Tracking**: Track license expiry dates and notifications
- ✅ **Role-based Access**: Different access levels for users, staff, and administrators
- ✅ **Audit Trail**: Complete logging of verification activities

## Architecture

### Components

1. **Controllers**
   - `AuthController`: Basic license verification through auth endpoints
   - `LicenseController`: Advanced license management and verification

2. **Services**
   - `AuthService`: Basic license verification integration
   - `LicenseVerificationService`: Comprehensive license management

3. **DTOs**
   - `VerifyLicenseRequest`: Request model for license verification
   - `VerifyLicenseResponse`: Response model with verification results
   - `LicenseDetails`: Detailed license information model

4. **Repositories**
   - `DrivingLicenseRepository`: Database operations for driving licenses

5. **Mapping**
   - `LicenseMapper`: Extension methods for entity-DTO mapping

## API Endpoints

### Authentication Controller

#### POST /api/auth/verify-license
Basic license verification through the authentication system.

**Request Body:**
```json
{
  "licenseNumber": "123456789",
  "issueDate": "2020-01-15",
  "issuedBy": "HO CHI MINH",
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1990-05-20",
  "licenseImage": "file upload (optional)"
}
```

**Response (200 OK):**
```json
{
  "statusCode": 200,
  "message": "LICENSE_VERIFICATION_SUCCESS",
  "data": {
    "isValid": true,
    "status": "VERIFIED",
    "message": "License verification successful",
    "licenseDetails": {
      "licenseNumber": "123456789",
      "holderName": "John Doe",
      "issueDate": "2020-01-15",
      "expiryDate": "2030-01-15",
      "issuedBy": "HO CHI MINH",
      "status": "ACTIVE",
      "licenseClass": "B"
    },
    "verifiedAt": "2024-01-15T10:30:00Z"
  }
}
```

### License Controller

#### POST /api/license/verify
Comprehensive license verification with full validation.

**Request (Form Data):**
- `licenseNumber`: String (required) - License number to verify
- `issueDate`: Date (required) - Date when license was issued
- `issuedBy`: String (required) - Authority that issued the license
- `firstName`: String (required) - License holder's first name
- `lastName`: String (required) - License holder's last name
- `dateOfBirth`: Date (required) - License holder's date of birth
- `licenseImage`: File (optional) - License image for verification

#### GET /api/license/check-exists?licenseNumber={number}
Check if a license number is already registered in the system.

#### GET /api/license/info?licenseNumber={number} [Authenticated]
Get detailed license information (user can only view their own licenses).

#### PATCH /api/license/status?licenseNumber={number}&status={status} [Admin Only]
Update license status (admin/staff only).

## Validation Rules

### License Number Format
Supports Vietnamese driving license formats:
- **9 digits**: `123456789`
- **Letter + 8 digits**: `A12345678`
- **12 digits**: `123456789012`

### Age Requirements
- Minimum age: 16 years old at the time of license issuance
- Maximum age considered: 100 years (prevents unrealistic birth dates)

### Issue Date Validation
- Cannot be in the future
- Cannot be older than 50 years
- Must be reasonable date range

### Image Upload (Optional)
- Supported formats: JPG, JPEG, PNG, GIF, BMP
- Maximum size: 5MB
- MIME type validation included

## License Status Types

- **ACTIVE**: License is valid and current
- **EXPIRED**: License has passed its expiry date
- **SUSPENDED**: License is temporarily suspended
- **REVOKED**: License has been permanently revoked
- **VERIFIED**: License has been verified but not yet registered

## Database Schema

### DrivingLicense Table
```sql
CREATE TABLE driving_licenses (
    id INTEGER PRIMARY KEY,
    co_owner_id INTEGER REFERENCES co_owners(user_id),
    license_number VARCHAR(50) UNIQUE NOT NULL,
    issued_by VARCHAR(100) NOT NULL,
    issue_date DATE NOT NULL,
    expiry_date DATE,
    license_image_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Error Codes and Messages

### Validation Errors (400)
- `LICENSE_NUMBER_REQUIRED`: License number is required
- `LICENSE_NUMBER_INVALID_LENGTH`: License number must be 6-15 characters
- `LICENSE_NUMBER_INVALID_FORMAT`: Invalid license number format
- `ISSUE_DATE_REQUIRED`: Issue date is required
- `ISSUE_DATE_CANNOT_BE_FUTURE`: Issue date cannot be in the future
- `FIRST_NAME_REQUIRED`: First name is required
- `LAST_NAME_REQUIRED`: Last name is required
- `DATE_OF_BIRTH_REQUIRED`: Date of birth is required
- `MUST_BE_AT_LEAST_16_YEARS_OLD`: Must be at least 16 years old
- `INVALID_IMAGE_FILE`: Invalid image file format
- `IMAGE_SIZE_TOO_LARGE`: Image size exceeds 5MB limit

### Verification Errors (400)
- `LICENSE_VERIFICATION_FAILED`: License verification failed
- `INVALID_LICENSE_FORMAT`: License format is invalid
- `INVALID_ISSUE_DATE`: Issue date is invalid
- `AGE_REQUIREMENT_NOT_MET`: Age requirement not met for license issuance

### Conflict Errors (409)
- `LICENSE_ALREADY_REGISTERED`: License number already exists in system

### Access Errors (403)
- `ACCESS_DENIED`: User does not have permission to perform this action

### Not Found Errors (404)
- `LICENSE_NOT_FOUND`: License not found in system
- `USER_NOT_FOUND`: User not found

## Security Features

### Authentication & Authorization
- JWT token-based authentication
- Role-based access control (User, Staff, Admin)
- Users can only view their own licenses
- Admins can view and manage all licenses

### Input Validation
- Comprehensive FluentValidation rules
- SQL injection prevention
- File upload security checks
- Input sanitization

### Audit Trail
- Complete logging of all verification attempts
- User action tracking
- Failed verification attempt monitoring

## Mock Verification Logic

The system includes sophisticated mock verification logic that simulates real-world license verification:

### Valid License Patterns by Authority
```csharp
HO CHI MINH: ^[0-9]{9}$, ^B[0-9]{8}$
HA NOI: ^[0-9]{9}$, ^A[0-9]{8}$
DA NANG: ^[0-9]{9}$, ^C[0-9]{8}$
CAN THO: ^[0-9]{9}$, ^D[0-9]{8}$
```

### Blacklisted Licenses
The system maintains a blacklist of suspended/revoked licenses:
- `123456789` (Mock suspended license)
- `SUSPENDED01` (Mock suspended license)
- `REVOKED001` (Mock revoked license)

### Verification Steps
1. **Blacklist Check**: Verify license is not suspended/revoked
2. **Format Validation**: Check license format matches issuing authority
3. **Date Validation**: Verify issue date is reasonable
4. **Age Validation**: Ensure holder was old enough when license was issued
5. **Mock API Call**: Simulate external government database check
6. **Response Generation**: Create detailed verification response

## Extension Methods

### LicenseMapper Extensions
- `ToLicenseDetails()`: Convert DrivingLicense entity to LicenseDetails DTO
- `ToEntity()`: Convert VerifyLicenseRequest to DrivingLicense entity
- `ToSummary()`: Generate summary object from DrivingLicense
- `IsValidVietnameseLicenseFormat()`: Validate Vietnamese license format
- `GetDaysUntilExpiry()`: Calculate days until license expires
- `IsExpired()`: Check if license is expired
- `IsExpiringSoon()`: Check if license expires within threshold

## Testing Endpoints

### Development/Testing Features

#### GET /api/license/test/mock-verification
Returns mock verification scenarios for testing.

#### GET /api/license/test/validate-format?licenseNumber={number}
Tests license number format validation.

## Usage Examples

### Basic License Verification (JavaScript)
```javascript
const verifyLicense = async (licenseData) => {
  const formData = new FormData();
  formData.append('licenseNumber', licenseData.licenseNumber);
  formData.append('issueDate', licenseData.issueDate);
  formData.append('issuedBy', licenseData.issuedBy);
  formData.append('firstName', licenseData.firstName);
  formData.append('lastName', licenseData.lastName);
  formData.append('dateOfBirth', licenseData.dateOfBirth);
  
  if (licenseData.licenseImage) {
    formData.append('licenseImage', licenseData.licenseImage);
  }
  
  const response = await fetch('/api/license/verify', {
    method: 'POST',
    body: formData
  });
  
  return await response.json();
};
```

### Check License Exists
```javascript
const checkLicenseExists = async (licenseNumber) => {
  const response = await fetch(`/api/license/check-exists?licenseNumber=${licenseNumber}`);
  return await response.json();
};
```

### Get License Info (Authenticated)
```javascript
const getLicenseInfo = async (licenseNumber, token) => {
  const response = await fetch(`/api/license/info?licenseNumber=${licenseNumber}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return await response.json();
};
```

## Configuration

### FluentValidation Integration
The system uses FluentValidation for automatic request validation. All DTOs include comprehensive validation rules that are automatically applied through the `ValidationActionFilter`.

### Service Registration
Services are automatically registered in `ServiceConfigurations.cs`:
```csharp
services.AddScoped<ILicenseVerificationService, LicenseVerificationService>();
```

### Logging Integration
Complete integration with Serilog for structured logging:
- Request/response logging
- Error tracking
- Performance monitoring
- Security event logging

## Best Practices

### Client-Side Implementation
1. **Pre-validate inputs** before sending requests
2. **Handle all response status codes** appropriately
3. **Implement retry logic** for network failures
4. **Show user-friendly error messages** based on error codes
5. **Cache verification results** to avoid duplicate requests

### Security Considerations
1. **Always use HTTPS** in production
2. **Validate file uploads** on both client and server
3. **Implement rate limiting** for verification endpoints
4. **Monitor failed verification attempts** for security threats
5. **Sanitize all user inputs** before processing

### Performance Optimization
1. **Use pagination** for license listing endpoints
2. **Implement caching** for frequently accessed license data
3. **Optimize database queries** with proper indexes
4. **Use asynchronous operations** for all database calls
5. **Implement connection pooling** for database connections

## Future Enhancements

### Planned Features
1. **Real Government API Integration**: Connect to actual license verification services
2. **OCR Support**: Extract license information from uploaded images
3. **Batch Verification**: Support for verifying multiple licenses at once
4. **License Renewal Notifications**: Automatic notifications for expiring licenses
5. **Mobile App Integration**: Dedicated mobile endpoints
6. **Blockchain Integration**: Immutable license verification records
7. **Advanced Analytics**: License verification statistics and reporting

### Integration Opportunities
1. **Payment System**: Link license verification to payment processing
2. **Vehicle Registration**: Connect verified licenses to vehicle registrations
3. **Insurance Integration**: Integrate with insurance verification systems
4. **Background Check Services**: Enhanced verification through third-party services

## Conclusion

The License Verification System provides a robust, secure, and scalable solution for managing driving license verification in the EV Co-Ownership platform. With comprehensive validation, role-based access control, and extensive logging, it ensures reliable license management while maintaining security and performance standards.

For technical support or feature requests, please refer to the project documentation or contact the development team.