# API User Management - Quản lý người dùng

## 📋 Mục lục
- [Tổng quan](#tổng-quan)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Danh sách API](#danh-sách-api)
- [Chi tiết từng API](#chi-tiết-từng-api)
- [Error Codes](#error-codes)
- [Ví dụ sử dụng](#ví-dụ-sử-dụng)

---

## 🎯 Tổng quan

Module User Management cung cấp các API để quản lý thông tin người dùng trong hệ thống, bao gồm:
- Xem danh sách người dùng (Admin/Staff)
- Xem thông tin chi tiết người dùng
- Cập nhật thông tin người dùng
- Xóa người dùng (Admin only)

**Đặc điểm:**
- Phân quyền chặt chẽ: User chỉ xem/sửa được thông tin của chính mình
- Admin/Staff có thể quản lý tất cả người dùng
- Tích hợp với Profile API cho quản lý hồ sơ chi tiết hơn

---

## 🔗 Base URL

```
http://localhost:5215/api/user
```

Trong production: `https://your-domain.com/api/user`

---

## 🔐 Authentication

**Tất cả API đều yêu cầu JWT Bearer Token:**

```http
Authorization: Bearer {access_token}
```

---

## 📑 Danh sách API

| STT | Method | Endpoint | Mô tả | Role Required |
|-----|--------|----------|-------|---------------|
| 1 | GET | `/` | Lấy danh sách người dùng (phân trang) | Admin, Staff |
| 2 | GET | `/{id}` | Xem thông tin người dùng theo ID | Any (chỉ xem của mình), Admin/Staff (xem tất cả) |
| 3 | PUT | `/{id}` | Cập nhật thông tin người dùng | Any (chỉ sửa của mình) |
| 4 | DELETE | `/{id}` | Xóa người dùng | Admin |

---

## 📖 Chi tiết từng API

### 1. 📋 Lấy danh sách người dùng - GET `/`

**Mô tả:** Lấy danh sách tất cả người dùng trong hệ thống với phân trang (chỉ Admin/Staff).

**Authorization:** `Admin, Staff`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| pageIndex | integer | ❌ | 1 | Số trang (bắt đầu từ 1) |
| pageSize | integer | ❌ | 10 | Số lượng items trên mỗi trang |

**Request:**
```http
GET /api/user?pageIndex=1&pageSize=20
Authorization: Bearer {admin_token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "USERS_RETRIEVED_SUCCESSFULLY",
  "data": {
    "items": [
      {
        "userId": 1,
        "email": "user1@example.com",
        "firstName": "Nguyen",
        "lastName": "Van A",
        "phone": "+84912345678",
        "address": "123 Nguyen Hue, HCM",
        "dateOfBirth": "1990-05-15",
        "role": "CoOwner",
        "status": "Active",
        "createdAt": "2024-01-15T10:30:00Z",
        "updatedAt": "2024-10-20T14:25:00Z"
      },
      {
        "userId": 2,
        "email": "user2@example.com",
        "firstName": "Tran",
        "lastName": "Thi B",
        "phone": "+84987654321",
        "address": "456 Le Loi, HCM",
        "dateOfBirth": "1992-08-20",
        "role": "CoOwner",
        "status": "Active",
        "createdAt": "2024-02-10T09:15:00Z",
        "updatedAt": "2024-10-18T16:40:00Z"
      }
    ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Response 401 - Chưa đăng nhập:**
```json
{
  "statusCode": 401,
  "message": "UNAUTHORIZED",
  "data": null
}
```

**Response 403 - Không có quyền:**
```json
{
  "statusCode": 403,
  "message": "ACCESS_DENIED",
  "data": null
}
```

---

### 2. 👤 Xem thông tin người dùng - GET `/{id}`

**Mô tả:** Lấy thông tin chi tiết của một người dùng theo ID.

**Authorization:**
- User thường: Chỉ xem được thông tin của chính mình
- Admin/Staff: Xem được thông tin của bất kỳ ai

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | ✅ | ID của người dùng cần xem |

**Request (User xem thông tin của chính mình):**
```http
GET /api/user/5
Authorization: Bearer {user_token}
```

**Request (Admin xem thông tin người dùng khác):**
```http
GET /api/user/10
Authorization: Bearer {admin_token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "USER_RETRIEVED_SUCCESSFULLY",
  "data": {
    "userId": 5,
    "email": "user@example.com",
    "firstName": "Nguyen",
    "lastName": "Van A",
    "phone": "+84912345678",
    "address": "123 Nguyen Hue, District 1, HCM",
    "dateOfBirth": "1990-05-15",
    "role": "CoOwner",
    "status": "Active",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-10-20T14:25:00Z"
  }
}
```

**Response 403 - User cố xem thông tin người khác:**
```json
{
  "statusCode": 403,
  "message": "ACCESS_DENIED",
  "data": null
}
```

**Response 404 - Không tìm thấy người dùng:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

---

### 3. ✏️ Cập nhật thông tin người dùng - PUT `/{id}`

**Mô tả:** Cập nhật thông tin cơ bản của người dùng (tên, số điện thoại, địa chỉ, ngày sinh).

**Authorization:** User chỉ có thể cập nhật thông tin của chính mình.

**Lưu ý:** Để cập nhật chi tiết hơn (ảnh đại diện, mật khẩu, v.v.), sử dụng Profile API (`/api/profile`).

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | ✅ | ID của người dùng cần cập nhật |

**Request Body:**
```json
{
  "fullName": "Nguyen Van A Updated",
  "phoneNumber": "+84912345999",
  "address": "456 Le Loi, District 3, HCM",
  "dateOfBirth": "1990-05-15"
}
```

**Request Schema:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| fullName | string | ❌ | Max 101 chars (firstName + space + lastName) |
| phoneNumber | string | ❌ | Vietnam phone format |
| address | string | ❌ | Max 200 chars |
| dateOfBirth | string | ❌ | ISO 8601 date, must be 18+ years old |

**Request:**
```http
PUT /api/user/5
Authorization: Bearer {user_token}
Content-Type: application/json

{
  "fullName": "Nguyen Van A Updated",
  "phoneNumber": "+84912345999",
  "address": "456 Le Loi, District 3, HCM",
  "dateOfBirth": "1990-05-15"
}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "USER_UPDATED_SUCCESSFULLY",
  "data": {
    "userId": 5,
    "email": "user@example.com",
    "firstName": "Nguyen",
    "lastName": "Van A Updated",
    "phone": "+84912345999",
    "address": "456 Le Loi, District 3, HCM",
    "dateOfBirth": "1990-05-15",
    "role": "CoOwner",
    "status": "Active",
    "updatedAt": "2025-10-24T10:30:00Z"
  }
}
```

**Response 400 - Validation error:**
```json
{
  "statusCode": 400,
  "message": "VALIDATION_ERROR",
  "errors": {
    "phoneNumber": ["INVALID_VIETNAM_PHONE_FORMAT"],
    "dateOfBirth": ["MUST_BE_AT_LEAST_18_YEARS_OLD"]
  }
}
```

**Response 403 - User cố sửa thông tin người khác:**
```json
{
  "statusCode": 403,
  "message": "ACCESS_DENIED",
  "data": null
}
```

**Response 404 - Không tìm thấy người dùng:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

---

### 4. 🗑️ Xóa người dùng - DELETE `/{id}`

**Mô tả:** Xóa một người dùng khỏi hệ thống (chỉ Admin).

**⚠️ Cảnh báo:** Thao tác này không thể hoàn tác. Nên cân nhắc sử dụng "suspend" thay vì xóa hoàn toàn.

**Authorization:** `Admin`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | ✅ | ID của người dùng cần xóa |

**Request:**
```http
DELETE /api/user/10
Authorization: Bearer {admin_token}
```

**Response 200 - Thành công:**
```json
{
  "statusCode": 200,
  "message": "USER_DELETED_SUCCESSFULLY",
  "data": {
    "deletedUserId": 10,
    "deletedAt": "2025-10-24T10:30:00Z"
  }
}
```

**Response 403 - Không có quyền:**
```json
{
  "statusCode": 403,
  "message": "ACCESS_DENIED",
  "data": null
}
```

**Response 404 - Không tìm thấy người dùng:**
```json
{
  "statusCode": 404,
  "message": "USER_NOT_FOUND",
  "data": null
}
```

---

## ❌ Error Codes

### Authentication & Authorization (4xx)
| Status | Code | Message | Ý nghĩa |
|--------|------|---------|---------|
| 401 | Unauthorized | `UNAUTHORIZED` | Chưa đăng nhập |
| 401 | Unauthorized | `INVALID_TOKEN` | Token không hợp lệ |
| 403 | Forbidden | `ACCESS_DENIED` | Không có quyền truy cập |
| 404 | Not Found | `USER_NOT_FOUND` | Không tìm thấy người dùng |

### Validation Errors (400)
| Code | Ý nghĩa |
|------|---------|
| `INVALID_VIETNAM_PHONE_FORMAT` | Số điện thoại không đúng định dạng Việt Nam |
| `MUST_BE_AT_LEAST_18_YEARS_OLD` | Phải đủ 18 tuổi |
| `ADDRESS_MAX_200_CHARACTERS` | Địa chỉ tối đa 200 ký tự |
| `FULL_NAME_REQUIRED` | Họ tên là bắt buộc |

---

## 💡 Ví dụ sử dụng

### Use Case 1: Admin xem danh sách người dùng với phân trang

```javascript
// Hàm lấy danh sách người dùng
async function getUserList(pageIndex = 1, pageSize = 20, adminToken) {
  try {
    const response = await fetch(
      `http://localhost:5215/api/user?pageIndex=${pageIndex}&pageSize=${pageSize}`,
      {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${adminToken}`
        }
      }
    );

    const data = await response.json();

    if (data.statusCode === 200) {
      console.log(`Total users: ${data.data.totalCount}`);
      console.log(`Page ${data.data.pageIndex} of ${data.data.totalPages}`);
      
      data.data.items.forEach(user => {
        console.log(`- ${user.firstName} ${user.lastName} (${user.email})`);
      });

      return {
        success: true,
        users: data.data.items,
        pagination: {
          pageIndex: data.data.pageIndex,
          totalPages: data.data.totalPages,
          totalCount: data.data.totalCount,
          hasNext: data.data.hasNextPage,
          hasPrevious: data.data.hasPreviousPage
        }
      };
    } else if (data.statusCode === 403) {
      return { success: false, error: 'Bạn không có quyền xem danh sách người dùng' };
    }
  } catch (error) {
    console.error('Error:', error);
    return { success: false, error: 'Không thể kết nối đến server' };
  }
}

// Sử dụng
const adminToken = 'admin_jwt_token_here';
const result = await getUserList(1, 20, adminToken);

if (result.success) {
  console.log('Users loaded:', result.users.length);
  
  // Load trang tiếp theo nếu có
  if (result.pagination.hasNext) {
    const nextPage = await getUserList(result.pagination.pageIndex + 1, 20, adminToken);
  }
}
```

### Use Case 2: User xem và cập nhật thông tin của chính mình

```javascript
// Component React để quản lý profile cơ bản
import React, { useState, useEffect } from 'react';

function UserProfileEditor() {
  const [user, setUser] = useState(null);
  const [editing, setEditing] = useState(false);
  const [formData, setFormData] = useState({});
  const userId = getCurrentUserId(); // Lấy từ token hoặc context
  const token = getAccessToken();

  // Lấy thông tin người dùng
  useEffect(() => {
    async function fetchUser() {
      const response = await fetch(`http://localhost:5215/api/user/${userId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      const data = await response.json();
      
      if (data.statusCode === 200) {
        setUser(data.data);
        setFormData({
          fullName: `${data.data.firstName} ${data.data.lastName}`,
          phoneNumber: data.data.phone || '',
          address: data.data.address || '',
          dateOfBirth: data.data.dateOfBirth || ''
        });
      }
    }
    fetchUser();
  }, [userId, token]);

  // Cập nhật thông tin
  async function handleSubmit(e) {
    e.preventDefault();
    
    const response = await fetch(`http://localhost:5215/api/user/${userId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(formData)
    });

    const data = await response.json();

    if (data.statusCode === 200) {
      setUser(data.data);
      setEditing(false);
      alert('Cập nhật thành công!');
    } else if (data.statusCode === 400) {
      alert('Lỗi validation: ' + JSON.stringify(data.errors));
    } else {
      alert('Đã xảy ra lỗi: ' + data.message);
    }
  }

  if (!user) return <div>Loading...</div>;

  return (
    <div>
      <h2>Thông tin cá nhân</h2>
      {!editing ? (
        <div>
          <p><strong>Email:</strong> {user.email}</p>
          <p><strong>Họ tên:</strong> {user.firstName} {user.lastName}</p>
          <p><strong>Số điện thoại:</strong> {user.phone || 'Chưa cập nhật'}</p>
          <p><strong>Địa chỉ:</strong> {user.address || 'Chưa cập nhật'}</p>
          <p><strong>Ngày sinh:</strong> {user.dateOfBirth || 'Chưa cập nhật'}</p>
          <p><strong>Vai trò:</strong> {user.role}</p>
          <p><strong>Trạng thái:</strong> {user.status}</p>
          <button onClick={() => setEditing(true)}>Chỉnh sửa</button>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          <div>
            <label>Họ tên:</label>
            <input
              type="text"
              value={formData.fullName}
              onChange={e => setFormData({...formData, fullName: e.target.value})}
            />
          </div>
          <div>
            <label>Số điện thoại:</label>
            <input
              type="tel"
              value={formData.phoneNumber}
              onChange={e => setFormData({...formData, phoneNumber: e.target.value})}
              placeholder="+84912345678"
            />
          </div>
          <div>
            <label>Địa chỉ:</label>
            <textarea
              value={formData.address}
              onChange={e => setFormData({...formData, address: e.target.value})}
              placeholder="123 Nguyen Hue, District 1, HCM"
            />
          </div>
          <div>
            <label>Ngày sinh:</label>
            <input
              type="date"
              value={formData.dateOfBirth}
              onChange={e => setFormData({...formData, dateOfBirth: e.target.value})}
            />
          </div>
          <button type="submit">Lưu</button>
          <button type="button" onClick={() => setEditing(false)}>Hủy</button>
        </form>
      )}
    </div>
  );
}
```

### Use Case 3: Admin xóa người dùng với xác nhận

```javascript
async function deleteUserWithConfirmation(userId, adminToken) {
  // Bước 1: Lấy thông tin người dùng trước
  const userResponse = await fetch(`http://localhost:5215/api/user/${userId}`, {
    headers: { 'Authorization': `Bearer ${adminToken}` }
  });
  
  const userData = await userResponse.json();
  
  if (userData.statusCode !== 200) {
    alert('Không tìm thấy người dùng');
    return;
  }

  const user = userData.data;

  // Bước 2: Xác nhận với admin
  const confirmed = confirm(
    `Bạn có chắc chắn muốn xóa người dùng:\n\n` +
    `- Họ tên: ${user.firstName} ${user.lastName}\n` +
    `- Email: ${user.email}\n` +
    `- Vai trò: ${user.role}\n\n` +
    `⚠️ Hành động này KHÔNG THỂ HOÀN TÁC!`
  );

  if (!confirmed) {
    console.log('Admin đã hủy thao tác xóa');
    return;
  }

  // Bước 3: Thực hiện xóa
  try {
    const deleteResponse = await fetch(`http://localhost:5215/api/user/${userId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${adminToken}` }
    });

    const deleteData = await deleteResponse.json();

    if (deleteData.statusCode === 200) {
      alert('Đã xóa người dùng thành công');
      // Refresh danh sách hoặc redirect
      window.location.reload();
    } else if (deleteData.statusCode === 403) {
      alert('Bạn không có quyền xóa người dùng');
    } else if (deleteData.statusCode === 404) {
      alert('Người dùng không tồn tại hoặc đã bị xóa');
    } else {
      alert('Đã xảy ra lỗi: ' + deleteData.message);
    }
  } catch (error) {
    console.error('Delete error:', error);
    alert('Không thể kết nối đến server');
  }
}

// Sử dụng
const adminToken = 'admin_jwt_token';
deleteUserWithConfirmation(10, adminToken);
```

### Use Case 4: Tạo DataTable với phân trang cho Admin

```javascript
// Component React DataTable với phân trang
import React, { useState, useEffect } from 'react';

function UserManagementTable() {
  const [users, setUsers] = useState([]);
  const [pagination, setPagination] = useState({
    pageIndex: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  });
  const [loading, setLoading] = useState(false);
  const adminToken = getAdminToken();

  // Load users
  async function loadUsers(page = 1, size = 10) {
    setLoading(true);
    try {
      const response = await fetch(
        `http://localhost:5215/api/user?pageIndex=${page}&pageSize=${size}`,
        { headers: { 'Authorization': `Bearer ${adminToken}` } }
      );
      
      const data = await response.json();
      
      if (data.statusCode === 200) {
        setUsers(data.data.items);
        setPagination({
          pageIndex: data.data.pageIndex,
          pageSize: data.data.pageSize,
          totalCount: data.data.totalCount,
          totalPages: data.data.totalPages
        });
      }
    } catch (error) {
      console.error('Error loading users:', error);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadUsers(pagination.pageIndex, pagination.pageSize);
  }, []);

  return (
    <div>
      <h2>Quản lý người dùng ({pagination.totalCount} users)</h2>
      
      {loading ? (
        <div>Loading...</div>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Họ tên</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Hành động</th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.userId}>
                  <td>{user.userId}</td>
                  <td>{user.firstName} {user.lastName}</td>
                  <td>{user.email}</td>
                  <td>{user.role}</td>
                  <td>
                    <span className={`status-${user.status.toLowerCase()}`}>
                      {user.status}
                    </span>
                  </td>
                  <td>
                    <button onClick={() => viewUser(user.userId)}>Xem</button>
                    <button onClick={() => deleteUser(user.userId)}>Xóa</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          <div className="pagination">
            <button
              disabled={pagination.pageIndex === 1}
              onClick={() => loadUsers(pagination.pageIndex - 1, pagination.pageSize)}
            >
              ← Trước
            </button>
            
            <span>
              Trang {pagination.pageIndex} / {pagination.totalPages}
            </span>
            
            <button
              disabled={pagination.pageIndex === pagination.totalPages}
              onClick={() => loadUsers(pagination.pageIndex + 1, pagination.pageSize)}
            >
              Sau →
            </button>
          </div>

          {/* Page size selector */}
          <div>
            <label>Items per page:</label>
            <select
              value={pagination.pageSize}
              onChange={e => loadUsers(1, parseInt(e.target.value))}
            >
              <option value="10">10</option>
              <option value="20">20</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
          </div>
        </>
      )}
    </div>
  );
}
```

---

## 🔄 Mối quan hệ với API khác

### Profile API (`/api/profile`)
User API cung cấp quản lý cơ bản, trong khi Profile API cung cấp:
- Upload ảnh đại diện
- Đổi mật khẩu
- Xem thống kê (vehicles, bookings, payments)
- Validation profile completeness
- Activity summary

**Khi nào dùng User API:**
- Admin quản lý danh sách người dùng
- Cập nhật thông tin cơ bản nhanh

**Khi nào dùng Profile API:**
- User tự quản lý hồ sơ chi tiết
- Upload ảnh, đổi password
- Xem dashboard cá nhân

### Auth API (`/api/auth`)
- Auth API: Đăng ký, đăng nhập, reset password
- User API: Quản lý thông tin sau khi đã đăng nhập

---

## 🔐 Best Practices

### 1. Kiểm tra quyền truy cập

```javascript
function canAccessUser(currentUserId, targetUserId, userRole) {
  // User chỉ có thể xem/sửa thông tin của chính mình
  if (currentUserId === targetUserId) {
    return true;
  }
  
  // Admin/Staff có thể xem/sửa mọi người
  if (userRole === 'Admin' || userRole === 'Staff') {
    return true;
  }
  
  return false;
}

