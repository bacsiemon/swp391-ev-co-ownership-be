# Admin Features Integration Guide

## 🔑 Overview

This guide covers all admin-specific functionality for managing users, licenses, groups, notifications, and system settings in the EV Co-Ownership platform.

## 🏗️ Admin Authentication

All admin endpoints require:
- **Authentication**: Bearer token
- **Authorization**: Admin role (role = 2)
- **Base URL**: `/api/admin`

### Admin Authorization Check
```typescript
// utils/adminAuth.ts
export const isAdmin = (user: any): boolean => {
  return user?.role === 2; // Admin role
};

export const requireAdminRole = (user: any) => {
  if (!user || user.role !== 2) {
    throw new Error('Admin access required');
  }
};
```

## 👥 User Management

### 1. Get All Users (Paginated)

**Endpoint**: `GET /api/admin/users`

```typescript
interface GetUsersParams {
  pageIndex?: number;
  pageSize?: number;
}

interface UserListResponse {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  roleEnum: number; // 0=CoOwner, 1=Staff, 2=Admin
  statusEnum: number; // 0=Active, 1=Inactive, 2=Suspended
  createdAt: string;
  updatedAt: string;
}

// service
export const adminUserService = {
  async getAllUsers(params: GetUsersParams = {}): Promise<BaseResponse<PaginatedResult<UserListResponse>>> {
    const queryParams = new URLSearchParams({
      pageIndex: (params.pageIndex || 1).toString(),
      pageSize: (params.pageSize || 10).toString()
    });
    
    return await apiClient.get(`/admin/users?${queryParams}`);
  }
};

// component usage
const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<UserListResponse[]>([]);
  const [pagination, setPagination] = useState({
    pageIndex: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  });

  const loadUsers = async () => {
    try {
      const response = await adminUserService.getAllUsers({
        pageIndex: pagination.pageIndex,
        pageSize: pagination.pageSize
      });
      
      if (response.statusCode === 200) {
        setUsers(response.data.items);
        setPagination(prev => ({
          ...prev,
          totalCount: response.data.totalCount,
          totalPages: response.data.totalPages
        }));
      }
    } catch (error) {
      console.error('Failed to load users:', error);
    }
  };

  useEffect(() => {
    loadUsers();
  }, [pagination.pageIndex, pagination.pageSize]);

  return (
    <div className="user-management">
      <h2>User Management</h2>
      
      {/* User table */}
      <table className="users-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map(user => (
            <tr key={user.id}>
              <td>{user.id}</td>
              <td>{user.firstName} {user.lastName}</td>
              <td>{user.email}</td>
              <td>{getRoleName(user.roleEnum)}</td>
              <td>{getStatusName(user.statusEnum)}</td>
              <td>
                <button onClick={() => editUser(user.id)}>Edit</button>
                <button onClick={() => deleteUser(user.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      
      {/* Pagination */}
      <Pagination 
        current={pagination.pageIndex}
        total={pagination.totalCount}
        pageSize={pagination.pageSize}
        onChange={(page) => setPagination(prev => ({ ...prev, pageIndex: page }))}
      />
    </div>
  );
};
```

### 2. Create User

**Endpoint**: `POST /api/admin/user`

```typescript
interface CreateUserRequest {
  email: string;
  fullName: string;
  phoneNumber: string;
  password: string;
}

export const adminUserService = {
  async createUser(userData: CreateUserRequest): Promise<BaseResponse<User>> {
    return await apiClient.post('/admin/user', userData);
  }
};

// component
const CreateUserForm: React.FC = () => {
  const [formData, setFormData] = useState<CreateUserRequest>({
    email: '',
    fullName: '',
    phoneNumber: '',
    password: ''
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await adminUserService.createUser(formData);
      if (response.statusCode === 201) {
        toast.success('User created successfully');
        onUserCreated(response.data);
      }
    } catch (error) {
      toast.error('Failed to create user');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="email"
        placeholder="Email"
        value={formData.email}
        onChange={(e) => setFormData({...formData, email: e.target.value})}
        required
      />
      <input
        type="text"
        placeholder="Full Name"
        value={formData.fullName}
        onChange={(e) => setFormData({...formData, fullName: e.target.value})}
        required
      />
      <input
        type="tel"
        placeholder="Phone Number"
        value={formData.phoneNumber}
        onChange={(e) => setFormData({...formData, phoneNumber: e.target.value})}
        required
      />
      <input
        type="password"
        placeholder="Password"
        value={formData.password}
        onChange={(e) => setFormData({...formData, password: e.target.value})}
        required
      />
      <button type="submit">Create User</button>
    </form>
  );
};
```

