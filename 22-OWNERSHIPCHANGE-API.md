# OwnershipChange API Documentation

## Overview
The OwnershipChange API manages vehicle ownership percentage changes within the EV co-ownership system. It facilitates proposing, approving, and tracking ownership adjustments among co-owners.

### Base URL
```
/api/ownership-change
```

---

## Endpoints

### 1. Propose Ownership Change
**POST** `/propose`

#### Description
Proposes a change to vehicle ownership percentages.

#### Request Body
```json
{
  "vehicleId": 5,
  "reason": "Adjusting ownership after new co-owner investment. User A increases stake, User B decreases proportionally.",
  "proposedChanges": [
    {
      "coOwnerId": 10,
      "proposedPercentage": 60.0,
      "proposedInvestment": 600000000
    },
    {
      "coOwnerId": 11,
      "proposedPercentage": 40.0,
      "proposedInvestment": 400000000
    }
  ]
}
```

#### Responses
- **201 Created**: Ownership change request created successfully.
- **400 Bad Request**: Invalid input (e.g., `INVALID_CO_OWNER_IDS_IN_PROPOSED_CHANGES`).
- **403 Forbidden**: Only co-owners can propose.
- **404 Not Found**: Vehicle not found.
- **409 Conflict**: Vehicle has a pending ownership change request.
- **500 Internal Server Error**: Unexpected error.

---

### 2. Get Ownership Change Request Details
**GET** `/{requestId}`

#### Description
Gets details of a specific ownership change request.

#### URL Parameters
- `requestId` (int): Ownership change request ID.

#### Responses
- **200 OK**: Ownership change request retrieved successfully.
- **403 Forbidden**: Not authorized to view this request.
- **404 Not Found**: Request not found.
- **500 Internal Server Error**: Unexpected error.

---

### 3. Get Vehicle Ownership Change Requests
**GET** `/vehicle/{vehicleId}`

#### Description
Gets all ownership change requests for a specific vehicle.

#### URL Parameters
- `vehicleId` (int): Vehicle ID.

#### Query Parameters
- `includeCompleted` (bool): Include completed requests (default: false).

#### Responses
- **200 OK**: Ownership change requests retrieved successfully.
- **403 Forbidden**: Not authorized to view vehicle requests.
- **500 Internal Server Error**: Unexpected error.

---

### 4. Get Pending Approvals
**GET** `/pending-approvals`

#### Description
Gets all pending ownership change requests requiring approval from the current user.

#### Responses
- **200 OK**: Pending approvals retrieved successfully.
- **500 Internal Server Error**: Unexpected error.

---

### 5. Approve or Reject Ownership Change
**POST** `/{requestId}/respond`

#### Description
Approves or rejects an ownership change request.

#### URL Parameters
- `requestId` (int): Ownership change request ID.

#### Request Body
Approve:
```json
{
  "approve": true,
  "comments": "I agree with this ownership adjustment"
}
```
Reject:
```json
{
  "approve": false,
  "comments": "I disagree with the proposed percentage split"
}
```

#### Responses
- **200 OK**: Decision recorded successfully.
- **400 Bad Request**: Request already approved/rejected.
- **403 Forbidden**: Not authorized to approve this request.
- **404 Not Found**: Request not found.
- **500 Internal Server Error**: Unexpected error.

---

### 6. Cancel Ownership Change Request
**DELETE** `/{requestId}`

#### Description
Cancels a pending ownership change request.

#### URL Parameters
- `requestId` (int): Ownership change request ID.

#### Responses
- **200 OK**: Request cancelled successfully.
- **400 Bad Request**: Cannot cancel request with current status.
- **403 Forbidden**: Only proposer can cancel.
- **404 Not Found**: Request not found.
- **500 Internal Server Error**: Unexpected error.

---

### 7. Get Ownership Change Statistics
**GET** `/statistics`

#### Description
Gets ownership change statistics (Admin/Staff only).

#### Responses
- **200 OK**: Statistics retrieved successfully.
- **403 Forbidden**: Admin/Staff role required.
- **500 Internal Server Error**: Unexpected error.

---

### 8. Get User Ownership Change Requests
**GET** `/my-requests`

#### Description
Gets all ownership change requests for the current user (as proposer or approver).

#### Query Parameters
- `includeCompleted` (bool): Include completed requests (default: false).

#### Responses
- **200 OK**: Ownership change requests retrieved successfully.
- **500 Internal Server Error**: Unexpected error.

---

## Notes
- **Access Control:** Most endpoints are restricted to co-owners, staff, or admins.
- **Group Consensus Logic:** All co-owners must approve for changes to be applied.
- **Error Handling:** Consistent error codes and messages are used across endpoints.