using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for booking management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        /// <summary>
        /// Initializes a new instance of the BookingController
        /// </summary>
        /// <param name="bookingService">Booking service</param>
        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Creates a new booking
        /// </summary>
        /// <param name="request">Create booking request</param>
        /// <response code="201">Booking created successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="409">Booking time conflict</response>
        [HttpPost]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.CreateBookingAsync(userId, request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                409 => Conflict(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <response code="200">Booking retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetBookingByIdAsync(id, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets current user's bookings
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Bookings retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        [HttpGet("my-bookings")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetMyBookings(int pageIndex = 1, int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetUserBookingsAsync(userId, pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets bookings for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Bookings retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        [HttpGet("vehicle/{vehicleId:int}")]
        public async Task<IActionResult> GetVehicleBookings(int vehicleId, int pageIndex = 1, int pageSize = 10)
        {
            var response = await _bookingService.GetVehicleBookingsAsync(vehicleId, pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets all bookings (Admin/Staff only)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Bookings retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetAllBookings(int pageIndex = 1, int pageSize = 10)
        {
            var response = await _bookingService.GetAllBookingsAsync(pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Updates a booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="request">Update booking request</param>
        /// <response code="200">Booking updated successfully</response>
        /// <response code="400">Validation error or booking cannot be updated</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] UpdateBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.UpdateBookingAsync(id, userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Approves or rejects a booking (Admin/Staff only)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="request">Approve booking request</param>
        /// <response code="200">Booking processed successfully</response>
        /// <response code="400">Booking already processed</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("{id:int}/approve")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> ApproveBooking(int id, [FromBody] ApproveBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.ApproveBookingAsync(id, userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Cancels a booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <response code="200">Booking cancelled successfully</response>
        /// <response code="400">Cannot cancel completed booking</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.CancelBookingAsync(id, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Deletes a booking (Admin only)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <response code="200">Booking deleted successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="404">Booking not found</response>
        [HttpDelete("{id:int}")]
        [AuthorizeRoles(EUserRole.Admin)]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var response = await _bookingService.DeleteBookingAsync(id);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets booking statistics (Admin/Staff only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _bookingService.GetBookingStatisticsAsync();
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets booking calendar for a date range
        /// </summary>
        /// <param name="startDate">Start date of calendar view (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date of calendar view (format: yyyy-MM-dd)</param>
        /// <param name="vehicleId">Optional: Filter by specific vehicle</param>
        /// <param name="status">Optional: Filter by status (Pending, Confirmed, Active, Completed, Cancelled)</param>
        /// <response code="200">Calendar retrieved successfully</response>
        /// <response code="400">Invalid date range or status filter</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">User not found</response>
        /// <remarks>
        /// **BOOKING CALENDAR - Role-Based Access**
        /// 
        /// **Access Control by Role:**
        /// - **Co-owner**: Can see bookings for vehicles in their co-ownership groups only
        /// - **Staff/Admin**: Can see ALL bookings in the system
        /// 
        /// **Purpose:**
        /// This endpoint provides a shared calendar view of all bookings within a date range,
        /// helping users coordinate vehicle usage and avoid scheduling conflicts.
        /// 
        /// **Use Cases:**
        /// - View bookings for the next week/month
        /// - Check when specific vehicles are available
        /// - See who has booked vehicles and when
        /// - Identify your own bookings vs others
        /// - Filter by vehicle or booking status
        /// 
        /// **Date Range:**
        /// - Maximum: 90 days
        /// - Recommended: 7-30 days for typical calendar views
        /// 
        /// **Response Includes:**
        /// - All booking events in the date range
        /// - Vehicle details (name, brand, model, license plate)
        /// - Co-owner information (who booked it)
        /// - Booking duration in hours
        /// - Status indicators
        /// - Summary statistics (total bookings, status breakdown, your bookings)
        /// 
        /// **Example Requests:**
        /// 
        /// **1. View next 7 days:**
        /// GET /api/booking/calendar?startDate=2025-01-17&amp;endDate=2025-01-24
        /// 
        /// **2. View specific vehicle for next month:**
        /// GET /api/booking/calendar?startDate=2025-01-17&amp;endDate=2025-02-17&amp;vehicleId=5
        /// 
        /// **3. View only confirmed bookings:**
        /// GET /api/booking/calendar?startDate=2025-01-17&amp;endDate=2025-01-24&amp;status=Confirmed
        /// 
        /// **4. View pending approvals (Staff/Admin):**
        /// GET /api/booking/calendar?startDate=2025-01-17&amp;endDate=2025-01-24&amp;status=Pending
        /// </remarks>
        [HttpGet("calendar")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> GetBookingCalendar(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? vehicleId = null,
            [FromQuery] string? status = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetBookingCalendarAsync(userId, startDate, endDate, vehicleId, status);
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
        /// Checks if a vehicle is available for a specific time slot
        /// </summary>
        /// <param name="vehicleId">Vehicle ID to check</param>
        /// <param name="startTime">Start time of desired booking (format: yyyy-MM-ddTHH:mm:ss)</param>
        /// <param name="endTime">End time of desired booking (format: yyyy-MM-ddTHH:mm:ss)</param>
        /// <response code="200">Availability check completed successfully</response>
        /// <response code="400">Invalid time range</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Vehicle not found</response>
        /// <remarks>
        /// **VEHICLE AVAILABILITY CHECK**
        /// 
        /// **Purpose:**
        /// Check if a vehicle is available for booking during a specific time period.
        /// This helps users know if they can book a vehicle before actually creating the booking.
        /// 
        /// **Use Cases:**
        /// - Pre-validate booking availability before creating a booking
        /// - Check for time conflicts with existing bookings
        /// - See what other bookings overlap with desired time slot
        /// - Plan alternative time slots if vehicle is busy
        /// 
        /// **Response:**
        /// - `isAvailable: true` - Vehicle is free, you can create booking
        /// - `isAvailable: false` - Vehicle has conflicting bookings
        /// - `conflictingBookings` - List of overlapping bookings (if any)
        /// 
        /// **Conflict Detection:**
        /// A booking conflicts if it overlaps with existing non-cancelled bookings:
        /// - New booking starts during an existing booking
        /// - New booking ends during an existing booking
        /// - New booking completely contains an existing booking
        /// 
        /// **Example Requests:**
        /// 
        /// **1. Check availability for tomorrow 9 AM - 5 PM:**
        /// GET /api/booking/availability?vehicleId=5&amp;startTime=2025-01-18T09:00:00&amp;endTime=2025-01-18T17:00:00
        /// 
        /// **2. Check weekend availability:**
        /// GET /api/booking/availability?vehicleId=5&amp;startTime=2025-01-20T08:00:00&amp;endTime=2025-01-20T18:00:00
        /// 
        /// **Response Example (Available):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "VEHICLE_AVAILABLE",
        ///   "data": {
        ///     "vehicleId": 5,
        ///     "vehicleName": "VinFast VF8",
        ///     "isAvailable": true,
        ///     "message": "VEHICLE_AVAILABLE",
        ///     "conflictingBookings": null
        ///   }
        /// }
        /// ```
        /// 
        /// **Response Example (Not Available):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
        ///   "data": {
        ///     "vehicleId": 5,
        ///     "vehicleName": "VinFast VF8",
        ///     "isAvailable": false,
        ///     "message": "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT",
        ///     "conflictingBookings": [
        ///       {
        ///         "bookingId": 123,
        ///         "coOwnerName": "Nguyen Van A",
        ///         "startTime": "2025-01-18T10:00:00",
        ///         "endTime": "2025-01-18T15:00:00",
        ///         "status": "Confirmed"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpGet("availability")]
        [AuthorizeRoles(EUserRole.CoOwner, EUserRole.Staff, EUserRole.Admin)]
        public async Task<IActionResult> CheckVehicleAvailability(
            [FromQuery] int vehicleId,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            var response = await _bookingService.CheckVehicleAvailabilityAsync(vehicleId, startTime, endTime);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #region Booking Slot Request Endpoints

        /// <summary>
        /// Request a booking slot with intelligent conflict detection and alternatives (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **This feature allows co-owners to request time slots with smart handling:**
        /// 
        /// **Key Features:**
        /// - **Auto-confirmation**: If slot is available and no conflicts, booking is confirmed automatically
        /// - **Conflict detection**: Checks for overlapping bookings with other co-owners
        /// - **Alternative suggestions**: System generates alternative time slots if preferred slot has conflicts
        /// - **Flexible booking**: Option to provide your own alternative slots
        /// - **Priority levels**: Low, Medium, High, Urgent
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "preferredStartTime": "2025-01-25T09:00:00",
        ///   "preferredEndTime": "2025-01-25T17:00:00",
        ///   "purpose": "Business trip to downtown",
        ///   "priority": 2,
        ///   "isFlexible": true,
        ///   "autoConfirmIfAvailable": true,
        ///   "estimatedDistance": 150,
        ///   "usageType": 0,
        ///   "alternativeSlots": [
        ///     {
        ///       "startTime": "2025-01-25T10:00:00",
        ///       "endTime": "2025-01-25T18:00:00",
        ///       "preferenceRank": 1
        ///     },
        ///     {
        ///       "startTime": "2025-01-26T09:00:00",
        ///       "endTime": "2025-01-26T17:00:00",
        ///       "preferenceRank": 2
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **Response (Auto-Confirmed):**
        /// ```json
        /// {
        ///   "statusCode": 201,
        ///   "message": "BOOKING_SLOT_AUTO_CONFIRMED",
        ///   "data": {
        ///     "requestId": 123,
        ///     "bookingId": 123,
        ///     "status": 1,
        ///     "availabilityStatus": 0,
        ///     "autoConfirmationMessage": "Slot was automatically confirmed as it's available with no conflicts",
        ///     "conflictingBookings": null,
        ///     "alternativeSuggestions": null,
        ///     "metadata": {
        ///       "requiresCoOwnerApproval": false,
        ///       "systemRecommendation": "Your preferred slot is available and can be confirmed"
        ///     }
        ///   }
        /// }
        /// ```
        /// 
        /// **Response (Has Conflicts):**
        /// ```json
        /// {
        ///   "statusCode": 201,
        ///   "message": "BOOKING_SLOT_REQUEST_CREATED",
        ///   "data": {
        ///     "requestId": 124,
        ///     "bookingId": 124,
        ///     "status": 0,
        ///     "availabilityStatus": 3,
        ///     "conflictingBookings": [
        ///       {
        ///         "bookingId": 120,
        ///         "coOwnerName": "John Smith",
        ///         "startTime": "2025-01-25T14:00:00",
        ///         "endTime": "2025-01-25T19:00:00",
        ///         "status": 1,
        ///         "overlapHours": 3.0
        ///       }
        ///     ],
        ///     "alternativeSuggestions": [
        ///       {
        ///         "startTime": "2025-01-25T06:00:00",
        ///         "endTime": "2025-01-25T14:00:00",
        ///         "isAvailable": true,
        ///         "reason": "Earlier the same day",
        ///         "recommendationScore": 70
        ///       }
        ///     ],
        ///     "metadata": {
        ///       "requiresCoOwnerApproval": true,
        ///       "approvalPendingFrom": ["John Smith"],
        ///       "systemRecommendation": "Your slot conflicts with 1 booking(s). Co-owner approval required."
        ///     }
        ///   }
        /// }
        /// ```
        /// 
        /// **Priority Levels:**
        /// - 0: Low - Regular personal use
        /// - 1: Medium - Standard commute/errands
        /// - 2: High - Important appointments
        /// - 3: Urgent - Emergency situations
        /// 
        /// **SlotRequestStatus:**
        /// - 0: Pending - Awaiting approval
        /// - 1: AutoConfirmed - Automatically confirmed (no conflicts)
        /// - 2: Approved - Manually approved
        /// - 3: Rejected - Rejected by co-owner
        /// 
        /// **AvailabilityStatus:**
        /// - 0: Available - Fully available
        /// - 1: PartiallyAvailable - Some overlap
        /// - 2: Unavailable - Fully booked
        /// - 3: RequiresApproval - Available but needs approval
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to book</param>
        /// <param name="request">Booking slot request details</param>
        /// <response code="201">Booking slot request created (or auto-confirmed)</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpPost("vehicle/{vehicleId:int}/request-slot")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> RequestBookingSlot(
            int vehicleId,
            [FromBody] RequestBookingSlotRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.RequestBookingSlotAsync(vehicleId, userId, request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Respond to a pending booking slot request (Approve/Reject) (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Allows co-owners to approve or reject pending booking slot requests from other co-owners.**
        /// 
        /// **Use Cases:**
        /// - Approve a pending request if you don't need the vehicle at that time
        /// - Reject with reason and optionally suggest alternative time
        /// - Coordinate vehicle usage among co-owners
        /// 
        /// **Request Body (Approve):**
        /// ```json
        /// {
        ///   "isApproved": true,
        ///   "notes": "Approved - have a safe trip!"
        /// }
        /// ```
        /// 
        /// **Request Body (Reject with Alternative):**
        /// ```json
        /// {
        ///   "isApproved": false,
        ///   "rejectionReason": "I need the vehicle that day for medical appointment",
        ///   "suggestedStartTime": "2025-01-26T09:00:00",
        ///   "suggestedEndTime": "2025-01-26T17:00:00",
        ///   "notes": "Can you use it the next day instead?"
        /// }
        /// ```
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "BOOKING_REQUEST_APPROVED",
        ///   "data": {
        ///     "requestId": 124,
        ///     "status": 2,
        ///     "processedAt": "2025-01-17T10:30:00",
        ///     "processedBy": "Alice"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="requestId">Booking request ID</param>
        /// <param name="request">Response details (approve/reject)</param>
        /// <response code="200">Request processed successfully</response>
        /// <response code="400">Request already processed</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Request not found</response>
        [HttpPost("slot-request/{requestId:int}/respond")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> RespondToSlotRequest(
            int requestId,
            [FromBody] RespondToSlotRequestRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.RespondToSlotRequestAsync(requestId, userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Cancel a pending booking slot request (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Allows the requester to cancel their own pending slot request.**
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "reason": "Plans changed, no longer need the vehicle"
        /// }
        /// ```
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "BOOKING_REQUEST_CANCELLED",
        ///   "data": "Request #124 cancelled: Plans changed, no longer need the vehicle"
        /// }
        /// ```
        /// 
        /// **Note:** Can only cancel requests with Pending status
        /// </remarks>
        /// <param name="requestId">Booking request ID</param>
        /// <param name="request">Cancellation details</param>
        /// <response code="200">Request cancelled successfully</response>
        /// <response code="400">Can only cancel pending requests</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be request owner</response>
        /// <response code="404">Request not found</response>
        [HttpPost("slot-request/{requestId:int}/cancel")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> CancelSlotRequest(
            int requestId,
            [FromBody] CancelSlotRequestRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.CancelSlotRequestAsync(requestId, userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get all pending booking slot requests for a vehicle (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Retrieves all pending booking slot requests that require approval.**
        /// 
        /// **Use Cases:**
        /// - View pending requests from other co-owners
        /// - Check what approvals are needed
        /// - Monitor booking request queue
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "PENDING_REQUESTS_RETRIEVED",
        ///   "data": {
        ///     "vehicleId": 5,
        ///     "vehicleName": "Tesla Model 3",
        ///     "totalPendingCount": 3,
        ///     "oldestRequestDate": "2025-01-15T10:00:00",
        ///     "pendingRequests": [
        ///       {
        ///         "requestId": 125,
        ///         "requesterName": "John Smith",
        ///         "preferredStartTime": "2025-01-25T09:00:00",
        ///         "preferredEndTime": "2025-01-25T17:00:00",
        ///         "purpose": "Business trip",
        ///         "priority": 2,
        ///         "requestedAt": "2025-01-15T10:00:00"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Pending requests retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("vehicle/{vehicleId:int}/pending-slot-requests")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetPendingSlotRequests(int vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetPendingSlotRequestsAsync(vehicleId, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => Forbid(response.Message),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get analytics for booking slot requests (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Provides insights into booking request patterns and approval rates.**
        /// 
        /// **Query Parameters:**
        /// - `startDate` (optional): Start date for analysis (default: 90 days ago)
        /// - `endDate` (optional): End date for analysis (default: today)
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "ANALYTICS_RETRIEVED",
        ///   "data": {
        ///     "totalRequests": 45,
        ///     "approvedCount": 38,
        ///     "rejectedCount": 5,
        ///     "autoConfirmedCount": 25,
        ///     "approvalRate": 84.4,
        ///     "averageProcessingTimeHours": 4.5,
        ///     "mostRequestedTimeSlots": [
        ///       {
        ///         "dayOfWeek": 1,
        ///         "hourOfDay": 9,
        ///         "requestCount": 12,
        ///         "approvalRate": 91.7
        ///       }
        ///     ],
        ///     "requestsByCoOwner": [
        ///       {
        ///         "coOwnerId": 5,
        ///         "coOwnerName": "John Smith",
        ///         "totalRequests": 18,
        ///         "approvedRequests": 16,
        ///         "approvalRate": 88.9
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// 
        /// **Insights Provided:**
        /// - Total requests and approval/rejection counts
        /// - Auto-confirmation rate
        /// - Average time to process requests
        /// - Most popular time slots
        /// - Request statistics by co-owner
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="startDate">Optional: Analysis start date</param>
        /// <param name="endDate">Optional: Analysis end date</param>
        /// <response code="200">Analytics retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        [HttpGet("vehicle/{vehicleId:int}/slot-request-analytics")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetSlotRequestAnalytics(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetSlotRequestAnalyticsAsync(vehicleId, userId, startDate, endDate);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => Forbid(response.Message),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion

        #region Booking Conflict Resolution (Advanced Approve/Reject)

        /// <summary>
        /// Resolve a booking conflict with advanced intelligence (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Advanced conflict resolution system for EV co-ownership with intelligent decision-making.**
        /// 
        /// **Resolution Types:**
        /// - **SimpleApproval (0)**: Basic approve/reject decision
        /// - **CounterOffer (1)**: Reject with alternative time suggestion
        /// - **PriorityOverride (2)**: Use ownership % and usage fairness to decide winner
        /// - **AutoNegotiation (3)**: Let system auto-resolve based on multiple factors
        /// - **ConsensusRequired (4)**: All conflicting co-owners must approve
        /// 
        /// **Key Features:**
        /// - **Ownership weighting**: Higher ownership % = higher priority (if enabled)
        /// - **Usage fairness**: Co-owners with less usage get priority
        /// - **Auto-negotiation**: System calculates optimal resolution
        /// - **Counter-offers**: Suggest alternative times instead of rejecting
        /// - **Multi-stakeholder tracking**: See who approved/rejected
        /// - **Transparent decision**: Full explanation of resolution
        /// 
        /// **Request Body (Simple Approval):**
        /// ```json
        /// {
        ///   "isApproved": true,
        ///   "resolutionType": 0,
        ///   "notes": "Approved - I can use public transport that day"
        /// }
        /// ```
        /// 
        /// **Request Body (Counter-Offer):**
        /// ```json
        /// {
        ///   "isApproved": false,
        ///   "resolutionType": 1,
        ///   "rejectionReason": "I need the car for medical appointment",
        ///   "counterOfferStartTime": "2025-01-26T09:00:00",
        ///   "counterOfferEndTime": "2025-01-26T17:00:00",
        ///   "notes": "Can you use it the next day instead?"
        /// }
        /// ```
        /// 
        /// **Request Body (Priority Override):**
        /// ```json
        /// {
        ///   "isApproved": false,
        ///   "resolutionType": 2,
        ///   "useOwnershipWeighting": true,
        ///   "priorityJustification": "I have 60% ownership and less usage this month",
        ///   "rejectionReason": "I need priority for this booking"
        /// }
        /// ```
        /// 
        /// **Request Body (Auto-Negotiation):**
        /// ```json
        /// {
        ///   "isApproved": true,
        ///   "resolutionType": 3,
        ///   "enableAutoNegotiation": true,
        ///   "useOwnershipWeighting": true
        /// }
        /// ```
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "BOOKING_CONFLICT_RESOLVED_APPROVED",
        ///   "data": {
        ///     "bookingId": 124,
        ///     "outcome": 0,
        ///     "finalStatus": 1,
        ///     "resolvedBy": "Alice Johnson",
        ///     "resolvedAt": "2025-01-17T14:30:00",
        ///     "resolutionExplanation": "Booking approved by Alice Johnson. Conflicting bookings cancelled.",
        ///     "stakeholders": [
        ///       {
        ///         "userId": 5,
        ///         "name": "Alice Johnson",
        ///         "ownershipPercentage": 40,
        ///         "usageHoursThisMonth": 45,
        ///         "hasApproved": true,
        ///         "priorityWeight": 35
        ///       }
        ///     ],
        ///     "approvalStatus": {
        ///       "totalStakeholders": 1,
        ///       "approvalsReceived": 1,
        ///       "rejectionsReceived": 0,
        ///       "isFullyApproved": true,
        ///       "approvalPercentage": 100,
        ///       "weightedApprovalPercentage": 40
        ///     },
        ///     "recommendedActions": []
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to resolve</param>
        /// <param name="request">Resolution details</param>
        /// <response code="200">Conflict resolved successfully</response>
        /// <response code="400">Booking already processed</response>
        /// <response code="403">Access denied - not a co-owner</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("{bookingId:int}/resolve-conflict")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> ResolveBookingConflict(
            int bookingId,
            [FromBody] ResolveBookingConflictRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.ResolveBookingConflictAsync(bookingId, userId, request);
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
        /// Get pending conflicts requiring resolution (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Get all pending booking conflicts that require your attention.**
        /// 
        /// **Features:**
        /// - Filter by vehicle
        /// - Show only conflicts involving you
        /// - Auto-resolution preview
        /// - Priority filtering
        /// - Actionable insights
        /// 
        /// **Query Parameters:**
        /// - `vehicleId` (optional): Filter by specific vehicle
        /// - `onlyMyConflicts` (optional): Show only conflicts you're involved in
        /// - `minimumPriority` (optional): Filter by priority level (0=Low, 1=Medium, 2=High, 3=Urgent)
        /// - `includeAutoResolvable` (optional): Include conflicts that can be auto-resolved
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "PENDING_CONFLICTS_RETRIEVED",
        ///   "data": {
        ///     "totalConflicts": 3,
        ///     "requiringMyAction": 2,
        ///     "autoResolvable": 1,
        ///     "oldestConflictDate": "2025-01-15T10:00:00",
        ///     "actionItems": [
        ///       "You have 2 conflict(s) requiring your response",
        ///       "1 conflict(s) can be auto-resolved"
        ///     ],
        ///     "conflicts": [
        ///       {
        ///         "bookingId": 124,
        ///         "requesterName": "Bob Smith",
        ///         "requestedStartTime": "2025-01-25T09:00:00",
        ///         "requestedEndTime": "2025-01-25T17:00:00",
        ///         "purpose": "Business trip",
        ///         "priority": 2,
        ///         "conflictsWith": [
        ///           {
        ///             "bookingId": 120,
        ///             "coOwnerName": "Alice Johnson",
        ///             "overlapHours": 8,
        ///             "hasResponded": false
        ///           }
        ///         ],
        ///         "daysPending": 2,
        ///         "canAutoResolve": true,
        ///         "autoResolutionPreview": {
        ///           "predictedOutcome": 0,
        ///           "winnerName": "Bob Smith",
        ///           "explanation": "Bob Smith likely to be approved (higher priority)",
        ///           "confidence": 0.8
        ///         }
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Filter parameters</param>
        /// <response code="200">Pending conflicts retrieved</response>
        /// <response code="403">Access denied - not a co-owner</response>
        [HttpGet("pending-conflicts")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetPendingConflicts([FromQuery] GetPendingConflictsRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetPendingConflictsAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Get conflict resolution analytics for a vehicle (CoOwner only)
        /// </summary>
        /// <remarks>
        /// **Analytics about booking conflict resolution patterns and co-owner behavior.**
        /// 
        /// **Metrics:**
        /// - Total conflicts resolved vs pending
        /// - Approval and rejection rates
        /// - Average resolution time
        /// - Auto-resolution rate
        /// - Per co-owner statistics
        /// - Common conflict patterns
        /// - Actionable recommendations
        /// 
        /// **Query Parameters:**
        /// - `vehicleId` (required): Vehicle ID
        /// - `startDate` (optional): Start date for analytics (default: 90 days ago)
        /// - `endDate` (optional): End date for analytics (default: today)
        /// 
        /// **Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CONFLICT_ANALYTICS_RETRIEVED",
        ///   "data": {
        ///     "totalConflictsResolved": 45,
        ///     "totalConflictsPending": 3,
        ///     "averageResolutionTimeHours": 12.5,
        ///     "approvalRate": 68.9,
        ///     "rejectionRate": 31.1,
        ///     "autoResolutionRate": 15.6,
        ///     "statsByCoOwner": [
        ///       {
        ///         "userId": 5,
        ///         "name": "Alice Johnson",
        ///         "conflictsInitiated": 12,
        ///         "conflictsReceived": 8,
        ///         "approvalsGiven": 6,
        ///         "rejectionsGiven": 2,
        ///         "successRateAsRequester": 75.0,
        ///         "averageResponseTimeHours": 8.2
        ///       }
        ///     ],
        ///     "commonPatterns": [
        ///       {
        ///         "pattern": "High weekend conflict rate",
        ///         "occurrences": 15,
        ///         "recommendation": "Consider implementing weekend rotation schedule"
        ///       }
        ///     ],
        ///     "recommendations": [
        ///       {
        ///         "recommendation": "Implement weekend rotation schedule",
        ///         "rationale": "High weekend conflict rate detected",
        ///         "suggestedApproach": 4
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="startDate">Start date for analytics</param>
        /// <param name="endDate">End date for analytics</param>
        /// <response code="200">Analytics retrieved successfully</response>
        /// <response code="403">Access denied - not a vehicle co-owner</response>
        [HttpGet("vehicle/{vehicleId:int}/conflict-analytics")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetConflictAnalytics(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.GetConflictAnalyticsAsync(vehicleId, userId, startDate, endDate);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #endregion

        #region Booking Modification and Cancellation

        /// <summary>
        /// Modifies an existing booking with conflict validation and impact analysis
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (must be the booking creator)
        /// 
        /// **Modify Booking with Enhanced Validation:**
        /// 
        /// Allows co-owners to modify their bookings with intelligent conflict detection, impact analysis, and co-owner notification:
        /// 
        /// **Key Features:**
        /// - **Conflict Validation**: Automatically detects conflicts with other bookings when time is changed
        /// - **Impact Analysis**: Calculates time changes, cost implications, and required approvals
        /// - **Approval Workflow**: Can request co-owner approval if modification creates conflicts
        /// - **Co-owner Notifications**: Optionally notify affected co-owners about the modification
        /// - **Alternative Suggestions**: Provides alternative time slots if conflicts are detected
        /// - **Smart Warnings**: Alerts about late modifications (within 2 hours of booking start)
        /// 
        /// **Modification Rules:**
        /// - Only `Pending` or `Confirmed` bookings can be modified
        /// - Cannot modify `Completed` or `Cancelled` bookings
        /// - Must be the original booking creator
        /// - If modification creates conflicts:
        ///   - With `RequestApprovalIfConflict=true`: Status becomes `PendingApproval` (requires co-owner approval)
        ///   - With `RequestApprovalIfConflict=false`: Returns 409 Conflict with alternative suggestions
        /// 
        /// **Modification Scenarios:**
        /// 
        /// 1. **Simple Modification (No Conflicts):**
        ///    - Change purpose only → Success immediately
        ///    - Change time with no conflicts → Success immediately
        /// 
        /// 2. **Modification with Conflicts (Approval Requested):**
        ///    - Change time creates conflicts → Status: `PendingApproval`
        ///    - Requires approval from conflicting co-owners → Returns list of required approvals
        ///    - Modification not applied until approved
        /// 
        /// 3. **Modification with Conflicts (No Approval Requested):**
        ///    - Change time creates conflicts → Returns 409 Conflict
        ///    - Provides alternative time slot suggestions
        ///    - User must choose different time or request approval
        /// 
        /// **Sample Request (Simple Modification):**
        /// ```json
        /// {
        ///   "newStartTime": "2025-01-25T14:00:00Z",
        ///   "newEndTime": "2025-01-25T18:00:00Z",
        ///   "newPurpose": "Updated: Shopping and errands",
        ///   "modificationReason": "Need to extend the booking by 1 hour",
        ///   "skipConflictCheck": false,
        ///   "notifyAffectedCoOwners": true,
        ///   "requestApprovalIfConflict": false
        /// }
        /// ```
        /// 
        /// **Sample Request (With Approval Request):**
        /// ```json
        /// {
        ///   "newStartTime": "2025-01-26T08:00:00Z",
        ///   "newEndTime": "2025-01-26T12:00:00Z",
        ///   "modificationReason": "Need to reschedule to Saturday morning",
        ///   "skipConflictCheck": false,
        ///   "notifyAffectedCoOwners": true,
        ///   "requestApprovalIfConflict": true
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to modify</param>
        /// <param name="request">Modification request with new values</param>
        /// <response code="200">Booking modified successfully (no conflicts or conflicts resolved)</response>
        /// <response code="202">Modification pending approval (conflicts detected, approval requested)</response>
        /// <response code="400">Cannot modify completed/cancelled booking</response>
        /// <response code="403">Access denied - not the booking creator</response>
        /// <response code="404">Booking not found</response>
        /// <response code="409">Modification creates conflicts (no approval requested - includes alternative suggestions)</response>
        [HttpPost("{bookingId:int}/modify")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> ModifyBooking(
            int bookingId,
            [FromBody] ModifyBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.ModifyBookingAsync(bookingId, userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                202 => Accepted(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                409 => Conflict(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Cancels a booking with policy-based fees, refunds, and reschedule options
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (must be the booking creator)
        /// 
        /// **Enhanced Booking Cancellation with Policies:**
        /// 
        /// Allows co-owners to cancel their bookings with intelligent policy application, fee calculation, refund processing, and reschedule alternatives:
        /// 
        /// **Key Features:**
        /// - **Smart Cancellation Policies**: Fees based on timing (grace period, normal, late cancellation)
        /// - **Refund Calculation**: Automatic calculation of refundable amounts based on policy
        /// - **Reschedule Options**: Option to reschedule instead of cancelling
        /// - **Emergency Handling**: No-fee cancellation for emergency or vehicle issues
        /// - **Co-owner Notifications**: Automatically notifies other co-owners
        /// - **Alternative Suggestions**: Provides alternative time slots if rescheduling
        /// 
        /// **Cancellation Policy (User-Initiated):**
        /// 
        /// | Time Before Booking | Cancellation Fee | Refund % | Grace Period |
        /// |---------------------|------------------|----------|--------------|
        /// | 24+ hours           | 0%               | 100%     | ✅ Yes       |
        /// | 2-24 hours          | 25%              | 75%      | ❌ No        |
        /// | Less than 2 hours   | 50%              | 50%      | ❌ No        |
        /// | After booking start | 100% (No cancel) | 0%       | ❌ No        |
        /// 
        /// **Cancellation Types:**
        /// - `UserInitiated` (0): Normal user cancellation - follows policy above
        /// - `SystemCancelled` (1): System cancellation - full refund
        /// - `Emergency` (2): Emergency cancellation - no fee, full refund
        /// - `VehicleUnavailable` (3): Vehicle issue - no fee, full refund
        /// - `MaintenanceRequired` (4): Maintenance needed - no fee, full refund
        /// 
        /// **Reschedule Flow:**
        /// 1. Set `RequestReschedule=true`
        /// 2. Optionally provide `PreferredRescheduleStart` and `PreferredRescheduleEnd`
        /// 3. If preferred time available → Creates new booking, cancels old one (status: `Rescheduled`)
        /// 4. If preferred time unavailable → Returns alternative slot suggestions
        /// 5. No cancellation fee for rescheduling
        /// 
        /// **Sample Request (Normal Cancellation - 24+ hours):**
        /// ```json
        /// {
        ///   "cancellationReason": "Plans changed - no longer need the vehicle",
        ///   "cancellationType": 0,
        ///   "requestReschedule": false,
        ///   "acceptCancellationFee": true
        /// }
        /// ```
        /// 
        /// **Sample Request (Late Cancellation):**
        /// ```json
        /// {
        ///   "cancellationReason": "Unexpected conflict at work",
        ///   "cancellationType": 0,
        ///   "requestReschedule": false,
        ///   "acceptCancellationFee": true
        /// }
        /// ```
        /// 
        /// **Sample Request (Reschedule to Specific Time):**
        /// ```json
        /// {
        ///   "cancellationReason": "Need to reschedule to next weekend",
        ///   "cancellationType": 0,
        ///   "requestReschedule": true,
        ///   "preferredRescheduleStart": "2025-02-01T09:00:00Z",
        ///   "preferredRescheduleEnd": "2025-02-01T13:00:00Z",
        ///   "acceptCancellationFee": true
        /// }
        /// ```
        /// 
        /// **Sample Request (Emergency Cancellation):**
        /// ```json
        /// {
        ///   "cancellationReason": "Family emergency - immediate cancellation needed",
        ///   "cancellationType": 2,
        ///   "requestReschedule": false,
        ///   "acceptCancellationFee": false
        /// }
        /// ```
        /// </remarks>
        /// <param name="bookingId">Booking ID to cancel</param>
        /// <param name="request">Cancellation request with reason and options</param>
        /// <response code="200">Booking cancelled successfully (includes policy info, refund details, reschedule options)</response>
        /// <response code="400">Cannot cancel completed/already cancelled booking, or cancellation not allowed by policy</response>
        /// <response code="403">Access denied - not the booking creator</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("{bookingId:int}/cancel-enhanced")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> CancelBookingEnhanced(
            int bookingId,
            [FromBody] CancelBookingRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.CancelBookingEnhancedAsync(bookingId, userId, request);
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
        /// Validates a proposed booking modification without applying changes
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner (must be the booking creator)
        /// 
        /// **Pre-Validation for Booking Modifications:**
        /// 
        /// Allows co-owners to check the feasibility and impact of a proposed modification before actually making the change:
        /// 
        /// **Key Features:**
        /// - **Pre-Flight Check**: Validates modification without committing changes
        /// - **Conflict Detection**: Identifies all conflicts with proposed time change
        /// - **Impact Analysis**: Shows detailed impact (conflicts, costs, approvals needed)
        /// - **Warnings**: Lists all warnings (close to booking time, conflicts, etc.)
        /// - **Alternative Suggestions**: Provides conflict-free alternative time slots
        /// - **Smart Recommendations**: AI-driven recommendations based on analysis
        /// 
        /// **Use Cases:**
        /// 1. **Check Before Modify**: Validate proposed changes before actual modification
        /// 2. **Explore Options**: See impact of different time changes
        /// 3. **Find Alternatives**: Get suggestions for conflict-free modifications
        /// 4. **Approval Preview**: See which co-owners need to approve
        /// 
        /// **Validation Results:**
        /// - `IsValid`: Whether modification is allowed (booking not completed/cancelled)
        /// - `HasConflicts`: Whether proposed time creates conflicts
        /// - `ValidationErrors[]`: List of validation errors (blocking issues)
        /// - `Warnings[]`: List of warnings (non-blocking but important)
        /// - `ImpactAnalysis`: Detailed impact analysis object
        /// - `AlternativeSuggestions[]`: Conflict-free alternative time slots
        /// - `Recommendation`: Smart recommendation based on analysis
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "bookingId": 42,
        ///   "newStartTime": "2025-01-26T09:00:00Z",
        ///   "newEndTime": "2025-01-26T13:00:00Z",
        ///   "newPurpose": "Grocery shopping and bank errands"
        /// }
        /// ```
        /// 
        /// **Sample Response (No Conflicts):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "VALIDATION_PASSED",
        ///   "data": {
        ///     "isValid": true,
        ///     "hasConflicts": false,
        ///     "validationErrors": [],
        ///     "warnings": [],
        ///     "impactAnalysis": {
        ///       "hasTimeChange": true,
        ///       "hasConflicts": false,
        ///       "conflictCount": 0,
        ///       "timeDeltaHours": 1,
        ///       "requiresCoOwnerApproval": false,
        ///       "impactSummary": "No conflicts - modification can proceed"
        ///     },
        ///     "recommendation": "✅ Modification can proceed without issues."
        ///   }
        /// }
        /// ```
        /// 
        /// **Sample Response (With Conflicts):**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "VALIDATION_PASSED",
        ///   "data": {
        ///     "isValid": true,
        ///     "hasConflicts": true,
        ///     "validationErrors": [],
        ///     "warnings": [
        ///       "Modification creates 2 conflict(s)"
        ///     ],
        ///     "impactAnalysis": {
        ///       "hasTimeChange": true,
        ///       "hasConflicts": true,
        ///       "conflictCount": 2,
        ///       "conflictingBookings": [
        ///         {
        ///           "bookingId": 45,
        ///           "coOwnerName": "Alice Johnson",
        ///           "startTime": "2025-01-26T10:00:00Z",
        ///           "endTime": "2025-01-26T12:00:00Z",
        ///           "status": 1,
        ///           "purpose": "Weekend trip",
        ///           "overlapHours": 2.0
        ///         }
        ///       ],
        ///       "requiresCoOwnerApproval": true,
        ///       "impactSummary": "2 conflict(s) - requires co-owner approval"
        ///     },
        ///     "alternativeSuggestions": [
        ///       {
        ///         "startTime": "2025-01-26T13:00:00Z",
        ///         "endTime": "2025-01-26T17:00:00Z",
        ///         "reason": "Next available slot after conflicts",
        ///         "hasConflict": false
        ///       }
        ///     ],
        ///     "recommendation": "⚠️ Modification creates 2 conflict(s). Consider alternative time slots or request co-owner approval."
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Validation request with proposed changes</param>
        /// <response code="200">Validation completed (check isValid field for result)</response>
        /// <response code="403">Access denied - not the booking creator</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("validate-modification")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> ValidateModification([FromBody] ValidateModificationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _bookingService.ValidateModificationAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Retrieves modification history for bookings
        /// </summary>
        /// <remarks>
        /// **Role requirement:** CoOwner
        /// 
        /// **Booking Modification Audit Trail:**
        /// 
        /// Provides full history of all booking modifications and cancellations with before/after snapshots:
        /// 
        /// **Key Features:**
        /// - **Full Audit Trail**: Complete history of all modifications
        /// - **Before/After Comparison**: Shows original and modified values
        /// - **Filter by Booking**: Get history for specific booking
        /// - **Filter by User**: Get history for specific co-owner
        /// - **Filter by Date**: Get history within date range
        /// - **Filter by Status**: Filter by modification status
        /// - **Statistics**: Summary stats (total modifications, cancellations)
        /// 
        /// **History Entry Contains:**
        /// - Modification type (TimeChange, PurposeChange, StatusChange, Cancellation)
        /// - Modified by (user who made the change)
        /// - Modified at (timestamp)
        /// - Reason for modification
        /// - Before/After snapshots
        /// - Status (Success, PendingApproval, Rejected, Failed)
        /// - Required approvals
        /// - Approved by (if applicable)
        /// 
        /// **Query Parameters:**
        /// - `bookingId` (optional): Filter by specific booking
        /// - `userId` (optional): Filter by user who made modification
        /// - `startDate` (optional): Filter modifications from this date
        /// - `endDate` (optional): Filter modifications until this date
        /// - `status` (optional): Filter by modification status
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/booking/modification-history?bookingId=42
        /// GET /api/booking/modification-history?userId=5&amp;startDate=2025-01-01
        /// GET /api/booking/modification-history?status=1
        /// ```
        /// 
        /// **Note:** This feature requires a `BookingHistory` database table to track modifications.
        /// Current implementation returns placeholder data. Full implementation pending database migration.
        /// </remarks>
        /// <param name="bookingId">Optional: Filter by booking ID</param>
        /// <param name="userId">Optional: Filter by user who made modification</param>
        /// <param name="startDate">Optional: Filter from this date</param>
        /// <param name="endDate">Optional: Filter until this date</param>
        /// <param name="status">Optional: Filter by modification status</param>
        /// <response code="200">Modification history retrieved successfully</response>
        [HttpGet("modification-history")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetModificationHistory(
            [FromQuery] int? bookingId = null,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] ModificationStatus? status = null)
        {
            var request = new GetModificationHistoryRequest
            {
                BookingId = bookingId,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                FilterByStatus = status
            };

            var response = await _bookingService.GetModificationHistoryAsync(request);
            return Ok(response);
        }

        #endregion
    }
}