### 3. Update & Delete Users

```typescript
export const adminUserService = {
  async updateUser(id: number, userData: Partial<User>): Promise<BaseResponse<User>> {
    return await apiClient.patch(`/admin/user/${id}`, userData);
  },

  async deleteUser(id: number): Promise<BaseResponse<any>> {
    return await apiClient.delete(`/admin/user/${id}`);
  }
};
```

## 📄 License Management

### 1. Get All Licenses

**Endpoint**: `GET /api/admin/licenses`

```typescript
interface LicenseListParams {
  status?: 'pending' | 'verified' | 'rejected' | 'expired';
  page?: number;
  pageSize?: number;
}

interface LicenseListResponse {
  id: number;
  licenseNumber: string;
  issuedBy: string;
  issueDate: string;
  expiryDate?: string;
  licenseImageUrl?: string;
  verificationStatus: number; // 0=Pending, 1=Verified, 2=Rejected, 3=Expired
  rejectReason?: string;
  userName: string;
  userId: number;
  submittedAt: string;
  verifiedByUserName?: string;
  verifiedAt?: string;
  isExpired: boolean;
}

export const adminLicenseService = {
  async getAllLicenses(params: LicenseListParams = {}): Promise<BaseResponse<PaginatedResult<LicenseListResponse>>> {
    const queryParams = new URLSearchParams();
    if (params.status) queryParams.append('status', params.status);
    if (params.page) queryParams.append('page', params.page.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    
    return await apiClient.get(`/admin/licenses?${queryParams}`);
  }
};

// component
const LicenseManagement: React.FC = () => {
  const [licenses, setLicenses] = useState<LicenseListResponse[]>([]);
  const [filter, setFilter] = useState<string>('');

  const getStatusBadge = (status: number) => {
    const statusMap = {
      0: { text: 'Pending', color: 'yellow' },
      1: { text: 'Verified', color: 'green' },
      2: { text: 'Rejected', color: 'red' },
      3: { text: 'Expired', color: 'gray' }
    };
    return statusMap[status] || { text: 'Unknown', color: 'gray' };
  };

  return (
    <div className="license-management">
      <h2>License Management</h2>
      
      {/* Filter */}
      <select value={filter} onChange={(e) => setFilter(e.target.value)}>
        <option value="">All Licenses</option>
        <option value="pending">Pending</option>
        <option value="verified">Verified</option>
        <option value="rejected">Rejected</option>
        <option value="expired">Expired</option>
      </select>
      
      {/* License list */}
      <div className="license-list">
        {licenses.map(license => (
          <div key={license.id} className="license-card">
            <div className="license-info">
              <h3>{license.userName}</h3>
              <p>License: {license.licenseNumber}</p>
              <p>Issued by: {license.issuedBy}</p>
              <p>Submitted: {new Date(license.submittedAt).toLocaleDateString()}</p>
              
              <span className={`status-badge ${getStatusBadge(license.verificationStatus).color}`}>
                {getStatusBadge(license.verificationStatus).text}
              </span>
            </div>
            
            {license.licenseImageUrl && (
              <img src={license.licenseImageUrl} alt="License" className="license-image" />
            )}
            
            <div className="license-actions">
              {license.verificationStatus === 0 && (
                <>
                  <button onClick={() => approveLicense(license.id)}>Approve</button>
                  <button onClick={() => rejectLicense(license.id)}>Reject</button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
```

### 2. Approve/Reject License

