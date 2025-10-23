# Profile Page API Documentation

## Tổng quan
Chức năng Profile Page cho phép người dùng quản lý thông tin cá nhân, xem tóm tắt hoạt động và thống kê liên quan đến tài khoản của họ trong hệ thống EV Co-ownership.

## Cấu trúc API

### Base URL
```
/api/profile
```

### Authentication
Tất cả endpoints đều yêu cầu JWT authentication với header:
```
Authorization: Bearer {token}
```

## Endpoints

### 1. Lấy thông tin Profile
```http
GET /api/profile
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy thông tin profile thành công",
  "data": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "Nguyễn",
    "lastName": "Văn A",
    "phone": "0909123456",
    "dateOfBirth": "1990-01-01T00:00:00Z",
    "address": "123 Đường ABC, Quận 1, TP.HCM",
    "profileImageUrl": "https://example.com/images/profile.jpg",
    "role": "CoOwner",
    "status": "Active",
    "profileCompleteness": 85.5,
    "totalVehicles": 2,
    "totalBookings": 15,
    "totalPayments": 8,
    "totalAmountPaid": 2500000.0,
    "memberSince": "2023-01-15T00:00:00Z",
    "lastLoginAt": "2024-01-16T10:30:00Z"
  }
}
```

### 2. Cập nhật thông tin Profile
```http
PUT /api/profile
```

**Request Body:**
```json
{
  "firstName": "Nguyễn",
  "lastName": "Văn B",
  "phone": "0909876543",
  "dateOfBirth": "1991-01-01T00:00:00Z",
  "address": "456 Đường XYZ, Quận 2, TP.HCM"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Cập nhật profile thành công",
  "data": {
    // Updated profile data
  }
}
```

### 3. Đổi mật khẩu
```http
PUT /api/profile/change-password
```

**Request Body:**
```json
{
  "currentPassword": "currentPassword123",
  "newPassword": "newPassword456",
  "confirmPassword": "newPassword456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Đổi mật khẩu thành công"
}
```

### 4. Lấy tóm tắt phương tiện
```http
GET /api/profile/vehicles-summary
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy tóm tắt phương tiện thành công",
  "data": {
    "totalVehicles": 2,
    "totalShares": 150000000.0,
    "avgUtilization": 75.5,
    "vehicles": [
      {
        "id": 1,
        "licensePlate": "51A-12345",
        "make": "VinFast",
        "model": "VF8",
        "year": 2023,
        "sharePercentage": 60.0,
        "shareValue": 90000000.0,
        "monthlyUsageHours": 45.5,
        "status": "Active"
      }
    ]
  }
}
```

### 5. Lấy tóm tắt hoạt động
```http
GET /api/profile/activity-summary
```

**Response:**
```json
{
  "success": true,
  "message": "Lấy tóm tắt hoạt động thành công",
  "data": {
    "totalBookings": 15,
    "totalPayments": 8,
    "totalAmountPaid": 2500000.0,
    "avgBookingDuration": 3.5,
    "monthlyStats": {
      "bookingsThisMonth": 3,
      "paymentsThisMonth": 2,
      "amountPaidThisMonth": 500000.0
    },
    "recentBookings": [
      {
        "id": 1,
        "vehicleLicensePlate": "51A-12345",
        "startTime": "2024-01-15T09:00:00Z",
        "endTime": "2024-01-15T17:00:00Z",
        "purpose": "Công việc",
        "status": "Completed",
        "totalCost": 300000.0
      }
    ],
    "recentPayments": [
      {
        "id": 1,
        "amount": 300000.0,
        "paymentGateway": "VNPay",
        "status": "Completed",
        "paidAt": "2024-01-15T17:30:00Z",
        "transactionId": "TXN123456"
      }
    ]
  }
}
```

### 6. Upload ảnh đại diện
```http
POST /api/profile/upload-profile-image
```

**Request:** Form-data
- `file`: Image file (JPG, PNG, max 5MB)

**Response:**
```json
{
  "success": true,
  "message": "Upload ảnh đại diện thành công",
  "data": {
    "profileImageUrl": "https://example.com/images/new-profile.jpg"
  }
}
```

### 7. Xóa ảnh đại diện
```http
DELETE /api/profile/profile-image
```

**Response:**
```json
{
  "success": true,
  "message": "Xóa ảnh đại diện thành công"
}
```

### 8. Kiểm tra tính đầy đủ của profile
```http
GET /api/profile/completeness
```

**Response:**
```json
{
  "success": true,
  "message": "Kiểm tra tính đầy đủ profile thành công",
  "data": {
    "completeness": 85.5,
    "missingFields": ["profileImageUrl"],
    "suggestions": [
      "Thêm ảnh đại diện để hoàn thiện profile"
    ]
  }
}
```

## Validation Rules

### Thông tin cá nhân
- **FirstName**: Bắt buộc, 1-50 ký tự, chỉ chữ cái và khoảng trắng
- **LastName**: Bắt buộc, 1-50 ký tự, chỉ chữ cái và khoảng trắng
- **Phone**: Định dạng số điện thoại Việt Nam (0XXXXXXXXX)
- **DateOfBirth**: Phải đủ 18 tuổi
- **Address**: Tối đa 200 ký tự

### Đổi mật khẩu
- **CurrentPassword**: Bắt buộc
- **NewPassword**: Tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt
- **ConfirmPassword**: Phải khớp với NewPassword

### Upload ảnh
- **Định dạng**: JPG, JPEG, PNG
- **Kích thước**: Tối đa 5MB
- **Kích thước ảnh**: Tối thiểu 100x100px, tối đa 2000x2000px

## Error Codes

| Code | Message | Description |
|------|---------|-------------|
| 400 | "Dữ liệu không hợp lệ" | Validation failed |
| 401 | "Không có quyền truy cập" | Unauthorized |
| 404 | "Không tìm thấy người dùng" | User not found |
| 409 | "Số điện thoại đã được sử dụng" | Phone already exists |
| 422 | "Mật khẩu hiện tại không đúng" | Invalid current password |
| 500 | "Lỗi hệ thống" | Internal server error |

## Security Features

### Rate Limiting
- Profile updates: 10 requests/hour
- Password changes: 5 requests/hour
- Image uploads: 20 requests/hour

### Data Privacy
- Sensitive information được mã hóa
- Audit logs cho tất cả thay đổi profile
- GDPR compliance cho việc xóa dữ liệu

### File Security
- Virus scanning cho uploaded files
- Secure file storage với signed URLs
- Automatic image optimization và compression

## Implementation Details

### Architecture
```
ProfileController → IUserProfileService → IUnitOfWork → Repositories
```

### Dependencies
- **IUserProfileService**: Business logic layer
- **IFileUploadService**: File handling service
- **IUnitOfWork**: Data access abstraction
- **FluentValidation**: Input validation
- **JWT Authentication**: Security

### Database Queries Optimization
- Sử dụng Include() cho related data
- Pagination cho large datasets
- Caching cho frequently accessed data
- Database indexing trên user-related fields

## Testing

### Unit Tests
- Service layer business logic
- Validation rules
- Error handling scenarios

### Integration Tests
- End-to-end API testing
- Database operations
- File upload functionality

### Performance Tests
- Response time < 200ms
- Concurrent user handling
- Memory usage optimization

## Monitoring & Analytics

### Metrics Tracked
- Profile completion rates
- Feature usage statistics
- Error rates và response times
- User engagement patterns

### Logging
- All profile changes được logged
- Security events tracking
- Performance monitoring
- Error tracking và alerting