// Sử dụng
const currentUserId = getUserIdFromToken(token);
const currentUserRole = getRoleFromToken(token);

if (!canAccessUser(currentUserId, targetUserId, currentUserRole)) {
  alert('Bạn không có quyền truy cập thông tin này');
  return;
}
```

### 2. Xử lý phân trang hiệu quả

```javascript
// Cache để tránh load lại dữ liệu đã có
class UserCache {
  constructor() {
    this.cache = new Map();
  }

  getKey(pageIndex, pageSize) {
    return `${pageIndex}-${pageSize}`;
  }

  get(pageIndex, pageSize) {
    return this.cache.get(this.getKey(pageIndex, pageSize));
  }

  set(pageIndex, pageSize, data) {
    this.cache.set(this.getKey(pageIndex, pageSize), data);
  }

  clear() {
    this.cache.clear();
  }
}

const userCache = new UserCache();

async function loadUsersWithCache(pageIndex, pageSize, token) {
  // Kiểm tra cache trước
  const cached = userCache.get(pageIndex, pageSize);
  if (cached) {
    console.log('Loaded from cache');
    return cached;
  }

  // Nếu không có trong cache, fetch từ API
  const response = await fetch(
    `http://localhost:5215/api/user?pageIndex=${pageIndex}&pageSize=${pageSize}`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );
  
  const data = await response.json();
  
  if (data.statusCode === 200) {
    userCache.set(pageIndex, pageSize, data.data);
    return data.data;
  }
  
  return null;
}

