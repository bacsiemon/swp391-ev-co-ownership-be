# Vehicle "View Available" Feature Documentation

## 📋 Tổng Quan

Tính năng **"View Available Vehicles"** cho phép Co-owner xem danh sách tất cả các xe điện đang available trong hệ thống để:
- Tìm kiếm xe để booking
- Tìm kiếm cơ hội đồng sở hữu (co-ownership)
- Xem thông tin chi tiết về xe và các co-owner hiện tại
- Lọc theo trạng thái và tình trạng xác minh

---

## 🎯 Tính Năng Chính

### 1. **Xem Danh Sách Xe Available**
- Pagination: Hỗ trợ phân trang (mặc định 10 items/page, max 50)
- Sorting: Sắp xếp theo ngày tạo (mới nhất trước)
- Filtering: Lọc theo status và verification status

### 2. **Thông Tin Hiển Thị**
Mỗi vehicle trong danh sách bao gồm:
- **Thông tin cơ bản**: Brand, Model, Year, License Plate, VIN
- **Thông số kỹ thuật**: Battery Capacity, Range (km), Color
- **Tài chính**: Purchase Price, Warranty Until
- **Co-ownership**: Danh sách co-owners + ownership percentage
- **Available ownership**: Tính % ownership còn lại (100% - total active ownership)
- **Location**: GPS coordinates (latitude, longitude)
- **Status**: Available, InUse, Maintenance, Unavailable
- **Verification**: Pending, Verified, Rejected, etc.

### 3. **Default Behavior**
Khi không có filter parameters:
- **Status filter**: Chỉ hiển thị `Available` vehicles
- **Verification filter**: Chỉ hiển thị `Verified` vehicles
- **Reason**: Đảm bảo user chỉ thấy xe đã được xác minh và sẵn sàng sử dụng

---

## 🔧 API Endpoint

### **GET `/api/vehicle/available`**

#### **Authorization**
- Roles: `CoOwner`, `Staff`
- Requires: Valid JWT token

#### **Query Parameters**

| Parameter | Type | Required | Default | Max | Description |
|-----------|------|----------|---------|-----|-------------|
| `pageIndex` | int | No | 1 | - | Số trang (bắt đầu từ 1) |
| `pageSize` | int | No | 10 | 50 | Số items mỗi trang |
| `status` | string | No | Available | - | Filter theo vehicle status |
| `verificationStatus` | string | No | Verified | - | Filter theo verification status |

#### **Vehicle Status Values**
```
Available    - Xe sẵn sàng cho booking
InUse        - Xe đang được sử dụng (có booking active)
Maintenance  - Xe đang bảo dưỡng/sửa chữa
Unavailable  - Xe không khả dụng (lý do khác)
```

#### **Verification Status Values**
```
Pending              - Đang chờ xác minh
VerificationRequested - Đã yêu cầu xác minh
RequiresRecheck      - Cần xác minh lại
Verified             - Đã xác minh (approved)
Rejected             - Bị từ chối
```

#### **Response Format**

**Success (200 OK)**
```json
{
  "statusCode": 200,
  "message": "AVAILABLE_VEHICLES_RETRIEVED_SUCCESSFULLY",
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Tesla Model 3 Standard Range",
        "description": "Electric sedan with autopilot",
        "brand": "Tesla",
        "model": "Model 3",
        "year": 2023,
        "vin": "5YJ3E1EA1KF123456",
        "licensePlate": "30A-12345",
        "color": "Pearl White",
        "batteryCapacity": 60.5,
        "rangeKm": 430,
        "purchaseDate": "2023-05-15",
        "purchasePrice": 1200000000,
        "warrantyUntil": "2026-05-15",
        "distanceTravelled": 12500,
        "status": "Available",
        "verificationStatus": "Verified",
        "locationLatitude": 10.762622,
        "locationLongitude": 106.660172,
        "createdAt": "2023-05-15T10:30:00Z",
        "coOwners": [
          {
            "coOwnerId": 1,
            "userId": 5,
            "firstName": "Nguyen",
            "lastName": "Van A",
            "email": "nguyenvana@example.com",
            "ownershipPercentage": 70.0,
            "investmentAmount": 840000000,
            "status": "Active",
            "createdAt": "2023-05-15T10:30:00Z"
          }
        ]
      }
    ],
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Error Responses**

```json
// 400 Bad Request - Invalid page index
{
  "message": "INVALID_PAGE_INDEX",
  "details": "Page index must be at least 1"
}

// 400 Bad Request - Invalid page size
{
  "message": "INVALID_PAGE_SIZE",
  "details": "Page size must be between 1 and 50"
}

// 401 Unauthorized
{
  "message": "INVALID_TOKEN"
}

