# Booking API Documentation

## 📋 Mục lục
- [Tổng quan](#tổng-quan)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Danh sách API](#danh-sách-api)
- [Basic Booking Operations](#basic-booking-operations)
- [Advanced Booking Features](#advanced-booking-features)
- [Slot Request System](#slot-request-system)
- [Conflict Resolution](#conflict-resolution)
- [Modification & Cancellation](#modification--cancellation)
- [Enums và Constants](#enums-và-constants)
- [Error Codes](#error-codes)
- [Ví dụ sử dụng](#ví-dụ-sử-dụng)

---

## 🎯 Tổng quan

Module Booking API cung cấp hệ thống đặt xe toàn diện cho EV Co-ownership với các tính năng tiên tiến:

### 🚗 Basic Booking Operations
- **CRUD Operations**: Tạo, xem, sửa, xóa booking
- **Role-based Access**: Co-owner chỉ book xe mình tham gia
- **Time Conflict Detection**: Tự động phát hiện xung đột thời gian
- **Status Management**: Pending → Confirmed → Active → Completed

### 🧠 Advanced Intelligence Features
- **Slot Request System**: Yêu cầu slot với auto-approval
- **Conflict Resolution**: Giải quyết xung đột thông minh với ownership weighting
- **Smart Modification**: Sửa booking với impact analysis
- **Enhanced Cancellation**: Hủy với policy-based fees và refund

### 📊 Analytics & Insights
- **Calendar View**: Xem booking calendar theo role
- **Availability Check**: Kiểm tra tình trạng xe trước khi book
- **Usage Statistics**: Thống kê booking theo co-owner
- **Conflict Analytics**: Phân tích pattern xung đột

### 🔄 Workflow Automation
- **Auto-confirmation**: Tự động confirm nếu không có xung đột
- **Alternative Suggestions**: Gợi ý slot khác khi có xung đột
- **Approval Workflow**: Quy trình approve/reject giữa co-owner
- **Notification System**: Thông báo tự động cho co-owner

---

## 🔗 Base URL

```
http://localhost:5215/api/booking
```

Trong production: `https://your-domain.com/api/booking`

---

## 🔐 Authentication

Tất cả endpoints yêu cầu JWT Bearer Token:

```http
Authorization: Bearer {access_token}
```

**Role Requirements:**
- **Co-owner**: Chỉ được book xe mình tham gia
- **Staff/Admin**: Có thể xem/quản lý tất cả booking

---

## 📑 Danh sách API

### Basic Booking Operations
| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 1 | POST | `/` | Tạo booking mới | Co-owner |
| 2 | GET | `/{id}` | Xem booking theo ID | Co-owner, Staff, Admin |
| 3 | GET | `/my-bookings` | Xem booking của tôi | Co-owner |
| 4 | GET | `/vehicle/{vehicleId}` | Xem booking của xe | All |
| 5 | GET | `/` | Xem tất cả booking | Staff, Admin |
| 6 | PUT | `/{id}` | Cập nhật booking | Co-owner |
| 7 | POST | `/{id}/approve` | Duyệt/từ chối booking | Staff, Admin |
| 8 | POST | `/{id}/cancel` | Hủy booking | Co-owner |
| 9 | DELETE | `/{id}` | Xóa booking | Admin |

### Advanced Features
| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 10 | GET | `/statistics` | Thống kê booking | Staff, Admin |
| 11 | GET | `/calendar` | Xem calendar booking | Co-owner, Staff, Admin |
| 12 | GET | `/availability` | Kiểm tra tình trạng xe | Co-owner, Staff, Admin |

### Slot Request System
| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 13 | POST | `/vehicle/{vehicleId}/request-slot` | Yêu cầu slot booking | Co-owner |
| 14 | POST | `/slot-request/{requestId}/respond` | Phản hồi yêu cầu slot | Co-owner |
| 15 | POST | `/slot-request/{requestId}/cancel` | Hủy yêu cầu slot | Co-owner |
| 16 | GET | `/vehicle/{vehicleId}/pending-slot-requests` | Xem yêu cầu slot pending | Co-owner |
| 17 | GET | `/vehicle/{vehicleId}/slot-request-analytics` | Analytics yêu cầu slot | Co-owner |

### Conflict Resolution
| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 18 | POST | `/{bookingId}/resolve-conflict` | Giải quyết xung đột | Co-owner |
| 19 | GET | `/pending-conflicts` | Xem xung đột pending | Co-owner |
| 20 | GET | `/vehicle/{vehicleId}/conflict-analytics` | Analytics xung đột | Co-owner |

### Modification & Cancellation
| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 21 | POST | `/{bookingId}/modify` | Sửa booking với validation | Co-owner |
| 22 | POST | `/{bookingId}/cancel-enhanced` | Hủy với policy và refund | Co-owner |
| 23 | POST | `/validate-modification` | Validate trước khi sửa | Co-owner |
| 24 | GET | `/modification-history` | Lịch sử modification | Co-owner |

---

## 🚗 Basic Booking Operations

### 1. ➕ Tạo booking mới - POST `/`

**Mô tả:** Tạo booking mới với auto conflict detection.

**Role:** Co-owner (xe mình tham gia)

**Request Body:**
```json
{
  "vehicleId": 1,
  "startTime": "2025-01-25T09:00:00Z",
  "endTime": "2025-01-25T17:00:00Z",
  "purpose": "Business trip to downtown",
  "estimatedDistance": 150,
  "usageType": 0
}
```

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "BOOKING_CREATED_SUCCESSFULLY",
  "data": {
    "bookingId": 123,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "coOwnerId": 5,
    "coOwnerName": "John Doe",
    "startTime": "2025-01-25T09:00:00Z",
    "endTime": "2025-01-25T17:00:00Z",
    "duration": 8,
    "purpose": "Business trip to downtown",
    "estimatedDistance": 150,
    "usageType": "Personal",
    "status": "Confirmed",
    "createdAt": "2025-01-17T10:00:00Z"
  }
}
```

**Response 409 - Xung đột thời gian:**
```json
{
  "statusCode": 409,
  "message": "BOOKING_TIME_CONFLICT",
  "data": {
    "conflictingBookings": [
      {
        "bookingId": 120,
        "coOwnerName": "Alice Smith",
        "startTime": "2025-01-25T14:00:00Z",
        "endTime": "2025-01-25T18:00:00Z",
        "status": "Confirmed"
      }
    ]
  }
}
```

---

### 2. 👁️ Xem booking theo ID - GET `/{id}`

**Mô tả:** Lấy thông tin chi tiết một booking.

**Role:** Co-owner (booking của mình), Staff, Admin

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_RETRIEVED_SUCCESSFULLY",
  "data": {
    "bookingId": 123,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "coOwnerId": 5,
    "coOwnerName": "John Doe",
    "startTime": "2025-01-25T09:00:00Z",
    "endTime": "2025-01-25T17:00:00Z",
    "duration": 8,
    "purpose": "Business trip to downtown",
    "estimatedDistance": 150,
    "actualDistance": null,
    "usageType": "Personal",
    "status": "Confirmed",
    "createdAt": "2025-01-17T10:00:00Z",
    "updatedAt": "2025-01-17T10:00:00Z"
  }
}
```

---

### 3. 📋 Xem booking của tôi - GET `/my-bookings`

**Mô tả:** Lấy danh sách booking của user hiện tại.

**Role:** Co-owner

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| pageIndex | int | 1 | Số trang |
| pageSize | int | 10 | Items per page |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "USER_BOOKINGS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "bookingId": 123,
      "vehicleName": "Tesla Model 3",
      "startTime": "2025-01-25T09:00:00Z",
      "endTime": "2025-01-25T17:00:00Z",
      "purpose": "Business trip",
      "status": "Confirmed",
      "daysFromNow": 8
    },
    {
      "bookingId": 124,
      "vehicleName": "VinFast VF8",
      "startTime": "2025-01-30T14:00:00Z",
      "endTime": "2025-01-30T18:00:00Z",
      "purpose": "Shopping",
      "status": "Pending",
      "daysFromNow": 13
    }
  ]
}
```

---

### 4. 🚙 Xem booking của xe - GET `/vehicle/{vehicleId}`

**Mô tả:** Lấy danh sách booking của một xe cụ thể.

**Role:** All (role-based filtering áp dụng)

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_BOOKINGS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "bookingId": 123,
      "coOwnerName": "John Doe",
      "startTime": "2025-01-25T09:00:00Z",
      "endTime": "2025-01-25T17:00:00Z",
      "purpose": "Business trip",
      "status": "Confirmed"
    }
  ]
}
```

---

### 5. 📊 Thống kê booking - GET `/statistics`

**Mô tả:** Lấy thống kê tổng quan về booking trong hệ thống.

**Role:** Staff, Admin

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_STATISTICS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "totalBookings": 1247,
    "pendingBookings": 23,
    "confirmedBookings": 156,
    "activeBookings": 8,
    "completedBookings": 1045,
    "cancelledBookings": 15,
    "totalHoursBooked": 8756,
    "averageBookingDuration": 6.8,
    "mostActiveVehicle": {
      "vehicleId": 5,
      "vehicleName": "Tesla Model 3",
      "bookingCount": 234
    },
    "mostActiveCoOwner": {
      "coOwnerId": 12,
      "coOwnerName": "Alice Johnson",
      "bookingCount": 45
    },
    "bookingsByStatus": {
      "Pending": 23,
      "Confirmed": 156,
      "Active": 8,
      "Completed": 1045,
      "Cancelled": 15
    },
    "monthlyTrends": [
      {
        "month": "2025-01",
        "totalBookings": 89,
        "averageDuration": 7.2
      }
    ]
  }
}
```

---

## 🧠 Advanced Booking Features

### 6. 📅 Xem calendar booking - GET `/calendar`

**Mô tả:** Xem calendar booking theo role-based access.

**Role:** Co-owner (xe mình tham gia), Staff/Admin (tất cả)

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | ✅ | Ngày bắt đầu (yyyy-MM-dd) |
| endDate | DateTime | ✅ | Ngày kết thúc (yyyy-MM-dd) |
| vehicleId | int | ❌ | Filter theo xe |
| status | string | ❌ | Filter theo status |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_CALENDAR_RETRIEVED_SUCCESSFULLY",
  "data": {
    "dateRange": {
      "startDate": "2025-01-17",
      "endDate": "2025-01-24",
      "totalDays": 7
    },
    "calendarEvents": [
      {
        "bookingId": 123,
        "eventDate": "2025-01-25",
        "startTime": "09:00",
        "endTime": "17:00",
        "durationHours": 8,
        "vehicleId": 1,
        "vehicleName": "Tesla Model 3",
        "licensePlate": "51A-12345",
        "coOwnerId": 5,
        "coOwnerName": "John Doe",
        "purpose": "Business trip",
        "status": "Confirmed",
        "isMyBooking": false
      }
    ],
    "summary": {
      "totalBookings": 12,
      "myBookings": 3,
      "statusBreakdown": {
        "Pending": 2,
        "Confirmed": 8,
        "Active": 1,
        "Completed": 1
      },
      "vehicleUtilization": [
        {
          "vehicleId": 1,
          "vehicleName": "Tesla Model 3",
          "bookedHours": 24,
          "utilizationPercent": 14.3
        }
      ]
    }
  }
}
```

---

### 7. ✅ Kiểm tra tình trạng xe - GET `/availability`

**Mô tả:** Kiểm tra xe có available trong khoảng thời gian không.

**Role:** Co-owner, Staff, Admin

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID xe cần check |
| startTime | DateTime | ✅ | Thời gian bắt đầu |
| endTime | DateTime | ✅ | Thời gian kết thúc |

**Response 200 - Available:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_AVAILABLE",
  "data": {
    "vehicleId": 5,
    "vehicleName": "VinFast VF8",
    "isAvailable": true,
    "message": "VEHICLE_AVAILABLE",
    "conflictingBookings": null
  }
}
```

**Response 200 - Not Available:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
  "data": {
    "vehicleId": 5,
    "vehicleName": "VinFast VF8",
    "isAvailable": false,
    "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
    "conflictingBookings": [
      {
        "bookingId": 123,
        "coOwnerName": "Nguyen Van A",
        "startTime": "2025-01-18T10:00:00Z",
        "endTime": "2025-01-18T15:00:00Z",
        "status": "Confirmed"
      }
    ]
  }
}
```

---

## 🎯 Slot Request System

### 8. 🎫 Yêu cầu slot booking - POST `/vehicle/{vehicleId}/request-slot`

**Mô tả:** Yêu cầu slot với intelligent conflict detection và auto-confirmation.

**Role:** Co-owner

**Request Body:**
```json
{
  "preferredStartTime": "2025-01-25T09:00:00Z",
  "preferredEndTime": "2025-01-25T17:00:00Z",
  "purpose": "Business trip to downtown",
  "priority": 2,
  "isFlexible": true,
  "autoConfirmIfAvailable": true,
  "estimatedDistance": 150,
  "usageType": 0,
  "alternativeSlots": [
    {
      "startTime": "2025-01-25T10:00:00Z",
      "endTime": "2025-01-25T18:00:00Z",
      "preferenceRank": 1
    }
  ]
}
```

**Response 201 - Auto-Confirmed:**
```json
{
  "statusCode": 201,
  "message": "BOOKING_SLOT_AUTO_CONFIRMED",
  "data": {
    "requestId": 123,
    "bookingId": 123,
    "status": 1,
    "availabilityStatus": 0,
    "autoConfirmationMessage": "Slot was automatically confirmed as it's available with no conflicts",
    "conflictingBookings": null,
    "alternativeSuggestions": null,
    "metadata": {
      "requiresCoOwnerApproval": false,
      "systemRecommendation": "Your preferred slot is available and can be confirmed"
    }
  }
}
```

**Response 201 - Has Conflicts:**
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
        "coOwnerName": "John Smith",
        "startTime": "2025-01-25T14:00:00Z",
        "endTime": "2025-01-25T19:00:00Z",
        "status": 1,
        "overlapHours": 3.0
      }
    ],
    "alternativeSuggestions": [
      {
        "startTime": "2025-01-25T06:00:00Z",
        "endTime": "2025-01-25T14:00:00Z",
        "isAvailable": true,
        "reason": "Earlier the same day",
        "recommendationScore": 70
      }
    ],
    "metadata": {
      "requiresCoOwnerApproval": true,
      "approvalPendingFrom": ["John Smith"],
      "systemRecommendation": "Your slot conflicts with 1 booking(s). Co-owner approval required."
    }
  }
}
```

**Priority Levels:**
- **0**: Low - Regular personal use
- **1**: Medium - Standard commute/errands  
- **2**: High - Important appointments
- **3**: Urgent - Emergency situations

---

### 9. 👍 Phản hồi yêu cầu slot - POST `/slot-request/{requestId}/respond`

**Mô tả:** Approve/reject yêu cầu slot từ co-owner khác.

**Role:** Co-owner

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
  "suggestedStartTime": "2025-01-26T09:00:00Z",
  "suggestedEndTime": "2025-01-26T17:00:00Z",
  "notes": "Can you use it the next day instead?"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_REQUEST_APPROVED",
  "data": {
    "requestId": 124,
    "status": 2,
    "processedAt": "2025-01-17T10:30:00Z",
    "processedBy": "Alice"
  }
}
```

