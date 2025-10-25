# Fund API Documentation

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

Module Fund API cung cấp các chức năng quản lý quỹ cho phương tiện trong hệ thống EV Co-ownership, bao gồm:
- **Xem số dư quỹ** với phân tích trạng thái tài chính
- **Lịch sử giao dịch** (nạp tiền và chi tiêu) với pagination
- **Quản lý chi tiêu** (tạo, sửa, xóa fund usage)
- **Phân tích ngân sách** theo danh mục chi tiêu
- **Tổng hợp tài chính** với thống kê chi tiết

**Đặc điểm chính:**
- **Role-based Access**: Co-owner của xe mới được truy cập
- **Budget Analysis**: Phân tích chi tiêu theo 5 categories
- **Fund Health Monitoring**: Healthy/Warning/Low status
- **Expense Tracking**: Link với maintenance records

---

## 🔗 Base URL

```
http://localhost:5215/api/fund
```

Trong production: `https://your-domain.com/api/fund`

---

## 🔐 Authentication

Tất cả endpoints yêu cầu JWT Bearer Token trong header:

```http
Authorization: Bearer {access_token}
```

**Role Requirements:**
- **Co-owner**: Chỉ được truy cập fund của xe mình tham gia
- **Staff/Admin**: Có thể truy cập fund của mọi xe

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 1 | GET | `/balance/{vehicleId}` | Xem số dư quỹ hiện tại | Co-owner, Staff, Admin |
| 2 | GET | `/additions/{vehicleId}` | Xem lịch sử nạp quỹ | Co-owner, Staff, Admin |
| 3 | GET | `/usages/{vehicleId}` | Xem lịch sử chi tiêu | Co-owner, Staff, Admin |
| 4 | GET | `/summary/{vehicleId}` | Xem tổng hợp quỹ đầy đủ | Co-owner, Staff, Admin |
| 5 | POST | `/usage` | Tạo giao dịch chi tiêu | Co-owner, Staff, Admin |
| 6 | PUT | `/usage/{usageId}` | Cập nhật chi tiêu | Co-owner, Staff, Admin |
| 7 | DELETE | `/usage/{usageId}` | Xóa chi tiêu (hoàn tiền) | Co-owner, Staff, Admin |
| 8 | GET | `/category/{vehicleId}/usages/{category}` | Xem chi tiêu theo danh mục | Co-owner, Staff, Admin |
| 9 | GET | `/category/{vehicleId}/analysis` | Phân tích ngân sách theo danh mục | Co-owner, Staff, Admin |

---

## 📖 Chi tiết từng API

### 1. 💰 Xem số dư quỹ - GET `/balance/{vehicleId}`

**Mô tả:** Lấy số dư hiện tại của quỹ với phân tích trạng thái tài chính.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe cần xem quỹ |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_BALANCE_RETRIEVED_SUCCESSFULLY",
  "data": {
    "fundId": 5,
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "licensePlate": "51A-12345",
    "currentBalance": 8500000,
    "totalAddedAmount": 15000000,
    "totalUsedAmount": 6500000,
    "totalAdditions": 12,
    "totalUsages": 8,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-10-20T14:25:00Z",
    "balanceStatus": "Healthy",
    "recommendedMinBalance": 5000000
  }
}
```

**Balance Status Logic:**
- **Healthy**: Balance ≥ 1.5x recommended minimum
- **Warning**: Balance between 1x and 1.5x recommended minimum  
- **Low**: Balance < recommended minimum

**Recommended minimum** = 2x average monthly expenses

---

### 2. 📈 Xem lịch sử nạp quỹ - GET `/additions/{vehicleId}`

**Mô tả:** Lấy lịch sử các giao dịch nạp tiền vào quỹ với pagination.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageNumber | int | ❌ | 1 | Số trang |
| pageSize | int | ❌ | 20 | Items per page (max: 100) |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_ADDITIONS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "id": 101,
      "fundId": 5,
      "coOwnerId": 3,
      "coOwnerName": "John Doe",
      "amount": 2000000,
      "paymentMethod": "BankTransfer",
      "transactionId": "TXN123456789",
      "description": "Monthly contribution",
      "status": "Completed",
      "createdAt": "2024-10-20T10:00:00Z"
    },
    {
      "id": 102,
      "fundId": 5,
      "coOwnerId": 5,
      "coOwnerName": "Jane Smith",
      "amount": 3000000,
      "paymentMethod": "Cash",
      "transactionId": "TXN123456790",
      "description": "Emergency fund top-up",
      "status": "Completed",
      "createdAt": "2024-10-18T15:30:00Z"
    }
  ]
}
```

