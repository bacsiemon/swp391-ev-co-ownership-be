# Authentication & Authorization Guide

## 🔐 Overview

This guide covers authentication and authorization implementation for the EV Co-Ownership system. The backend uses JWT tokens with refresh token mechanism.

## 🏗️ Authentication Flow

```
User Login → Validate Credentials → Generate JWT + Refresh Token → Store in Frontend → Use for API Calls
```

## 📡 API Endpoints

### Base URL
```typescript
const AUTH_BASE_URL = '/api/Auth';
```

## 🔑 Authentication Endpoints

### 1. User Login

**Endpoint**: `POST /api/Auth/login`

#### Request Interface
```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```

#### Response Interface
```typescript
interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: number; // 0=CoOwner, 1=Staff, 2=Admin
    status: number; // 0=Active, 1=Inactive, 2=Suspended
  };
}
```

#### Implementation Example
```typescript
// services/authService.ts
import apiClient from './api';

export const authService = {
  async login(credentials: LoginRequest): Promise<BaseResponse<LoginResponse>> {
    return await apiClient.post('/Auth/login', credentials);
  }
};

// components/LoginForm.tsx
const handleLogin = async (formData: LoginRequest) => {
  try {
    const response = await authService.login(formData);
    
    if (response.statusCode === 200) {
      // Store tokens
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.data.user));
      
      // Redirect based on role
      const userRole = response.data.user.role;
      switch (userRole) {
        case 2: // Admin
          navigate('/admin/dashboard');
          break;
        case 1: // Staff
          navigate('/staff/dashboard');
          break;
        case 0: // CoOwner
        default:
          navigate('/dashboard');
          break;
      }
    }
  } catch (error) {
    // Handle errors
    if (error.response?.status === 400) {
      setError('Invalid email or password');
    } else if (error.response?.status === 403) {
      setError('Account suspended or inactive');
    }
  }
};
```

#### Possible Responses
- **200**: `LOGIN_SUCCESS`
- **400**: `INVALID_EMAIL_OR_PASSWORD`
- **403**: `ACCOUNT_SUSPENDED`, `ACCOUNT_INACTIVE`

---

### 2. User Registration

**Endpoint**: `POST /api/Auth/register`

#### Request Interface
```typescript
interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phone?: string;
  dateOfBirth?: string; // YYYY-MM-DD
  address?: string;
}
```

#### Implementation Example
```typescript
export const authService = {
  async register(userData: RegisterRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/Auth/register', userData);
  }
};

// components/RegisterForm.tsx
const handleRegister = async (formData: RegisterRequest) => {
  try {
    const response = await authService.register(formData);
    
    if (response.statusCode === 201) {
      toast.success('Registration successful! Please login.');
      navigate('/login');
    }
  } catch (error) {
    if (error.response?.status === 409) {
      setError('Email already exists');
    } else if (error.response?.status === 400) {
      // Handle validation errors
      const errors = error.response.data.errors;
      setValidationErrors(errors);
    }
  }
};
```

#### Validation Rules
- **Email**: Required, valid format, unique
- **Password**: Minimum 8 characters, must contain uppercase, lowercase, number, special character
- **ConfirmPassword**: Must match password
- **FirstName & LastName**: Required
- **Phone**: Optional, valid format
- **DateOfBirth**: Optional, valid date

#### Possible Responses
- **201**: `REGISTRATION_SUCCESS`
- **400**: Validation errors (see validation rules)
- **409**: `EMAIL_ALREADY_EXISTS`

---

### 3. Token Refresh

**Endpoint**: `POST /api/Auth/refresh-token`

#### Request Interface
```typescript
interface RefreshTokenRequest {
  refreshToken: string;
}
```

#### Implementation Example
```typescript
export const authService = {
  async refreshToken(refreshToken: string): Promise<BaseResponse<LoginResponse>> {
    return await apiClient.post('/Auth/refresh-token', { refreshToken });
  }
};

// utils/tokenManager.ts
export const refreshAccessToken = async (): Promise<boolean> => {
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return false;

    const response = await authService.refreshToken(refreshToken);
    
    if (response.statusCode === 200) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      return true;
    }
    return false;
  } catch (error) {
    // Clear tokens on refresh failure
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    return false;
  }
};
```

