````markdown
# Vehicle API Documentation

## 📋 Mục lục
- [Tổng quan](#tổng-quan)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Danh sách API](#danh-sách-api)
- [Chi tiết từng API](#chi-tiết-từng-api)
- [Enums và Constants](#enums-và-constants)
- [Error Codes](#error-codes)
- [Ví dụ sử dụng](#ví-dụ-sử-dụng)

---

## 🎯 Tổng quan

Module Vehicle API cung cấp các chức năng quản lý phương tiện trong hệ thống EV Co-ownership, bao gồm:
- **Tạo phương tiện mới** (Co-owner có thể tạo)
- **Quản lý đồng sở hữu** (mời, phản hồi, xóa co-owner)
- **Xem danh sách phương tiện** (role-based access)
- **Lịch trình và tính khả dụng** (availability schedule, find slots)
- **Phân tích sử dụng** (utilization comparison)
- **Chi tiết phương tiện đầy đủ** (fund, co-owners, specifications)

**Đặc điểm chính:**
- **Role-based Access Control**: Co-owner, Staff, Admin có quyền khác nhau
- **Advanced Filtering & Pagination**: Filter theo brand, model, price, year, etc.
- **Real-time Availability**: Kiểm tra xe trống/bận theo thời gian thực
- **Investment Management**: Quản lý tỷ lệ sở hữu và khoản đầu tư

---

## 🔗 Base URL

```
http://localhost:5215/api/vehicle
```

Trong production: `https://your-domain.com/api/vehicle`

---

## 🔐 Authentication

Tất cả endpoints yêu cầu JWT Bearer Token trong header:

```http
Authorization: Bearer {access_token}
```

**Role Requirements:**
- **Co-owner**: Có thể tạo xe, quản lý xe của mình, xem xe trong group
- **Staff**: Có thể xem tất cả xe, hỗ trợ quản lý
- **Admin**: Quyền đầy đủ, có thể quản lý mọi xe

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 1 | POST | `/` | Tạo phương tiện mới | Co-owner |
| 2 | POST | `/{vehicleId}/co-owners` | Thêm đồng sở hữu | Co-owner, Staff, Admin |
| 3 | PUT | `/{vehicleId}/invitations/respond` | Phản hồi lời mời đồng sở hữu | Co-owner |
| 4 | GET | `/{vehicleId}/details` | Xem chi tiết xe (legacy) | Co-owner |
| 5 | GET | `/my-vehicles` | Xem xe của tôi | Co-owner |
| 6 | GET | `/invitations/pending` | Xem lời mời chờ duyệt | Co-owner |
| 7 | DELETE | `/{vehicleId}/co-owners/{coOwnerUserId}` | Xóa đồng sở hữu | Co-owner, Staff, Admin |
| 8 | PUT | `/{vehicleId}` | Cập nhật thông tin xe | Co-owner, Staff, Admin |
| 9 | GET | `/available` | Xem xe khả dụng (với filter) | Co-owner, Staff, Admin |
| 10 | GET | `/{vehicleId}` | Xem chi tiết xe đầy đủ | Co-owner, Staff, Admin |
| 11 | GET | `/validate-creation-eligibility` | [DEV] Kiểm tra điều kiện tạo xe | Any |
| 12 | GET | `/{vehicleId}/availability/schedule` | Xem lịch trình sử dụng xe | Co-owner, Staff, Admin |
| 13 | GET | `/{vehicleId}/availability/find-slots` | Tìm slot trống để đặt xe | Co-owner, Staff, Admin |
| 14 | GET | `/utilization/compare` | So sánh hiệu suất sử dụng | Co-owner, Staff, Admin |

---

## 📖 Chi tiết từng API

### 1. 🚗 Tạo phương tiện mới - POST `/`

**Mô tả:** Tạo phương tiện mới trong hệ thống. Người tạo sẽ trở thành chủ sở hữu chính.

**Role:** Co-owner

**Request Body:**
```json
{
  "name": "Tesla Model 3 2024",
  "brand": "Tesla",
  "model": "Model 3",
  "year": 2024,
  "vin": "1HGCM82633A123456",
  "licensePlate": "51A-12345",
  "color": "Pearl White",
  "batteryCapacity": 75.0,
  "range": 448,
  "purchaseDate": "2024-01-15",
  "purchasePrice": 1500000000,
  "warrantyExpiryDate": "2027-01-15",
  "latitude": 10.762622,
  "longitude": 106.660172
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| name | string | ✅ | Max 100 chars |
| brand | string | ✅ | Max 50 chars |
| model | string | ✅ | Max 50 chars |
| year | int | ✅ | 1900-current year |
| vin | string | ✅ | Exactly 17 chars, valid VIN format |
| licensePlate | string | ✅ | Vietnamese license plate format |
| color | string | ✅ | Max 30 chars |
| batteryCapacity | decimal | ✅ | > 0 |
| range | int | ✅ | > 0 (km) |
| purchaseDate | string | ✅ | ISO 8601 date |
| purchasePrice | decimal | ✅ | > 0 (VND) |
| warrantyExpiryDate | string | ❌ | ISO 8601 date |
| latitude | decimal | ❌ | -90 to 90 |
| longitude | decimal | ❌ | -180 to 180 |

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "VEHICLE_CREATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 15,
    "name": "Tesla Model 3 2024",
    "brand": "Tesla",
    "model": "Model 3",
    "vin": "1HGCM82633A123456",
    "licensePlate": "51A-12345",
    "status": "Available",
    "verificationStatus": "Pending",
    "createdAt": "2025-01-17T10:30:00Z",
    "createdBy": {
      "userId": 5,
      "fullName": "Nguyen Van A"
    }
  }
}
```

**Validation Errors:**
- `VEHICLE_NAME_REQUIRED` - Tên xe là bắt buộc
- `BRAND_REQUIRED` - Thương hiệu là bắt buộc
- `VIN_REQUIRED` - Số VIN là bắt buộc
- `VIN_INVALID_FORMAT` - Định dạng VIN không hợp lệ
- `LICENSE_PLATE_REQUIRED` - Biển số xe là bắt buộc
- `LICENSE_PLATE_INVALID_FORMAT` - Định dạng biển số không hợp lệ
- `USER_NOT_ELIGIBLE_TO_CREATE_VEHICLE` - Người dùng không đủ điều kiện
- `NO_DRIVING_LICENSE_REGISTERED` - Chưa đăng ký bằng lái xe
- `DRIVING_LICENSE_NOT_VERIFIED` - Bằng lái xe chưa được xác minh

**Business Rules:**
- User phải có role Co-owner
- User phải có bằng lái xe đã verified và chưa hết hạn
- VIN và biển số xe phải unique
- Định dạng biển số Vietnam: 30A-123.45 hoặc 30A-12345

---

### 2. 👥 Thêm đồng sở hữu - POST `/{vehicleId}/co-owners`

**Mô tả:** Mời người dùng khác trở thành đồng sở hữu của xe.

**Role:** Co-owner, Staff, Admin

**Request Body:**
```json
{
  "userId": 8,
  "ownershipPercentage": 25.0,
  "investmentAmount": 375000000
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| userId | int | ✅ | Must exist, must be Co-owner role |
| ownershipPercentage | decimal | ✅ | 0.1 - 99.9, not exceed available |
| investmentAmount | decimal | ✅ | > 0, should match percentage |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CO_OWNER_INVITATION_SENT_SUCCESSFULLY",
  "data": {
    "invitationId": 25,
    "vehicleId": 15,
    "vehicleName": "Tesla Model 3 2024",
    "targetUserId": 8,
    "targetUserName": "Tran Thi B",
    "ownershipPercentage": 25.0,
    "investmentAmount": 375000000,
    "status": "Pending",
    "createdAt": "2025-01-17T11:00:00Z"
  }
}
```

**Error Responses:**
- `TARGET_USER_NOT_CO_OWNER` - User được mời không có role Co-owner
- `USER_ALREADY_CO_OWNER_OF_VEHICLE` - User đã là đồng sở hữu
- `OWNERSHIP_PERCENTAGE_EXCEEDS_LIMIT` - Vượt quá tỷ lệ còn lại
- `INVITATION_ALREADY_PENDING` - Đã có lời mời chờ duyệt

---

### 3. ✅ Phản hồi lời mời - PUT `/{vehicleId}/invitations/respond`

**Mô tả:** Chấp nhận hoặc từ chối lời mời đồng sở hữu.

**Role:** Co-owner

**Request Body:**
```json
{
  "response": true
}
```

**Request Schema:**
| Field | Type | Required | Values |
|-------|------|----------|--------|
| response | boolean | ✅ | true = accept, false = reject |

**Response 200 - Chấp nhận:**
```json
{
  "statusCode": 200,
  "message": "INVITATION_ACCEPTED_SUCCESSFULLY",
  "data": {
    "vehicleId": 15,
    "vehicleName": "Tesla Model 3 2024",
    "ownershipPercentage": 25.0,
    "investmentAmount": 375000000,
    "status": "Active",
    "acceptedAt": "2025-01-17T12:00:00Z"
  }
}
```

**Response 200 - Từ chối:**
```json
{
  "statusCode": 200,
  "message": "INVITATION_REJECTED_SUCCESSFULLY",
  "data": {
    "vehicleId": 15,
    "status": "Rejected",
    "rejectedAt": "2025-01-17T12:00:00Z"
  }
}
```

---

### 4. 🏠 Xem xe của tôi - GET `/my-vehicles`

**Mô tả:** Lấy danh sách tất cả xe mà user hiện tại sở hữu hoặc đồng sở hữu.

**Role:** Co-owner

**Query Parameters:** Không có

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "USER_VEHICLES_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "vehicleId": 15,
      "name": "Tesla Model 3 2024",
      "brand": "Tesla",
      "model": "Model 3",
      "licensePlate": "51A-12345",
      "status": "Available",
      "verificationStatus": "Verified",
      "myOwnershipPercentage": 75.0,
      "myInvestmentAmount": 1125000000,
      "myRole": "Creator",
      "totalCoOwners": 2,
      "availableOwnershipPercentage": 0.0,
      "currentBalance": 8500000,
      "lastActivityAt": "2025-01-16T14:30:00Z"
    }
  ]
}
```

**Use Cases:**
- Dashboard "Xe của tôi"
- Portfolio quản lý đầu tư
- Kiểm tra tình trạng sở hữu

---

### 5. 📨 Xem lời mời chờ duyệt - GET `/invitations/pending`

**Mô tả:** Lấy danh sách lời mời đồng sở hữu chờ phản hồi.

**Role:** Co-owner

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "PENDING_INVITATIONS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "invitationId": 25,
      "vehicleId": 15,
      "vehicleName": "Tesla Model 3 2024",
      "vehicleBrand": "Tesla",
      "vehicleModel": "Model 3",
      "licensePlate": "51A-12345",
      "inviterName": "Nguyen Van A",
      "ownershipPercentage": 25.0,
      "investmentAmount": 375000000,
      "createdAt": "2025-01-17T11:00:00Z",
      "expiresAt": "2025-01-24T11:00:00Z"
    }
  ]
}
```

---

### 6. 🗑️ Xóa đồng sở hữu - DELETE `/{vehicleId}/co-owners/{coOwnerUserId}`

**Mô tả:** Xóa một đồng sở hữu khỏi xe (chỉ creator mới được xóa).

**Role:** Co-owner, Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |
| coOwnerUserId | int | ✅ | ID của co-owner cần xóa |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CO_OWNER_REMOVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 15,
    "removedUserId": 8,
    "removedUserName": "Tran Thi B",
    "ownershipPercentageFreed": 25.0,
    "newAvailablePercentage": 25.0
  }
}
```

**Business Rules:**
- Chỉ creator của xe mới có thể xóa co-owner
- Không thể xóa co-owner cuối cùng (phải có ít nhất 1 active owner)
- Xóa sẽ giải phóng ownership percentage

---

### 7. 🔄 Cập nhật thông tin xe - PUT `/{vehicleId}`

**Mô tả:** Cập nhật thông tin xe (chỉ co-owner active mới được cập nhật).

**Role:** Co-owner, Staff, Admin

**Request Body:** Giống như tạo xe nhưng các field đều optional
```json
{
  "name": "Tesla Model 3 2024 Updated",
  "color": "Midnight Silver",
  "latitude": 10.762622,
  "longitude": 106.660172
}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_UPDATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 15,
    "name": "Tesla Model 3 2024 Updated",
    "color": "Midnight Silver",
    "updatedAt": "2025-01-17T13:00:00Z"
  }
}
```

**Lưu ý:** VIN và license plate không thể thay đổi vì là định danh duy nhất.

---

### 8. 🔍 Xem xe khả dụng (với filter) - GET `/available`

**Mô tả:** Lấy danh sách xe khả dụng với filtering và pagination nâng cao.

**Role-based Access:**
- **Co-owner**: Chỉ xem xe trong groups mình tham gia
- **Staff/Admin**: Xem tất cả xe trong hệ thống

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | int | ❌ | 1 | Số trang |
| pageSize | int | ❌ | 10 | Items per page (max: 50) |
| status | string | ❌ | Available | Available, InUse, Maintenance, Unavailable |
| verificationStatus | string | ❌ | Verified | Pending, Verified, Rejected, etc. |
| brand | string | ❌ | null | Filter theo thương hiệu (partial match) |
| model | string | ❌ | null | Filter theo model (partial match) |
| minYear | int | ❌ | null | Năm sản xuất tối thiểu |
| maxYear | int | ❌ | null | Năm sản xuất tối đa |
| minPrice | decimal | ❌ | null | Giá mua tối thiểu (VND) |
| maxPrice | decimal | ❌ | null | Giá mua tối đa (VND) |
| search | string | ❌ | null | Tìm kiếm tổng hợp (name, brand, model, VIN, plate) |
| sortBy | string | ❌ | createdAt | name, brand, model, year, price, createdAt |
| sortDesc | boolean | ❌ | true | true = descending, false = ascending |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "AVAILABLE_VEHICLES_RETRIEVED_SUCCESSFULLY",
  "data": {
    "items": [
      {
        "vehicleId": 15,
        "name": "Tesla Model 3 2024",
        "brand": "Tesla",
        "model": "Model 3",
        "year": 2024,
        "licensePlate": "51A-12345",
        "color": "Pearl White",
        "range": 448,
        "status": "Available",
        "verificationStatus": "Verified",
        "purchasePrice": 1500000000,
        "availableOwnershipPercentage": 25.0,
        "totalCoOwners": 2,
        "currentUtilizationRate": 24.5,
        "location": {
          "latitude": 10.762622,
          "longitude": 106.660172
        },
        "coOwners": [
          {
            "userId": 5,
            "fullName": "Nguyen Van A",
            "ownershipPercentage": 75.0,
            "isCreator": true
          }
        ]
      }
    ],
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Example Requests:**

**1. Tìm xe Tesla:**
```
GET /api/vehicle/available?brand=Tesla&sortBy=price&sortDesc=false
```

**2. Xe trong khoảng giá 1-2 tỷ:**
```
GET /api/vehicle/available?minPrice=1000000000&maxPrice=2000000000
```

**3. Xe sản xuất từ 2022:**
```
GET /api/vehicle/available?minYear=2022&sortBy=year&sortDesc=true
```

**4. Tìm kiếm "VF8":**
```
GET /api/vehicle/available?search=VF8
```

---

### 9. 📄 Xem chi tiết xe đầy đủ - GET `/{vehicleId}`

**Mô tả:** Lấy thông tin chi tiết đầy đủ của xe bao gồm fund, co-owners, specifications.

**Role:** Co-owner (chỉ xe mình tham gia), Staff/Admin (mọi xe)

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_DETAIL_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicle": {
      "vehicleId": 15,
      "name": "Tesla Model 3 2024",
      "brand": "Tesla",
      "model": "Model 3",
      "year": 2024,
      "vin": "1HGCM82633A123456",
      "licensePlate": "51A-12345",
      "color": "Pearl White",
      "batteryCapacity": 75.0,
      "range": 448,
      "distanceTravelled": 1250,
      "status": "Available",
      "verificationStatus": "Verified",
      "purchaseDate": "2024-01-15T00:00:00Z",
      "purchasePrice": 1500000000,
      "warrantyExpiryDate": "2027-01-15T00:00:00Z",
      "location": {
        "latitude": 10.762622,
        "longitude": 106.660172
      },
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2025-01-17T13:00:00Z"
    },
    "coOwners": [
      {
        "userId": 5,
        "fullName": "Nguyen Van A",
        "email": "nguyenvana@example.com",
        "phone": "0901234567",
        "ownershipPercentage": 75.0,
        "investmentAmount": 1125000000,
        "status": "Active",
        "isCreator": true,
        "joinedAt": "2024-01-15T10:30:00Z"
      },
      {
        "userId": 8,
        "fullName": "Tran Thi B",
        "email": "tranthib@example.com",
        "phone": "0901234568",
        "ownershipPercentage": 25.0,
        "investmentAmount": 375000000,
        "status": "Active",
        "isCreator": false,
        "joinedAt": "2025-01-17T12:00:00Z"
      }
    ],
    "totalOwnership": 100.0,
    "availableOwnership": 0.0,
    "fund": {
      "fundId": 8,
      "currentBalance": 8500000,
      "totalAddedAmount": 15000000,
      "totalUsedAmount": 6500000,
      "totalAdditions": 12,
      "totalUsages": 8,
      "balanceStatus": "Healthy",
      "recommendedMinBalance": 5000000,
      "createdAt": "2024-01-20T00:00:00Z",
      "updatedAt": "2025-01-16T14:25:00Z"
    },
    "creator": {
      "userId": 5,
      "fullName": "Nguyen Van A",
      "email": "nguyenvana@example.com"
    }
  }
}
```