**Payment Methods:**
- **BankTransfer**: Chuyển khoản ngân hàng
- **Cash**: Tiền mặt
- **CreditCard**: Thẻ tín dụng
- **DebitCard**: Thẻ ghi nợ
- **DigitalWallet**: Ví điện tử (Momo, ZaloPay, etc.)

---

### 3. 📉 Xem lịch sử chi tiêu - GET `/usages/{vehicleId}`

**Mô tả:** Lấy lịch sử các giao dịch chi tiêu từ quỹ với pagination.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageNumber | int | ❌ | 1 | Số trang |
| pageSize | int | ❌ | 20 | Items per page (max: 100) |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_USAGES_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "id": 201,
      "fundId": 5,
      "usageType": "Maintenance",
      "amount": 1500000,
      "description": "Brake pad replacement",
      "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
      "maintenanceCostId": 45,
      "createdAt": "2024-10-18T14:30:00Z"
    },
    {
      "id": 202,
      "fundId": 5,
      "usageType": "Insurance",
      "amount": 2000000,
      "description": "6-month insurance premium",
      "imageUrl": null,
      "maintenanceCostId": null,
      "createdAt": "2024-10-15T09:00:00Z"
    }
  ]
}
```

**Usage Types:**
- **Maintenance** (0): Bảo dưỡng, sửa chữa
- **Insurance** (1): Bảo hiểm
- **Fuel** (2): Xăng, điện sạc
- **Parking** (3): Phí đỗ xe, bảo quản
- **Other** (4): Chi phí khác

---

### 4. 📊 Xem tổng hợp quỹ đầy đủ - GET `/summary/{vehicleId}`

**Mô tả:** Lấy tổng hợp đầy đủ về quỹ bao gồm số dư, lịch sử gần đây và thống kê.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| monthsToAnalyze | int | ❌ | 6 | Số tháng phân tích (max: 24) |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_SUMMARY_RETRIEVED_SUCCESSFULLY",
  "data": {
    "balance": {
      "fundId": 5,
      "vehicleId": 1,
      "currentBalance": 8500000,
      "balanceStatus": "Healthy",
      "recommendedMinBalance": 5000000
    },
    "recentAdditions": [
      {
        "id": 101,
        "coOwnerName": "John Doe",
        "amount": 2000000,
        "paymentMethod": "BankTransfer",
        "description": "Monthly contribution",
        "createdAt": "2024-10-20T10:00:00Z"
      }
    ],
    "recentUsages": [
      {
        "id": 201,
        "usageType": "Maintenance",
        "amount": 1500000,
        "description": "Brake pad replacement",
        "createdAt": "2024-10-18T14:30:00Z"
      }
    ],
    "statistics": {
      "averageMonthlyAddition": 2500000,
      "averageMonthlyUsage": 1800000,
      "netMonthlyFlow": 700000,
      "monthsCovered": 4,
      "usageByType": {
        "Maintenance": 3500000,
        "Insurance": 2000000,
        "Fuel": 1200000,
        "Parking": 500000,
        "Other": 300000
      },
      "monthlyFlows": [
        {
          "year": 2024,
          "month": 10,
          "totalAdded": 3000000,
          "totalUsed": 2200000,
          "netFlow": 800000,
          "endingBalance": 8500000
        },
        {
          "year": 2024,
          "month": 9,
          "totalAdded": 2500000,
          "totalUsed": 1800000,
          "netFlow": 700000,
          "endingBalance": 7700000
        }
      ]
    }
  }
}
```

**Key Metrics:**
- **averageMonthlyAddition**: Trung bình nạp mỗi tháng
- **averageMonthlyUsage**: Trung bình chi mỗi tháng
- **netMonthlyFlow**: Cash flow ròng (addition - usage)
- **monthsCovered**: Số tháng có thể chi với số dư hiện tại
- **usageByType**: Phân bổ chi tiêu theo category

---

### 5. ➕ Tạo giao dịch chi tiêu - POST `/usage`

