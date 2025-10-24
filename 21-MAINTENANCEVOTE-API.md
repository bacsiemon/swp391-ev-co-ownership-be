# MaintenanceVote API Documentation

## Overview
The MaintenanceVote API is responsible for managing the voting system for maintenance expenditures in the EV co-ownership system. It allows co-owners to propose, vote, and manage maintenance-related expenses collaboratively.

### Base URL
```
/api/maintenance-vote
```

---

## Endpoints

### 1. Propose Maintenance Expenditure
**POST** `/propose`

#### Description
Proposes a maintenance expenditure that requires co-owner voting approval.

#### Request Body
```json
{
  "vehicleId": 1,
  "maintenanceCostId": 45,
  "reason": "Emergency brake system replacement - safety critical",
  "amount": 5000000,
  "imageUrl": "https://storage.example.com/receipts/brake-quote.jpg"
}
```

#### Responses
- **201 Created**: Proposal created successfully.
- **400 Bad Request**: Invalid input (e.g., `INVALID_AMOUNT`).
- **403 Forbidden**: Only co-owners can propose.
- **404 Not Found**: Vehicle, maintenance cost, or fund not found.
- **500 Internal Server Error**: Unexpected error.

---

### 2. Vote on Proposal
**POST** `/{fundUsageId}/vote`

#### Description
Votes (approve/reject) on a proposed maintenance expenditure.

#### URL Parameters
- `fundUsageId` (int): ID of the fund usage proposal.

#### Request Body
Approve:
```json
{
  "approve": true,
  "comments": "I agree this repair is necessary for safety"
}
```
Reject:
```json
{
  "approve": false,
  "comments": "Too expensive, please get a second quote"
}
```

#### Responses
- **200 OK**: Vote recorded successfully.
- **400 Bad Request**: Proposal already finalized or insufficient fund balance.
- **403 Forbidden**: Only co-owners can vote.
- **404 Not Found**: Proposal, fund, or vehicle not found.
- **500 Internal Server Error**: Unexpected error.

---

### 3. Get Proposal Details
**GET** `/{fundUsageId}`

#### Description
Gets details of a specific maintenance expenditure proposal.

#### URL Parameters
- `fundUsageId` (int): ID of the fund usage proposal.

#### Responses
- **200 OK**: Proposal details retrieved successfully.
- **403 Forbidden**: Access denied.
- **404 Not Found**: Proposal, fund, or vehicle not found.
- **500 Internal Server Error**: Unexpected error.

#### Sample Response
```json
{
  "statusCode": 200,
  "message": "PROPOSAL_DETAILS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "fundUsageId": 123,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "maintenanceCostId": 45,
    "maintenanceDescription": "Brake pad replacement",
    "maintenanceType": "Repair",
    "amount": 5000000,
    "reason": "Emergency brake system replacement",
    "proposedByUserName": "John Doe",
    "proposedAt": "2024-10-23T10:00:00Z",
    "totalCoOwners": 3,
    "requiredApprovals": 2,
    "currentApprovals": 1,
    "currentRejections": 0,
    "approvalPercentage": 33.33,
    "votingStatus": "Pending",
    "votes": [
      {
        "userId": 10,
        "userName": "John Doe",
        "hasVoted": true,
        "isAgree": true,
        "votedAt": "2024-10-23T10:00:00Z"
      },
      {
        "userId": 11,
        "userName": "Jane Smith",
        "hasVoted": false
      }
    ]
  }
}
```

---

### 4. Get Pending Proposals
**GET** `/vehicle/{vehicleId}/pending`

#### Description
Gets all pending maintenance expenditure proposals for a vehicle.

#### URL Parameters
- `vehicleId` (int): ID of the vehicle.

#### Responses
- **200 OK**: Pending proposals retrieved successfully.
- **403 Forbidden**: Access denied.
- **404 Not Found**: Vehicle not found.
- **500 Internal Server Error**: Unexpected error.

---

### 5. Get User Voting History
**GET** `/my-voting-history`

#### Description
Gets voting history for the authenticated user.

#### Responses
- **200 OK**: Voting history retrieved successfully.
- **404 Not Found**: User not found.
- **500 Internal Server Error**: Unexpected error.

---

### 6. Cancel Proposal
**DELETE** `/{fundUsageId}/cancel`

#### Description
Cancels a pending maintenance expenditure proposal.

#### URL Parameters
- `fundUsageId` (int): ID of the fund usage proposal.

#### Responses
- **200 OK**: Proposal cancelled successfully.
- **400 Bad Request**: Proposal already finalized.
- **403 Forbidden**: Only proposer or admin can cancel.
- **404 Not Found**: Proposal or user not found.
- **500 Internal Server Error**: Unexpected error.

---

## Notes
- **Access Control:** Most endpoints are restricted to co-owners or admins.
- **Voting Logic:** Majority approval (> 50%) is required for proposals to pass.
- **Error Handling:** Consistent error codes and messages are used across endpoints.