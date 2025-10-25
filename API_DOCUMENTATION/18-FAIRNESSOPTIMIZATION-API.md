# Fairness Optimization API Documentation

## Tổng quan
API Fairness Optimization cung cấp các tính năng phân tích công bằng và tối ưu hóa chi phí dựa trên AI. Các tính năng bao gồm:
- Phân tích công bằng sử dụng (so sánh giữa tỷ lệ sử dụng thực tế và tỷ lệ sở hữu).
- Gợi ý lịch đặt xe công bằng.
- Đề xuất bảo trì dự đoán.
- Cơ hội tiết kiệm chi phí.

**Yêu cầu vai trò**: CoOwner

---

## 1. Tạo báo cáo công bằng

### Endpoint
```
GET /api/fairnessoptimization/vehicle/{vehicleId:int}/fairness-report
```

### Mô tả
Tạo báo cáo công bằng toàn diện cho một phương tiện, phân tích các mẫu sử dụng và cung cấp các khuyến nghị hành động.

### Tham số
| Tên                | Loại       | Bắt buộc | Mô tả                                                                 |
|--------------------|------------|----------|----------------------------------------------------------------------|
| `vehicleId`        | int        | Có       | ID của phương tiện cần phân tích.                                    |
| `startDate`        | DateTime   | Không    | Ngày bắt đầu phân tích (mặc định: ngày tạo phương tiện).             |
| `endDate`          | DateTime   | Không    | Ngày kết thúc phân tích (mặc định: ngày hiện tại).                   |
| `includeRecommendations` | bool | Không    | Bao gồm các khuyến nghị hành động (mặc định: true).                  |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "FAIRNESS_REPORT_GENERATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "overview": {
      "overallFairnessStatus": "Good",
      "fairnessScore": 78.5,
      "balancedCoOwnersCount": 1,
      "overutilizedCoOwnersCount": 1,
      "underutilizedCoOwnersCount": 1
    },
    "coOwnersDetails": [
      {
        "coOwnerId": 1,
        "coOwnerName": "John Doe",
        "ownershipPercentage": 40.00,
        "averageUsagePercentage": 55.50,
        "usageVsOwnershipDelta": 15.50,
        "usagePattern": "Overutilized",
        "fairnessScore": 70,
        "expectedCostShare": 4000000,
        "actualCostShare": 5550000,
        "costAdjustmentNeeded": 1550000,
        "recommendations": [
          "Consider reducing usage by 15.5% to match ownership share",
          "Additional cost contribution of 1,550,000 VND may be fair"
        ]
      }
    ],
    "recommendations": [
      {
        "type": "Usage",
        "priority": "High",
        "title": "Rebalance Usage Distribution",
        "description": "1 co-owner(s) are overutilizing while 1 are underutilizing",
        "actionItems": [
          "Schedule group meeting to discuss fair usage",
          "Implement booking rotation system"
        ],
        "affectedCoOwnerIds": [1, 2]
      }
    ]
  }
}
```

---

## 2. Gợi ý lịch đặt xe công bằng

### Endpoint
```
GET /api/fairnessoptimization/vehicle/{vehicleId:int}/schedule-suggestions
```

### Mô tả
Đề xuất lịch đặt xe tối ưu để phân phối sử dụng công bằng giữa các đồng sở hữu.

### Tham số
| Tên                   | Loại       | Bắt buộc | Mô tả                                                                 |
|-----------------------|------------|----------|----------------------------------------------------------------------|
| `vehicleId`           | int        | Có       | ID của phương tiện.                                                  |
| `startDate`           | DateTime   | Có       | Ngày bắt đầu giai đoạn lịch.                                         |
| `endDate`             | DateTime   | Có       | Ngày kết thúc giai đoạn lịch.                                        |
| `preferredDurationHours` | int    | Không    | Thời lượng đặt xe ưu tiên (giờ, mặc định: 4).                        |
| `usageType`           | EUsageType | Không    | Loại sử dụng (Bảo trì, Bảo hiểm, Nhiên liệu, Đỗ xe, Khác).           |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "SCHEDULE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "coOwnerSuggestions": [
      {
        "coOwnerId": 1,
        "coOwnerName": "John Doe",
        "ownershipPercentage": 40.00,
        "currentUsagePercentage": 25.00,
        "recommendedUsagePercentage": 40.00,
        "suggestedBookingsCount": 5,
        "suggestedTotalHours": 20.0,
        "suggestedSlots": [
          {
            "startTime": "2024-11-05T08:00:00Z",
            "endTime": "2024-11-05T12:00:00Z",
            "durationHours": 4.0,
            "reason": "Optimal morning slot on Tuesday for maintenance",
            "conflictProbability": 0.15,
            "benefits": ["Low conflict risk", "Balanced usage distribution"]
          }
        ],
        "rationale": "You're currently underutilizing by 15.0%. Suggested bookings will help you use your fair share."
      }
    ],
    "optimalTimeSlots": [
      {
        "dayOfWeek": "Tuesday",
        "startTime": "06:00:00",
        "endTime": "12:00:00",
        "utilizationRate": 20.5,
        "peakType": "Low",
        "recommendedForCoOwnerIds": [1, 2, 3]
      }
    ],
    "insights": {
      "currentUtilizationRate": 35.2,
      "optimalUtilizationRate": 40.0,
      "conflictingBookingsCount": 3,
      "peakUsagePeriods": ["Weekends", "Weekday evenings"],
      "underutilizedPeriods": ["Weekday mornings", "Tuesday-Thursday afternoons"],
      "potentialEfficiencyGain": 4.8
    }
  }
}
```

