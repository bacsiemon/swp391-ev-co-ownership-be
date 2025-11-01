# Staff Features Integration Guide

## 🔑 Overview

This guide covers all staff-specific functionality for managing groups, bookings, check-in/check-out processes, maintenance, disputes, and operational tasks in the EV Co-Ownership platform.

## 🏗️ Staff Authentication

All staff endpoints require:
- **Authentication**: Bearer token
- **Authorization**: Staff role (role = 1)
- **Base URL**: `/api/staff`

### Staff Authorization Check
```typescript
// utils/staffAuth.ts
export const isStaff = (user: any): boolean => {
  return user?.role === 1; // Staff role
};

export const requireStaffRole = (user: any) => {
  if (!user || user.role !== 1) {
    throw new Error('Staff access required');
  }
};
```

## 🏢 Group Management

### 1. Get All Groups

**Endpoint**: `GET /api/staff/groups`

```typescript
interface GetGroupsParams {
  status?: string;
  pageIndex?: number;
  pageSize?: number;
}

interface StaffGroupResponse {
  id: number;
  name: string;
  description: string;
  memberCount: number;
  vehicleCount: number;
  status: string;
  createdDate: string;
  totalFunds: number;
  activeBookings: number;
  pendingDisputes: number;
}

export const staffGroupService = {
  async getAllGroups(params: GetGroupsParams = {}): Promise<BaseResponse<StaffGroupResponse[]>> {
    const queryParams = new URLSearchParams();
    if (params.status) queryParams.append('status', params.status);
    if (params.pageIndex) queryParams.append('pageIndex', params.pageIndex.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    
    return await apiClient.get(`/staff/groups?${queryParams}`);
  }
};

// component
const GroupManagement: React.FC = () => {
  const [groups, setGroups] = useState<StaffGroupResponse[]>([]);
  const [filter, setFilter] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const loadGroups = async () => {
    setLoading(true);
    try {
      const response = await staffGroupService.getAllGroups({
        status: filter || undefined,
        pageIndex: 1,
        pageSize: 20
      });
      
      if (response.statusCode === 200) {
        setGroups(response.data);
      }
    } catch (error) {
      console.error('Failed to load groups:', error);
      toast.error('Failed to load groups');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadGroups();
  }, [filter]);

  return (
    <div className="group-management">
      <div className="page-header">
        <h2>Group Management</h2>
        <select value={filter} onChange={(e) => setFilter(e.target.value)}>
          <option value="">All Groups</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="suspended">Suspended</option>
        </select>
      </div>
      
      {loading ? (
        <div className="loading">Loading groups...</div>
      ) : (
        <div className="groups-grid">
          {groups.map(group => (
            <div key={group.id} className="group-card">
              <div className="group-header">
                <h3>{group.name}</h3>
                <span className={`status-badge ${group.status.toLowerCase()}`}>
                  {group.status}
                </span>
              </div>
              
              <div className="group-stats">
                <div className="stat">
                  <span className="label">Members:</span>
                  <span className="value">{group.memberCount}</span>
                </div>
                <div className="stat">
                  <span className="label">Vehicles:</span>
                  <span className="value">{group.vehicleCount}</span>
                </div>
                <div className="stat">
                  <span className="label">Funds:</span>
                  <span className="value">{group.totalFunds.toLocaleString()} VND</span>
                </div>
                <div className="stat">
                  <span className="label">Active Bookings:</span>
                  <span className="value">{group.activeBookings}</span>
                </div>
              </div>
              
              <div className="group-actions">
                <button onClick={() => viewGroupDetails(group.id)}>
                  View Details
                </button>
                <button onClick={() => manageBookings(group.id)}>
                  Manage Bookings
                </button>
                {group.pendingDisputes > 0 && (
                  <button className="dispute-btn" onClick={() => viewDisputes(group.id)}>
                    Disputes ({group.pendingDisputes})
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
```

