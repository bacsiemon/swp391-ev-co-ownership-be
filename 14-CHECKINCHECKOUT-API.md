# CheckInCheckOut API Documentation

## Overview
The CheckInCheckOut API manages vehicle check-in and check-out operations, including QR code-based self-service and staff-assisted processes. It ensures secure, efficient, and detailed handling of vehicle usage.

---

## Endpoints

### 1. QR Code Check-In
**URL:** `POST /api/checkincheckout/qr-checkin`

**Description:**
Allows co-owners to check in using a QR code. Includes optional condition reporting and location tracking.

**Request Body:**
```json
{
  "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,...}",
  "conditionReport": {
    "conditionType": 1,
    "cleanlinessLevel": 4,
    "hasDamages": false,
    "notes": "Vehicle looks good, ready to use"
  },
  "notes": "Picked up at 8:45 AM as scheduled",
  "locationLatitude": 10.762622,
  "locationLongitude": 106.660172
}
```

**Responses:**
- `200 OK`: Check-in successful.
- `400 Bad Request`: Invalid QR code or other issues.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking or vehicle station not found.

---

### 2. QR Code Check-Out
**URL:** `POST /api/checkincheckout/qr-checkout`

**Description:**
Allows co-owners to check out using a QR code. Requires a mandatory condition report and calculates fees.

**Request Body:**
```json
{
  "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,...}",
  "conditionReport": {
    "conditionType": 2,
    "cleanlinessLevel": 3,
    "hasDamages": true,
    "damages": [
      {
        "damageType": "Scratch",
        "severity": 0,
        "location": "Front bumper left side",
        "description": "Minor scratch, about 5cm long",
        "photoIds": [101, 102],
        "estimatedCost": 500000
      }
    ],
    "notes": "Small scratch on front bumper - parking incident"
  },
  "odometerReading": 45250,
  "batteryLevel": 65,
  "notes": "Minor damage reported and documented"
}
```

**Responses:**
- `200 OK`: Check-out successful.
- `400 Bad Request`: Invalid QR code or other issues.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking or vehicle station not found.

---

### 3. Generate QR Code
**URL:** `GET /api/checkincheckout/generate-qr/{bookingId}`

**Description:**
Generates a QR code for a confirmed booking.

**Responses:**
- `200 OK`: QR code generated successfully.
- `400 Bad Request`: Booking not confirmed.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking or vehicle station not found.

---

### 4. Manual Check-In
**URL:** `POST /api/checkincheckout/manual-checkin`

**Description:**
Staff-assisted check-in with detailed condition reporting.

**Request Body:**
```json
{
  "bookingId": 42,
  "vehicleStationId": 1,
  "conditionReport": {
    "conditionType": 1,
    "cleanlinessLevel": 5,
    "hasDamages": false,
    "notes": "Vehicle inspected - excellent condition"
  },
  "odometerReading": 45165,
  "batteryLevel": 100,
  "staffNotes": "Assisted customer with vehicle familiarization. Explained charging procedure.",
  "conditionPhotoIds": [201, 202, 203, 204]
}
```

**Responses:**
- `200 OK`: Manual check-in successful.
- `400 Bad Request`: Booking not confirmed or already checked in.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking or vehicle station not found.

---

### 5. Manual Check-Out
**URL:** `POST /api/checkincheckout/manual-checkout`

**Description:**
Staff-assisted check-out with damage assessment and fee calculation.

**Request Body:**
```json
{
  "bookingId": 42,
  "vehicleStationId": 1,
  "conditionReport": {
    "conditionType": 3,
    "cleanlinessLevel": 2,
    "hasDamages": true,
    "damages": [
      {
        "damageType": "Dent",
        "severity": 1,
        "location": "Rear bumper center",
        "description": "Moderate dent, approximately 10cm diameter",
        "photoIds": [301, 302, 303],
        "estimatedCost": 1500000
      }
    ],
    "notes": "Multiple damages found during inspection. Co-owner acknowledged."
  },
  "odometerReading": 45320,
  "batteryLevel": 45,
  "staffNotes": "Vehicle returned 2 hours late. Damages documented with co-owner present."
}
```

**Responses:**
- `200 OK`: Manual check-out successful.
- `400 Bad Request`: Not checked in or already checked out.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking or vehicle station not found.

---

### 6. Validate Check-In Eligibility
**URL:** `GET /api/checkincheckout/validate-checkin/{bookingId}`

**Description:**
Validates if a booking is eligible for check-in.

**Responses:**
- `200 OK`: Validation completed.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking not found.

---

### 7. Validate Check-Out Eligibility
**URL:** `GET /api/checkincheckout/validate-checkout/{bookingId}`

**Description:**
Validates if a booking is eligible for check-out.

**Responses:**
- `200 OK`: Validation completed.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking not found.

---

### 8. Get Check-In/Check-Out History
**URL:** `GET /api/checkincheckout/history/{bookingId}`

**Description:**
Retrieves the history of check-in and check-out for a booking.

**Responses:**
- `200 OK`: History retrieved successfully.
- `403 Forbidden`: Access denied.
- `404 Not Found`: Booking not found.

---

## Notes
- Ensure proper role-based access control for all endpoints.
- Validate all request data to prevent unauthorized access or invalid operations.
- Use the provided sample requests and responses as a reference for integration.