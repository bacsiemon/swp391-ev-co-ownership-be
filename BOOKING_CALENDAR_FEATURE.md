# View Shared Booking Calendar Feature

## Tổng quan (Overview)

Tính năng **Booking Calendar** cho phép các co-owner xem lịch đặt xe chung của nhóm họ, giúp:
- ✅ **Tránh xung đột thời gian** - Xem xe đang được đặt bởi ai, khi nào
- ✅ **Lập kế hoạch sử dụng** - Tìm khoảng thời gian trống để đặt xe
- ✅ **Tăng tính minh bạch** - Mọi người trong nhóm đều thấy lịch sử dụng chung
- ✅ **Kiểm tra khả dụng** - Check xe có sẵn trước khi tạo booking

---

## Tính năng đã triển khai

### 1️⃣ **View Booking Calendar** (GET /api/booking/calendar)
Xem lịch booking trong khoảng thời gian cụ thể

**Phân quyền theo vai trò:**
- **Co-owner**: Chỉ xem bookings của các xe trong nhóm của mình
- **Staff/Admin**: Xem TẤT CẢ bookings trong hệ thống

**Tham số:**
- `startDate` (required): Ngày bắt đầu (yyyy-MM-dd)
- `endDate` (required): Ngày kết thúc (yyyy-MM-dd)
- `vehicleId` (optional): Lọc theo xe cụ thể
- `status` (optional): Lọc theo trạng thái (Pending, Confirmed, Active, Completed, Cancelled)

**Giới hạn:**
- Khoảng thời gian tối đa: **90 ngày**
- Khuyến nghị: 7-30 ngày cho calendar view thông thường

### 2️⃣ **Check Vehicle Availability** (GET /api/booking/availability)
Kiểm tra xe có sẵn cho booking không

**Mục đích:**
- Validate trước khi tạo booking
- Xem các booking xung đột (nếu có)
- Lên kế hoạch thời gian thay thế

**Tham số:**
- `vehicleId` (required): ID xe cần kiểm tra
- `startTime` (required): Thời gian bắt đầu (yyyy-MM-ddTHH:mm:ss)
- `endTime` (required): Thời gian kết thúc (yyyy-MM-ddTHH:mm:ss)

---

## API Endpoints

### 📅 GET /api/booking/calendar

**Mô tả:** Lấy lịch booking trong khoảng thời gian

**Authorization:** CoOwner, Staff, Admin

**Request Example:**
```http
GET /api/booking/calendar?startDate=2025-01-17&endDate=2025-01-24
```

**Response Example (Success - 200):**
```json
{
  "statusCode": 200,
  "message": "BOOKING_CALENDAR_RETRIEVED_SUCCESSFULLY",
  "data": {
    "startDate": "2025-01-17T00:00:00",
    "endDate": "2025-01-24T00:00:00",
    "events": [
      {
        "bookingId": 101,
        "vehicleId": 5,
        "vehicleName": "VinFast VF8 Premium",
        "brand": "VinFast",
        "model": "VF8",
        "licensePlate": "51H-12345",
        "coOwnerId": 10,
        "coOwnerName": "Nguyen Van A",
        "startTime": "2025-01-18T09:00:00",
        "endTime": "2025-01-18T17:00:00",
        "purpose": "Di công tác Đà Nẵng",
        "status": "Confirmed",
        "statusDisplay": "Confirmed",
        "durationHours": 8,
        "isCurrentUser": false
      },
      {
        "bookingId": 102,
        "vehicleId": 5,
        "vehicleName": "VinFast VF8 Premium",
        "brand": "VinFast",
        "model": "VF8",
        "licensePlate": "51H-12345",
        "coOwnerId": 15,
        "coOwnerName": "Tran Thi B",
        "startTime": "2025-01-20T08:00:00",
        "endTime": "2025-01-20T18:00:00",
        "purpose": "Đi du lịch cuối tuần",
        "status": "Pending",
        "statusDisplay": "Pending",
        "durationHours": 10,
        "isCurrentUser": true
      }
    ],
    "totalEvents": 2,
    "summary": {
      "totalBookings": 2,
      "pendingBookings": 1,
      "confirmedBookings": 1,
      "activeBookings": 0,
      "completedBookings": 0,
      "cancelledBookings": 0,
      "totalVehicles": 1,
      "myBookings": 1
    }
  }
}
```

### ✅ GET /api/booking/availability

**Mô tả:** Kiểm tra xe có sẵn cho booking không

**Authorization:** CoOwner, Staff, Admin

**Request Example:**
```http
GET /api/booking/availability?vehicleId=5&startTime=2025-01-18T09:00:00&endTime=2025-01-18T17:00:00
```