## 📝 Contract Management

### 1. Get Contracts

**Endpoint**: `GET /api/staff/contracts`

```typescript
interface ContractListParams {
  status?: 'draft' | 'pending' | 'active' | 'completed' | 'terminated';
  groupId?: number;
  pageIndex?: number;
  pageSize?: number;
}

interface ContractResponse {
  id: number;
  contractNumber: string;
  groupId: number;
  groupName: string;
  status: string;
  startDate: string;
  endDate: string;
  totalValue: number;
  signaturesCompleted: number;
  totalSignaturesRequired: number;
  createdDate: string;
  lastUpdated: string;
}

export const staffContractService = {
  async getContracts(params: ContractListParams = {}): Promise<BaseResponse<PaginatedResult<ContractResponse>>> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) {
        queryParams.append(key, value.toString());
      }
    });
    
    return await apiClient.get(`/staff/contracts?${queryParams}`);
  },

  async createContract(contractData: CreateContractRequest): Promise<BaseResponse<ContractResponse>> {
    return await apiClient.post('/staff/contracts', contractData);
  },

  async updateContract(id: number, updates: Partial<ContractResponse>): Promise<BaseResponse<ContractResponse>> {
    return await apiClient.patch(`/staff/contracts/${id}`, updates);
  }
};

// Contract management component
const ContractManagement: React.FC = () => {
  const [contracts, setContracts] = useState<ContractResponse[]>([]);
  const [filter, setFilter] = useState<ContractListParams>({});

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'green';
      case 'pending': return 'orange';
      case 'draft': return 'gray';
      case 'completed': return 'blue';
      case 'terminated': return 'red';
      default: return 'gray';
    }
  };

  return (
    <div className="contract-management">
      <div className="page-header">
        <h2>Contract Management</h2>
        <div className="filters">
          <select 
            value={filter.status || ''} 
            onChange={(e) => setFilter({...filter, status: e.target.value as any})}
          >
            <option value="">All Statuses</option>
            <option value="draft">Draft</option>
            <option value="pending">Pending</option>
            <option value="active">Active</option>
            <option value="completed">Completed</option>
            <option value="terminated">Terminated</option>
          </select>
          
          <button onClick={() => createNewContract()}>
            Create New Contract
          </button>
        </div>
      </div>
      
      <div className="contracts-table">
        <table>
          <thead>
            <tr>
              <th>Contract #</th>
              <th>Group</th>
              <th>Status</th>
              <th>Value</th>
              <th>Signatures</th>
              <th>Period</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {contracts.map(contract => (
              <tr key={contract.id}>
                <td>{contract.contractNumber}</td>
                <td>{contract.groupName}</td>
                <td>
                  <span className={`status-badge ${getStatusColor(contract.status)}`}>
                    {contract.status}
                  </span>
                </td>
                <td>{contract.totalValue.toLocaleString()} VND</td>
                <td>
                  {contract.signaturesCompleted}/{contract.totalSignaturesRequired}
                </td>
                <td>
                  {new Date(contract.startDate).toLocaleDateString()} - 
                  {new Date(contract.endDate).toLocaleDateString()}
                </td>
                <td>
                  <button onClick={() => viewContract(contract.id)}>View</button>
                  <button onClick={() => editContract(contract.id)}>Edit</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
```

## 🚗 Check-in/Check-out Management

### 1. Vehicle Check-in

**Endpoint**: `POST /api/staff/check-in`

