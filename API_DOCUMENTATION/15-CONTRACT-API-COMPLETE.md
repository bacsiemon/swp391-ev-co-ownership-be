# Contract API Documentation - Electronic Contract Management

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

Module Contract API cung cấp các chức năng quản lý hợp đồng điện tử toàn diện cho hệ thống đồng sở hữu xe điện, bao gồm:
- Tạo và quản lý hợp đồng điện tử
- Ký số và xác thực chữ ký điện tử
- Theo dõi trạng thái và quy trình phê duyệt
- Quản lý mẫu hợp đồng và tùy chỉnh
- Xuất hợp đồng PDF cho lưu trữ

**Các loại hợp đồng được hỗ trợ:**
- **Hợp đồng đồng sở hữu** (CoOwnershipAgreement)
- **Thỏa thuận sử dụng xe** (VehicleUsageAgreement)
- **Thỏa thuận chia sẻ chi phí** (CostSharingAgreement)
- **Hợp đồng bảo trì** (MaintenanceAgreement)

---

## 🔗 Base URL

```
http://localhost:5215/api/contract
```

Trong production: `https://your-domain.com/api/contract`

---

## 🔐 Authentication

Tất cả API yêu cầu JWT Bearer Token trong header:

```http
Authorization: Bearer {access_token}
```

**Role Requirements:**
- **CoOwner**: Có thể tạo, ký, và quản lý hợp đồng
- **Admin**: Có thể quản lý tất cả hợp đồng và chấm dút hợp đồng

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Auth Required |
|-----|--------|----------|-------|---------------|
| 1 | POST | `/` | Tạo hợp đồng điện tử mới | ✅ CoOwner |
| 2 | GET | `/{contractId}` | Lấy chi tiết hợp đồng theo ID | ✅ CoOwner |
| 3 | GET | `/` | Lấy danh sách hợp đồng với bộ lọc | ✅ CoOwner |
| 4 | POST | `/{contractId}/sign` | Ký hợp đồng điện tử | ✅ CoOwner |
| 5 | POST | `/{contractId}/decline` | Từ chối ký hợp đồng | ✅ CoOwner |
| 6 | POST | `/{contractId}/terminate` | Chấm dút hợp đồng sớm | ✅ CoOwner/Admin |
| 7 | GET | `/templates` | Lấy danh sách mẫu hợp đồng | ✅ CoOwner |
| 8 | GET | `/templates/{templateType}` | Lấy chi tiết mẫu hợp đồng | ✅ CoOwner |
| 9 | GET | `/{contractId}/download` | Tải hợp đồng PDF | ✅ CoOwner |
| 10 | GET | `/pending-signature` | Lấy hợp đồng chờ ký | ✅ CoOwner |
| 11 | GET | `/signed` | Lấy hợp đồng đã ký | ✅ CoOwner |

---

## 📖 Chi tiết từng API

### 1. 📝 Tạo hợp đồng điện tử - POST `/`

**Mô tả:** Tạo hợp đồng điện tử mới cho đồng sở hữu xe, thỏa thuận sử dụng, chia sẻ chi phí, v.v.

**Role requirement:** CoOwner

