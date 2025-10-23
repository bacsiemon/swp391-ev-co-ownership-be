# 🚗 Booking Conflict Resolution Feature - Intelligent Approve/Reject System

## 📋 Table of Contents
- [Overview](#overview)
- [Business Context](#business-context)
- [Key Features](#key-features)
- [API Endpoints](#api-endpoints)
- [Resolution Types](#resolution-types)
- [Business Logic](#business-logic)
- [Data Models](#data-models)
- [Usage Examples](#usage-examples)
- [Frontend Integration](#frontend-integration)
- [Analytics & Insights](#analytics--insights)

---

## 🎯 Overview

**Advanced booking conflict resolution system for EV co-ownership** that goes beyond simple approve/reject decisions. This intelligent system considers ownership percentages, usage fairness, priority levels, and co-owner collaboration to make optimal decisions for vehicle booking conflicts.

### **Problem Solved**
In an EV co-ownership scenario with multiple owners sharing one vehicle, booking conflicts are inevitable. Traditional first-come-first-served or admin-approval systems don't account for:
- **Ownership equity**: Someone owning 60% should have more say than someone with 10%
- **Usage fairness**: A co-owner who rarely uses the vehicle should get priority over heavy users
- **Emergency situations**: Urgent medical trips should override routine commutes
- **Transparency**: Decisions need clear explanations to maintain trust

### **Solution Approach**
Multi-tier intelligent resolution system:
1. **Simple Approval**: Basic approve/reject with notes
2. **Counter-Offers**: Suggest alternative times instead of flat rejection
3. **Priority Override**: Use ownership weight + usage fairness for automatic resolution
4. **Auto-Negotiation**: AI-driven conflict resolution based on multiple factors
5. **Consensus Required**: Democratic approval from all conflicting parties

---

## 💼 Business Context

### **Real-World Scenario**

**The Smith Family's EV Sharing:**
- **Alice (40% ownership)**: Uses car for commute, ~60h/month
- **Bob (35% ownership)**: Weekend trips, ~30h/month  
- **Charlie (25% ownership)**: Occasional errands, ~15h/month

**Conflict Situation:**
- Bob requests car for Saturday 9AM-5PM (priority: Medium)
- Alice already has a pending booking for Saturday 2PM-8PM (overlap: 3 hours)

**Traditional Systems:** 
- ❌ First-come: Bob gets it (unfair to Alice's higher ownership)
- ❌ Admin approval: Delays, manual intervention

**Our Intelligent System:**
```json
{
  "resolutionType": "AutoNegotiation",
  "decision": "Approve Bob's request",
  "reasoning": [
    "Bob has lower usage this month (30h vs Alice's 60h)",
    "Alice can shift her booking to 5PM-11PM (suggested alternative)",
    "Bob's ownership % is close to Alice's (35% vs 40%)",
    "No hard conflicts with confirmed bookings"
  ],
  "fairnessScore": 0.85
}
```

---

## ✨ Key Features

### 1. **Multi-Resolution Types**
| Type | Use Case | Intelligence Level |
|------|----------|-------------------|
| **Simple Approval** | Quick decisions | Low - Manual |
| **Counter-Offer** | Suggest alternatives | Medium - Semi-auto |
| **Priority Override** | Ownership-based | High - Rule-based |
| **Auto-Negotiation** | Complex conflicts | Very High - AI |
| **Consensus** | Major decisions | Collaborative |

### 2. **Ownership Weighting**
```typescript
Priority Weight = (Ownership% / 2) + Fairness Score - Conflict Penalty

// Example:
// Alice: 40% ownership, 60h usage → Weight = 20 + 0 - 10 = 10
// Bob: 35% ownership, 30h usage → Weight = 17.5 + 20 - 0 = 37.5
// Winner: Bob (higher fairness due to lower usage)
```

### 3. **Usage Fairness Calculation**
- **Fairness Weight**: `Max(0, 30 - (usageHours / 10))`
- Less usage = Higher priority
- Encourages balanced sharing

### 4. **Conflict Penalty**
- **-10 points** if you already have a booking during requested time
- Prevents double-booking

### 5. **Auto-Resolution Algorithm**
```csharp
if (RequesterPriorityWeight > AvgConflictPriority + 10) {
    if (RequesterOwnership > 50%) {
        return "Approved - Majority owner";
    } else if (RequesterUsage < AvgConflictUsage) {
        return "Approved - Fairness principle";
    } else {
        return "Approved - Higher priority weight";
    }
} else {
    return "Rejected - Existing bookings take precedence";
}
```

---

## 🔌 API Endpoints

### 1. **POST /api/booking/{bookingId}/resolve-conflict**
Resolve a booking conflict with intelligent decision-making.

**Authorization:** `CoOwner` role required

**Request Body:**
```json
{
  "isApproved": true,
  "resolutionType": 3,
  "useOwnershipWeighting": true,
  "enableAutoNegotiation": true,
  "rejectionReason": "string (optional)",
  "priorityJustification": "string (optional)",
  "counterOfferStartTime": "2025-01-26T09:00:00 (optional)",
  "counterOfferEndTime": "2025-01-26T17:00:00 (optional)",
  "notes": "string (optional)"
}
```

**Response:**
```json
{
  "statusCode": 200,
  "message": "CONFLICT_AUTO_RESOLVED",
  "data": {
    "bookingId": 124,
    "outcome": 3,
    "finalStatus": 1,
    "resolvedBy": "System",
    "resolvedAt": "2025-01-17T14:30:00",
    "resolutionExplanation": "Auto-approved: Bob Smith has lower usage this month",
    "stakeholders": [
      {
        "userId": 5,
        "name": "Alice Johnson",
        "ownershipPercentage": 40,
        "usageHoursThisMonth": 60,
        "hasApproved": false,
        "priorityWeight": 10
      },
      {
        "userId": 6,
        "name": "Bob Smith",
        "ownershipPercentage": 35,
        "usageHoursThisMonth": 30,
        "hasApproved": true,
        "priorityWeight": 37.5
      }
    ],
    "approvalStatus": {
      "totalStakeholders": 2,
      "approvalsReceived": 1,
      "rejectionsReceived": 0,
      "pendingResponses": 1,
      "approvalPercentage": 50,
      "weightedApprovalPercentage": 35,
      "isFullyApproved": false
    },
    "autoResolution": {
      "wasAutoResolved": true,
      "reason": 1,
      "explanation": "Auto-approved: Bob Smith has lower usage this month",
      "requesterPriorityScore": 37.5,
      "conflictingOwnerPriorityScore": 10,
      "winnerName": "Bob Smith",
      "factorsConsidered": [
        "Requester has lower usage this month (30h)",
        "Ownership percentages are similar"
      ]
    },
    "recommendedActions": []
  }
}
```

---

### 2. **GET /api/booking/pending-conflicts**
Get all pending conflicts requiring resolution.

**Authorization:** `CoOwner` role required

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `vehicleId` | int | No | Filter by vehicle |
| `onlyMyConflicts` | bool | No | Show only conflicts you're involved in |
| `minimumPriority` | int | No | Filter by priority (0-3) |
| `includeAutoResolvable` | bool | No | Include auto-resolvable conflicts |

**Response:**
```json
{
  "statusCode": 200,
  "message": "PENDING_CONFLICTS_RETRIEVED",
  "data": {
    "totalConflicts": 3,
    "requiringMyAction": 2,
    "autoResolvable": 1,
    "oldestConflictDate": "2025-01-15T10:00:00",
    "actionItems": [
      "You have 2 conflict(s) requiring your response",
      "1 conflict(s) can be auto-resolved"
    ],
    "conflicts": [
      {
        "bookingId": 124,
        "requesterName": "Bob Smith",
        "requestedStartTime": "2025-01-25T09:00:00",
        "requestedEndTime": "2025-01-25T17:00:00",
        "purpose": "Weekend trip",
        "priority": 2,
        "conflictsWith": [
          {
            "bookingId": 120,
            "coOwnerName": "Alice Johnson",
            "startTime": "2025-01-25T14:00:00",
            "endTime": "2025-01-25T20:00:00",
            "overlapHours": 3,
            "coOwnerOwnershipPercentage": 40,
            "hasResponded": false
          }
        ],
        "daysPending": 2,
        "canAutoResolve": true,
        "autoResolutionPreview": {
          "predictedOutcome": 0,
          "winnerName": "Bob Smith",
          "explanation": "Bob Smith likely to be approved (lower usage)",
          "confidence": 0.8,
          "factors": [
            "Requester priority: 37.5",
            "Average conflict priority: 10",
            "Requester usage: 30h"
          ]
        }
      }
    ]
  }
}
```

---

### 3. **GET /api/booking/vehicle/{vehicleId}/conflict-analytics**
Get conflict resolution analytics and insights.

**Authorization:** `CoOwner` role required

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `vehicleId` | int | Yes | Vehicle ID |
| `startDate` | DateTime | No | Start date (default: 90 days ago) |
| `endDate` | DateTime | No | End date (default: today) |

**Response:**
```json
{
  "statusCode": 200,
  "message": "CONFLICT_ANALYTICS_RETRIEVED",
  "data": {
    "totalConflictsResolved": 45,
    "totalConflictsPending": 3,
    "averageResolutionTimeHours": 12.5,
    "approvalRate": 68.9,
    "rejectionRate": 31.1,
    "autoResolutionRate": 15.6,
    "statsByCoOwner": [
      {
        "userId": 5,
        "name": "Alice Johnson",
        "conflictsInitiated": 12,
        "conflictsReceived": 8,
        "approvalsGiven": 6,
        "rejectionsGiven": 2,
        "approvalRateAsResponder": 75.0,
        "successRateAsRequester": 66.7,
        "averageResponseTimeHours": 8.2
      },
      {
        "userId": 6,
        "name": "Bob Smith",
        "conflictsInitiated": 8,
        "conflictsReceived": 10,
        "approvalsGiven": 8,
        "rejectionsGiven": 2,
        "approvalRateAsResponder": 80.0,
        "successRateAsRequester": 87.5,
        "averageResponseTimeHours": 6.5
      }
    ],
    "commonPatterns": [
      {
        "pattern": "High weekend conflict rate",
        "occurrences": 15,
        "recommendation": "Consider implementing weekend rotation schedule"
      },
      {
        "pattern": "Morning commute conflicts (7-9 AM)",
        "occurrences": 8,
        "recommendation": "Establish priority system for commute times"
      }
    ],
    "recommendations": [
      {
        "recommendation": "Implement weekend rotation schedule",
        "rationale": "High weekend conflict rate detected",
        "suggestedApproach": 4
      },
      {
        "recommendation": "Review fairness policies for low-success co-owners",
        "rationale": "Some co-owners have low approval rates",
        "suggestedApproach": 3
      }
    ]
  }
}
```

---

## 🎭 Resolution Types

### **0. Simple Approval**
**Use Case:** Quick yes/no decisions without complex logic.

**Example:**
```json
{
  "isApproved": true,
  "resolutionType": 0,
  "notes": "Approved - I can use public transport that day"
}
```

---

### **1. Counter-Offer**
**Use Case:** Reject but suggest alternative time.

**Example:**
```json
{
  "isApproved": false,
  "resolutionType": 1,
  "rejectionReason": "I need the car for medical appointment",
  "counterOfferStartTime": "2025-01-26T09:00:00",
  "counterOfferEndTime": "2025-01-26T17:00:00",
  "notes": "Can you use it the next day instead?"
}
```

**System Response:**
- Creates counter-offer record
- Notifies requester
- Requester can accept/reject/propose new time

---

### **2. Priority Override**
**Use Case:** Let ownership % and usage fairness decide.

**Example:**
```json
{
  "isApproved": false,
  "resolutionType": 2,
  "useOwnershipWeighting": true,
  "priorityJustification": "I have 60% ownership and less usage this month",
  "rejectionReason": "I need priority for this booking"
}
```

**Calculation:**
```
Alice Weight = (60% / 2) + Fairness(20h) - Conflict(0) = 30 + 25 + 0 = 55
Bob Weight = (35% / 2) + Fairness(60h) - Conflict(-10) = 17.5 + 0 - 10 = 7.5

Winner: Alice (higher weight)
```

---

### **3. Auto-Negotiation**
**Use Case:** Let system decide based on all factors.

**Example:**
```json
{
  "isApproved": true,
  "resolutionType": 3,
  "enableAutoNegotiation": true,
  "useOwnershipWeighting": true
}
```

**System Considers:**
- Ownership percentages
- Usage hours this month
- Priority levels
- Conflict history
- Response times

---

### **4. Consensus Required**
**Use Case:** Democratic decision - all must approve.

**Example:**
```json
{
  "isApproved": true,
  "resolutionType": 4,
  "notes": "I approve, waiting for others"
}
```

**System Tracks:**
- Who approved
- Who rejected
- Who's pending
- Weighted approval % (based on ownership)

---

## 🧠 Business Logic

### **Priority Weight Calculation**
```csharp
int CalculatePriorityWeight(decimal ownershipPercentage, int usageHoursThisMonth, bool hasConflict)
{
    // Base weight from ownership (0-50 points)
    var ownershipWeight = (int)(ownershipPercentage / 2);

    // Fairness weight: Less usage = higher priority (0-30 points)
    var fairnessWeight = Math.Max(0, 30 - (usageHoursThisMonth / 10));

    // Conflict penalty (-10 points if has existing booking)
    var conflictPenalty = hasConflict ? -10 : 0;

    return ownershipWeight + fairnessWeight + conflictPenalty;
}
```

**Example Calculations:**

| Co-Owner | Ownership | Usage (h) | Conflict | Calculation | Weight |
|----------|-----------|-----------|----------|-------------|--------|
| Alice | 60% | 80h | Yes | 30 + 0 - 10 | **20** |
| Bob | 30% | 20h | No | 15 + 20 + 0 | **35** |
| Charlie | 10% | 5h | No | 5 + 29 + 0 | **34** |

**Winner:** Bob (highest fairness-adjusted weight despite lower ownership)

---

## 📊 Data Models

### **ResolveBookingConflictRequest**
```csharp
public class ResolveBookingConflictRequest
{
    public bool IsApproved { get; set; }
    public ConflictResolutionType ResolutionType { get; set; }
    public string? RejectionReason { get; set; }
    public string? PriorityJustification { get; set; }
    public DateTime? CounterOfferStartTime { get; set; }
    public DateTime? CounterOfferEndTime { get; set; }
    public string? Notes { get; set; }
    public bool UseOwnershipWeighting { get; set; } = true;
    public bool EnableAutoNegotiation { get; set; } = false;
}
```

### **BookingConflictResolutionResponse**
```csharp
public class BookingConflictResolutionResponse
{
    public int BookingId { get; set; }
    public ConflictResolutionOutcome Outcome { get; set; }
    public EBookingStatus FinalStatus { get; set; }
    public string ResolvedBy { get; set; }
    public DateTime ResolvedAt { get; set; }
    public string ResolutionExplanation { get; set; }
    public CounterOfferInfo? CounterOffer { get; set; }
    public List<ConflictStakeholder> Stakeholders { get; set; }
    public ConflictApprovalStatus ApprovalStatus { get; set; }
    public AutoResolutionInfo? AutoResolution { get; set; }
    public List<string> RecommendedActions { get; set; }
}
```

### **Enums**

**ConflictResolutionType:**
- `SimpleApproval = 0`
- `CounterOffer = 1`
- `PriorityOverride = 2`
- `AutoNegotiation = 3`
- `ConsensusRequired = 4`

**ConflictResolutionOutcome:**
- `Approved = 0`
- `Rejected = 1`
- `CounterOfferMade = 2`
- `AutoResolved = 3`
- `AwaitingMoreApprovals = 4`
- `Negotiating = 5`

**AutoResolutionReason:**
- `OwnershipWeight = 0`
- `UsageFairness = 1`
- `PriorityLevel = 2`
- `FirstComeFirstServed = 3`
- `ConsensusReached = 4`

---

## 💡 Usage Examples

### **Scenario 1: Simple Approval**

**Context:** Alice has a conflict with Bob's booking. She approves it.

```typescript
// Frontend code
const approveBooking = async (bookingId: number) => {
  const response = await fetch(`/api/booking/${bookingId}/resolve-conflict`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      isApproved: true,
      resolutionType: 0, // SimpleApproval
      notes: "Approved - I can take the bus"
    })
  });
  
  const result = await response.json();
  console.log(result.data.resolutionExplanation);
  // "Booking approved by Alice Johnson. Conflicting bookings cancelled."
};
```

---

### **Scenario 2: Counter-Offer**

**Context:** Bob rejects but suggests next day.

```typescript
const makeCounterOffer = async (bookingId: number) => {
  const response = await fetch(`/api/booking/${bookingId}/resolve-conflict`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      isApproved: false,
      resolutionType: 1, // CounterOffer
      rejectionReason: "I need the car for medical appointment",
      counterOfferStartTime: "2025-01-26T09:00:00",
      counterOfferEndTime: "2025-01-26T17:00:00",
      notes: "Can you use it the next day? I can drop it off at your place."
    })
  });
  
  const result = await response.json();
  if (result.data.counterOffer) {
    showNotification(
      `Counter-offer made: ${result.data.counterOffer.suggestedStartTime} - ${result.data.counterOffer.suggestedEndTime}`
    );
  }
};
```

---

### **Scenario 3: Auto-Negotiation**

**Context:** Let system decide based on fairness.

```typescript
const autoResolve = async (bookingId: number) => {
  const response = await fetch(`/api/booking/${bookingId}/resolve-conflict`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      isApproved: true,
      resolutionType: 3, // AutoNegotiation
      enableAutoNegotiation: true,
      useOwnershipWeighting: true
    })
  });
  
  const result = await response.json();
  
  if (result.data.autoResolution) {
    console.log("Auto-resolution explanation:");
    console.log(result.data.autoResolution.explanation);
    console.log("Factors considered:");
    result.data.autoResolution.factorsConsidered.forEach(factor => {
      console.log(`- ${factor}`);
    });
  }
};
```

---

### **Scenario 4: Get Pending Conflicts**

```typescript
const getPendingConflicts = async () => {
  const params = new URLSearchParams({
    vehicleId: '5',
    onlyMyConflicts: 'true',
    includeAutoResolvable: 'true'
  });
  
  const response = await fetch(`/api/booking/pending-conflicts?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result = await response.json();
  
  console.log(`Total conflicts: ${result.data.totalConflicts}`);
  console.log(`Requiring your action: ${result.data.requiringMyAction}`);
  
  result.data.conflicts.forEach(conflict => {
    console.log(`\nConflict #${conflict.bookingId}:`);
    console.log(`- Requester: ${conflict.requesterName}`);
    console.log(`- Time: ${conflict.requestedStartTime} - ${conflict.requestedEndTime}`);
    console.log(`- Days pending: ${conflict.daysPending}`);
    
    if (conflict.autoResolutionPreview) {
      console.log(`- Auto-resolve: ${conflict.autoResolutionPreview.explanation}`);
      console.log(`- Confidence: ${conflict.autoResolutionPreview.confidence * 100}%`);
    }
  });
};
```

---

## 🎨 Frontend Integration

### **React Component: Conflict Resolution Dashboard**

```typescript
import React, { useState, useEffect } from 'react';
import { BookingConflictAPI } from './api';

interface PendingConflict {
  bookingId: number;
  requesterName: string;
  requestedStartTime: string;
  requestedEndTime: string;
  purpose: string;
  priority: number;
  daysPending: number;
  conflictsWith: ConflictingBooking[];
  canAutoResolve: boolean;
  autoResolutionPreview?: AutoResolutionPreview;
}

const ConflictResolutionDashboard: React.FC = () => {
  const [conflicts, setConflicts] = useState<PendingConflict[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedResolutionType, setSelectedResolutionType] = useState(0);

  useEffect(() => {
    loadConflicts();
  }, []);

  const loadConflicts = async () => {
    const response = await BookingConflictAPI.getPendingConflicts({
      onlyMyConflicts: true,
      includeAutoResolvable: true
    });
    setConflicts(response.data.conflicts);
    setLoading(false);
  };

  const handleResolve = async (bookingId: number, approved: boolean) => {
    const request = {
      isApproved: approved,
      resolutionType: selectedResolutionType,
      useOwnershipWeighting: true,
      enableAutoNegotiation: selectedResolutionType === 3,
      notes: approved ? "Approved" : "Rejected"
    };

    const response = await BookingConflictAPI.resolveConflict(bookingId, request);
    
    if (response.statusCode === 200) {
      alert(response.data.resolutionExplanation);
      loadConflicts(); // Refresh list
    }
  };

  const renderAutoResolutionPreview = (preview: AutoResolutionPreview) => (
    <div className="auto-resolution-preview">
      <h4>Auto-Resolution Preview</h4>
      <p><strong>Predicted Winner:</strong> {preview.winnerName}</p>
      <p><strong>Explanation:</strong> {preview.explanation}</p>
      <p><strong>Confidence:</strong> {(preview.confidence * 100).toFixed(0)}%</p>
      <div className="factors">
        <strong>Factors:</strong>
        <ul>
          {preview.factors.map((factor, idx) => (
            <li key={idx}>{factor}</li>
          ))}
        </ul>
      </div>
    </div>
  );

  if (loading) return <div>Loading...</div>;

  return (
    <div className="conflict-dashboard">
      <h2>Pending Booking Conflicts ({conflicts.length})</h2>

      <div className="resolution-type-selector">
        <label>Resolution Type:</label>
        <select 
          value={selectedResolutionType} 
          onChange={(e) => setSelectedResolutionType(Number(e.target.value))}
        >
          <option value={0}>Simple Approval</option>
          <option value={1}>Counter-Offer</option>
          <option value={2}>Priority Override</option>
          <option value={3}>Auto-Negotiation</option>
          <option value={4}>Consensus Required</option>
        </select>
      </div>

      {conflicts.map(conflict => (
        <div key={conflict.bookingId} className="conflict-card">
          <div className="conflict-header">
            <h3>Request from {conflict.requesterName}</h3>
            <span className={`priority priority-${conflict.priority}`}>
              Priority: {['Low', 'Medium', 'High', 'Urgent'][conflict.priority]}
            </span>
          </div>

          <div className="conflict-details">
            <p><strong>Time:</strong> {new Date(conflict.requestedStartTime).toLocaleString()} - 
               {new Date(conflict.requestedEndTime).toLocaleString()}</p>
            <p><strong>Purpose:</strong> {conflict.purpose}</p>
            <p><strong>Days Pending:</strong> {conflict.daysPending}</p>
          </div>

          <div className="conflicting-bookings">
            <h4>Conflicts With:</h4>
            {conflict.conflictsWith.map((cb, idx) => (
              <div key={idx} className="conflicting-booking">
                <p>{cb.coOwnerName} - Overlap: {cb.overlapHours}h</p>
              </div>
            ))}
          </div>

          {conflict.canAutoResolve && conflict.autoResolutionPreview && 
            renderAutoResolutionPreview(conflict.autoResolutionPreview)}

          <div className="conflict-actions">
            <button 
              className="btn-approve" 
              onClick={() => handleResolve(conflict.bookingId, true)}
            >
              Approve
            </button>
            <button 
              className="btn-reject" 
              onClick={() => handleResolve(conflict.bookingId, false)}
            >
              Reject
            </button>
            {conflict.canAutoResolve && (
              <button 
                className="btn-auto" 
                onClick={() => {
                  setSelectedResolutionType(3);
                  handleResolve(conflict.bookingId, true);
                }}
              >
                Auto-Resolve
              </button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};

export default ConflictResolutionDashboard;
```

---

## 📈 Analytics & Insights

### **1. Conflict Resolution Analytics**

**Purpose:** Track resolution patterns, co-owner behavior, and fairness metrics.

**Key Metrics:**
- **Resolution Time**: How fast conflicts get resolved
- **Approval Rate**: % of conflicts approved vs rejected
- **Auto-Resolution Rate**: How often system can auto-resolve
- **Per Co-Owner Stats**: Success rates, response times
- **Pattern Detection**: Weekend conflicts, morning rush conflicts

**Usage:**
```typescript
const getAnalytics = async (vehicleId: number) => {
  const response = await fetch(
    `/api/booking/vehicle/${vehicleId}/conflict-analytics?startDate=2025-01-01`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );
  
  const analytics = await response.json();
  
  console.log(`Approval Rate: ${analytics.data.approvalRate}%`);
  console.log(`Avg Resolution Time: ${analytics.data.averageResolutionTimeHours}h`);
  
  analytics.data.commonPatterns.forEach(pattern => {
    console.log(`Pattern: ${pattern.pattern}`);
    console.log(`Recommendation: ${pattern.recommendation}`);
  });
};
```

---

### **2. Fairness Metrics**

**Weighted Approval Percentage:**
```
Weighted% = Σ(ApproverOwnership%) / TotalOwnership%

Example:
- Alice (40%) approves → 40%
- Bob (35%) approves → 35%
- Total: 75% weighted approval (even though only 2 out of 3 people approved)
```

**Usage Fairness Score:**
```
FairnessScore = 1 - (StdDev(UsageHours) / Mean(UsageHours))

Perfect fairness = 1.0 (everyone uses equally)
Poor fairness = 0.0 (huge usage disparity)
```

---

## 🔒 Security & Authorization

### **Access Control**
- Only **CoOwner** role can resolve conflicts
- Can only resolve conflicts for vehicles they co-own
- Cannot resolve own requests (must be done by other co-owners)

### **Validation**
- FluentValidation on all request DTOs
- Rejection reason required when rejecting
- Counter-offer times must be valid (future dates, end > start)
- Resolution type must match provided data (e.g., counter-offer requires times)

---

## 🚀 Next Steps

1. **Test all 5 resolution types** with different scenarios
2. **Implement real-time notifications** (SignalR) for conflict updates
3. **Add counter-offer acceptance flow** (requester responds to counter-offers)
4. **Create dashboard** with analytics charts
5. **Implement recurring conflict patterns** (e.g., every Saturday morning)
6. **Add conflict history** tracking for audit trail
7. **Integrate with mobile app** for push notifications

---

## 📝 Summary

This **Booking Conflict Resolution Feature** provides an **intelligent, fair, and transparent** system for managing booking conflicts in EV co-ownership scenarios. It goes beyond simple approve/reject by considering:

✅ **Ownership equity** (% ownership matters)  
✅ **Usage fairness** (balanced sharing)  
✅ **Transparency** (clear explanations)  
✅ **Flexibility** (counter-offers, auto-negotiation)  
✅ **Analytics** (patterns, recommendations)

**Result:** A collaborative, data-driven conflict resolution system that maintains trust and fairness among co-owners while maximizing vehicle utilization.