#### Possible Responses
- **200**: `TOKEN_REFRESH_SUCCESS`
- **401**: `INVALID_OR_EXPIRED_REFRESH_TOKEN`
- **403**: `ACCOUNT_SUSPENDED`, `ACCOUNT_INACTIVE`
- **404**: `USER_NOT_FOUND`

---

### 4. Forgot Password

**Endpoint**: `POST /api/Auth/forgot-password`

#### Request Interface
```typescript
interface ForgotPasswordRequest {
  email: string;
}
```

#### Implementation Example
```typescript
export const authService = {
  async forgotPassword(email: string): Promise<BaseResponse<any>> {
    return await apiClient.post('/Auth/forgot-password', { email });
  }
};

// components/ForgotPasswordForm.tsx
const handleForgotPassword = async (email: string) => {
  try {
    const response = await authService.forgotPassword(email);
    
    if (response.statusCode === 200) {
      toast.success('OTP sent to your email');
      setShowOtpForm(true);
    }
  } catch (error) {
    if (error.response?.status === 404) {
      setError('Email not found');
    }
  }
};
```

#### Possible Responses
- **200**: `SUCCESS`
- **404**: `USER_NOT_FOUND`

---

### 5. Reset Password

**Endpoint**: `PATCH /api/Auth/reset-password`

#### Request Interface
```typescript
interface ResetPasswordRequest {
  email: string;
  otp: string;
  newPassword: string;
}
```

#### Implementation Example
```typescript
export const authService = {
  async resetPassword(data: ResetPasswordRequest): Promise<BaseResponse<any>> {
    return await apiClient.patch('/Auth/reset-password', data);
  }
};

// components/ResetPasswordForm.tsx
const handleResetPassword = async (formData: ResetPasswordRequest) => {
  try {
    const response = await authService.resetPassword(formData);
    
    if (response.statusCode === 200) {
      toast.success('Password reset successful');
      navigate('/login');
    }
  } catch (error) {
    if (error.response?.status === 400) {
      setError('Invalid OTP or password format');
    } else if (error.response?.status === 404) {
      setError('User not found');
    }
  }
};
```

#### Possible Responses
- **200**: `SUCCESS`
- **400**: Validation errors
- **404**: `USER_NOT_FOUND`

---

### 6. Basic License Verification

**Endpoint**: `POST /api/Auth/verify-license`

#### Request Interface
```typescript
interface VerifyLicenseRequest {
  licenseNumber: string;
  issuedBy: string;
  issueDate: string; // YYYY-MM-DD
  expiryDate?: string; // YYYY-MM-DD
  firstName: string;
  lastName: string;
}
```

#### Implementation Example
```typescript
export const authService = {
  async verifyLicense(licenseData: VerifyLicenseRequest): Promise<BaseResponse<any>> {
    return await apiClient.post('/Auth/verify-license', licenseData);
  }
};
```

#### Possible Responses
- **200**: `LICENSE_VERIFICATION_SUCCESS`
- **400**: Validation errors
- **409**: `LICENSE_ALREADY_REGISTERED`
- **500**: `INTERNAL_SERVER_ERROR`

---

## 🔒 Authorization Implementation

### Protected Route Component
```typescript
// components/ProtectedRoute.tsx
import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: number; // 0=CoOwner, 1=Staff, 2=Admin
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRole 
}) => {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole !== undefined && user?.role !== requiredRole) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;
```

