# Vehicle Comprehensive Filtering Feature

## Overview
Enhanced the vehicle filtering system with comprehensive search, filter, and sort capabilities for all 3 roles (Co-owner, Staff, Admin).

## Implementation Details

### **API Endpoint**
```
GET /api/vehicle/available
```

### **Authorization**
- **Co-owner**: Can only see vehicles in their co-ownership groups
- **Staff/Admin**: Can see ALL vehicles in the system

### **Filter Parameters**

#### **Basic Filters (Existing)**
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `pageIndex` | int | Page number (default: 1) | `1` |
| `pageSize` | int | Items per page (default: 10, max: 50) | `20` |
| `status` | string | Vehicle status filter | `Available`, `InUse`, `Maintenance`, `Unavailable` |
| `verificationStatus` | string | Verification status filter | `Pending`, `Verified`, `Rejected` |

#### **NEW - Advanced Filters**
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `brand` | string | Brand filter (partial match, case-insensitive) | `VinFast`, `Tesla`, `BMW` |
| `model` | string | Model filter (partial match, case-insensitive) | `VF8`, `Model 3`, `i4` |
| `minYear` | int? | Minimum manufacturing year | `2020` |
| `maxYear` | int? | Maximum manufacturing year | `2024` |
| `minPrice` | decimal? | Minimum purchase price (VND) | `500000000` (500M VND) |
| `maxPrice` | decimal? | Maximum purchase price (VND) | `2000000000` (2B VND) |
| `search` | string | Multi-field keyword search | `VF8`, `51H-12345` |
| `sortBy` | string | Sort field | `name`, `brand`, `model`, `year`, `price`, `createdAt` |
| `sortDesc` | bool | Sort direction (default: true) | `true` (descending), `false` (ascending) |

### **Search Fields**
The `search` parameter performs case-insensitive keyword search across:
- Vehicle Name
- Brand
- Model
- VIN (Vehicle Identification Number)
- License Plate

### **Default Behavior**
When no filters are provided:
- Status: `Available` only
- VerificationStatus: `Verified` only
- Sorting: `createdAt` descending (newest first)

### **Sorting Options**
| Sort Field | Description | Example Use Case |
|------------|-------------|------------------|
| `name` | Vehicle name | Alphabetical browsing |
| `brand` | Brand name | Group by manufacturer |
| `model` | Model name | Find specific models |
| `year` | Manufacturing year | Find newest/oldest vehicles |
| `price` | Purchase price | Budget-based search |
| `createdAt` | Date added (default) | Latest additions first |

## Example Usage

### **1. Basic Request (Default Filters)**
```
GET /api/vehicle/available
```
Returns: Available + Verified vehicles, newest first

### **2. Filter by Brand**
```
GET /api/vehicle/available?brand=VinFast
```
Returns: All VinFast vehicles in user's scope

### **3. Filter by Model**
```
GET /api/vehicle/available?model=VF8
```
Returns: All VF8 models

### **4. Price Range Filter**
```
GET /api/vehicle/available?minPrice=500000000&maxPrice=2000000000
```
Returns: Vehicles priced between 500M - 2B VND

### **5. Year Range Filter**
```
GET /api/vehicle/available?minYear=2020&maxYear=2024
```
Returns: Vehicles manufactured from 2020 to 2024

### **6. Combined Filters**
```
GET /api/vehicle/available?brand=Tesla&minYear=2022&status=Available
```
Returns: Available Tesla vehicles from 2022 onwards

### **7. Keyword Search**
```
GET /api/vehicle/available?search=VF8
```
Returns: All vehicles matching "VF8" in name, brand, model, VIN, or license plate

### **8. Search by VIN**
```
GET /api/vehicle/available?search=VF8CS1234567890
```
Returns: Vehicle with matching VIN

### **9. Search by License Plate**
```
GET /api/vehicle/available?search=51H-12345
```
Returns: Vehicle with matching license plate

### **10. Sort by Price (Ascending)**
```
GET /api/vehicle/available?sortBy=price&sortDesc=false
```
Returns: Vehicles sorted by price, cheapest first

### **11. Sort by Year (Newest First)**
```
GET /api/vehicle/available?sortBy=year&sortDesc=true
```
Returns: Vehicles sorted by year, newest first

### **12. Complex Query**
```
GET /api/vehicle/available?brand=VinFast&minPrice=1000000000&maxPrice=3000000000&sortBy=price&sortDesc=false&pageSize=20
```
Returns: VinFast vehicles priced 1B-3B VND, sorted by price ascending, 20 items per page

### **13. Search + Year Range**
```
GET /api/vehicle/available?search=electric&minYear=2023&sortBy=createdAt
```
Returns: Vehicles matching "electric" keyword, manufactured 2023+, sorted by creation date

### **14. Staff/Admin: View All Maintenance Vehicles**
```
GET /api/vehicle/available?status=Maintenance&verificationStatus=Verified
```
Returns: All verified vehicles currently in maintenance (staff/admin only)

