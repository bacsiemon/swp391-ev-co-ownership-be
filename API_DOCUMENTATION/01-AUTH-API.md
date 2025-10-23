# API Authentication - Xác thực người dùng

## 📋 Mục lục
- [Tổng quan](#tổng-quan)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Danh sách API](#danh-sách-api)
- [Chi tiết từng API](#chi-tiết-từng-api)
- [Enums và Constants](#enums-và-constants)
- [Error Codes](#error-codes)
- [Ví dụ sử dụng](#ví-dụ-sử-dụng)

---

## 🎯 Tổng quan

Module Authentication cung cấp các chức năng xác thực và quản lý tài khoản người dùng trong hệ thống EV Co-ownership, bao gồm:
- Đăng nhập/Đăng ký
- Refresh token
- Quên mật khẩu và đặt lại mật khẩu
- Xác minh giấy phép lái xe (basic)

**Lưu ý quan trọng:**
- JWT Token được sử dụng để xác thực
- Refresh Token để duy trì phiên đăng nhập
- Hệ thống OTP qua email cho việc đặt lại mật khẩu

---

## 🔗 Base URL

```
http://localhost:5215/api/auth
```

Trong production: `https://your-domain.com/api/auth`

---

## 🔐 Authentication

Hầu hết các API yêu cầu JWT Bearer Token trong header:

```http
Authorization: Bearer {access_token}
```

**Ngoại lệ (không cần token):**
- POST `/login`
- POST `/register`
- POST `/forgot-password`
- PATCH `/reset-password`

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Auth Required |
|-----|--------|----------|-------|---------------|
| 1 | POST | `/login` | Đăng nhập hệ thống | ❌ |
| 2 | POST | `/register` | Đăng ký tài khoản mới | ❌ |
| 3 | POST | `/refresh-token` | Làm mới access token | ❌ |
| 4 | POST | `/forgot-password` | Gửi OTP qua email để reset password | ❌ |
| 5 | PATCH | `/reset-password` | Đặt lại mật khẩu bằng OTP | ❌ |
| 6 | POST | `/verify-license` | Xác minh giấy phép lái xe (basic) | ❌ |
| 7 | GET | `/test/get-forgot-password-otp` | [DEV] Lấy OTP test | ❌ |

---

## 📖 Chi tiết từng API

### 1. 🔑 Đăng nhập - POST `/login`

**Mô tả:** Xác thực người dùng với email và password, trả về access token và refresh token.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd123"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | ✅ | Email format, max 100 chars |
| password | string | ✅ | Min 8 chars |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "LOGIN_SUCCESS",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "userId": 1,
      "email": "user@example.com",
      "firstName": "Nguyen",
      "lastName": "Van A",
      "role": "CoOwner",
      "status": "Active"
    }
  }
}
```

**Response 400 - Email/Password sai:**
```json
{
  "statusCode": 400,
  "message": "INVALID_EMAIL_OR_PASSWORD",
  "data": null,
  "errors": null
}
```

**Response 403 - Tài khoản bị khóa:**
```json
{
  "statusCode": 403,
  "message": "ACCOUNT_SUSPENDED",
  "data": null
}
```

**Response 403 - Tài khoản chưa kích hoạt:**
```json
{
  "statusCode": 403,
  "message": "ACCOUNT_INACTIVE",
  "data": null
}
```

**Validation Errors:**
- `EMAIL_REQUIRED` - Email là bắt buộc
- `INVALID_EMAIL_FORMAT` - Định dạng email không hợp lệ
- `PASSWORD_REQUIRED` - Mật khẩu là bắt buộc

---

### 2. 📝 Đăng ký - POST `/register`

**Mô tả:** Tạo tài khoản người dùng mới trong hệ thống.

**Request Body:**
```json
{
  "email": "newuser@example.com",
  "password": "SecureP@ss123",
  "confirmPassword": "SecureP@ss123",
  "firstName": "Nguyen",
  "lastName": "Van B"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | ✅ | Email format, max 100 chars |
| password | string | ✅ | Min 8 chars, uppercase + lowercase + number + special char |
| confirmPassword | string | ✅ | Phải trùng với password |
| firstName | string | ✅ | Max 50 chars |
| lastName | string | ✅ | Max 50 chars |

**Response 201 - Thành công:**
```json
{
  "statusCode": 201,
  "message": "REGISTRATION_SUCCESS",
  "data": {
    "userId": 5,
    "email": "newuser@example.com",
    "firstName": "Nguyen",
    "lastName": "Van B",
    "role": "User",
    "status": "Active",
    "createdAt": "2025-01-17T10:30:00Z"
  }
}
```

**Response 409 - Email đã tồn tại:**
```json
{
  "statusCode": 409,
  "message": "EMAIL_ALREADY_EXISTS",
  "data": null
}
```

**Validation Errors:**
- `EMAIL_REQUIRED` - Email là bắt buộc
- `INVALID_EMAIL_FORMAT` - Email không đúng định dạng
- `PASSWORD_REQUIRED` - Password là bắt buộc
- `PASSWORD_MIN_8_CHARACTERS` - Password tối thiểu 8 ký tự
- `PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL` - Password phải có chữ hoa, chữ thường, số và ký tự đặc biệt
- `CONFIRM_PASSWORD_MUST_MATCH` - Confirm password không khớp
- `FIRST_NAME_REQUIRED` - Tên là bắt buộc
- `LAST_NAME_REQUIRED` - Họ là bắt buộc

**Quy tắc Password:**
- Tối thiểu 8 ký tự
- Ít nhất 1 chữ HOA (A-Z)
- Ít nhất 1 chữ thường (a-z)
- Ít nhất 1 số (0-9)
- Ít nhất 1 ký tự đặc biệt (@$!%*?&)

---

### 3. 🔄 Làm mới Token - POST `/refresh-token`

**Mô tả:** Sử dụng refresh token để lấy access token mới khi token cũ hết hạn.

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| refreshToken | string | ✅ | JWT format |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "TOKEN_REFRESH_SUCCESS",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  }
}
```

**Response 401 - Refresh token không hợp lệ:**
```json
{
  "statusCode": 401,
  "message": "INVALID_OR_EXPIRED_REFRESH_TOKEN",
  "data": null
}
```

**Response 404 - Không tìm thấy user:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

**Response 403 - Tài khoản bị khóa:**
```json
{
  "statusCode": 403,
  "message": "ACCOUNT_SUSPENDED",
  "data": null
}
```

---

### 4. 🔐 Quên mật khẩu - POST `/forgot-password`

**Mô tả:** Gửi mã OTP qua email để người dùng đặt lại mật khẩu.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | ✅ | Email format |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "SUCCESS",
  "data": {
    "email": "user@example.com",
    "message": "OTP has been sent to your email",
    "expiresIn": 600
  }
}
```

**Response 404 - Email không tồn tại:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

**Lưu ý:**
- OTP có hiệu lực 10 phút
- Mỗi email chỉ có thể nhận 1 OTP active tại một thời điểm
- OTP gồm 6 chữ số

---

### 5. 🔓 Đặt lại mật khẩu - PATCH `/reset-password`

**Mô tả:** Đặt lại mật khẩu mới bằng mã OTP đã nhận qua email.

**Request Body:**
```json
{
  "email": "user@example.com",
  "otp": "123456",
  "newPassword": "NewP@ssw0rd123"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | ✅ | Email format |
| otp | string | ✅ | 6 digits |
| newPassword | string | ✅ | Min 8 chars, uppercase + lowercase + number + special char |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "SUCCESS",
  "data": {
    "email": "user@example.com",
    "message": "Password has been reset successfully"
  }
}
```

**Response 400 - OTP không đúng/hết hạn:**
```json
{
  "statusCode": 400,
  "message": "INVALID_OR_EXPIRED_OTP",
  "data": null
}
```

**Response 404 - Email không tồn tại:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

**Validation Errors:**
- `EMAIL_REQUIRED` - Email là bắt buộc
- `INVALID_EMAIL_FORMAT` - Email không hợp lệ
- `OTP_MIN_6_CHARACTERS` - OTP phải có 6 ký tự
- `NEW_PASSWORD_MIN_8_CHARACTERS` - Password mới tối thiểu 8 ký tự

---

### 6. 🪪 Xác minh giấy phép lái xe - POST `/verify-license`

**Mô tả:** Xác minh giấy phép lái xe cơ bản thông qua AuthService (chức năng nâng cao xem `/api/license`).

**Request Body:**
```json
{
  "licenseNumber": "012345678",
  "issueDate": "2020-05-15",
  "firstName": "Nguyen",
  "lastName": "Van A"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| licenseNumber | string | ✅ | Valid license format |
| issueDate | string | ✅ | ISO 8601 date format |
| firstName | string | ✅ | Max 50 chars |
| lastName | string | ✅ | Max 50 chars |

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "LICENSE_VERIFICATION_SUCCESS",
  "data": {
    "licenseNumber": "012345678",
    "isValid": true,
    "verificationDate": "2025-01-17T10:30:00Z"
  }
}
```

**Response 400 - Xác minh thất bại:**
```json
{
  "statusCode": 400,
  "message": "INVALID_LICENSE_FORMAT",
  "data": null
}
```

**Response 409 - Giấy phép đã được đăng ký:**
```json
{
  "statusCode": 409,
  "message": "LICENSE_ALREADY_REGISTERED",
  "data": null
}
```

**Validation Errors:**
- `LICENSE_NUMBER_REQUIRED` - Số giấy phép là bắt buộc
- `INVALID_LICENSE_FORMAT` - Định dạng số giấy phép không hợp lệ
- `ISSUE_DATE_REQUIRED` - Ngày cấp là bắt buộc
- `FIRST_NAME_REQUIRED` - Tên là bắt buộc
- `LAST_NAME_REQUIRED` - Họ là bắt buộc

---

### 7. 🧪 [Development] Lấy OTP Test - GET `/test/get-forgot-password-otp`

**Mô tả:** Endpoint test để lấy OTP đã tạo (chỉ dùng trong môi trường development).

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| email | string | ✅ | Email đã request OTP |

**Request:**
```http
GET /api/auth/test/get-forgot-password-otp?email=user@example.com
```

**Response 200 - Có OTP:**
```json
{
  "statusCode": 200,
  "message": "SUCCESS",
  "data": {
    "email": "user@example.com",
    "otp": "123456",
    "createdAt": "2025-01-17T10:30:00Z",
    "expiresAt": "2025-01-17T10:40:00Z"
  }
}
```

**Response 404 - Không có OTP:**
```json
{
  "statusCode": 404,
  "message": "OTP_NOT_FOUND",
  "data": null
}
```

**⚠️ Lưu ý:** Endpoint này chỉ hoạt động trong môi trường Development, sẽ bị vô hiệu hóa trong Production.

---

## 🔢 Enums và Constants

### User Role (EUserRole)
```typescript
enum EUserRole {
  Admin = 0,      // Quản trị viên
  Staff = 1,      // Nhân viên
  CoOwner = 2,    // Đồng sở hữu (có thể tạo/tham gia nhóm xe)
  User = 3        // Người dùng thường (chưa là co-owner)
}
```

### User Status (EUserStatus)
```typescript
enum EUserStatus {
  Active = 0,     // Tài khoản hoạt động bình thường
  Inactive = 1,   // Tài khoản chưa kích hoạt
  Suspended = 2   // Tài khoản bị khóa/đình chỉ
}
```

---

## ❌ Error Codes

### Authentication Errors (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 400 | Bad Request | `INVALID_EMAIL_OR_PASSWORD` | Email hoặc password sai |
| 401 | Unauthorized | `INVALID_TOKEN` | Token không hợp lệ |
| 401 | Unauthorized | `INVALID_OR_EXPIRED_REFRESH_TOKEN` | Refresh token hết hạn hoặc không hợp lệ |
| 403 | Forbidden | `ACCOUNT_SUSPENDED` | Tài khoản bị khóa |
| 403 | Forbidden | `ACCOUNT_INACTIVE` | Tài khoản chưa kích hoạt |
| 404 | Not Found | `USER_NOT_FOUND` | Không tìm thấy người dùng |
| 409 | Conflict | `EMAIL_ALREADY_EXISTS` | Email đã được đăng ký |
| 409 | Conflict | `LICENSE_ALREADY_REGISTERED` | Giấy phép đã được đăng ký |

### Validation Errors (400)
| Code | Ý nghĩa |
|------|---------|
| `EMAIL_REQUIRED` | Email là bắt buộc |
| `INVALID_EMAIL_FORMAT` | Email không đúng định dạng |
| `PASSWORD_REQUIRED` | Password là bắt buộc |
| `PASSWORD_MIN_8_CHARACTERS` | Password tối thiểu 8 ký tự |
| `PASSWORD_MUST_CONTAIN_UPPERCASE_LOWERCASE_NUMBER_SPECIAL` | Password phải có chữ hoa, thường, số và ký tự đặc biệt |
| `CONFIRM_PASSWORD_MUST_MATCH` | Confirm password không khớp |
| `FIRST_NAME_REQUIRED` | Tên là bắt buộc |
| `LAST_NAME_REQUIRED` | Họ là bắt buộc |

### System Errors (5xx)
| Status | Code | Ý nghĩa |
|--------|------|---------|
| 500 | Internal Server Error | `INTERNAL_SERVER_ERROR` | Lỗi hệ thống |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Flow đăng ký và đăng nhập hoàn chỉnh

```javascript
// 1. Đăng ký tài khoản mới
const registerResponse = await fetch('http://localhost:5215/api/auth/register', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'newuser@example.com',
    password: 'SecureP@ss123',
    confirmPassword: 'SecureP@ss123',
    firstName: 'Nguyen',
    lastName: 'Van B'
  })
});

const registerData = await registerResponse.json();
console.log('User ID:', registerData.data.userId);

// 2. Đăng nhập với tài khoản vừa tạo
const loginResponse = await fetch('http://localhost:5215/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'newuser@example.com',
    password: 'SecureP@ss123'
  })
});

const loginData = await loginResponse.json();
const accessToken = loginData.data.accessToken;
const refreshToken = loginData.data.refreshToken;

// 3. Sử dụng access token để gọi API khác
const profileResponse = await fetch('http://localhost:5215/api/profile', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${accessToken}`
  }
});

