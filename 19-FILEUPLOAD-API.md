# File Upload API Documentation

## Tổng quan
API File Upload cung cấp các chức năng quản lý tệp, bao gồm tải lên, tải xuống, lấy thông tin metadata và xóa tệp. 

**Yêu cầu vai trò**: User, Admin (tùy endpoint)

---

## 1. Tải lên tệp

### Endpoint
```
POST /api/fileupload/upload
```

### Mô tả
Tải lên một tệp mới vào hệ thống.

### Tham số
| Tên   | Loại       | Bắt buộc | Mô tả                                                                 |
|-------|------------|----------|----------------------------------------------------------------------|
| `file`| IFormFile  | Có       | Tệp cần tải lên. Kích thước tối đa: 10MB.                           |

### Loại tệp hỗ trợ
- **Hình ảnh**: JPEG, JPG, PNG, GIF, WEBP
- **Tài liệu**: PDF, DOC, DOCX, XLS, XLSX, TXT

### Phản hồi mẫu
```json
{
  "statusCode": 201,
  "message": "FILE_UPLOAD_SUCCESS",
  "data": {
    "fileId": 123,
    "fileName": "example.pdf",
    "fileSize": 1048576,
    "uploadDate": "2025-10-24T12:00:00Z"
  }
}
```

### Mã lỗi
- `400 FILE_REQUIRED`: Không có tệp nào được tải lên.
- `400 INVALID_FILE_TYPE`: Loại tệp không hợp lệ.
- `400 FILE_SIZE_EXCEEDS_LIMIT`: Kích thước tệp vượt quá giới hạn.
- `500 FILE_UPLOAD_FAILED`: Lỗi máy chủ khi tải lên tệp.

---

## 2. Tải xuống tệp

### Endpoint
```
GET /api/fileupload/{id:int}/download
```

### Mô tả
Tải xuống tệp dựa trên ID của tệp.

### Tham số
| Tên   | Loại       | Bắt buộc | Mô tả                                                                 |
|-------|------------|----------|----------------------------------------------------------------------|
| `id`  | int        | Có       | ID của tệp cần tải xuống.                                           |

### Phản hồi mẫu
- **Thành công**: Trả về nội dung tệp với header phù hợp.
- **Lỗi**:
  - `404 FILE_NOT_FOUND`: Không tìm thấy tệp.
  - `500 FILE_RETRIEVAL_FAILED`: Lỗi máy chủ khi lấy tệp.

---

## 3. Lấy thông tin tệp

### Endpoint
```
GET /api/fileupload/{id:int}/info
```

### Mô tả
Lấy thông tin metadata của tệp mà không cần tải xuống nội dung.

### Tham số
| Tên   | Loại       | Bắt buộc | Mô tả                                                                 |
|-------|------------|----------|----------------------------------------------------------------------|
| `id`  | int        | Có       | ID của tệp cần lấy thông tin.                                       |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "FILE_INFO_RETRIEVED_SUCCESS",
  "data": {
    "fileId": 123,
    "fileName": "example.pdf",
    "fileSize": 1048576,
    "uploadDate": "2025-10-24T12:00:00Z",
    "fileType": "application/pdf",
    "downloadUrl": "/api/fileupload/123/download"
  }
}
```

### Mã lỗi
- `404 FILE_NOT_FOUND`: Không tìm thấy tệp.
- `500 FILE_INFO_RETRIEVAL_FAILED`: Lỗi máy chủ khi lấy thông tin tệp.

---

## 4. Xóa tệp

### Endpoint
```
DELETE /api/fileupload/{id:int}
```

### Mô tả
Xóa một tệp khỏi hệ thống. Hành động này không thể hoàn tác.

### Tham số
| Tên   | Loại       | Bắt buộc | Mô tả                                                                 |
|-------|------------|----------|----------------------------------------------------------------------|
| `id`  | int        | Có       | ID của tệp cần xóa.                                                 |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "FILE_DELETE_SUCCESS"
}
```

### Mã lỗi
- `404 FILE_NOT_FOUND`: Không tìm thấy tệp.
- `500 FILE_DELETE_FAILED`: Lỗi máy chủ khi xóa tệp.

---