**Mô tả:** Tạo giao dịch chi tiêu mới từ quỹ với tự động trừ số dư.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Request Body:**
```json
{
  "vehicleId": 1,
  "usageType": 0,
  "amount": 1500000,
  "description": "Brake pad replacement",
  "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
  "maintenanceCostId": 45
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| vehicleId | int | ✅ | Must exist, user must be co-owner |
| usageType | int | ✅ | 0-4 (enum EUsageType) |
| amount | decimal | ✅ | > 0, <= fund balance |
| description | string | ✅ | Max 500 chars |
| imageUrl | string | ❌ | Valid URL, receipt proof |
| maintenanceCostId | int | ❌ | Link to maintenance record |

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "FUND_USAGE_CREATED_SUCCESSFULLY",
  "data": {
    "usageId": 301,
    "fundId": 5,
    "vehicleId": 1,
    "usageType": "Maintenance",
    "amount": 1500000,
    "description": "Brake pad replacement",
    "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
    "maintenanceCostId": 45,
    "remainingBalance": 7000000,
    "createdAt": "2024-10-24T10:00:00Z"
  }
}
```

**Validation Rules:**
- Fund balance phải đủ để chi
- UsageType phải trong enum (0-4)
- Amount > 0
- MaintenanceCostId phải tồn tại nếu provided

---

### 6. ✏️ Cập nhật chi tiêu - PUT `/usage/{usageId}`

**Mô tả:** Cập nhật thông tin giao dịch chi tiêu đã có (tự động điều chỉnh fund balance).

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| usageId | int | ✅ | ID của fund usage cần update |

**Request Body:**
```json
{
  "amount": 1800000,
  "description": "Brake pad and rotor replacement (updated)",
  "imageUrl": "https://storage.example.com/receipts/receipt124.jpg"
}
```

**Request Schema:** Tất cả fields đều optional
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| usageType | int | ❌ | 0-4 (enum EUsageType) |
| amount | decimal | ❌ | > 0, fund must support the difference |
| description | string | ❌ | Max 500 chars |
| imageUrl | string | ❌ | Valid URL |
| maintenanceCostId | int | ❌ | Must exist if provided |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_USAGE_UPDATED_SUCCESSFULLY",
  "data": {
    "usageId": 301,
    "oldAmount": 1500000,
    "newAmount": 1800000,
    "amountDifference": 300000,
    "newFundBalance": 6700000,
    "description": "Brake pad and rotor replacement (updated)",
    "updatedAt": "2024-10-24T12:00:00Z"
  }
}
```

**Business Logic:**
- Nếu tăng amount: Trừ thêm từ fund balance
- Nếu giảm amount: Hoàn lại vào fund balance
- Fund balance phải đủ để cover việc tăng amount

---

### 7. 🗑️ Xóa chi tiêu (hoàn tiền) - DELETE `/usage/{usageId}`

**Mô tả:** Xóa giao dịch chi tiêu và hoàn lại số tiền vào quỹ.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| usageId | int | ✅ | ID của fund usage cần xóa |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_USAGE_DELETED_SUCCESSFULLY",
  "data": {
    "deletedId": 201,
    "refundedAmount": 1500000,
    "newFundBalance": 10000000,
    "deletedAt": "2024-10-24T14:00:00Z"
  }
}
```

**Effects:**
- Xóa record fund usage
- Hoàn tiền vào fund balance
- Update fund UpdatedAt timestamp

**Use Cases:**
- Sửa lỗi entry
- Xóa duplicate records
- Cancel unverified expenses

---

### 8. 📂 Xem chi tiêu theo danh mục - GET `/category/{vehicleId}/usages/{category}`

**Mô tả:** Lấy danh sách chi tiêu theo category cụ thể với optional date filtering.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |
| category | int | ✅ | Category (0-4) |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | ❌ | Filter từ ngày (ISO 8601) |
| endDate | DateTime | ❌ | Filter đến ngày (ISO 8601) |

