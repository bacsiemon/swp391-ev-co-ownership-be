# CoOwner API Documentation

## Overview
The CoOwner API provides endpoints for managing co-ownership of vehicles. This includes eligibility checks, promotions, and statistics related to co-owners.

### Base URL
```
/api/coowner
```

---

## Endpoints

### 1. **GET /api/coowner/eligibility**
Check if a user is eligible to become a co-owner.

#### Use Case
- **Scenario**: A user wants to check if they meet the criteria to become a co-owner of a vehicle.
- **Frontend Integration**: Use this endpoint in the "Become a CoOwner" page.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (User must be authenticated)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "ELIGIBILITY_CHECKED",
  "data": {
    "isEligible": true,
    "criteria": [
      "Must have a valid driving license",
      "Must not have any outstanding payments",
      "Must have a verified profile"
    ]
  }
}
```

#### Notes
- Ensure the Bearer Token is valid and not expired.
- This endpoint is commonly used during the onboarding process.

---

### 2. **POST /api/coowner/promote**
Promote a user to co-owner status.

#### Use Case
- **Scenario**: An admin promotes a user to co-owner status after verifying their eligibility.
- **Frontend Integration**: Use this endpoint in the admin panel for user management.

#### Request
- **Method**: POST
- **Authorization**: Bearer Token (Admin role required)
- **Body**:
```json
{
  "userId": 10,
  "vehicleId": 5
}
```

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "USER_PROMOTED",
  "data": {
    "userId": 10,
    "vehicleId": 5,
    "role": "CoOwner"
  }
}
```

#### Notes
- Ensure the user meets all eligibility criteria before promotion.
- Notify the user about their new role.

---

### 3. **GET /api/coowner/statistics**
Retrieve statistics related to co-owners.

#### Use Case
- **Scenario**: An admin wants to view statistics about co-owners, such as the total number of co-owners and their distribution.
- **Frontend Integration**: Use this endpoint in the admin dashboard.

#### Request
- **Method**: GET
- **Authorization**: Bearer Token (Admin role required)

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "STATISTICS_RETRIEVED",
  "data": {
    "totalCoOwners": 150,
    "activeCoOwners": 120,
    "inactiveCoOwners": 30
  }
}
```

#### Notes
- Only admins can access this endpoint.
- Ensure proper error handling for unauthorized access.

---

### 4. **GET /api/coowner/vehicles**
Retrieve the list of vehicles associated with a co-owner.

#### Use Case
- **Scenario**: A co-owner wants to view all vehicles they are associated with.
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

### 5. **DELETE /api/coowner/remove**
Remove a user from co-owner status.

#### Use Case
- **Scenario**: An admin removes a user from co-owner status due to violations or inactivity.
- **Frontend Integration**: Use this endpoint in the admin panel for user management.

#### Request
- **Method**: DELETE
- **Authorization**: Bearer Token (Admin role required)
- **Body**:
```json
{
  "userId": 10,
  "vehicleId": 5
}
```

#### Response
- **Status Code**: 200 OK
- **Body**:
```json
{
  "statusCode": 200,
  "message": "USER_REMOVED",
  "data": {
    "userId": 10,
    "vehicleId": 5
  }
}
```

#### Notes
- Notify the user about their removal from co-owner status.
- Ensure proper error handling for invalid user or vehicle IDs.

---

## Best Practices
- Always validate user input on the frontend before sending requests.
- Notify users about changes to their co-owner status.
- Use HTTPS for all API requests to ensure data security.

---

## Related APIs
- [Profile API](03-PROFILE-API.md)
- [Vehicle API](06-VEHICLE-API.md)