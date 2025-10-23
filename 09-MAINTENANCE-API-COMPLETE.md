# Maintenance API Documentation

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

Module Maintenance API cung cấp hệ thống quản lý bảo dưỡng toàn diện cho EV Co-ownership:

### 🔧 Core Features
- **CRUD Operations**: Tạo, xem, sửa, xóa maintenance records
- **Vehicle History**: Lịch sử bảo dưỡng đầy đủ cho từng xe
- **Cost Management**: Theo dõi chi phí bảo dưỡng và thanh toán
- **Statistics & Analytics**: Thống kê chi phí, frequency, trends

### 📊 Business Intelligence
- **Vehicle Performance**: Tracking maintenance frequency per vehicle
- **Cost Analysis**: Phân tích chi phí bảo dưỡng theo loại và thời gian
- **Predictive Insights**: Dự đoán maintenance schedule
- **ROI Tracking**: Theo dõi return on investment

### 🔐 Role-based Access
- **Co-owner**: Xem maintenance của xe mình tham gia, tạo maintenance request
- **Staff**: Quản lý maintenance, update status, mark as paid
- **Admin**: Full access, xóa records, xem statistics tổng quan

---

## 🔗 Base URL

```
http://localhost:5215/api/maintenance
```

Trong production: `https://your-domain.com/api/maintenance`

---

## 🔐 Authentication

Tất cả endpoints yêu cầu JWT Bearer Token:

```http
Authorization: Bearer {access_token}
```

**Role Requirements:**
- **Co-owner**: Xem maintenance của xe mình tham gia
- **Staff**: Quản lý maintenance records
- **Admin**: Full access to all operations

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 1 | POST | `/` | Tạo maintenance record mới | Admin, Staff, Co-owner |
| 2 | GET | `/{id}` | Xem maintenance theo ID | All |
| 3 | GET | `/vehicle/{vehicleId}` | Xem maintenance của xe | All |
| 4 | GET | `/vehicle/{vehicleId}/history` | Lịch sử maintenance đầy đủ | All |
| 5 | GET | `/` | Xem tất cả maintenance | Staff, Admin |
| 6 | PUT | `/{id}` | Cập nhật maintenance | Staff, Admin |
| 7 | POST | `/{id}/mark-paid` | Đánh dấu đã thanh toán | Staff, Admin |
| 8 | DELETE | `/{id}` | Xóa maintenance record | Admin |
| 9 | GET | `/statistics` | Thống kê tổng quan | Staff, Admin |
| 10 | GET | `/vehicle/{vehicleId}/statistics` | Thống kê theo xe | All |

---

## 📖 Chi tiết từng API

### 1. ➕ Tạo maintenance record - POST `/`

**Mô tả:** Tạo maintenance record mới với auto cost tracking và fund integration.

**Role:** Admin, Staff, Co-owner

**Request Body:**
```json
{
  "vehicleId": 1,
  "maintenanceType": 0,
  "description": "Regular oil change and filter replacement",
  "cost": 1200000,
  "serviceProvider": "Tesla Service Center District 1",
  "maintenanceDate": "2025-01-25T10:00:00Z",
  "nextMaintenanceDate": "2025-04-25T10:00:00Z",
  "odometer": 15000,
  "severity": 0,
  "isEmergency": false,
  "receiptImageUrl": "https://storage.example.com/receipts/maintenance123.jpg",
  "notes": "Routine maintenance - engine running smoothly",
  "bookingId": 45
}
```