**Request Body:**
```json
{
  "vehicleId": 5,
  "templateType": "CoOwnershipAgreement",
  "title": "2025 Tesla Model 3 Co-Ownership Agreement",
  "description": "This agreement establishes the co-ownership terms for our shared Tesla Model 3",
  "signatoryUserIds": [10, 15, 20],
  "effectiveDate": "2025-11-01T00:00:00Z",
  "expiryDate": "2027-10-31T23:59:59Z",
  "signatureDeadline": "2025-11-15T23:59:59Z",
  "autoActivate": true,
  "customTerms": {
    "usageRatio": "40:35:25",
    "costSharingMethod": "proportional",
    "maintenanceResponsibility": "shared"
  },
  "attachmentUrls": ["https://storage.com/vehicle-inspection-report.pdf"]
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| vehicleId | integer | ✅ | Must be valid vehicle ID |
| templateType | string | ✅ | CoOwnershipAgreement, VehicleUsageAgreement, CostSharingAgreement, MaintenanceAgreement |
| title | string | ✅ | 5-200 characters |
| description | string | ❌ | Max 2000 characters |
| signatoryUserIds | array | ✅ | List of valid user IDs (excluding creator) |
| effectiveDate | datetime | ❌ | Future date, default: contract creation |
| expiryDate | datetime | ❌ | After effective date |
| signatureDeadline | datetime | ❌ | Default: 30 days from creation |
| autoActivate | boolean | ❌ | Default: true |
| customTerms | object | ❌ | JSON with custom terms |
| attachmentUrls | array | ❌ | URLs to supporting documents |

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "CONTRACT_CREATED_SUCCESSFULLY",
  "data": {
    "contractId": 42,
    "title": "2025 Tesla Model 3 Co-Ownership Agreement",
    "templateType": "CoOwnershipAgreement",
    "status": "PendingSignatures",
    "creatorName": "John Doe",
    "vehicleName": "Tesla Model 3 - 29A-12345",
    "signatories": [
      {
        "userId": 10,
        "userName": "John Doe",
        "email": "john@example.com",
        "signatureStatus": "Pending",
        "isCreator": true
      },
      {
        "userId": 15,
        "userName": "Jane Smith",
        "email": "jane@example.com", 
        "signatureStatus": "Pending",
        "isCreator": false
      }
    ],
    "effectiveDate": "2025-11-01T00:00:00Z",
    "expiryDate": "2027-10-31T23:59:59Z",
    "signatureDeadline": "2025-11-15T23:59:59Z",
    "createdAt": "2025-10-24T14:30:00Z"
  }
}
```

**Error Responses:**
- `403 NOT_AUTHORIZED` - User is not a co-owner
- `404 VEHICLE_NOT_FOUND` - Vehicle ID not found
- `400 INVALID_SIGNATORY_USER_IDS` - Invalid or non-existent user IDs

---

### 2. 📄 Lấy chi tiết hợp đồng - GET `/{contractId}`

**Mô tả:** Lấy thông tin chi tiết của hợp đồng bao gồm nội dung, chữ ký, và trạng thái.

