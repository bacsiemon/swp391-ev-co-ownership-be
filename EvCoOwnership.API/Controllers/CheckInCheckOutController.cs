using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.CheckInCheckOutDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for vehicle check-in/check-out operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles]
    public class CheckInCheckOutController : ControllerBase
    {
        private readonly ICheckInCheckOutService _checkInCheckOutService;

        public CheckInCheckOutController(ICheckInCheckOutService checkInCheckOutService)
        {
            _checkInCheckOutService = checkInCheckOutService;
        }

        #region QR Code Check-In/Out (CoOwner Self-Service)

        /// <summary>
        /// QR scan check-in - CoOwner confirms vehicle pickup using QR code
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (must be the booking owner)
        /// 
        /// **QR Code Self-Service Pickup:**
        /// 
        /// Allows co-owners to pick up their vehicle by scanning a QR code generated for their confirmed booking:
        /// 
        /// **Key Features:**
        /// - **Self-Service**: No staff required, fully automated check-in
        /// - **QR Code Validation**: Verifies QR code authenticity and expiry (24 hours)
        /// - **Timing Validation**: Can check-in up to 30 minutes before booking start time
        /// - **Condition Reporting**: Optional vehicle condition report at pickup
        /// - **Location Tracking**: Optional GPS coordinates for check-in location
        /// - **Automatic Status Update**: Booking status changes from `Confirmed` → `Active`
        /// 
        /// **How It Works:**
        /// 1. Co-owner scans QR code at vehicle station
        /// 2. System validates QR code data (booking ID, vehicle, expiry)
        /// 3. System verifies co-owner ownership and booking status
        /// 4. System checks timing (30 min before start time allowed)
        /// 5. Co-owner reports vehicle condition (optional)
        /// 6. System creates CheckIn record and updates booking to `Active`
        /// 7. Co-owner receives confirmation with pickup details
        /// 
        /// **QR Code Format:**
        /// The QR code contains JSON data with:
        /// - `BookingId`: Unique booking identifier
        /// - `VehicleId` and `LicensePlate`: Vehicle identification
        /// - `CoOwnerId` and `CoOwnerName`: Owner identification
        /// - `BookingStartTime` and `BookingEndTime`: Time window
        /// - `VehicleStationId`: Pickup location
        /// - `GeneratedAt`: QR generation timestamp
        /// - `QRCodeHash`: Security hash for verification
        /// 
        /// **Check-In Timing Rules:**
        /// - ✅ **Can check-in**: Up to 30 minutes before booking start time
        /// - ❌ **Too early**: More than 30 minutes before start time
        /// - ✅ **On time**: During booking time window
        /// - ⚠️ **Late**: After booking start time (allowed but may incur fees)
        /// 
        /// **Vehicle Condition Types:**
        /// - `Excellent` (0): Like new, pristine condition
        /// - `Good` (1): Normal condition, minor wear
        /// - `Fair` (2): Some visible issues, still functional
        /// - `Poor` (3): Significant issues, requires attention
        /// - `Damaged` (4): Damaged, needs immediate repair
        /// 
        /// **Sample Request (Minimal):**
        /// ```json
        /// {
        ///   "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,\"vehicleLicensePlate\":\"29A-12345\",\"coOwnerId\":10,\"coOwnerName\":\"John Doe\",\"bookingStartTime\":\"2025-10-24T09:00:00Z\",\"bookingEndTime\":\"2025-10-24T12:00:00Z\",\"vehicleStationId\":1,\"vehicleStationName\":\"Central Station\",\"generatedAt\":\"2025-10-23T15:00:00Z\",\"qrCodeHash\":\"abc123xyz\"}"
        /// }
        /// ```
        /// 
        /// **Sample Request (Full with Condition Report):**
        /// ```json
        /// {
        ///   "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,\"vehicleLicensePlate\":\"29A-12345\",\"coOwnerId\":10,\"coOwnerName\":\"John Doe\",\"bookingStartTime\":\"2025-10-24T09:00:00Z\",\"bookingEndTime\":\"2025-10-24T12:00:00Z\",\"vehicleStationId\":1,\"vehicleStationName\":\"Central Station\",\"generatedAt\":\"2025-10-23T15:00:00Z\",\"qrCodeHash\":\"abc123xyz\"}",
        ///   "conditionReport": {
        ///     "conditionType": 1,
        ///     "cleanlinessLevel": 4,
        ///     "hasDamages": false,
        ///     "notes": "Vehicle looks good, ready to use"
        ///   },
        ///   "notes": "Picked up at 8:45 AM as scheduled",
        ///   "locationLatitude": 10.762622,
        ///   "locationLongitude": 106.660172
        /// }
        /// ```
        /// 
        /// **Sample Response (Success):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CHECK_IN_SUCCESS",
        ///   "data": {
        ///     "checkInId": 123,
        ///     "bookingId": 42,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "29A-12345",
        ///     "coOwnerName": "John Doe",
        ///     "vehicleStationName": "Central Station",
        ///     "vehicleStationAddress": "123 Main St, District 1",
        ///     "checkInTime": "2025-10-24T08:45:00Z",
        ///     "vehicleCondition": 1,
        ///     "wasQRScanned": true,
        ///     "status": 0,
        ///     "statusMessage": "Vehicle picked up successfully via QR scan"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">QR scan check-in request with QR code data</param>
        /// <response code="200">Check-in successful - vehicle picked up</response>
        /// <response code="400">Bad request - invalid QR code, too early, already checked in, or QR expired</response>
        /// <response code="403">Access denied - not the booking owner</response>
        /// <response code="404">Booking or vehicle station not found</response>
        [HttpPost("qr-checkin")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> QRScanCheckIn([FromBody] QRScanCheckInRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.QRScanCheckInAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// QR scan check-out - CoOwner confirms vehicle return using QR code
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (must be the booking owner)
        /// 
        /// **QR Code Self-Service Return:**
        /// 
        /// Allows co-owners to return their vehicle by scanning a QR code, with comprehensive condition reporting and automatic fee calculation:
        /// 
        /// **Key Features:**
        /// - **Self-Service Return**: No staff required, fully automated check-out
        /// - **Mandatory Condition Report**: Required vehicle condition inspection at return
        /// - **Damage Documentation**: Photo evidence and detailed damage reports
        /// - **Automatic Fee Calculation**: Late fees, damage charges calculated automatically
        /// - **Usage Summary**: Complete trip statistics (duration, distance, battery usage)
        /// - **Status Completion**: Booking status changes from `Active` → `Completed`
        /// 
        /// **How It Works:**
        /// 1. Co-owner returns vehicle to station and scans QR code
        /// 2. System verifies QR code and check-in status
        /// 3. Co-owner MUST report vehicle condition (required)
        /// 4. Co-owner optionally uploads damage photos
        /// 5. System calculates usage duration and late fees (if any)
        /// 6. System creates CheckOut record with all data
        /// 7. System updates booking to `Completed` with total cost
        /// 8. Co-owner receives detailed usage summary
        /// 
        /// **Late Fee Calculation:**
        /// - Grace period: 15 minutes after booking end time
        /// - After grace period: 50,000 VND per hour (rounded up)
        /// - Example: 1 hour 10 minutes late = 100,000 VND (2 hours charged)
        /// 
        /// **Damage Charge Calculation:**
        /// - Based on damage reports provided
        /// - Each damage has `EstimatedCost` field
        /// - Total damage charges = Sum of all estimated costs
        /// - Added to booking total cost
        /// 
        /// **Check-Out Statuses:**
        /// - `Success` (0): Normal return, no issues
        /// - `SuccessWithDamages` (1): Returned with damages - charges apply
        /// - `SuccessWithLateFee` (2): Returned late - late fee charged
        /// - `Failed` (3): Check-out failed
        /// - `PendingDamageInspection` (4): Requires staff verification
        /// - `AlreadyCheckedOut` (5): Already completed
        /// 
        /// **Damage Severity Types:**
        /// - `Minor` (0): Small scratches, cosmetic issues
        /// - `Moderate` (1): Noticeable damage, functional impact
        /// - `Severe` (2): Significant damage, major repair needed
        /// 
        /// **Sample Request (No Damages):**
        /// ```json
        /// {
        ///   "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,...}",
        ///   "conditionReport": {
        ///     "conditionType": 1,
        ///     "cleanlinessLevel": 4,
        ///     "hasDamages": false,
        ///     "notes": "Vehicle returned in good condition"
        ///   },
        ///   "odometerReading": 45230,
        ///   "batteryLevel": 85,
        ///   "notes": "Returned on time, no issues",
        ///   "locationLatitude": 10.762622,
        ///   "locationLongitude": 106.660172
        /// }
        /// ```
        /// 
        /// **Sample Request (With Damages):**
        /// ```json
        /// {
        ///   "qrCodeData": "{\"bookingId\":42,\"vehicleId\":5,...}",
        ///   "conditionReport": {
        ///     "conditionType": 2,
        ///     "cleanlinessLevel": 3,
        ///     "hasDamages": true,
        ///     "damages": [
        ///       {
        ///         "damageType": "Scratch",
        ///         "severity": 0,
        ///         "location": "Front bumper left side",
        ///         "description": "Minor scratch, about 5cm long",
        ///         "photoIds": [101, 102],
        ///         "estimatedCost": 500000
        ///       }
        ///     ],
        ///     "notes": "Small scratch on front bumper - parking incident"
        ///   },
        ///   "odometerReading": 45250,
        ///   "batteryLevel": 65,
        ///   "conditionPhotoIds": [101, 102, 103],
        ///   "notes": "Minor damage reported and documented"
        /// }
        /// ```
        /// 
        /// **Sample Response (Success with Late Fee):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CHECK_OUT_SUCCESS",
        ///   "data": {
        ///     "checkOutId": 456,
        ///     "bookingId": 42,
        ///     "vehicleName": "Tesla Model 3",
        ///     "checkOutTime": "2025-10-24T13:45:00Z",
        ///     "vehicleCondition": 1,
        ///     "wasQRScanned": true,
        ///     "odometerReading": 45230,
        ///     "batteryLevel": 85,
        ///     "hasNewDamages": false,
        ///     "status": 2,
        ///     "statusMessage": "Vehicle returned late - late fee: 50,000 VND",
        ///     "usageSummary": {
        ///       "checkInTime": "2025-10-24T09:00:00Z",
        ///       "checkOutTime": "2025-10-24T13:45:00Z",
        ///       "totalDuration": "04:45:00",
        ///       "totalHours": 5,
        ///       "distanceTraveled": 65,
        ///       "batteryUsed": 15,
        ///       "bookingCost": 300000,
        ///       "lateFee": 50000,
        ///       "totalCost": 350000
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">QR scan check-out request with condition report</param>
        /// <response code="200">Check-out successful - vehicle returned (may include late fees or damage charges)</response>
        /// <response code="400">Bad request - invalid QR code, not checked in, or already checked out</response>
        /// <response code="403">Access denied - not the booking owner</response>
        /// <response code="404">Booking or vehicle station not found</response>
        [HttpPost("qr-checkout")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> QRScanCheckOut([FromBody] QRScanCheckOutRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.QRScanCheckOutAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Generates QR code data for a confirmed booking
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (booking owner) or Staff/Admin
        /// 
        /// **QR Code Generation:**
        /// 
        /// Generates a secure QR code containing booking and vehicle information for self-service check-in/check-out:
        /// 
        /// **Key Features:**
        /// - **24-Hour Validity**: QR code expires 24 hours after generation
        /// - **Security Hash**: Includes SHA256 hash for verification
        /// - **Complete Info**: Contains all necessary booking and vehicle details
        /// - **Station Location**: Includes pickup/return station information
        /// 
        /// **Generation Rules:**
        /// - Only for `Confirmed` bookings
        /// - Can be regenerated multiple times
        /// - Each generation creates new hash and timestamp
        /// 
        /// **Use Cases:**
        /// 1. Co-owner generates QR before pickup
        /// 2. Staff generates QR for co-owner at station
        /// 3. App displays QR for scanning at station kiosk
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "QR_CODE_GENERATED",
        ///   "data": {
        ///     "bookingId": 42,
        ///     "vehicleId": 5,
        ///     "vehicleLicensePlate": "29A-12345",
        ///     "coOwnerId": 10,
        ///     "coOwnerName": "John Doe",
        ///     "bookingStartTime": "2025-10-24T09:00:00Z",
        ///     "bookingEndTime": "2025-10-24T12:00:00Z",
        ///     "vehicleStationId": 1,
        ///     "vehicleStationName": "Central Station",
        ///     "generatedAt": "2025-10-23T15:00:00Z",
        ///     "qrCodeHash": "a1b2c3d4e5f6..."
        ///   }
        /// }
        /// ```
        /// 
        /// **Frontend Usage:**
        /// ```javascript
        /// // Convert to QR code image
        /// const qrData = JSON.stringify(response.data);
        /// QRCode.toDataURL(qrData, (err, url) => {
        ///   // Display QR code image
        ///   document.getElementById('qr-image').src = url;
        /// });
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to generate QR code for</param>
        /// <response code="200">QR code generated successfully</response>
        /// <response code="400">Booking not confirmed</response>
        /// <response code="403">Access denied - not booking owner or staff</response>
        /// <response code="404">Booking or vehicle station not found</response>
        [HttpGet("generate-qr/{bookingId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GenerateBookingQRCode(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.GenerateBookingQRCodeAsync(bookingId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion

        #region Manual Check-In/Out (Staff Verification)

        /// <summary>
        /// Manual check-in - Staff verifies and confirms vehicle pickup
        /// </summary>
        /// <remarks>
        /// **Role requirement:** Staff or Admin
        /// 
        /// **Manual Staff-Assisted Pickup:**
        /// 
        /// Allows staff to manually process vehicle pickup when QR code scanning is not available or additional verification is needed:
        /// 
        /// **Key Features:**
        /// - **Staff Verification**: Manual inspection and documentation by staff
        /// - **Detailed Condition Report**: Required comprehensive vehicle condition check
        /// - **Photo Documentation**: Photos of vehicle condition at pickup
        /// - **Override Check-In Time**: Optional time override for special cases
        /// - **Staff Notes**: Staff observations and comments
        /// 
        /// **When to Use Manual Check-In:**
        /// - QR code scanning not working/available
        /// - Special assistance needed by co-owner
        /// - Pre-damage documentation required
        /// - First-time user needs guidance
        /// - VIP customer service
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "bookingId": 42,
        ///   "vehicleStationId": 1,
        ///   "conditionReport": {
        ///     "conditionType": 1,
        ///     "cleanlinessLevel": 5,
        ///     "hasDamages": false,
        ///     "notes": "Vehicle inspected - excellent condition"
        ///   },
        ///   "odometerReading": 45165,
        ///   "batteryLevel": 100,
        ///   "staffNotes": "Assisted customer with vehicle familiarization. Explained charging procedure.",
        ///   "conditionPhotoIds": [201, 202, 203, 204]
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Manual check-in request</param>
        /// <response code="200">Manual check-in successful</response>
        /// <response code="400">Booking not confirmed or already checked in</response>
        /// <response code="403">Access denied - staff/admin only</response>
        /// <response code="404">Booking or vehicle station not found</response>
        [HttpPost("manual-checkin")]
        [AuthorizeRoles(EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> ManualCheckIn([FromBody] ManualCheckInRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var staffId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.ManualCheckInAsync(staffId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Manual check-out - Staff verifies and confirms vehicle return
        /// </summary>
        /// <remarks>
        /// **Role requirement:** Staff or Admin
        /// 
        /// **Manual Staff-Assisted Return:**
        /// 
        /// Allows staff to manually process vehicle return with comprehensive inspection and damage assessment:
        /// 
        /// **Key Features:**
        /// - **Thorough Inspection**: Staff conducts detailed vehicle inspection
        /// - **Damage Assessment**: Staff identifies and documents all damages
        /// - **Photo Evidence**: Required photos of any damages found
        /// - **Cost Estimation**: Staff estimates repair costs for damages
        /// - **Override Check-Out Time**: Optional time override for special cases
        /// - **Detailed Documentation**: Staff notes and observations
        /// 
        /// **When to Use Manual Check-Out:**
        /// - QR code scanning not working/available
        /// - Damages require professional assessment
        /// - Dispute resolution needed
        /// - Special circumstances (emergency, etc.)
        /// - Quality control inspection
        /// 
        /// **Damage Documentation Process:**
        /// 1. Staff inspects vehicle thoroughly
        /// 2. Photographs all damages (existing + new)
        /// 3. Documents each damage with:
        ///    - Type (scratch, dent, etc.)
        ///    - Severity (minor/moderate/severe)
        ///    - Location on vehicle
        ///    - Estimated repair cost
        /// 4. System calculates total damage charges
        /// 5. Co-owner notified of charges
        /// 
        /// **Sample Request (With Damages):**
        /// ```json
        /// {
        ///   "bookingId": 42,
        ///   "vehicleStationId": 1,
        ///   "conditionReport": {
        ///     "conditionType": 3,
        ///     "cleanlinessLevel": 2,
        ///     "hasDamages": true,
        ///     "damages": [
        ///       {
        ///         "damageType": "Dent",
        ///         "severity": 1,
        ///         "location": "Rear bumper center",
        ///         "description": "Moderate dent, approximately 10cm diameter. Likely parking collision.",
        ///         "photoIds": [301, 302, 303],
        ///         "estimatedCost": 1500000
        ///       },
        ///       {
        ///         "damageType": "Scratch",
        ///         "severity": 0,
        ///         "location": "Driver door",
        ///         "description": "Surface scratch, 8cm long",
        ///         "photoIds": [304],
        ///         "estimatedCost": 300000
        ///       }
        ///     ],
        ///     "notes": "Multiple damages found during inspection. Co-owner acknowledged."
        ///   },
        ///   "odometerReading": 45320,
        ///   "batteryLevel": 45,
        ///   "staffNotes": "Vehicle returned 2 hours late. Damages documented with co-owner present. Estimated total repair: 1,800,000 VND. Co-owner signed acknowledgment form.",
        ///   "conditionPhotoIds": [301, 302, 303, 304, 305],
        ///   "damagesFound": [
        ///     {
        ///       "damageType": "Dent",
        ///       "severity": 1,
        ///       "location": "Rear bumper center",
        ///       "description": "Moderate dent, approximately 10cm diameter",
        ///       "photoIds": [301, 302, 303],
        ///       "estimatedCost": 1500000
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Manual check-out request</param>
        /// <response code="200">Manual check-out successful (includes damage charges if any)</response>
        /// <response code="400">Not checked in or already checked out</response>
        /// <response code="403">Access denied - staff/admin only</response>
        /// <response code="404">Booking or vehicle station not found</response>
        [HttpPost("manual-checkout")]
        [AuthorizeRoles(EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> ManualCheckOut([FromBody] ManualCheckOutRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var staffId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.ManualCheckOutAsync(staffId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion

        #region Validation & Utilities

        /// <summary>
        /// Validates if booking is ready for check-in
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (booking owner)
        /// 
        /// Pre-validates check-in eligibility before attempting actual check-in.
        /// 
        /// **Checks:**
        /// - Booking is confirmed
        /// - Not already checked in
        /// - Within check-in time window (30 min before start)
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CHECK_IN_ELIGIBLE",
        ///   "data": {
        ///     "isEligible": true,
        ///     "errors": [],
        ///     "bookingStartTime": "2025-10-24T09:00:00Z",
        ///     "earliestCheckInTime": "2025-10-24T08:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to validate</param>
        /// <response code="200">Validation completed</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("validate-checkin/{bookingId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> ValidateCheckInEligibility(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.ValidateCheckInEligibilityAsync(bookingId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Validates if booking is ready for check-out
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (booking owner)
        /// 
        /// Pre-validates check-out eligibility before attempting actual check-out.
        /// 
        /// **Checks:**
        /// - Booking is checked in
        /// - Not already checked out
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CHECK_OUT_ELIGIBLE",
        ///   "data": {
        ///     "isEligible": true,
        ///     "errors": [],
        ///     "bookingEndTime": "2025-10-24T12:00:00Z",
        ///     "checkInTime": "2025-10-24T08:55:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to validate</param>
        /// <response code="200">Validation completed</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("validate-checkout/{bookingId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> ValidateCheckOutEligibility(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.ValidateCheckOutEligibilityAsync(bookingId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets check-in/check-out history for a booking
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (booking owner)
        /// 
        /// Retrieves complete check-in/check-out history for a booking.
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "HISTORY_RETRIEVED",
        ///   "data": {
        ///     "bookingId": 42,
        ///     "checkIns": [
        ///       {
        ///         "checkInId": 123,
        ///         "checkTime": "2025-10-24T08:55:00Z",
        ///         "stationName": "Central Station",
        ///         "stationAddress": "123 Main St",
        ///         "staffName": "Self-service",
        ///         "wasManual": false
        ///       }
        ///     ],
        ///     "checkOuts": [
        ///       {
        ///         "checkOutId": 456,
        ///         "checkTime": "2025-10-24T12:30:00Z",
        ///         "stationName": "Central Station",
        ///         "stationAddress": "123 Main St",
        ///         "staffName": "John Staff",
        ///         "wasManual": true
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID</param>
        /// <response code="200">History retrieved successfully</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("history/{bookingId:int}")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetBookingCheckInOutHistory(int bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _checkInCheckOutService.GetBookingCheckInOutHistoryAsync(bookingId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion
    }
}