---

### 10. 📋 Xem yêu cầu slot pending - GET `/vehicle/{vehicleId}/pending-slot-requests`

**Mô tả:** Xem tất cả yêu cầu slot đang chờ approval.

**Role:** Co-owner

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "PENDING_REQUESTS_RETRIEVED",
  "data": {
    "vehicleId": 5,
    "vehicleName": "Tesla Model 3",
    "totalPendingCount": 3,
    "oldestRequestDate": "2025-01-15T10:00:00Z",
    "pendingRequests": [
      {
        "requestId": 125,
        "requesterName": "John Smith",
        "preferredStartTime": "2025-01-25T09:00:00Z",
        "preferredEndTime": "2025-01-25T17:00:00Z",
        "purpose": "Business trip",
        "priority": 2,
        "requestedAt": "2025-01-15T10:00:00Z"
      }
    ]
  }
}
```

---

## ⚔️ Conflict Resolution

### 11. 🛠️ Giải quyết xung đột - POST `/{bookingId}/resolve-conflict`

**Mô tả:** Advanced conflict resolution với ownership weighting.

**Role:** Co-owner

**Request Body (Priority Override):**
```json
{
  "isApproved": false,
  "resolutionType": 2,
  "useOwnershipWeighting": true,
  "priorityJustification": "I have 60% ownership and less usage this month",
  "rejectionReason": "I need priority for this booking"
}
```

**Request Body (Auto-Negotiation):**
```json
{
  "isApproved": true,
  "resolutionType": 3,
  "enableAutoNegotiation": true,
  "useOwnershipWeighting": true
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_CONFLICT_RESOLVED_APPROVED",
  "data": {
    "bookingId": 124,
    "outcome": 0,
    "finalStatus": 1,
    "resolvedBy": "Alice Johnson",
    "resolvedAt": "2025-01-17T14:30:00Z",
    "resolutionExplanation": "Booking approved by Alice Johnson. Conflicting bookings cancelled.",
    "stakeholders": [
      {
        "userId": 5,
        "name": "Alice Johnson",
        "ownershipPercentage": 40,
        "usageHoursThisMonth": 45,
        "hasApproved": true,
        "priorityWeight": 35
      }
    ],
    "approvalStatus": {
      "totalStakeholders": 1,
      "approvalsReceived": 1,
      "rejectionsReceived": 0,
      "isFullyApproved": true,
      "approvalPercentage": 100,
      "weightedApprovalPercentage": 40
    }
  }
}
```

**Resolution Types:**
- **0**: SimpleApproval - Basic approve/reject
- **1**: CounterOffer - Reject với alternative time
- **2**: PriorityOverride - Dùng ownership % để quyết định
- **3**: AutoNegotiation - System tự resolve
- **4**: ConsensusRequired - Tất cả phải approve

---

### 12. 📊 Analytics xung đột - GET `/vehicle/{vehicleId}/conflict-analytics`

**Mô tả:** Phân tích pattern xung đột và co-owner behavior.

**Role:** Co-owner

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| startDate | DateTime | 90 days ago | Ngày bắt đầu phân tích |
| endDate | DateTime | Today | Ngày kết thúc phân tích |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "CONFLICT_ANALYTICS_RETRIEVED",
  "data": {
    "totalConflictsResolved": 45,
    "totalConflictsPending": 3,
    "averageResolutionTimeHours": 12.5,
    "approvalRate": 68.9,
    "rejectionRate": 31.1,
    "autoResolutionRate": 15.6,
    "statsByCoOwner": [
      {
        "userId": 5,
        "name": "Alice Johnson",
        "conflictsInitiated": 12,
        "conflictsReceived": 8,
        "approvalsGiven": 6,
        "rejectionsGiven": 2,
        "successRateAsRequester": 75.0,
        "averageResponseTimeHours": 8.2
      }
    ],
    "commonPatterns": [
      {
        "pattern": "High weekend conflict rate",
        "occurrences": 15,
        "recommendation": "Consider implementing weekend rotation schedule"
      }
    ],
    "recommendations": [
      {
        "recommendation": "Implement weekend rotation schedule",
        "rationale": "High weekend conflict rate detected",
        "suggestedApproach": 4
      }
    ]
  }
}
```

