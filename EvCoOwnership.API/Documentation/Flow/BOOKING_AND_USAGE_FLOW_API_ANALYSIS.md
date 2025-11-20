# Phân tích API - Quy trình Đặt xe và Sử dụng xe (Booking & Usage Flow)

## 📋 Tổng quan

Tài liệu này phân tích các API liên quan đến quy trình đặt xe và sử dụng xe trong hệ thống EV Co-Ownership, bao gồm:

1. **Xem lịch trình sử dụng xe hiện tại**
2. **Đặt xe (Booking)**
3. **Kiểm tra tỉ lệ sở hữu và lịch sử sử dụng**
4. **Check-in và bắt đầu sử dụng**
5. **Check-out và kết thúc sử dụng**
6. **Hỗ trợ của Staff trong quá trình check-in/check-out**

---

## 🔄 Luồng Nghiệp Vụ (Business Flow)

### Flow 1: Co-owner xem lịch trình sử dụng xe hiện tại

#### API Endpoints:

##### 1.1. Lấy danh sách xe của Co-owner
- **Endpoint:** `GET /api/coowner/ownership`
- **Controller:** `CoOwnerController.GetOwnership()`
- **Service:** `IVehicleService.GetUserVehiclesAsync(int userId)`

##### 1.2. Xem lịch trình xe cụ thể
- **Endpoint:** `GET /api/coowner/schedule/vehicle/{vehicleId}?startDate={date}&endDate={date}&statusFilter={status}`
- **Controller:** `CoOwnerController.GetVehicleSchedule()`
- **Service:** `IScheduleService.GetVehicleScheduleAsync()`
- **DTO:** `GetVehicleScheduleRequest`, `VehicleScheduleResponse`

##### 1.3. Xem lịch trình cá nhân
- **Endpoint:** `GET /api/coowner/schedule/my-schedule?startDate={date}&endDate={date}`
- **Controller:** `CoOwnerController.GetMySchedule()`
- **Service:** `IScheduleService.GetUserScheduleAsync()`

---

### Flow 2: Co-owner đặt xe (Create Booking)

#### API Endpoints:

##### 2.1. Kiểm tra khả năng đặt xe (Check Availability)
- **Endpoint:** `POST /api/coowner/schedule/check-availability`
- **Controller:** `CoOwnerController.CheckAvailability()`
- **Service:** `IScheduleService.CheckAvailabilityAsync()`
- **DTO:** `CheckAvailabilityRequest`, `AvailabilityCheckResponse`

##### 2.2. Tạo booking cơ bản
- **Endpoint:** `POST /api/coowner/bookings`
- **Controller:** `CoOwnerController.CreateBooking()`
- **Service:** `IBookingService.CreateBookingAsync()`
- **DTO:** `CreateBookingRequest`, `BookingResponse`

##### 2.3. Đặt xe với yêu cầu slot (Request Booking Slot) - Advanced
- **Endpoint:** `POST /api/booking/request-slot`
- **Service:** `IBookingService.RequestBookingSlotAsync()`
- **DTO:** `RequestBookingSlotRequest`, `BookingSlotRequestResponse`

##### 2.4. Tìm khung giờ tối ưu
- **Endpoint:** `POST /api/coowner/schedule/find-optimal-slots`
- **Controller:** `CoOwnerController.FindOptimalSlots()`
- **Service:** `IScheduleService.FindOptimalSlotsAsync()`
- **DTO:** `FindOptimalSlotsRequest`, `OptimalSlotsResponse`

---

### Flow 3: Hệ thống kiểm tra tỉ lệ sở hữu và lịch sử sử dụng

#### API Endpoints:

##### 3.1. Kiểm tra lịch sử sử dụng (Usage History)
- **Endpoint:** `GET /api/coowner/analytics/my-usage-history?vehicleId={id}&startDate={date}&endDate={date}`
- **Controller:** `CoOwnerController.GetMyUsageHistory()`
- **Service:** `IUsageAnalyticsService.GetPersonalUsageHistoryAsync()`
- **DTO:** `GetPersonalUsageHistoryRequest`, `PersonalUsageHistoryResponse`