## Implementation Layers

### **1. Repository Layer** (VehicleRepository.cs)
```csharp
public async Task<(List<Vehicle> vehicles, int totalCount)> GetAllAvailableVehiclesAsync(
    int pageIndex, int pageSize,
    int? coOwnerId = null,
    EVehicleStatus? statusFilter = null,
    EVehicleVerificationStatus? verificationStatusFilter = null,
    string? brandFilter = null,
    string? modelFilter = null,
    int? minYear = null, int? maxYear = null,
    decimal? minPrice = null, decimal? maxPrice = null,
    string? searchKeyword = null,
    string? sortBy = null,
    bool sortDescending = true)
```

**Filtering Logic:**
- Brand: Case-insensitive `Contains`
- Model: Case-insensitive `Contains`
- Year: Range filtering with `>=` and `<=`
- Price: Range filtering in VND
- Search: Multi-field OR query
- Sort: Switch expression for dynamic field selection

### **2. Service Layer** (VehicleService.cs)
```csharp
public async Task<BaseResponse<PagedResult<VehicleResponse>>> GetAvailableVehiclesAsync(
    int userId, int pageIndex, int pageSize,
    string? status = null, string? verificationStatus = null,
    string? brand = null, string? model = null,
    int? minYear = null, int? maxYear = null,
    decimal? minPrice = null, decimal? maxPrice = null,
    string? search = null, string? sortBy = null, bool sortDescending = true)
```

**Business Logic:**
- Role checking (CoOwner vs Staff/Admin)
- CoOwner: Filter by coOwnerId
- Staff/Admin: No role-based filtering
- Enum validation and parsing
- Response mapping to DTOs

### **3. Controller Layer** (VehicleController.cs)
```csharp
[HttpGet("available")]
[AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
public async Task<IActionResult> GetAvailableVehicles(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? status = null,
    [FromQuery] string? verificationStatus = null,
    [FromQuery] string? brand = null,
    [FromQuery] string? model = null,
    [FromQuery] int? minYear = null,
    [FromQuery] int? maxYear = null,
    [FromQuery] decimal? minPrice = null,
    [FromQuery] decimal? maxPrice = null,
    [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] bool sortDesc = true)
```

## Technical Implementation

### **Query Building (LINQ)**
```csharp
var query = _context.Vehicles
    .Include(v => v.VehicleCoOwners)
        .ThenInclude(vco => vco.CoOwner)
            .ThenInclude(co => co.User)
    .Include(v => v.Fund)
    .AsQueryable();

// Role-based filtering
if (coOwnerId.HasValue)
{
    query = query.Where(v => v.VehicleCoOwners.Any(vco => 
        vco.CoOwnerId == coOwnerId.Value && 
        vco.IsActive));
}

// Brand filter
if (!string.IsNullOrWhiteSpace(brandFilter))
    query = query.Where(v => v.Brand.ToLower().Contains(brandFilter.ToLower()));

// Model filter
if (!string.IsNullOrWhiteSpace(modelFilter))
    query = query.Where(v => v.Model.ToLower().Contains(modelFilter.ToLower()));

// Year range
if (minYear.HasValue)
    query = query.Where(v => v.Year >= minYear.Value);
if (maxYear.HasValue)
    query = query.Where(v => v.Year <= maxYear.Value);

// Price range
if (minPrice.HasValue)
    query = query.Where(v => v.PurchasePrice >= minPrice.Value);
if (maxPrice.HasValue)
    query = query.Where(v => v.PurchasePrice <= maxPrice.Value);

// Multi-field search
if (!string.IsNullOrWhiteSpace(searchKeyword))
{
    var keyword = searchKeyword.ToLower();
    query = query.Where(v =>
        v.Name.ToLower().Contains(keyword) ||
        v.Brand.ToLower().Contains(keyword) ||
        v.Model.ToLower().Contains(keyword) ||
        v.Vin.ToLower().Contains(keyword) ||
        v.LicensePlate.ToLower().Contains(keyword));
}

// Dynamic sorting
query = sortBy?.ToLower() switch
{
    "name" => sortDescending ? query.OrderByDescending(v => v.Name) : query.OrderBy(v => v.Name),
    "brand" => sortDescending ? query.OrderByDescending(v => v.Brand) : query.OrderBy(v => v.Brand),
    "model" => sortDescending ? query.OrderByDescending(v => v.Model) : query.OrderBy(v => v.Model),
    "year" => sortDescending ? query.OrderByDescending(v => v.Year) : query.OrderBy(v => v.Year),
    "price" => sortDescending ? query.OrderByDescending(v => v.PurchasePrice) : query.OrderBy(v => v.PurchasePrice),
    "createdat" => sortDescending ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt),
    _ => query.OrderByDescending(v => v.CreatedAt)
};
```