---

## ✏️ Modification & Cancellation

### 13. 🔧 Sửa booking - POST `/{bookingId}/modify`

**Mô tả:** Sửa booking với conflict validation và impact analysis.

**Role:** Co-owner (phải là người tạo booking)

**Request Body:**
```json
{
  "newStartTime": "2025-01-25T14:00:00Z",
  "newEndTime": "2025-01-25T18:00:00Z",
  "newPurpose": "Updated: Shopping and errands",
  "modificationReason": "Need to extend the booking by 1 hour",
  "skipConflictCheck": false,
  "notifyAffectedCoOwners": true,
  "requestApprovalIfConflict": false
}
```

**Response 200 - No Conflicts:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_MODIFIED_SUCCESSFULLY",
  "data": {
    "bookingId": 123,
    "modificationApplied": true,
    "newStatus": "Confirmed",
    "changes": {
      "timeChanged": true,
      "purposeChanged": true,
      "oldStartTime": "2025-01-25T09:00:00Z",
      "newStartTime": "2025-01-25T14:00:00Z",
      "timeDeltaHours": 5
    },
    "conflictAnalysis": {
      "hasConflicts": false,
      "conflictCount": 0
    }
  }
}
```

**Response 202 - Pending Approval:**
```json
{
  "statusCode": 202,
  "message": "MODIFICATION_PENDING_APPROVAL",
  "data": {
    "bookingId": 123,
    "modificationApplied": false,
    "newStatus": "PendingApproval",
    "conflictAnalysis": {
      "hasConflicts": true,
      "conflictCount": 1,
      "conflictingBookings": [
        {
          "bookingId": 125,
          "coOwnerName": "Bob Wilson",
          "overlapHours": 2.0
        }
      ]
    },
    "approvalRequired": {
      "approvalNeededFrom": ["Bob Wilson"],
      "estimatedApprovalTimeHours": 24
    }
  }
}
```

---

### 14. 🚫 Hủy booking nâng cao - POST `/{bookingId}/cancel-enhanced`

**Mô tả:** Hủy booking với policy-based fees và refund calculation.

**Role:** Co-owner (phải là người tạo booking)

**Request Body (Normal Cancellation):**
```json
{
  "cancellationReason": "Plans changed - no longer need the vehicle",
  "cancellationType": 0,
  "requestReschedule": false,
  "acceptCancellationFee": true
}
```

**Request Body (Reschedule):**
```json
{
  "cancellationReason": "Need to reschedule to next weekend",
  "cancellationType": 0,
  "requestReschedule": true,
  "preferredRescheduleStart": "2025-02-01T09:00:00Z",
  "preferredRescheduleEnd": "2025-02-01T13:00:00Z",
  "acceptCancellationFee": true
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "BOOKING_CANCELLED_SUCCESSFULLY",
  "data": {
    "bookingId": 123,
    "cancellationStatus": "Cancelled",
    "cancelledAt": "2025-01-17T10:30:00Z",
    "cancellationPolicy": {
      "hoursBeforeBooking": 48,
      "gracePeriod": true,
      "cancellationFeePercent": 0,
      "refundPercent": 100
    },
    "financialImpact": {
      "originalAmount": 500000,
      "cancellationFee": 0,
      "refundAmount": 500000,
      "refundMethod": "Original payment method"
    },
    "rescheduleInfo": {
      "wasRescheduled": false,
      "newBookingId": null
    }
  }
}
```

**Cancellation Policy:**
| Time Before Booking | Fee | Refund % | Grace Period |
|---------------------|-----|----------|--------------|
| 24+ hours | 0% | 100% | ✅ Yes |
| 2-24 hours | 25% | 75% | ❌ No |
| Less than 2 hours | 50% | 50% | ❌ No |
| After start | 100% | 0% | ❌ No |

**Cancellation Types:**
- **0**: UserInitiated - Follows policy above
- **1**: SystemCancelled - Full refund
- **2**: Emergency - No fee, full refund
- **3**: VehicleUnavailable - No fee, full refund
- **4**: MaintenanceRequired - No fee, full refund

---

### 15. ✅ Validate modification - POST `/validate-modification`

**Mô tả:** Pre-validate modification trước khi apply changes.

**Role:** Co-owner

**Request Body:**
```json
{
  "bookingId": 42,
  "newStartTime": "2025-01-26T09:00:00Z",
  "newEndTime": "2025-01-26T13:00:00Z",
  "newPurpose": "Grocery shopping and bank errands"
}
```

**Response 200 - No Conflicts:**
```json
{
  "statusCode": 200,
  "message": "VALIDATION_PASSED",
  "data": {
    "isValid": true,
    "hasConflicts": false,
    "validationErrors": [],
    "warnings": [],
    "impactAnalysis": {
      "hasTimeChange": true,
      "hasConflicts": false,
      "conflictCount": 0,
      "timeDeltaHours": 1,
      "requiresCoOwnerApproval": false,
      "impactSummary": "No conflicts - modification can proceed"
    },
    "recommendation": "✅ Modification can proceed without issues."
  }
}
```

**Response 200 - With Conflicts:**
```json
{
  "statusCode": 200,
  "message": "VALIDATION_PASSED",
  "data": {
    "isValid": true,
    "hasConflicts": true,
    "validationErrors": [],
    "warnings": [
      "Modification creates 2 conflict(s)"
    ],
    "impactAnalysis": {
      "hasTimeChange": true,
      "hasConflicts": true,
      "conflictCount": 2,
      "conflictingBookings": [
        {
          "bookingId": 45,
          "coOwnerName": "Alice Johnson",
          "startTime": "2025-01-26T10:00:00Z",
          "endTime": "2025-01-26T12:00:00Z",
          "status": 1,
          "purpose": "Weekend trip",
          "overlapHours": 2.0
        }
      ],
      "requiresCoOwnerApproval": true,
      "impactSummary": "2 conflict(s) - requires co-owner approval"
    },
    "alternativeSuggestions": [
      {
        "startTime": "2025-01-26T13:00:00Z",
        "endTime": "2025-01-26T17:00:00Z",
        "reason": "Next available slot after conflicts",
        "hasConflict": false
      }
    ],
    "recommendation": "⚠️ Modification creates 2 conflict(s). Consider alternative time slots or request co-owner approval."
  }
}
```

---

### 16. 📝 Lịch sử modification - GET `/modification-history`

**Mô tả:** Audit trail của tất cả modifications và cancellations.

**Role:** Co-owner

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| bookingId | int | Filter theo booking |
| userId | int | Filter theo user |
| startDate | DateTime | Filter từ ngày |
| endDate | DateTime | Filter đến ngày |
| status | ModificationStatus | Filter theo status |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MODIFICATION_HISTORY_RETRIEVED",
  "data": {
    "totalModifications": 156,
    "totalCancellations": 23,
    "modifications": [
      {
        "id": 301,
        "bookingId": 123,
        "modificationType": "TimeChange",
        "modifiedBy": "John Doe",
        "modifiedAt": "2025-01-17T14:30:00Z",
        "reason": "Need to extend booking by 2 hours",
        "status": "Success",
        "beforeSnapshot": {
          "startTime": "2025-01-25T09:00:00Z",
          "endTime": "2025-01-25T17:00:00Z",
          "purpose": "Business trip"
        },
        "afterSnapshot": {
          "startTime": "2025-01-25T09:00:00Z",
          "endTime": "2025-01-25T19:00:00Z",
          "purpose": "Business trip"
        }
      }
    ]
  }
}
```

