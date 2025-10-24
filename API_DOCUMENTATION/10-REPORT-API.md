# Report API Documentation

## Tổng quan
Module Report API cung cấp các endpoint để tạo báo cáo chi tiết về việc sử dụng và chi phí của xe. Các báo cáo có thể được tạo theo tháng, quý, năm hoặc xuất ra định dạng PDF/Excel. Ngoài ra, module còn hỗ trợ lấy danh sách các khoảng thời gian có sẵn để tạo báo cáo.

---

## Danh sách Endpoint

### 1. Tạo báo cáo hàng tháng
**URL:** `POST /api/reports/monthly`

**Mô tả:**
- Tạo báo cáo chi tiết hàng tháng bao gồm thống kê sử dụng, chi phí, bảo trì và trạng thái quỹ.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "vehicleId": 1,
    "year": 2025,
    "month": 10
}
```

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `400 Bad Request`: Giá trị tháng không hợp lệ (1-12).
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 2. Tạo báo cáo hàng quý
**URL:** `POST /api/reports/quarterly`

**Mô tả:**
- Tạo báo cáo chi tiết hàng quý bao gồm thống kê sử dụng, chi phí và xu hướng theo tháng.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "vehicleId": 1,
    "year": 2025,
    "quarter": 4
}
```

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `400 Bad Request`: Giá trị quý không hợp lệ (1-4).
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 3. Tạo báo cáo hàng năm
**URL:** `POST /api/reports/yearly`

**Mô tả:**
- Tạo báo cáo chi tiết hàng năm bao gồm thống kê sử dụng, chi phí và phân tích theo quý.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "vehicleId": 1,
    "year": 2025
}
```

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 4. Xuất báo cáo (PDF/Excel)
**URL:** `POST /api/reports/export`

**Mô tả:**
- Xuất báo cáo dưới dạng PDF hoặc Excel.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Request Body:**
```json
{
    "vehicleId": 1,
    "year": 2025,
    "month": 10,
    "exportFormat": "PDF"
}
```

**Response:**
- `200 OK`: Báo cáo được xuất thành công (trả về file).
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 5. Lấy danh sách khoảng thời gian có sẵn
**URL:** `GET /api/reports/vehicle/{vehicleId}/available-periods`

**Mô tả:**
- Lấy danh sách các tháng có dữ liệu để tạo báo cáo.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Danh sách được trả về thành công.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 6. Báo cáo tháng hiện tại
**URL:** `GET /api/reports/vehicle/{vehicleId}/current-month`

**Mô tả:**
- Tạo báo cáo cho tháng hiện tại.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 7. Báo cáo quý hiện tại
**URL:** `GET /api/reports/vehicle/{vehicleId}/current-quarter`

**Mô tả:**
- Tạo báo cáo cho quý hiện tại.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

### 8. Báo cáo năm hiện tại
**URL:** `GET /api/reports/vehicle/{vehicleId}/current-year`

**Mô tả:**
- Tạo báo cáo cho năm hiện tại.

**Yêu cầu:**
- Người dùng phải là đồng sở hữu của xe.

**Response:**
- `200 OK`: Báo cáo được tạo thành công.
- `403 Forbidden`: Người dùng không phải đồng sở hữu của xe.
- `404 Not Found`: Xe không tồn tại.
- `500 Internal Server Error`: Lỗi hệ thống.

---

## Lưu ý
- Tất cả các endpoint yêu cầu người dùng phải xác thực bằng JWT.
- Đảm bảo rằng các tham số đầu vào hợp lệ trước khi gửi request.