##### 3.2. So sánh Usage vs Ownership
- **Endpoint:** `GET /api/coowner/analytics/vehicle/{vehicleId}/usage-vs-ownership?startDate={date}&endDate={date}&usageMetric=Hours`
- **Controller:** `CoOwnerController.GetUsageVsOwnershipComparison()`
- **Service:** `IUsageAnalyticsService.GetUsageVsOwnershipAsync()`
- **DTO:** `GetUsageVsOwnershipRequest`, `UsageVsOwnershipResponse`

##### 3.3. Kiểm tra ownership percentage (Automatic)
- Được thực hiện tự động trong `BookingService.CreateBookingAsync()`
- Validation logic kiểm tra co-owner có quyền đặt xe hay không

---

### Flow 4: Co-owner Check-in và bắt đầu sử dụng

#### API Endpoints:

##### 4.1. Generate QR Code cho booking
- **Endpoint:** `GET /api/checkin/generate-qr/{bookingId}`
- **Service:** `ICheckInCheckOutService.GenerateBookingQRCodeAsync()`
- **DTO:** `VehicleQRCodeData`

##### 4.2. QR Scan Check-in (Self-service)
- **Endpoint:** `POST /api/checkin/qr-scan`
- **Service:** `ICheckInCheckOutService.QRScanCheckInAsync()`
- **DTO:** `QRScanCheckInRequest`, `CheckInResponse`

##### 4.3. Validate Check-in Eligibility
- **Endpoint:** `GET /api/checkin/validate/{bookingId}`
- **Service:** `ICheckInCheckOutService.ValidateCheckInEligibilityAsync()`

---

### Flow 5: Staff hỗ trợ check-in

#### API Endpoints:

##### 5.1. Manual Check-in (Staff verification)
- **Endpoint:** `POST /api/staff/checkin`
- **Controller:** `StaffController.CheckIn()` hoặc `StaffController.PerformStaffAssistedCheckIn()`
- **Service:** `ICheckInCheckOutService.ManualCheckInAsync()`
- **DTO:** `ManualCheckInRequest`, `CheckInResponse`

##### 5.2. Get Pending Check-ins
- **Endpoint:** `GET /api/staff/checkins/pending`
- **Controller:** `StaffController.GetPendingCheckIns()`

---

### Flow 6: Co-owner Check-out sau khi sử dụng

#### API Endpoints:

##### 6.1. QR Scan Check-out (Self-service)
- **Endpoint:** `POST /api/checkout/qr-scan`
- **Service:** `ICheckInCheckOutService.QRScanCheckOutAsync()`
- **DTO:** `QRScanCheckOutRequest`, `CheckOutResponse`

##### 6.2. Validate Check-out Eligibility
- **Endpoint:** `GET /api/checkout/validate/{bookingId}`
- **Service:** `ICheckInCheckOutService.ValidateCheckOutEligibilityAsync()`

---

### Flow 7: Staff hỗ trợ check-out

#### API Endpoints:

##### 7.1. Manual Check-out (Staff verification)
- **Endpoint:** `POST /api/staff/checkout`
- **Controller:** `StaffController.CheckOut()` hoặc `StaffController.PerformStaffAssistedCheckOut()`
- **Service:** `ICheckInCheckOutService.ManualCheckOutAsync()`
- **DTO:** `ManualCheckOutRequest`, `CheckOutResponse`

---

## 📊 Các API Phụ trợ

### 1. Booking Management

#### Get My Bookings
- **Endpoint:** `GET /api/coowner/bookings/my-bookings?status={status}&page={page}&pageSize={pageSize}`

#### Get Booking Details
- **Endpoint:** `GET /api/coowner/bookings/{id}`

#### Update Booking
- **Endpoint:** `PUT /api/coowner/bookings/{id}`
- **Service:** `IBookingService.UpdateBookingAsync()` hoặc `ModifyBookingAsync()`