**Role requirement:** CoOwner (phải là người tạo, người ký, hoặc đồng sở hữu xe)

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_RETRIEVED_SUCCESSFULLY",
  "data": {
    "contractId": 42,
    "title": "2025 Tesla Model 3 Co-Ownership Agreement",
    "description": "This agreement establishes the co-ownership terms...",
    "templateType": "CoOwnershipAgreement",
    "status": "FullySigned",
    "vehicleInfo": {
      "vehicleId": 5,
      "vehicleName": "Tesla Model 3",
      "licensePlate": "29A-12345"
    },
    "creatorInfo": {
      "userId": 10,
      "userName": "John Doe",
      "email": "john@example.com"
    },
    "contractContent": "THỎA THUẬN ĐỒNG SỞ HỮU XE ĐIỆN...",
    "customTerms": {
      "usageRatio": "40:35:25",
      "costSharingMethod": "proportional"
    },
    "signatories": [
      {
        "userId": 10,
        "userName": "John Doe",
        "signatureStatus": "Signed",
        "signedAt": "2025-10-24T15:00:00Z",
        "signatureHash": "abc123...",
        "ipAddress": "192.168.1.100",
        "deviceInfo": "Chrome 120.0 on Windows 11"
      }
    ],
    "effectiveDate": "2025-11-01T00:00:00Z",
    "expiryDate": "2027-10-31T23:59:59Z",
    "signatureDeadline": "2025-11-15T23:59:59Z",
    "attachmentUrls": ["https://storage.com/vehicle-inspection-report.pdf"],
    "createdAt": "2025-10-24T14:30:00Z",
    "lastUpdatedAt": "2025-10-24T15:00:00Z"
  }
}
```

**Error Responses:**
- `403 ACCESS_DENIED` - User not authorized to view this contract
- `404 CONTRACT_NOT_FOUND` - Contract ID not found

---

### 3. 📋 Lấy danh sách hợp đồng - GET `/`

**Mô tả:** Lấy danh sách hợp đồng với các bộ lọc và phân trang.

**Role requirement:** CoOwner

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| vehicleId | integer | Lọc theo xe cụ thể |
| templateType | string | Lọc theo loại hợp đồng |
| status | string | Draft, PendingSignatures, PartiallySigned, FullySigned, Active, Expired, Terminated, Rejected |
| isCreator | boolean | Hợp đồng do bạn tạo |
| isSignatory | boolean | Hợp đồng bạn cần ký |
| mySignatureStatus | string | Pending, Signed, Declined |
| createdFrom | datetime | Từ ngày tạo |
| createdTo | datetime | Đến ngày tạo |
| activeOnly | boolean | Chỉ hợp đồng đang hoạt động |
| pendingMySignature | boolean | Chờ chữ ký của bạn |
| pageNumber | integer | Số trang (default: 1) |
| pageSize | integer | Số item/trang (default: 20, max: 100) |
| sortBy | string | CreatedAt, UpdatedAt, EffectiveDate, ExpiryDate, Title |
| sortOrder | string | asc, desc (default: desc) |

**Sample Request:**
```http
GET /api/contract?vehicleId=5&status=Active&pageNumber=1&pageSize=20
Authorization: Bearer {token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACTS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "contracts": [
      {
        "contractId": 42,
        "title": "2025 Tesla Model 3 Co-Ownership Agreement",
        "templateType": "CoOwnershipAgreement",
        "status": "Active",
        "vehicleName": "Tesla Model 3 - 29A-12345",
        "creatorName": "John Doe",
        "myRole": "Creator",
        "mySignatureStatus": "Signed",
        "totalSignatories": 3,
        "completedSignatures": 3,
        "effectiveDate": "2025-11-01T00:00:00Z",
        "expiryDate": "2027-10-31T23:59:59Z",
        "createdAt": "2025-10-24T14:30:00Z"
      }
    ],
    "pagination": {
      "pageNumber": 1,
      "pageSize": 20,
      "totalItems": 45,
      "totalPages": 3,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  }
}
```

---

### 4. ✍️ Ký hợp đồng điện tử - POST `/{contractId}/sign`

**Mô tả:** Ký hợp đồng điện tử với chữ ký số và metadata bảo mật.

**Role requirement:** CoOwner (phải là người ký được chỉ định)

**Request Body:**
```json
{
  "signature": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "ipAddress": "192.168.1.100",
  "deviceInfo": "Chrome 120.0 on Windows 11",
  "geolocation": "21.0285,105.8542",
  "agreementConfirmation": "I agree to all terms and conditions",
  "signerNotes": "Reviewed and approved all terms"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| signature | string | ✅ | Min 10 characters, encrypted hash |
| ipAddress | string | ❌ | Valid IP address |
| deviceInfo | string | ❌ | Device/browser information |
| geolocation | string | ❌ | GPS coordinates |
| agreementConfirmation | string | ❌ | Confirmation text |
| signerNotes | string | ❌ | Max 500 characters |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_SIGNED_SUCCESSFULLY",
  "data": {
    "contractId": 42,
    "title": "2025 Tesla Model 3 Co-Ownership Agreement",
    "signatureId": 123,
    "signedAt": "2025-10-24T15:00:00Z",
    "contractStatus": "FullySigned",
    "isFullySigned": true,
    "isNowActive": true,
    "remainingSignatures": 0,
    "signatureProgress": {
      "completed": 3,
      "total": 3,
      "percentage": 100
    }
  }
}
```

**Error Responses:**
- `403 NOT_A_SIGNATORY` - User not listed as signatory
- `400 ALREADY_SIGNED` - User already signed this contract
- `400 SIGNATURE_DEADLINE_EXPIRED` - Signature deadline passed
- `400 CONTRACT_NOT_PENDING_SIGNATURES` - Contract not in correct status

---

### 5. ❌ Từ chối ký hợp đồng - POST `/{contractId}/decline`

**Mô tả:** Từ chối ký hợp đồng với lý do và đề xuất thay đổi.

**Role requirement:** CoOwner (phải là người ký được chỉ định)

**Request Body:**
```json
{
  "reason": "The cost sharing ratio doesn't reflect my actual usage pattern. I use the vehicle 40% of the time but the agreement splits costs equally.",
  "suggestedChanges": "Suggest changing to usage-based cost sharing: 40% me, 35% John, 25% Sarah based on actual booking hours"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| reason | string | ✅ | 10-1000 characters |
| suggestedChanges | string | ❌ | Max 2000 characters |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_DECLINED_SUCCESSFULLY",
  "data": {
    "contractId": 42,
    "declinedAt": "2025-10-24T15:30:00Z",
    "contractStatus": "Rejected",
    "reason": "The cost sharing ratio doesn't reflect my actual usage pattern...",
    "suggestedChanges": "Suggest changing to usage-based cost sharing..."
  }
}
```

**Error Responses:**
- `403 NOT_A_SIGNATORY` - User not listed as signatory
- `400 ALREADY_SIGNED` - User already signed this contract
- `404 CONTRACT_NOT_FOUND` - Contract not found

---

### 6. 🚫 Chấm dút hợp đồng - POST `/{contractId}/terminate`

**Mô tả:** Chấm dút hợp đồng đang hoạt động trước thời hạn.

**Role requirement:** CoOwner (người tạo) hoặc Admin

**Request Body:**
```json
{
  "reason": "Vehicle has been sold. All co-owners have agreed to terminate the co-ownership agreement.",
  "effectiveDate": "2025-12-31T23:59:59Z",
  "notes": "Final settlement completed. All parties satisfied."
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| reason | string | ✅ | 10-1000 characters |
| effectiveDate | datetime | ❌ | Default: now |
| notes | string | ❌ | Max 2000 characters |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_TERMINATED_SUCCESSFULLY",
  "data": {
    "contractId": 42,
    "terminatedAt": "2025-10-24T16:00:00Z",
    "terminatedBy": "John Doe",
    "contractStatus": "Terminated",
    "effectiveDate": "2025-12-31T23:59:59Z",
    "reason": "Vehicle has been sold...",
    "notes": "Final settlement completed..."
  }
}
```

**Error Responses:**
- `403 NOT_AUTHORIZED` - Only creator or admin can terminate
- `400 ALREADY_TERMINATED` - Contract already terminated
- `404 CONTRACT_NOT_FOUND` - Contract not found

---

### 7. 📝 Lấy danh sách mẫu hợp đồng - GET `/templates`

**Mô tả:** Lấy danh sách tất cả mẫu hợp đồng có sẵn.

**Role requirement:** CoOwner

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_TEMPLATES_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "templateType": "CoOwnershipAgreement",
      "templateName": "Thỏa thuận đồng sở hữu xe điện",
      "description": "Hợp đồng thiết lập quyền và nghĩa vụ của các bên đồng sở hữu xe điện",
      "category": "Ownership",
      "requiredFields": ["vehicleId", "signatoryUserIds"],
      "optionalFields": ["usageRatio", "costSharingMethod"],
      "estimatedDuration": "2-5 years",
      "complexity": "High",
      "usageCount": 156
    },
    {
      "templateType": "VehicleUsageAgreement",
      "templateName": "Thỏa thuận sử dụng xe",
      "description": "Quy định về cách sử dụng xe, thời gian, và trách nhiệm",
      "category": "Usage",
      "requiredFields": ["vehicleId", "usageSchedule"],
      "optionalFields": ["usageLimitations", "emergencyContacts"],
      "estimatedDuration": "6 months - 2 years",
      "complexity": "Medium",
      "usageCount": 89
    }
  ]
}
```

