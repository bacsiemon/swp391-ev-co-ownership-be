# Co-Owner Features Integration Guide

## 🔑 Overview

This guide covers all co-owner (main user) functionality including dashboard, vehicle booking, fund management, usage analytics, profile management, and group participation in the EV Co-Ownership platform.

## 🏗️ Co-Owner Authentication

All co-owner endpoints require:
- **Authentication**: Bearer token
- **Authorization**: CoOwner role (role = 0)
- **Base URL**: `/api/coowner`

### Co-Owner Authorization Check
```typescript
// utils/coOwnerAuth.ts
export const isCoOwner = (user: any): boolean => {
  return user?.role === 0; // CoOwner role
};

export const requireCoOwnerRole = (user: any) => {
  if (!user || user.role !== 0) {
    throw new Error('Co-owner access required');
  }
};
```

## 👤 Profile Management

### 1. Get Co-Owner Profile

**Endpoint**: `GET /api/coowner/profile`

```typescript
interface CoOwnerProfile {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  dateOfBirth?: string;
  address?: string;
  profileImageUrl?: string;
  status: number; // 0=Active, 1=Inactive, 2=Suspended
  coOwnerInfo: {
    memberSince: string;
    totalGroups: number;
    totalVehicles: number;
    verificationStatus: string;
    licenseStatus: string;
  };
  currentGroups: Array<{
    groupId: number;
    groupName: string;
    ownershipPercentage: number;
    role: string;
    joinedDate: string;
  }>;
  statistics: {
    totalBookings: number;
    totalDriveTime: number;
    totalDistance: number;
    favoriteVehicle?: string;
  };
}

export const coOwnerProfileService = {
  async getProfile(): Promise<BaseResponse<CoOwnerProfile>> {
    return await apiClient.get('/coowner/profile');
  },

  async updateProfile(profileData: UpdateCoOwnerProfileRequest): Promise<BaseResponse<any>> {
    return await apiClient.put('/coowner/profile', profileData);
  }
};

// Profile component
const CoOwnerProfile: React.FC = () => {
  const [profile, setProfile] = useState<CoOwnerProfile | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    try {
      const response = await coOwnerProfileService.getProfile();
      if (response.statusCode === 200) {
        setProfile(response.data);
      }
    } catch (error) {
      console.error('Failed to load profile:', error);
    }
  };

  if (!profile) return <div>Loading profile...</div>;

  return (
    <div className="coowner-profile">
      <div className="profile-header">
        <div className="profile-avatar">
          <img 
            src={profile.profileImageUrl || '/default-avatar.png'} 
            alt="Profile"
            className="avatar-image"
          />
        </div>
        
        <div className="profile-info">
          <h1>{profile.firstName} {profile.lastName}</h1>
          <p className="email">{profile.email}</p>
          <p className="member-since">
            Member since {new Date(profile.coOwnerInfo.memberSince).getFullYear()}
          </p>
          
          <div className="verification-badges">
            <span className={`badge ${profile.coOwnerInfo.verificationStatus.toLowerCase()}`}>
              {profile.coOwnerInfo.verificationStatus}
            </span>
            <span className={`badge ${profile.coOwnerInfo.licenseStatus.toLowerCase()}`}>
              License: {profile.coOwnerInfo.licenseStatus}
            </span>
          </div>
        </div>
      </div>

      {/* Profile Stats */}
      <div className="profile-stats">
        <div className="stat-card">
          <h3>Groups</h3>
          <p className="stat-number">{profile.coOwnerInfo.totalGroups}</p>
        </div>
        <div className="stat-card">
          <h3>Vehicles</h3>
          <p className="stat-number">{profile.coOwnerInfo.totalVehicles}</p>
        </div>
        <div className="stat-card">
          <h3>Total Bookings</h3>
          <p className="stat-number">{profile.statistics.totalBookings}</p>
        </div>
        <div className="stat-card">
          <h3>Total Distance</h3>
          <p className="stat-number">{profile.statistics.totalDistance} km</p>
        </div>
      </div>

      {/* Current Groups */}
      <div className="current-groups">
        <h2>My Groups</h2>
        <div className="groups-grid">
          {profile.currentGroups.map(group => (
            <div key={group.groupId} className="group-card">
              <h3>{group.groupName}</h3>
              <p>Ownership: {group.ownershipPercentage}%</p>
              <p>Role: {group.role}</p>
              <p>Joined: {new Date(group.joinedDate).toLocaleDateString()}</p>
              <button onClick={() => viewGroupDetails(group.groupId)}>
                View Details
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
```