**Request Schema:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |
| maintenanceType | int | ✅ | Loại maintenance (0-5) |
| description | string | ✅ | Mô tả công việc maintenance |
| cost | decimal | ✅ | Chi phí maintenance |
| serviceProvider | string | ❌ | Nhà cung cấp dịch vụ |
| maintenanceDate | DateTime | ✅ | Ngày thực hiện |
| nextMaintenanceDate | DateTime | ❌ | Ngày maintenance tiếp theo |
| odometer | int | ❌ | Số km hiện tại |
| severity | int | ❌ | Mức độ nghiêm trọng (0-2) |
| isEmergency | bool | ❌ | Có phải emergency không |
| receiptImageUrl | string | ❌ | URL ảnh hóa đơn |
| notes | string | ❌ | Ghi chú thêm |
| bookingId | int | ❌ | Liên kết với booking |

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "MAINTENANCE_CREATED_SUCCESSFULLY",
  "data": {
    "maintenanceId": 123,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "maintenanceType": "RoutineMaintenance",
    "description": "Regular oil change and filter replacement",
    "cost": 1200000,
    "serviceProvider": "Tesla Service Center District 1",
    "maintenanceDate": "2025-01-25T10:00:00Z",
    "nextMaintenanceDate": "2025-04-25T10:00:00Z",
    "odometer": 15000,
    "severity": "Low",
    "status": "Completed",
    "isPaid": false,
    "createdAt": "2025-01-17T10:00:00Z",
    "fundIntegration": {
      "fundUsageCreated": true,
      "fundUsageId": 501,
      "remainingBalance": 8500000
    }
  }
}
```

**Business Logic:**
- Tự động tạo fund usage nếu xe có fund
- Validate xe tồn tại và user có quyền access
- Auto-set status dựa trên maintenance date
- Link với booking nếu có

---

### 2. 👁️ Xem maintenance theo ID - GET `/{id}`

**Mô tả:** Lấy thông tin chi tiết một maintenance record.

**Role:** All (role-based filtering)

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_RETRIEVED_SUCCESSFULLY",
  "data": {
    "maintenanceId": 123,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "maintenanceType": "RoutineMaintenance",
    "description": "Regular oil change and filter replacement",
    "cost": 1200000,
    "serviceProvider": "Tesla Service Center District 1",
    "maintenanceDate": "2025-01-25T10:00:00Z",
    "nextMaintenanceDate": "2025-04-25T10:00:00Z",
    "odometer": 15000,
    "severity": "Low",
    "status": "Completed",
    "isPaid": true,
    "paidAt": "2025-01-25T15:30:00Z",
    "isEmergency": false,
    "receiptImageUrl": "https://storage.example.com/receipts/maintenance123.jpg",
    "notes": "Routine maintenance - engine running smoothly",
    "createdAt": "2025-01-17T10:00:00Z",
    "updatedAt": "2025-01-25T15:30:00Z",
    "bookingInfo": {
      "bookingId": 45,
      "coOwnerName": "John Doe",
      "bookingPurpose": "Business trip"
    },
    "fundInfo": {
      "fundUsageId": 501,
      "fundBalance": 8500000
    }
  }
}
```

---

### 3. 🚗 Xem maintenance của xe - GET `/vehicle/{vehicleId}`

**Mô tả:** Lấy danh sách maintenance records của một xe với pagination.

