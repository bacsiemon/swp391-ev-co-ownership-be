# BookingReminder API Documentation

## Tổng quan
Module BookingReminder API cung cấp các endpoint để quản lý nhắc nhở đặt xe. Các chức năng bao gồm cấu hình nhắc nhở, lấy thông tin nhắc nhở, gửi nhắc nhở thủ công, và thống kê.

---

## Danh sách Endpoint

### 1. Cấu hình nhắc nhở đặt xe
**URL:** `POST /api/booking-reminder/configure`

**Mô tả:**
- Cấu hình thời gian và trạng thái nhắc nhở đặt xe cho người dùng hiện tại.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Request Body:**
```json
{
    "hoursBeforeBooking": 24,
    "enabled": true
}
```

**Response:**
- `200 OK`: Cấu hình thành công.
- `400 Bad Request`: Giá trị không hợp lệ.
- `401 Unauthorized`: Token không hợp lệ.
- `404 Not Found`: Người dùng không tồn tại.

---

### 2. Lấy thông tin nhắc nhở đặt xe
**URL:** `GET /api/booking-reminder/preferences`

**Mô tả:**
- Lấy thông tin cấu hình nhắc nhở của người dùng hiện tại.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Response:**
- `200 OK`: Trả về thông tin cấu hình nhắc nhở.
- `401 Unauthorized`: Token không hợp lệ.
- `404 Not Found`: Người dùng không tồn tại.

**Example Response:**
```json
{
    "statusCode": 200,
    "message": "Reminder preferences retrieved successfully",
    "data": {
        "userId": 5,
        "hoursBeforeBooking": 24,
        "enabled": true,
        "updatedAt": "2025-10-23T10:30:00Z"
    }
}
```

---

### 3. Lấy danh sách đặt xe sắp tới
**URL:** `GET /api/booking-reminder/upcoming`

**Mô tả:**
- Lấy danh sách các đặt xe sắp tới cùng thông tin nhắc nhở.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Query Parameters:**
- `daysAhead` (mặc định: 7, tối đa: 30)

**Response:**
- `200 OK`: Trả về danh sách đặt xe sắp tới.
- `400 Bad Request`: Giá trị `daysAhead` không hợp lệ.
- `401 Unauthorized`: Token không hợp lệ.
- `404 Not Found`: Người dùng không tồn tại.

**Example Response:**
```json
{
    "statusCode": 200,
    "message": "Found 3 upcoming bookings",
    "data": {
        "userId": 5,
        "totalUpcomingBookings": 3,
        "upcomingBookings": [
            {
                "bookingId": 101,
                "vehicleId": 5,
                "vehicleName": "VinFast VF8",
                "licensePlate": "30A-12345",
                "startTime": "2025-10-24T08:00:00Z",
                "endTime": "2025-10-24T18:00:00Z",
                "purpose": "Đi công tác",
                "hoursUntilStart": 21.5,
                "reminderSent": true,
                "reminderSentAt": "2025-10-23T11:00:00Z"
            }
        ]
    }
}
```

---

### 4. Gửi nhắc nhở thủ công
**URL:** `POST /api/booking-reminder/send/{bookingId}`

**Mô tả:**
- Gửi nhắc nhở thủ công cho một đặt xe cụ thể.

**Yêu cầu:**
- Người dùng phải đăng nhập.

**Response:**
- `200 OK`: Nhắc nhở được gửi thành công.
- `400 Bad Request`: Không thể gửi nhắc nhở cho đặt xe đã qua.
- `401 Unauthorized`: Token không hợp lệ.
- `403 Forbidden`: Người dùng không có quyền truy cập đặt xe này.
- `404 Not Found`: Đặt xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

**Example Response:**
```json
{
    "statusCode": 200,
    "message": "Reminder sent successfully",
    "data": true
}
```

---

### 5. Lấy thống kê nhắc nhở đặt xe
**URL:** `GET /api/booking-reminder/statistics`

**Mô tả:**
- Lấy thống kê hệ thống về nhắc nhở đặt xe (chỉ dành cho Admin).

**Yêu cầu:**
- Người dùng phải có quyền Admin.

**Response:**
- `200 OK`: Trả về thống kê nhắc nhở.
- `401 Unauthorized`: Token không hợp lệ.
- `403 Forbidden`: Yêu cầu quyền Admin.
- `500 Internal Server Error`: Lỗi hệ thống.

**Example Response:**
```json
{
    "statusCode": 200,
    "message": "Reminder statistics retrieved successfully",
    "data": {
        "totalRemindersScheduled": 45,
        "remindersSentToday": 12,
        "remindersScheduledNext24Hours": 8,
        "remindersScheduledNext7Days": 32,
        "usersWithRemindersEnabled": 15,
        "lastReminderSentAt": "2025-10-23T11:45:00Z",
        "statisticsGeneratedAt": "2025-10-23T12:00:00Z"
    }
}
```

---

## Lưu ý
- Tất cả các endpoint yêu cầu xác thực bằng JWT.
- Đảm bảo rằng các tham số đầu vào hợp lệ trước khi gửi request.