**Category Mapping:**
- **0**: Maintenance
- **1**: Insurance  
- **2**: Fuel
- **3**: Parking
- **4**: Other

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "FUND_USAGES_BY_CATEGORY_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "category": "Maintenance",
    "categoryCode": 0,
    "dateRange": {
      "startDate": "2024-10-01T00:00:00Z",
      "endDate": "2024-10-31T23:59:59Z"
    },
    "usages": [
      {
        "id": 201,
        "amount": 1500000,
        "description": "Brake pad replacement",
        "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
        "maintenanceCostId": 45,
        "createdAt": "2024-10-18T14:30:00Z"
      },
      {
        "id": 205,
        "amount": 2500000,
        "description": "Battery checkup and replacement",
        "imageUrl": "https://storage.example.com/receipts/receipt127.jpg",
        "maintenanceCostId": 48,
        "createdAt": "2024-10-10T09:15:00Z"
      }
    ],
    "totalAmount": 4000000,
    "totalCount": 2,
    "averageAmount": 2000000
  }
}
```

**Example Requests:**
```
GET /api/fund/category/1/usages/0                              # All maintenance
GET /api/fund/category/1/usages/1?startDate=2024-10-01        # Insurance from Oct 1
GET /api/fund/category/1/usages/2?startDate=2024-10-01&endDate=2024-10-31  # Fuel in October
```

---

### 9. 📊 Phân tích ngân sách theo danh mục - GET `/category/{vehicleId}/analysis`

**Mô tả:** Phân tích ngân sách tháng hiện tại theo từng category với budget limits.

**Role:** Co-owner (xe mình tham gia), Staff, Admin

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| vehicleId | int | ✅ | ID của xe |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CATEGORY_BUDGET_ANALYSIS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "analysisMonth": 10,
    "analysisYear": 2024,
    "categoryBudgets": [
      {
        "category": "Maintenance",
        "categoryCode": 0,
        "monthlyBudgetLimit": 3000000,
        "currentMonthSpending": 2500000,
        "remainingBudget": 500000,
        "budgetUtilizationPercent": 83.33,
        "budgetStatus": "Warning",
        "transactionCount": 3,
        "averageTransactionAmount": 833333
      },
      {
        "category": "Insurance",
        "categoryCode": 1,
        "monthlyBudgetLimit": 1000000,
        "currentMonthSpending": 2000000,
        "remainingBudget": -1000000,
        "budgetUtilizationPercent": 200.0,
        "budgetStatus": "Exceeded",
        "transactionCount": 1,
        "averageTransactionAmount": 2000000
      },
      {
        "category": "Fuel",
        "categoryCode": 2,
        "monthlyBudgetLimit": 2000000,
        "currentMonthSpending": 1200000,
        "remainingBudget": 800000,
        "budgetUtilizationPercent": 60.0,
        "budgetStatus": "OnTrack",
        "transactionCount": 4,
        "averageTransactionAmount": 300000
      },
      {
        "category": "Parking",
        "categoryCode": 3,
        "monthlyBudgetLimit": 500000,
        "currentMonthSpending": 300000,
        "remainingBudget": 200000,
        "budgetUtilizationPercent": 60.0,
        "budgetStatus": "OnTrack",
        "transactionCount": 2,
        "averageTransactionAmount": 150000
      },
      {
        "category": "Other",
        "categoryCode": 4,
        "monthlyBudgetLimit": 1000000,
        "currentMonthSpending": 200000,
        "remainingBudget": 800000,
        "budgetUtilizationPercent": 20.0,
        "budgetStatus": "OnTrack",
        "transactionCount": 1,
        "averageTransactionAmount": 200000
      }
    ],
    "totalBudget": 7500000,
    "totalSpending": 6200000,
    "overallUtilizationPercent": 82.67,
    "overallBudgetStatus": "Warning",
    "monthlyTrend": "Tăng 15% so với tháng trước",
    "recommendations": [
      "Insurance vượt ngân sách 100% - cần xem xét lại",
      "Maintenance sắp đạt ngân sách - theo dõi chặt chẽ",
      "Fuel và Parking đang ổn định"
    ]
  }
}
```

**Default Monthly Budget Limits:**
- **Maintenance**: 3,000,000 VND
- **Insurance**: 1,000,000 VND  
- **Fuel**: 2,000,000 VND
- **Parking**: 500,000 VND
- **Other**: 1,000,000 VND
- **Total**: 7,500,000 VND/month

**Budget Status Logic:**
- **OnTrack**: Spending < 80% of budget
- **Warning**: Spending 80-100% of budget  
- **Exceeded**: Spending > budget

---

## 🔢 Enums và Constants

