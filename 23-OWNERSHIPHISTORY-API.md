# OwnershipHistory API Documentation

## Overview
The OwnershipHistory API provides endpoints to track and retrieve the history of ownership changes for vehicles in the EV co-ownership system. It supports filtering, snapshots, and statistics for ownership records.

### Base URL
```
/api/ownershiphistory
```

---

## Endpoints

### 1. Get Vehicle Ownership History
**GET** `/vehicle/{vehicleId}`

#### Description
Retrieves the ownership history for a specific vehicle with optional filters.

#### URL Parameters
- `vehicleId` (int): The ID of the vehicle.

#### Query Parameters
- `changeType` (string, optional): Filter by change type (e.g., Initial, Adjustment, Transfer, Exit, etc.).
- `startDate` (ISO 8601, optional): Filter by start date.
- `endDate` (ISO 8601, optional): Filter by end date.
- `coOwnerId` (int, optional): Filter by specific co-owner.
- `offset` (int, optional): Pagination offset (default: 0).
- `limit` (int, optional): Pagination limit (default: 50).

#### Responses
- **200 OK**: Ownership history retrieved successfully.
- **403 Forbidden**: User is not authorized to view the vehicle history.
- **500 Internal Server Error**: Unexpected error.

---

### 2. Get Vehicle Ownership Timeline
**GET** `/vehicle/{vehicleId}/timeline`

#### Description
Retrieves the complete ownership timeline for a specific vehicle, showing all co-owners and their changes over time.

#### URL Parameters
- `vehicleId` (int): The ID of the vehicle.

#### Responses
- **200 OK**: Ownership timeline retrieved successfully.
- **403 Forbidden**: User is not authorized to view the vehicle timeline.
- **404 Not Found**: Vehicle not found.
- **500 Internal Server Error**: Unexpected error.

---

### 3. Get Ownership Snapshot
**GET** `/vehicle/{vehicleId}/snapshot`

#### Description
Retrieves the ownership distribution for a specific vehicle at a given date.

#### URL Parameters
- `vehicleId` (int): The ID of the vehicle.

#### Query Parameters
- `date` (ISO 8601): The date for the snapshot.

#### Responses
- **200 OK**: Ownership snapshot retrieved successfully.
- **403 Forbidden**: User is not authorized to view the snapshot.
- **404 Not Found**: Vehicle not found.
- **500 Internal Server Error**: Unexpected error.

---

### 4. Get Ownership History Statistics
**GET** `/vehicle/{vehicleId}/statistics`

#### Description
Retrieves aggregated statistics about ownership changes for a specific vehicle.

#### URL Parameters
- `vehicleId` (int): The ID of the vehicle.

#### Responses
- **200 OK**: Ownership statistics retrieved successfully.
- **403 Forbidden**: User is not authorized to view the statistics.
- **404 Not Found**: Vehicle not found.
- **500 Internal Server Error**: Unexpected error.

---

### 5. Get Co-Owner Ownership History
**GET** `/my-history`

#### Description
Retrieves the complete ownership history for the authenticated co-owner across all vehicles.

#### Responses
- **200 OK**: Co-owner ownership history retrieved successfully.
- **404 Not Found**: User is not a co-owner.
- **500 Internal Server Error**: Unexpected error.

---

## Notes
- **Access Control:** Most endpoints are restricted to co-owners.
- **Filtering:** Supports advanced filtering by date, change type, and co-owner.
- **Error Handling:** Consistent error codes and messages are used across endpoints.