---

## 🔢 Enums và Constants

### Booking Status (EBookingStatus)
```typescript
enum EBookingStatus {
  Pending = 0,       // Chờ duyệt
  Confirmed = 1,     // Đã xác nhận
  Active = 2,        // Đang sử dụng
  Completed = 3,     // Hoàn thành
  Cancelled = 4,     // Đã hủy
  PendingApproval = 5 // Chờ approval (modification)
}
```

### Usage Type (EUsageType)
```typescript
enum EUsageType {
  Personal = 0,      // Cá nhân
  Business = 1,      // Công việc
  Emergency = 2,     // Khẩn cấp
  Maintenance = 3    // Bảo dưỡng
}
```

### Priority Levels
```typescript
enum PriorityLevel {
  Low = 0,          // Thấp
  Medium = 1,       // Trung bình
  High = 2,         // Cao
  Urgent = 3        // Khẩn cấp
}
```

### Resolution Types
```typescript
enum ResolutionType {
  SimpleApproval = 0,    // Approve/reject đơn giản
  CounterOffer = 1,      // Reject với alternative
  PriorityOverride = 2,  // Dùng ownership % để quyết định
  AutoNegotiation = 3,   // System tự resolve
  ConsensusRequired = 4  // Tất cả phải approve
}
```

### Cancellation Types
```typescript
enum CancellationType {
  UserInitiated = 0,        // User hủy - theo policy
  SystemCancelled = 1,      // System hủy - full refund
  Emergency = 2,            // Khẩn cấp - no fee
  VehicleUnavailable = 3,   // Xe không available - no fee
  MaintenanceRequired = 4   // Cần bảo dưỡng - no fee
}
```

