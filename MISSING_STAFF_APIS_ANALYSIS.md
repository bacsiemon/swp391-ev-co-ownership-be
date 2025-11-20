# Staff API Gap Analysis

## Overview
This document analyzes the gap between the frontend Staff API requirements and the current backend implementation.

## Frontend API Requirements vs Backend Implementation

### ✅ IMPLEMENTED APIs

#### Group Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `groups.getAll()` | ✅ Implemented | `GET /api/staff/groups` |
| `groups.getById()` | ✅ Implemented | `GET /api/staff/group/{id}` |
| `groups.getGroupMembers()` | ✅ Implemented | `GET /api/staff/groups/assigned` |
| `groups.getGroupVehicles()` | ✅ Implemented | `GET /api/staff/group/{groupId}/details` |
| `groups.getGroupBookings()` | ❌ Missing | - |

#### Contract Management  
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `contracts.getAll()` | ✅ Implemented | `GET /api/staff/contracts` |
| `contracts.getById()` | ❌ Missing | - |
| `contracts.create()` | ❌ Missing | - |
| `contracts.update()` | ✅ Partial | `PATCH /api/staff/contract/{id}/status` |
| `contracts.approve()` | ❌ Missing | - |
| `contracts.reject()` | ❌ Missing | - |
| `contracts.getTemplate()` | ❌ Missing | - |
| `contracts.generateContract()` | ❌ Missing | - |

#### Check-in/Check-out Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `checkInOut.checkIn()` | ✅ Implemented | `POST /api/staff/checkin` |
| `checkInOut.checkOut()` | ✅ Implemented | `POST /api/staff/checkout` |
| `checkInOut.getPendingCheckIns()` | ✅ Implemented | `GET /api/staff/checkins/pending` |
| `checkInOut.getPendingCheckOuts()` | ❌ Missing | - |
| `checkInOut.getCheckInHistory()` | ❌ Missing | - |
| `checkInOut.approveCheckIn()` | ❌ Missing | - |
| `checkInOut.rejectCheckIn()` | ❌ Missing | - |
| `checkInOut.approveCheckOut()` | ❌ Missing | - |
| `checkInOut.rejectCheckOut()` | ❌ Missing | - |

#### Maintenance Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `maintenance.getTasks()` | ✅ Implemented | `GET /api/staff/services` |
| `maintenance.getTaskById()` | ❌ Missing | - |
| `maintenance.createTask()` | ✅ Implemented | `POST /api/staff/service` |
| `maintenance.assignTask()` | ❌ Missing | - |
| `maintenance.updateStatus()` | ✅ Implemented | `PATCH /api/staff/service/{id}/status` |
| `maintenance.createReport()` | ❌ Missing | - |
| `maintenance.getMaintenanceHistory()` | ❌ Missing | - |
| `maintenance.scheduleMaintenace()` | ❌ Missing | - |

#### Dispute Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `disputes.getAll()` | ✅ Implemented | `GET /api/staff/disputes` |
| `disputes.getById()` | ❌ Missing | - |
| `disputes.assign()` | ❌ Missing | - |
| `disputes.updateStatus()` | ✅ Implemented | `PATCH /api/staff/dispute/{id}/status` |
| `disputes.resolve()` | ❌ Missing | - |
| `disputes.escalate()` | ❌ Missing | - |
| `disputes.addNote()` | ✅ Implemented | `POST /api/staff/dispute/{disputeId}/message` |
| `disputes.getDisputeHistory()` | ❌ Missing | - |

#### Vehicle Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `vehicles.getAll()` | ❌ Missing | - |
| `vehicles.getById()` | ❌ Missing | - |
| `vehicles.verify()` | ❌ Missing | - |
| `vehicles.approve()` | ❌ Missing | - |
| `vehicles.reject()` | ❌ Missing | - |
| `vehicles.updateStatus()` | ❌ Missing | - |
| `vehicles.getInspectionHistory()` | ❌ Missing | - |

#### Profile Management
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `profile.get()` | ✅ Implemented | `GET /api/staff/profile` |
| `profile.update()` | ✅ Implemented | `PUT /api/staff/profile` |
| `profile.changePassword()` | ✅ Implemented | `POST /api/staff/profile/change-password` |
| `profile.getWorkSchedule()` | ❌ Missing | - |
| `profile.updateWorkSchedule()` | ❌ Missing | - |
| `profile.getPerformanceMetrics()` | ❌ Missing | - |
| `profile.getAssignedTasks()` | ❌ Missing | - |

#### Dashboard
| Frontend API | Backend Status | Endpoint |
|--------------|---------------|----------|
| `dashboard.getData()` | ❌ Missing | - |
| `dashboard.getWorkload()` | ❌ Missing | - |
| `dashboard.getRecentActivity()` | ❌ Missing | - |
| `dashboard.getStats()` | ❌ Missing | - |

---

## 🔴 CRITICAL MISSING APIs (High Priority)

