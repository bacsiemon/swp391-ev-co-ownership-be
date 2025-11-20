# Staff API Analysis - Missing Backend Endpoints

## Overview
Analysis of missing Staff APIs based on frontend client code compared to existing backend implementation in `StaffController.cs`.

## Frontend vs Backend API Comparison

### ✅ IMPLEMENTED APIs

| Frontend Endpoint | Backend Status | Implementation |
|------------------|----------------|----------------|
| `GET /staff/groups` | ✅ Implemented | `GetGroups()` |
| `GET /staff/groups/{id}` | ✅ Implemented | `GetGroup()` |
| `GET /staff/groups/{groupId}/members` | ✅ Implemented | Group management section |
| `GET /staff/groups/{groupId}/vehicles` | ✅ Implemented | Group management section |
| `GET /staff/groups/{groupId}/bookings` | ⚠️ Partial | Limited implementation |
| `GET /staff/contracts` | ✅ Implemented | `GetContracts()` |
| `GET /staff/contracts/{id}` | ⚠️ Partial | Mock implementation |
| `POST /staff/check-in` | ✅ Implemented | `CheckIn()` |
| `POST /staff/check-out` | ✅ Implemented | `CheckOut()` |
| `GET /staff/check-ins/pending` | ✅ Implemented | `GetPendingCheckIns()` |
| `GET /staff/maintenance` | ✅ Implemented | `GetMaintenanceRequests()` |
| `GET /staff/disputes` | ✅ Implemented | `GetDisputes()` |
| `GET /staff/profile` | ✅ Implemented | Profile management section |
| `GET /staff/dashboard` | ⚠️ Partial | Basic reports only |

### ❌ MISSING APIs (Critical)

#### 1. Contract Management APIs
```javascript
// Missing endpoints:
contracts: {
  create: (contractData) => axiosClient.post('/staff/contracts', contractData),
  update: (id, updates) => axiosClient.patch(`/staff/contracts/${id}`, updates),
  approve: (id, notes) => axiosClient.patch(`/staff/contracts/${id}/approve`, { notes }),
  reject: (id, reason) => axiosClient.patch(`/staff/contracts/${id}/reject`, { reason }),
  getTemplate: (type) => axiosClient.get(`/staff/contracts/template/${type}`),
  generateContract: (contractRequest) => axiosClient.post('/staff/contracts/generate', contractRequest)
}
```

#### 2. Check-in/Check-out Management APIs
```javascript
// Missing endpoints:
checkInOut: {
  getPendingCheckOuts: () => axiosClient.get('/staff/check-outs/pending'),
  getCheckInHistory: (vehicleId, page = 1) => axiosClient.get(`/staff/check-ins/history?vehicleId=${vehicleId}&page=${page}`),
  approveCheckIn: (checkInId, notes) => axiosClient.patch(`/staff/check-ins/${checkInId}/approve`, { notes }),
  rejectCheckIn: (checkInId, reason) => axiosClient.patch(`/staff/check-ins/${checkInId}/reject`, { reason }),
  approveCheckOut: (checkOutId, notes) => axiosClient.patch(`/staff/check-outs/${checkOutId}/approve`, { notes }),
  rejectCheckOut: (checkOutId, reason) => axiosClient.patch(`/staff/check-outs/${checkOutId}/reject`, { reason })
}
```

#### 3. Maintenance Management APIs
```javascript
// Missing endpoints:
maintenance: {
  getTaskById: (taskId) => axiosClient.get(`/staff/maintenance/${taskId}`),
  createTask: (taskData) => axiosClient.post('/staff/maintenance', taskData),
  assignTask: (taskId, staffId) => axiosClient.patch(`/staff/maintenance/${taskId}/assign`, { staffId }),
  updateStatus: (taskId, status, notes) => axiosClient.patch(`/staff/maintenance/${taskId}/status`, { status, notes }),
  createReport: (report) => axiosClient.post('/staff/maintenance/report', report),
  getMaintenanceHistory: (vehicleId) => axiosClient.get(`/staff/maintenance/history/${vehicleId}`),
  scheduleMaintenace: (scheduleData) => axiosClient.post('/staff/maintenance/schedule', scheduleData)
}
```