// 500 Internal Server Error
{
  "statusCode": 500,
  "message": "INTERNAL_SERVER_ERROR"
}
```

---

## 💻 Example Usage

### **1. Get All Available & Verified Vehicles (Default)**
```bash
GET /api/vehicle/available
Authorization: Bearer <JWT_TOKEN>
```

### **2. Get All Vehicles (Including Maintenance, Unverified)**
```bash
GET /api/vehicle/available?status=&verificationStatus=
Authorization: Bearer <JWT_TOKEN>
```

### **3. Get Only Vehicles Under Maintenance**
```bash
GET /api/vehicle/available?status=Maintenance
Authorization: Bearer <JWT_TOKEN>
```

### **4. Get Pending Verification Vehicles**
```bash
GET /api/vehicle/available?verificationStatus=Pending
Authorization: Bearer <JWT_TOKEN>
```

### **5. Pagination Example**
```bash
GET /api/vehicle/available?pageIndex=2&pageSize=20
Authorization: Bearer <JWT_TOKEN>
```

### **6. Combined Filters**
```bash
GET /api/vehicle/available?status=Available&verificationStatus=Verified&pageIndex=1&pageSize=15
Authorization: Bearer <JWT_TOKEN>
```

---

## 🔍 Business Logic

### **Available Ownership Calculation**
Frontend có thể tính phần trăm ownership còn lại:

```javascript
const calculateAvailableOwnership = (vehicle) => {
  const totalActiveOwnership = vehicle.coOwners
    .filter(co => co.status === "Active")
    .reduce((sum, co) => sum + co.ownershipPercentage, 0);
  
  return 100 - totalActiveOwnership;
};

// Example
const vehicle = response.data.items[0];
const available = calculateAvailableOwnership(vehicle);
console.log(`Available ownership: ${available}%`); // 30%
```

### **Investment Amount Estimation**
Ước tính số tiền cần đầu tư cho % ownership mong muốn:

```javascript
const estimateInvestment = (vehicle, desiredPercentage) => {
  return (vehicle.purchasePrice * desiredPercentage) / 100;
};