**Role:** All (role-based access)

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| pageIndex | int | 1 | Số trang |
| pageSize | int | 10 | Items per page (max: 50) |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_MAINTENANCES_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "totalMaintenances": 45,
    "totalPages": 5,
    "currentPage": 1,
    "maintenances": [
      {
        "maintenanceId": 123,
        "maintenanceType": "RoutineMaintenance",
        "description": "Regular oil change and filter replacement",
        "cost": 1200000,
        "serviceProvider": "Tesla Service Center District 1",
        "maintenanceDate": "2025-01-25T10:00:00Z",
        "severity": "Low",
        "status": "Completed",
        "isPaid": true,
        "daysSinceCreated": 8
      },
      {
        "maintenanceId": 124,
        "maintenanceType": "EmergencyRepair",
        "description": "Brake pad replacement due to wear",
        "cost": 2500000,
        "serviceProvider": "ABC Auto Service",
        "maintenanceDate": "2025-01-20T14:00:00Z",
        "severity": "Medium",
        "status": "Completed",
        "isPaid": false,
        "daysSinceCreated": 13
      }
    ],
    "summary": {
      "totalCost": 3700000,
      "averageCost": 1850000,
      "lastMaintenanceDate": "2025-01-25T10:00:00Z",
      "nextScheduledDate": "2025-04-25T10:00:00Z",
      "maintenanceFrequency": "Every 3 months"
    }
  }
}
```

---

### 4. 📚 Lịch sử maintenance đầy đủ - GET `/vehicle/{vehicleId}/history`

**Mô tả:** Lấy lịch sử maintenance đầy đủ với timeline và insights.

**Role:** All (role-based access)

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_MAINTENANCE_HISTORY_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "vehicleAge": "2 years 3 months",
    "totalMaintenances": 45,
    "totalCost": 45000000,
    "timeline": [
      {
        "year": 2025,
        "quarter": 1,
        "maintenanceCount": 8,
        "totalCost": 12000000,
        "averageCost": 1500000,
        "maintenanceTypes": {
          "RoutineMaintenance": 5,
          "EmergencyRepair": 2,
          "Upgrade": 1
        }
      },
      {
        "year": 2024,
        "quarter": 4,
        "maintenanceCount": 6,
        "totalCost": 8500000,
        "averageCost": 1416667,
        "maintenanceTypes": {
          "RoutineMaintenance": 4,
          "EmergencyRepair": 1,
          "PreventiveMaintenance": 1
        }
      }
    ],
    "insights": {
      "averageMonthlyCost": 1875000,
      "mostCommonType": "RoutineMaintenance",
      "emergencyRepairRate": 15.6,
      "nextRecommendedMaintenance": {
        "type": "RoutineMaintenance",
        "estimatedDate": "2025-04-25T10:00:00Z",
        "estimatedCost": 1200000,
        "urgency": "Low"
      },
      "costTrends": {
        "isIncreasing": false,
        "trendPercentage": -8.5,
        "explanation": "Maintenance costs decreased 8.5% compared to last quarter"
      }
    },
    "upcomingMaintenances": [
      {
        "type": "RoutineMaintenance",
        "description": "Oil change and tire rotation",
        "estimatedDate": "2025-04-25T10:00:00Z",
        "estimatedCost": 1200000,
        "priority": "Medium"
      },
      {
        "type": "Inspection",
        "description": "Annual safety inspection",
        "estimatedDate": "2025-06-15T10:00:00Z",
        "estimatedCost": 500000,
        "priority": "High"
      }
    ]
  }
}
```

---

### 5. 📋 Xem tất cả maintenance - GET `/`

**Mô tả:** Lấy danh sách tất cả maintenance records trong hệ thống.

**Role:** Staff, Admin

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| pageIndex | int | 1 | Số trang |
| pageSize | int | 10 | Items per page (max: 100) |

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "ALL_MAINTENANCES_RETRIEVED_SUCCESSFULLY",
  "data": {
    "totalMaintenances": 456,
    "totalPages": 46,
    "currentPage": 1,
    "maintenances": [
      {
        "maintenanceId": 123,
        "vehicleId": 1,
        "vehicleName": "Tesla Model 3",
        "licensePlate": "51A-12345",
        "maintenanceType": "RoutineMaintenance",
        "description": "Regular oil change",
        "cost": 1200000,
        "maintenanceDate": "2025-01-25T10:00:00Z",
        "status": "Completed",
        "isPaid": true,
        "serviceProvider": "Tesla Service Center"
      }
    ],
    "summary": {
      "totalCost": 234000000,
      "averageCost": 512500,
      "pendingPayments": 15,
      "pendingPaymentAmount": 25000000,
      "statusBreakdown": {
        "Completed": 425,
        "InProgress": 20,
        "Scheduled": 11
      }
    }
  }
}
```

---

### 6. ✏️ Cập nhật maintenance - PUT `/{id}`

**Mô tả:** Cập nhật thông tin maintenance record.

**Role:** Staff, Admin

**Request Body:**
```json
{
  "description": "Oil change and brake inspection (updated)",
  "cost": 1500000,
  "serviceProvider": "Tesla Service Center District 2",
  "maintenanceDate": "2025-01-25T14:00:00Z",
  "nextMaintenanceDate": "2025-05-25T10:00:00Z",
  "odometer": 15200,
  "severity": 1,
  "notes": "Updated: Found minor brake wear, recommended replacement in 2 months",
  "status": "Completed"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_UPDATED_SUCCESSFULLY",
  "data": {
    "maintenanceId": 123,
    "updatedFields": [
      "description",
      "cost",
      "serviceProvider",
      "maintenanceDate",
      "odometer",
      "severity",
      "notes",
      "status"
    ],
    "oldCost": 1200000,
    "newCost": 1500000,
    "costDifference": 300000,
    "fundAdjustment": {
      "fundUsageId": 501,
      "oldAmount": 1200000,
      "newAmount": 1500000,
      "adjustmentAmount": 300000,
      "newFundBalance": 8200000
    },
    "updatedAt": "2025-01-17T15:30:00Z"
  }
}
```

**Business Logic:**
- Tự động update fund usage nếu cost thay đổi
- Validate các trường được update
- Log changes for audit trail

---

### 7. 💳 Đánh dấu đã thanh toán - POST `/{id}/mark-paid`

**Mô tả:** Đánh dấu maintenance record đã được thanh toán.

**Role:** Staff, Admin

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_MARKED_AS_PAID",
  "data": {
    "maintenanceId": 123,
    "cost": 1200000,
    "isPaid": true,
    "paidAt": "2025-01-17T16:00:00Z",
    "paymentMethod": "Fund",
    "fundInfo": {
      "fundUsageId": 501,
      "remainingBalance": 8200000,
      "balanceStatus": "Healthy"
    }
  }
}
```

