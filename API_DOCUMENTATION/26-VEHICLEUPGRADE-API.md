# VehicleUpgrade API Documentation

## Tổng quan
Module `VehicleUpgrade API` cung cấp hệ thống bỏ phiếu và quản lý nâng cấp xe điện. Cho phép các chủ sở hữu chung đề xuất, bỏ phiếu và thực hiện các nâng cấp như pin, bảo hiểm, công nghệ, nội thất, hiệu suất và an toàn.

### Base URL
```
/api/upgrade-vote
```

### Authentication
Tất cả endpoints yêu cầu Bearer token trong header:
```
Authorization: Bearer {your_jwt_token}
```

---

## Endpoints

### 1. Đề Xuất Nâng Cấp Xe
**[User - Co-Owner]**

- **Mô tả:**
  Tạo đề xuất nâng cấp mới cho xe. Người đề xuất sẽ tự động được tính là đã bỏ phiếu "Đồng ý".

- **Endpoint:**
  ```
  POST /propose
  ```

- **Request Body:**
  ```json
  {
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "upgradeType": 0,
    "title": "Nâng cấp pin lên 100kWh",
    "description": "Nâng cấp pin thế hệ mới để tăng phạm vi hoạt động",
    "estimatedCost": 15000.00,
    "justification": "Pin hiện tại đã giảm 20% dung lượng, ảnh hưởng hoạt động hàng ngày",
    "imageUrl": "https://example.com/battery-specs.jpg",
    "vendorName": "Tesla Battery Solutions",
    "vendorContact": "+1-555-0123",
    "proposedInstallationDate": "2024-12-01T00:00:00Z",
    "estimatedDurationDays": 3
  }
  ```

- **Parameters:**
  | Tên                        | Kiểu     | Bắt buộc | Mô tả                                         |
  |----------------------------|----------|----------|-----------------------------------------------|
  | `vehicleId`                | `Guid`   | Có       | ID của xe cần nâng cấp                        |
  | `upgradeType`              | `int`    | Có       | Loại nâng cấp (xem bảng bên dưới)            |
  | `title`                    | `string` | Có       | Tiêu đề đề xuất                               |
  | `description`              | `string` | Có       | Mô tả chi tiết nâng cấp                       |
  | `estimatedCost`            | `decimal`| Có       | Chi phí ước tính (USD)                        |
  | `justification`            | `string` | Có       | Lý do cần nâng cấp                            |
  | `imageUrl`                 | `string` | Không    | URL hình ảnh minh họa                         |
  | `vendorName`               | `string` | Không    | Tên nhà cung cấp                              |
  | `vendorContact`            | `string` | Không    | Thông tin liên hệ nhà cung cấp                |
  | `proposedInstallationDate` | `DateTime` | Không  | Ngày dự kiến lắp đặt                          |
  | `estimatedDurationDays`    | `int`    | Không    | Số ngày ước tính để hoàn thành                |

- **Loại nâng cấp (upgradeType):**
  | Mã | Tên                    | Mô tả                                         |
  |----|------------------------|-----------------------------------------------|
  | 0  | BatteryUpgrade         | Nâng cấp pin (dung lượng, công nghệ)         |
  | 1  | InsurancePackage       | Gói bảo hiểm mới/nâng cấp                     |
  | 2  | TechnologyUpgrade      | Nâng cấp công nghệ (phần mềm, hardware)      |
  | 3  | InteriorUpgrade        | Nâng cấp nội thất                             |
  | 4  | PerformanceUpgrade     | Nâng cấp hiệu suất                            |
  | 5  | SafetyUpgrade          | Nâng cấp an toàn                              |
  | 6  | Other                  | Nâng cấp khác                                 |

