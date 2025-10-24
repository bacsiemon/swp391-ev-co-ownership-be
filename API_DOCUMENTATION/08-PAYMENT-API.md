# Payment API Documentation

## Tổng quan
API Payment quản lý các chức năng liên quan đến thanh toán, bao gồm tạo thanh toán, xử lý callback, hủy thanh toán, và thống kê. API hỗ trợ nhiều cổng thanh toán như VNPay, Momo, ZaloPay.

---

## Danh sách Endpoint

### 1. Tạo thanh toán
- **URL:** `POST /api/payment`
- **Vai trò yêu cầu:** CoOwner
- **Request Body:**
```json
{
  "amount": 500000,
  "paymentGateway": 0,
  "paymentMethod": 1,
  "paymentType": 0,
  "bookingId": 123,
  "description": "Payment for vehicle booking"
}
```
- **Response:**
```json
{
  "statusCode": 201,
  "message": "PAYMENT_CREATED",
  "data": {
    "paymentUrl": "https://sandbox.vnpayment.vn/payment/12345"
  }
}
```
- **Mô tả:** Tạo một thanh toán mới và trả về URL thanh toán.

---

### 2. Xử lý callback từ cổng thanh toán
- **URL:** `POST /api/payment/process`
- **Request Body:**
```json
{
  "paymentId": 123,
  "transactionId": "TXN12345",
  "isSuccess": true
}
```
- **Response:**
```json
{
  "statusCode": 200,
  "message": "PAYMENT_PROCESSED",
  "data": {
    "paymentId": 123,
    "status": "Success"
  }
}
```
- **Mô tả:** Xử lý callback từ cổng thanh toán và cập nhật trạng thái thanh toán.

---

### 3. Lấy thông tin thanh toán theo ID
- **URL:** `GET /api/payment/{id}`
- **Response:**
```json
{
  "statusCode": 200,
  "message": "PAYMENT_RETRIEVED",
  "data": {
    "paymentId": 123,
    "amount": 500000,
    "status": "Success",
    "createdAt": "2025-10-24T10:00:00"
  }
}
```
- **Mô tả:** Lấy thông tin chi tiết của một thanh toán theo ID.

---

### 4. Lấy danh sách thanh toán của người dùng hiện tại
- **URL:** `GET /api/payment/my-payments`
- **Query Parameters:**
  - `pageIndex` (mặc định: 1)
  - `pageSize` (mặc định: 10)
- **Response:**
```json
{
  "statusCode": 200,
  "message": "MY_PAYMENTS_RETRIEVED",
  "data": [
    {
      "paymentId": 123,
      "amount": 500000,
      "status": "Success",
      "createdAt": "2025-10-24T10:00:00"
    }
  ]
}
```
- **Mô tả:** Lấy danh sách các thanh toán của người dùng hiện tại.

---

### 5. Hủy thanh toán
- **URL:** `POST /api/payment/{id}/cancel`
- **Response:**
```json
{
  "statusCode": 200,
  "message": "PAYMENT_CANCELLED",
  "data": {
    "paymentId": 123,
    "status": "Cancelled"
  }
}
```
- **Mô tả:** Hủy một thanh toán đang chờ xử lý.

---

### 6. Lấy danh sách cổng thanh toán khả dụng
- **URL:** `GET /api/payment/gateways`
- **Response:**
```json
{
  "statusCode": 200,
  "message": "GATEWAYS_RETRIEVED",
  "data": [
    {
      "gateway": "VNPay",
      "description": "Hỗ trợ thanh toán qua ngân hàng, thẻ tín dụng",
      "status": "Available"
    }
  ]
}
```
- **Mô tả:** Lấy danh sách các cổng thanh toán khả dụng.

---

### 7. Lấy tất cả thanh toán (Admin/Staff)
- **URL:** `GET /api/payment`
- **Vai trò yêu cầu:** Admin, Staff
- **Query Parameters:**
  - `pageIndex` (mặc định: 1)
  - `pageSize` (mặc định: 10)
- **Response:**
```json
{
  "statusCode": 200,
  "message": "ALL_PAYMENTS_RETRIEVED",
  "data": [
    {
      "paymentId": 123,
      "userId": 45,
      "userEmail": "user@example.com",
      "amount": 500000,
      "status": "Success",
      "paymentGateway": "VNPay",
      "createdAt": "2025-10-24T10:00:00"
    }
  ]
}
```
- **Mô tả:** Admin/Staff lấy danh sách tất cả thanh toán trong hệ thống.

---

### 8. Lấy thống kê thanh toán (Admin/Staff)
- **URL:** `GET /api/payment/statistics`
- **Vai trò yêu cầu:** Admin, Staff
- **Response:**
```json
{
  "statusCode": 200,
  "message": "PAYMENT_STATISTICS_RETRIEVED",
  "data": {
    "totalPayments": 1250,
    "totalAmount": 625000000,
    "successfulPayments": 1100,
    "failedPayments": 150,
    "successRate": 88.0,
    "gatewayStats": [
      {
        "gateway": "VNPay",
        "totalPayments": 800,
        "totalAmount": 400000000,
        "successRate": 92.5
      }
    ],
    "monthlyStats": [
      {
        "month": "2025-10",
        "totalPayments": 120,
        "totalAmount": 60000000
      }
    ]
  }
}
```
- **Mô tả:** Lấy thống kê tổng quan về thanh toán.

---

### 9. VNPay Callback (Webhook)
- **URL:** `GET /api/payment/vnpay-callback`
- **Mô tả:** Endpoint callback từ VNPay sau khi người dùng hoàn tất thanh toán.
- **Không yêu cầu authentication** - VNPay không thể gửi JWT token.
- **Query Parameters:** Các tham số được VNPay gửi về
- **Response:** Redirect người dùng về trang frontend với kết quả thanh toán

---

## Best Practices
- **Validation:** Đảm bảo các trường bắt buộc được cung cấp trong request.
- **Error Handling:** Xử lý các mã lỗi trả về như 400, 401, 403, 404.
- **Security:** Chỉ cho phép các vai trò phù hợp truy cập các endpoint tương ứng.

---

## Changelog
- **24/10/2025:** Tạo tài liệu API Payment.