**Response Example (Available - 200):**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_AVAILABLE",
  "data": {
    "vehicleId": 5,
    "vehicleName": "VinFast VF8 Premium",
    "isAvailable": true,
    "message": "VEHICLE_AVAILABLE",
    "conflictingBookings": null
  }
}
```

**Response Example (Not Available - 200):**
```json
{
  "statusCode": 200,
  "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
  "data": {
    "vehicleId": 5,
    "vehicleName": "VinFast VF8 Premium",
    "isAvailable": false,
    "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
    "conflictingBookings": [
      {
        "bookingId": 101,
        "vehicleId": 5,
        "vehicleName": "VinFast VF8 Premium",
        "brand": "VinFast",
        "model": "VF8",
        "licensePlate": "51H-12345",
        "coOwnerId": 10,
        "coOwnerName": "Nguyen Van A",
        "startTime": "2025-01-18T10:00:00",
        "endTime": "2025-01-18T15:00:00",
        "purpose": "Di công tác",
        "status": "Confirmed",
        "statusDisplay": "Confirmed",
        "durationHours": 5,
        "isCurrentUser": false
      }
    ]
  }
}
```

---

## Use Cases thực tế

### 🎯 Use Case 1: Co-owner xem lịch xe trong tuần
**Scenario:** Anh A muốn đặt xe VF8 cho tuần tới, anh cần xem lịch xe đã được đặt chưa.

**Flow:**
1. Anh A gọi API: `GET /api/booking/calendar?startDate=2025-01-17&endDate=2025-01-24&vehicleId=5`
2. Hệ thống trả về lịch booking của xe VF8 trong tuần
3. Anh A thấy:
   - Thứ 2-3: Xe được đặt bởi người khác (9h-17h)
   - Thứ 4-5: Xe trống
   - Thứ 6-7: Xe được đặt bởi anh A (pending)
4. Anh A quyết định đặt xe vào Thứ 4

### 🎯 Use Case 2: Kiểm tra xe có sẵn trước khi đặt
**Scenario:** Chị B muốn đặt xe VF8 từ 9h-17h ngày mai.

**Flow:**
1. Chị B gọi API: `GET /api/booking/availability?vehicleId=5&startTime=2025-01-18T09:00:00&endTime=2025-01-18T17:00:00`
2. Hệ thống kiểm tra:
   - Có booking nào xung đột không?
   - Booking: 10h-15h (Confirmed) → Xung đột!
3. Response: `isAvailable: false` + danh sách booking xung đột
4. Chị B thấy xe bận từ 10h-15h, quyết định đặt 15h-20h thay vì

### 🎯 Use Case 3: Staff xem tổng quan bookings hệ thống
**Scenario:** Staff muốn xem tất cả bookings trong tuần để quản lý.

**Flow:**
1. Staff gọi API: `GET /api/booking/calendar?startDate=2025-01-17&endDate=2025-01-24&status=Pending`
2. Hệ thống trả về TẤT CẢ bookings đang pending (không bị giới hạn theo nhóm)
3. Staff xem summary:
   - 5 bookings pending cần approve
   - 3 xe đang được sử dụng nhiều
4. Staff approve các bookings hợp lệ

### 🎯 Use Case 4: Co-owner xem lịch tất cả xe trong nhóm
**Scenario:** Anh C có 3 xe trong nhóm, muốn xem lịch chung để biết xe nào rảnh.

**Flow:**
1. Anh C gọi API: `GET /api/booking/calendar?startDate=2025-01-17&endDate=2025-01-24` (không filter vehicleId)
2. Hệ thống trả về bookings của TẤT CẢ xe trong nhóm anh C
3. Anh C thấy:
   - VF8: Bận thứ 2, 3, 5
   - VF9: Bận thứ 4, 6
   - Model 3: Trống cả tuần
4. Anh C đặt Model 3 cho thứ 2

---

## Kiến trúc kỹ thuật

### Layer Structure

```
API Layer (Controller)
    ↓
Service Layer (Business Logic)
    ↓
Repository Layer (Data Access)
    ↓
