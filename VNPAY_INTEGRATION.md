# VNPay Integration Guide

## Overview
This project integrates **VNPay Sandbox** payment gateway for processing online payments in the EV Co-Ownership system.

## Architecture

### Components Created

1. **VnPayHelper** (`EvCoOwnership.Helpers/Helpers/VnPayHelper.cs`)
   - Core helper for VNPay API integration
   - Handles HMACSHA512 signature generation
   - Request/response data management
   - Signature validation for callbacks

2. **VnPayConfig** (`EvCoOwnership.Helpers/Configuration/VnPayConfig.cs`)
   - Configuration model for VNPay settings
   - Maps to `VnPayConfig` section in appsettings.json

3. **VnPayDTOs** (`EvCoOwnership.Repositories/DTOs/PaymentDTOs/VnPayDTOs.cs`)
   - `VnPayCallbackRequest`: Receives callback from VNPay
   - `VnPayCallbackResponse`: Processed callback response

4. **IVnPayService** (`EvCoOwnership.Services/Interfaces/IVnPayService.cs`)
   - Service interface for VNPay operations

5. **VnPayService** (`EvCoOwnership.Services/Services/VnPayService.cs`)
   - Implements VNPay payment URL generation
   - Processes VNPay callbacks
   - Validates signatures
   - Maps VNPay response codes to Vietnamese messages

6. **PaymentService** (Updated)
   - Integrated with `IVnPayService`
   - Uses VNPay service when `PaymentGateway = "VNPay"`

7. **PaymentController** (Updated)
   - Added `/api/payment/vnpay-callback` endpoint
   - Processes VNPay return URL
   - Redirects to frontend after payment

## Configuration

### Step 1: Get VNPay Sandbox Credentials

1. Go to VNPay Sandbox Portal: https://sandbox.vnpayment.vn/
2. Register/Login to get:
   - **TMN Code** (Merchant Terminal Code)
   - **Hash Secret** (for HMACSHA512 signing)

### Step 2: Update Configuration Files

Update both `appsettings.json` and `appsettings.Development.json`:

```json
{
  "VnPayConfig": {
    "TmnCode": "YOUR_TMN_CODE_FROM_VNPAY",
    "HashSecret": "YOUR_HASH_SECRET_FROM_VNPAY",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://localhost:7240/api/payment/vnpay-callback",
    "Version": "2.1.0",
    "Command": "pay",
    "CurrCode": "VND",
    "Locale": "vn",
    "TimeZoneId": "SE Asia Standard Time"
  }
}
```