```typescript
interface CheckInRequest {
  bookingId: number;
  vehicleStationId: number;
  vehicleCondition: {
    conditionType: number; // 0=Excellent, 1=Good, 2=Fair, 3=Poor, 4=Damaged
    description: string;
    odometerReading: number;
    fuelLevel: number;
    damageReported: boolean;
    photoUrls?: string[];
  };
  notes?: string;
}

interface CheckInResponse {
  checkInId: number;
  bookingId: number;
  vehicleId: number;
  staffId: number;
  checkTime: string;
  vehicleCondition: any;
  status: string;
}

export const staffCheckInOutService = {
  async checkIn(request: CheckInRequest): Promise<BaseResponse<CheckInResponse>> {
    return await apiClient.post('/staff/check-in', request);
  },

  async checkOut(request: CheckOutRequest): Promise<BaseResponse<CheckOutResponse>> {
    return await apiClient.post('/staff/check-out', request);
  },

  async getPendingCheckIns(): Promise<BaseResponse<PendingCheckIn[]>> {
    return await apiClient.get('/staff/check-ins/pending');
  },

  async getPendingCheckOuts(): Promise<BaseResponse<PendingCheckOut[]>> {
    return await apiClient.get('/staff/check-outs/pending');
  }
};

// Check-in component
const VehicleCheckIn: React.FC = () => {
  const [pendingBookings, setPendingBookings] = useState<PendingCheckIn[]>([]);
  const [selectedBooking, setSelectedBooking] = useState<PendingCheckIn | null>(null);
  const [conditionForm, setConditionForm] = useState({
    conditionType: 1,
    description: '',
    odometerReading: 0,
    fuelLevel: 100,
    damageReported: false,
    notes: ''
  });

  const handleCheckIn = async () => {
    if (!selectedBooking) return;
    
    try {
      const response = await staffCheckInOutService.checkIn({
        bookingId: selectedBooking.bookingId,
        vehicleStationId: selectedBooking.stationId,
        vehicleCondition: {
          conditionType: conditionForm.conditionType,
          description: conditionForm.description,
          odometerReading: conditionForm.odometerReading,
          fuelLevel: conditionForm.fuelLevel,
          damageReported: conditionForm.damageReported
        },
        notes: conditionForm.notes
      });
      
      if (response.statusCode === 200) {
        toast.success('Vehicle checked in successfully');
        loadPendingBookings(); // Refresh list
        setSelectedBooking(null);
      }
    } catch (error) {
      toast.error('Failed to check in vehicle');
    }
  };

  return (
    <div className="vehicle-check-in">
      <h2>Vehicle Check-In</h2>
      
      <div className="check-in-layout">
        {/* Pending bookings list */}
        <div className="pending-bookings">
          <h3>Pending Check-Ins</h3>
          {pendingBookings.map(booking => (
            <div 
              key={booking.bookingId}
              className={`booking-card ${selectedBooking?.bookingId === booking.bookingId ? 'selected' : ''}`}
              onClick={() => setSelectedBooking(booking)}
            >
              <div className="booking-info">
                <h4>{booking.vehicleName}</h4>
                <p>Booking ID: {booking.bookingId}</p>
                <p>Customer: {booking.customerName}</p>
                <p>Scheduled: {new Date(booking.startTime).toLocaleString()}</p>
              </div>
            </div>
          ))}
        </div>
        
        {/* Check-in form */}
        {selectedBooking && (
          <div className="check-in-form">
            <h3>Check-In: {selectedBooking.vehicleName}</h3>
            
            <div className="form-group">
              <label>Vehicle Condition</label>
              <select 
                value={conditionForm.conditionType}
                onChange={(e) => setConditionForm({
                  ...conditionForm, 
                  conditionType: parseInt(e.target.value)
                })}
              >
                <option value={0}>Excellent</option>
                <option value={1}>Good</option>
                <option value={2}>Fair</option>
                <option value={3}>Poor</option>
                <option value={4}>Damaged</option>
              </select>
            </div>
            
            <div className="form-group">
              <label>Odometer Reading</label>
              <input
                type="number"
                value={conditionForm.odometerReading}
                onChange={(e) => setConditionForm({
                  ...conditionForm,
                  odometerReading: parseInt(e.target.value)
                })}
              />
            </div>
            
            <div className="form-group">
              <label>Battery Level (%)</label>
              <input
                type="number"
                min="0"
                max="100"
                value={conditionForm.fuelLevel}
                onChange={(e) => setConditionForm({
                  ...conditionForm,
                  fuelLevel: parseInt(e.target.value)
                })}
              />
            </div>
            
            <div className="form-group">
              <label>Condition Description</label>
              <textarea
                value={conditionForm.description}
                onChange={(e) => setConditionForm({
                  ...conditionForm,
                  description: e.target.value
                })}
                placeholder="Describe vehicle condition..."
              />
            </div>
            
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={conditionForm.damageReported}
                  onChange={(e) => setConditionForm({
                    ...conditionForm,
                    damageReported: e.target.checked
                  })}
                />
                Damage Reported
              </label>
            </div>
            
            <div className="form-group">
              <label>Additional Notes</label>
              <textarea
                value={conditionForm.notes}
                onChange={(e) => setConditionForm({
                  ...conditionForm,
                  notes: e.target.value
                })}
                placeholder="Additional notes..."
              />
            </div>
            
            <div className="form-actions">
              <button onClick={() => setSelectedBooking(null)}>Cancel</button>
              <button onClick={handleCheckIn} className="primary">
                Complete Check-In
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
```