// Clear cache khi có thay đổi (create, update, delete)
function onUserChanged() {
  userCache.clear();
}
```

### 3. Validation phía frontend

```javascript
function validateUserUpdate(formData) {
  const errors = {};

  // Validate phone number (Vietnam format)
  if (formData.phoneNumber) {
    const phoneRegex = /^(\+84|0)[1-9][0-9]{8}$/;
    if (!phoneRegex.test(formData.phoneNumber)) {
      errors.phoneNumber = 'Số điện thoại không đúng định dạng Việt Nam';
    }
  }

  // Validate date of birth (must be 18+)
  if (formData.dateOfBirth) {
    const birthDate = new Date(formData.dateOfBirth);
    const today = new Date();
    const age = today.getFullYear() - birthDate.getFullYear();
    
    if (age < 18) {
      errors.dateOfBirth = 'Phải đủ 18 tuổi';
    }
    
    if (age > 120) {
      errors.dateOfBirth = 'Ngày sinh không hợp lệ';
    }
  }

  // Validate address length
  if (formData.address && formData.address.length > 200) {
    errors.address = 'Địa chỉ tối đa 200 ký tự';
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors
  };
}

// Sử dụng
const validation = validateUserUpdate(formData);
if (!validation.isValid) {
  console.error('Validation errors:', validation.errors);
  return;
}
```

---

## 📞 Liên hệ và Hỗ trợ

- **API Documentation:** http://localhost:5215/swagger
- **Related APIs:** 
  - Profile API: `/api/profile` (chi tiết hơn)
  - Auth API: `/api/auth` (đăng ký/đăng nhập)
- **Backend Team:** [Your team contact]

---

**Last Updated:** 2025-10-24  
**Version:** 1.0.0  
**Author:** Backend Development Team