---

### 8. 📃 Lấy mẫu hợp đồng cụ thể - GET `/templates/{templateType}`

**Mô tả:** Lấy chi tiết mẫu hợp đồng cụ thể và nội dung template.

**Role requirement:** CoOwner

**Sample Request:**
```http
GET /api/contract/templates/CoOwnershipAgreement
Authorization: Bearer {token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "CONTRACT_TEMPLATE_RETRIEVED_SUCCESSFULLY",
  "data": {
    "templateType": "CoOwnershipAgreement",
    "templateName": "Thỏa thuận đồng sở hữu xe điện",
    "description": "Hợp đồng thiết lập quyền và nghĩa vụ của các bên đồng sở hữu xe điện",
    "category": "Ownership",
    "templateContent": "THỎA THUẬN ĐỒNG SỞ HỮU XE ĐIỆN\n\nĐiều 1: Các bên tham gia\n{{SIGNATORY_LIST}}\n\nĐiều 2: Xe điện\n- Loại xe: {{VEHICLE_NAME}}\n- Biển số: {{LICENSE_PLATE}}\n...",
    "placeholders": [
      {
        "key": "{{SIGNATORY_LIST}}",
        "description": "Danh sách người ký hợp đồng",
        "dataType": "string",
        "required": true
      },
      {
        "key": "{{VEHICLE_NAME}}",
        "description": "Tên xe",
        "dataType": "string",
        "required": true
      }
    ],
    "requiredFields": ["vehicleId", "signatoryUserIds"],
    "optionalFields": ["usageRatio", "costSharingMethod", "maintenanceResponsibility"],
    "legalNotes": "Hợp đồng này tuân thủ Luật Dân sự Việt Nam và các quy định về giao dịch điện tử",
    "estimatedDuration": "2-5 years",
    "complexity": "High",
    "lastUpdated": "2025-10-01T00:00:00Z"
  }
}
```