---

## ❌ Error Codes

### Booking Access Errors (4xx)
| Status | Message | Ý nghĩa |
|--------|---------|---------|
| 403 | `ACCESS_DENIED_NOT_VEHICLE_CO_OWNER` | Không phải co-owner của xe |
| 403 | `ACCESS_DENIED_NOT_BOOKING_CREATOR` | Không phải người tạo booking |
| 404 | `BOOKING_NOT_FOUND` | Không tìm thấy booking |
| 404 | `VEHICLE_NOT_FOUND` | Không tìm thấy xe |

### Booking Conflict Errors (4xx)
| Status | Message | Ý nghĩa |
|--------|---------|---------|
| 409 | `BOOKING_TIME_CONFLICT` | Xung đột thời gian với booking khác |
| 400 | `BOOKING_ALREADY_PROCESSED` | Booking đã được xử lý |
| 400 | `CANNOT_MODIFY_COMPLETED_BOOKING` | Không thể sửa booking đã hoàn thành |
| 400 | `CANNOT_CANCEL_COMPLETED_BOOKING` | Không thể hủy booking đã hoàn thành |

### Slot Request Errors (4xx)
| Status | Message | Ý nghĩa |
|--------|---------|---------|
| 400 | `SLOT_REQUEST_ALREADY_PROCESSED` | Yêu cầu slot đã được xử lý |
| 400 | `INVALID_PRIORITY_LEVEL` | Priority level không hợp lệ |
| 400 | `INVALID_TIME_RANGE` | Khoảng thời gian không hợp lệ |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Quy trình booking cơ bản