### 2. Vehicle Check-out

```typescript
interface CheckOutRequest {
  bookingId: number;
  vehicleStationId: number;
  vehicleCondition: {
    conditionType: number;
    description: string;
    odometerReading: number;
    fuelLevel: number;
    damageReported: boolean;
    photoUrls?: string[];
  };
  additionalCharges?: Array<{
    type: string;
    amount: number;
    description: string;
  }>;
  notes?: string;
}

// Similar implementation to check-in but for check-out process
```

## 🔧 Maintenance Management

### 1. Get Maintenance Tasks

**Endpoint**: `GET /api/staff/maintenance`

```typescript
interface MaintenanceTask {
  id: number;
  vehicleId: number;
  vehicleName: string;
  maintenanceType: number; // 0=Routine, 1=Repair, 2=Emergency, 3=Upgrade
  description: string;
  scheduledDate: string;
  status: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  estimatedCost: number;
  assignedStaffId?: number;
  assignedStaffName?: string;
}

export const staffMaintenanceService = {
  async getMaintenanceTasks(status?: string): Promise<BaseResponse<MaintenanceTask[]>> {
    const params = status ? `?status=${status}` : '';
    return await apiClient.get(`/staff/maintenance${params}`);
  },

  async assignMaintenance(taskId: number, staffId: number): Promise<BaseResponse<any>> {
    return await apiClient.patch(`/staff/maintenance/${taskId}/assign`, { staffId });
  },

  async updateMaintenanceStatus(taskId: number, status: string, notes?: string): Promise<BaseResponse<any>> {
    return await apiClient.patch(`/staff/maintenance/${taskId}/status`, { status, notes });
  },

  async createMaintenanceReport(report: CreateMaintenanceReportRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/staff/maintenance/report', report);
  }
};

// Maintenance management component
const MaintenanceManagement: React.FC = () => {
  const [maintenanceTasks, setMaintenanceTasks] = useState<MaintenanceTask[]>([]);
  const [filter, setFilter] = useState<string>('pending');

  const getPriorityColor = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'critical': return 'red';
      case 'high': return 'orange';
      case 'medium': return 'yellow';
      case 'low': return 'green';
      default: return 'gray';
    }
  };

  const getMaintenanceTypeName = (type: number) => {
    const types = ['Routine', 'Repair', 'Emergency', 'Upgrade'];
    return types[type] || 'Unknown';
  };

  return (
    <div className="maintenance-management">
      <div className="page-header">
        <h2>Maintenance Management</h2>
        <div className="filters">
          <select value={filter} onChange={(e) => setFilter(e.target.value)}>
            <option value="pending">Pending</option>
            <option value="in-progress">In Progress</option>
            <option value="completed">Completed</option>
            <option value="all">All Tasks</option>
          </select>
          
          <button onClick={() => createMaintenanceTask()}>
            Create Task
          </button>
        </div>
      </div>
      
      <div className="maintenance-grid">
        {maintenanceTasks.map(task => (
          <div key={task.id} className="maintenance-card">
            <div className="task-header">
              <h3>{task.vehicleName}</h3>
              <span className={`priority-badge ${getPriorityColor(task.priority)}`}>
                {task.priority}
              </span>
            </div>
            
            <div className="task-details">
              <p><strong>Type:</strong> {getMaintenanceTypeName(task.maintenanceType)}</p>
              <p><strong>Description:</strong> {task.description}</p>
              <p><strong>Scheduled:</strong> {new Date(task.scheduledDate).toLocaleDateString()}</p>
              <p><strong>Estimated Cost:</strong> {task.estimatedCost.toLocaleString()} VND</p>
              
              {task.assignedStaffName && (
                <p><strong>Assigned to:</strong> {task.assignedStaffName}</p>
              )}
            </div>
            
            <div className="task-actions">
              <button onClick={() => viewTaskDetails(task.id)}>
                View Details
              </button>
              
              {task.status === 'pending' && (
                <button onClick={() => assignTask(task.id)}>
                  Assign
                </button>
              )}
              
              {task.status === 'in-progress' && (
                <button onClick={() => completeTask(task.id)}>
                  Complete
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
```