const profileData = await profileResponse.json();
console.log('Profile:', profileData);
```

### Use Case 2: Xử lý token hết hạn với Refresh Token

```javascript
let accessToken = 'current_access_token';
let refreshToken = 'current_refresh_token';

async function callApiWithTokenRefresh(url, options = {}) {
  // Thử gọi API với access token hiện tại
  let response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${accessToken}`
    }
  });

  // Nếu token hết hạn (401), refresh token
  if (response.status === 401) {
    console.log('Access token expired, refreshing...');
    
    const refreshResponse = await fetch('http://localhost:5215/api/auth/refresh-token', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        refreshToken: refreshToken
      })
    });

    if (refreshResponse.ok) {
      const refreshData = await refreshResponse.json();
      accessToken = refreshData.data.accessToken;
      refreshToken = refreshData.data.refreshToken;

      // Lưu token mới vào localStorage
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);

      // Thử lại request với token mới
      response = await fetch(url, {
        ...options,
        headers: {
          ...options.headers,
          'Authorization': `Bearer ${accessToken}`
        }
      });
    } else {
      // Refresh token cũng hết hạn, redirect về login
      window.location.href = '/login';
      throw new Error('Session expired, please login again');
    }
  }

  return response;
}

// Sử dụng
const data = await callApiWithTokenRefresh('http://localhost:5215/api/vehicle/available');
```

### Use Case 3: Flow quên mật khẩu và đặt lại

```javascript
// 1. Người dùng quên mật khẩu, request OTP
const forgotResponse = await fetch('http://localhost:5215/api/auth/forgot-password', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com'
  })
});

const forgotData = await forgotResponse.json();
console.log(forgotData.data.message); // "OTP has been sent to your email"

// 2. [Development only] Lấy OTP để test
const otpResponse = await fetch('http://localhost:5215/api/auth/test/get-forgot-password-otp?email=user@example.com');
const otpData = await otpResponse.json();
const otp = otpData.data.otp; // "123456"

// 3. Người dùng nhập OTP và mật khẩu mới
const resetResponse = await fetch('http://localhost:5215/api/auth/reset-password', {
  method: 'PATCH',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com',
    otp: otp,
    newPassword: 'NewSecureP@ss456'
  })
});

const resetData = await resetResponse.json();
console.log(resetData.data.message); // "Password has been reset successfully"

// 4. Đăng nhập với mật khẩu mới
const loginResponse = await fetch('http://localhost:5215/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'NewSecureP@ss456'
  })
});
```

### Use Case 4: Xác minh giấy phép lái xe khi đăng ký

```javascript
// Sau khi đăng ký thành công, xác minh giấy phép lái xe
const verifyLicenseResponse = await fetch('http://localhost:5215/api/auth/verify-license', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    licenseNumber: '012345678',
    issueDate: '2020-05-15',
    firstName: 'Nguyen',
    lastName: 'Van B'
  })
});

const verifyData = await verifyLicenseResponse.json();

if (verifyData.statusCode === 200) {
  console.log('License verified successfully!');
  // Người dùng có thể tiếp tục đăng ký thành Co-owner
} else if (verifyData.statusCode === 409) {
  console.log('License already registered by another user');
} else {
  console.log('License verification failed:', verifyData.message);
}
```

---

## 🔐 Best Practices

### 1. Lưu trữ Token an toàn

```javascript
// ✅ TỐT: Lưu trong memory/localStorage với httpOnly cookie
localStorage.setItem('accessToken', accessToken);
sessionStorage.setItem('refreshToken', refreshToken); // Hoặc httpOnly cookie

// ❌ TỆ: Không lưu token trong code hoặc URL
const token = 'hardcoded_token_here'; // NEVER DO THIS!
window.location.href = '/dashboard?token=' + token; // NEVER DO THIS!
```

### 2. Xử lý lỗi chuẩn

```javascript
async function login(email, password) {
  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    const data = await response.json();

    switch(data.statusCode) {
      case 200:
        // Đăng nhập thành công
        saveTokens(data.data.accessToken, data.data.refreshToken);
        return { success: true, user: data.data.user };
      
      case 400:
        // Email hoặc password sai
        return { success: false, error: 'Email hoặc mật khẩu không đúng' };
      
      case 403:
        if (data.message === 'ACCOUNT_SUSPENDED') {
          return { success: false, error: 'Tài khoản đã bị khóa. Vui lòng liên hệ admin.' };
        } else if (data.message === 'ACCOUNT_INACTIVE') {
          return { success: false, error: 'Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email.' };
        }
        break;
      
      default:
        return { success: false, error: 'Đã xảy ra lỗi. Vui lòng thử lại.' };
    }
  } catch (error) {
    console.error('Login error:', error);
    return { success: false, error: 'Không thể kết nối đến server' };
  }
}
```

### 3. Auto-refresh token trước khi hết hạn

```javascript
// Kiểm tra token còn bao lâu hết hạn
function getTokenExpirationTime(token) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  return payload.exp * 1000; // Convert to milliseconds
}

// Tự động refresh token trước 5 phút khi sắp hết hạn
function setupTokenRefresh(accessToken, refreshToken) {
  const expirationTime = getTokenExpirationTime(accessToken);
  const currentTime = Date.now();
  const timeUntilExpiration = expirationTime - currentTime;
  const refreshTime = timeUntilExpiration - (5 * 60 * 1000); // 5 minutes before expiration

  if (refreshTime > 0) {
    setTimeout(async () => {
      const newTokens = await refreshAccessToken(refreshToken);
      if (newTokens) {
        setupTokenRefresh(newTokens.accessToken, newTokens.refreshToken);
      }
    }, refreshTime);
  }
}
```

---

## 📞 Liên hệ và Hỗ trợ

- **API Documentation:** http://localhost:5215/swagger
- **Backend Team:** [Your team contact]
- **Issues:** [GitHub Issues URL]

---

**Last Updated:** 2025-01-17  
**Version:** 1.0.0  
**Author:** Backend Development Team