```typescript
interface ApproveLicenseRequest {
  licenseId: number;
  notes?: string;
}

interface RejectLicenseRequest {
  licenseId: number;
  rejectReason: string;
  notes?: string;
}

export const adminLicenseService = {
  async approveLicense(request: ApproveLicenseRequest): Promise<BaseResponse<any>> {
    return await apiClient.patch('/admin/license/approve', request);
  },

  async rejectLicense(request: RejectLicenseRequest): Promise<BaseResponse<any>> {
    return await apiClient.patch('/admin/license/reject', request);
  }
};

// component methods
const approveLicense = async (licenseId: number) => {
  try {
    const response = await adminLicenseService.approveLicense({
      licenseId,
      notes: 'License approved by admin'
    });
    
    if (response.statusCode === 200) {
      toast.success('License approved successfully');
      loadLicenses(); // Refresh list
    }
  } catch (error) {
    toast.error('Failed to approve license');
  }
};

const rejectLicense = async (licenseId: number) => {
  const reason = prompt('Enter reason for rejection:');
  if (!reason) return;
  
  try {
    const response = await adminLicenseService.rejectLicense({
      licenseId,
      rejectReason: reason
    });
    
    if (response.statusCode === 200) {
      toast.success('License rejected');
      loadLicenses(); // Refresh list
    }
  } catch (error) {
    toast.error('Failed to reject license');
  }
};
```

## 🏢 Group Management

### 1. Get All Groups

**Endpoint**: `GET /api/admin/groups`

```typescript
interface GroupOverviewResponse {
  groupId: number;
  groupName: string;
  memberCount: number;
  vehicleCount: number;
  status: string;
  createdDate: string;
  totalFunds: number;
  activeDisputeCount: number;
  utilizationRate: number;
  healthScore: string;
}

export const adminGroupService = {
  async getAllGroups(): Promise<BaseResponse<GroupOverviewResponse[]>> {
    return await apiClient.get('/admin/groups');
  },

  async getGroupsOverview(): Promise<BaseResponse<any>> {
    return await apiClient.get('/admin/groups/overview');
  }
};
```

### 2. Create Group

**Endpoint**: `POST /api/admin/group`

```typescript
interface CreateGroupRequest {
  groupName: string;
  description: string;
  createdByUserId: number;
  initialMembers: Array<{
    userId: number;
    ownershipPercentage: number;
    role: string;
  }>;
  settings: {
    autoApproveBookings: boolean;
    maxBookingDays: number;
    minimumFundBalance: number;
    allowMemberInvites: boolean;
    requireUnanimousVoting: boolean;
  };
}

export const adminGroupService = {
  async createGroup(groupData: CreateGroupRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/admin/group', groupData);
  }
};
```

### 3. Update Group Status

```typescript
interface UpdateGroupStatusRequest {
  groupId: number;
  newStatus: string;
  reason: string;
  notifyMembers: boolean;
  effectiveDate: string;
}

export const adminGroupService = {
  async updateGroupStatus(request: UpdateGroupStatusRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/admin/group/status', request);
  }
};
```

## ⚙️ System Settings

### 1. Get System Settings

**Endpoint**: `GET /api/admin/settings`

```typescript
interface SystemSettings {
  MaxBookingDuration: number;
  BookingAdvanceTime: number;
  DefaultDepositAmount: number;
  MaintenanceFeePercentage: number;
  [key: string]: any;
}

export const adminSystemService = {
  async getSettings(): Promise<BaseResponse<SystemSettings>> {
    return await apiClient.get('/admin/settings');
  },

  async updateSettings(settings: Partial<SystemSettings>): Promise<BaseResponse<any>> {
    return await apiClient.patch('/admin/settings', settings);
  }
};

// component
const SystemSettings: React.FC = () => {
  const [settings, setSettings] = useState<SystemSettings>({
    MaxBookingDuration: 24,
    BookingAdvanceTime: 2,
    DefaultDepositAmount: 500000,
    MaintenanceFeePercentage: 10
  });

  const loadSettings = async () => {
    try {
      const response = await adminSystemService.getSettings();
      if (response.statusCode === 200) {
        setSettings(response.data);
      }
    } catch (error) {
      console.error('Failed to load settings:', error);
    }
  };

  const saveSettings = async () => {
    try {
      const response = await adminSystemService.updateSettings(settings);
      if (response.statusCode === 200) {
        toast.success('Settings updated successfully');
      }
    } catch (error) {
      toast.error('Failed to update settings');
    }
  };

  return (
    <div className="system-settings">
      <h2>System Settings</h2>
      
      <div className="settings-form">
        <div className="setting-item">
          <label>Max Booking Duration (hours)</label>
          <input
            type="number"
            value={settings.MaxBookingDuration}
            onChange={(e) => setSettings({
              ...settings,
              MaxBookingDuration: parseInt(e.target.value)
            })}
          />
        </div>
        
        <div className="setting-item">
          <label>Booking Advance Time (hours)</label>
          <input
            type="number"
            value={settings.BookingAdvanceTime}
            onChange={(e) => setSettings({
              ...settings,
              BookingAdvanceTime: parseInt(e.target.value)
            })}
          />
        </div>
        
        <div className="setting-item">
          <label>Default Deposit Amount (VND)</label>
          <input
            type="number"
            value={settings.DefaultDepositAmount}
            onChange={(e) => setSettings({
              ...settings,
              DefaultDepositAmount: parseFloat(e.target.value)
            })}
          />
        </div>
        
        <div className="setting-item">
          <label>Maintenance Fee Percentage (%)</label>
          <input
            type="number"
            value={settings.MaintenanceFeePercentage}
            onChange={(e) => setSettings({
              ...settings,
              MaintenanceFeePercentage: parseInt(e.target.value)
            })}
          />
        </div>
        
        <button onClick={saveSettings} className="save-button">
          Save Settings
        </button>
      </div>
    </div>
  );
};
```