---

### 10. 📅 Xem lịch trình sử dụng xe - GET `/{vehicleId}/availability/schedule`

**Mô tả:** Xem lịch trình sử dụng xe trong khoảng thời gian cụ thể, biết khi nào xe bận/rảnh.

**Role:** Co-owner (xe mình tham gia), Staff/Admin

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | ✅ | Ngày bắt đầu (yyyy-MM-dd) |
| endDate | DateTime | ✅ | Ngày kết thúc (yyyy-MM-dd, max 90 days) |
| statusFilter | string | ❌ | Filter booking status (Confirmed, Pending, etc.) |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_AVAILABILITY_SCHEDULE_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicle": {
      "vehicleId": 15,
      "name": "Tesla Model 3 2024",
      "licensePlate": "51A-12345",
      "status": "Available"
    },
    "period": {
      "startDate": "2025-01-17T00:00:00Z",
      "endDate": "2025-01-24T23:59:59Z",
      "totalDays": 8
    },
    "bookedSlots": [
      {
        "bookingId": 125,
        "coOwnerName": "Nguyen Van A",
        "startTime": "2025-01-18T08:00:00Z",
        "endTime": "2025-01-18T18:00:00Z",
        "duration": 10.0,
        "purpose": "Đi công tác Đà Nẵng",
        "status": "Confirmed"
      },
      {
        "bookingId": 126,
        "coOwnerName": "Tran Thi B",
        "startTime": "2025-01-20T14:00:00Z",
        "endTime": "2025-01-20T20:00:00Z",
        "duration": 6.0,
        "purpose": "Dự tiệc cưới",
        "status": "Confirmed"
      }
    ],
    "availableDays": [
      "2025-01-17",
      "2025-01-19", 
      "2025-01-21",
      "2025-01-22",
      "2025-01-23",
      "2025-01-24"
    ],
    "utilization": {
      "totalHoursInPeriod": 192,
      "bookedHours": 16.0,
      "utilizationPercentage": 8.33,
      "totalBookings": 2,
      "confirmedBookings": 2,
      "averageBookingDuration": 8.0
    }
  }
}
```

**Use Cases:**
- Lập kế hoạch đặt xe
- Xem ai đang sử dụng xe khi nào
- Phân tích mức độ sử dụng xe

---

### 11. 🔍 Tìm slot trống - GET `/{vehicleId}/availability/find-slots`

**Mô tả:** Tự động tìm các khung thời gian xe rảnh để đặt.

**Role:** Co-owner (xe mình tham gia), Staff/Admin

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| startDate | DateTime | ✅ | - | Ngày bắt đầu tìm |
| endDate | DateTime | ✅ | - | Ngày kết thúc tìm |
| minimumDurationHours | int | ❌ | 1 | Thời gian tối thiểu cần (max: 24) |
| fullDayOnly | boolean | ❌ | false | Chỉ tìm slot ≥8 tiếng |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "AVAILABLE_TIME_SLOTS_FOUND_SUCCESSFULLY",
  "data": {
    "vehicle": {
      "vehicleId": 15,
      "name": "Tesla Model 3 2024",
      "licensePlate": "51A-12345"
    },
    "searchCriteria": {
      "startDate": "2025-01-17T00:00:00Z",
      "endDate": "2025-01-24T23:59:59Z",
      "minimumDurationHours": 4,
      "fullDayOnly": false
    },
    "availableSlots": [
      {
        "startTime": "2025-01-17T00:00:00Z",
        "endTime": "2025-01-17T23:59:59Z",
        "durationHours": 24.0,
        "isFullDay": true,
        "recommendation": "Cả ngày 17/1 đều rảnh"
      },
      {
        "startTime": "2025-01-18T18:00:00Z",
        "endTime": "2025-01-18T23:59:59Z",
        "durationHours": 6.0,
        "isFullDay": false,
        "recommendation": "6 tiếng tối 18/1 sau booking"
      },
      {
        "startTime": "2025-01-19T00:00:00Z",
        "endTime": "2025-01-19T23:59:59Z",
        "durationHours": 24.0,
        "isFullDay": true,
        "recommendation": "Cả ngày 19/1 đều rảnh"
      }
    ],
    "totalSlotsFound": 3,
    "message": "Tìm thấy 3 khung thời gian phù hợp"
  }
}
```

