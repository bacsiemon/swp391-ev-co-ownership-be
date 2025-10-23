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
    }
}