**Error Responses:**
- `400 INVALID_TEMPLATE_TYPE` - Template type not supported

---

### 9. 📥 Tải hợp đồng PDF - GET `/{contractId}/download`

**Mô tả:** Tải hợp đồng đã ký dưới dạng PDF để lưu trữ hoặc in ấn.

**Role requirement:** CoOwner (phải có quyền truy cập hợp đồng)

**Sample Request:**
```http
GET /api/contract/42/download
Authorization: Bearer {token}
```

**Response 200 - Thành công:**
- **Content-Type:** `application/pdf`
- **Content-Disposition:** `attachment; filename="contract-42.pdf"`
- **Body:** PDF file binary data

**Error Responses:**
- `403 ACCESS_DENIED` - User not authorized to download
- `404 CONTRACT_NOT_FOUND` - Contract not found

---

### 10. ⏳ Lấy hợp đồng chờ ký - GET `/pending-signature`

**Mô tả:** Lấy danh sách hợp đồng đang chờ chữ ký của người dùng hiện tại.

**Role requirement:** CoOwner

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "PENDING_CONTRACTS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "contractId": 42,
      "title": "2025 Tesla Model 3 Co-Ownership Agreement",
      "templateType": "CoOwnershipAgreement",
      "vehicleName": "Tesla Model 3 - 29A-12345",
      "creatorName": "John Doe",
      "createdAt": "2025-10-24T14:30:00Z",
      "signatureDeadline": "2025-11-15T23:59:59Z",
      "daysUntilDeadline": 22,
      "priority": "High",
      "totalSignatories": 3,
      "completedSignatures": 1,
      "pendingSignatures": 2
    }
  ],
  "totalPendingContracts": 3,
  "urgentContracts": 1
}
```

---

### 11. ✅ Lấy hợp đồng đã ký - GET `/signed`

**Mô tả:** Lấy danh sách hợp đồng đã ký bởi người dùng hiện tại.

**Role requirement:** CoOwner

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| vehicleId | integer | Lọc theo xe cụ thể |

**Sample Request:**
```http
GET /api/contract/signed?vehicleId=5
Authorization: Bearer {token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "SIGNED_CONTRACTS_RETRIEVED_SUCCESSFULLY",
  "data": [
    {
      "contractId": 42,
      "title": "2025 Tesla Model 3 Co-Ownership Agreement",
      "templateType": "CoOwnershipAgreement",
      "vehicleName": "Tesla Model 3 - 29A-12345",
      "contractStatus": "Active",
      "mySignedAt": "2025-10-24T15:00:00Z",
      "effectiveDate": "2025-11-01T00:00:00Z",
      "expiryDate": "2027-10-31T23:59:59Z",
      "isActive": true,
      "daysUntilExpiry": 738
    }
  ],
  "totalSignedContracts": 8,
  "activeContracts": 5,
  "expiredContracts": 2,
  "terminatedContracts": 1
}
```

---

## 🔢 Enums và Constants

### Contract Status (EContractStatus)
```typescript
enum EContractStatus {
  Draft = 0,              // Bản nháp
  PendingSignatures = 1,  // Chờ chữ ký
  PartiallySigned = 2,    // Một số người đã ký
  FullySigned = 3,        // Tất cả đã ký
  Active = 4,             // Đang hoạt động
  Expired = 5,            // Hết hạn
  Terminated = 6,         // Đã chấm dút
  Rejected = 7            // Bị từ chối
}
```

### Template Types
```typescript
enum EContractTemplateType {
  CoOwnershipAgreement = "CoOwnershipAgreement",         // Hợp đồng đồng sở hữu
  VehicleUsageAgreement = "VehicleUsageAgreement",       // Thỏa thuận sử dụng xe
  CostSharingAgreement = "CostSharingAgreement",         // Thỏa thuận chia sẻ chi phí
  MaintenanceAgreement = "MaintenanceAgreement"          // Hợp đồng bảo trì
}
```

### Signature Status
```typescript
enum ESignatureStatus {
  Pending = 0,    // Chờ ký
  Signed = 1,     // Đã ký
  Declined = 2    // Từ chối
}
```

---

## ❌ Error Codes

### Authentication Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 403 | Forbidden | `NOT_AUTHORIZED` | Không có quyền tạo hợp đồng |
| 403 | Forbidden | `ACCESS_DENIED` | Không có quyền truy cập hợp đồng |
| 403 | Forbidden | `NOT_A_SIGNATORY` | Không phải người ký được chỉ định |

### Validation Errors (400)
| Code | Ý nghĩa |
|------|---------|
| `INVALID_TEMPLATE_TYPE` | Loại mẫu hợp đồng không hợp lệ |
| `INVALID_SIGNATORY_USER_IDS` | Danh sách người ký không hợp lệ |
| `ALREADY_SIGNED` | Đã ký hợp đồng này rồi |
| `ALREADY_DECLINED` | Đã từ chối hợp đồng này rồi |
| `SIGNATURE_DEADLINE_EXPIRED` | Hạn chót ký đã qua |
| `CONTRACT_NOT_PENDING_SIGNATURES` | Hợp đồng không ở trạng thái chờ ký |
| `ALREADY_TERMINATED` | Hợp đồng đã bị chấm dút |

### Not Found Errors (404)
| Code | Ý nghĩa |
|------|---------|
| `CONTRACT_NOT_FOUND` | Không tìm thấy hợp đồng |
| `VEHICLE_NOT_FOUND` | Không tìm thấy xe |
| `TEMPLATE_NOT_FOUND` | Không tìm thấy mẫu hợp đồng |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Quy trình tạo và ký hợp đồng đồng sở hữu

```javascript
// 1. Lấy danh sách mẫu hợp đồng
const templatesResponse = await fetch('/api/contract/templates', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});
const templates = await templatesResponse.json();
console.log('Available templates:', templates.data);