#### Cancel Booking
- **Endpoint:** `POST /api/coowner/bookings/{id}/cancel`
- **Service:** `IBookingService.CancelBookingAsync()` hoặc `CancelBookingEnhancedAsync()`
- **DTO:** `CancelBookingRequest`, `CancelBookingResponse`

#### Get Vehicle Bookings
- **Endpoint:** `GET /api/coowner/bookings/vehicle/{vehicleId}?startDate={date}&endDate={date}`

#### Get Vehicle Availability
- **Endpoint:** `GET /api/coowner/bookings/availability?vehicleId={id}&startDate={date}&endDate={date}`

---

### 2. Conflict Resolution (Advanced)

#### Get Pending Conflicts
- **Endpoint:** `GET /api/booking/conflicts/pending?vehicleId={id}`
- **Service:** `IBookingService.GetPendingConflictsAsync()`
- **DTO:** `GetPendingConflictsRequest`, `PendingConflictsResponse`

#### Resolve Booking Conflict
- **Endpoint:** `POST /api/booking/conflicts/{bookingId}/resolve`
- **Service:** `IBookingService.ResolveBookingConflictAsync()`
- **DTO:** `ResolveBookingConflictRequest`, `BookingConflictResolutionResponse`

#### Get Conflict Analytics
- **Endpoint:** `GET /api/booking/conflicts/analytics?vehicleId={id}&startDate={date}&endDate={date}`
- **Service:** `IBookingService.GetConflictAnalyticsAsync()`

#### Get Pending Slot Requests
- **Endpoint:** `GET /api/booking/slot-requests/pending?vehicleId={id}`
- **Service:** `IBookingService.GetPendingSlotRequestsAsync()`

#### Respond to Slot Request
- **Endpoint:** `POST /api/booking/slot-requests/{requestId}/respond`
- **Service:** `IBookingService.RespondToSlotRequestAsync()`

#### Cancel Slot Request
- **Endpoint:** `POST /api/booking/slot-requests/{requestId}/cancel`
- **Service:** `IBookingService.CancelSlotRequestAsync()`

#### Get Slot Request Analytics
- **Endpoint:** `GET /api/booking/slot-requests/analytics?vehicleId={id}&startDate={date}&endDate={date}`
- **Service:** `IBookingService.GetSlotRequestAnalyticsAsync()`

---

### 3. Usage Analytics

#### Get Usage Trends
- **Endpoint:** `GET /api/coowner/analytics/vehicle/{vehicleId}/usage-trends?startDate={date}&endDate={date}&period=monthly`
- **Service:** `IUsageAnalyticsService.GetUsageVsOwnershipTrendsAsync()`

#### Get Group Usage Summary
- **Endpoint:** `GET /api/coowner/analytics/group-summary`
- **Service:** `IUsageAnalyticsService.GetGroupUsageSummaryAsync()`

#### Compare Co-owners Usage
- **Endpoint:** `POST /api/analytics/compare-coowners`
- **Service:** `IUsageAnalyticsService.CompareCoOwnersUsageAsync()`

#### Compare Vehicles Usage
- **Endpoint:** `POST /api/analytics/compare-vehicles`
- **Service:** `IUsageAnalyticsService.CompareVehiclesUsageAsync()`

#### Compare Period Usage
- **Endpoint:** `POST /api/analytics/compare-periods`
- **Service:** `IUsageAnalyticsService.ComparePeriodUsageAsync()`

---

### 4. Schedule Management

#### Get Schedule Conflicts
- **Endpoint:** `GET /api/coowner/schedule/conflicts?startDate={date}&endDate={date}`
- **Controller:** `CoOwnerController.GetScheduleConflicts()`

#### Get Booking Calendar
- **Endpoint:** `GET /api/booking/calendar?startDate={date}&endDate={date}&vehicleId={id}&status={status}`
- **Service:** `IBookingService.GetBookingCalendarAsync()`

#### Check Vehicle Availability
- **Endpoint:** `GET /api/booking/check-availability?vehicleId={id}&startTime={time}&endTime={time}`
- **Service:** `IBookingService.CheckVehicleAvailabilityAsync()`