### Usage Type (EUsageType)
```typescript
enum EUsageType {
  Maintenance = 0,   // Bảo dưỡng, sửa chữa
  Insurance = 1,     // Bảo hiểm
  Fuel = 2,         // Xăng, điện sạc
  Parking = 3,      // Phí đỗ xe, bảo quản
  Other = 4         // Chi phí khác
}
```

### Fund Addition Status (EFundAdditionStatus)
```typescript
enum EFundAdditionStatus {
  Pending = 0,      // Chờ xử lý
  Completed = 1,    // Đã hoàn thành
  Failed = 2,       // Thất bại
  Cancelled = 3     // Đã hủy
}
```

### Payment Method (EPaymentMethod)
```typescript
enum EPaymentMethod {
  Cash = 0,         // Tiền mặt
  BankTransfer = 1, // Chuyển khoản
  CreditCard = 2,   // Thẻ tín dụng
  DebitCard = 3,    // Thẻ ghi nợ
  DigitalWallet = 4 // Ví điện tử
}
```

---

## ❌ Error Codes

### Fund Access Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 403 | Forbidden | `ACCESS_DENIED_NOT_VEHICLE_CO_OWNER` | Không phải đồng sở hữu của xe |
| 404 | Not Found | `VEHICLE_NOT_FOUND` | Không tìm thấy xe |
| 404 | Not Found | `FUND_NOT_FOUND_FOR_VEHICLE` | Xe chưa có quỹ |
| 404 | Not Found | `FUND_NOT_FOUND` | Không tìm thấy quỹ |

### Fund Usage Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 400 | Bad Request | `INVALID_AMOUNT` | Số tiền không hợp lệ |
| 400 | Bad Request | `INSUFFICIENT_FUND_BALANCE` | Quỹ không đủ số dư |
| 404 | Not Found | `FUND_USAGE_NOT_FOUND` | Không tìm thấy giao dịch chi tiêu |
| 404 | Not Found | `MAINTENANCE_COST_NOT_FOUND` | Không tìm thấy maintenance record |

### Category Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 400 | Bad Request | `INVALID_CATEGORY` | Category không hợp lệ (phải 0-4) |

### System Errors (5xx)
| Status | Code | Ý nghĩa |
|--------|------|---------|
| 500 | Internal Server Error | `INTERNAL_SERVER_ERROR` | Lỗi hệ thống |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Kiểm tra tình trạng tài chính xe

```javascript
// 1. Xem số dư quỹ
const balanceResponse = await fetch('/api/fund/balance/1', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const balance = await balanceResponse.json();
console.log(`Current balance: ${balance.data.currentBalance.toLocaleString()} VND`);
console.log(`Status: ${balance.data.balanceStatus}`);

// 2. Xem phân tích ngân sách tháng này
const analysisResponse = await fetch('/api/fund/category/1/analysis', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const analysis = await analysisResponse.json();
console.log(`Total spending this month: ${analysis.data.totalSpending.toLocaleString()} VND`);
console.log(`Budget utilization: ${analysis.data.overallUtilizationPercent}%`);

// 3. Xem tổng hợp đầy đủ
const summaryResponse = await fetch('/api/fund/summary/1?monthsToAnalyze=6', {
  headers: { 'Authorization': `Bearer ${token}` }
});

const summary = await summaryResponse.json();
console.log(`Average monthly cash flow: ${summary.data.statistics.netMonthlyFlow.toLocaleString()} VND`);
console.log(`Months covered: ${summary.data.statistics.monthsCovered} months`);
```

### Use Case 2: Ghi nhận chi tiêu bảo dưỡng

```javascript
// 1. Tạo fund usage cho bảo dưỡng
const createUsageResponse = await fetch('/api/fund/usage', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    vehicleId: 1,
    usageType: 0, // Maintenance
    amount: 2500000,
    description: "Thay pin xe điện tại Tesla Service Center",
    imageUrl: "https://storage.example.com/receipts/tesla-battery-receipt.jpg",
    maintenanceCostId: 78
  })
});

const usageData = await createUsageResponse.json();

if (usageData.statusCode === 201) {
  console.log(`Created usage ${usageData.data.usageId}`);
  console.log(`Remaining balance: ${usageData.data.remainingBalance.toLocaleString()} VND`);
  
  // 2. Xem chi tiêu maintenance trong tháng
  const maintenanceResponse = await fetch('/api/fund/category/1/usages/0?startDate=2024-10-01&endDate=2024-10-31', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const maintenance = await maintenanceResponse.json();
  console.log(`Total maintenance this month: ${maintenance.data.totalAmount.toLocaleString()} VND`);
} else {
  console.error('Failed to create usage:', usageData.message);
}
```