Database (PostgreSQL)
```

### 1. **DTOs Created**
📄 `BookingCalendarDTOs.cs`
- `BookingCalendarEvent`: Thông tin chi tiết 1 booking event
- `BookingCalendarResponse`: Response chứa list events + summary
- `BookingCalendarSummary`: Thống kê tổng quan (total, by status, my bookings)
- `VehicleAvailabilityRequest`: Request check availability
- `VehicleAvailabilityResponse`: Response availability + conflicting bookings

### 2. **Repository Layer**
📄 `IBookingRepository.cs` + `BookingRepository.cs`

**Phương thức mới:**
- `GetBookingsForCalendarAsync()`: Lấy bookings trong khoảng thời gian
  - Role-based filtering (co-owner chỉ thấy xe trong nhóm)
  - Filter by vehicle, status
  - Date range overlap detection
  
- `GetConflictingBookingsAsync()`: Tìm bookings xung đột
  - Time overlap logic: `(existing.start < new.end) AND (existing.end > new.start)`
  - Exclude cancelled bookings
  - Optional exclude specific booking (for updates)

### 3. **Service Layer**
📄 `IBookingService.cs` + `BookingService.cs`

**Business Logic:**
- `GetBookingCalendarAsync()`:
  - Validate date range (max 90 days)
  - Get user role (CoOwner/Staff/Admin)
  - Apply role-based filtering
  - Parse status filter
  - Map to calendar events
  - Calculate summary statistics
  
- `CheckVehicleAvailabilityAsync()`:
  - Validate time range
  - Check vehicle exists
  - Find conflicting bookings
  - Return availability status + conflicts

### 4. **Controller Layer**
📄 `BookingController.cs`

**Endpoints:**
- `GET /api/booking/calendar`: Comprehensive XML documentation với examples
- `GET /api/booking/availability`: Detailed availability check

---

## Xử lý xung đột thời gian (Time Conflict Detection)

### Overlap Logic
Booking **xung đột** khi thỏa mãn:
```
(existing.startTime < new.endTime) AND (existing.endTime > new.startTime)
```

### Các trường hợp xung đột:

1. **New booking bắt đầu trong existing booking**
   ```
   Existing: |--------|
   New:         |-----|
   ```

2. **New booking kết thúc trong existing booking**
   ```
   Existing:    |--------|
   New:      |-----|
   ```

3. **New booking bao phủ existing booking**
   ```
   Existing:   |-----|
   New:      |---------|
   ```

4. **Existing booking bao phủ new booking**
   ```
   Existing: |---------|
   New:        |-----|
   ```

### Không xung đột:
```
Existing: |-----|
New:              |-----|  (Bắt đầu sau khi existing kết thúc)

Existing:         |-----|
New:      |-----|        (Kết thúc trước khi existing bắt đầu)
```

---

## Database Query Optimization

### Indexing Strategy
```sql
-- Recommended indexes for performance
CREATE INDEX idx_bookings_vehicle_dates ON bookings(vehicle_id, start_time, end_time);
CREATE INDEX idx_bookings_status ON bookings(status_enum);
CREATE INDEX idx_vehicle_co_owners ON vehicle_co_owners(co_owner_id, vehicle_id);
```

### Query Performance
- **Eager Loading**: Include Vehicle, CoOwner, User trong 1 query
- **Date Range Filtering**: WHERE clause trước, Include sau
- **Cancelled Booking Exclusion**: Filter sớm để giảm dataset

---

## Error Handling

### Các lỗi có thể xảy ra:

| Error Code | Message | Cause | Solution |
|------------|---------|-------|----------|
| 400 | INVALID_DATE_RANGE | startDate >= endDate | Đảm bảo startDate < endDate |
| 400 | DATE_RANGE_TOO_LARGE | Range > 90 days | Giảm khoảng thời gian xuống ≤ 90 ngày |
| 400 | INVALID_STATUS_FILTER | Status không hợp lệ | Dùng: Pending, Confirmed, Active, Completed, Cancelled |
| 400 | INVALID_TIME_RANGE | startTime >= endTime | Đảm bảo startTime < endTime |
| 403 | USER_NOT_CO_OWNER | User không phải co-owner | Chỉ co-owner mới xem calendar |
| 404 | USER_NOT_FOUND | UserId không tồn tại | Kiểm tra JWT token |
| 404 | VEHICLE_NOT_FOUND | VehicleId không tồn tại | Kiểm tra vehicle ID |
| 500 | INTERNAL_SERVER_ERROR | Server error | Liên hệ admin |

---

## Frontend Integration Suggestions

### Calendar View Implementation
```javascript
// Example: Get next 7 days calendar
const startDate = new Date();
const endDate = new Date();
endDate.setDate(endDate.getDate() + 7);

const response = await fetch(
  `/api/booking/calendar?` +
  `startDate=${startDate.toISOString().split('T')[0]}&` +
  `endDate=${endDate.toISOString().split('T')[0]}`,
  {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }
);

