# UsageAnalytics API Documentation

## Overview
The `UsageAnalytics API` provides endpoints for analyzing vehicle usage patterns, comparing usage vs ownership, and generating insights for co-owners. This module is essential for understanding vehicle utilization and ensuring fairness among co-owners.

### Base URL
```
/api/usageanalytics
```

---

## Endpoints

### 1. Get Usage vs Ownership Comparison
**[CoOwner]**

- **Description:**
  Returns comparison data between actual vehicle usage and ownership percentages for all co-owners. Highlights overutilization or underutilization patterns.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/usage-vs-ownership
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleId`  | `int`    | Path     | Yes      | The ID of the vehicle.                          |
  | `startDate`  | `string` | Query    | No       | Analysis start date (ISO 8601 format).          |
  | `endDate`    | `string` | Query    | No       | Analysis end date (ISO 8601 format).            |
  | `usageMetric`| `string` | Query    | No       | Metric for usage calculation (e.g., "Hours").   |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/vehicle/1/usage-vs-ownership?usageMetric=Hours
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "USAGE_VS_OWNERSHIP_DATA_RETRIEVED_SUCCESSFULLY",
    "data": {
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "licensePlate": "30A-12345",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "usageMetric": "Hours",
      "coOwnersData": [
        {
          "coOwnerId": 1,
          "userId": 5,
          "coOwnerName": "John Doe",
          "email": "john@example.com",
          "ownershipPercentage": 40.00,
          "investmentAmount": 400000000.00,
          "usagePercentage": 55.50,
          "actualUsageValue": 222.0,
          "totalBookings": 15,
          "completedBookings": 13,
          "usageVsOwnershipDelta": 15.50,
          "usagePattern": "Overutilized",
          "fairUsageValue": 160.0
        }
      ],
      "summary": {
        "totalUsageValue": 400.0,
        "averageOwnershipPercentage": 50.00,
        "averageUsagePercentage": 50.00,
        "usageVariance": 15.50,
        "totalBookings": 25,
        "completedBookings": 22,
        "mostActiveCoOwner": { "coOwnerId": 1, "coOwnerName": "John Doe", "usagePercentage": 55.50 },
        "leastActiveCoOwner": { "coOwnerId": 2, "coOwnerName": "Jane Smith", "usagePercentage": 44.50 },
        "balancedCoOwnersCount": 0,
        "overutilizedCoOwnersCount": 1,
        "underutilizedCoOwnersCount": 1
      },
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Usage vs ownership data retrieved successfully. |
  | 403         | NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS.   |
  | 404         | VEHICLE_NOT_FOUND.                         |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 2. Get Usage vs Ownership Trends
**[CoOwner]**

- **Description:**
  Returns time-series data showing how usage patterns evolved compared to ownership over time. Useful for creating trend charts.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/usage-vs-ownership/trends
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleId`  | `int`    | Path     | Yes      | The ID of the vehicle.                          |
  | `startDate`  | `string` | Query    | No       | Analysis start date.                            |
  | `endDate`    | `string` | Query    | No       | Analysis end date.                              |
  | `granularity`| `string` | Query    | No       | Time period granularity (e.g., "Monthly").      |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/vehicle/1/usage-vs-ownership/trends?granularity=Monthly
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "USAGE_VS_OWNERSHIP_TRENDS_RETRIEVED_SUCCESSFULLY",
    "data": {
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "licensePlate": "30A-12345",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "granularity": "Monthly",
      "trendData": [
        {
          "date": "2024-01-01T00:00:00Z",
          "period": "Jan 2024",
          "coOwnersData": [
            {
              "coOwnerId": 1,
              "coOwnerName": "John Doe",
              "ownershipPercentage": 40.00,
              "usagePercentage": 60.00,
              "usageValue": 48.0
            }
          ]
        }
      ],
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Usage vs ownership trends retrieved successfully. |
  | 403         | NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS.   |
  | 404         | VEHICLE_NOT_FOUND.                         |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 3. Get Co-Owner Usage Detail
**[CoOwner]**

- **Description:**
  Returns detailed usage breakdown for a specific co-owner, including booking history and usage patterns.

- **Endpoint:**
  ```
  GET /vehicle/{vehicleId}/co-owner/{coOwnerId}/usage-detail
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleId`  | `int`    | Path     | Yes      | The ID of the vehicle.                          |
  | `coOwnerId`  | `int`    | Path     | Yes      | The ID of the co-owner.                         |
  | `startDate`  | `string` | Query    | No       | Analysis start date.                            |
  | `endDate`    | `string` | Query    | No       | Analysis end date.                              |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/vehicle/1/co-owner/1/usage-detail
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "CO_OWNER_USAGE_DETAIL_RETRIEVED_SUCCESSFULLY",
    "data": {
      "coOwnerId": 1,
      "userId": 5,
      "coOwnerName": "John Doe",
      "email": "john@example.com",
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "ownershipPercentage": 40.00,
      "usagePercentage": 55.50,
      "usageVsOwnershipDelta": 15.50,
      "usageMetrics": {
        "totalHours": 222.0,
        "hoursPercentage": 55.50,
        "totalDistance": 1500.0,
        "distancePercentage": 52.00,
        "totalBookings": 15,
        "bookingsPercentage": 60.00,
        "completedBookings": 13,
        "cancelledBookings": 2,
        "averageBookingDuration": 14.80
      },
      "recentBookings": [
        {
          "bookingId": 45,
          "startTime": "2024-10-20T08:00:00Z",
          "endTime": "2024-10-20T18:00:00Z",
          "durationHours": 10.0,
          "distanceTravelled": 120,
          "status": "Completed",
          "purpose": "Business trip"
        }
      ],
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Co-owner usage detail retrieved successfully. |
  | 403         | NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS.   |
  | 404         | CO_OWNER_NOT_FOUND.                        |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 4. Get My Usage History
**[CoOwner]**

- **Description:**
  Returns personal usage history for the current co-owner across all vehicles, with detailed booking and usage statistics.

- **Endpoint:**
  ```
  GET /my/usage-history
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `startDate`  | `string` | Query    | No       | History start date (ISO 8601 format).           |
  | `endDate`    | `string` | Query    | No       | History end date (ISO 8601 format).             |
  | `vehicleId`  | `int`    | Query    | No       | Filter by specific vehicle.                      |
  | `pageIndex`  | `int`    | Query    | No       | Page number (default: 1).                       |
  | `pageSize`   | `int`    | Query    | No       | Items per page (default: 20).                   |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/my/usage-history?vehicleId=1&pageIndex=1&pageSize=20
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "MY_USAGE_HISTORY_RETRIEVED_SUCCESSFULLY",
    "data": {
      "userId": 5,
      "userName": "John Doe",
      "email": "john@example.com",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "summary": {
        "totalVehicles": 3,
        "totalBookings": 45,
        "completedBookings": 42,
        "cancelledBookings": 3,
        "totalUsageHours": 650.5,
        "totalDistance": 8750,
        "averageBookingDuration": 15.5,
        "favoriteVehicle": "Tesla Model 3",
        "totalSpent": 12500000
      },
      "usageHistory": [
        {
          "bookingId": 156,
          "vehicleId": 1,
          "vehicleName": "Tesla Model 3",
          "licensePlate": "30A-12345",
          "startTime": "2024-10-20T08:00:00Z",
          "endTime": "2024-10-20T18:00:00Z",
          "durationHours": 10.0,
          "distanceTraveled": 120,
          "purpose": "Business trip",
          "cost": 500000,
          "status": "Completed"
        }
      ],
      "pagination": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalItems": 45,
        "totalPages": 3,
        "hasNextPage": true,
        "hasPreviousPage": false
      },
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | My usage history retrieved successfully.    |
  | 401         | UNAUTHORIZED.                               |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 5. Get Group Usage Summary
**[CoOwner]**

- **Description:**
  Returns usage summary for all co-ownership groups the user belongs to, providing insights into group dynamics and vehicle utilization.

- **Endpoint:**
  ```
  GET /group-summary
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `startDate`  | `string` | Query    | No       | Analysis start date.                            |
  | `endDate`    | `string` | Query    | No       | Analysis end date.                              |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/group-summary
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "GROUP_USAGE_SUMMARY_RETRIEVED_SUCCESSFULLY",
    "data": {
      "userId": 5,
      "userName": "John Doe",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "groupSummaries": [
        {
          "vehicleId": 1,
          "vehicleName": "Tesla Model 3",
          "licensePlate": "30A-12345",
          "groupSize": 3,
          "myOwnershipPercentage": 40.0,
          "myUsagePercentage": 45.5,
          "myUsageHours": 182.2,
          "groupTotalUsageHours": 400.0,
          "usageBalance": "Slightly Over",
          "usageRank": 1,
          "coOwners": [
            {
              "coOwnerId": 2,
              "userName": "Jane Smith",
              "ownershipPercentage": 35.0,
              "usagePercentage": 30.2
            }
          ],
          "utilizationEfficiency": 85.5,
          "groupHarmony": "Good"
        }
      ],
      "overallStats": {
        "totalGroups": 2,
        "averageUsageBalance": 15.2,
        "mostActiveGroup": "Tesla Model 3",
        "bestHarmonyGroup": "BMW i4",
        "improvementSuggestions": [
          "Consider adjusting usage schedule for Tesla Model 3",
          "Cost sharing appears well balanced across all groups"
        ]
      },
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Group usage summary retrieved successfully. |
  | 401         | UNAUTHORIZED.                               |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 6. Compare Co-Owners Usage
**[CoOwner]**

- **Description:**
  Compares usage patterns between different co-owners within the same vehicle group, useful for understanding fairness and planning.

- **Endpoint:**
  ```
  GET /compare/co-owners
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleId`  | `int`    | Query    | Yes      | The ID of the vehicle for comparison.           |
  | `coOwnerIds` | `string` | Query    | No       | Comma-separated co-owner IDs to compare.        |
  | `startDate`  | `string` | Query    | No       | Comparison start date.                          |
  | `endDate`    | `string` | Query    | No       | Comparison end date.                            |
  | `metric`     | `string` | Query    | No       | Comparison metric (Hours, Distance, Bookings).  |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/compare/co-owners?vehicleId=1&coOwnerIds=1,2,3&metric=Hours
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "CO_OWNERS_COMPARISON_RETRIEVED_SUCCESSFULLY",
    "data": {
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "comparisonMetric": "Hours",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "coOwnersComparison": [
        {
          "coOwnerId": 1,
          "userName": "John Doe",
          "ownershipPercentage": 40.0,
          "actualUsage": 182.2,
          "usagePercentage": 45.5,
          "expectedUsage": 160.0,
          "varianceFromExpected": 22.2,
          "variancePercentage": 13.9,
          "usagePattern": "Above Average",
          "efficiency": 92.3,
          "rank": 1
        },
        {
          "coOwnerId": 2,
          "userName": "Jane Smith",
          "ownershipPercentage": 35.0,
          "actualUsage": 121.0,
          "usagePercentage": 30.2,
          "expectedUsage": 140.0,
          "varianceFromExpected": -19.0,
          "variancePercentage": -13.6,
          "usagePattern": "Below Average",
          "efficiency": 86.4,
          "rank": 2
        }
      ],
      "comparisonInsights": {
        "mostActiveCoOwner": "John Doe",
        "leastActiveCoOwner": "Mike Johnson",
        "avgUsageVariance": 15.2,
        "fairnessScore": 7.8,
        "recommendations": [
          "John Doe is using 13.9% more than ownership share",
          "Consider adjusting cost sharing to reflect actual usage",
          "Jane Smith has room to increase usage if needed"
        ]
      },
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Co-owners comparison retrieved successfully. |
  | 400         | INVALID_VEHICLE_ID or INVALID_CO_OWNER_IDS. |
  | 403         | NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS.   |
  | 404         | VEHICLE_NOT_FOUND.                         |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 7. Compare Vehicles Usage
**[CoOwner]**

- **Description:**
  Compares usage efficiency and patterns across different vehicles the user has access to, helping optimize vehicle selection.

- **Endpoint:**
  ```
  GET /compare/vehicles
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleIds` | `string` | Query    | No       | Comma-separated vehicle IDs to compare.         |
  | `startDate`  | `string` | Query    | No       | Comparison start date.                          |
  | `endDate`    | `string` | Query    | No       | Comparison end date.                            |
  | `metric`     | `string` | Query    | No       | Comparison metric (Efficiency, Usage, Cost).    |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/compare/vehicles?vehicleIds=1,2,3&metric=Efficiency
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "VEHICLES_COMPARISON_RETRIEVED_SUCCESSFULLY",
    "data": {
      "userId": 5,
      "userName": "John Doe",
      "comparisonMetric": "Efficiency",
      "analysisStartDate": "2024-01-01T00:00:00Z",
      "analysisEndDate": "2024-10-23T00:00:00Z",
      "vehiclesComparison": [
        {
          "vehicleId": 1,
          "vehicleName": "Tesla Model 3",
          "licensePlate": "30A-12345",
          "myOwnershipPercentage": 40.0,
          "myUsageHours": 182.2,
          "myUsagePercentage": 45.5,
          "averageCostPerHour": 50000,
          "totalCostSpent": 9110000,
          "utilizationRate": 85.5,
          "efficiency": 92.3,
          "satisfaction": 4.8,
          "rank": 1
        },
        {
          "vehicleId": 2,
          "vehicleName": "BMW i4",
          "licensePlate": "30B-67890",
          "myOwnershipPercentage": 50.0,
          "myUsageHours": 95.5,
          "myUsagePercentage": 48.2,
          "averageCostPerHour": 55000,
          "totalCostSpent": 5252500,
          "utilizationRate": 76.2,
          "efficiency": 88.9,
          "satisfaction": 4.6,
          "rank": 2
        }
      ],
      "comparisonInsights": {
        "mostEfficientVehicle": "Tesla Model 3",
        "mostCostEffectiveVehicle": "Tesla Model 3",
        "highestSatisfactionVehicle": "Tesla Model 3",
        "recommendations": [
          "Tesla Model 3 offers best value for money",
          "Consider increasing usage of BMW i4 to maximize ownership value",
          "Overall usage distribution is well balanced"
        ]
      },
      "overallPerformance": {
        "totalUsageHours": 277.7,
        "totalCostSpent": 14362500,
        "averageEfficiency": 90.6,
        "averageSatisfaction": 4.7
      },
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Vehicles comparison retrieved successfully.  |
  | 400         | INVALID_VEHICLE_IDS.                       |
  | 403         | NOT_AUTHORIZED_TO_VIEW_ANALYTICS.           |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

### 8. Compare Time Periods
**[CoOwner]**

- **Description:**
  Compares usage patterns across different time periods to identify trends, seasonal variations, and changes in behavior.

- **Endpoint:**
  ```
  GET /compare/periods
  ```

- **Parameters:**
  | Name         | Type     | Location | Required | Description                                      |
  |--------------|----------|----------|----------|--------------------------------------------------|
  | `vehicleId`  | `int`    | Query    | No       | Filter by specific vehicle.                     |
  | `period1Start` | `string` | Query  | Yes      | First period start date.                        |
  | `period1End` | `string` | Query    | Yes      | First period end date.                          |
  | `period2Start` | `string` | Query  | Yes      | Second period start date.                       |
  | `period2End` | `string` | Query    | Yes      | Second period end date.                         |
  | `granularity`| `string` | Query    | No       | Daily, Weekly, Monthly (default: Monthly).      |

- **Sample Request:**
  ```http
  GET /api/usageanalytics/compare/periods?vehicleId=1&period1Start=2024-01-01&period1End=2024-06-30&period2Start=2024-07-01&period2End=2024-10-23&granularity=Monthly
  Authorization: Bearer {token}
  ```

- **Sample Response:**
  ```json
  {
    "statusCode": 200,
    "message": "PERIODS_COMPARISON_RETRIEVED_SUCCESSFULLY",
    "data": {
      "userId": 5,
      "userName": "John Doe",
      "vehicleId": 1,
      "vehicleName": "Tesla Model 3",
      "granularity": "Monthly",
      "period1": {
        "label": "Q1-Q2 2024",
        "startDate": "2024-01-01T00:00:00Z",
        "endDate": "2024-06-30T23:59:59Z",
        "totalUsageHours": 95.5,
        "totalBookings": 18,
        "averageBookingDuration": 5.3,
        "totalDistance": 1250,
        "totalCost": 4775000,
        "utilizationRate": 78.2
      },
      "period2": {
        "label": "Q3-Q4 2024",
        "startDate": "2024-07-01T00:00:00Z",
        "endDate": "2024-10-23T23:59:59Z",
        "totalUsageHours": 86.7,
        "totalBookings": 24,
        "averageBookingDuration": 3.6,
        "totalDistance": 1180,
        "totalCost": 4335000,
        "utilizationRate": 82.1
      },
      "comparison": {
        "usageHoursChange": -8.8,
        "usageHoursChangePercentage": -9.2,
        "bookingsChange": 6,
        "bookingsChangePercentage": 33.3,
        "averageDurationChange": -1.7,
        "distanceChange": -70,
        "costChange": -440000,
        "utilizationChange": 3.9,
        "trend": "More frequent, shorter trips",
        "insights": [
          "Usage frequency increased by 33.3%",
          "Average trip duration decreased by 32.1%",
          "Cost efficiency improved slightly",
          "Pattern suggests shift to daily commute usage"
        ]
      },
      "monthlyBreakdown": [
        {
          "month": "2024-01",
          "period1Hours": 18.2,
          "period2Hours": null,
          "label": "Jan 2024"
        },
        {
          "month": "2024-07",
          "period1Hours": null,
          "period2Hours": 22.5,
          "label": "Jul 2024"
        }
      ],
      "generatedAt": "2024-10-23T10:30:00Z"
    }
  }
  ```

- **Responses:**
  | Status Code | Message                                      |
  |-------------|---------------------------------------------|
  | 200         | Periods comparison retrieved successfully.   |
  | 400         | INVALID_DATE_RANGES or INVALID_PARAMETERS.  |
  | 403         | NOT_AUTHORIZED_TO_VIEW_ANALYTICS.           |
  | 404         | VEHICLE_NOT_FOUND.                         |
  | 500         | INTERNAL_SERVER_ERROR.                     |

---

*Additional endpoints will be documented similarly.*