```javascript
// 1. Kiểm tra xe available trước
const checkAvailable = await fetch('/api/booking/availability?vehicleId=1&startTime=2025-01-25T09:00:00Z&endTime=2025-01-25T17:00:00Z', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const availability = await checkAvailable.json();

if (availability.data.isAvailable) {
  // 2. Tạo booking
  const createResponse = await fetch('/api/booking', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      vehicleId: 1,
      startTime: "2025-01-25T09:00:00Z",
      endTime: "2025-01-25T17:00:00Z",
      purpose: "Business trip to downtown",
      estimatedDistance: 150,
      usageType: 0
    })
  });

  const booking = await createResponse.json();
  
  if (booking.statusCode === 201) {
    console.log(`Booking created: ${booking.data.bookingId}`);
    console.log(`Status: ${booking.data.status}`);
  }
} else {
  console.log('Vehicle not available');
  console.log('Conflicting bookings:', availability.data.conflictingBookings);
}
```

### Use Case 2: Slot request với auto-confirmation

```javascript
// 1. Request slot với auto-confirmation
const requestResponse = await fetch('/api/booking/vehicle/1/request-slot', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    preferredStartTime: "2025-01-25T09:00:00Z",
    preferredEndTime: "2025-01-25T17:00:00Z",
    purpose: "Business trip to downtown",
    priority: 2,
    isFlexible: true,
    autoConfirmIfAvailable: true,
    estimatedDistance: 150,
    usageType: 0,
    alternativeSlots: [
      {
        startTime: "2025-01-25T10:00:00Z",
        endTime: "2025-01-25T18:00:00Z",
        preferenceRank: 1
      }
    ]
  })
});

const request = await requestResponse.json();

if (request.data.status === 1) {
  console.log('🎉 Auto-confirmed! Booking ID:', request.data.bookingId);
} else if (request.data.status === 0) {
  console.log('⏳ Pending approval from:', request.data.metadata.approvalPendingFrom);
  console.log('Alternative suggestions:', request.data.alternativeSuggestions);
}
```