### 2. Update Profile

```typescript
interface UpdateCoOwnerProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  dateOfBirth?: string;
  address?: string;
  profileImageUrl?: string;
}

const ProfileEditForm: React.FC<{ profile: CoOwnerProfile; onSave: () => void }> = ({ 
  profile, 
  onSave 
}) => {
  const [formData, setFormData] = useState<UpdateCoOwnerProfileRequest>({
    firstName: profile.firstName,
    lastName: profile.lastName,
    phone: profile.phone || '',
    dateOfBirth: profile.dateOfBirth || '',
    address: profile.address || ''
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await coOwnerProfileService.updateProfile(formData);
      if (response.statusCode === 200) {
        toast.success('Profile updated successfully');
        onSave();
      }
    } catch (error) {
      toast.error('Failed to update profile');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="profile-edit-form">
      <div className="form-row">
        <div className="form-group">
          <label>First Name</label>
          <input
            type="text"
            value={formData.firstName}
            onChange={(e) => setFormData({...formData, firstName: e.target.value})}
            required
          />
        </div>
        <div className="form-group">
          <label>Last Name</label>
          <input
            type="text"
            value={formData.lastName}
            onChange={(e) => setFormData({...formData, lastName: e.target.value})}
            required
          />
        </div>
      </div>
      
      <div className="form-group">
        <label>Phone Number</label>
        <input
          type="tel"
          value={formData.phone}
          onChange={(e) => setFormData({...formData, phone: e.target.value})}
        />
      </div>
      
      <div className="form-group">
        <label>Date of Birth</label>
        <input
          type="date"
          value={formData.dateOfBirth}
          onChange={(e) => setFormData({...formData, dateOfBirth: e.target.value})}
        />
      </div>
      
      <div className="form-group">
        <label>Address</label>
        <textarea
          value={formData.address}
          onChange={(e) => setFormData({...formData, address: e.target.value})}
          rows={3}
        />
      </div>
      
      <div className="form-actions">
        <button type="button" onClick={() => onSave()}>Cancel</button>
        <button type="submit">Save Changes</button>
      </div>
    </form>
  );
};
```

## 📅 Vehicle Booking System

### 1. Get Available Vehicles

**Endpoint**: `GET /api/coowner/vehicles/available`

