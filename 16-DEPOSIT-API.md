# Deposit API Documentation

## Overview
The Deposit API manages deposit transactions for vehicle co-ownership, including creating deposits, retrieving deposit details, viewing deposit history, canceling deposits, and handling payment callbacks. It supports multiple payment methods such as credit cards, e-wallets, online banking, and QR code payments.

### Base URL
```
/api/deposit
```

---

## Endpoints

### 1. Create a New Deposit
**[POST] /api/deposit**

**Description:**
Creates a new deposit transaction and returns a payment URL.

**Request Body:**
```json
{
  "amount": 500000,
  "depositMethod": 0,
  "description": "Deposit for vehicle maintenance"
}
```

**Deposit Methods:**
- `0`: CreditCard
- `1`: EWallet
- `2`: OnlineBanking
- `3`: QRCode

**Responses:**
- `201 Created`: Deposit created successfully.
- `400 Bad Request`: Validation error.
- `401 Unauthorized`: User not authenticated.
- `404 Not Found`: User not found.
- `500 Internal Server Error`: Server error.

---

### 2. Get Deposit by ID
**[GET] /api/deposit/{id}**

**Description:**
Retrieve detailed information about a specific deposit transaction.

**Responses:**
- `200 OK`: Deposit details retrieved successfully.
- `401 Unauthorized`: User not authenticated.
- `403 Forbidden`: User cannot access this deposit.
- `404 Not Found`: Deposit not found.
- `500 Internal Server Error`: Server error.

---

### 3. Get User's Deposit History
**[GET] /api/deposit/my-deposits**

**Description:**
Retrieve a paginated list of the user's deposit transactions with filtering options.

**Query Parameters:**
- `depositMethod` (optional): Filter by method.
- `status` (optional): Filter by status.
- `fromDate` (optional): Filter deposits from this date.
- `toDate` (optional): Filter deposits to this date.
- `minAmount` (optional): Minimum deposit amount.
- `maxAmount` (optional): Maximum deposit amount.
- `pageNumber` (default: 1): Page number.
- `pageSize` (default: 20): Items per page.
- `sortBy` (optional): Sort field.
- `sortOrder` (default: desc): Sort order.

**Responses:**
- `200 OK`: Deposits retrieved successfully.
- `401 Unauthorized`: User not authenticated.
- `404 Not Found`: User not found.
- `500 Internal Server Error`: Server error.

---

### 4. Get All Deposits (Admin/Staff)
**[GET] /api/deposit**

**Description:**
Retrieve a paginated list of all deposit transactions with filtering options. Admin and staff only.

**Responses:**
- `200 OK`: Deposits retrieved successfully.
- `401 Unauthorized`: User not authenticated.
- `403 Forbidden`: Requires Admin or Staff role.
- `500 Internal Server Error`: Server error.

---

### 5. Cancel a Deposit
**[POST] /api/deposit/{id}/cancel**

**Description:**
Cancel a pending deposit. Users can only cancel deposits with status = Pending.

**Responses:**
- `200 OK`: Deposit canceled successfully.
- `400 Bad Request`: Cannot cancel - deposit is not pending.
- `401 Unauthorized`: User not authenticated.
- `403 Forbidden`: User cannot cancel this deposit.
- `404 Not Found`: Deposit not found.
- `500 Internal Server Error`: Server error.

---

### 6. Get User's Deposit Statistics
**[GET] /api/deposit/my-statistics**

**Description:**
Retrieve aggregated statistics about the user's deposit history, including total deposits, amounts, and breakdowns by method and status.

**Responses:**
- `200 OK`: Statistics retrieved successfully.
- `401 Unauthorized`: User not authenticated.
- `500 Internal Server Error`: Server error.

---

### 7. Get Payment Methods
**[GET] /api/deposit/payment-methods**

**Description:**
Retrieve a list of supported deposit methods with details such as availability, min/max amounts, and supported banks/e-wallets. Public endpoint.

**Responses:**
- `200 OK`: Payment methods retrieved successfully.
- `500 Internal Server Error`: Server error.

---

### 8. Payment Gateway Callback
**[GET] /api/deposit/callback**

**Description:**
Internal endpoint called by payment gateways (e.g., VNPay, Momo, ZaloPay) after payment completion. Verifies payment signature and updates deposit status.

**Query Parameters:**
- `depositId`: Deposit ID.
- `gatewayTransactionId`: Gateway transaction ID.
- `isSuccess`: Payment success status.
- `responseCode` (optional): Gateway response code.
- `secureHash` (optional): Signature hash for verification.

**Responses:**
- `200 OK`: Callback processed successfully.
- `400 Bad Request`: Invalid callback data or signature.
- `404 Not Found`: Deposit not found.
- `500 Internal Server Error`: Server error.

---

### 9. Verify Callback (POST)
**[POST] /api/deposit/verify-callback**

**Description:**
Alternative POST endpoint for payment gateway callbacks. Verifies payment signature and updates deposit status.

**Request Body:**
```json
{
  "depositId": 123,
  "gatewayTransactionId": "abc123",
  "isSuccess": true,
  "responseCode": "00",
  "secureHash": "securehashvalue"
}
```

**Responses:**
- `200 OK`: Callback processed successfully.
- `400 Bad Request`: Invalid callback data or already processed.
- `404 Not Found`: Deposit not found.
- `500 Internal Server Error`: Server error.

---

## Notes
- Authentication is required for most endpoints except public ones (e.g., `payment-methods`, `callback`).
- Ensure proper role-based access control for sensitive operations.
- Use appropriate HTTP status codes for error handling.