**Use Cases:**
- "Tôi cần xe 4 tiếng tuần tới"
- "Tìm ngày nào xe rảnh cả ngày"
- "Khi nào có thể đặt xe cho chuyến đi ngắn"

---

### 12. 📊 So sánh hiệu suất sử dụng - GET `/utilization/compare`

**Mô tả:** So sánh mức độ sử dụng của các xe trong group/toàn hệ thống.

**Role-based Access:**
- **Co-owner**: So sánh xe trong groups mình tham gia
- **Staff/Admin**: So sánh tất cả xe

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | ✅ | Ngày bắt đầu phân tích |
| endDate | DateTime | ✅ | Ngày kết thúc phân tích |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_UTILIZATION_COMPARISON_RETRIEVED_SUCCESSFULLY",
  "data": {
    "period": {
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2025-01-31T23:59:59Z",
      "totalDays": 31
    },
    "vehicles": [
      {
        "vehicleId": 15,
        "vehicleName": "Tesla Model 3 2024",
        "licensePlate": "51A-12345",
        "utilizationPercentage": 65.2,
        "totalBookings": 18,
        "totalBookedHours": 486.5,
        "totalAvailableHours": 744,
        "averageBookingDuration": 27.0,
        "mostActiveDay": "Saturday",
        "rank": 1
      },
      {
        "vehicleId": 12,
        "vehicleName": "VinFast VF8 2023",
        "licensePlate": "51B-67890",
        "utilizationPercentage": 45.3,
        "totalBookings": 14,
        "totalBookedHours": 337.1,
        "totalAvailableHours": 744,
        "averageBookingDuration": 24.1,
        "mostActiveDay": "Sunday",
        "rank": 2
      },
      {
        "vehicleId": 8,
        "vehicleName": "BMW i4 2022",
        "licensePlate": "50A-11111",
        "utilizationPercentage": 20.1,
        "totalBookings": 8,
        "totalBookedHours": 149.5,
        "totalAvailableHours": 744,
        "averageBookingDuration": 18.7,
        "mostActiveDay": "Friday",
        "rank": 3
      }
    ],
    "summary": {
      "totalVehicles": 3,
      "highestUtilization": {
        "vehicleId": 15,
        "vehicleName": "Tesla Model 3 2024",
        "utilizationPercentage": 65.2
      },
      "lowestUtilization": {
        "vehicleId": 8,
        "vehicleName": "BMW i4 2022",
        "utilizationPercentage": 20.1
      },
      "averageUtilization": 43.5,
      "totalBookings": 40,
      "totalBookedHours": 973.1
    },
    "insights": [
      "Tesla Model 3 là xe được sử dụng nhiều nhất (65.2%)",
      "BMW i4 ít được sử dụng (20.1%) - có thể cân nhắc giảm ownership",
      "Trung bình fleet: 43.5% utilization"
    ]
  }
}
```

**Use Cases:**
- **Fleet Management**: Xe nào hot, xe nào ế
- **Investment Decision**: Nên tăng hay giảm ownership
- **Booking Strategy**: Chọn xe ít bận để dễ đặt

---

### 13. 🧪 [Development] Kiểm tra điều kiện tạo xe - GET `/validate-creation-eligibility`

**Mô tả:** Kiểm tra user hiện tại có đủ điều kiện tạo xe không (endpoint test).

**Response 200 - Đủ điều kiện:**
```json
{
  "statusCode": 200,
  "message": "USER_ELIGIBLE_TO_CREATE_VEHICLE",
  "data": {
    "userId": 5,
    "isEligible": true,
    "userRole": "CoOwner",
    "hasVerifiedLicense": true,
    "licenseExpiryDate": "2028-05-15T00:00:00Z",
    "checks": {
      "hasCoOwnerRole": true,
      "hasActiveLicense": true,
      "licenseVerified": true,
      "licenseNotExpired": true
    }
  }
}
```

**Response 400 - Không đủ điều kiện:**
```json
{
  "statusCode": 400,
  "message": "USER_NOT_ELIGIBLE_TO_CREATE_VEHICLE",
  "data": {
    "userId": 5,
    "isEligible": false,
    "reasons": [
      "NO_DRIVING_LICENSE_REGISTERED",
      "USER_ROLE_NOT_CO_OWNER"
    ]
  }
}
```

---

## 🔢 Enums và Constants

### Vehicle Status (EVehicleStatus)
```typescript
enum EVehicleStatus {
  Available = 0,     // Xe sẵn sàng sử dụng
  InUse = 1,        // Đang được sử dụng (có booking active)
  Maintenance = 2,   // Đang bảo dưỡng
  Unavailable = 3    // Không khả dụng vì lý do khác
}
```

### Verification Status (EVerificationStatus)
```typescript
enum EVerificationStatus {
  Pending = 0,              // Chờ xác minh
  VerificationRequested = 1, // Đã yêu cầu xác minh
  RequiresRecheck = 2,      // Cần kiểm tra lại
  Verified = 3,             // Đã xác minh thành công
  Rejected = 4              // Xác minh bị từ chối
}
```

### Co-Owner Status (ECoOwnerStatus)
```typescript
enum ECoOwnerStatus {
  Active = 0,    // Đồng sở hữu đang hoạt động
  Pending = 1,   // Chờ phản hồi lời mời
  Rejected = 2,  // Đã từ chối lời mời
  Inactive = 3   // Không hoạt động (bị xóa)
}
```

---

## ❌ Error Codes

### Vehicle Creation Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 400 | Bad Request | `USER_NOT_ELIGIBLE_TO_CREATE_VEHICLE` | User không đủ điều kiện tạo xe |
| 400 | Bad Request | `NO_DRIVING_LICENSE_REGISTERED` | Chưa đăng ký bằng lái xe |
| 400 | Bad Request | `DRIVING_LICENSE_NOT_VERIFIED` | Bằng lái xe chưa được xác minh |
| 400 | Bad Request | `DRIVING_LICENSE_EXPIRED` | Bằng lái xe đã hết hạn |
| 409 | Conflict | `LICENSE_PLATE_ALREADY_EXISTS` | Biển số xe đã tồn tại |
| 409 | Conflict | `VIN_ALREADY_EXISTS` | Số VIN đã tồn tại |

### Co-ownership Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 400 | Bad Request | `TARGET_USER_NOT_CO_OWNER` | User được mời không có role Co-owner |
| 400 | Bad Request | `OWNERSHIP_PERCENTAGE_EXCEEDS_LIMIT` | Vượt quá tỷ lệ sở hữu còn lại |
| 409 | Conflict | `USER_ALREADY_CO_OWNER_OF_VEHICLE` | User đã là đồng sở hữu của xe |
| 409 | Conflict | `INVITATION_ALREADY_PENDING` | Đã có lời mời chờ duyệt |
| 404 | Not Found | `INVITATION_NOT_FOUND` | Không tìm thấy lời mời |

### Access Control Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 403 | Forbidden | `ACCESS_DENIED_NOT_VEHICLE_CO_OWNER` | Không phải đồng sở hữu của xe |
| 403 | Forbidden | `ACCESS_DENIED_ONLY_CREATOR_CAN_REMOVE` | Chỉ creator mới có thể xóa co-owner |
| 403 | Forbidden | `ACCESS_DENIED_INSUFFICIENT_PERMISSIONS` | Không đủ quyền thực hiện |

### System Errors (5xx)
| Status | Code | Ý nghĩa |
|--------|------|---------|
| 500 | Internal Server Error | `INTERNAL_SERVER_ERROR` | Lỗi hệ thống |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Flow tạo xe và mời đồng sở hữu

```javascript
// 1. Kiểm tra điều kiện tạo xe
const eligibilityResponse = await fetch('/api/vehicle/validate-creation-eligibility', {
  headers: { 'Authorization': `Bearer ${token}` }
});