```typescript
interface AvailableVehicle {
  id: number;
  name: string;
  brand: string;
  model: string;
  year: number;
  color: string;
  batteryCapacity: number;
  rangeKm: number;
  licensePlate: string;
  currentLocation: {
    latitude: number;
    longitude: number;
    stationName: string;
  };
  pricePerHour: number;
  rating: number;
  totalBookings: number;
  images: string[];
  features: string[];
  availability: Array<{
    date: string;
    availableSlots: Array<{
      startTime: string;
      endTime: string;
    }>;
  }>;
}

export const coOwnerBookingService = {
  async getAvailableVehicles(
    startDate: string,
    endDate: string,
    location?: { lat: number; lng: number }
  ): Promise<BaseResponse<AvailableVehicle[]>> {
    const params = new URLSearchParams({
      startDate,
      endDate
    });
    
    if (location) {
      params.append('lat', location.lat.toString());
      params.append('lng', location.lng.toString());
    }
    
    return await apiClient.get(`/coowner/vehicles/available?${params}`);
  },

  async getVehicleDetails(vehicleId: number): Promise<BaseResponse<VehicleDetails>> {
    return await apiClient.get(`/coowner/vehicles/${vehicleId}`);
  }
};

// Available vehicles component
const AvailableVehicles: React.FC = () => {
  const [vehicles, setVehicles] = useState<AvailableVehicle[]>([]);
  const [searchParams, setSearchParams] = useState({
    startDate: '',
    endDate: '',
    location: null as { lat: number; lng: number } | null
  });

  const searchVehicles = async () => {
    if (!searchParams.startDate || !searchParams.endDate) {
      toast.error('Please select start and end dates');
      return;
    }

    try {
      const response = await coOwnerBookingService.getAvailableVehicles(
        searchParams.startDate,
        searchParams.endDate,
        searchParams.location || undefined
      );
      
      if (response.statusCode === 200) {
        setVehicles(response.data);
      }
    } catch (error) {
      toast.error('Failed to search vehicles');
    }
  };

  return (
    <div className="available-vehicles">
      <div className="search-form">
        <h2>Find Available Vehicles</h2>
        
        <div className="search-inputs">
          <div className="date-inputs">
            <div className="form-group">
              <label>Start Date & Time</label>
              <input
                type="datetime-local"
                value={searchParams.startDate}
                onChange={(e) => setSearchParams({
                  ...searchParams,
                  startDate: e.target.value
                })}
              />
            </div>
            
            <div className="form-group">
              <label>End Date & Time</label>
              <input
                type="datetime-local"
                value={searchParams.endDate}
                onChange={(e) => setSearchParams({
                  ...searchParams,
                  endDate: e.target.value
                })}
              />
            </div>
          </div>
          
          <button onClick={searchVehicles} className="search-btn">
            Search Vehicles
          </button>
        </div>
      </div>

      <div className="vehicles-grid">
        {vehicles.map(vehicle => (
          <div key={vehicle.id} className="vehicle-card">
            <div className="vehicle-images">
              <img 
                src={vehicle.images[0] || '/default-car.png'} 
                alt={vehicle.name}
                className="vehicle-image"
              />
              <div className="vehicle-rating">
                ⭐ {vehicle.rating.toFixed(1)}
              </div>
            </div>
            
            <div className="vehicle-info">
              <h3>{vehicle.name}</h3>
              <p className="vehicle-specs">
                {vehicle.brand} {vehicle.model} ({vehicle.year})
              </p>
              <p className="license-plate">{vehicle.licensePlate}</p>
              
              <div className="vehicle-features">
                <span className="feature">🔋 {vehicle.batteryCapacity} kWh</span>
                <span className="feature">🛣️ {vehicle.rangeKm} km range</span>
                <span className="feature">📍 {vehicle.currentLocation.stationName}</span>
              </div>
              
              <div className="price-info">
                <span className="price">{vehicle.pricePerHour.toLocaleString()} VND/hour</span>
                <span className="bookings">{vehicle.totalBookings} bookings</span>
              </div>
            </div>
            
            <div className="vehicle-actions">
              <button 
                onClick={() => viewVehicleDetails(vehicle.id)}
                className="details-btn"
              >
                View Details
              </button>
              <button 
                onClick={() => bookVehicle(vehicle.id)}
                className="book-btn"
              >
                Book Now
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
```

### 2. Create Booking

**Endpoint**: `POST /api/coowner/bookings`

