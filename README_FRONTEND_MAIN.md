# EV Co-Ownership Frontend Integration Guide

## 📋 Overview

This is the main integration guide for React frontend developers working with the EV Co-Ownership backend API. The system manages electric vehicle sharing among multiple co-owners with features like booking, maintenance tracking, fund management, and more.

## 🏗️ System Architecture

```
Frontend (React) ←→ Backend API (.NET Core) ←→ PostgreSQL Database
```

## 📚 Documentation Structure

This documentation is split into multiple files for better organization:

### Core Documentation Files

| File | Description | Target Audience |
|------|-------------|-----------------|
| `README_FRONTEND_AUTH.md` | Authentication & Authorization | All developers |
| `README_FRONTEND_ADMIN.md` | Admin functionality | Admin panel developers |
| `README_FRONTEND_STAFF.md` | Staff operations | Staff interface developers |
| `README_FRONTEND_COOWNER.md` | Co-owner features | Main user interface developers |
| `README_FRONTEND_LICENSE.md` | License verification | License management developers |
| `README_FRONTEND_GROUP.md` | Group/Vehicle management | Vehicle management developers |
| `README_FRONTEND_FILEUPLOAD.md` | File upload system | File handling developers |

## 🔧 Technical Prerequisites

### Required Knowledge
- React 18+
- TypeScript (recommended)
- Axios for HTTP requests
- JWT token handling
- Local storage management

### Backend Information
- **Base URL**: `https://localhost:7296/api` (Development)
- **Authentication**: JWT Bearer Token
- **Response Format**: Standardized `BaseResponse<T>`
- **Database**: PostgreSQL with Entity Framework Core

## 🌐 API Base Configuration

### Environment Setup
```typescript
// config/api.ts
export const API_CONFIG = {
  baseURL: process.env.REACT_APP_API_BASE_URL || 'https://localhost:7296/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  }
};
```

### Axios Instance Setup
```typescript
// services/api.ts
import axios from 'axios';
import { API_CONFIG } from '../config/api';

const apiClient = axios.create(API_CONFIG);

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for handling common responses
apiClient.interceptors.response.use(
  (response) => response.data, // Return BaseResponse<T> directly
  (error) => {
    if (error.response?.status === 401) {
      // Handle token expiration
      localStorage.removeItem('accessToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

## 📝 Standard Response Format

All API endpoints return responses in this format:

```typescript
interface BaseResponse<T> {
  statusCode: number;
  message: string;
  data?: T;
  additionalData?: any;
  errors?: any;
}
```

### Success Response Example
```json
{
  "statusCode": 200,
  "message": "LOGIN_SUCCESS",
  "data": {
    "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
    "refreshToken": "refresh_token_string",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": 0
    }
  }
}
```

### Error Response Example
```json
{
  "statusCode": 400,
  "message": "INVALID_EMAIL_OR_PASSWORD",
  "errors": {
    "email": ["Invalid email format"],
    "password": ["Password is required"]
  }
}
```

## 🔐 User Roles & Permissions

```typescript
enum UserRole {
  CoOwner = 0,
  Staff = 1,
  Admin = 2
}

enum UserStatus {
  Active = 0,
  Inactive = 1,
  Suspended = 2
}
```

## 🚀 Quick Start Integration Steps

1. **Setup Authentication** - Start with `README_FRONTEND_AUTH.md`
2. **Implement User Dashboard** - Use `README_FRONTEND_COOWNER.md`
3. **Add Admin Features** - Follow `README_FRONTEND_ADMIN.md`
4. **Integrate Staff Operations** - Use `README_FRONTEND_STAFF.md`
5. **Add License Management** - Follow `README_FRONTEND_LICENSE.md`
6. **Implement Vehicle Management** - Use `README_FRONTEND_GROUP.md`
7. **Setup File Upload** - Follow `README_FRONTEND_FILEUPLOAD.md`

## 📊 Database Entity Overview

### Core Entities
- **Users**: Authentication and user management
- **CoOwners**: Co-ownership specific data
- **Vehicles**: Electric vehicle information
- **Bookings**: Vehicle reservation system
- **Funds**: Financial management
- **DrivingLicenses**: License verification system

### Relationships
- Users → CoOwners (1:1)
- Vehicles → Bookings (1:N)
- Vehicles → VehicleCoOwners (1:N)
- Funds → FundAdditions/FundUsage (1:N)

## 🔄 State Management Recommendations

### Context for Authentication
```typescript
// context/AuthContext.tsx
interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  role: UserRole | null;
}
```

### Context for Vehicles/Bookings
```typescript
// context/VehicleContext.tsx
interface VehicleState {
  vehicles: Vehicle[];
  currentBooking: Booking | null;
  bookingHistory: Booking[];
}
```

## 🛠️ Development Tools

### TypeScript Interfaces
All API request/response interfaces are available in each specific README file.

### Testing Utilities
```typescript
// utils/testHelpers.ts
export const mockApiResponse = <T>(data: T): BaseResponse<T> => ({
  statusCode: 200,
  message: 'SUCCESS',
  data
});
```

## 📞 Support & Documentation

- **API Documentation**: Each controller has detailed Swagger documentation
- **Validation Rules**: FluentValidation rules are documented in each endpoint
- **Error Handling**: Comprehensive error codes and messages provided
- **Sample Data**: Database includes sample data for testing

## 🔗 Next Steps

Choose the appropriate documentation file based on the feature you're implementing:

- **Starting with authentication?** → `README_FRONTEND_AUTH.md`
- **Building admin dashboard?** → `README_FRONTEND_ADMIN.md`
- **Working on user features?** → `README_FRONTEND_COOWNER.md`
- **Implementing staff interface?** → `README_FRONTEND_STAFF.md`
- **Adding license verification?** → `README_FRONTEND_LICENSE.md`
- **Managing vehicles/groups?** → `README_FRONTEND_GROUP.md`
- **Handling file uploads?** → `README_FRONTEND_FILEUPLOAD.md`

---

**Happy coding! 🚗⚡**