// 2. Tạo hợp đồng đồng sở hữu mới
const createContractResponse = await fetch('/api/contract', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`
  },
  body: JSON.stringify({
    vehicleId: 5,
    templateType: 'CoOwnershipAgreement',
    title: '2025 Tesla Model 3 Co-Ownership Agreement',
    description: 'Co-ownership agreement for shared Tesla Model 3',
    signatoryUserIds: [15, 20, 25],
    effectiveDate: '2025-11-01T00:00:00Z',
    expiryDate: '2027-10-31T23:59:59Z',
    signatureDeadline: '2025-11-15T23:59:59Z',
    autoActivate: true,
    customTerms: {
      usageRatio: '40:30:30',
      costSharingMethod: 'proportional',
      maintenanceResponsibility: 'shared'
    }
  })
});

const contract = await createContractResponse.json();
console.log('Contract created:', contract.data.contractId);

// 3. Người ký thứ nhất ký hợp đồng
const signResponse = await fetch(`/api/contract/${contract.data.contractId}/sign`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}` // Token của người ký
  },
  body: JSON.stringify({
    signature: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
    ipAddress: '192.168.1.100',
    deviceInfo: 'Chrome 120.0 on Windows 11',
    agreementConfirmation: 'I agree to all terms and conditions'
  })
});