```typescript
interface CreateBookingRequest {
  vehicleId: number;
  startTime: string;
  endTime: string;
  purpose: string;
  pickupLocationId: number;
  dropoffLocationId: number;
  additionalNotes?: string;
}

interface BookingResponse {
  bookingId: number;
  vehicleId: number;
  vehicleName: string;
  startTime: string;
  endTime: string;
  status: string;
  totalCost: number;
  estimatedDuration: number;
  confirmationCode: string;
  pickupLocation: string;
  dropoffLocation: string;
}

export const coOwnerBookingService = {
  async createBooking(bookingData: CreateBookingRequest): Promise<BaseResponse<BookingResponse>> {
    return await apiClient.post('/coowner/bookings', bookingData);
  },

  async getMyBookings(status?: string): Promise<BaseResponse<BookingResponse[]>> {
    const params = status ? `?status=${status}` : '';
    return await apiClient.get(`/coowner/bookings${params}`);
  },

  async cancelBooking(bookingId: number, reason?: string): Promise<BaseResponse<any>> {
    return await apiClient.delete(`/coowner/bookings/${bookingId}`, {
      data: { reason }
    });
  }
};

// Booking form component
const BookingForm: React.FC<{ vehicle: AvailableVehicle; onSuccess: () => void }> = ({ 
  vehicle, 
  onSuccess 
}) => {
  const [bookingData, setBookingData] = useState<CreateBookingRequest>({
    vehicleId: vehicle.id,
    startTime: '',
    endTime: '',
    purpose: '',
    pickupLocationId: 0,
    dropoffLocationId: 0,
    additionalNotes: ''
  });
  
  const [estimatedCost, setEstimatedCost] = useState(0);

  const calculateCost = () => {
    if (bookingData.startTime && bookingData.endTime) {
      const start = new Date(bookingData.startTime);
      const end = new Date(bookingData.endTime);
      const hours = (end.getTime() - start.getTime()) / (1000 * 60 * 60);
      setEstimatedCost(hours * vehicle.pricePerHour);
    }
  };

  useEffect(() => {
    calculateCost();
  }, [bookingData.startTime, bookingData.endTime]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      const response = await coOwnerBookingService.createBooking(bookingData);
      if (response.statusCode === 201) {
        toast.success('Booking created successfully!');
        onSuccess();
      }
    } catch (error) {
      toast.error('Failed to create booking');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="booking-form">
      <h2>Book {vehicle.name}</h2>
      
      <div className="form-row">
        <div className="form-group">
          <label>Start Date & Time</label>
          <input
            type="datetime-local"
            value={bookingData.startTime}
            onChange={(e) => setBookingData({
              ...bookingData,
              startTime: e.target.value
            })}
            required
          />
        </div>
        
        <div className="form-group">
          <label>End Date & Time</label>
          <input
            type="datetime-local"
            value={bookingData.endTime}
            onChange={(e) => setBookingData({
              ...bookingData,
              endTime: e.target.value
            })}
            required
          />
        </div>
      </div>
      
      <div className="form-group">
        <label>Purpose</label>
        <select
          value={bookingData.purpose}
          onChange={(e) => setBookingData({
            ...bookingData,
            purpose: e.target.value
          })}
          required
        >
          <option value="">Select purpose</option>
          <option value="Personal">Personal</option>
          <option value="Business">Business</option>
          <option value="Tourism">Tourism</option>
          <option value="Emergency">Emergency</option>
          <option value="Other">Other</option>
        </select>
      </div>
      
      <div className="form-group">
        <label>Additional Notes</label>
        <textarea
          value={bookingData.additionalNotes}
          onChange={(e) => setBookingData({
            ...bookingData,
            additionalNotes: e.target.value
          })}
          placeholder="Any special requirements or notes..."
          rows={3}
        />
      </div>
      
      <div className="cost-estimate">
        <h3>Estimated Cost: {estimatedCost.toLocaleString()} VND</h3>
      </div>
      
      <div className="form-actions">
        <button type="button" onClick={() => onSuccess()}>Cancel</button>
        <button type="submit" className="primary">Confirm Booking</button>
      </div>
    </form>
  );
};
```

### 3. My Bookings

```typescript
const MyBookings: React.FC = () => {
  const [bookings, setBookings] = useState<BookingResponse[]>([]);
  const [filter, setFilter] = useState<string>('all');

  useEffect(() => {
    loadBookings();
  }, [filter]);

  const loadBookings = async () => {
    try {
      const response = await coOwnerBookingService.getMyBookings(
        filter === 'all' ? undefined : filter
      );
      
      if (response.statusCode === 200) {
        setBookings(response.data);
      }
    } catch (error) {
      console.error('Failed to load bookings:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'confirmed': return 'green';
      case 'pending': return 'orange';
      case 'active': return 'blue';
      case 'completed': return 'gray';
      case 'cancelled': return 'red';
      default: return 'gray';
    }
  };

  return (
    <div className="my-bookings">
      <div className="page-header">
        <h2>My Bookings</h2>
        <select value={filter} onChange={(e) => setFilter(e.target.value)}>
          <option value="all">All Bookings</option>
          <option value="pending">Pending</option>
          <option value="confirmed">Confirmed</option>
          <option value="active">Active</option>
          <option value="completed">Completed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>
      
      <div className="bookings-list">
        {bookings.map(booking => (
          <div key={booking.bookingId} className="booking-card">
            <div className="booking-header">
              <h3>{booking.vehicleName}</h3>
              <span className={`status-badge ${getStatusColor(booking.status)}`}>
                {booking.status}
              </span>
            </div>
            
            <div className="booking-details">
              <div className="detail-row">
                <span className="label">Booking ID:</span>
                <span className="value">{booking.confirmationCode}</span>
              </div>
              <div className="detail-row">
                <span className="label">Date & Time:</span>
                <span className="value">
                  {new Date(booking.startTime).toLocaleString()} - 
                  {new Date(booking.endTime).toLocaleString()}
                </span>
              </div>
              <div className="detail-row">
                <span className="label">Duration:</span>
                <span className="value">{booking.estimatedDuration} hours</span>
              </div>
              <div className="detail-row">
                <span className="label">Total Cost:</span>
                <span className="value">{booking.totalCost.toLocaleString()} VND</span>
              </div>
              <div className="detail-row">
                <span className="label">Pickup:</span>
                <span className="value">{booking.pickupLocation}</span>
              </div>
              <div className="detail-row">
                <span className="label">Dropoff:</span>
                <span className="value">{booking.dropoffLocation}</span>
              </div>
            </div>
            
            <div className="booking-actions">
              <button onClick={() => viewBookingDetails(booking.bookingId)}>
                View Details
              </button>
              
              {booking.status === 'pending' && (
                <button 
                  onClick={() => cancelBooking(booking.bookingId)}
                  className="cancel-btn"
                >
                  Cancel
                </button>
              )}
              
              {booking.status === 'confirmed' && (
                <button 
                  onClick={() => modifyBooking(booking.bookingId)}
                  className="modify-btn"
                >
                  Modify
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

## 💰 Fund Management

### 1. View Fund Balance

**Endpoint**: `GET /api/coowner/funds`

```typescript
interface FundInfo {
  groupId: number;
  groupName: string;
  currentBalance: number;
  myContribution: number;
  ownershipPercentage: number;
  recentTransactions: Array<{
    id: number;
    type: 'addition' | 'usage';
    amount: number;
    description: string;
    date: string;
    status: string;
  }>;
  monthlyStatement: {
    totalAdded: number;
    totalUsed: number;
    netChange: number;
  };
}