---

### 5. Booking Modification & Validation

#### Validate Modification
- **Endpoint:** `POST /api/booking/validate-modification`
- **Service:** `IBookingService.ValidateModificationAsync()`

#### Get Modification History
- **Endpoint:** `GET /api/booking/modification-history?bookingId={id}&userId={id}&vehicleId={id}`
- **Service:** `IBookingService.GetModificationHistoryAsync()`

---

### 6. Check-in/Check-out History

#### Get Booking Check-in/Check-out History
- **Endpoint:** `GET /api/checkin/history/{bookingId}`
- **Service:** `ICheckInCheckOutService.GetBookingCheckInOutHistoryAsync()`

---

### 7. Vehicle Management

#### Get Available Vehicles
- **Endpoint:** `GET /api/vehicle/available?page={page}&pageSize={size}&status={status}&brand={brand}&model={model}`
- **Service:** `IVehicleService.GetAvailableVehiclesAsync()`

#### Get Vehicle Detail
- **Endpoint:** `GET /api/vehicle/{vehicleId}/detail`
- **Service:** `IVehicleService.GetVehicleDetailAsync()`

#### Get Vehicle Availability Schedule
- **Endpoint:** `GET /api/vehicle/{vehicleId}/availability?startDate={date}&endDate={date}&statusFilter={status}`
- **Service:** `IVehicleService.GetVehicleAvailabilityScheduleAsync()`

#### Find Available Time Slots
- **Endpoint:** `GET /api/vehicle/{vehicleId}/available-slots?startDate={date}&endDate={date}&minimumDurationHours={hours}&fullDayOnly={bool}`
- **Service:** `IVehicleService.FindAvailableTimeSlotsAsync()`

#### Compare Vehicle Utilization
- **Endpoint:** `GET /api/vehicle/compare-utilization?startDate={date}&endDate={date}`
- **Service:** `IVehicleService.CompareVehicleUtilizationAsync()`

---

## 🔐 Authorization & Security

### Role-based Access

| Endpoint Category | Co-owner | Staff | Admin |
|------------------|----------|-------|-------|
| View Schedule | ✅ (Own vehicles) | ✅ (All) | ✅ (All) |
| Create Booking | ✅ | ❌ | ✅ |
| Check-in (QR) | ✅ | ❌ | ✅ |
| Check-in (Manual) | ❌ | ✅ | ✅ |
| Check-out (QR) | ✅ | ❌ | ✅ |
| Check-out (Manual) | ❌ | ✅ | ✅ |
| View Analytics | ✅ (Own data) | ✅ (All) | ✅ (All) |
| Conflict Resolution | ✅ | ✅ | ✅ |

### Authentication
Tất cả endpoints yêu cầu JWT token:
```
Authorization: Bearer {jwt_token}
```

---

## 📝 Database Models