---

## 3. Đề xuất bảo trì dự đoán

### Endpoint
```
GET /api/fairnessoptimization/vehicle/{vehicleId:int}/maintenance-suggestions
```

### Mô tả
Cung cấp các đề xuất bảo trì dự đoán dựa trên tình trạng và mẫu sử dụng của phương tiện.

### Tham số
| Tên                | Loại       | Bắt buộc | Mô tả                                                                 |
|--------------------|------------|----------|----------------------------------------------------------------------|
| `vehicleId`        | int        | Có       | ID của phương tiện.                                                  |
| `includePredictive`| bool       | Không    | Bao gồm phân tích dự đoán AI (mặc định: true).                       |
| `lookaheadDays`    | int        | Không    | Số ngày dự báo (mặc định: 30, tối đa: 365).                          |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "MAINTENANCE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "healthStatus": {
      "currentOdometer": 45000,
      "averageDailyDistance": 65.5,
      "daysSinceLastMaintenance": 125,
      "distanceSinceLastMaintenance": 8200,
      "overallHealth": "Good",
      "healthScore": 82,
      "healthIssues": []
    },
    "suggestions": [
      {
        "maintenanceType": "Routine",
        "title": "Tire Rotation and Inspection",
        "description": "Tire rotation recommended every 8,000-10,000 km",
        "urgency": "Medium",
        "reason": "8200 km since last service",
        "recommendedOdometerReading": 47000,
        "recommendedDate": "2024-11-06T00:00:00Z",
        "daysUntilRecommended": 14,
        "estimatedCost": 800000,
        "costSavingIfDoneNow": 300000,
        "consequences": [
          "Uneven tire wear",
          "Reduced safety and handling",
          "Need for premature tire replacement"
        ],
        "benefits": [
          "Extended tire life",
          "Improved safety",
          "Better fuel efficiency"
        ]
      }
    ],
    "costForecast": {
      "forecastPeriodDays": 60,
      "estimatedTotalCost": 3300000,
      "averageMonthlyCost": 1650000,
      "costPerCoOwnerAverage": 1100000,
      "monthlyForecasts": [
        {
          "month": "Nov 2024",
          "estimatedCost": 1300000,
          "expectedMaintenanceTypes": ["Routine", "Routine"]
        }
      ]
    }
  }
}
```

---

## 4. Đề xuất tiết kiệm chi phí

### Endpoint
```
GET /api/fairnessoptimization/vehicle/{vehicleId:int}/cost-saving-recommendations
```

### Mô tả
Phân tích chi phí và cung cấp các khuyến nghị hành động để tiết kiệm chi phí.

### Tham số
| Tên                        | Loại       | Bắt buộc | Mô tả                                                                 |
|----------------------------|------------|----------|----------------------------------------------------------------------|
| `vehicleId`                | int        | Có       | ID của phương tiện.                                                  |
| `analysisPeriodDays`       | int        | Không    | Số ngày phân tích (mặc định: 90, tối thiểu: 7, tối đa: 365).         |
| `includeFundOptimization`  | bool       | Không    | Bao gồm tối ưu hóa quỹ (mặc định: true).                             |
| `includeMaintenanceOptimization` | bool | Không    | Bao gồm tối ưu hóa chi phí bảo trì (mặc định: true).                 |

### Phản hồi mẫu
```json
{
  "statusCode": 200,
  "message": "COST_SAVING_RECOMMENDATIONS_GENERATED_SUCCESSFULLY",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Tesla Model 3",
    "summary": {
      "analysisPeriodDays": 90,
      "totalCostsIncurred": 15000000,
      "averageMonthlyCost": 5000000,
      "costPerKm": 125,
      "costPerBooking": 500000,
      "potentialSavings": 4500000,
      "savingsPercentage": 30.0,
      "costBreakdowns": [
        {"category": "Routine", "amount": 10000000, "percentage": 66.7, "trend": "Stable"},
        {"category": "Repair", "amount": 5000000, "percentage": 33.3, "trend": "Increasing"}
      ]
    },
    "recommendations": [
      {
        "category": "Maintenance",
        "priority": "High",
        "title": "Switch to Preventive Maintenance Schedule",
        "description": "Regular preventive maintenance costs less than reactive repairs",
        "potentialSavingsAmount": 3000000,
        "potentialSavingsPercentage": 30.0,
        "timeframeForSavings": "6-12 months",
        "actionSteps": [
          "Create maintenance schedule based on odometer readings",
          "Book services during off-peak times for discounts",
          "Keep detailed maintenance records"
        ],
        "difficulty": "Easy",
        "implementationCost": 0,
        "roi": 300.0
      },
      {
        "category": "General",
        "priority": "Medium",
        "title": "Optimize Charging Costs",
        "description": "Charge during off-peak hours to reduce electricity costs",
        "potentialSavingsAmount": 750000,
        "potentialSavingsPercentage": 15.0,
        "timeframeForSavings": "Ongoing",
        "actionSteps": [
          "Use time-of-use electricity rates",
          "Schedule charging for overnight (off-peak)",
          "Track charging costs per co-owner"
        ],
        "difficulty": "Easy",
        "implementationCost": 0,
        "roi": 150.0
      }
    ]
  }
}
```

---