export const coOwnerFundService = {
  async getFundInfo(): Promise<BaseResponse<FundInfo[]>> {
    return await apiClient.get('/coowner/funds');
  },

  async addFunds(request: AddFundsRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/coowner/funds/add', request);
  },

  async getFundHistory(groupId: number, page: number = 1): Promise<BaseResponse<any>> {
    return await apiClient.get(`/coowner/funds/${groupId}/history?page=${page}`);
  }
};
```

### 2. Add Funds

```typescript
interface AddFundsRequest {
  groupId: number;
  amount: number;
  paymentMethod: 'bank_transfer' | 'credit_card' | 'debit_card';
  description?: string;
}

const AddFundsForm: React.FC<{ groupId: number; onSuccess: () => void }> = ({ 
  groupId, 
  onSuccess 
}) => {
  const [fundData, setFundData] = useState<AddFundsRequest>({
    groupId,
    amount: 0,
    paymentMethod: 'bank_transfer',
    description: ''
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (fundData.amount <= 0) {
      toast.error('Amount must be greater than 0');
      return;
    }

    try {
      const response = await coOwnerFundService.addFunds(fundData);
      if (response.statusCode === 200) {
        toast.success('Funds added successfully');
        onSuccess();
      }
    } catch (error) {
      toast.error('Failed to add funds');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="add-funds-form">
      <h3>Add Funds</h3>
      
      <div className="form-group">
        <label>Amount (VND)</label>
        <input
          type="number"
          min="10000"
          step="10000"
          value={fundData.amount}
          onChange={(e) => setFundData({
            ...fundData,
            amount: parseFloat(e.target.value)
          })}
          required
        />
      </div>
      
      <div className="form-group">
        <label>Payment Method</label>
        <select
          value={fundData.paymentMethod}
          onChange={(e) => setFundData({
            ...fundData,
            paymentMethod: e.target.value as any
          })}
          required
        >
          <option value="bank_transfer">Bank Transfer</option>
          <option value="credit_card">Credit Card</option>
          <option value="debit_card">Debit Card</option>
        </select>
      </div>
      
      <div className="form-group">
        <label>Description (Optional)</label>
        <input
          type="text"
          value={fundData.description}
          onChange={(e) => setFundData({
            ...fundData,
            description: e.target.value
          })}
          placeholder="Purpose of this fund addition..."
        />
      </div>
      
      <div className="form-actions">
        <button type="button" onClick={() => onSuccess()}>Cancel</button>
        <button type="submit" className="primary">Add Funds</button>
      </div>
    </form>
  );
};
```

## 📊 Co-Owner Dashboard

```typescript
interface CoOwnerDashboard {
  summary: {
    totalGroups: number;
    totalVehicles: number;
    activeBookings: number;
    thisMonthUsage: number;
  };
  upcomingBookings: BookingResponse[];
  recentActivity: Array<{
    id: number;
    type: string;
    description: string;
    timestamp: string;
  }>;
  fundSummary: Array<{
    groupName: string;
    balance: number;
    myContribution: number;
  }>;
  notifications: Array<{
    id: number;
    type: string;
    title: string;
    message: string;
    timestamp: string;
    isRead: boolean;
  }>;
}

export const coOwnerDashboardService = {
  async getDashboardData(): Promise<BaseResponse<CoOwnerDashboard>> {
    return await apiClient.get('/coowner/dashboard');
  }
};

const CoOwnerDashboard: React.FC = () => {
  const [dashboardData, setDashboardData] = useState<CoOwnerDashboard | null>(null);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      const response = await coOwnerDashboardService.getDashboardData();
      if (response.statusCode === 200) {
        setDashboardData(response.data);
      }
    } catch (error) {
      console.error('Failed to load dashboard:', error);
    }
  };

  if (!dashboardData) return <div>Loading dashboard...</div>;

  return (
    <div className="coowner-dashboard">
      <h1>Welcome Back!</h1>
      
      {/* Summary Cards */}
      <div className="summary-cards">
        <div className="summary-card">
          <h3>Groups</h3>
          <p className="summary-number">{dashboardData.summary.totalGroups}</p>
        </div>
        <div className="summary-card">
          <h3>Vehicles</h3>
          <p className="summary-number">{dashboardData.summary.totalVehicles}</p>
        </div>
        <div className="summary-card">
          <h3>Active Bookings</h3>
          <p className="summary-number">{dashboardData.summary.activeBookings}</p>
        </div>
        <div className="summary-card">
          <h3>This Month Usage</h3>
          <p className="summary-number">{dashboardData.summary.thisMonthUsage}h</p>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="quick-actions">
        <button onClick={() => navigate('/book-vehicle')} className="action-btn primary">
          Book Vehicle
        </button>
        <button onClick={() => navigate('/add-funds')} className="action-btn secondary">
          Add Funds
        </button>
        <button onClick={() => navigate('/my-bookings')} className="action-btn secondary">
          My Bookings
        </button>
      </div>

      {/* Two Column Layout */}
      <div className="dashboard-content">
        {/* Left Column */}
        <div className="left-column">
          {/* Upcoming Bookings */}
          <div className="dashboard-section">
            <h2>Upcoming Bookings</h2>
            {dashboardData.upcomingBookings.length > 0 ? (
              <div className="upcoming-bookings">
                {dashboardData.upcomingBookings.map(booking => (
                  <div key={booking.bookingId} className="booking-item">
                    <h4>{booking.vehicleName}</h4>
                    <p>{new Date(booking.startTime).toLocaleDateString()}</p>
                    <p>{new Date(booking.startTime).toLocaleTimeString()} - 
                       {new Date(booking.endTime).toLocaleTimeString()}</p>
                    <span className="status">{booking.status}</span>
                  </div>
                ))}
              </div>
            ) : (
              <p>No upcoming bookings</p>
            )}
          </div>

          {/* Fund Summary */}
          <div className="dashboard-section">
            <h2>Fund Summary</h2>
            <div className="fund-summary">
              {dashboardData.fundSummary.map((fund, index) => (
                <div key={index} className="fund-item">
                  <h4>{fund.groupName}</h4>
                  <p>Balance: {fund.balance.toLocaleString()} VND</p>
                  <p>My Contribution: {fund.myContribution.toLocaleString()} VND</p>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Right Column */}
        <div className="right-column">
          {/* Notifications */}
          <div className="dashboard-section">
            <h2>Notifications</h2>
            <div className="notifications">
              {dashboardData.notifications.map(notification => (
                <div 
                  key={notification.id} 
                  className={`notification-item ${!notification.isRead ? 'unread' : ''}`}
                >
                  <h4>{notification.title}</h4>
                  <p>{notification.message}</p>
                  <span className="timestamp">
                    {new Date(notification.timestamp).toLocaleString()}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Recent Activity */}
          <div className="dashboard-section">
            <h2>Recent Activity</h2>
            <div className="recent-activity">
              {dashboardData.recentActivity.map(activity => (
                <div key={activity.id} className="activity-item">
                  <span className="activity-type">{activity.type}</span>
                  <p>{activity.description}</p>
                  <span className="timestamp">
                    {new Date(activity.timestamp).toLocaleString()}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
```

---

**Next Step**: Tôi sẽ tiếp tục tạo file `README_FRONTEND_LICENSE.md` trong message tiếp theo để tránh request quá lớn.