// Example: Muốn sở hữu 30% của xe trị giá 1.2 tỷ
const investment = estimateInvestment(vehicle, 30);
console.log(`Estimated investment: ${investment.toLocaleString('vi-VN')} VND`);
// Output: 360,000,000 VND
```

### **Distance-Based Search** (Frontend Implementation)
Tính khoảng cách từ vị trí user đến vehicle:

```javascript
const calculateDistance = (lat1, lon1, lat2, lon2) => {
  const R = 6371; // Radius of the earth in km
  const dLat = deg2rad(lat2 - lat1);
  const dLon = deg2rad(lon2 - lon1);
  const a = 
    Math.sin(dLat/2) * Math.sin(dLat/2) +
    Math.cos(deg2rad(lat1)) * Math.cos(deg2rad(lat2)) *
    Math.sin(dLon/2) * Math.sin(dLon/2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
  return R * c; // Distance in km
};

const deg2rad = (deg) => deg * (Math.PI/180);

// Sort vehicles by distance
const sortByDistance = (vehicles, userLat, userLon) => {
  return vehicles
    .map(v => ({
      ...v,
      distance: calculateDistance(
        userLat, 
        userLon, 
        v.locationLatitude, 
        v.locationLongitude
      )
    }))
    .sort((a, b) => a.distance - b.distance);
};
```

---

## 🧪 Testing Scenarios

### **1. Test Default Behavior**
```bash
# Should only return Available + Verified vehicles
curl -X GET "https://localhost:7240/api/vehicle/available" \
  -H "Authorization: Bearer <TOKEN>"
```

**Expected**: Status = Available, VerificationStatus = Verified

### **2. Test Pagination**
```bash
# Get first page
curl -X GET "https://localhost:7240/api/vehicle/available?pageIndex=1&pageSize=5" \
  -H "Authorization: Bearer <TOKEN>"

# Get second page
curl -X GET "https://localhost:7240/api/vehicle/available?pageIndex=2&pageSize=5" \
  -H "Authorization: Bearer <TOKEN>"
```

**Expected**: Different items, correct totalCount

### **3. Test Filter by Status**
```bash
# Get vehicles in maintenance
curl -X GET "https://localhost:7240/api/vehicle/available?status=Maintenance" \
  -H "Authorization: Bearer <TOKEN>"
```

**Expected**: All items have status = "Maintenance"

### **4. Test Invalid Parameters**
```bash
# Invalid page index
curl -X GET "https://localhost:7240/api/vehicle/available?pageIndex=0" \
  -H "Authorization: Bearer <TOKEN>"

# Invalid page size
curl -X GET "https://localhost:7240/api/vehicle/available?pageSize=100" \
  -H "Authorization: Bearer <TOKEN>"
```

**Expected**: 400 Bad Request with appropriate error message

### **5. Test Unauthorized Access**
```bash
# No token
curl -X GET "https://localhost:7240/api/vehicle/available"

# Invalid role (if User role exists)
curl -X GET "https://localhost:7240/api/vehicle/available" \
  -H "Authorization: Bearer <USER_ROLE_TOKEN>"
```

**Expected**: 401 Unauthorized

---

## 📊 Database Query Performance

### **Optimizations Implemented**

1. **Eager Loading**: Includes co-owners and users in single query
   ```csharp
   .Include(v => v.VehicleCoOwners)
   .ThenInclude(vco => vco.CoOwner)
   .ThenInclude(co => co.User)
   ```

2. **Filtered Query**: Applies filters before pagination
   ```csharp
   query = query.Where(v => v.StatusEnum == EVehicleStatus.Available);
   query = query.Where(v => v.VerificationStatusEnum == EVehicleVerificationStatus.Verified);
   ```

3. **Pagination**: Uses Skip/Take for efficient paging
   ```csharp
   .Skip((pageIndex - 1) * pageSize)
   .Take(pageSize)
   ```

### **Recommended Indexes**

```sql
-- Index on status columns for faster filtering
CREATE INDEX idx_vehicle_status 
ON "Vehicle" ("StatusEnum", "VerificationStatusEnum");

-- Index on created_at for sorting
CREATE INDEX idx_vehicle_created_at 
ON "Vehicle" ("CreatedAt" DESC);

-- Composite index for common query pattern
CREATE INDEX idx_vehicle_status_created 
ON "Vehicle" ("StatusEnum", "VerificationStatusEnum", "CreatedAt" DESC);
```

---

## 🔐 Security Considerations

### **1. Authorization**
- Chỉ Co-owner và Staff mới có thể xem available vehicles
- User thường không thể truy cập (phải upgrade lên Co-owner)

### **2. Data Exposure**
- Không hiển thị sensitive data (email co-owners được mask nếu cần)
- VIN và License Plate được hiển thị để verify authenticity

### **3. Rate Limiting** (Recommended)
```csharp
// Add in Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## 📈 Use Cases

### **1. Co-owner tìm xe để đầu tư**
- Filter: `status=Available&verificationStatus=Verified`
- Check available ownership percentage
- Calculate investment amount based on desired ownership
- Contact existing co-owners via platform

### **2. Co-owner tìm xe để booking**
- Filter: `status=Available`
- Check location proximity
- View vehicle specs (range, battery capacity)
- Create booking request

### **3. Staff kiểm tra xe cần verify**
- Filter: `verificationStatus=Pending`
- Review vehicle details
- Perform verification process
- Update verification status

### **4. User browse available vehicles** (Future)
- Public endpoint với limited information
- No co-owner details exposed
- Read-only access for marketing purposes

---

## 🚀 Future Enhancements

### **1. Advanced Filtering**
```
- Filter by brand/model
- Filter by price range
- Filter by ownership availability (e.g., >30%)
- Filter by location radius (distance from user)
- Filter by battery capacity/range
```

### **2. Search Functionality**
```
- Full-text search on name, description
- Search by VIN or license plate
- Search by co-owner name
```

### **3. Sorting Options**
```
- Sort by price (low to high, high to low)
- Sort by distance from user
- Sort by available ownership percentage
- Sort by year (newest/oldest)
- Sort by range (highest/lowest)
```

### **4. Map View**
```
- Display vehicles on interactive map
- Cluster markers for nearby vehicles
- Show distance to user's location
- Filter by map bounds
```

### **5. Recommendations**
```
- ML-based vehicle recommendations
- Based on user's booking history
- Based on investment preferences
- Based on location patterns
```

---

## ✅ Checklist

### **Implementation Complete**
- ✅ Repository method: `GetAllAvailableVehiclesAsync()`
- ✅ Service method: `GetAvailableVehiclesAsync()`
- ✅ Controller endpoint: `GET /api/vehicle/available`
- ✅ Pagination support (PagedResult)
- ✅ Status filtering
- ✅ Verification status filtering
- ✅ Authorization (CoOwner, Staff roles)
- ✅ Error handling
- ✅ Logging
- ✅ XML documentation
- ✅ Response standardization (BaseResponse)
- ✅ Build successful (0 errors)

### **Testing Required**
- ⬜ Unit tests for repository method
- ⬜ Unit tests for service method
- ⬜ Integration tests for API endpoint
- ⬜ Load testing with large dataset
- ⬜ Authorization tests (different roles)
- ⬜ Pagination edge cases (empty results, last page, etc.)
- ⬜ Filter validation tests

### **Documentation Complete**
- ✅ API endpoint documentation
- ✅ Request/Response examples
- ✅ Error scenarios
- ✅ Business logic explanation
- ✅ Usage examples
- ✅ Testing scenarios

---

## 📞 Support

Nếu có vấn đề hoặc câu hỏi về tính năng này, vui lòng:
1. Kiểm tra log files trong `/logs` directory
2. Verify JWT token validity
3. Check database connection
4. Review Swagger documentation tại `/swagger`

**Build Status**: ✅ Success (0 errors, 92 warnings - only XML comments và nullable warnings)