const data = await response.json();
// Render calendar with data.data.events
```

### Availability Check Before Booking
```javascript
// Check availability before showing booking form
async function checkAvailability(vehicleId, startTime, endTime) {
  const response = await fetch(
    `/api/booking/availability?` +
    `vehicleId=${vehicleId}&` +
    `startTime=${startTime.toISOString()}&` +
    `endTime=${endTime.toISOString()}`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  
  const result = await response.json();
  
  if (!result.data.isAvailable) {
    // Show conflicting bookings
    alert('Xe không khả dụng. Có booking xung đột:');
    result.data.conflictingBookings.forEach(booking => {
      console.log(`${booking.coOwnerName}: ${booking.startTime} - ${booking.endTime}`);
    });
  }
  
  return result.data.isAvailable;
}
```

### Calendar UI Components (Recommended)
- **FullCalendar.js**: Popular calendar library
- **React Big Calendar**: For React apps
- **Syncfusion Scheduler**: Enterprise-grade scheduler

**Event Colors by Status:**
- Pending: Orange/Yellow 🟡
- Confirmed: Blue 🔵
- Active: Green 🟢
- Completed: Gray ⚫
- Cancelled: Red 🔴

---

## Testing Scenarios

### Test Case 1: Co-owner Calendar Access
1. Login as co-owner
2. Call calendar API for next 7 days
3. Verify: Only see vehicles in my groups
4. Verify: See both my bookings and others' bookings

### Test Case 2: Staff Full Access
1. Login as staff
2. Call calendar API for next 7 days
3. Verify: See ALL vehicles across all groups
4. Verify: Can filter by vehicle, status

### Test Case 3: Date Range Validation
1. Call calendar with startDate > endDate
2. Verify: 400 INVALID_DATE_RANGE
3. Call calendar with 100-day range
4. Verify: 400 DATE_RANGE_TOO_LARGE

### Test Case 4: Availability Check
1. Create booking: Vehicle 5, Jan 18 10:00-15:00
2. Check availability: Vehicle 5, Jan 18 09:00-17:00
3. Verify: isAvailable = false
4. Verify: conflictingBookings contains the booking

### Test Case 5: Time Conflict Detection
Test all overlap scenarios:
- New starts during existing ✓
- New ends during existing ✓
- New contains existing ✓
- Existing contains new ✓
- No overlap ✓

---

## Files Modified

### 📁 DTOs
- ✅ `BookingCalendarDTOs.cs` (NEW) - Calendar DTOs

### 📁 Repository Layer
- ✅ `IBookingRepository.cs` - Added 2 methods
- ✅ `BookingRepository.cs` - Implemented calendar + conflict queries

### 📁 Service Layer
- ✅ `IBookingService.cs` - Added 2 method signatures
- ✅ `BookingService.cs` - Implemented calendar + availability logic

### 📁 Controller Layer
- ✅ `BookingController.cs` - Added 2 endpoints with full documentation

---

## Build Status
✅ **Build Successful** - All layers compile without errors  
✅ **Zero Breaking Changes** - Existing APIs unaffected  
✅ **XML Documentation** - Complete with examples  
✅ **Role-Based Access** - Co-owner/Staff/Admin filtering implemented  

---

## Next Steps

### ✅ Completed
- [x] Repository methods for calendar queries
- [x] Service layer business logic
- [x] API endpoints with authorization
- [x] Comprehensive documentation
- [x] Time conflict detection
- [x] Role-based filtering

### 🔜 Recommended Enhancements
- [ ] Add notification when booking conflicts with my time
- [ ] Export calendar to iCal/Google Calendar
- [ ] Recurring bookings support
- [ ] Booking reminders (1 day before, 1 hour before)
- [ ] Vehicle usage statistics (most booked times)
- [ ] Suggested alternative time slots

---

## Summary

Tính năng **View Shared Booking Calendar** đã được triển khai đầy đủ với:

✅ **2 API endpoints mới:**
1. `GET /api/booking/calendar` - Xem lịch booking theo role
2. `GET /api/booking/availability` - Check xe có sẵn không

✅ **Tính năng chính:**
- Role-based calendar (Co-owner: group vehicles, Staff/Admin: all vehicles)
- Date range filtering (max 90 days)
- Vehicle + status filters
- Time conflict detection
- Summary statistics
- Conflicting bookings display

✅ **Phù hợp với thực tế:**
- Giúp co-owners phối hợp sử dụng xe
- Tránh booking xung đột
- Minh bạch lịch sử dụng
- Staff quản lý tổng quan hệ thống

---

**Feature Status:** ✅ **COMPLETE - Ready for Frontend Integration**  
**Created:** January 17, 2025  
**Author:** GitHub Copilot Agent