## ⚖️ Dispute Management

### 1. Get Disputes

**Endpoint**: `GET /api/staff/disputes`

```typescript
interface Dispute {
  id: number;
  groupId: number;
  groupName: string;
  reportedByUserId: number;
  reportedByUserName: string;
  disputeType: string;
  description: string;
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  createdDate: string;
  assignedStaffId?: number;
  assignedStaffName?: string;
  resolutionNotes?: string;
}

export const staffDisputeService = {
  async getDisputes(status?: string): Promise<BaseResponse<Dispute[]>> {
    const params = status ? `?status=${status}` : '';
    return await apiClient.get(`/staff/disputes${params}`);
  },

  async assignDispute(disputeId: number, staffId: number): Promise<BaseResponse<any>> {
    return await apiClient.patch(`/staff/disputes/${disputeId}/assign`, { staffId });
  },

  async updateDisputeStatus(disputeId: number, status: string, notes?: string): Promise<BaseResponse<any>> {
    return await apiClient.patch(`/staff/disputes/${disputeId}/status`, { status, notes });
  },

  async resolveDispute(disputeId: number, resolution: DisputeResolutionRequest): Promise<BaseResponse<any>> {
    return await apiClient.post(`/staff/disputes/${disputeId}/resolve`, resolution);
  }
};

// Dispute management component
const DisputeManagement: React.FC = () => {
  const [disputes, setDisputes] = useState<Dispute[]>([]);
  const [selectedDispute, setSelectedDispute] = useState<Dispute | null>(null);
  const [showResolutionModal, setShowResolutionModal] = useState(false);

  const handleResolveDispute = async (disputeId: number, resolution: string) => {
    try {
      const response = await staffDisputeService.resolveDispute(disputeId, {
        resolution,
        resolutionType: 'Mediation',
        additionalNotes: ''
      });
      
      if (response.statusCode === 200) {
        toast.success('Dispute resolved successfully');
        loadDisputes(); // Refresh list
        setShowResolutionModal(false);
      }
    } catch (error) {
      toast.error('Failed to resolve dispute');
    }
  };

  return (
    <div className="dispute-management">
      <h2>Dispute Management</h2>
      
      <div className="disputes-list">
        {disputes.map(dispute => (
          <div key={dispute.id} className="dispute-card">
            <div className="dispute-header">
              <h3>Dispute #{dispute.id}</h3>
              <span className={`status-badge ${dispute.status.toLowerCase()}`}>
                {dispute.status}
              </span>
              <span className={`priority-badge ${dispute.priority.toLowerCase()}`}>
                {dispute.priority}
              </span>
            </div>
            
            <div className="dispute-details">
              <p><strong>Group:</strong> {dispute.groupName}</p>
              <p><strong>Reported by:</strong> {dispute.reportedByUserName}</p>
              <p><strong>Type:</strong> {dispute.disputeType}</p>
              <p><strong>Description:</strong> {dispute.description}</p>
              <p><strong>Created:</strong> {new Date(dispute.createdDate).toLocaleDateString()}</p>
              
              {dispute.assignedStaffName && (
                <p><strong>Assigned to:</strong> {dispute.assignedStaffName}</p>
              )}
            </div>
            
            <div className="dispute-actions">
              <button onClick={() => viewDisputeDetails(dispute.id)}>
                View Details
              </button>
              
              {dispute.status === 'Open' && (
                <button onClick={() => assignDispute(dispute.id)}>
                  Assign to Me
                </button>
              )}
              
              {dispute.status === 'InProgress' && (
                <button 
                  onClick={() => {
                    setSelectedDispute(dispute);
                    setShowResolutionModal(true);
                  }}
                >
                  Resolve
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
      
      {/* Resolution Modal */}
      {showResolutionModal && selectedDispute && (
        <DisputeResolutionModal
          dispute={selectedDispute}
          onResolve={handleResolveDispute}
          onClose={() => setShowResolutionModal(false)}
        />
      )}
    </div>
  );
};
```