### Use Case 3: Xem calendar và xử lý conflicts

```javascript
// 1. Xem calendar để plan booking
const calendarResponse = await fetch('/api/booking/calendar?startDate=2025-01-17&endDate=2025-01-24', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const calendar = await calendarResponse.json();

console.log('📅 Calendar Summary:');
console.log(`Total bookings: ${calendar.data.summary.totalBookings}`);
console.log(`My bookings: ${calendar.data.summary.myBookings}`);

// 2. Tìm slot trống
const events = calendar.data.calendarEvents;
const busyTimes = events.map(e => ({
  start: new Date(e.eventDate + 'T' + e.startTime),
  end: new Date(e.eventDate + 'T' + e.endTime),
  vehicle: e.vehicleName
}));

console.log('🚗 Busy times:', busyTimes);

// 3. Nếu có pending conflicts, xử lý
const conflictsResponse = await fetch('/api/booking/pending-conflicts', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const conflicts = await conflictsResponse.json();

if (conflicts.data.totalConflicts > 0) {
  console.log(`⚠️ You have ${conflicts.data.requiringMyAction} conflict(s) requiring action`);
  
  for (const conflict of conflicts.data.conflicts) {
    if (conflict.canAutoResolve) {
      console.log(`🤖 Auto-resolvable conflict: ${conflict.autoResolutionPreview.explanation}`);
    }
  }
}
```

### Use Case 4: Modification workflow

```javascript
// 1. Validate modification trước
const validateResponse = await fetch('/api/booking/validate-modification', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    bookingId: 42,
    newStartTime: "2025-01-26T09:00:00Z",
    newEndTime: "2025-01-26T13:00:00Z",
    newPurpose: "Updated purpose"
  })
});

const validation = await validateResponse.json();

if (validation.data.isValid && !validation.data.hasConflicts) {
  // 2. Apply modification nếu không có conflict
  const modifyResponse = await fetch('/api/booking/42/modify', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      newStartTime: "2025-01-26T09:00:00Z",
      newEndTime: "2025-01-26T13:00:00Z",
      newPurpose: "Updated purpose",
      modificationReason: "Need to change time",
      skipConflictCheck: false,
      notifyAffectedCoOwners: true,
      requestApprovalIfConflict: false
    })
  });

  const modify = await modifyResponse.json();
  
  if (modify.statusCode === 200) {
    console.log('✅ Modification applied successfully');
  } else if (modify.statusCode === 202) {
    console.log('⏳ Modification pending approval');
  }
} else if (validation.data.hasConflicts) {
  console.log('⚠️ Conflicts detected:');
  validation.data.impactAnalysis.conflictingBookings.forEach(conflict => {
    console.log(`- ${conflict.coOwnerName}: ${conflict.overlapHours}h overlap`);
  });

  console.log('💡 Alternative suggestions:');
  validation.data.alternativeSuggestions.forEach(alt => {
    console.log(`- ${alt.startTime} to ${alt.endTime}: ${alt.reason}`);
  });
}
```

### Use Case 5: Enhanced cancellation với reschedule