## Response Format

### **Success Response (200 OK)**
```json
{
  "statusCode": 200,
  "message": "AVAILABLE_VEHICLES_RETRIEVED_SUCCESSFULLY",
  "data": {
    "items": [
      {
        "vehicleId": 1,
        "name": "VinFast VF8 Premium",
        "brand": "VinFast",
        "model": "VF8",
        "year": 2023,
        "purchasePrice": 1200000000,
        "licensePlate": "51H-12345",
        "vin": "VF8CS1234567890",
        "status": "Available",
        "verificationStatus": "Verified",
        "coOwners": [
          {
            "coOwnerId": 1,
            "userId": 10,
            "fullName": "Nguyen Van A",
            "ownershipPercentage": 50.00
          }
        ],
        "availableOwnershipPercentage": 50.00,
        "currentLatitude": 10.7756,
        "currentLongitude": 106.7019
      }
    ],
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

## Use Cases by Role

### **Co-owner Use Cases**
1. **Browse group vehicles**: View all vehicles in my co-ownership groups
2. **Find vehicles by brand**: Filter to find specific brands I co-own
3. **Price-based search**: Find vehicles in my budget for additional investment
4. **Keyword search**: Quick lookup by license plate or VIN
5. **Sort by year**: Find newest electric vehicles in my groups
6. **Combined filters**: Brand + year + status for specific needs

### **Staff/Admin Use Cases**
1. **Platform overview**: View all vehicles across all groups
2. **Maintenance tracking**: Filter vehicles by maintenance status
3. **Brand analytics**: Filter by brand for fleet analysis
4. **Price range reports**: Generate reports for specific price ranges
5. **Verification management**: Filter by verification status
6. **Age analysis**: Filter by year range for fleet age statistics
7. **Search operations**: Quick lookup by VIN/license plate for support

## Performance Considerations

### **Optimization Techniques**
1. **Indexed Fields**: Brand, Model, Year, PurchasePrice, Status, VerificationStatus
2. **Pagination**: Limits result set size (max 50 items)
3. **Case-Insensitive Search**: Uses `ToLower()` for consistent comparisons
4. **Optional Filters**: Only applies filters when values are provided
5. **Default Filters**: Status=Available, Verification=Verified reduces initial dataset

### **Query Efficiency**
- **Eager Loading**: Includes related entities (CoOwners, Fund) in single query
- **Filtered Loading**: Applies WHERE clauses before loading related data
- **Sorting**: Database-level sorting before pagination
- **Count Optimization**: Separate count query for total records

## Testing Recommendations

### **Filter Combinations to Test**
1. Single filter: Brand only
2. Single filter: Year range only
3. Single filter: Price range only
4. Two filters: Brand + Year
5. Two filters: Price + Status
6. Three filters: Brand + Year + Price
7. Search + Filter: Search keyword + Year range
8. Sort variations: Each sort field with asc/desc
9. Edge cases: Empty results, invalid sort field
10. Role-based: CoOwner vs Staff/Admin access

### **Performance Tests**
1. Large dataset (1000+ vehicles)
2. Complex filter combinations
3. Multi-field search performance
4. Pagination efficiency
5. Concurrent user requests

## Files Modified

1. **EvCoOwnership.Repositories/Interfaces/IVehicleRepository.cs**
   - Added 8 new parameters to `GetAllAvailableVehiclesAsync`

2. **EvCoOwnership.Repositories/Repositories/VehicleRepository.cs**
   - Implemented comprehensive filtering logic
   - Added brand, model, year, price, search filters
   - Added dynamic sorting with 6 sort options

3. **EvCoOwnership.Services/Interfaces/IVehicleService.cs**
   - Updated service interface with new parameters

4. **EvCoOwnership.Services/Services/VehicleService.cs**
   - Updated service implementation to pass all filters to repository

5. **EvCoOwnership.API/Controllers/VehicleController.cs**
   - Added 9 new query parameters to endpoint
   - Updated comprehensive XML documentation with examples
   - Updated service method call with all parameters

## Build Status
✅ **Build Successful** - All layers compile without errors  
✅ **XML Documentation** - All parameter warnings resolved  
✅ **Repository Layer** - Fully implemented with comprehensive filters  
✅ **Service Layer** - Business logic updated with role-based access  
✅ **Controller Layer** - API endpoint ready with complete documentation  

## Next Steps
1. ✅ **Complete** - Backend implementation (Repository, Service, Controller)
2. ⏭️ **Test** - Test comprehensive filtering with various combinations
3. ⏭️ **Frontend** - Implement UI filters (dropdowns, sliders, search box)
4. ⏭️ **Documentation** - Update API documentation for frontend team
5. ⏭️ **Performance** - Monitor query performance with real data

---
**Feature Status**: ✅ **COMPLETE - Ready for Testing**  
**Created**: January 16, 2025  
**Author**: GitHub Copilot Agent  
