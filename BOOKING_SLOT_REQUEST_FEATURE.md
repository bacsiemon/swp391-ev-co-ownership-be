# Request Booking Slot Feature - Comprehensive Documentation

## 📋 Table of Contents
1. [Feature Overview](#feature-overview)
2. [Architecture & Design](#architecture--design)
3. [API Endpoints](#api-endpoints)
4. [Data Models](#data-models)
5. [Business Logic](#business-logic)
6. [Integration Guide](#integration-guide)
7. [Usage Examples](#usage-examples)
8. [Error Handling](#error-handling)

---

## 🎯 Feature Overview

### What is "Request Booking Slot"?

The **Request Booking Slot** feature is an intelligent booking system designed for EV co-ownership scenarios. Unlike simple booking creation, this feature:

- **Automatically detects conflicts** with existing bookings
- **Auto-confirms available slots** when there are no conflicts
- **Generates alternative suggestions** when preferred slots are unavailable
- **Supports flexible booking** with user-provided alternatives
- **Requires co-owner approval** for conflicting time slots
- **Provides analytics** on booking request patterns

### Key Differences from Basic Booking

| Feature | Basic Booking (`CreateBooking`) | Request Booking Slot |
|---------|--------------------------------|---------------------|
| Conflict Detection | Manual (returns 409 error) | Automatic with details |
| Auto-Confirmation | No | Yes (if available) |
| Alternative Slots | No | Yes (system + user-provided) |
| Approval Workflow | Simple Pending → Confirmed | Advanced with co-owner approval |
| Flexibility | Fixed time only | Multiple alternative times |
| Analytics | No | Yes (patterns, approval rates) |
| Priority Levels | No | Yes (Low/Medium/High/Urgent) |

---

## 🏗️ Architecture & Design

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    BookingController                         │
│  (REST API Layer - Handles HTTP requests/responses)         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    IBookingService                           │
│  (Service Interface - Business logic contracts)             │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    BookingService                            │
│  (Implementation - Core business logic)                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ • RequestBookingSlotAsync                              │ │
│  │ • RespondToSlotRequestAsync                            │ │
│  │ • CancelSlotRequestAsync                               │ │
│  │ • GetPendingSlotRequestsAsync                          │ │
│  │ • GetSlotRequestAnalyticsAsync                         │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    IUnitOfWork                               │
│  (Data Access Layer - Repository pattern)                   │
│  • BookingRepository                                         │
│  • VehicleRepository                                         │
│  • VehicleCoOwnerRepository                                 │
│  • UserRepository                                            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    PostgreSQL Database                       │
│  • Booking table (StatusEnum: Pending/Confirmed/Cancelled)  │
│  • Vehicle, VehicleCoOwner, User tables                     │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

```
User Request → API Controller → Service Layer → Repository → Database
                    ↓                ↓
              JWT Validation   Conflict Detection
                    ↓                ↓
              Role Check       Alternative Generation
                    ↓                ↓
              DTO Validation   Auto-Confirmation Logic
                    ↓                ↓
              Response         Approval Workflow
```

---

## 🚀 API Endpoints

### 1. Request Booking Slot

**Endpoint:** `POST /api/booking/vehicle/{vehicleId}/request-slot`

**Authentication:** Required (CoOwner role)

**Description:** Creates a booking slot request with intelligent conflict detection and auto-confirmation.

**Request Body:**
```json
{
  "preferredStartTime": "2025-01-25T09:00:00",
  "preferredEndTime": "2025-01-25T17:00:00",
  "purpose": "Business trip to downtown",
  "priority": 2,
  "isFlexible": true,
  "autoConfirmIfAvailable": true,
  "estimatedDistance": 150,
  "usageType": 0,
  "alternativeSlots": [
    {
      "startTime": "2025-01-25T10:00:00",
      "endTime": "2025-01-25T18:00:00",
      "preferenceRank": 1
    },
    {
      "startTime": "2025-01-26T09:00:00",
      "endTime": "2025-01-26T17:00:00",
      "preferenceRank": 2
    }
  ]
}
```

**Response (Auto-Confirmed - 201):**
```json
{
  "statusCode": 201,
  "message": "BOOKING_SLOT_AUTO_CONFIRMED",
  "data": {
    "requestId": 123,
    "bookingId": 123,
    "vehicleId": 5,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "HN-12345",
    "requesterId": 10,
    "requesterName": "John Doe",
    "preferredStartTime": "2025-01-25T09:00:00",
    "preferredEndTime": "2025-01-25T17:00:00",
    "purpose": "Business trip to downtown",
    "priority": 2,
    "status": 1,
    "isFlexible": true,
    "estimatedDistance": 150,
    "usageType": 0,
    "requestedAt": "2025-01-17T10:00:00",
    "availabilityStatus": 0,
    "conflictingBookings": null,
    "alternativeSuggestions": null,
    "autoConfirmationMessage": "Slot was automatically confirmed as it's available with no conflicts",
    "metadata": {
      "totalAlternativesProvided": 2,
      "processingTimeSeconds": 0,
      "requiresCoOwnerApproval": false,
      "approvalPendingFrom": [],
      "systemRecommendation": "Your preferred slot is available and can be confirmed"
    }
  }
}
```

**Response (Has Conflicts - 201):**
```json
{
  "statusCode": 201,
  "message": "BOOKING_SLOT_REQUEST_CREATED",
  "data": {
    "requestId": 124,
    "bookingId": 124,
    "status": 0,
    "availabilityStatus": 3,
    "conflictingBookings": [
      {
        "bookingId": 120,
        "coOwnerName": "Jane Smith",
        "startTime": "2025-01-25T14:00:00",
        "endTime": "2025-01-25T19:00:00",
        "status": 1,
        "purpose": "Medical appointment",
        "overlapHours": 3.0
      }
    ],
    "alternativeSuggestions": [
      {
        "startTime": "2025-01-25T06:00:00",
        "endTime": "2025-01-25T14:00:00",
        "durationHours": 8.0,
        "isAvailable": true,
        "reason": "Earlier the same day",
        "conflictProbability": 0,
        "recommendationScore": 70
      },
      {
        "startTime": "2025-01-26T09:00:00",
        "endTime": "2025-01-26T17:00:00",
        "durationHours": 8.0,
        "isAvailable": true,
        "reason": "Next day at same time",
        "conflictProbability": 0,
        "recommendationScore": 70
      }
    ],
    "metadata": {
      "totalAlternativesProvided": 2,
      "processingTimeSeconds": 1,
      "requiresCoOwnerApproval": true,
      "approvalPendingFrom": ["Jane Smith"],
      "systemRecommendation": "Your slot conflicts with 1 booking(s). Co-owner approval required."
    }
  }
}
```

---

### 2. Respond to Slot Request

**Endpoint:** `POST /api/booking/slot-request/{requestId}/respond`

**Authentication:** Required (CoOwner role)

**Description:** Allows co-owners to approve or reject pending booking slot requests.

**Request Body (Approve):**
```json
{
  "isApproved": true,
  "notes": "Approved - have a safe trip!"
}
```

**Request Body (Reject with Alternative):**
```json
{
  "isApproved": false,
  "rejectionReason": "I need the vehicle that day for medical appointment",
  "suggestedStartTime": "2025-01-26T09:00:00",
  "suggestedEndTime": "2025-01-26T17:00:00",
  "notes": "Can you use it the next day instead?"
}
```

**Response (200):**
```json
{
  "statusCode": 200,
  "message": "BOOKING_REQUEST_APPROVED",
  "data": {
    "requestId": 124,
    "bookingId": 124,
    "status": 2,
    "processedAt": "2025-01-17T11:00:00",
    "processedBy": "Jane",
    "metadata": {
      "systemRecommendation": "Request approved successfully"
    }
  }
}
```

---

### 3. Cancel Slot Request

**Endpoint:** `POST /api/booking/slot-request/{requestId}/cancel`

**Authentication:** Required (CoOwner role - must be request owner)

**Description:** Cancel a pending slot request.

**Request Body:**
```json
{
  "reason": "Plans changed, no longer need the vehicle"
}
```

**Response (200):**
```json
{
  "statusCode": 200,
  "message": "BOOKING_REQUEST_CANCELLED",
  "data": "Request #124 cancelled: Plans changed, no longer need the vehicle"
}
```

---

### 4. Get Pending Slot Requests

**Endpoint:** `GET /api/booking/vehicle/{vehicleId}/pending-slot-requests`

**Authentication:** Required (CoOwner role)

**Description:** Get all pending booking slot requests for a vehicle.

**Response (200):**
```json
{
  "statusCode": 200,
  "message": "PENDING_REQUESTS_RETRIEVED",
  "data": {
    "vehicleId": 5,
    "vehicleName": "Tesla Model 3",
    "totalPendingCount": 3,
    "oldestRequestDate": "2025-01-15T10:00:00",
    "pendingRequests": [
      {
        "requestId": 125,
        "requesterName": "John Smith",
        "preferredStartTime": "2025-01-25T09:00:00",
        "preferredEndTime": "2025-01-25T17:00:00",
        "purpose": "Business trip",
        "priority": 2,
        "requestedAt": "2025-01-15T10:00:00",
        "metadata": {
          "requiresCoOwnerApproval": true
        }
      },
      {
        "requestId": 126,
        "requesterName": "Alice Johnson",
        "preferredStartTime": "2025-01-26T08:00:00",
        "preferredEndTime": "2025-01-26T18:00:00",
        "purpose": "Family trip",
        "priority": 1,
        "requestedAt": "2025-01-16T09:00:00",
        "metadata": {
          "requiresCoOwnerApproval": true
        }
      }
    ]
  }
}
```

---

### 5. Get Slot Request Analytics

**Endpoint:** `GET /api/booking/vehicle/{vehicleId}/slot-request-analytics?startDate=2024-10-17&endDate=2025-01-17`

**Authentication:** Required (CoOwner role)

**Description:** Get analytics on booking request patterns and approval rates.

**Query Parameters:**
- `startDate` (optional): Start date for analysis (default: 90 days ago)
- `endDate` (optional): End date for analysis (default: today)

**Response (200):**
```json
{
  "statusCode": 200,
  "message": "ANALYTICS_RETRIEVED",
  "data": {
    "totalRequests": 45,
    "approvedCount": 38,
    "rejectedCount": 5,
    "autoConfirmedCount": 25,
    "cancelledCount": 2,
    "averageProcessingTimeHours": 4.5,
    "approvalRate": 84.4,
    "mostRequestedTimeSlots": [
      {
        "dayOfWeek": 1,
        "hourOfDay": 9,
        "requestCount": 12,
        "approvalRate": 91.7
      },
      {
        "dayOfWeek": 5,
        "hourOfDay": 17,
        "requestCount": 8,
        "approvalRate": 87.5
      }
    ],
    "requestsByCoOwner": [
      {
        "coOwnerId": 5,
        "coOwnerName": "John Smith",
        "totalRequests": 18,
        "approvedRequests": 16,
        "rejectedRequests": 2,
        "approvalRate": 88.9
      },
      {
        "coOwnerId": 6,
        "coOwnerName": "Jane Doe",
        "totalRequests": 15,
        "approvedRequests": 13,
        "rejectedRequests": 2,
        "approvalRate": 86.7
      }
    ]
  }
}
```

---

## 📊 Data Models

### Enums

#### BookingPriority
```csharp
public enum BookingPriority
{
    Low = 0,        // Regular personal use
    Medium = 1,     // Standard commute/errands
    High = 2,       // Important appointments
    Urgent = 3      // Emergency situations
}
```

#### SlotRequestStatus
```csharp
public enum SlotRequestStatus
{
    Pending = 0,           // Awaiting approval
    AutoConfirmed = 1,     // Automatically confirmed (no conflicts)
    Approved = 2,          // Manually approved
    Rejected = 3,          // Rejected by co-owner/system
    Cancelled = 4,         // Cancelled by requester
    Expired = 5,           // Request expired
    ConflictResolved = 6   // Conflict resolved with alternative
}
```

#### SlotAvailabilityStatus
```csharp
public enum SlotAvailabilityStatus
{
    Available = 0,              // Fully available
    PartiallyAvailable = 1,     // Some overlap
    Unavailable = 2,            // Fully booked
    RequiresApproval = 3        // Available but needs approval
}
```

---

## 💼 Business Logic

### Auto-Confirmation Logic

```csharp
if (no conflicts detected)
{
    if (autoConfirmIfAvailable == true)
    {
        // Create booking with status = Confirmed
        // Return status: AutoConfirmed
    }
    else
    {
        // Create booking with status = Pending
        // Return status: Pending
    }
}
else
{
    // Has conflicts
    // Create booking with status = Pending
    // Return status: Pending
    // Provide conflict details
}
```

### Conflict Detection Algorithm

```csharp
// Check for overlapping bookings
var conflicts = bookings.Where(b => 
    b.StatusEnum != Cancelled &&
    (
        (requestStart >= b.Start && requestStart < b.End) ||  // Starts during
        (requestEnd > b.Start && requestEnd <= b.End) ||       // Ends during
        (requestStart <= b.Start && requestEnd >= b.End)       // Completely contains
    )
);

// Calculate overlap hours
overlapHours = (min(requestEnd, bookingEnd) - max(requestStart, bookingStart)).TotalHours;
```

### Alternative Slot Generation

**System generates 4 types of alternatives:**

1. **User-Provided Alternatives** (highest priority)
   - Sorted by `preferenceRank`
   - Checked for availability
   - Scored 90, 80, 70... based on rank

2. **Before Preferred Time** (1.5x duration earlier)
   ```
   Alternative = PreferredStart - (Duration × 1.5)
   ```

3. **After Preferred Time** (1.5x duration later)
   ```
   Alternative = PreferredStart + (Duration × 1.5)
   ```

4. **Same Time Next/Previous Day**
   ```
   Alternative = PreferredStart ± 1 day
   ```

**Recommendation Scoring:**
- Available slot: 70 points
- User-provided alternative (rank 1): 90 points
- User-provided alternative (rank 2): 80 points
- Unavailable slot: 40 points
- Conflict probability: 0-1 (0 = no conflict, 1 = guaranteed conflict)

---

## 🔗 Integration Guide

### Frontend Integration

#### 1. Request Booking Slot Component

```typescript
import axios from 'axios';

interface RequestSlotParams {
  vehicleId: number;
  preferredStartTime: string;
  preferredEndTime: string;
  purpose: string;
  priority: number;
  isFlexible: boolean;
  autoConfirmIfAvailable: boolean;
  estimatedDistance?: number;
  alternativeSlots?: Array<{
    startTime: string;
    endTime: string;
    preferenceRank: number;
  }>;
}

export const requestBookingSlot = async (params: RequestSlotParams) => {
  try {
    const response = await axios.post(
      `/api/booking/vehicle/${params.vehicleId}/request-slot`,
      params,
      {
        headers: {
          'Authorization': `Bearer ${getAuthToken()}`,
          'Content-Type': 'application/json'
        }
      }
    );
    return response.data;
  } catch (error) {
    throw error;
  }
};
```

#### 2. Booking Request UI Component (React Example)

```tsx
import React, { useState } from 'react';
import { requestBookingSlot } from './api';

export const BookingRequestForm = ({ vehicleId }) => {
  const [formData, setFormData] = useState({
    preferredStartTime: '',
    preferredEndTime: '',
    purpose: '',
    priority: 1,
    isFlexible: false,
    autoConfirmIfAvailable: true,
    estimatedDistance: null,
    alternativeSlots: []
  });
  
  const [response, setResponse] = useState(null);
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    
    try {
      const result = await requestBookingSlot({
        vehicleId,
        ...formData
      });
      
      setResponse(result);
      
      // Handle response
      if (result.data.status === 1) {
        // Auto-confirmed
        alert('Booking confirmed automatically!');
      } else if (result.data.status === 0) {
        // Pending approval
        if (result.data.conflictingBookings) {
          // Show conflicts
          console.log('Conflicts:', result.data.conflictingBookings);
        }
        if (result.data.alternativeSuggestions) {
          // Show alternatives
          console.log('Alternatives:', result.data.alternativeSuggestions);
        }
      }
    } catch (error) {
      console.error('Error:', error);
    }
  };
  
  return (
    <form onSubmit={handleSubmit}>
      <h2>Request Booking Slot</h2>
      
      <div>
        <label>Start Time:</label>
        <input
          type="datetime-local"
          value={formData.preferredStartTime}
          onChange={(e) => setFormData({
            ...formData,
            preferredStartTime: e.target.value
          })}
          required
        />
      </div>
      
      <div>
        <label>End Time:</label>
        <input
          type="datetime-local"
          value={formData.preferredEndTime}
          onChange={(e) => setFormData({
            ...formData,
            preferredEndTime: e.target.value
          })}
          required
        />
      </div>
      
      <div>
        <label>Purpose:</label>
        <textarea
          value={formData.purpose}
          onChange={(e) => setFormData({
            ...formData,
            purpose: e.target.value
          })}
          required
        />
      </div>
      
      <div>
        <label>Priority:</label>
        <select
          value={formData.priority}
          onChange={(e) => setFormData({
            ...formData,
            priority: parseInt(e.target.value)
          })}
        >
          <option value={0}>Low</option>
          <option value={1}>Medium</option>
          <option value={2}>High</option>
          <option value={3}>Urgent</option>
        </select>
      </div>
      
      <div>
        <label>
          <input
            type="checkbox"
            checked={formData.isFlexible}
            onChange={(e) => setFormData({
              ...formData,
              isFlexible: e.target.checked
            })}
          />
          Flexible (accept alternative times)
        </label>
      </div>
      
      <div>
        <label>
          <input
            type="checkbox"
            checked={formData.autoConfirmIfAvailable}
            onChange={(e) => setFormData({
              ...formData,
              autoConfirmIfAvailable: e.target.checked
            })}
          />
          Auto-confirm if available
        </label>
      </div>
      
      <button type="submit">Submit Request</button>
      
      {/* Display response */}
      {response && (
        <div className="response-panel">
          {response.data.status === 1 ? (
            <div className="success">
              ✅ Booking Auto-Confirmed!
              <p>{response.data.autoConfirmationMessage}</p>
            </div>
          ) : (
            <div className="pending">
              ⏳ Booking Pending Approval
              
              {response.data.conflictingBookings && (
                <div className="conflicts">
                  <h3>Conflicting Bookings:</h3>
                  {response.data.conflictingBookings.map((conflict) => (
                    <div key={conflict.bookingId}>
                      <p>{conflict.coOwnerName}</p>
                      <p>{conflict.startTime} - {conflict.endTime}</p>
                      <p>Overlap: {conflict.overlapHours} hours</p>
                    </div>
                  ))}
                </div>
              )}
              
              {response.data.alternativeSuggestions && (
                <div className="alternatives">
                  <h3>Alternative Time Slots:</h3>
                  {response.data.alternativeSuggestions.map((alt, idx) => (
                    <div key={idx} className={alt.isAvailable ? 'available' : 'unavailable'}>
                      <p>{alt.startTime} - {alt.endTime}</p>
                      <p>{alt.reason}</p>
                      <p>Score: {alt.recommendationScore}/100</p>
                      {alt.isAvailable && (
                        <button onClick={() => selectAlternative(alt)}>
                          Use This Slot
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </form>
  );
};
```

#### 3. Pending Requests Dashboard

```tsx
export const PendingRequestsDashboard = ({ vehicleId }) => {
  const [pendingRequests, setPendingRequests] = useState(null);
  
  useEffect(() => {
    fetchPendingRequests();
  }, [vehicleId]);
  
  const fetchPendingRequests = async () => {
    const response = await axios.get(
      `/api/booking/vehicle/${vehicleId}/pending-slot-requests`,
      {
        headers: { 'Authorization': `Bearer ${getAuthToken()}` }
      }
    );
    setPendingRequests(response.data.data);
  };
  
  const handleApprove = async (requestId) => {
    await axios.post(
      `/api/booking/slot-request/${requestId}/respond`,
      { isApproved: true },
      {
        headers: { 'Authorization': `Bearer ${getAuthToken()}` }
      }
    );
    fetchPendingRequests(); // Refresh
  };
  
  const handleReject = async (requestId, reason) => {
    await axios.post(
      `/api/booking/slot-request/${requestId}/respond`,
      {
        isApproved: false,
        rejectionReason: reason
      },
      {
        headers: { 'Authorization': `Bearer ${getAuthToken()}` }
      }
    );
    fetchPendingRequests(); // Refresh
  };
  
  return (
    <div>
      <h2>Pending Requests ({pendingRequests?.totalPendingCount || 0})</h2>
      
      {pendingRequests?.pendingRequests.map((request) => (
        <div key={request.requestId} className="request-card">
          <h3>{request.requesterName}</h3>
          <p>{request.preferredStartTime} - {request.preferredEndTime}</p>
          <p>Purpose: {request.purpose}</p>
          <p>Priority: {['Low', 'Medium', 'High', 'Urgent'][request.priority]}</p>
          
          <div className="actions">
            <button onClick={() => handleApprove(request.requestId)}>
              ✅ Approve
            </button>
            <button onClick={() => {
              const reason = prompt('Rejection reason:');
              if (reason) handleReject(request.requestId, reason);
            }}>
              ❌ Reject
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};
```

---

## 📝 Usage Examples

### Example 1: Simple Auto-Confirmed Request

**Scenario:** User wants to book tomorrow morning, no conflicts

**Request:**
```bash
curl -X POST "https://api.example.com/api/booking/vehicle/5/request-slot" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "preferredStartTime": "2025-01-18T09:00:00",
    "preferredEndTime": "2025-01-18T17:00:00",
    "purpose": "Commute to office",
    "priority": 1,
    "isFlexible": false,
    "autoConfirmIfAvailable": true
  }'
```

**Result:** Booking auto-confirmed with status `AutoConfirmed` (1)

---

### Example 2: Flexible Request with Conflicts

**Scenario:** User wants weekend trip but has conflict, provides alternatives

**Request:**
```bash
curl -X POST "https://api.example.com/api/booking/vehicle/5/request-slot" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "preferredStartTime": "2025-01-25T08:00:00",
    "preferredEndTime": "2025-01-25T20:00:00",
    "purpose": "Weekend family trip",
    "priority": 2,
    "isFlexible": true,
    "autoConfirmIfAvailable": true,
    "estimatedDistance": 200,
    "alternativeSlots": [
      {
        "startTime": "2025-01-26T08:00:00",
        "endTime": "2025-01-26T20:00:00",
        "preferenceRank": 1
      }
    ]
  }'
```

**Result:** 
- Conflict detected with existing booking
- System generates 4 alternative slots
- User's alternative (next day) is shown as most recommended
- Booking created with status `Pending` (0)
- Approval required from conflicting co-owner

---

### Example 3: Co-Owner Approves Request

**Scenario:** Jane approves John's request

**Request:**
```bash
curl -X POST "https://api.example.com/api/booking/slot-request/124/respond" \
  -H "Authorization: Bearer JANE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "isApproved": true,
    "notes": "Approved! I will use the alternative time slot."
  }'
```

**Result:** Booking status changed from `Pending` (0) to `Approved` (2)

---

### Example 4: Analytics Dashboard

**Scenario:** Admin views booking request analytics

**Request:**
```bash
curl -X GET "https://api.example.com/api/booking/vehicle/5/slot-request-analytics?startDate=2024-10-17&endDate=2025-01-17" \
  -H "Authorization: Bearer ADMIN_TOKEN"
```

**Result:**
- 84.4% approval rate
- 25 auto-confirmed bookings
- Monday 9 AM is most requested slot
- John Smith has 88.9% approval rate

---

## ⚠️ Error Handling

### Common Error Codes

| Status Code | Message | Cause | Solution |
|------------|---------|-------|----------|
| 400 | VALIDATION_ERROR | Invalid request data | Check FluentValidation rules |
| 401 | INVALID_TOKEN | JWT token invalid/expired | Re-authenticate |
| 403 | USER_NOT_CO_OWNER | User is not a co-owner | Verify user role |
| 403 | ACCESS_DENIED_NOT_VEHICLE_CO_OWNER | Not co-owner of this vehicle | Check VehicleCoOwner relationship |
| 403 | ACCESS_DENIED_NOT_REQUEST_OWNER | Can't cancel someone else's request | Only requester can cancel |
| 404 | VEHICLE_NOT_FOUND | Vehicle doesn't exist | Verify vehicle ID |
| 404 | BOOKING_REQUEST_NOT_FOUND | Request doesn't exist | Verify request ID |
| 400 | BOOKING_REQUEST_ALREADY_PROCESSED | Already approved/rejected | Can't process again |
| 400 | CAN_ONLY_CANCEL_PENDING_REQUESTS | Request not pending | Only pending requests can be cancelled |

### Validation Rules

**RequestBookingSlotRequest:**
- `preferredStartTime`: Must be in the future
- `preferredEndTime`: Must be after start time
- `purpose`: Required, max 500 characters
- `priority`: Must be valid enum (0-3)
- `estimatedDistance`: Must be > 0 if provided
- `alternativeSlots`: Max 5 alternatives

**RespondToSlotRequestRequest:**
- `rejectionReason`: Required if `isApproved = false`
- `rejectionReason`: Max 500 characters
- `suggestedEndTime`: Must be after `suggestedStartTime`
- `notes`: Max 1000 characters

**CancelSlotRequestRequest:**
- `reason`: Required, max 500 characters

---

## 🎨 Frontend UI/UX Recommendations

### 1. Request Form
- **Date/Time Picker**: Use calendar with conflict highlighting
- **Priority Indicator**: Color-coded badges (Low=Green, Medium=Yellow, High=Orange, Urgent=Red)
- **Flexible Toggle**: Enable alternative slots input
- **Distance Estimator**: Google Maps integration
- **Conflict Preview**: Real-time availability check

### 2. Response Visualization
- **Auto-Confirmed**: Green success banner with confetti animation
- **Pending**: Yellow warning banner with clock icon
- **Conflicts**: Red alert with timeline showing overlaps
- **Alternatives**: Cards with recommendation scores, sorted by score

### 3. Pending Requests Dashboard
- **List View**: Sort by priority, date, or requester
- **Quick Actions**: Approve/Reject buttons with tooltips
- **Batch Operations**: Approve multiple requests at once
- **Notification Badge**: Show count of pending approvals

### 4. Analytics Dashboard
- **Approval Rate Chart**: Pie chart (approved vs rejected)
- **Popular Times Heatmap**: Calendar heatmap showing request density
- **Co-Owner Stats Table**: Sortable table with approval rates
- **Trend Line**: Line chart showing requests over time

---

## 🔐 Security Considerations

1. **Authorization**: Always verify user is co-owner of vehicle
2. **Ownership Validation**: Only requester can cancel their requests
3. **Approval Rights**: Any co-owner can approve/reject (except requester)
4. **Data Isolation**: Filter bookings by VehicleCoOwner relationships
5. **JWT Validation**: All endpoints require valid authentication

---

## 🚦 Performance Optimization

### Database Queries
- Use `.Include()` for eager loading relationships
- Indexed columns: `VehicleId`, `CoOwnerId`, `StatusEnum`, `StartTime`, `EndTime`
- Pagination for analytics (default 90 days)

### Caching Strategy
```csharp
// Cache pending requests for 5 minutes
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetPendingSlotRequests(int vehicleId)
```

### Async Operations
- All service methods are async
- Database operations use `await`
- Non-blocking conflict detection

---

## 📈 Future Enhancements

1. **AI-Powered Scheduling**
   - Machine learning to predict optimal booking times
   - Smart conflict resolution suggestions

2. **Calendar Integration**
   - Sync with Google Calendar, Outlook
   - Export bookings to iCal format

3. **Notification System**
   - Email/SMS notifications for approvals
   - Push notifications for conflicts

4. **Booking Templates**
   - Save frequently used booking patterns
   - One-click recurring bookings

5. **Fairness Scoring**
   - Track usage vs ownership percentage
   - Automatic priority adjustment

6. **Group Consensus**
   - Require majority approval for high-priority slots
   - Voting system for conflicting requests

---

## 📞 Support & Contact

For issues, questions, or feature requests:
- GitHub Issues: [Repository Link]
- Email: support@example.com
- Documentation: [Wiki Link]

---

**Last Updated:** January 17, 2025  
**Version:** 1.0.0  
**Author:** EV Co-Ownership Platform Team