```javascript
// 1. Cancel với reschedule option
const cancelResponse = await fetch('/api/booking/42/cancel-enhanced', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    cancellationReason: "Need to reschedule to next weekend",
    cancellationType: 0, // UserInitiated
    requestReschedule: true,
    preferredRescheduleStart: "2025-02-01T09:00:00Z",
    preferredRescheduleEnd: "2025-02-01T13:00:00Z",
    acceptCancellationFee: true
  })
});

const cancel = await cancelResponse.json();

console.log('💰 Financial Impact:');
console.log(`Original amount: ${cancel.data.financialImpact.originalAmount.toLocaleString()} VND`);
console.log(`Cancellation fee: ${cancel.data.financialImpact.cancellationFee.toLocaleString()} VND`);
console.log(`Refund amount: ${cancel.data.financialImpact.refundAmount.toLocaleString()} VND`);

if (cancel.data.rescheduleInfo.wasRescheduled) {
  console.log(`🔄 Rescheduled to booking ID: ${cancel.data.rescheduleInfo.newBookingId}`);
} else {
  console.log('❌ Cancelled without reschedule');
}

// 2. Check modification history
const historyResponse = await fetch('/api/booking/modification-history?bookingId=42', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const history = await historyResponse.json();

console.log('📝 Modification History:');
history.data.modifications.forEach(mod => {
  console.log(`${mod.modifiedAt}: ${mod.modificationType} by ${mod.modifiedBy}`);
  console.log(`Reason: ${mod.reason}`);
});
```

### Use Case 6: Advanced conflict resolution

```javascript
// 1. Resolve conflict với priority override
const resolveResponse = await fetch('/api/booking/124/resolve-conflict', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    isApproved: false,
    resolutionType: 2, // PriorityOverride
    useOwnershipWeighting: true,
    priorityJustification: "I have 60% ownership and less usage this month",
    rejectionReason: "I need priority for this booking"
  })
});

const resolution = await resolveResponse.json();

console.log('⚖️ Conflict Resolution:');
console.log(`Outcome: ${resolution.data.outcome}`);
console.log(`Resolved by: ${resolution.data.resolvedBy}`);
console.log(`Explanation: ${resolution.data.resolutionExplanation}`);

console.log('👥 Stakeholder Analysis:');
resolution.data.stakeholders.forEach(stakeholder => {
  console.log(`${stakeholder.name}: ${stakeholder.ownershipPercentage}% ownership, ${stakeholder.usageHoursThisMonth}h usage`);
  console.log(`Priority weight: ${stakeholder.priorityWeight}`);
});

console.log('📊 Approval Status:');
console.log(`Approval rate: ${resolution.data.approvalStatus.approvalPercentage}%`);
console.log(`Weighted approval: ${resolution.data.approvalStatus.weightedApprovalPercentage}%`);
```

---

## 🔐 Best Practices

### 1. Always check availability first

```javascript
async function smartBooking(vehicleId, startTime, endTime, purpose) {
  // 1. Check availability
  const available = await checkAvailability(vehicleId, startTime, endTime);
  
  if (!available.data.isAvailable) {
    // 2. Use slot request system instead
    return await requestSlot(vehicleId, {
      preferredStartTime: startTime,
      preferredEndTime: endTime,
      purpose: purpose,
      autoConfirmIfAvailable: true,
      isFlexible: true
    });
  }
  
  // 3. Direct booking if available
  return await createBooking(vehicleId, startTime, endTime, purpose);
}
```

### 2. Handle conflicts gracefully

```javascript
async function handleBookingConflicts(conflictData) {
  console.log('⚠️ Booking conflicts detected');
  
  // Show alternative suggestions
  if (conflictData.alternativeSuggestions?.length > 0) {
    console.log('💡 Alternative time slots:');
    conflictData.alternativeSuggestions.forEach((alt, index) => {
      console.log(`${index + 1}. ${alt.startTime} - ${alt.endTime}`);
      console.log(`   Reason: ${alt.reason}`);
      console.log(`   Score: ${alt.recommendationScore}/100`);
    });
  }
  
  // Auto-resolve if possible
  if (conflictData.canAutoResolve) {
    const autoResolution = conflictData.autoResolutionPreview;
    console.log(`🤖 Auto-resolution available: ${autoResolution.explanation}`);
    console.log(`Confidence: ${autoResolution.confidence * 100}%`);
  }
}
```

### 3. Validate before modifying

```javascript
async function safeModifyBooking(bookingId, changes) {
  // 1. Always validate first
  const validation = await validateModification({
    bookingId: bookingId,
    ...changes
  });
  
  if (!validation.data.isValid) {
    console.error('❌ Modification not allowed:', validation.data.validationErrors);
    return false;
  }
  
  if (validation.data.hasConflicts) {
    console.warn('⚠️ Modification creates conflicts');
    
    // Show user the impact
    const impact = validation.data.impactAnalysis;
    console.log(`Conflicts: ${impact.conflictCount}`);
    console.log(`Requires approval: ${impact.requiresCoOwnerApproval}`);
    
    // Ask user confirmation
    const userConfirms = confirm(
      `This modification creates ${impact.conflictCount} conflict(s). ` +
      `${impact.requiresCoOwnerApproval ? 'Co-owner approval required.' : ''} Continue?`
    );
    
    if (!userConfirms) return false;
  }
  
  // 2. Apply modification
  return await modifyBooking(bookingId, {
    ...changes,
    requestApprovalIfConflict: true,
    notifyAffectedCoOwners: true
  });
}
```

---

## 📞 Liên hệ và Hỗ trợ

- **API Documentation:** http://localhost:5215/swagger
- **Backend Team:** [Your team contact]
- **Issues:** [GitHub Issues URL]

---

**Last Updated:** 2025-01-17  
**Version:** 2.0.0  
**Author:** Backend Development Team