---

### 8. 🗑️ Xóa maintenance record - DELETE `/{id}`

**Mô tả:** Xóa maintenance record và revert fund usage.

**Role:** Admin only

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_DELETED_SUCCESSFULLY",
  "data": {
    "deletedId": 123,
    "refundInfo": {
      "fundUsageDeleted": true,
      "refundAmount": 1200000,
      "newFundBalance": 9700000
    },
    "deletedAt": "2025-01-17T16:30:00Z"
  }
}
```

**Business Logic:**
- Xóa fund usage liên quan
- Hoàn tiền vào fund nếu có
- Log deletion for audit

---

### 9. 📊 Thống kê tổng quan - GET `/statistics`

**Mô tả:** Thống kê maintenance toàn hệ thống.

**Role:** Staff, Admin

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_STATISTICS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "overview": {
      "totalMaintenances": 1247,
      "totalCost": 845000000,
      "averageCost": 677500,
      "totalVehicles": 89,
      "averageMaintenancePerVehicle": 14
    },
    "byType": {
      "RoutineMaintenance": {
        "count": 756,
        "totalCost": 456000000,
        "averageCost": 603175,
        "percentage": 60.6
      },
      "EmergencyRepair": {
        "count": 201,
        "totalCost": 234000000,
        "averageCost": 1164179,
        "percentage": 16.1
      },
      "PreventiveMaintenance": {
        "count": 145,
        "totalCost": 89000000,
        "averageCost": 613793,
        "percentage": 11.6
      },
      "Upgrade": {
        "count": 89,
        "totalCost": 45000000,
        "averageCost": 505618,
        "percentage": 7.1
      },
      "Inspection": {
        "count": 56,
        "totalCost": 21000000,
        "averageCost": 375000,
        "percentage": 4.5
      }
    },
    "bySeverity": {
      "Low": 856,
      "Medium": 321,
      "High": 70
    },
    "paymentStatus": {
      "paid": 1156,
      "unpaid": 91,
      "unpaidAmount": 67500000
    },
    "trends": {
      "monthlyGrowth": 5.2,
      "costTrend": "Stable",
      "emergencyRate": 16.1,
      "averageResolutionDays": 2.8
    },
    "topServiceProviders": [
      {
        "name": "Tesla Service Center District 1",
        "maintenanceCount": 234,
        "totalCost": 145000000,
        "averageCost": 619658
      },
      {
        "name": "VinFast Service Network",
        "maintenanceCount": 189,
        "totalCost": 123000000,
        "averageCost": 650794
      }
    ]
  }
}
```

---

### 10. 🚗 Thống kê theo xe - GET `/vehicle/{vehicleId}/statistics`

**Mô tả:** Thống kê maintenance chi tiết cho một xe cụ thể.

**Role:** All (role-based access)