## 📊 Reports & Analytics

### 1. System Reports

**Endpoint**: `GET /api/admin/reports`

```typescript
interface SystemReports {
  TotalUsers: number;
  TotalGroups: number;
  PendingLicenses: number;
  ActiveBookings: number;
  Revenue: {
    ThisMonth: number;
    LastMonth: number;
    Growth: number;
  };
}

export const adminReportService = {
  async getSystemReports(): Promise<BaseResponse<SystemReports>> {
    return await apiClient.get('/admin/reports');
  }
};

// Dashboard component
const AdminDashboard: React.FC = () => {
  const [reports, setReports] = useState<SystemReports | null>(null);

  const loadReports = async () => {
    try {
      const response = await adminReportService.getSystemReports();
      if (response.statusCode === 200) {
        setReports(response.data);
      }
    } catch (error) {
      console.error('Failed to load reports:', error);
    }
  };

  useEffect(() => {
    loadReports();
  }, []);

  if (!reports) return <div>Loading...</div>;

  return (
    <div className="admin-dashboard">
      <h1>Admin Dashboard</h1>
      
      <div className="stats-grid">
        <div className="stat-card">
          <h3>Total Users</h3>
          <p className="stat-number">{reports.TotalUsers}</p>
        </div>
        
        <div className="stat-card">
          <h3>Total Groups</h3>
          <p className="stat-number">{reports.TotalGroups}</p>
        </div>
        
        <div className="stat-card">
          <h3>Pending Licenses</h3>
          <p className="stat-number">{reports.PendingLicenses}</p>
        </div>
        
        <div className="stat-card">
          <h3>Active Bookings</h3>
          <p className="stat-number">{reports.ActiveBookings}</p>
        </div>
        
        <div className="stat-card revenue-card">
          <h3>This Month Revenue</h3>
          <p className="stat-number">{reports.Revenue.ThisMonth.toLocaleString()} VND</p>
          <p className={`growth ${reports.Revenue.Growth >= 0 ? 'positive' : 'negative'}`}>
            {reports.Revenue.Growth.toFixed(1)}% vs last month
          </p>
        </div>
      </div>
    </div>
  );
};
```

### 2. Audit Logs

**Endpoint**: `GET /api/admin/audit-logs`

```typescript
interface AuditLog {
  Id: number;
  Action: string;
  UserId: number;
  UserName: string;
  Timestamp: string;
  IpAddress: string;
  Details: string;
}

export const adminReportService = {
  async getAuditLogs(pageIndex: number = 1, pageSize: number = 50): Promise<BaseResponse<PaginatedResult<AuditLog>>> {
    return await apiClient.get(`/admin/audit-logs?pageIndex=${pageIndex}&pageSize=${pageSize}`);
  }
};
```

## 🔔 Notification Management

### 1. Send Notification to User

**Endpoint**: `POST /api/admin/notifications/send-to-user`