const signResult = await signResponse.json();
console.log('Signature status:', signResult.data.contractStatus);

// 4. Kiểm tra trạng thái hợp đồng
const contractDetailsResponse = await fetch(`/api/contract/${contract.data.contractId}`, {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const contractDetails = await contractDetailsResponse.json();
console.log('Contract status:', contractDetails.data.status);
console.log('Signatures completed:', contractDetails.data.signatories.filter(s => s.signatureStatus === 'Signed').length);
```

### Use Case 2: Quản lý hợp đồng chờ ký

```javascript
// 1. Lấy danh sách hợp đồng chờ ký
const pendingResponse = await fetch('/api/contract/pending-signature', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const pendingContracts = await pendingResponse.json();
console.log(`You have ${pendingContracts.data.length} contracts waiting for your signature`);

// 2. Hiển thị hợp đồng cần ký gấp
const urgentContracts = pendingContracts.data.filter(contract => 
  contract.daysUntilDeadline <= 7
);

urgentContracts.forEach(contract => {
  console.log(`URGENT: Contract "${contract.title}" expires in ${contract.daysUntilDeadline} days`);
});

// 3. Xem chi tiết hợp đồng trước khi ký
const contractId = pendingContracts.data[0].contractId;
const detailResponse = await fetch(`/api/contract/${contractId}`, {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const contractDetail = await detailResponse.json();
console.log('Contract content:', contractDetail.data.contractContent);

// 4. Ký hoặc từ chối
const userDecision = confirm('Do you want to sign this contract?');

if (userDecision) {
  // Ký hợp đồng
  const signResponse = await fetch(`/api/contract/${contractId}/sign`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      signature: generateSignature(), // Implement your signature generation
      agreementConfirmation: 'I agree to all terms and conditions'
    })
  });
  
  if (signResponse.ok) {
    console.log('Contract signed successfully!');
  }
} else {
  // Từ chối ký
  const reason = prompt('Please provide reason for declining:');
  const declineResponse = await fetch(`/api/contract/${contractId}/decline`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      reason: reason,
      suggestedChanges: 'Please clarify the cost sharing methodology'
    })
  });
  
  if (declineResponse.ok) {
    console.log('Contract declined successfully');
  }
}
```

### Use Case 3: Tải và lưu trữ hợp đồng

```javascript
// 1. Lấy danh sách hợp đồng đã ký
const signedResponse = await fetch('/api/contract/signed', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const signedContracts = await signedResponse.json();

// 2. Tải hợp đồng PDF cho lưu trữ
signedContracts.data.forEach(async (contract) => {
  if (contract.isActive) {
    const downloadResponse = await fetch(`/api/contract/${contract.contractId}/download`, {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });
    
    if (downloadResponse.ok) {
      const blob = await downloadResponse.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `contract-${contract.contractId}-${contract.title}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      
      console.log(`Downloaded contract: ${contract.title}`);
    }
  }
});
```

### Use Case 4: Chấm dút hợp đồng sớm

```javascript
// 1. Lấy hợp đồng đang hoạt động
const activeContractsResponse = await fetch('/api/contract?status=Active', {
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const activeContracts = await activeContractsResponse.json();

// 2. Chấm dút hợp đồng (chỉ người tạo hoặc admin)
const contractToTerminate = activeContracts.data[0];
const terminateResponse = await fetch(`/api/contract/${contractToTerminate.contractId}/terminate`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`
  },
  body: JSON.stringify({
    reason: 'Vehicle has been sold. All co-owners agreed to terminate the agreement.',
    effectiveDate: '2025-12-31T23:59:59Z',
    notes: 'Final settlement completed. All financial obligations cleared.'
  })
});

if (terminateResponse.ok) {
  const terminateResult = await terminateResponse.json();
  console.log('Contract terminated successfully:', terminateResult.data);
} else {
  const error = await terminateResponse.json();
  console.error('Failed to terminate contract:', error.message);
}
```

---

## 🔐 Best Practices

### 1. Bảo mật chữ ký điện tử

```javascript
// Tạo chữ ký an toàn với hash
function generateSecureSignature(contractContent, userPrivateKey) {
  const combinedData = contractContent + userPrivateKey + Date.now();
  return CryptoJS.SHA256(combinedData).toString();
}

// Sử dụng khi ký hợp đồng
const signature = generateSecureSignature(contractDetail.data.contractContent, userPrivateKey);
```

### 2. Xử lý lỗi và validation

```javascript
async function signContract(contractId, signatureData) {
  try {
    const response = await fetch(`/api/contract/${contractId}/sign`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify(signatureData)
    });

    const result = await response.json();

    switch(result.statusCode) {
      case 200:
        return { success: true, message: 'Hợp đồng đã được ký thành công', data: result.data };
      
      case 400:
        if (result.message === 'ALREADY_SIGNED') {
          return { success: false, error: 'Bạn đã ký hợp đồng này rồi' };
        } else if (result.message === 'SIGNATURE_DEADLINE_EXPIRED') {
          return { success: false, error: 'Hạn chót ký hợp đồng đã qua' };
        }
        break;
      
      case 403:
        return { success: false, error: 'Bạn không có quyền ký hợp đồng này' };
      
      case 404:
        return { success: false, error: 'Không tìm thấy hợp đồng' };
      
      default:
        return { success: false, error: 'Đã xảy ra lỗi không xác định' };
    }
  } catch (error) {
    console.error('Network error:', error);
    return { success: false, error: 'Không thể kết nối đến server' };
  }
}
```

### 3. Theo dõi tiến độ ký hợp đồng

```javascript
// Thiết lập polling để theo dõi trạng thái hợp đồng
function startContractStatusPolling(contractId, callback) {
  const pollInterval = setInterval(async () => {
    try {
      const response = await fetch(`/api/contract/${contractId}`, {
        headers: { 'Authorization': `Bearer ${accessToken}` }
      });
      
      if (response.ok) {
        const contract = await response.json();
        callback(contract.data);
        
        // Dừng polling khi hợp đồng hoàn tất
        if (['FullySigned', 'Active', 'Rejected', 'Terminated'].includes(contract.data.status)) {
          clearInterval(pollInterval);
        }
      }
    } catch (error) {
      console.error('Error polling contract status:', error);
    }
  }, 30000); // Kiểm tra mỗi 30 giây

  return pollInterval;
}

// Sử dụng
const pollInterval = startContractStatusPolling(contractId, (contract) => {
  updateContractStatusUI(contract);
  
  if (contract.status === 'FullySigned') {
    showNotification('Hợp đồng đã được ký đầy đủ và kích hoạt!');
  }
});
```

---

## 📞 Liên hệ và Hỗ trợ

- **API Documentation:** http://localhost:5215/swagger
- **Backend Team:** [Your team contact]
- **Issues:** [GitHub Issues URL]

---

**Last Updated:** 2025-10-24  
**Version:** 1.0.0  
**Author:** Backend Development Team