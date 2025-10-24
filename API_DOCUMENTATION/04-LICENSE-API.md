# License API Documentation

## Overview
The License API provides endpoints for managing and verifying driving licenses. This includes uploading, validating, and retrieving license details.

### Base URL
```
/api/license
```

---

## Endpoints

### 1. **GET /api/license**
Retrieve the list of licenses for the authenticated user.

#### Use Case
- **Scenario**: A user wants to view all their uploaded driving licenses.
- **Frontend Integration**: Use this endpoint to display a list of licenses on the "My Licenses" page.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "LICENSES_RETRIEVED",
  "data": [
    {
      "licenseId": 1,
      "licenseNumber": "B1234567",
      "fullName": "John Doe",
      "issueDate": "2020-01-01",
      "expiryDate": "2030-01-01",
      "status": "Valid"
    },
    {
      "licenseId": 2,
      "licenseNumber": "C7654321",
      "fullName": "John Doe",
      "issueDate": "2018-05-15",
      "expiryDate": "2028-05-15",
      "status": "Expired"
    }
  ]
}
```

#### Notes
- Ensure the Bearer Token is valid and not expired.
- This endpoint is commonly used on the "My Licenses" page.

---

### 2. **GET /api/license/{licenseId}**
Retrieve details of a specific license by its ID.

#### Use Case
- **Scenario**: A user wants to view detailed information about a specific license.
- **Frontend Integration**: Use this endpoint to display license details in a modal or separate page.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)
- **Path Parameters**:
  - `licenseId` (integer): The ID of the license to retrieve.

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "LICENSE_RETRIEVED",
  "data": {
    "licenseId": 1,
    "licenseNumber": "B1234567",
    "fullName": "John Doe",
    "issueDate": "2020-01-01",
    "expiryDate": "2030-01-01",
    "status": "Valid",
    "imageUrl": "https://example.com/licenses/license1.jpg"
  }
}
```

#### Notes
- Ensure the user has permission to access the license details.
- Handle cases where the license does not exist (404).

---

### 3. **POST /api/license/upload**
Upload a new driving license.

#### Use Case
- **Scenario**: A user wants to upload a new driving license for verification.
- **Frontend Integration**: Use this endpoint in the "Upload License" form.

#### Request
- **Method**: POST
- **Authorization**: Bearer Token (User must be authenticated)
- **Body**:
  - Multipart form-data with the key `file` containing the license image file.

#### Response
- **Status Code**: 201 Created
- **Body**:
```json
{
  "statusCode": 201,
  "message": "LICENSE_UPLOADED",
  "data": {
    "licenseId": 3,
    "licenseNumber": "D9876543",
    "fullName": "John Doe",
    "issueDate": "2023-10-01",
    "expiryDate": "2033-10-01",
    "status": "Pending Verification",
    "imageUrl": "https://example.com/licenses/license3.jpg"
  }
}
```

#### Notes
- Limit image size to 5MB.
- Supported formats: JPEG, PNG, WEBP.
- Notify the user that the license is pending verification.

---

### 4. **PUT /api/license/{licenseId}/verify**
Verify a driving license (Admin only).

#### Use Case
- **Scenario**: An admin verifies the authenticity of a user's driving license.
- **Frontend Integration**: Use this endpoint in the admin panel for license verification.

#### Request
- **Method**: PUT
- **Authorization**: Bearer Token (Admin role required)
- **Path Parameters**:
  - `licenseId` (integer): The ID of the license to verify.
- **Body**:
```json
{
  "status": "Valid"
}
```

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "LICENSE_VERIFIED",
  "data": {
    "licenseId": 3,
    "status": "Valid"
  }
}
```

#### Notes
- Only admins can access this endpoint.
- Ensure proper error handling for invalid license IDs.

---

### 5. **DELETE /api/license/{licenseId}**
Delete a driving license.

#### Use Case
- **Scenario**: A user wants to remove an outdated or incorrect license.
- **Frontend Integration**: Use this endpoint in the "Manage Licenses" page.

#### Request
- **Method**: DELETE
- **Authorization**: Bearer Token (User must be authenticated)
- **Path Parameters**:
  - `licenseId` (integer): The ID of the license to delete.

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "LICENSE_DELETED"
}
```

#### Notes
- Ensure the user has permission to delete the license.
- Handle cases where the license does not exist (404).

---

## Best Practices
- Validate license details on the frontend before uploading.
- Notify users when their license status changes (e.g., via email or push notification).
- Use HTTPS for all API requests to ensure data security.

---

## Related APIs
- [Profile API](03-PROFILE-API.md)
- [User API](02-USER-API.md)