## 👤 Staff Profile Management

### Staff Profile Endpoints

```typescript
export const staffProfileService = {
  async getProfile(): Promise<BaseResponse<StaffProfile>> {
    return await apiClient.get('/staff/profile');
  },

  async updateProfile(profileData: UpdateProfileRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/staff/profile', profileData);
  },

  async changePassword(passwordData: ChangePasswordRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/staff/profile/change-password', passwordData);
  },

  async getWorkSchedule(): Promise<BaseResponse<WorkSchedule[]>> {
    return await apiClient.get('/staff/profile/work-schedule');
  },

  async updateWorkSchedule(schedule: WorkScheduleRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/staff/profile/work-schedule', schedule);
  },

  async getPerformanceMetrics(): Promise<BaseResponse<PerformanceMetrics>> {
    return await apiClient.get('/staff/profile/performance');
  }
};

// Staff profile component
const StaffProfile: React.FC = () => {
  const [profile, setProfile] = useState<StaffProfile | null>(null);
  const [activeTab, setActiveTab] = useState('profile');

  return (
    <div className="staff-profile">
      <div className="profile-header">
        <h2>Staff Profile</h2>
        <div className="profile-tabs">
          <button 
            className={activeTab === 'profile' ? 'active' : ''}
            onClick={() => setActiveTab('profile')}
          >
            Profile
          </button>
          <button 
            className={activeTab === 'schedule' ? 'active' : ''}
            onClick={() => setActiveTab('schedule')}
          >
            Work Schedule
          </button>
          <button 
            className={activeTab === 'performance' ? 'active' : ''}
            onClick={() => setActiveTab('performance')}
          >
            Performance
          </button>
        </div>
      </div>
      
      <div className="profile-content">
        {activeTab === 'profile' && <ProfileTab profile={profile} />}
        {activeTab === 'schedule' && <ScheduleTab />}
        {activeTab === 'performance' && <PerformanceTab />}
      </div>
    </div>
  );
};
```

## 📊 Staff Dashboard