#### 4. Vehicle Management APIs
```javascript
// Missing endpoints:
vehicles: {
  getAll: (params = {}) => axiosClient.get(`/staff/vehicles?${queryParams}`),
  getById: (vehicleId) => axiosClient.get(`/staff/vehicles/${vehicleId}`),
  verify: (vehicleId, verificationData) => axiosClient.post(`/staff/vehicles/${vehicleId}/verify`, verificationData),
  approve: (vehicleId, notes) => axiosClient.patch(`/staff/vehicles/${vehicleId}/approve`, { notes }),
  reject: (vehicleId, reason) => axiosClient.patch(`/staff/vehicles/${vehicleId}/reject`, { reason }),
  updateStatus: (vehicleId, status, notes) => axiosClient.patch(`/staff/vehicles/${vehicleId}/status`, { status, notes }),
  getInspectionHistory: (vehicleId) => axiosClient.get(`/staff/vehicles/${vehicleId}/inspections`)
}
```

#### 5. Dispute Management APIs
```javascript
// Missing endpoints:
disputes: {
  getById: (disputeId) => axiosClient.get(`/staff/disputes/${disputeId}`),
  assign: (disputeId, staffId) => axiosClient.patch(`/staff/disputes/${disputeId}/assign`, { staffId }),
  updateStatus: (disputeId, status, notes) => axiosClient.patch(`/staff/disputes/${disputeId}/status`, { status, notes }),
  resolve: (disputeId, resolution) => axiosClient.post(`/staff/disputes/${disputeId}/resolve`, resolution),
  escalate: (disputeId, reason) => axiosClient.post(`/staff/disputes/${disputeId}/escalate`, { reason }),
  addNote: (disputeId, note) => axiosClient.post(`/staff/disputes/${disputeId}/notes`, { note }),
  getDisputeHistory: (disputeId) => axiosClient.get(`/staff/disputes/${disputeId}/history`)
}
```

#### 6. Profile Management APIs
```javascript
// Missing endpoints:
profile: {
  update: (profileData) => axiosClient.put('/staff/profile', profileData),
  changePassword: (passwordData) => axiosClient.post('/staff/profile/change-password', passwordData),
  getWorkSchedule: () => axiosClient.get('/staff/profile/work-schedule'),
  updateWorkSchedule: (schedule) => axiosClient.put('/staff/profile/work-schedule', schedule),
  getPerformanceMetrics: () => axiosClient.get('/staff/profile/performance'),
  getAssignedTasks: () => axiosClient.get('/staff/profile/assigned-tasks')
}
```

#### 7. Dashboard APIs
```javascript
// Missing endpoints:
dashboard: {
  getData: () => axiosClient.get('/staff/dashboard'),
  getWorkload: () => axiosClient.get('/staff/dashboard/workload'),
  getRecentActivity: () => axiosClient.get('/staff/dashboard/recent-activity'),
  getStats: () => axiosClient.get('/staff/dashboard/stats')
}
```

## Priority Assessment

### 🔴 HIGH PRIORITY (Essential for functionality)
1. **Vehicle Management APIs** - Core functionality for staff to manage vehicle approvals
2. **Dashboard APIs** - Critical for staff overview and daily operations
3. **Complete Contract Management** - Important for legal compliance
4. **Maintenance Task Management** - Essential for vehicle maintenance workflow

### 🟡 MEDIUM PRIORITY (Important features)
1. **Enhanced Check-in/Check-out Management** - Approval workflows
2. **Complete Dispute Resolution** - Full dispute management lifecycle
3. **Profile and Work Schedule Management** - Staff productivity features

### 🟢 LOW PRIORITY (Nice to have)
1. **Performance Metrics** - Analytics and reporting
2. **Advanced Reporting** - Detailed analytics

## Database Impact

Most missing APIs can be implemented using existing database entities:
- Vehicle management: `Vehicle`, `VehicleCoOwner` tables
- Maintenance: `MaintenanceCost` table (expand if needed)
- Disputes: May need new `Dispute`, `DisputeMessage` tables
- Contracts: May need new `Contract` table
- Dashboard: Aggregate data from existing tables

## Implementation Recommendations

1. **Start with Vehicle Management APIs** - Most critical missing functionality
2. **Add Dashboard APIs** - Essential for staff daily operations  
3. **Implement remaining Contract Management** - Complete the existing partial implementation
4. **Expand Maintenance Management** - Add task assignment and detailed workflow
5. **Complete Dispute Management** - Add full resolution workflow
6. **Add Profile and Work Schedule features** - Staff productivity improvements

## Next Steps

1. Implement Vehicle Management APIs (7 endpoints)
2. Add Dashboard APIs (4 endpoints)  
3. Complete Contract Management (5 remaining endpoints)
4. Expand Maintenance Management (6 additional endpoints)
5. Enhance Dispute Management (6 additional endpoints)
6. Add Profile Management features (5 endpoints)

Total Missing: **33 critical API endpoints** that need implementation to match frontend expectations.