**Important:**
- Replace `YOUR_TMN_CODE_FROM_VNPAY` with your actual TMN Code
- Replace `YOUR_HASH_SECRET_FROM_VNPAY` with your actual Hash Secret
- Update `ReturnUrl` to match your API URL (e.g., https://yourdomain.com/api/payment/vnpay-callback)

### Step 3: Update Frontend URL

In `PaymentController.cs`, update the `GetFrontendUrl()` method:

```csharp
private string GetFrontendUrl()
{
    // TODO: Move to appsettings.json
    return "http://localhost:3000"; // Your frontend URL
}
```

Or add to appsettings.json:

```json
{
  "AppSettings": {
    "FrontendUrl": "http://localhost:3000"
  }
}
```

## Payment Flow

### 1. Create Payment
```http
POST /api/payment
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "amount": 100000,
  "paymentGateway": "VNPay",
  "fundAdditionId": 1,
  "description": "Thanh toán nạp quỹ"
}
```

**Response:**
```json
{
  "statusCode": 201,
  "message": "Payment created successfully",
  "data": {
    "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=10000000&vnp_Command=pay&...",
    "paymentId": 123,
    "transactionRef": "PAY_123_638123456789012345"
  }
}
```

### 2. Redirect User to Payment URL
Frontend redirects user to `paymentUrl` from response.

### 3. User Completes Payment
User enters card details on VNPay sandbox page.

### 4. VNPay Callback
VNPay redirects back to: `https://localhost:7240/api/payment/vnpay-callback?vnp_Amount=...&vnp_ResponseCode=00&...`

### 5. Backend Processing
- Validates VNPay signature
- Updates payment status in database
- Redirects to frontend:
  - Success: `http://localhost:3000/payment/success?paymentId=123&amount=100000`
  - Failure: `http://localhost:3000/payment/failure?message=Insufficient+balance`

## VNPay Response Codes

| Code | Meaning |
|------|---------|
| 00 | Success |
| 07 | Suspicious transaction |
| 09 | Card not registered for internet banking |
| 10 | Wrong card info 3 times |
| 11 | Payment timeout |
| 12 | Card locked |
| 13 | Wrong OTP |
| 24 | User cancelled |
| 51 | Insufficient balance |
| 65 | Transaction limit exceeded |
| 75 | Bank under maintenance |
| 79 | Wrong password too many times |

## Testing with VNPay Sandbox

### Test Cards
VNPay sandbox provides test cards:

1. **NCB (National Citizen Bank)**
   - Card Number: `9704198526191432198`
   - Cardholder: `NGUYEN VAN A`
   - Issue Date: `07/15`
   - OTP: `123456`

2. **Other banks** - Check VNPay sandbox documentation

### Test Scenarios

**Success Payment:**
1. Create payment via API
2. Open `paymentUrl` in browser
3. Select NCB bank
4. Enter test card details
5. Enter OTP: `123456`
6. Confirm payment
7. Should redirect to frontend success page

**Failed Payment:**
1. Create payment
2. Open payment URL
3. Select "Cancel" on VNPay page
4. Should redirect to frontend failure page with code `24`

## API Endpoints

### Payment Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/payment` | Create payment | Yes |
| POST | `/api/payment/process` | Process payment (manual) | Yes |
| GET | `/api/payment/{id}` | Get payment details | Yes |
| GET | `/api/payment/my` | Get my payments | Yes |
| GET | `/api/payment/all` | Get all payments (Admin) | Admin |
| POST | `/api/payment/{id}/cancel` | Cancel payment | Yes |
| GET | `/api/payment/statistics` | Payment statistics (Admin) | Admin |
| GET | `/api/payment/vnpay-callback` | VNPay callback (no auth) | No |

## Security Features

1. **HMACSHA512 Signature**
   - All requests signed with hash secret
   - All responses validated before processing

2. **Transaction Reference**
   - Unique per payment: `{paymentId}_{timestamp}`
   - Prevents duplicate processing

3. **Callback Validation**
   - Signature validation before updating database
   - Amount verification
   - Transaction status check

4. **AllowAnonymous Callback**
   - VNPay cannot send JWT token
   - Endpoint secured by signature validation instead

## Troubleshooting

### Issue: Invalid Signature
**Cause:** Wrong Hash Secret in configuration
**Solution:** Check `VnPayConfig.HashSecret` matches VNPay merchant portal

### Issue: Payment Not Updating
**Cause:** Callback URL not accessible
**Solution:** 
- Ensure API is publicly accessible
- Check firewall/ngrok if localhost
- Verify `ReturnUrl` in configuration

### Issue: Wrong Amount
**Cause:** VNPay uses cents (100 VND = 10000 cents)
**Solution:** Amount automatically multiplied by 100 in `VnPayService.CreatePaymentUrl()`

### Issue: Timezone Problems
**Cause:** VNPay requires SE Asia timezone
**Solution:** Set `TimeZoneId = "SE Asia Standard Time"` in config

## Production Deployment

### 1. Get Production Credentials
Contact VNPay to get production credentials

### 2. Update Production Config
In `appsettings.Production.json`:
```json
{
  "VnPayConfig": {
    "TmnCode": "PRODUCTION_TMN_CODE",
    "HashSecret": "PRODUCTION_HASH_SECRET",
    "BaseUrl": "https://pay.vnpay.vn/vpcpay.html",
    "ReturnUrl": "https://yourdomain.com/api/payment/vnpay-callback",
    "Version": "2.1.0",
    "Command": "pay",
    "CurrCode": "VND",
    "Locale": "vn",
    "TimeZoneId": "SE Asia Standard Time"
  }
}
```

### 3. SSL Required
VNPay requires HTTPS for production ReturnUrl

### 4. Whitelist IP
VNPay may require whitelisting your server IP

## References

- VNPay API Documentation: https://sandbox.vnpayment.vn/apis/
- VNPay Sandbox Portal: https://sandbox.vnpayment.vn/
- Test Cards: https://sandbox.vnpayment.vn/apis/vnpay-test-data.html

## Support

For issues:
1. Check logs in `/logs` directory
2. Verify VNPay credentials
3. Test with VNPay sandbox test cards
4. Contact VNPay support for gateway issues

---

## ✅ Implementation Checklist

### Core Components
- ✅ **VnPayHelper.cs** - HMACSHA512 signature generation & validation
- ✅ **VnPayConfig.cs** - Configuration model
- ✅ **VnPayDTOs.cs** - Callback request/response models (with ALL VNPay parameters)
- ✅ **IVnPayService.cs** - Service interface
- ✅ **VnPayService.cs** - Full implementation with Vietnamese error messages

### Integration
- ✅ **PaymentService.cs** - Integrated with VNPayService + IHttpContextAccessor for IP detection
- ✅ **PaymentController.cs** - Added `/api/payment/vnpay-callback` endpoint (AllowAnonymous)
- ✅ **ServiceConfigurations.cs** - Registered VNPayService in DI
- ✅ **Program.cs** - Registered IHttpContextAccessor
- ✅ **appsettings.json** - Added VnPayConfig section
- ✅ **appsettings.Development.json** - Added VnPayConfig section

### Business Logic
- ✅ **ProcessPaymentAsync** - Updates FundAddition.StatusEnum to Completed when payment succeeds
- ✅ **Signature Validation** - All VNPay callback parameters included in signature check
- ✅ **Amount Handling** - Correctly converts VND to cents (x100) and back
- ✅ **Transaction Reference** - Format: `{paymentId}_{timestamp}` for uniqueness
- ✅ **Payment Expiry** - 15 minutes expiration time

### Build Status
- ✅ **0 Errors**, 89 Warnings (only XML comments and unused variables)
- ✅ All projects build successfully

### Security
- ✅ HMACSHA512 signature on all requests
- ✅ Signature validation on all callbacks
- ✅ Amount verification in callback
- ✅ Transaction status double-check (ResponseCode + TransactionStatus)

### What Still Needs Configuration
1. **VNPay Credentials** - Replace `YOUR_TMN_CODE` and `YOUR_HASH_SECRET` in appsettings.json
2. **Frontend URL** - Update `GetFrontendUrl()` method in PaymentController.cs
3. **Return URL** - Update `VnPayConfig.ReturnUrl` to match your deployed API domain