if (eligibilityResponse.ok) {
  // 2. Tạo xe mới
  const createResponse = await fetch('/api/vehicle', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      name: 'Tesla Model 3 2024',
      brand: 'Tesla',
      model: 'Model 3',
      year: 2024,
      vin: '1HGCM82633A123456',
      licensePlate: '51A-12345',
      color: 'Pearl White',
      batteryCapacity: 75.0,
      range: 448,
      purchaseDate: '2024-01-15',
      purchasePrice: 1500000000
    })
  });

  const vehicleData = await createResponse.json();
  const vehicleId = vehicleData.data.vehicleId;

  // 3. Mời đồng sở hữu
  const inviteResponse = await fetch(`/api/vehicle/${vehicleId}/co-owners`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      userId: 8,
      ownershipPercentage: 25.0,
      investmentAmount: 375000000
    })
  });

  console.log('Invitation sent!');
}
```

### Use Case 2: Xem và phản hồi lời mời

```javascript
// 1. Xem lời mời chờ duyệt
const invitationsResponse = await fetch('/api/vehicle/invitations/pending', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const invitations = await invitationsResponse.json();

// 2. Phản hồi lời mời (chấp nhận)
for (const invitation of invitations.data) {
  const respondResponse = await fetch(`/api/vehicle/${invitation.vehicleId}/invitations/respond`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      response: true  // true = accept, false = reject
    })
  });

  if (respondResponse.ok) {
    console.log(`Accepted invitation for ${invitation.vehicleName}`);
  }
}
```

### Use Case 3: Tìm xe và lên kế hoạch booking

```javascript
// 1. Tìm xe Tesla trong khoảng giá 1-2 tỷ
const searchResponse = await fetch('/api/vehicle/available?brand=Tesla&minPrice=1000000000&maxPrice=2000000000&sortBy=price', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const vehicles = await searchResponse.json();
const selectedVehicle = vehicles.data.items[0];

// 2. Xem lịch trình xe tuần tới
const scheduleResponse = await fetch(`/api/vehicle/${selectedVehicle.vehicleId}/availability/schedule?startDate=2025-01-17&endDate=2025-01-24`, {
  headers: { 'Authorization': `Bearer ${token}` }
});

const schedule = await scheduleResponse.json();
console.log('Available days:', schedule.data.availableDays);

// 3. Tìm slot 4 tiếng để đặt xe
const slotsResponse = await fetch(`/api/vehicle/${selectedVehicle.vehicleId}/availability/find-slots?startDate=2025-01-17&endDate=2025-01-24&minimumDurationHours=4`, {
  headers: { 'Authorization': `Bearer ${token}` }
});

const slots = await slotsResponse.json();
console.log('Available 4-hour slots:', slots.data.availableSlots);
```

### Use Case 4: Quản lý portfolio xe

```javascript
// 1. Xem danh sách xe của tôi
const myVehiclesResponse = await fetch('/api/vehicle/my-vehicles', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const myVehicles = await myVehiclesResponse.json();

// 2. Xem chi tiết từng xe
for (const vehicle of myVehicles.data) {
  const detailResponse = await fetch(`/api/vehicle/${vehicle.vehicleId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const detail = await detailResponse.json();
  
  console.log(`${vehicle.name}:`);
  console.log(`- My ownership: ${vehicle.myOwnershipPercentage}%`);
  console.log(`- Investment: ${vehicle.myInvestmentAmount.toLocaleString()} VND`);
  console.log(`- Fund balance: ${detail.data.fund.currentBalance.toLocaleString()} VND`);
  console.log(`- Co-owners: ${detail.data.coOwners.length}`);
}

// 3. So sánh hiệu suất sử dụng các xe
const utilizationResponse = await fetch('/api/vehicle/utilization/compare?startDate=2025-01-01&endDate=2025-01-31', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const utilization = await utilizationResponse.json();
console.log('Utilization ranking:');
utilization.data.vehicles.forEach(v => {
  console.log(`${v.rank}. ${v.vehicleName}: ${v.utilizationPercentage}%`);
});
```

---

## 🔐 Best Practices

### 1. Role-based UI

```javascript
// Hiển thị chức năng theo role
const userRole = getUserRole(); // Co-owner, Staff, Admin

if (userRole === 'Co-owner') {
  // Chỉ hiển thị xe trong groups mình tham gia
  showMyGroupVehicles();
  showCreateVehicleButton();
  showInvitationManagement();
} else if (userRole === 'Staff' || userRole === 'Admin') {
  // Hiển thị tất cả xe
  showAllVehicles();
  showAdvancedManagement();
}
```

### 2. Optimistic UI cho invitations

```javascript
// Hiển thị ngay khi gửi lời mời, update khi có response
async function inviteCoOwner(vehicleId, userId, percentage, amount) {
  // 1. Update UI optimistic
  showPendingInvitation(userId, percentage);

  try {
    // 2. Gửi request
    const response = await fetch(`/api/vehicle/${vehicleId}/co-owners`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ userId, ownershipPercentage: percentage, investmentAmount: amount })
    });

    if (response.ok) {
      showSuccessMessage('Lời mời đã được gửi');
    } else {
      // 3. Rollback nếu lỗi
      hidePendingInvitation(userId);
      showErrorMessage('Không thể gửi lời mời');
    }
  } catch (error) {
    hidePendingInvitation(userId);
    showErrorMessage('Lỗi kết nối');
  }
}
```

### 3. Smart filtering và caching

```javascript
// Cache kết quả search để tránh gọi API liên tục
const searchCache = new Map();

async function searchVehicles(filters) {
  const cacheKey = JSON.stringify(filters);
  
  if (searchCache.has(cacheKey)) {
    return searchCache.get(cacheKey);
  }

  const queryString = new URLSearchParams(filters).toString();
  const response = await fetch(`/api/vehicle/available?${queryString}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await response.json();
  
  // Cache trong 5 phút
  searchCache.set(cacheKey, data);
  setTimeout(() => searchCache.delete(cacheKey), 5 * 60 * 1000);

  return data;
}
```

### 4. Real-time availability updates

```javascript
// WebSocket hoặc polling để update availability real-time
function setupAvailabilityUpdates(vehicleId) {
  setInterval(async () => {
    const response = await fetch(`/api/vehicle/${vehicleId}/availability/schedule?startDate=${today}&endDate=${nextWeek}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    const data = await response.json();
    updateCalendarView(data.data.bookedSlots);
  }, 30000); // Update every 30 seconds
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
````