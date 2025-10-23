# Voting API Documentation

## Tổng quan
Module Voting API cung cấp các endpoint để quản lý các đề xuất nâng cấp xe và hệ thống bỏ phiếu. Các chức năng bao gồm tạo đề xuất, bỏ phiếu, lấy lịch sử bỏ phiếu, và thống kê.

---

## Danh sách Endpoint

### 1. Tạo đề xuất nâng cấp xe
**URL:** `POST /api/upgrade-vote/propose`

**Mô tả:**
- Tạo một đề xuất nâng cấp xe mới.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "upgradeType": 0,
    "title": "Battery Upgrade to 100kWh",
    "description": "Upgrade to new generation battery for extended range",
    "estimatedCost": 15000.00,
    "justification": "Current battery capacity degraded by 20%",
    "imageUrl": "https://example.com/battery-specs.jpg",
    "vendorName": "Tesla Battery Solutions",
    "vendorContact": "+1-555-0123",
    "proposedInstallationDate": "2024-12-01T00:00:00Z",
    "estimatedDurationDays": 3
}
```

**Response:**
- `201 Created`: Đề xuất được tạo thành công.
- `400 Bad Request`: Dữ liệu không hợp lệ.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 2. Bỏ phiếu cho đề xuất nâng cấp
**URL:** `POST /api/upgrade-vote/{proposalId}/vote`

**Mô tả:**
- Bỏ phiếu chấp thuận hoặc từ chối một đề xuất nâng cấp.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "isApprove": true,
    "comments": "Great idea! This will significantly improve our vehicle's performance"
}
```

**Response:**
- `200 OK`: Bỏ phiếu thành công.
- `400 Bad Request`: Đã bỏ phiếu hoặc trạng thái đề xuất không cho phép bỏ phiếu.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Đề xuất không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 3. Lấy chi tiết đề xuất nâng cấp
**URL:** `GET /api/upgrade-vote/{proposalId}`

**Mô tả:**
- Lấy thông tin chi tiết về một đề xuất nâng cấp.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Trả về chi tiết đề xuất.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Đề xuất không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 4. Lấy danh sách đề xuất đang chờ xử lý
**URL:** `GET /api/upgrade-vote/vehicle/{vehicleId}/pending`

**Mô tả:**
- Lấy danh sách các đề xuất nâng cấp đang chờ xử lý cho một xe cụ thể.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Trả về danh sách đề xuất đang chờ xử lý.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 5. Lấy lịch sử bỏ phiếu của người dùng
**URL:** `GET /api/upgrade-vote/my-history`

**Mô tả:**
- Lấy lịch sử bỏ phiếu của người dùng hiện tại.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Response:**
- `200 OK`: Trả về lịch sử bỏ phiếu.
- `404 Not Found`: Người dùng không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

## Lưu ý
- Tất cả các endpoint yêu cầu xác thực bằng JWT.
- Đảm bảo rằng các tham số đầu vào hợp lệ trước khi gửi request.