**Response 200:**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_STATISTICS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "vehicleAge": "2 years 3 months",
    "totalMaintenances": 45,
    "totalCost": 32500000,
    "averageCost": 722222,
    "costPerKm": 2167,
    "totalOdometer": 15000,
    "maintenanceFrequency": {
      "averageDaysBetween": 18,
      "maintenancesPerMonth": 1.7,
      "maintenancesPerYear": 20
    },
    "byType": {
      "RoutineMaintenance": {
        "count": 28,
        "totalCost": 18000000,
        "percentage": 62.2
      },
      "EmergencyRepair": {
        "count": 8,
        "totalCost": 9500000,
        "percentage": 17.8
      },
      "PreventiveMaintenance": {
        "count": 6,
        "totalCost": 3500000,
        "percentage": 13.3
      },
      "Upgrade": {
        "count": 2,
        "totalCost": 1200000,
        "percentage": 4.4
      },
      "Inspection": {
        "count": 1,
        "totalCost": 300000,
        "percentage": 2.2
      }
    },
    "costTrends": {
      "last3Months": 8500000,
      "last6Months": 15000000,
      "last12Months": 25000000,
      "monthlyAverage": 2083333,
      "isIncreasing": true,
      "growthRate": 12.5
    },
    "emergencyAnalysis": {
      "emergencyCount": 8,
      "emergencyRate": 17.8,
      "emergencyCost": 9500000,
      "lastEmergencyDate": "2025-01-20T14:00:00Z",
      "averageDaysBetweenEmergencies": 68
    },
    "nextMaintenance": {
      "recommendedDate": "2025-04-25T10:00:00Z",
      "type": "RoutineMaintenance",
      "estimatedCost": 1200000,
      "priority": "Medium",
      "daysUntilDue": 98
    },
    "serviceProviders": [
      {
        "name": "Tesla Service Center District 1",
        "count": 32,
        "totalCost": 23000000,
        "averageCost": 718750,
        "rating": 4.8
      },
      {
        "name": "ABC Auto Service",
        "count": 13,
        "totalCost": 9500000,
        "averageCost": 730769,
        "rating": 4.5
      }
    ],
    "recommendations": [
      "Consider preventive maintenance to reduce emergency repairs",
      "Monitor brake system - frequent repairs detected",
      "Switch to higher-rated service provider for better value"
    ]
  }
}
```

---

## 🔢 Enums và Constants

### Maintenance Type (EMaintenanceType)
```typescript
enum EMaintenanceType {
  RoutineMaintenance = 0,    // Bảo dưỡng định kỳ
  EmergencyRepair = 1,       // Sửa chữa khẩn cấp
  PreventiveMaintenance = 2, // Bảo dưỡng phòng ngừa
  Upgrade = 3,               // Nâng cấp
  Inspection = 4,            // Kiểm tra
  Warranty = 5               // Bảo hành
}
```

### Severity Type (ESeverityType)
```typescript
enum ESeverityType {
  Low = 0,      // Thấp - routine maintenance
  Medium = 1,   // Trung bình - important but not urgent
  High = 2      // Cao - critical/safety related
}
```

### Maintenance Status
```typescript
enum MaintenanceStatus {
  Scheduled = 0,    // Đã lên lịch
  InProgress = 1,   // Đang thực hiện
  Completed = 2,    // Hoàn thành
  Cancelled = 3     // Đã hủy
}
```

---

## ❌ Error Codes

### Access Errors (4xx)
| Status | Message | Ý nghĩa |
|--------|---------|---------|
| 403 | `ACCESS_DENIED_NOT_VEHICLE_CO_OWNER` | Không phải co-owner của xe |
| 404 | `MAINTENANCE_NOT_FOUND` | Không tìm thấy maintenance record |
| 404 | `VEHICLE_NOT_FOUND` | Không tìm thấy xe |

### Business Logic Errors (4xx)
| Status | Message | Ý nghĩa |
|--------|---------|---------|
| 400 | `INVALID_MAINTENANCE_TYPE` | Loại maintenance không hợp lệ |
| 400 | `INVALID_COST_AMOUNT` | Số tiền không hợp lệ |
| 400 | `MAINTENANCE_ALREADY_PAID` | Maintenance đã được thanh toán |
| 400 | `INSUFFICIENT_FUND_BALANCE` | Quỹ không đủ số dư |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Quy trình maintenance hoàn chỉnh

```javascript
// 1. Tạo maintenance record
const createResponse = await fetch('/api/maintenance', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    vehicleId: 1,
    maintenanceType: 0, // RoutineMaintenance
    description: "Regular oil change and filter replacement",
    cost: 1200000,
    serviceProvider: "Tesla Service Center District 1",
    maintenanceDate: "2025-01-25T10:00:00Z",
    nextMaintenanceDate: "2025-04-25T10:00:00Z",
    odometer: 15000,
    severity: 0,
    receiptImageUrl: "https://storage.example.com/receipts/maintenance123.jpg",
    notes: "Routine maintenance - engine running smoothly"
  })
});