### Booking
```csharp
public class Booking
{
    public int Id { get; set; }
    public int CoOwnerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Purpose { get; set; }
    public EBookingStatus Status { get; set; }
    public int? ApprovedBy { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### CheckIn
```csharp
public class CheckIn
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public int? StaffId { get; set; }
    public int? VehicleStationId { get; set; }
    public int? VehicleConditionId { get; set; }
    public DateTime CheckTime { get; set; }
}
```

### CheckOut
```csharp
public class CheckOut
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public int? StaffId { get; set; }
    public int? VehicleStationId { get; set; }
    public int? VehicleConditionId { get; set; }
    public DateTime CheckTime { get; set; }
}
```

### VehicleCoOwner
```csharp
public class VehicleCoOwner
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public int UserId { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public decimal InvestmentAmount { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

---

## 🎯 Business Rules Summary

### Booking Rules
1. ✅ Co-owner phải sở hữu ít nhất 1% xe để được đặt
2. ✅ Không được đặt booking trùng thời gian với booking khác
3. ✅ Đặt booking tối đa 30 ngày trước
4. ✅ Hủy booking phải trước 24h để tránh phí phạt
5. ✅ Usage không được vượt quá 150% ownership percentage trong tháng

### Check-in Rules
1. ✅ Check-in chỉ được phép trong khoảng ±15 phút từ booking start time
2. ✅ Booking phải ở trạng thái `Confirmed` hoặc `Approved`
3. ✅ Phải báo cáo vehicle condition trước khi check-in
4. ✅ QR code chỉ valid trong 30 phút

### Check-out Rules
1. ✅ Check-out phải sau check-in
2. ✅ Bắt buộc báo cáo vehicle condition và damages (nếu có)
3. ✅ Odometer reading phải lớn hơn lúc check-in
4. ✅ Late check-out (sau booking end time) sẽ tính phí phạt
5. ✅ Damage charges được tính tự động dựa trên severity

---

## 🚀 Advanced Features

### 1. Booking Slot Request System
- Đặt xe với multiple alternative time slots
- Auto-confirmation nếu không có conflict
- Priority-based booking (High, Medium, Low, Urgent)
- Flexible booking với system suggestions

### 2. Conflict Resolution
- Automatic conflict detection
- Ownership-weighted priority
- Usage fairness calculation
- Counter-offer mechanism
- Auto-negotiation based on rules

### 3. Usage Analytics
- Real-time usage tracking
- Usage vs Ownership comparison
- Fairness score calculation
- Trend analysis (Daily/Weekly/Monthly)
- Multi-vehicle comparison
- Co-owner comparison
- Period-to-period comparison

### 4. QR Code System
- Secure QR code generation with hash
- Time-limited QR validity
- Location verification (GPS)
- Self-service check-in/check-out

---

## 📈 Performance Considerations

### Caching Strategy
- Vehicle schedules: Cache 5 minutes
- User bookings: Cache 2 minutes
- Analytics: Cache 15 minutes

### Database Optimization
- Index on `Booking(VehicleId, StartTime, EndTime, Status)`
- Index on `VehicleCoOwner(UserId, VehicleId)`
- Index on `CheckIn(BookingId)`, `CheckOut(BookingId)`

---

## 🔍 Error Handling

### Common Error Codes

| Code | Message | Description |
|------|---------|-------------|
| 200 | SUCCESS | Operation successful |
| 400 | VALIDATION_ERROR | Input validation failed |
| 403 | NOT_CO_OWNER | User không phải co-owner |
| 403 | USAGE_QUOTA_EXCEEDED | Vượt quá quota sử dụng |
| 404 | BOOKING_NOT_FOUND | Không tìm thấy booking |
| 409 | BOOKING_CONFLICT | Xung đột lịch đặt xe |
| 409 | ALREADY_CHECKED_IN | Đã check-in rồi |
| 500 | INTERNAL_SERVER_ERROR | Lỗi hệ thống |

---

## 📚 Related Documentation

- [Booking DTOs](EvCoOwnership.Repositories/DTOs/BookingDTOs/)
- [Check-in/Check-out DTOs](EvCoOwnership.Repositories/DTOs/CheckInCheckOutDTOs/)
- [Schedule DTOs](EvCoOwnership.Repositories/DTOs/ScheduleDTOs/)
- [Usage Analytics DTOs](EvCoOwnership.Repositories/DTOs/UsageAnalyticsDTOs/)
- [Profile Page Documentation](EvCoOwnership.API/Documentation/PROFILE_PAGE_DOCUMENTATION.md)

---

## 📌 Key Services & Interfaces

### Service Layer
- `IBookingService` - Booking management
- `IScheduleService` - Schedule & availability
- `ICheckInCheckOutService` - Check-in/check-out operations
- `IUsageAnalyticsService` - Usage analytics & reporting
- `IVehicleService` - Vehicle management

### Controllers
- `CoOwnerController` - Co-owner specific endpoints
- `StaffController` - Staff operations
- `GroupController` - Group & vehicle management

---

**Generated:** 2024-01-15  
**Version:** 1.0  
**Author:** GitHub Copilot Analysis