### 1. Vehicle Management APIs - **CRITICAL**
```typescript
// These are essential for vehicle verification and management
vehicles.getAll(params)           // List all vehicles for staff oversight
vehicles.getById(vehicleId)       // Get detailed vehicle information  
vehicles.verify(vehicleId, data)  // Verify vehicle documentation
vehicles.approve(vehicleId)       // Approve vehicle for platform
vehicles.reject(vehicleId)        // Reject vehicle with reasons
vehicles.updateStatus(vehicleId)  // Update vehicle operational status
```

### 2. Dashboard APIs - **HIGH PRIORITY**
```typescript
// Essential for staff dashboard functionality
dashboard.getData()               // Main dashboard data aggregation
dashboard.getWorkload()          // Staff workload and task counts
dashboard.getRecentActivity()    // Recent staff activities
dashboard.getStats()             // Performance statistics
```

### 3. Contract Management APIs - **HIGH PRIORITY**
```typescript
// Critical for legal document management
contracts.getById(id)            // Get specific contract details
contracts.create(data)           // Create new contracts
contracts.approve(id)            // Approve contracts
contracts.reject(id)             // Reject contracts
contracts.getTemplate(type)      // Get contract templates
contracts.generateContract(data) // Generate contracts
```

---

## 🟡 MEDIUM PRIORITY APIs

### 4. Enhanced Check-in/Check-out
```typescript
checkInOut.getPendingCheckOuts()     // Pending checkout requests
checkInOut.getCheckInHistory()       // Historical check-in data
checkInOut.approveCheckIn(id)        // Approve check-in requests
checkInOut.rejectCheckIn(id)         // Reject check-in requests
checkInOut.approveCheckOut(id)       // Approve check-out requests
checkInOut.rejectCheckOut(id)        // Reject check-out requests
```

### 5. Enhanced Maintenance Management
```typescript
maintenance.getTaskById(id)          // Get specific maintenance task
maintenance.assignTask(id, staffId)  // Assign tasks to staff
maintenance.createReport(report)     // Create maintenance reports
maintenance.getMaintenanceHistory()  // Vehicle maintenance history
maintenance.scheduleMaintenace()     // Schedule maintenance tasks
```

### 6. Enhanced Dispute Management
```typescript
disputes.getById(id)                 // Get specific dispute details
disputes.assign(id, staffId)         // Assign dispute to staff
disputes.resolve(id, resolution)     // Resolve disputes
disputes.escalate(id, reason)        // Escalate disputes
disputes.getDisputeHistory(id)       // Dispute resolution history
```

---

## 🟢 LOW PRIORITY APIs

### 7. Profile Enhancement
```typescript
profile.getWorkSchedule()            // Staff work schedules
profile.updateWorkSchedule()         // Update work schedules
profile.getPerformanceMetrics()      // Performance analytics
profile.getAssignedTasks()          // Current task assignments
```

### 8. Group Management Enhancement
```typescript
groups.getGroupBookings(id, status)  // Group booking management
```

---

## Implementation Recommendations

### Phase 1: Critical Vehicle Management (Immediate)
1. **Vehicle APIs** - Essential for platform operation
   - Vehicle listing and search
   - Vehicle verification workflow
   - Vehicle status management
   - Vehicle inspection history

### Phase 2: Dashboard & Contract Management (High Priority)
1. **Dashboard APIs** - Essential for staff productivity
   - Workload management
   - Performance metrics
   - Activity tracking
2. **Contract APIs** - Legal compliance
   - Contract generation and management
   - Approval workflows

### Phase 3: Enhanced Operations (Medium Priority)
1. **Enhanced Check-in/Check-out** - Operational efficiency
2. **Enhanced Maintenance** - Vehicle maintenance tracking
3. **Enhanced Disputes** - Customer service improvement

### Phase 4: Staff Experience (Low Priority)  
1. **Profile Enhancement** - Staff productivity tools
2. **Group Management Enhancement** - Administrative features

---

## Technical Implementation Notes

### Database Integration
- Use existing repository pattern with UnitOfWork
- Leverage existing entities: Vehicle, DrivingLicense, User, etc.
- Follow established authentication and authorization patterns

### Response Format
- Use consistent BaseResponse<T> wrapper
- Implement proper error handling and logging
- Follow existing XML documentation standards

### Service Layer
- Create or extend existing services: IVehicleService, IDashboardService
- Implement proper business logic separation
- Use dependency injection patterns

---

## Estimated Development Effort

| Phase | APIs Count | Estimated Hours | Priority |
|-------|------------|-----------------|----------|
| Phase 1 - Vehicle Management | 7 APIs | 16-20 hours | CRITICAL |
| Phase 2 - Dashboard & Contracts | 11 APIs | 24-30 hours | HIGH |
| Phase 3 - Enhanced Operations | 15 APIs | 30-40 hours | MEDIUM |
| Phase 4 - Staff Experience | 5 APIs | 10-15 hours | LOW |

**Total Missing APIs: 38**
**Total Estimated Effort: 80-105 hours**