```typescript
interface StaffDashboardData {
  todayTasks: {
    checkIns: number;
    checkOuts: number;
    maintenanceTasks: number;
    disputes: number;
  };
  weeklyStats: {
    completedBookings: number;
    resolvedDisputes: number;
    maintenanceCompleted: number;
    customerSatisfaction: number;
  };
  recentActivities: Array<{
    id: number;
    type: string;
    description: string;
    timestamp: string;
  }>;
}

export const staffDashboardService = {
  async getDashboardData(): Promise<BaseResponse<StaffDashboardData>> {
    return await apiClient.get('/staff/dashboard');
  }
};

const StaffDashboard: React.FC = () => {
  const [dashboardData, setDashboardData] = useState<StaffDashboardData | null>(null);

  useEffect(() => {
    loadDashboardData();
  }, []);

  return (
    <div className="staff-dashboard">
      <h1>Staff Dashboard</h1>
      
      {/* Today's Tasks */}
      <div className="dashboard-section">
        <h2>Today's Tasks</h2>
        <div className="task-cards">
          <div className="task-card">
            <h3>Check-Ins</h3>
            <p className="task-count">{dashboardData?.todayTasks.checkIns || 0}</p>
            <Link to="/staff/check-ins">Manage →</Link>
          </div>
          
          <div className="task-card">
            <h3>Check-Outs</h3>
            <p className="task-count">{dashboardData?.todayTasks.checkOuts || 0}</p>
            <Link to="/staff/check-outs">Manage →</Link>
          </div>
          
          <div className="task-card">
            <h3>Maintenance</h3>
            <p className="task-count">{dashboardData?.todayTasks.maintenanceTasks || 0}</p>
            <Link to="/staff/maintenance">Manage →</Link>
          </div>
          
          <div className="task-card">
            <h3>Disputes</h3>
            <p className="task-count">{dashboardData?.todayTasks.disputes || 0}</p>
            <Link to="/staff/disputes">Manage →</Link>
          </div>
        </div>
      </div>
      
      {/* Weekly Performance */}
      <div className="dashboard-section">
        <h2>Weekly Performance</h2>
        <div className="performance-grid">
          <div className="performance-item">
            <span className="label">Completed Bookings</span>
            <span className="value">{dashboardData?.weeklyStats.completedBookings || 0}</span>
          </div>
          <div className="performance-item">
            <span className="label">Resolved Disputes</span>
            <span className="value">{dashboardData?.weeklyStats.resolvedDisputes || 0}</span>
          </div>
          <div className="performance-item">
            <span className="label">Maintenance Completed</span>
            <span className="value">{dashboardData?.weeklyStats.maintenanceCompleted || 0}</span>
          </div>
          <div className="performance-item">
            <span className="label">Customer Satisfaction</span>
            <span className="value">{dashboardData?.weeklyStats.customerSatisfaction || 0}%</span>
          </div>
        </div>
      </div>
      
      {/* Recent Activities */}
      <div className="dashboard-section">
        <h2>Recent Activities</h2>
        <div className="activities-list">
          {dashboardData?.recentActivities.map(activity => (
            <div key={activity.id} className="activity-item">
              <span className="activity-type">{activity.type}</span>
              <span className="activity-description">{activity.description}</span>
              <span className="activity-time">
                {new Date(activity.timestamp).toLocaleString()}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
```

## 🛡️ Error Handling

```typescript
// utils/staffErrorHandler.ts
export const handleStaffError = (error: any) => {
  switch (error.response?.status) {
    case 401:
      return 'Authentication required. Please login.';
    case 403:
      return 'Staff access required. Insufficient permissions.';
    case 404:
      return 'Resource not found.';
    case 409:
      return 'Conflict: Operation cannot be completed.';
    case 422:
      return 'Validation error. Please check your input.';
    case 500:
      return 'Internal server error. Please try again later.';
    default:
      return error.response?.data?.message || 'An unexpected error occurred';
  }
};
```

---

**Next Steps**:
- For Co-owner features → `README_FRONTEND_COOWNER.md`
- For License management → `README_FRONTEND_LICENSE.md`
- For File upload system → `README_FRONTEND_FILEUPLOAD.md`