### Use Case 3: Phân tích chi tiêu theo category

```javascript
// 1. Xem tất cả categories
const categories = [
  { code: 0, name: 'Maintenance' },
  { code: 1, name: 'Insurance' },
  { code: 2, name: 'Fuel' },
  { code: 3, name: 'Parking' },
  { code: 4, name: 'Other' }
];

const categoryAnalysis = {};

// 2. Lấy chi tiêu từng category
for (const category of categories) {
  const response = await fetch(`/api/fund/category/1/usages/${category.code}?startDate=2024-10-01&endDate=2024-10-31`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await response.json();
  categoryAnalysis[category.name] = {
    totalAmount: data.data.totalAmount,
    totalCount: data.data.totalCount,
    averageAmount: data.data.averageAmount
  };
}

console.log('Category spending analysis:', categoryAnalysis);

// 3. Tìm category chi nhiều nhất
const topSpendingCategory = Object.keys(categoryAnalysis)
  .reduce((a, b) => categoryAnalysis[a].totalAmount > categoryAnalysis[b].totalAmount ? a : b);

console.log(`Highest spending category: ${topSpendingCategory}`);
console.log(`Amount: ${categoryAnalysis[topSpendingCategory].totalAmount.toLocaleString()} VND`);
```

### Use Case 4: Sửa/xóa chi tiêu

```javascript
// 1. Cập nhật fund usage (tăng amount)
const updateResponse = await fetch('/api/fund/usage/301', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    amount: 2800000, // Tăng từ 2500000
    description: "Thay pin xe điện + kiểm tra hệ thống điện (updated)",
    imageUrl: "https://storage.example.com/receipts/tesla-battery-updated.jpg"
  })
});

const updateData = await updateResponse.json();

if (updateData.statusCode === 200) {
  console.log(`Updated usage. Difference: ${updateData.data.amountDifference.toLocaleString()} VND`);
  console.log(`New fund balance: ${updateData.data.newFundBalance.toLocaleString()} VND`);
} else {
  console.error('Update failed:', updateData.message);
}

// 2. Xóa fund usage nếu cần (hoàn tiền)
const confirmDelete = confirm('Are you sure you want to delete this expense and refund to fund?');

if (confirmDelete) {
  const deleteResponse = await fetch('/api/fund/usage/301', {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const deleteData = await deleteResponse.json();
  
  if (deleteData.statusCode === 200) {
    console.log(`Deleted usage. Refunded: ${deleteData.data.refundedAmount.toLocaleString()} VND`);
    console.log(`New fund balance: ${deleteData.data.newFundBalance.toLocaleString()} VND`);
  }
}
```

### Use Case 5: Dashboard tài chính

