# Notification API Documentation

## Tổng quan
Module Notification API cung cấp các endpoint để quản lý thông báo cho người dùng. Các chức năng bao gồm lấy danh sách thông báo, đánh dấu thông báo đã đọc, và gửi thông báo thủ công.

---

## Danh sách Endpoint

### 1. Lấy danh sách thông báo của người dùng
**URL:** `GET /api/notification/my-notifications`

**Mô tả:**
- Lấy danh sách thông báo của người dùng hiện tại với phân trang.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Query Parameters:**
- `pageIndex` (int, optional): Số trang (mặc định: 1).
- `pageSize` (int, optional): Số lượng thông báo mỗi trang (mặc định: 10, tối đa: 50).
- `includeRead` (bool, optional): Bao gồm thông báo đã đọc (mặc định: true).

**Response:**
- `200 OK`: Trả về danh sách thông báo.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `400 Bad Request`: Tham số không hợp lệ.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 2. Lấy số lượng thông báo chưa đọc
**URL:** `GET /api/notification/unread-count`

**Mô tả:**
- Lấy số lượng thông báo chưa đọc của người dùng hiện tại.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Response:**
- `200 OK`: Trả về số lượng thông báo chưa đọc.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `404 Not Found`: Người dùng không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 3. Đánh dấu thông báo là đã đọc
**URL:** `PUT /api/notification/mark-read`

**Mô tả:**
- Đánh dấu một thông báo cụ thể là đã đọc.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Request Body:**
```json
{
    "userNotificationId": 123
}
```

**Response:**
- `200 OK`: Thông báo được đánh dấu thành công.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `404 Not Found`: Thông báo không tồn tại hoặc không thuộc về người dùng.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 4. Đánh dấu nhiều thông báo là đã đọc
**URL:** `PUT /api/notification/mark-multiple-read`

**Mô tả:**
- Đánh dấu nhiều thông báo là đã đọc.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Request Body:**
```json
{
    "userNotificationIds": [1, 2, 3, 4, 5]
}
```

**Response:**
- `200 OK`: Trả về số lượng thông báo được đánh dấu thành công.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `400 Bad Request`: Tham số không hợp lệ.
- `404 Not Found`: Không tìm thấy thông báo hợp lệ.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 5. Đánh dấu tất cả thông báo là đã đọc
**URL:** `PUT /api/notification/mark-all-read`

**Mô tả:**
- Đánh dấu tất cả thông báo chưa đọc là đã đọc.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Response:**
- `200 OK`: Trả về số lượng thông báo được đánh dấu thành công.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `404 Not Found`: Người dùng không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 6. Gửi thông báo thủ công đến người dùng
**URL:** `POST /api/notification/send-to-user`

**Mô tả:**
- Gửi thông báo thủ công đến một người dùng cụ thể (chỉ dành cho Admin).

**Yêu cầu:**
- Người dùng phải có quyền Admin.

**Request Body:**
```json
{
    "userId": 123,
    "notificationType": "Booking",
    "additionalData": "{\"bookingId\": 456, \"vehicleId\": 789}"
}
```

**Response:**
- `200 OK`: Thông báo được gửi thành công.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `403 Forbidden`: Yêu cầu quyền Admin.
- `400 Bad Request`: Dữ liệu không hợp lệ.
- `404 Not Found`: Người dùng không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 7. Tạo và gửi thông báo đến nhiều người dùng
**URL:** `POST /api/notification/create-notification`

**Mô tả:**
- Tạo và gửi thông báo thủ công đến nhiều người dùng (chỉ dành cho Admin).

**Yêu cầu:**
- Người dùng phải có quyền Admin.

**Request Body:**
```json
{
    "notificationType": "System",
    "userIds": [1, 2, 3, 4, 5],
    "additionalData": "{\"maintenanceWindow\": \"2025-10-15T02:00:00Z\"}"
}
```

**Response:**
- `200 OK`: Thông báo được tạo và gửi thành công.
- `401 Unauthorized`: Token không hợp lệ hoặc thiếu.
- `403 Forbidden`: Yêu cầu quyền Admin.
- `400 Bad Request`: Dữ liệu không hợp lệ.
- `500 Internal Server Error`: Lỗi hệ thống.

---

## Lưu ý
- Tất cả các endpoint yêu cầu xác thực bằng JWT.
- Đảm bảo rằng các tham số đầu vào hợp lệ trước khi gửi request.