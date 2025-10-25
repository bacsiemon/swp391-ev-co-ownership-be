# VehicleReport API Documentation

## Tổng quan
Module `VehicleReport API` cung cấp các endpoint để tạo và xuất báo cáo sử dụng, chi phí và thống kê xe điện. Đây là module quan trọng giúp các chủ sở hữu chung theo dõi hiệu quả sử dụng và tài chính của xe.

### Base URL
```
/api/reports
```

### Authentication
Tất cả endpoints yêu cầu Bearer token trong header:
```
Authorization: Bearer {your_jwt_token}
```

---

## Endpoints

### 1. Tạo Báo Cáo Tháng
**[User - Co-Owner]**

- **Mô tả:**
  Tạo báo cáo tổng hợp sử dụng và chi phí cho một tháng cụ thể, bao gồm thống kê đặt xe, chi phí, bảo dưỡng và tình trạng quỹ.

- **Endpoint:**
  ```
  POST /monthly
  ```

- **Request Body:**
  ```json
  {
    "vehicleId": 1,
    "year": 2025,
    "month": 10
  }
  ```

- **Parameters:**
  | Tên         | Kiểu    | Bắt buộc | Mô tả                                           |
  |-------------|---------|----------|-------------------------------------------------|
  | `vehicleId` | `int`   | Có       | ID của xe cần tạo báo cáo                      |
  | `year`      | `int`   | Có       | Năm báo cáo (VD: 2025)                         |
  | `month`     | `int`   | Có       | Tháng báo cáo (1-12)                           |

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Báo cáo tháng được tạo thành công",
    "data": {
      "reportId": "rpt_monthly_2025_10_001",
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "licensePlate": "30A-12345",
      "reportPeriod": {
        "year": 2025,
        "month": 10,
        "monthName": "Tháng 10",
        "startDate": "2025-10-01T00:00:00Z",
        "endDate": "2025-10-31T23:59:59Z",
        "totalDays": 31
      },
      "usageStatistics": {
        "totalBookings": 25,
        "completedBookings": 23,
        "cancelledBookings": 2,
        "totalHours": 180.5,
        "totalKilometers": 1250,
        "averageBookingDuration": 7.2,
        "averageTripDistance": 54.3,
        "utilizationRate": 24.2,
        "mostActiveDay": "Thứ 7",
        "mostActiveTimeSlot": "Sáng"
      },
      "costBreakdown": {
        "totalIncome": 15000000,
        "totalExpenses": 8500000,
        "netProfit": 6500000,
        "expenses": [
          {
            "category": "Bảo dưỡng",
            "amount": 3000000,
            "percentage": 35.3
          },
          {
            "category": "Nhiên liệu/Điện",
            "amount": 2500000,
            "percentage": 29.4
          },
          {
            "category": "Bảo hiểm",
            "amount": 2000000,
            "percentage": 23.5
          },
          {
            "category": "Khác",
            "amount": 1000000,
            "percentage": 11.8
          }
        ]
      },
      "maintenanceSummary": {
        "scheduledMaintenances": 2,
        "emergencyRepairs": 1,
        "totalMaintenanceCost": 3000000,
        "averageMaintenanceCost": 1000000,
        "maintenanceItems": [
          {
            "type": "Thay dầu",
            "date": "2025-10-15T00:00:00Z",
            "cost": 500000,
            "status": "Hoàn thành"
          },
          {
            "type": "Kiểm tra pin",
            "date": "2025-10-20T00:00:00Z",
            "cost": 2500000,
            "status": "Hoàn thành"
          }
        ]
      },
      "fundStatus": {
        "currentBalance": 25000000,
        "monthlyContributions": 5000000,
        "monthlyExpenses": 8500000,
        "projectedBalance": 21500000
      },
      "coOwnerBreakdown": [
        {
          "coOwnerId": 1,
          "coOwnerName": "Nguyễn Văn A",
          "ownershipPercentage": 40.0,
          "usagePercentage": 48.0,
          "bookingCount": 12,
          "hoursUsed": 86.4,
          "contributionAmount": 2000000,
          "usageVsOwnership": 8.0
        },
        {
          "coOwnerId": 2,
          "coOwnerName": "Trần Thị B",
          "ownershipPercentage": 60.0,
          "usagePercentage": 52.0,
          "bookingCount": 13,
          "hoursUsed": 94.1,
          "contributionAmount": 3000000,
          "usageVsOwnership": -8.0
        }
      ],
      "generatedAt": "2025-10-24T10:30:00Z",
      "generatedBy": "Nguyễn Văn A"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Báo cáo tháng được tạo thành công             |
  | 400    | Giá trị tháng không hợp lệ (phải từ 1-12)     |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 2. Tạo Báo Cáo Quý
**[User - Co-Owner]**

- **Mô tả:**
  Tạo báo cáo tổng hợp cho một quý (3 tháng), bao gồm xu hướng theo tháng và so sánh với các quý trước.

- **Endpoint:**
  ```
  POST /quarterly
  ```

- **Request Body:**
  ```json
  {
    "vehicleId": 1,
    "year": 2025,
    "quarter": 4
  }
  ```

- **Parameters:**
  | Tên         | Kiểu    | Bắt buộc | Mô tả                                           |
  |-------------|---------|----------|-------------------------------------------------|
  | `vehicleId` | `int`   | Có       | ID của xe cần tạo báo cáo                      |
  | `year`      | `int`   | Có       | Năm báo cáo                                    |
  | `quarter`   | `int`   | Có       | Quý báo cáo (1-4)                              |

- **Mapping Quý:**
  - Q1: Tháng 1 - Tháng 3
  - Q2: Tháng 4 - Tháng 6  
  - Q3: Tháng 7 - Tháng 9
  - Q4: Tháng 10 - Tháng 12

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Báo cáo quý được tạo thành công",
    "data": {
      "reportId": "rpt_quarterly_2025_Q4_001",
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "quarter": 4,
      "quarterName": "Quý 4",
      "year": 2025,
      "startDate": "2025-10-01T00:00:00Z",
      "endDate": "2025-12-31T23:59:59Z",
      "totalDays": 92,
      "quarterSummary": {
        "totalBookings": 78,
        "totalHours": 562.5,
        "totalKilometers": 3850,
        "totalIncome": 47000000,
        "totalExpenses": 25500000,
        "netProfit": 21500000,
        "averageMonthlyUtilization": 26.8
      },
      "monthlyBreakdown": [
        {
          "month": 10,
          "monthName": "Tháng 10",
          "bookings": 25,
          "hours": 180.5,
          "kilometers": 1250,
          "income": 15000000,
          "expenses": 8500000,
          "profit": 6500000
        },
        {
          "month": 11,
          "monthName": "Tháng 11",
          "bookings": 28,
          "hours": 201.0,
          "kilometers": 1400,
          "income": 16000000,
          "expenses": 9000000,
          "profit": 7000000
        },
        {
          "month": 12,
          "monthName": "Tháng 12",
          "bookings": 25,
          "hours": 181.0,
          "kilometers": 1200,
          "income": 16000000,
          "expenses": 8000000,
          "profit": 8000000
        }
      ],
      "trends": {
        "bookingsTrend": "Ổn định",
        "hoursTrend": "Tăng nhẹ",
        "incomeTrend": "Tăng",
        "expensesTrend": "Giảm",
        "profitabilityTrend": "Cải thiện"
      },
      "generatedAt": "2025-10-24T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Báo cáo quý được tạo thành công               |
  | 400    | Giá trị quý không hợp lệ (phải từ 1-4)        |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 3. Tạo Báo Cáo Năm
**[User - Co-Owner]**

- **Mô tả:**
  Tạo báo cáo tổng hợp cho cả năm, bao gồm phân tích theo quý và tháng, xu hướng dài hạn.

- **Endpoint:**
  ```
  POST /yearly
  ```

- **Request Body:**
  ```json
  {
    "vehicleId": 1,
    "year": 2025
  }
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Báo cáo năm được tạo thành công",
    "data": {
      "reportId": "rpt_yearly_2025_001",
      "vehicleId": 1,
      "year": 2025,
      "yearSummary": {
        "totalBookings": 320,
        "totalHours": 2280.0,
        "totalKilometers": 15600,
        "totalIncome": 192000000,
        "totalExpenses": 96000000,
        "netProfit": 96000000,
        "averageMonthlyUtilization": 25.5,
        "roi": 24.0
      },
      "quarterlyBreakdown": [
        {
          "quarter": 1,
          "quarterName": "Quý 1",
          "summary": "..."
        }
      ],
      "monthlyBreakdown": [
        {
          "month": 1,
          "monthName": "Tháng 1",
          "summary": "..."
        }
      ],
      "yearlyTrends": {
        "peakMonth": "Tháng 12",
        "lowestMonth": "Tháng 2",
        "seasonalPattern": "Cao vào cuối năm",
        "growthRate": 15.5,
        "profitabilityTrend": "Cải thiện đều đặn"
      },
      "generatedAt": "2025-10-24T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Báo cáo năm được tạo thành công               |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 4. Xuất Báo Cáo (PDF/Excel)
**[User - Co-Owner]**

- **Mô tả:**
  Xuất báo cáo dưới định dạng PDF hoặc Excel để tải về. Tự động xác định loại báo cáo dựa trên tham số.

- **Endpoint:**
  ```
  POST /export
  ```

- **Request Body:**
  ```json
  {
    "vehicleId": 1,
    "year": 2025,
    "month": 10,
    "exportFormat": "PDF"
  }
  ```

- **Parameters:**
  | Tên            | Kiểu     | Bắt buộc | Mô tả                                         |
  |----------------|----------|----------|-----------------------------------------------|
  | `vehicleId`    | `int`    | Có       | ID của xe                                     |
  | `year`         | `int`    | Có       | Năm báo cáo                                   |
  | `month`        | `int`    | Không    | Tháng (nếu có = báo cáo tháng)               |
  | `quarter`      | `int`    | Không    | Quý (nếu có = báo cáo quý)                   |
  | `exportFormat` | `string` | Có       | Định dạng xuất: "PDF" hoặc "Excel"           |

- **Xác định loại báo cáo:**
  - **Báo cáo tháng**: Có tham số `month`
  - **Báo cáo quý**: Có tham số `quarter`  
  - **Báo cáo năm**: Không có `month` và `quarter`

- **Sample Requests:**
  ```json
  // Báo cáo tháng PDF
  {
    "vehicleId": 1,
    "year": 2025,
    "month": 10,
    "exportFormat": "PDF"
  }

  // Báo cáo quý Excel
  {
    "vehicleId": 1,
    "year": 2025,
    "quarter": 4,
    "exportFormat": "Excel"
  }

  // Báo cáo năm PDF
  {
    "vehicleId": 1,
    "year": 2025,
    "exportFormat": "PDF"
  }
  ```

- **Response (200 OK):**
  Trả về file để tải về với headers phù hợp:
  ```
  Content-Type: application/pdf hoặc application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
  Content-Disposition: attachment; filename="report_vehicle_1_2025_10.pdf"
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | File báo cáo được tạo thành công              |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server hoặc không thể tạo file            |

---

### 5. Lấy Danh Sách Kỳ Báo Cáo Khả Dụng
**[User - Co-Owner]**

- **Mô tả:**
  Trả về danh sách tất cả các tháng có dữ liệu khả dụng để tạo báo cáo, dựa trên lịch sử đặt xe.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/available-periods
  ```

- **Parameters:**
  | Tên         | Kiểu    | Vị trí | Bắt buộc | Mô tả                                    |
  |-------------|---------|--------|----------|------------------------------------------|
  | `vehicleId` | `Guid`  | Path   | Có       | ID của xe cần kiểm tra                   |

- **Sample Request:**
  ```
  GET /api/reports/vehicle/3fa85f64-5717-4562-b3fc-2c963f66afa6/available-periods
  Authorization: Bearer {token}
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Danh sách kỳ báo cáo khả dụng được lấy thành công",
    "data": {
      "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleName": "Tesla Model 3",
      "dataRange": {
        "earliestDate": "2024-01-15T00:00:00Z",
        "latestDate": "2025-10-24T00:00:00Z",
        "totalMonths": 21
      },
      "availablePeriods": [
        {
          "year": 2024,
          "month": 1,
          "monthName": "Tháng 1",
          "hasData": true,
          "bookingCount": 12,
          "dataQuality": "Đầy đủ"
        },
        {
          "year": 2024,
          "month": 2,
          "monthName": "Tháng 2",
          "hasData": true,
          "bookingCount": 18,
          "dataQuality": "Đầy đủ"
        },
        {
          "year": 2025,
          "month": 10,
          "monthName": "Tháng 10",
          "hasData": true,
          "bookingCount": 25,
          "dataQuality": "Đầy đủ"
        }
      ],
      "availableYears": [2024, 2025],
      "availableQuarters": [
        {
          "year": 2024,
          "quarter": 1,
          "quarterName": "Quý 1",
          "months": [1, 2, 3],
          "dataQuality": "Đầy đủ"
        },
        {
          "year": 2025,
          "quarter": 4,
          "quarterName": "Quý 4",
          "months": [10, 11, 12],
          "dataQuality": "Một phần"
        }
      ]
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Danh sách kỳ báo cáo được lấy thành công      |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 6. Lấy Báo Cáo Tháng Hiện Tại
**[User - Co-Owner]**

- **Mô tả:**
  Endpoint tiện lợi để tạo báo cáo cho tháng hiện tại mà không cần tham số ngày tháng.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/current-month
  ```

- **Sample Request:**
  ```
  GET /api/reports/vehicle/1/current-month
  Authorization: Bearer {token}
  ```

- **Sample Response:** Giống như endpoint "Tạo Báo Cáo Tháng"

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Báo cáo tháng hiện tại được tạo thành công    |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 7. Lấy Báo Cáo Quý Hiện Tại
**[User - Co-Owner]**

- **Mô tả:**
  Endpoint tiện lợi để tạo báo cáo cho quý hiện tại tự động.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/current-quarter
  ```

- **Sample Response:** Giống như endpoint "Tạo Báo Cáo Quý"

---

### 8. Lấy Báo Cáo Năm Hiện Tại
**[User - Co-Owner]**

- **Mô tả:**
  Endpoint tiện lợi để tạo báo cáo cho năm hiện tại tự động.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/current-year
  ```

- **Sample Response:** Giống như endpoint "Tạo Báo Cáo Năm"

---

## Best Practices

### 1. **Xử lý lỗi**
```javascript
try {
  const response = await fetch('/api/reports/monthly', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      vehicleId: 1,
      year: 2025,
      month: 10
    })
  });
  
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }
  
  const data = await response.json();
  
} catch (error) {
  console.error('Lỗi tạo báo cáo:', error);
}
```

### 2. **Kiểm tra quyền truy cập**
- Đảm bảo user là co-owner của xe trước khi gọi API
- Xử lý response 403 một cách thích hợp

### 3. **Tối ưu performance**
- Cache kết quả báo cáo cho các kỳ đã qua
- Sử dụng pagination cho danh sách dài
- Kiểm tra available-periods trước khi tạo báo cáo

### 4. **UI/UX tốt**
- Hiển thị loading state khi tạo báo cáo
- Cung cấp preview trước khi xuất file
- Cho phép user chọn kỳ báo cáo từ danh sách available-periods

### 5. **Bảo mật**
- Luôn validate vehicleId và quyền truy cập
- Không expose thông tin sensitive trong báo cáo
- Log các hoạt động xuất báo cáo để audit

---

## Error Codes

| Mã lỗi | Mô tả                                    |
|--------|------------------------------------------|
| 200    | Thành công                               |
| 400    | Dữ liệu đầu vào không hợp lệ            |
| 401    | Token không hợp lệ                       |
| 403    | Không có quyền truy cập xe               |
| 404    | Không tìm thấy xe hoặc dữ liệu           |
| 500    | Lỗi server nội bộ                       |

---

## Data Models

### GenerateMonthlyReportRequest
```typescript
interface GenerateMonthlyReportRequest {
  vehicleId: number;
  year: number;
  month: number; // 1-12
}
```

### GenerateQuarterlyReportRequest  
```typescript
interface GenerateQuarterlyReportRequest {
  vehicleId: number;
  year: number;
  quarter: number; // 1-4
}
```

### ExportReportRequest
```typescript
interface ExportReportRequest {
  vehicleId: number;
  year: number;
  month?: number;     // Optional - for monthly report
  quarter?: number;   // Optional - for quarterly report
  exportFormat: 'PDF' | 'Excel';
}
```