```typescript
interface SendNotificationRequest {
  userId: number;
  notificationType: string;
  additionalData: string;
}

export const adminNotificationService = {
  async sendNotificationToUser(request: SendNotificationRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/admin/notifications/send-to-user', request);
  }
};
```

### 2. Create Bulk Notification

**Endpoint**: `POST /api/admin/notifications/create-notification`

```typescript
interface CreateNotificationRequest {
  notificationType: string;
  userIds: number[];
  additionalData: string;
}

export const adminNotificationService = {
  async createNotification(request: CreateNotificationRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/admin/notifications/create-notification', {
      notificationType: request.notificationType,
      userIds: JSON.stringify(request.userIds),
      additionalData: request.additionalData
    });
  }
};
```

### 3. Get All Notifications

**Endpoint**: `GET /api/admin/notifications`

```typescript
export const adminNotificationService = {
  async getAllNotifications(
    pageIndex: number = 1,
    pageSize: number = 20,
    notificationType?: string
  ): Promise<BaseResponse<any>> {
    const params = new URLSearchParams({
      pageIndex: pageIndex.toString(),
      pageSize: pageSize.toString()
    });
    
    if (notificationType) {
      params.append('notificationType', notificationType);
    }
    
    return await apiClient.get(`/admin/notifications?${params}`);
  }
};
```

## 👤 Admin Profile Management

### Profile Endpoints

```typescript
// Get admin profile
export const adminProfileService = {
  async getProfile(): Promise<BaseResponse<any>> {
    return await apiClient.get('/admin/profile');
  },

  async updateProfile(profileData: UpdateProfileRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/admin/profile', profileData);
  },

  async changePassword(passwordData: ChangePasswordRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/admin/profile/change-password', passwordData);
  },

  async updateNotificationSettings(settings: UpdateNotificationSettingsRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/admin/profile/notification-settings', settings);
  },

  async getActivityLog(page: number = 1, pageSize: number = 50, category?: string): Promise<BaseResponse<any>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString()
    });
    if (category) params.append('category', category);
    
    return await apiClient.get(`/admin/profile/activity-log?${params}`);
  },

  async getSecurityLog(days: number = 30): Promise<BaseResponse<any>> {
    return await apiClient.get(`/admin/profile/security-log?days=${days}`);
  },

  async getUserProfile(userId: number): Promise<BaseResponse<any>> {
    return await apiClient.get(`/admin/profile/user/${userId}`);
  }
};
```

## 🛡️ Error Handling

```typescript
// utils/adminErrorHandler.ts
export const handleAdminError = (error: any) => {
  switch (error.response?.status) {
    case 401:
      return 'Authentication required. Please login.';
    case 403:
      return 'Admin access required. Insufficient permissions.';
    case 404:
      return 'Resource not found.';
    case 409:
      return 'Conflict: Resource already exists.';
    case 500:
      return 'Internal server error. Please try again later.';
    default:
      return error.response?.data?.message || 'An unexpected error occurred';
  }
};
```

## 🎨 UI Components Examples

### Admin Layout
```typescript
const AdminLayout: React.FC = () => {
  const { user, logout } = useAuth();
  
  if (!isAdmin(user)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return (
    <div className="admin-layout">
      <aside className="admin-sidebar">
        <nav>
          <Link to="/admin/dashboard">Dashboard</Link>
          <Link to="/admin/users">User Management</Link>
          <Link to="/admin/licenses">License Management</Link>
          <Link to="/admin/groups">Group Management</Link>
          <Link to="/admin/settings">System Settings</Link>
          <Link to="/admin/reports">Reports</Link>
          <Link to="/admin/notifications">Notifications</Link>
        </nav>
      </aside>
      
      <main className="admin-content">
        <header className="admin-header">
          <h1>Admin Panel</h1>
          <div className="admin-actions">
            <span>Welcome, {user.firstName}</span>
            <button onClick={logout}>Logout</button>
          </div>
        </header>
        
        <Outlet />
      </main>
    </div>
  );
};
```

---

**Next Steps**:
- For Staff features → `README_FRONTEND_STAFF.md`
- For Co-owner features → `README_FRONTEND_COOWNER.md`
- For License management → `README_FRONTEND_LICENSE.md`