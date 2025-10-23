# Request Booking Slot - Quick Reference Guide

## 🎯 Quick Overview

**Request Booking Slot** là tính năng đặt lịch thông minh cho hệ thống đồng sở hữu xe điện, tự động phát hiện xung đột và đề xuất thời gian thay thế.

---

## 🚀 API Endpoints Cheat Sheet

### 1️⃣ Yêu cầu đặt slot
```
POST /api/booking/vehicle/{vehicleId}/request-slot
Role: CoOwner
```

**Body mẫu:**
```json
{
  "preferredStartTime": "2025-01-25T09:00:00",
  "preferredEndTime": "2025-01-25T17:00:00",
  "purpose": "Đi công tác",
  "priority": 2,
  "isFlexible": true,
  "autoConfirmIfAvailable": true,
  "alternativeSlots": [
    {
      "startTime": "2025-01-26T09:00:00",
      "endTime": "2025-01-26T17:00:00",
      "preferenceRank": 1
    }
  ]
}
```

**Kết quả:**
- ✅ **Auto-Confirmed** nếu không có xung đột
- ⏳ **Pending** nếu có xung đột (cần approval)
- 💡 Đề xuất thời gian thay thế

---

### 2️⃣ Phê duyệt/Từ chối yêu cầu
```
POST /api/booking/slot-request/{requestId}/respond
Role: CoOwner
```

**Approve:**
```json
{
  "isApproved": true,
  "notes": "OK, you can use it!"
}
```

**Reject:**
```json
{
  "isApproved": false,
  "rejectionReason": "Tôi cần xe ngày đó",
  "suggestedStartTime": "2025-01-26T09:00:00",
  "suggestedEndTime": "2025-01-26T17:00:00"
}
```

---

### 3️⃣ Hủy yêu cầu
```
POST /api/booking/slot-request/{requestId}/cancel
Role: CoOwner (chỉ người tạo)
```

```json
{
  "reason": "Kế hoạch thay đổi"
}
```

---

### 4️⃣ Xem yêu cầu chờ duyệt
```
GET /api/booking/vehicle/{vehicleId}/pending-slot-requests
Role: CoOwner
```

---

### 5️⃣ Xem thống kê
```
GET /api/booking/vehicle/{vehicleId}/slot-request-analytics
    ?startDate=2024-10-17&endDate=2025-01-17
Role: CoOwner
```

---

## 📊 Enums Reference

### Priority (Mức độ ưu tiên)
```
0 = Low     (Sử dụng cá nhân thường xuyên)
1 = Medium  (Đi làm, việc vặt)
2 = High    (Cuộc hẹn quan trọng)
3 = Urgent  (Khẩn cấp)
```

### SlotRequestStatus (Trạng thái yêu cầu)
```
0 = Pending          (Chờ duyệt)
1 = AutoConfirmed    (Tự động xác nhận)
2 = Approved         (Đã duyệt thủ công)
3 = Rejected         (Bị từ chối)
4 = Cancelled        (Đã hủy)
5 = Expired          (Hết hạn)
6 = ConflictResolved (Đã giải quyết xung đột)
```

### SlotAvailabilityStatus (Tình trạng khả dụng)
```
0 = Available            (Hoàn toàn trống)
1 = PartiallyAvailable   (Có chồng lấn một phần)
2 = Unavailable          (Đã đầy)
3 = RequiresApproval     (Cần phê duyệt)
```

---

## 🔑 Key Features

### ✨ Auto-Confirmation
- Nếu `autoConfirmIfAvailable = true` và không có xung đột → Tự động xác nhận
- Booking được tạo với status = `Confirmed`

### 🔍 Conflict Detection
- Tự động kiểm tra booking trùng lặp
- Hiển thị chi tiết: Co-owner nào, thời gian nào, overlap bao nhiêu giờ

### 💡 Alternative Suggestions
Hệ thống đề xuất 4 loại thời gian thay thế:

1. **User-provided** (ưu tiên cao nhất)
2. **Trước thời gian mong muốn** (1.5x duration)
3. **Sau thời gian mong muốn** (1.5x duration)
4. **Cùng giờ ngày hôm trước/sau**

Mỗi đề xuất có:
- `isAvailable`: Có trống không
- `conflictProbability`: Xác suất xung đột (0-1)
- `recommendationScore`: Điểm đề xuất (0-100)

---

## 🎨 Frontend Integration

