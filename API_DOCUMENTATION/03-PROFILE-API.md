# Profile API Documentation

## Overview
The Profile API provides endpoints for managing user profiles, including retrieving, updating, and validating profile information. It also supports profile image uploads and activity summaries.

### Base URL
```
/api/profile
```

---

## Endpoints

### 1. **GET /api/profile**
Retrieve the profile of the currently authenticated user.

#### Use Case
- **Scenario**: A user logs into the system and wants to view their profile details.
- **Frontend Integration**: Use this endpoint to populate the "My Profile" page with user details.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "PROFILE_RETRIEVED",
  "data": {
    "id": 1,
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "phoneNumber": "123456789",
    "profileImageUrl": "https://example.com/images/profile.jpg",
    "role": "CoOwner",
    "status": "Active"
  }
}
```

#### Notes
- Ensure the Bearer Token is valid and not expired.
- This endpoint is commonly used on the dashboard or profile page.

---

### 2. **GET /api/profile/{userId}**
Retrieve the profile of a specific user (Admin only).

#### Use Case
- **Scenario**: An admin wants to view the profile of a specific user for management purposes.
- **Frontend Integration**: Use this endpoint in the admin panel to display user details.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (Admin role required)
- **Path Parameters**:
  - `userId` (integer): The ID of the user to retrieve.

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "PROFILE_RETRIEVED",
  "data": {
    "id": 2,
    "fullName": "Jane Smith",
    "email": "jane.smith@example.com",
    "phoneNumber": "987654321",
    "profileImageUrl": "https://example.com/images/profile2.jpg",
    "role": "Staff",
    "status": "Active"
  }
}
```

#### Notes
- Only admins can access this endpoint.
- Ensure proper error handling for unauthorized access.

---

### 3. **PUT /api/profile**
Update the profile of the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to update their profile information, such as their name or phone number.
- **Frontend Integration**: Use this endpoint in the "Edit Profile" form.

#### Request
- **Method**: PUT
- **Authorization**: Bearer Token (User must be authenticated)
- **Body**:
```json
{
  "fullName": "John Updated",
  "phoneNumber": "1122334455"
}
```

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "PROFILE_UPDATED",
  "data": {
    "id": 1,
    "fullName": "John Updated",
    "phoneNumber": "1122334455"
  }
}
```

#### Notes
- Validate user input on the frontend before sending the request.
- Ensure the Bearer Token matches the user being updated.

---

### 4. **PUT /api/profile/change-password**
Change the password of the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to change their password for security reasons.
- **Frontend Integration**: Use this endpoint in the "Change Password" form.

#### Request
- **Method**: PUT
- **Authorization**: Bearer Token (User must be authenticated)
- **Body**:
```json
{
  "currentPassword": "oldpassword",
  "newPassword": "newpassword123"
}
```

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "PASSWORD_CHANGED"
}
```

#### Notes
- Ensure the current password is correct before allowing the change.
- Enforce strong password policies on the frontend.

---

### 5. **POST /api/profile/upload-image**
Upload a profile image for the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to upload or update their profile picture.
- **Frontend Integration**: Use this endpoint in the "Edit Profile" page for image uploads.

#### Request
- **Method**: POST
- **Authorization**: Bearer Token (User must be authenticated)
- **Body**:
  - Multipart form-data with the key `file` containing the image file.

#### Response
- **Status Code**: 201 Created
- **Body**:
```json
{
  "statusCode": 201,
  "message": "IMAGE_UPLOADED",
  "data": {
    "profileImageUrl": "https://example.com/images/new-profile.jpg"
  }
}
```

#### Notes
- Limit image size to 5MB.
- Supported formats: JPEG, PNG, WEBP.

---

### 6. **DELETE /api/profile/delete-image**
Delete the profile image of the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to remove their profile picture.
- **Frontend Integration**: Use this endpoint in the "Edit Profile" page.

#### Request
- **Method**: DELETE
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "IMAGE_DELETED"
}
```

#### Notes
- Ensure the user has a profile image before attempting deletion.

---

### 7. **GET /api/profile/validate-completeness**
Validate if the user's profile is complete.

#### Use Case
- **Scenario**: The system checks if the user has provided all required profile details.
- **Frontend Integration**: Use this endpoint to prompt users to complete their profile.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "PROFILE_COMPLETE",
  "data": {
    "isComplete": true
  }
}
```

#### Notes
- Use this endpoint to enforce profile completeness for certain actions.

---

### 8. **GET /api/profile/activity**
Retrieve the activity summary of the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to view a summary of their activities, such as bookings and payments.
- **Frontend Integration**: Use this endpoint on the dashboard or activity page.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "ACTIVITY_SUMMARY_RETRIEVED",
  "data": {
    "totalBookings": 15,
    "totalPayments": 10,
    "totalVehicles": 3
  }
}
```

#### Notes
- Cache activity summaries to improve performance.

---

### 9. **GET /api/profile/vehicles**
Retrieve the list of vehicles associated with the currently authenticated user.

#### Use Case
- **Scenario**: A user wants to view all vehicles they own or co-own.
- **Frontend Integration**: Use this endpoint on the "My Vehicles" page.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "VEHICLES_RETRIEVED",
  "data": [
    {
      "vehicleId": 1,
      "name": "Tesla Model 3",
      "licensePlate": "29A-12345"
    },
    {
      "vehicleId": 2,
      "name": "Nissan Leaf",
      "licensePlate": "30B-67890"
    }
  ]
}
```

#### Notes
- Ensure the user has the appropriate permissions to view vehicle details.

---

## Best Practices
- Always validate user input on the frontend before sending requests.
- Cache frequently accessed data like activity summaries and vehicle lists to reduce API calls.
- Use HTTPS for all API requests to ensure data security.

---

## Related APIs
- [Auth API](01-AUTH-API.md)
- [User API](02-USER-API.md)