const maintenance = await createResponse.json();

if (maintenance.statusCode === 201) {
  console.log(`✅ Maintenance created: ${maintenance.data.maintenanceId}`);
  console.log(`💰 Cost: ${maintenance.data.cost.toLocaleString()} VND`);
  
  if (maintenance.data.fundIntegration.fundUsageCreated) {
    console.log(`💳 Fund usage created: ${maintenance.data.fundIntegration.fundUsageId}`);
    console.log(`💵 Remaining balance: ${maintenance.data.fundIntegration.remainingBalance.toLocaleString()} VND`);
  }

  // 2. Mark as paid
  const paidResponse = await fetch(`/api/maintenance/${maintenance.data.maintenanceId}/mark-paid`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const paid = await paidResponse.json();
  
  if (paid.statusCode === 200) {
    console.log(`✅ Marked as paid at: ${paid.data.paidAt}`);
  }
}
```

### Use Case 2: Theo dõi maintenance vehicle

```javascript
// 1. Xem lịch sử maintenance của xe
const historyResponse = await fetch('/api/maintenance/vehicle/1/history', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const history = await historyResponse.json();

console.log('📊 Vehicle Maintenance History:');
console.log(`Total maintenances: ${history.data.totalMaintenances}`);
console.log(`Total cost: ${history.data.totalCost.toLocaleString()} VND`);
console.log(`Vehicle age: ${history.data.vehicleAge}`);

// 2. Analyze cost trends
const insights = history.data.insights;
console.log('📈 Cost Trends:');
console.log(`Monthly average: ${insights.averageMonthlyCost.toLocaleString()} VND`);
console.log(`Most common type: ${insights.mostCommonType}`);
console.log(`Emergency rate: ${insights.emergencyRepairRate}%`);

if (insights.costTrends.isIncreasing) {
  console.log(`⚠️ Costs increasing by ${insights.costTrends.trendPercentage}%`);
} else {
  console.log(`✅ Costs decreased by ${Math.abs(insights.costTrends.trendPercentage)}%`);
}

// 3. Check upcoming maintenances
console.log('📅 Upcoming Maintenances:');
history.data.upcomingMaintenances.forEach(upcoming => {
  console.log(`${upcoming.type}: ${upcoming.description}`);
  console.log(`Date: ${upcoming.estimatedDate}`);
  console.log(`Est. cost: ${upcoming.estimatedCost.toLocaleString()} VND`);
  console.log(`Priority: ${upcoming.priority}`);
});

// 4. Get detailed statistics
const statsResponse = await fetch('/api/maintenance/vehicle/1/statistics', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const stats = await statsResponse.json();

console.log('📊 Detailed Statistics:');
console.log(`Maintenance frequency: ${stats.data.maintenanceFrequency.maintenancesPerMonth} per month`);
console.log(`Cost per km: ${stats.data.costPerKm.toLocaleString()} VND`);
console.log(`Emergency rate: ${stats.data.emergencyAnalysis.emergencyRate}%`);

// Recommendations
console.log('💡 Recommendations:');
stats.data.recommendations.forEach(rec => {
  console.log(`- ${rec}`);
});
```

### Use Case 3: Admin dashboard với statistics

```javascript
// 1. Get overall statistics
const overallStatsResponse = await fetch('/api/maintenance/statistics', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const overallStats = await overallStatsResponse.json();

console.log('🏢 System-wide Maintenance Statistics:');
console.log(`Total maintenances: ${overallStats.data.overview.totalMaintenances}`);
console.log(`Total cost: ${overallStats.data.overview.totalCost.toLocaleString()} VND`);
console.log(`Average cost: ${overallStats.data.overview.averageCost.toLocaleString()} VND`);

// 2. Analyze by type
console.log('🔧 Breakdown by Type:');
Object.entries(overallStats.data.byType).forEach(([type, data]) => {
  console.log(`${type}: ${data.count} (${data.percentage}%)`);
  console.log(`  Cost: ${data.totalCost.toLocaleString()} VND`);
  console.log(`  Avg: ${data.averageCost.toLocaleString()} VND`);
});

// 3. Payment status monitoring
const payment = overallStats.data.paymentStatus;
console.log('💳 Payment Status:');
console.log(`Paid: ${payment.paid}`);
console.log(`Unpaid: ${payment.unpaid}`);
console.log(`Unpaid amount: ${payment.unpaidAmount.toLocaleString()} VND`);

// 4. Service provider analysis
console.log('🏪 Top Service Providers:');
overallStats.data.topServiceProviders.forEach((provider, index) => {
  console.log(`${index + 1}. ${provider.name}`);
  console.log(`   Count: ${provider.maintenanceCount}`);
  console.log(`   Cost: ${provider.totalCost.toLocaleString()} VND`);
  console.log(`   Avg: ${provider.averageCost.toLocaleString()} VND`);
});

// 5. Get all maintenance records for detailed review
const allMaintenanceResponse = await fetch('/api/maintenance?pageIndex=1&pageSize=50', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const allMaintenance = await allMaintenanceResponse.json();

// Filter unpaid maintenances
const unpaidMaintenances = allMaintenance.data.maintenances.filter(m => !m.isPaid);

console.log(`💸 ${unpaidMaintenances.length} unpaid maintenances found:`);
unpaidMaintenances.forEach(m => {
  console.log(`- ${m.vehicleName} (${m.licensePlate}): ${m.cost.toLocaleString()} VND`);
  console.log(`  Service: ${m.description}`);
  console.log(`  Date: ${m.maintenanceDate}`);
});
```

### Use Case 4: Emergency maintenance workflow

```javascript
// Emergency maintenance scenario
async function handleEmergencyMaintenance(vehicleId, description, cost, serviceProvider) {
  // 1. Create emergency maintenance
  const emergencyResponse = await fetch('/api/maintenance', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      vehicleId: vehicleId,
      maintenanceType: 1, // EmergencyRepair
      description: description,
      cost: cost,
      serviceProvider: serviceProvider,
      maintenanceDate: new Date().toISOString(),
      severity: 2, // High
      isEmergency: true,
      notes: "EMERGENCY: Immediate repair required for safety"
    })
  });

  const emergency = await emergencyResponse.json();

  if (emergency.statusCode === 201) {
    console.log(`🚨 Emergency maintenance created: ${emergency.data.maintenanceId}`);
    
    // 2. Check fund balance impact
    if (emergency.data.fundIntegration) {
      const remainingBalance = emergency.data.fundIntegration.remainingBalance;
      console.log(`💰 Remaining fund balance: ${remainingBalance.toLocaleString()} VND`);
      
      // Alert if balance is low
      if (remainingBalance < 5000000) { // 5M VND threshold
        console.log('⚠️ WARNING: Fund balance is low after emergency maintenance!');
        
        // Get fund recommendations
        await recommendFundActions(vehicleId, remainingBalance);
      }
    }

    // 3. Auto-mark as paid for emergency (assuming immediate payment)
    const paidResponse = await fetch(`/api/maintenance/${emergency.data.maintenanceId}/mark-paid`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (paidResponse.ok) {
      console.log('✅ Emergency maintenance marked as paid');
    }

    // 4. Schedule follow-up inspection
    const followUpDate = new Date();
    followUpDate.setDate(followUpDate.getDate() + 7); // 1 week later

    const inspectionResponse = await fetch('/api/maintenance', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        vehicleId: vehicleId,
        maintenanceType: 4, // Inspection
        description: `Post-emergency inspection following: ${description}`,
        cost: 500000,
        serviceProvider: serviceProvider,
        maintenanceDate: followUpDate.toISOString(),
        severity: 1,
        notes: "Follow-up inspection after emergency repair"
      })
    });

    if (inspectionResponse.ok) {
      console.log('📋 Follow-up inspection scheduled');
    }

    return emergency.data;
  } else {
    console.error('❌ Failed to create emergency maintenance:', emergency.message);
    return null;
  }
}