### React Hook Example
```tsx
const useBookingSlotRequest = () => {
  const requestSlot = async (vehicleId, data) => {
    const response = await fetch(
      `/api/booking/vehicle/${vehicleId}/request-slot`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
      }
    );
    return response.json();
  };
  
  return { requestSlot };
};

// Sử dụng
const { requestSlot } = useBookingSlotRequest();

const handleSubmit = async () => {
  const result = await requestSlot(5, {
    preferredStartTime: '2025-01-25T09:00:00',
    preferredEndTime: '2025-01-25T17:00:00',
    purpose: 'Business trip',
    priority: 2,
    autoConfirmIfAvailable: true
  });
  
  if (result.data.status === 1) {
    alert('✅ Đã xác nhận tự động!');
  } else if (result.data.status === 0) {
    console.log('Conflicts:', result.data.conflictingBookings);
    console.log('Alternatives:', result.data.alternativeSuggestions);
  }
};
```

---

## 💻 Testing with Postman

### 1. Request Slot (Success)
```
POST http://localhost:5000/api/booking/vehicle/5/request-slot
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "preferredStartTime": "2025-01-25T09:00:00",
  "preferredEndTime": "2025-01-25T17:00:00",
  "purpose": "Test booking",
  "priority": 1,
  "isFlexible": false,
  "autoConfirmIfAvailable": true
}
```

**Expected:** Status 201, message "BOOKING_SLOT_AUTO_CONFIRMED"

---

### 2. Request Slot (With Conflict)
```
POST http://localhost:5000/api/booking/vehicle/5/request-slot
Authorization: Bearer USER1_TOKEN
Content-Type: application/json

{
  "preferredStartTime": "2025-01-25T14:00:00",
  "preferredEndTime": "2025-01-25T18:00:00",
  "purpose": "Test conflict",
  "priority": 2,
  "isFlexible": true,
  "autoConfirmIfAvailable": true
}
```

**Expected:** 
- Status 201
- Message "BOOKING_SLOT_REQUEST_CREATED"
- `conflictingBookings` array with details
- `alternativeSuggestions` array with recommendations

---

### 3. Approve Request
```
POST http://localhost:5000/api/booking/slot-request/124/respond
Authorization: Bearer USER2_TOKEN
Content-Type: application/json

{
  "isApproved": true,
  "notes": "Approved!"
}
```

**Expected:** Status 200, message "BOOKING_REQUEST_APPROVED"

---

### 4. Get Pending Requests
```
GET http://localhost:5000/api/booking/vehicle/5/pending-slot-requests
Authorization: Bearer YOUR_TOKEN
```

**Expected:** List of pending booking requests

---

### 5. Get Analytics
```
GET http://localhost:5000/api/booking/vehicle/5/slot-request-analytics
    ?startDate=2024-10-17&endDate=2025-01-17
Authorization: Bearer YOUR_TOKEN
```

**Expected:** Analytics with approval rates, popular time slots, etc.

---

## 🐛 Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| 403 USER_NOT_CO_OWNER | User không phải co-owner | Đăng nhập với tài khoản co-owner |
| 403 ACCESS_DENIED_NOT_VEHICLE_CO_OWNER | User không sở hữu xe này | Kiểm tra VehicleCoOwner |
| 404 VEHICLE_NOT_FOUND | vehicleId không tồn tại | Kiểm tra ID xe |
| 400 VALIDATION_ERROR | Dữ liệu không hợp lệ | Xem error details |
| 400 BOOKING_REQUEST_ALREADY_PROCESSED | Request đã xử lý | Không thể approve/reject lại |

---

## 📋 Checklist for Implementation

- [ ] DTOs created (`BookingSlotRequestDTOs.cs`)
- [ ] Service interface updated (`IBookingService.cs`)
- [ ] Service implementation added (`BookingService.cs`)
- [ ] Controller endpoints created (`BookingController.cs`)
- [ ] FluentValidation validators added
- [ ] Build successful (0 errors)
- [ ] Documentation complete
- [ ] API tested with Postman
- [ ] Frontend integration ready

---

## 🔗 Related Features

- **Basic Booking**: Simple booking creation
- **Booking Calendar**: View all bookings in calendar format
- **Check Availability**: Pre-check slot availability
- **Fairness Report**: Usage vs ownership analysis
- **Schedule Suggestions**: AI-powered optimal booking times

---

## 📞 Quick Support

- **Full Documentation**: `BOOKING_SLOT_REQUEST_FEATURE.md`
- **Source Files**:
  - DTOs: `EvCoOwnership.Repositories/DTOs/BookingDTOs/BookingSlotRequestDTOs.cs`
  - Service: `EvCoOwnership.Services/Services/BookingService.cs`
  - Controller: `EvCoOwnership.API/Controllers/BookingController.cs`

---

**Version:** 1.0.0  
**Last Updated:** January 17, 2025