- **Sample Response (201 Created):**
  ```json
  {
    "statusCode": 201,
    "message": "Đề xuất nâng cấp được tạo thành công",
    "data": {
      "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "proposerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "proposerName": "Nguyễn Văn A",
      "title": "Nâng cấp pin lên 100kWh",
      "upgradeType": "BatteryUpgrade",
      "estimatedCost": 15000.00,
      "status": "Pending",
      "votingProgress": {
        "totalCoOwners": 3,
        "votedCount": 1,
        "approvedCount": 1,
        "rejectedCount": 0,
        "requiredForApproval": 2,
        "autoApprovedBy": "Nguyễn Văn A (Người đề xuất)"
      },
      "createdAt": "2025-10-24T10:30:00Z",
      "votingDeadline": "2025-11-07T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 201    | Đề xuất được tạo thành công                   |
  | 400    | Dữ liệu không hợp lệ (VD: chi phí âm)         |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 2. Bỏ Phiếu Cho Đề Xuất Nâng Cấp
**[User - Co-Owner]**

- **Mô tả:**
  Bỏ phiếu đồng ý hoặc từ chối một đề xuất nâng cấp. Mỗi chủ sở hữu chung chỉ được bỏ phiếu một lần.

- **Endpoint:**
  ```
  POST /{proposalId}/vote
  ```

- **Request Body:**
  ```json
  {
    "isApprove": true,
    "comments": "Ý tưởng hay! Điều này sẽ cải thiện đáng kể hiệu suất xe của chúng ta"
  }
  ```

- **Parameters:**
  | Tên          | Kiểu     | Bắt buộc | Mô tả                                         |
  |--------------|----------|----------|-----------------------------------------------|
  | `proposalId` | `Guid`   | Có       | ID của đề xuất (trong URL)                   |
  | `isApprove`  | `bool`   | Có       | true = Đồng ý, false = Từ chối               |
  | `comments`   | `string` | Không    | Nhận xét về đề xuất                           |

- **Quy tắc bỏ phiếu:**
  - Mỗi chủ sở hữu chung chỉ được bỏ phiếu một lần
  - Nếu BẤT KỲ chủ sở hữu nào từ chối → Đề xuất bị từ chối ngay
  - Nếu > 50% chủ sở hữu đồng ý → Đề xuất được phê duyệt
  - Người đề xuất tự động được tính là đồng ý

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Phiếu bầu được ghi nhận thành công",
    "data": {
      "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "voterName": "Trần Thị B",
      "vote": "Approved",
      "comments": "Ý tưởng hay! Điều này sẽ cải thiện đáng kể hiệu suất xe của chúng ta",
      "votedAt": "2025-10-24T11:00:00Z",
      "proposalStatus": "Approved",
      "votingResult": {
        "totalCoOwners": 3,
        "votedCount": 3,
        "approvedCount": 3,
        "rejectedCount": 0,
        "finalStatus": "Approved",
        "approvedAt": "2025-10-24T11:00:00Z",
        "canExecute": true
      }
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Phiếu bầu được ghi nhận thành công            |
  | 400    | Đã bỏ phiếu hoặc đề xuất không thể bỏ phiếu   |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy đề xuất                        |
  | 500    | Lỗi server nội bộ                            |

---

### 3. Xem Chi Tiết Đề Xuất
**[User - Co-Owner]**

- **Mô tả:**
  Lấy thông tin chi tiết về một đề xuất nâng cấp, bao gồm thông tin bỏ phiếu và trạng thái thực hiện.

- **Endpoint:**
  ```
  GET /{proposalId}
  ```

- **Sample Request:**
  ```
  GET /api/upgrade-vote/3fa85f64-5717-4562-b3fc-2c963f66afa6
  Authorization: Bearer {token}
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Chi tiết đề xuất được lấy thành công",
    "data": {
      "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleName": "Tesla Model 3",
      "licensePlate": "30A-12345",
      "proposalInfo": {
        "title": "Nâng cấp pin lên 100kWh",
        "description": "Nâng cấp pin thế hệ mới để tăng phạm vi hoạt động",
        "upgradeType": "BatteryUpgrade",
        "upgradeTypeName": "Nâng cấp pin",
        "estimatedCost": 15000.00,
        "justification": "Pin hiện tại đã giảm 20% dung lượng",
        "imageUrl": "https://example.com/battery-specs.jpg",
        "vendorName": "Tesla Battery Solutions",
        "vendorContact": "+1-555-0123",
        "proposedInstallationDate": "2024-12-01T00:00:00Z",
        "estimatedDurationDays": 3
      },
      "proposerInfo": {
        "proposerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "proposerName": "Nguyễn Văn A",
        "proposerEmail": "nguyenvana@example.com",
        "proposedAt": "2025-10-24T10:30:00Z"
      },
      "votingInfo": {
        "status": "Approved",
        "totalCoOwners": 3,
        "votedCount": 3,
        "approvedCount": 3,
        "rejectedCount": 0,
        "votingDeadline": "2025-11-07T10:30:00Z",
        "approvedAt": "2025-10-24T11:00:00Z",
        "rejectedAt": null,
        "canVote": false,
        "userHasVoted": true,
        "userVote": "Approved"
      },
      "votes": [
        {
          "voterId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "voterName": "Nguyễn Văn A",
          "vote": "Approved",
          "comments": "Tự động phê duyệt (Người đề xuất)",
          "votedAt": "2025-10-24T10:30:00Z",
          "isProposer": true
        },
        {
          "voterId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "voterName": "Trần Thị B",
          "vote": "Approved",
          "comments": "Ý tưởng hay! Điều này sẽ cải thiện đáng kể hiệu suất",
          "votedAt": "2025-10-24T11:00:00Z",
          "isProposer": false
        },
        {
          "voterId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "voterName": "Lê Văn C",
          "vote": "Approved",
          "comments": "Đồng ý, cần thiết cho hiệu quả dài hạn",
          "votedAt": "2025-10-24T11:15:00Z",
          "isProposer": false
        }
      ],
      "executionInfo": {
        "isExecuted": false,
        "canExecute": true,
        "actualCost": null,
        "executedAt": null,
        "executedBy": null,
        "executionNotes": null,
        "invoiceImageUrl": null,
        "fundImpact": {
          "currentFundBalance": 25000.00,
          "estimatedCostImpact": 15000.00,
          "remainingBalanceAfter": 10000.00,
          "sufficientFunds": true
        }
      }
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Chi tiết đề xuất được lấy thành công          |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy đề xuất                        |
  | 500    | Lỗi server nội bộ                            |

---

### 4. Lấy Danh Sách Đề Xuất Đang Chờ
**[User - Co-Owner]**

- **Mô tả:**
  Lấy tất cả đề xuất nâng cấp đang chờ bỏ phiếu cho một xe cụ thể.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/pending
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Danh sách đề xuất chờ được lấy thành công",
    "data": {
      "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleName": "Tesla Model 3",
      "summary": {
        "totalPendingProposals": 2,
        "totalEstimatedCost": 28000.00,
        "averageCostPerProposal": 14000.00,
        "requiresUserVote": 1,
        "userCanVoteCount": 1
      },
      "pendingProposals": [
        {
          "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "title": "Nâng cấp hệ thống âm thanh",
          "upgradeType": "InteriorUpgrade",
          "upgradeTypeName": "Nâng cấp nội thất",
          "estimatedCost": 8000.00,
          "proposerName": "Trần Thị B",
          "proposedAt": "2025-10-20T00:00:00Z",
          "votingProgress": {
            "totalCoOwners": 3,
            "votedCount": 2,
            "approvedCount": 2,
            "rejectedCount": 0,
            "requiredForApproval": 2,
            "userHasVoted": false,
            "userCanVote": true
          },
          "votingDeadline": "2025-11-03T00:00:00Z",
          "daysRemaining": 10
        },
        {
          "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "title": "Nâng cấp camera 360",
          "upgradeType": "SafetyUpgrade", 
          "upgradeTypeName": "Nâng cấp an toàn",
          "estimatedCost": 20000.00,
          "proposerName": "Lê Văn C",
          "proposedAt": "2025-10-22T00:00:00Z",
          "votingProgress": {
            "totalCoOwners": 3,
            "votedCount": 1,
            "approvedCount": 1,
            "rejectedCount": 0,
            "requiredForApproval": 2,
            "userHasVoted": true,
            "userCanVote": false
          },
          "votingDeadline": "2025-11-05T00:00:00Z",
          "daysRemaining": 12
        }
      ]
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Danh sách đề xuất chờ được lấy thành công     |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

### 5. Đánh Dấu Đề Xuất Đã Thực Hiện
**[Admin hoặc Người đề xuất]**

- **Mô tả:**
  Đánh dấu một đề xuất đã được phê duyệt là đã thực hiện, trừ tiền từ quỹ và cập nhật thông tin thực tế.

- **Endpoint:**
  ```
  POST /{proposalId}/execute
  ```

- **Request Body:**
  ```json
  {
    "actualCost": 14500.00,
    "executionNotes": "Lắp đặt hoàn thành thành công. Pin hoạt động tốt hơn mong đợi.",
    "invoiceImageUrl": "https://example.com/invoice-12345.pdf"
  }
  ```

- **Parameters:**
  | Tên               | Kiểu     | Bắt buộc | Mô tả                                         |
  |-------------------|----------|----------|-----------------------------------------------|
  | `proposalId`      | `Guid`   | Có       | ID của đề xuất (trong URL)                   |
  | `actualCost`      | `decimal`| Có       | Chi phí thực tế                               |
  | `executionNotes`  | `string` | Không    | Ghi chú về quá trình thực hiện                |
  | `invoiceImageUrl` | `string` | Không    | URL hình ảnh hóa đơn                          |

- **Quy trình thực hiện:**
  1. Kiểm tra đề xuất đã được phê duyệt (không phải pending/rejected/cancelled)
  2. Kiểm tra quỹ có đủ số dư
  3. Tạo bản ghi sử dụng quỹ
  4. Trừ chi phí thực tế từ quỹ xe
  5. Đánh dấu đề xuất là đã thực hiện với timestamp

- **Quyền hạn:** Chỉ admin hoặc người đề xuất ban đầu mới có thể thực hiện

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Nâng cấp được đánh dấu đã thực hiện, quỹ đã được trừ",
    "data": {
      "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Nâng cấp pin lên 100kWh",
      "estimatedCost": 15000.00,
      "actualCost": 14500.00,
      "costSaving": 500.00,
      "executedAt": "2025-10-24T15:00:00Z",
      "executedBy": "Nguyễn Văn A",
      "executionNotes": "Lắp đặt hoàn thành thành công. Pin hoạt động tốt hơn mong đợi.",
      "fundImpact": {
        "previousBalance": 25000.00,
        "amountDeducted": 14500.00,
        "newBalance": 10500.00,
        "fundUsageId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      },
      "invoiceImageUrl": "https://example.com/invoice-12345.pdf"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Nâng cấp được đánh dấu đã thực hiện           |
  | 400    | Đề xuất chưa phê duyệt/đã thực hiện/quỹ không đủ |
  | 403    | Người dùng không phải admin hoặc người đề xuất |
  | 404    | Không tìm thấy đề xuất hoặc quỹ               |
  | 500    | Lỗi server nội bộ                            |

---

### 6. Hủy Đề Xuất
**[Admin hoặc Người đề xuất]**

- **Mô tả:**
  Hủy bỏ một đề xuất nâng cấp. Chỉ có thể hủy các đề xuất đang chờ hoặc đã phê duyệt chưa thực hiện.

- **Endpoint:**
  ```
  DELETE /{proposalId}/cancel
  ```

- **Quy tắc hủy:**
  - Chỉ có thể hủy đề xuất Pending hoặc Approved
  - Không thể hủy đề xuất đã thực hiện
  - Không hoàn lại tiền (tiền chỉ bị trừ khi thực hiện)

- **Quyền hạn:** Chỉ admin hoặc người đề xuất ban đầu

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Đề xuất được hủy thành công",
    "data": {
      "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Nâng cấp pin lên 100kWh",
      "previousStatus": "Approved",
      "newStatus": "Cancelled",
      "cancelledAt": "2025-10-24T16:00:00Z",
      "cancelledBy": "Nguyễn Văn A",
      "reason": "Hủy bởi người đề xuất"
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Đề xuất được hủy thành công                   |
  | 400    | Đề xuất đã thực hiện hoặc không thể hủy       |
  | 403    | Người dùng không phải admin hoặc người đề xuất |
  | 404    | Không tìm thấy đề xuất                        |
  | 500    | Lỗi server nội bộ                            |

---

### 7. Lịch Sử Bỏ Phiếu Cá Nhân
**[User]**

- **Mô tả:**
  Lấy lịch sử bỏ phiếu của người dùng hiện tại cho tất cả đề xuất nâng cấp.

- **Endpoint:**
  ```
  GET /my-history
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Lịch sử bỏ phiếu được lấy thành công",
    "data": {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "Nguyễn Văn A",
      "email": "nguyenvana@example.com",
      "summary": {
        "totalVotes": 15,
        "approvedVotes": 12,
        "rejectedVotes": 3,
        "proposalsCreated": 5,
        "executedProposals": 3,
        "totalEstimatedValue": 85000.00,
        "totalExecutedValue": 42000.00
      },
      "votingHistory": [
        {
          "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "vehicleName": "Tesla Model 3",
          "proposalTitle": "Nâng cấp pin lên 100kWh",
          "upgradeType": "BatteryUpgrade",
          "upgradeTypeName": "Nâng cấp pin",
          "estimatedCost": 15000.00,
          "actualCost": 14500.00,
          "proposerName": "Trần Thị B",
          "userVote": "Approved",
          "userComments": "Ý tưởng hay! Cần thiết cho hiệu quả dài hạn",
          "votedAt": "2025-10-20T10:00:00Z",
          "proposalStatus": "Executed",
          "executedAt": "2025-10-22T00:00:00Z",
          "isUserProposer": false
        },
        {
          "proposalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "vehicleName": "BMW i4",
          "proposalTitle": "Nâng cấp hệ thống âm thanh premium",
          "upgradeType": "InteriorUpgrade",
          "upgradeTypeName": "Nâng cấp nội thất",
          "estimatedCost": 8000.00,
          "actualCost": null,
          "proposerName": "Nguyễn Văn A",
          "userVote": "Approved",
          "userComments": "Tự động phê duyệt (Người đề xuất)",
          "votedAt": "2025-10-18T00:00:00Z",
          "proposalStatus": "Pending",
          "executedAt": null,
          "isUserProposer": true
        }
      ]
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Lịch sử bỏ phiếu được lấy thành công          |
  | 404    | Không tìm thấy người dùng                     |
  | 500    | Lỗi server nội bộ                            |

---

### 8. Thống Kê Nâng Cấp Xe
**[User - Co-Owner]**

- **Mô tả:**
  Lấy thống kê tổng hợp về các đề xuất nâng cấp cho một xe cụ thể.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/statistics
  ```

- **Sample Response (200 OK):**
  ```json
  {
    "statusCode": 200,
    "message": "Thống kê được lấy thành công",
    "data": {
      "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "vehicleName": "Tesla Model 3",
      "licensePlate": "30A-12345",
      "overallStatistics": {
        "totalProposals": 18,
        "pendingProposals": 3,
        "approvedProposals": 12,
        "rejectedProposals": 2,
        "cancelledProposals": 1,
        "executedProposals": 10,
        "executionRate": 55.6,
        "approvalRate": 66.7
      },
      "costStatistics": {
        "totalEstimatedCost": 125000.00,
        "totalExecutedCost": 89500.00,
        "averageEstimatedCost": 6944.44,
        "averageExecutedCost": 8950.00,
        "costSavings": 15500.00,
        "costSavingsPercentage": 14.8,
        "largestProposal": {
          "title": "Nâng cấp pin lên 120kWh",
          "cost": 25000.00
        },
        "smallestProposal": {
          "title": "Nâng cấp đèn LED",
          "cost": 500.00
        }
      },
      "upgradeTypeBreakdown": [
        {
          "upgradeType": "BatteryUpgrade",
          "upgradeTypeName": "Nâng cấp pin",
          "count": 5,
          "percentage": 27.8,
          "totalCost": 75000.00,
          "executedCount": 3,
          "executedCost": 52000.00
        },
        {
          "upgradeType": "TechnologyUpgrade", 
          "upgradeTypeName": "Nâng cấp công nghệ",
          "count": 4,
          "percentage": 22.2,
          "totalCost": 28000.00,
          "executedCount": 3,
          "executedCost": 21500.00
        },
        {
          "upgradeType": "SafetyUpgrade",
          "upgradeTypeName": "Nâng cấp an toàn",
          "count": 3,
          "percentage": 16.7,
          "totalCost": 15000.00,
          "executedCount": 2,
          "executedCost": 9500.00
        },
        {
          "upgradeType": "InteriorUpgrade",
          "upgradeTypeName": "Nâng cấp nội thất",
          "count": 3,
          "percentage": 16.7,
          "totalCost": 4500.00,
          "executedCount": 2,
          "executedCost": 3000.00
        },
        {
          "upgradeType": "PerformanceUpgrade",
          "upgradeTypeName": "Nâng cấp hiệu suất",
          "count": 2,
          "percentage": 11.1,
          "totalCost": 2000.00,
          "executedCount": 0,
          "executedCost": 0.00
        },
        {
          "upgradeType": "InsurancePackage",
          "upgradeTypeName": "Gói bảo hiểm",
          "count": 1,
          "percentage": 5.6,
          "totalCost": 500.00,
          "executedCount": 0,
          "executedCost": 0.00
        }
      ],
      "timeStatistics": {
        "firstProposal": "2024-03-15T00:00:00Z",
        "lastProposal": "2025-10-24T00:00:00Z",
        "averageVotingDuration": 8.5,
        "fastestApproval": 2.0,
        "slowestApproval": 14.0,
        "averageExecutionDelay": 15.5
      },
      "proposerStatistics": [
        {
          "proposerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "proposerName": "Nguyễn Văn A",
          "proposalCount": 8,
          "approvedCount": 6,
          "executedCount": 5,
          "totalValue": 65000.00,
          "successRate": 75.0
        },
        {
          "proposerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "proposerName": "Trần Thị B",
          "proposalCount": 6,
          "approvedCount": 4,
          "executedCount": 3,
          "totalValue": 35000.00,
          "successRate": 66.7
        },
        {
          "proposerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "proposerName": "Lê Văn C",
          "proposalCount": 4,
          "approvedCount": 2,
          "executedCount": 2,
          "totalValue": 25000.00,
          "successRate": 50.0
        }
      ]
    }
  }
  ```

- **Responses:**
  | Mã lỗi | Thông báo                                      |
  |--------|------------------------------------------------|
  | 200    | Thống kê được lấy thành công                  |
  | 403    | Người dùng không phải chủ sở hữu chung        |
  | 404    | Không tìm thấy xe                             |
  | 500    | Lỗi server nội bộ                            |

---

## Best Practices

### 1. **Xử lý bỏ phiếu**
```javascript
// Kiểm tra trước khi bỏ phiếu
const proposal = await getProposalDetails(proposalId);
if (!proposal.votingInfo.canVote) {
  alert('Bạn không thể bỏ phiếu cho đề xuất này');
  return;
}

// Bỏ phiếu với xác nhận
const confirmed = confirm(`Bạn có chắc chắn ${isApprove ? 'đồng ý' : 'từ chối'} đề xuất này?`);
if (confirmed) {
  await voteOnProposal(proposalId, { isApprove, comments });
}
```

### 2. **Kiểm tra quỹ trước khi đề xuất**
```javascript
// Lấy thông tin quỹ hiện tại
const fundInfo = await getFundBalance(vehicleId);
if (estimatedCost > fundInfo.availableBalance) {
  alert('Quỹ không đủ để thực hiện nâng cấp này');
  return;
}
```

### 3. **UI/UX cho voting**
- Hiển thị progress bar cho tiến độ bỏ phiếu
- Highlight các đề xuất cần user bỏ phiếu
- Countdown timer cho voting deadline
- Notification khi có đề xuất mới

### 4. **Quản lý trạng thái**
```javascript
const proposalStatuses = {
  'Pending': { color: 'orange', text: 'Đang chờ bỏ phiếu' },
  'Approved': { color: 'green', text: 'Đã phê duyệt' },
  'Rejected': { color: 'red', text: 'Bị từ chối' },
  'Executed': { color: 'blue', text: 'Đã thực hiện' },
  'Cancelled': { color: 'gray', text: 'Đã hủy' }
};
```

### 5. **Validation cho đề xuất**
- Chi phí > 0
- Ngày lắp đặt trong tương lai
- Thời gian ước tính hợp lý
- Upload hình ảnh minh họa
- Validate vendor contact format

---

## Error Codes

| Mã lỗi | Mô tả                                    |
|--------|------------------------------------------|
| 200    | Thành công                               |
| 201    | Tạo mới thành công                       |
| 400    | Dữ liệu đầu vào không hợp lệ            |
| 401    | Token không hợp lệ                       |
| 403    | Không có quyền truy cập                  |
| 404    | Không tìm thấy tài nguyên               |
| 500    | Lỗi server nội bộ                       |

---

## Data Models

### ProposeVehicleUpgradeRequest
```typescript
interface ProposeVehicleUpgradeRequest {
  vehicleId: string;
  upgradeType: number; // 0-6
  title: string;
  description: string;
  estimatedCost: number;
  justification: string;
  imageUrl?: string;
  vendorName?: string;
  vendorContact?: string;
  proposedInstallationDate?: string;
  estimatedDurationDays?: number;
}
```

### VoteVehicleUpgradeRequest
```typescript
interface VoteVehicleUpgradeRequest {
  isApprove: boolean;
  comments?: string;
}
```

### MarkUpgradeExecutedRequest
```typescript
interface MarkUpgradeExecutedRequest {
  actualCost: number;
  executionNotes?: string;
  invoiceImageUrl?: string;
}
```

---

## Workflow

### Quy trình nâng cấp hoàn chỉnh:
1. **Đề xuất** → Tạo proposal với chi tiết
2. **Bỏ phiếu** → Các co-owner vote approve/reject
3. **Phê duyệt** → Tự động khi đủ vote hoặc bị reject
4. **Thực hiện** → Admin/proposer mark as executed
5. **Hoàn thành** → Proposal archived với actual cost

### Voting Rules:
- **Immediate Rejection**: Bất kỳ vote "reject" nào → proposal rejected
- **Approval Threshold**: >50% co-owners approve → proposal approved
- **Auto-approval**: Proposer tự động được tính approve
- **Timeout**: Sau voting deadline → proposal expired (optional)