async function recommendFundActions(vehicleId, currentBalance) {
  // Get fund balance and analysis
  const fundResponse = await fetch(`/api/fund/balance/${vehicleId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const fund = await fundResponse.json();
  
  if (fund.statusCode === 200) {
    const recommendedMin = fund.data.recommendedMinBalance;
    const deficit = recommendedMin - currentBalance;
    
    if (deficit > 0) {
      console.log(`💡 Recommendations:`);
      console.log(`- Add ${deficit.toLocaleString()} VND to reach recommended minimum`);
      console.log(`- Consider increasing monthly contributions`);
      console.log(`- Review emergency fund allocation`);
    }
  }
}

// Usage
handleEmergencyMaintenance(
  1, 
  "Brake system failure - immediate replacement required", 
  3500000, 
  "Emergency Auto Repair 24/7"
);
```

---

## 🔐 Best Practices

### 1. Proactive maintenance scheduling

```javascript
// Monitor upcoming maintenances and schedule proactively
async function monitorMaintenanceSchedule(vehicleId) {
  const historyResponse = await fetch(`/api/maintenance/vehicle/${vehicleId}/history`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const history = await historyResponse.json();
  
  if (history.data.insights.nextRecommendedMaintenance) {
    const next = history.data.insights.nextRecommendedMaintenance;
    const daysUntil = Math.ceil((new Date(next.estimatedDate) - new Date()) / (1000 * 60 * 60 * 24));
    
    console.log(`🔔 Next maintenance in ${daysUntil} days`);
    
    // Alert if maintenance is due soon
    if (daysUntil <= 7) {
      console.log(`⚠️ Maintenance due soon: ${next.description}`);
      console.log(`Estimated cost: ${next.estimatedCost.toLocaleString()} VND`);
      
      // Auto-schedule if urgent
      if (next.urgency === 'High') {
        await scheduleMaintenanceReminder(vehicleId, next);
      }
    }
  }
}

async function scheduleMaintenanceReminder(vehicleId, maintenanceInfo) {
  // Implementation would integrate with notification system
  console.log(`📅 Scheduling reminder for: ${maintenanceInfo.description}`);
}
```

### 2. Cost optimization analysis

```javascript
// Analyze maintenance costs and find optimization opportunities
async function analyzeCostOptimization(vehicleId) {
  const statsResponse = await fetch(`/api/maintenance/vehicle/${vehicleId}/statistics`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const stats = await statsResponse.json();
  
  // Analyze emergency repair rate
  const emergencyRate = stats.data.emergencyAnalysis.emergencyRate;
  if (emergencyRate > 20) {
    console.log('⚠️ High emergency repair rate detected');
    console.log('💡 Consider increasing preventive maintenance');
  }

  // Analyze service provider efficiency
  const providers = stats.data.serviceProviders;
  const bestProvider = providers.reduce((best, current) => 
    current.averageCost < best.averageCost && current.rating > 4.0 ? current : best
  );

  console.log(`🏆 Most cost-effective provider: ${bestProvider.name}`);
  console.log(`Average cost: ${bestProvider.averageCost.toLocaleString()} VND`);

  // Cost trend analysis
  if (stats.data.costTrends.isIncreasing && stats.data.costTrends.growthRate > 15) {
    console.log('📈 Maintenance costs increasing rapidly');
    console.log('💡 Review maintenance strategy and consider:');
    console.log('- Switching to more cost-effective service providers');
    console.log('- Increasing preventive maintenance frequency');
    console.log('- Evaluating vehicle condition for major issues');
  }
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