### Auth Context Implementation
```typescript
// context/AuthContext.tsx
import React, { createContext, useContext, useReducer, useEffect } from 'react';

interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  status: number;
}

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

interface AuthContextType extends AuthState {
  login: (response: LoginResponse) => void;
  logout: () => void;
  updateUser: (user: User) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

const authReducer = (state: AuthState, action: any): AuthState => {
  switch (action.type) {
    case 'LOGIN':
      return {
        ...state,
        user: action.payload.user,
        accessToken: action.payload.accessToken,
        refreshToken: action.payload.refreshToken,
        isAuthenticated: true,
        isLoading: false,
      };
    case 'LOGOUT':
      return {
        ...state,
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,
      };
    case 'UPDATE_USER':
      return {
        ...state,
        user: action.payload,
      };
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.payload,
      };
    default:
      return state;
  }
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, {
    user: null,
    accessToken: null,
    refreshToken: null,
    isAuthenticated: false,
    isLoading: true,
  });

  useEffect(() => {
    // Check for stored tokens on app start
    const storedToken = localStorage.getItem('accessToken');
    const storedUser = localStorage.getItem('user');
    const storedRefreshToken = localStorage.getItem('refreshToken');

    if (storedToken && storedUser && storedRefreshToken) {
      dispatch({
        type: 'LOGIN',
        payload: {
          accessToken: storedToken,
          refreshToken: storedRefreshToken,
          user: JSON.parse(storedUser),
        },
      });
    } else {
      dispatch({ type: 'SET_LOADING', payload: false });
    }
  }, []);

  const login = (response: LoginResponse) => {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.user));
    
    dispatch({
      type: 'LOGIN',
      payload: response,
    });
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    
    dispatch({ type: 'LOGOUT' });
  };

  const updateUser = (user: User) => {
    localStorage.setItem('user', JSON.stringify(user));
    dispatch({ type: 'UPDATE_USER', payload: user });
  };

  return (
    <AuthContext.Provider
      value={{
        ...state,
        login,
        logout,
        updateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
```

### Route Configuration Example
```typescript
// App.tsx
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          
          {/* Protected routes for Co-owners */}
          <Route path="/dashboard" element={
            <ProtectedRoute requiredRole={0}>
              <CoOwnerDashboard />
            </ProtectedRoute>
          } />
          
          {/* Protected routes for Staff */}
          <Route path="/staff/*" element={
            <ProtectedRoute requiredRole={1}>
              <StaffRoutes />
            </ProtectedRoute>
          } />
          
          {/* Protected routes for Admin */}
          <Route path="/admin/*" element={
            <ProtectedRoute requiredRole={2}>
              <AdminRoutes />
            </ProtectedRoute>
          } />
        </Routes>
      </Router>
    </AuthProvider>
  );
}
```

## 🔄 Token Management Best Practices

### Automatic Token Refresh
```typescript
// utils/apiInterceptor.ts
import axios from 'axios';
import { refreshAccessToken } from './tokenManager';

// Response interceptor for automatic token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
      const refreshed = await refreshAccessToken();
      if (refreshed) {
        const newToken = localStorage.getItem('accessToken');
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } else {
        // Redirect to login
        window.location.href = '/login';
      }
    }
    
    return Promise.reject(error);
  }
);
```

## 🚨 Error Handling

### Common Authentication Errors
```typescript
// utils/errorHandler.ts
export const handleAuthError = (error: any) => {
  switch (error.response?.status) {
    case 400:
      return 'Invalid credentials or validation error';
    case 401:
      return 'Session expired. Please login again.';
    case 403:
      return 'Account suspended or insufficient permissions';
    case 409:
      return 'Email already exists';
    case 404:
      return 'User not found';
    default:
      return 'An unexpected error occurred';
  }
};
```

## 🧪 Testing Authentication

### Development Helper
For testing purposes, you can use this endpoint to get OTP:

**Endpoint**: `GET /api/Auth/test/get-forgot-password-otp?email={email}`

```typescript
// Only for development/testing
export const getTestOTP = async (email: string) => {
  return await apiClient.get(`/Auth/test/get-forgot-password-otp?email=${email}`);
};
```

---

**Next Steps**: 
- For Co-owner features → `README_FRONTEND_COOWNER.md`
- For Admin features → `README_FRONTEND_ADMIN.md`
- For Staff features → `README_FRONTEND_STAFF.md`