```javascript
// Tạo dashboard tổng hợp tình trạng tài chính
async function createFinancialDashboard(vehicleId) {
  // 1. Lấy số dư hiện tại
  const balance = await fetch(`/api/fund/balance/${vehicleId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());

  // 2. Lấy phân tích ngân sách
  const budgetAnalysis = await fetch(`/api/fund/category/${vehicleId}/analysis`, {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());

  // 3. Lấy tổng hợp 6 tháng
  const summary = await fetch(`/api/fund/summary/${vehicleId}?monthsToAnalyze=6`, {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());

  // 4. Tạo dashboard object
  const dashboard = {
    currentBalance: balance.data.currentBalance,
    balanceStatus: balance.data.balanceStatus,
    monthlyBudgetUsage: budgetAnalysis.data.overallUtilizationPercent,
    averageMonthlyCashFlow: summary.data.statistics.netMonthlyFlow,
    monthsCovered: summary.data.statistics.monthsCovered,
    topSpendingCategory: budgetAnalysis.data.categoryBudgets
      .sort((a, b) => b.currentMonthSpending - a.currentMonthSpending)[0],
    recentTransactions: {
      additions: summary.data.recentAdditions,
      usages: summary.data.recentUsages
    },
    recommendations: budgetAnalysis.data.recommendations
  };

  return dashboard;
}

// Sử dụng
const dashboard = await createFinancialDashboard(1);
console.log('Financial Dashboard:', dashboard);

// Hiển thị alerts
if (dashboard.balanceStatus === 'Low') {
  alert('⚠️ Fund balance is low! Consider adding more funds.');
}

if (dashboard.monthlyBudgetUsage > 90) {
  alert('💰 Monthly budget almost exhausted!');
}

dashboard.recommendations.forEach(rec => {
  console.log(`💡 ${rec}`);
});
```

---

## 🔐 Best Practices

### 1. Real-time balance monitoring

```javascript
// Monitor fund balance changes với polling
function startFundMonitoring(vehicleId, callback) {
  let lastBalance = null;
  
  const checkBalance = async () => {
    try {
      const response = await fetch(`/api/fund/balance/${vehicleId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      const data = await response.json();
      const currentBalance = data.data.currentBalance;
      
      if (lastBalance !== null && currentBalance !== lastBalance) {
        callback({
          oldBalance: lastBalance,
          newBalance: currentBalance,
          change: currentBalance - lastBalance,
          status: data.data.balanceStatus
        });
      }
      
      lastBalance = currentBalance;
    } catch (error) {
      console.error('Fund monitoring error:', error);
    }
  };

  // Check every 30 seconds
  const interval = setInterval(checkBalance, 30000);
  checkBalance(); // Initial check

  return () => clearInterval(interval); // Return cleanup function
}

// Sử dụng
const stopMonitoring = startFundMonitoring(1, (change) => {
  console.log(`Fund balance changed: ${change.change.toLocaleString()} VND`);
  console.log(`New balance: ${change.newBalance.toLocaleString()} VND`);
  
  if (change.status === 'Low') {
    showLowBalanceWarning();
  }
});
```

### 2. Smart expense categorization

```javascript
// Auto-suggest category dựa trên description
function suggestCategory(description) {
  const keywords = {
    maintenance: ['bảo dưỡng', 'sửa chữa', 'thay', 'kiểm tra', 'maintenance', 'repair', 'replace'],
    insurance: ['bảo hiểm', 'insurance', 'premium'],
    fuel: ['xăng', 'điện', 'sạc', 'fuel', 'charge', 'gas', 'electric'],
    parking: ['đỗ xe', 'parking', 'garage', 'bãi đỗ'],
  };

  const desc = description.toLowerCase();

  for (const [category, words] of Object.entries(keywords)) {
    if (words.some(word => desc.includes(word))) {
      return {
        category: category,
        code: ['maintenance', 'insurance', 'fuel', 'parking', 'other'].indexOf(category),
        confidence: 0.8
      };
    }
  }

  return { category: 'other', code: 4, confidence: 0.3 };
}

// Sử dụng
const expense = {
  description: "Thay pin xe điện tại Tesla Service"
};

const suggestion = suggestCategory(expense.description);
console.log(`Suggested category: ${suggestion.category} (${suggestion.confidence * 100}% confidence)`);
```

### 3. Expense validation

```javascript
// Validate expense trước khi submit
async function validateExpense(vehicleId, amount, category) {
  // 1. Check fund balance
  const balanceResponse = await fetch(`/api/fund/balance/${vehicleId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const balance = await balanceResponse.json();
  
  if (amount > balance.data.currentBalance) {
    return { valid: false, error: 'Insufficient fund balance' };
  }

  // 2. Check budget limits
  const analysisResponse = await fetch(`/api/fund/category/${vehicleId}/analysis`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const analysis = await analysisResponse.json();
  const categoryBudget = analysis.data.categoryBudgets.find(b => b.categoryCode === category);
  
  if (categoryBudget && categoryBudget.remainingBudget < amount) {
    return { 
      valid: false, 
      error: `Exceeds monthly budget for ${categoryBudget.category}`,
      budgetRemaining: categoryBudget.remainingBudget
    };
  }

  // 3. Check for large amounts
  if (amount > 5000000) { // 5M VND
    return { 
      valid: false, 
      error: 'Large amount detected. Please confirm.',
      requiresConfirmation: true
    };
  }

  return { valid: true };
}

// Sử dụng
const validation = await validateExpense(1, 2500000, 0);

if (!validation.valid) {
  if (validation.requiresConfirmation) {
    const confirm = window.confirm(`${validation.error} Continue?`);
    if (!confirm) return;
  } else {
    alert(validation.error);
    return;
  }
}

// Proceed with expense creation
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