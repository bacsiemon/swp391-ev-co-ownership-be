using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Services.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<BookingResponse>> CreateBookingAsync(int userId, CreateBookingRequest request)
        {
            try
            {
                // Get user's CoOwner record
                var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 403,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                // Check if vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Check if user is a co-owner of the vehicle
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwnerId == coOwner.UserId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Check for booking conflicts
                var hasConflict = await _unitOfWork.BookingRepository.GetQueryable()
                    .AnyAsync(b => b.VehicleId == request.VehicleId &&
                                  b.StatusEnum != EBookingStatus.Cancelled &&
                                  b.StatusEnum != EBookingStatus.Cancelled &&
                                  ((request.StartTime >= b.StartTime && request.StartTime < b.EndTime) ||
                                   (request.EndTime > b.StartTime && request.EndTime <= b.EndTime) ||
                                   (request.StartTime <= b.StartTime && request.EndTime >= b.EndTime)));

                if (hasConflict)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 409,
                        Message = "BOOKING_TIME_CONFLICT"
                    };
                }

                // Create booking
                var booking = new Booking
                {
                    CoOwnerId = coOwner.UserId,
                    VehicleId = request.VehicleId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Purpose = request.Purpose,
                    StatusEnum = EBookingStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.BookingRepository.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Get full booking details
                var createdBooking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .FirstOrDefaultAsync(b => b.Id == booking.Id);

                var response = MapToBookingResponse(createdBooking!);

                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 201,
                    Message = "BOOKING_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                // Log the exception for debugging
                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<BookingResponse>> GetBookingByIdAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.ApprovedByNavigation)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Check access rights (user must be the booking owner or admin/staff)
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

                // Simplified access check - only booking owner can view
                var isOwner = booking.CoOwner.UserId == userId;

                if (!isOwner)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var response = MapToBookingResponse(booking);

                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<BookingResponse>>> GetUserBookingsAsync(int userId, int pageIndex, int pageSize)
        {
            try
            {
                var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<PagedResult<BookingResponse>>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    };
                }

                var query = _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.ApprovedByNavigation)
                    .Where(b => b.CoOwnerId == coOwner.UserId)
                    .OrderByDescending(b => b.CreatedAt);

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var bookingResponses = bookings.Select(MapToBookingResponse).ToList();
                var pagedResult = new PagedResult<BookingResponse>(bookingResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<BookingResponse>>> GetVehicleBookingsAsync(int vehicleId, int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.ApprovedByNavigation)
                    .Where(b => b.VehicleId == vehicleId)
                    .OrderByDescending(b => b.CreatedAt);

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var bookingResponses = bookings.Select(MapToBookingResponse).ToList();
                var pagedResult = new PagedResult<BookingResponse>(bookingResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<BookingResponse>>> GetAllBookingsAsync(int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.ApprovedByNavigation)
                    .OrderByDescending(b => b.CreatedAt);

                var totalCount = await query.CountAsync();
                var bookings = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var bookingResponses = bookings.Select(MapToBookingResponse).ToList();
                var pagedResult = new PagedResult<BookingResponse>(bookingResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PagedResult<BookingResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<BookingResponse>> UpdateBookingAsync(int bookingId, int userId, UpdateBookingRequest request)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Check if user owns the booking
                if (booking.CoOwner.UserId != userId)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Only pending bookings can be updated
                if (booking.StatusEnum != EBookingStatus.Pending)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 400,
                        Message = "ONLY_PENDING_BOOKINGS_CAN_BE_UPDATED"
                    };
                }

                // Update fields
                if (request.StartTime.HasValue)
                    booking.StartTime = request.StartTime.Value;
                if (request.EndTime.HasValue)
                    booking.EndTime = request.EndTime.Value;
                if (!string.IsNullOrWhiteSpace(request.Purpose))
                    booking.Purpose = request.Purpose;

                booking.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                var updatedBooking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                var response = MapToBookingResponse(updatedBooking!);

                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 200,
                    Message = "BOOKING_UPDATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<BookingResponse>> ApproveBookingAsync(int bookingId, int approverId, ApproveBookingRequest request)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                if (booking.StatusEnum != EBookingStatus.Pending)
                {
                    return new BaseResponse<BookingResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_ALREADY_PROCESSED"
                    };
                }

                booking.StatusEnum = request.IsApproved ? EBookingStatus.Confirmed : EBookingStatus.Cancelled;
                booking.ApprovedBy = approverId;
                booking.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                var updatedBooking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.ApprovedByNavigation)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                var response = MapToBookingResponse(updatedBooking!);

                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 200,
                    Message = request.IsApproved ? "BOOKING_CONFIRMED_SUCCESSFULLY" : "BOOKING_CANCELLED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<string>> CancelBookingAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                if (booking.CoOwner.UserId != userId)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                if (booking.StatusEnum == EBookingStatus.Completed)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_CANCEL_COMPLETED_BOOKING"
                    };
                }

                booking.StatusEnum = EBookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "BOOKING_CANCELLED_SUCCESSFULLY"
                };
            }
            catch (Exception)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<string>> DeleteBookingAsync(int bookingId)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                await _unitOfWork.BookingRepository.DeleteAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "BOOKING_DELETED_SUCCESSFULLY"
                };
            }
            catch (Exception)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<BookingStatisticsResponse>> GetBookingStatisticsAsync()
        {
            try
            {
                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();

                var statistics = new BookingStatisticsResponse
                {
                    TotalBookings = allBookings.Count(),
                    PendingBookings = allBookings.Count(b => b.StatusEnum == EBookingStatus.Pending),
                    ApprovedBookings = allBookings.Count(b => b.StatusEnum == EBookingStatus.Confirmed),
                    RejectedBookings = 0, // Not tracked separately - cancelled bookings include rejections
                    CompletedBookings = allBookings.Count(b => b.StatusEnum == EBookingStatus.Completed),
                    CancelledBookings = allBookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled),
                    TotalRevenue = allBookings.Where(b => b.TotalCost.HasValue).Sum(b => b.TotalCost ?? 0)
                };

                return new BaseResponse<BookingStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = statistics
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        private BookingResponse MapToBookingResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                CoOwnerId = booking.CoOwnerId ?? 0,
                CoOwnerName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                VehicleId = booking.VehicleId ?? 0,
                VehicleName = booking.Vehicle?.Name ?? "",
                LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Purpose = booking.Purpose ?? "",
                Status = booking.StatusEnum ?? EBookingStatus.Pending,
                ApprovedBy = booking.ApprovedBy,
                ApprovedByName = booking.ApprovedByNavigation != null
                    ? $"{booking.ApprovedByNavigation.FirstName} {booking.ApprovedByNavigation.LastName}".Trim()
                    : null,
                TotalCost = booking.TotalCost,
                CreatedAt = booking.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = booking.UpdatedAt
            };
        }

        public async Task<BaseResponse<BookingCalendarResponse>> GetBookingCalendarAsync(
            int userId,
            DateTime startDate,
            DateTime endDate,
            int? vehicleId = null,
            string? status = null)
        {
            try
            {
                // Validate date range
                if (startDate >= endDate)
                {
                    return new BaseResponse<BookingCalendarResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_DATE_RANGE",
                        Errors = "Start date must be before end date"
                    };
                }

                // Check if date range is too large (e.g., max 90 days)
                if ((endDate - startDate).TotalDays > 90)
                {
                    return new BaseResponse<BookingCalendarResponse>
                    {
                        StatusCode = 400,
                        Message = "DATE_RANGE_TOO_LARGE",
                        Errors = "Date range cannot exceed 90 days"
                    };
                }

                // Get user info and determine role
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<BookingCalendarResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                // Parse status filter if provided
                EBookingStatus? statusFilter = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<EBookingStatus>(status, true, out var parsedStatus))
                    {
                        statusFilter = parsedStatus;
                    }
                    else
                    {
                        return new BaseResponse<BookingCalendarResponse>
                        {
                            StatusCode = 400,
                            Message = "INVALID_STATUS_FILTER",
                            Errors = $"Invalid status: {status}. Valid values: Pending, Confirmed, Active, Completed, Cancelled"
                        };
                    }
                }

                // Get coOwnerId for role-based filtering
                int? coOwnerIdFilter = null;
                if (user.RoleEnum == EUserRole.CoOwner)
                {
                    var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                        .FirstOrDefaultAsync(co => co.UserId == userId);

                    if (coOwner == null)
                    {
                        return new BaseResponse<BookingCalendarResponse>
                        {
                            StatusCode = 403,
                            Message = "USER_NOT_CO_OWNER"
                        };
                    }
                    coOwnerIdFilter = coOwner.UserId;
                }
                // Staff and Admin can see all bookings (coOwnerIdFilter remains null)

                // Get bookings for calendar
                var bookings = await _unitOfWork.BookingRepository.GetBookingsForCalendarAsync(
                    startDate, endDate, coOwnerIdFilter, vehicleId, statusFilter);

                // Map to calendar events
                var events = bookings.Select(b => new BookingCalendarEvent
                {
                    BookingId = b.Id,
                    VehicleId = b.VehicleId ?? 0,
                    VehicleName = b.Vehicle?.Name ?? "",
                    Brand = b.Vehicle?.Brand ?? "",
                    Model = b.Vehicle?.Model ?? "",
                    LicensePlate = b.Vehicle?.LicensePlate ?? "",
                    CoOwnerId = b.CoOwnerId ?? 0,
                    CoOwnerName = $"{b.CoOwner?.User?.FirstName} {b.CoOwner?.User?.LastName}".Trim(),
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Purpose = b.Purpose ?? "",
                    Status = b.StatusEnum ?? EBookingStatus.Pending,
                    StatusDisplay = (b.StatusEnum ?? EBookingStatus.Pending).ToString(),
                    DurationHours = (int)Math.Ceiling((b.EndTime - b.StartTime).TotalHours),
                    IsCurrentUser = b.CoOwner?.UserId == userId
                }).ToList();

                // Calculate summary statistics
                var summary = new BookingCalendarSummary
                {
                    TotalBookings = events.Count,
                    PendingBookings = events.Count(e => e.Status == EBookingStatus.Pending),
                    ConfirmedBookings = events.Count(e => e.Status == EBookingStatus.Confirmed),
                    ActiveBookings = events.Count(e => e.Status == EBookingStatus.Active),
                    CompletedBookings = events.Count(e => e.Status == EBookingStatus.Completed),
                    CancelledBookings = events.Count(e => e.Status == EBookingStatus.Cancelled),
                    TotalVehicles = events.Select(e => e.VehicleId).Distinct().Count(),
                    MyBookings = events.Count(e => e.IsCurrentUser)
                };

                var response = new BookingCalendarResponse
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Events = events,
                    TotalEvents = events.Count,
                    Summary = summary
                };

                return new BaseResponse<BookingCalendarResponse>
                {
                    StatusCode = 200,
                    Message = "BOOKING_CALENDAR_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingCalendarResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<VehicleAvailabilityResponse>> CheckVehicleAvailabilityAsync(
            int vehicleId,
            DateTime startTime,
            DateTime endTime)
        {
            try
            {
                // Validate time range
                if (startTime >= endTime)
                {
                    return new BaseResponse<VehicleAvailabilityResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_TIME_RANGE",
                        Errors = "Start time must be before end time"
                    };
                }

                // Check if vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<VehicleAvailabilityResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Get conflicting bookings
                var conflictingBookings = await _unitOfWork.BookingRepository
                    .GetConflictingBookingsAsync(vehicleId, startTime, endTime);

                bool isAvailable = !conflictingBookings.Any();
                string message = isAvailable
                    ? "VEHICLE_AVAILABLE"
                    : "VEHICLE_NOT_AVAILABLE_TIME_CONFLICT";

                var response = new VehicleAvailabilityResponse
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    IsAvailable = isAvailable,
                    Message = message,
                    ConflictingBookings = conflictingBookings.Any()
                        ? conflictingBookings.Select(b => new BookingCalendarEvent
                        {
                            BookingId = b.Id,
                            VehicleId = b.VehicleId ?? 0,
                            VehicleName = vehicle.Name,
                            Brand = vehicle.Brand,
                            Model = vehicle.Model,
                            LicensePlate = vehicle.LicensePlate,
                            CoOwnerId = b.CoOwnerId ?? 0,
                            CoOwnerName = $"{b.CoOwner?.User?.FirstName} {b.CoOwner?.User?.LastName}".Trim(),
                            StartTime = b.StartTime,
                            EndTime = b.EndTime,
                            Purpose = b.Purpose ?? "",
                            Status = b.StatusEnum ?? EBookingStatus.Pending,
                            StatusDisplay = (b.StatusEnum ?? EBookingStatus.Pending).ToString(),
                            DurationHours = (int)Math.Ceiling((b.EndTime - b.StartTime).TotalHours),
                            IsCurrentUser = false
                        }).ToList()
                        : null
                };

                return new BaseResponse<VehicleAvailabilityResponse>
                {
                    StatusCode = 200,
                    Message = message,
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<VehicleAvailabilityResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        #region Booking Slot Request Methods

        public async Task<BaseResponse<BookingSlotRequestResponse>> RequestBookingSlotAsync(
            int vehicleId,
            int userId,
            RequestBookingSlotRequest request)
        {
            try
            {
                var startTime = DateTime.Now;

                // Validate user is co-owner
                var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                // Check if vehicle exists and user is co-owner
                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .Include(v => v.VehicleCoOwners)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                var isCoOwner = vehicle.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == userId && vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Check for conflicting bookings
                var conflictingBookings = await _unitOfWork.BookingRepository.GetConflictingBookingsAsync(
                    vehicleId,
                    request.PreferredStartTime,
                    request.PreferredEndTime);

                var conflicts = conflictingBookings
                    .Where(b => b.StatusEnum != EBookingStatus.Cancelled)
                    .Select(b => new ConflictingBookingInfo
                    {
                        BookingId = b.Id,
                        CoOwnerName = $"{b.CoOwner?.User?.FirstName} {b.CoOwner?.User?.LastName}".Trim(),
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Status = b.StatusEnum ?? EBookingStatus.Pending,
                        Purpose = b.Purpose ?? "",
                        OverlapHours = CalculateOverlapHours(
                            request.PreferredStartTime, request.PreferredEndTime,
                            b.StartTime, b.EndTime)
                    })
                    .ToList();

                // Determine availability status
                SlotAvailabilityStatus availabilityStatus;
                SlotRequestStatus requestStatus;
                Booking? createdBooking = null;
                string? autoConfirmMessage = null;

                if (!conflicts.Any())
                {
                    availabilityStatus = SlotAvailabilityStatus.Available;

                    if (request.AutoConfirmIfAvailable)
                    {
                        // Auto-confirm: Create booking directly
                        createdBooking = new Booking
                        {
                            CoOwnerId = userId,
                            VehicleId = vehicleId,
                            StartTime = request.PreferredStartTime,
                            EndTime = request.PreferredEndTime,
                            Purpose = request.Purpose,
                            StatusEnum = EBookingStatus.Confirmed,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.BookingRepository.AddAsync(createdBooking);
                        await _unitOfWork.SaveChangesAsync();

                        requestStatus = SlotRequestStatus.AutoConfirmed;
                        autoConfirmMessage = "Slot was automatically confirmed as it's available with no conflicts";
                    }
                    else
                    {
                        // Create as pending for manual approval
                        createdBooking = new Booking
                        {
                            CoOwnerId = userId,
                            VehicleId = vehicleId,
                            StartTime = request.PreferredStartTime,
                            EndTime = request.PreferredEndTime,
                            Purpose = request.Purpose,
                            StatusEnum = EBookingStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.BookingRepository.AddAsync(createdBooking);
                        await _unitOfWork.SaveChangesAsync();

                        requestStatus = SlotRequestStatus.Pending;
                    }
                }
                else
                {
                    // Has conflicts
                    var hasHardConflict = conflicts.Any(c =>
                        c.Status == EBookingStatus.Confirmed || c.Status == EBookingStatus.Active);

                    if (hasHardConflict)
                    {
                        availabilityStatus = SlotAvailabilityStatus.Unavailable;
                    }
                    else
                    {
                        availabilityStatus = SlotAvailabilityStatus.PartiallyAvailable;
                    }

                    availabilityStatus = SlotAvailabilityStatus.RequiresApproval;

                    // Create booking with Pending status requiring approval
                    createdBooking = new Booking
                    {
                        CoOwnerId = userId,
                        VehicleId = vehicleId,
                        StartTime = request.PreferredStartTime,
                        EndTime = request.PreferredEndTime,
                        Purpose = request.Purpose,
                        StatusEnum = EBookingStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.BookingRepository.AddAsync(createdBooking);
                    await _unitOfWork.SaveChangesAsync();

                    requestStatus = SlotRequestStatus.Pending;
                }

                // Generate alternative suggestions if slot has conflicts or user wants flexibility
                var alternativeSuggestions = new List<AlternativeSlotSuggestion>();
                if (conflicts.Any() || request.IsFlexible)
                {
                    alternativeSuggestions = await GenerateAlternativeSlotSuggestionsAsync(
                        vehicleId,
                        request.PreferredStartTime,
                        request.PreferredEndTime,
                        request.AlternativeSlots);
                }

                // Get co-owners who need to approve (if conflicts exist)
                var approvalPendingFrom = new List<string>();
                if (conflicts.Any())
                {
                    approvalPendingFrom = conflicts
                        .Select(c => c.CoOwnerName)
                        .Distinct()
                        .ToList();
                }

                // Calculate processing time
                var processingTime = (int)(DateTime.Now - startTime).TotalSeconds;

                // Build response
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                var response = new BookingSlotRequestResponse
                {
                    RequestId = createdBooking!.Id,
                    BookingId = createdBooking.Id,
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    RequesterId = userId,
                    RequesterName = $"{user?.FirstName} {user?.LastName}".Trim(),
                    PreferredStartTime = request.PreferredStartTime,
                    PreferredEndTime = request.PreferredEndTime,
                    Purpose = request.Purpose,
                    Priority = request.Priority,
                    Status = requestStatus,
                    IsFlexible = request.IsFlexible,
                    EstimatedDistance = request.EstimatedDistance,
                    UsageType = request.UsageType,
                    RequestedAt = createdBooking.CreatedAt ?? DateTime.UtcNow,
                    AvailabilityStatus = availabilityStatus,
                    ConflictingBookings = conflicts.Any() ? conflicts : null,
                    AlternativeSuggestions = alternativeSuggestions.Any() ? alternativeSuggestions : null,
                    AutoConfirmationMessage = autoConfirmMessage,
                    Metadata = new BookingSlotRequestMetadata
                    {
                        TotalAlternativesProvided = request.AlternativeSlots?.Count ?? 0,
                        ProcessingTimeSeconds = processingTime,
                        RequiresCoOwnerApproval = conflicts.Any(),
                        ApprovalPendingFrom = approvalPendingFrom,
                        SystemRecommendation = GenerateSystemRecommendation(
                            availabilityStatus, conflicts.Count, alternativeSuggestions.Count)
                    }
                };

                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 201,
                    Message = requestStatus == SlotRequestStatus.AutoConfirmed
                        ? "BOOKING_SLOT_AUTO_CONFIRMED"
                        : "BOOKING_SLOT_REQUEST_CREATED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<BookingSlotRequestResponse>> RespondToSlotRequestAsync(
            int requestId,
            int userId,
            RespondToSlotRequestRequest request)
        {
            try
            {
                // Get the booking/request
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co!.User)
                    .Include(b => b.Vehicle)
                    .FirstOrDefaultAsync(b => b.Id == requestId);

                if (booking == null)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_REQUEST_NOT_FOUND"
                    };
                }

                // Validate that user is a co-owner of this vehicle
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == booking.VehicleId &&
                                    vco.CoOwnerId == userId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Check if already processed
                if (booking.StatusEnum != EBookingStatus.Pending)
                {
                    return new BaseResponse<BookingSlotRequestResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_REQUEST_ALREADY_PROCESSED"
                    };
                }

                // Process the response
                if (request.IsApproved)
                {
                    booking.StatusEnum = EBookingStatus.Confirmed;
                    booking.ApprovedBy = userId;
                    booking.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    booking.StatusEnum = EBookingStatus.Cancelled;
                    booking.ApprovedBy = userId;
                    booking.UpdatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Build response
                var response = new BookingSlotRequestResponse
                {
                    RequestId = booking.Id,
                    BookingId = booking.Id,
                    VehicleId = booking.VehicleId ?? 0,
                    VehicleName = booking.Vehicle?.Name ?? "",
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    RequesterId = booking.CoOwnerId ?? 0,
                    RequesterName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                    PreferredStartTime = booking.StartTime,
                    PreferredEndTime = booking.EndTime,
                    Purpose = booking.Purpose ?? "",
                    Priority = BookingPriority.Medium,
                    Status = request.IsApproved ? SlotRequestStatus.Approved : SlotRequestStatus.Rejected,
                    IsFlexible = false,
                    RequestedAt = booking.CreatedAt ?? DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessedBy = (await _unitOfWork.UserRepository.GetByIdAsync(userId))?.FirstName,
                    AvailabilityStatus = request.IsApproved
                        ? SlotAvailabilityStatus.Available
                        : SlotAvailabilityStatus.Unavailable,
                    Metadata = new BookingSlotRequestMetadata
                    {
                        SystemRecommendation = request.IsApproved
                            ? "Request approved successfully"
                            : $"Request rejected: {request.RejectionReason}"
                    }
                };

                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 200,
                    Message = request.IsApproved
                        ? "BOOKING_REQUEST_APPROVED"
                        : "BOOKING_REQUEST_REJECTED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<string>> CancelSlotRequestAsync(
            int requestId,
            int userId,
            CancelSlotRequestRequest request)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .FirstOrDefaultAsync(b => b.Id == requestId);

                if (booking == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_REQUEST_NOT_FOUND"
                    };
                }

                // Validate ownership
                if (booking.CoOwnerId != userId)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_REQUEST_OWNER"
                    };
                }

                // Can only cancel pending requests
                if (booking.StatusEnum != EBookingStatus.Pending)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 400,
                        Message = "CAN_ONLY_CANCEL_PENDING_REQUESTS"
                    };
                }

                booking.StatusEnum = EBookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "BOOKING_REQUEST_CANCELLED",
                    Data = $"Request #{requestId} cancelled: {request.Reason}"
                };
            }
            catch (Exception)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<PendingSlotRequestsResponse>> GetPendingSlotRequestsAsync(
            int vehicleId,
            int userId)
        {
            try
            {
                // Validate user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                    vco.CoOwnerId == userId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<PendingSlotRequestsResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<PendingSlotRequestsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Get all pending requests for this vehicle
                var pendingBookings = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co!.User)
                    .Include(b => b.Vehicle)
                    .Where(b => b.VehicleId == vehicleId && b.StatusEnum == EBookingStatus.Pending)
                    .OrderBy(b => b.CreatedAt)
                    .ToListAsync();

                var pendingRequests = pendingBookings.Select(b => new BookingSlotRequestResponse
                {
                    RequestId = b.Id,
                    BookingId = b.Id,
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    RequesterId = b.CoOwnerId ?? 0,
                    RequesterName = $"{b.CoOwner?.User?.FirstName} {b.CoOwner?.User?.LastName}".Trim(),
                    PreferredStartTime = b.StartTime,
                    PreferredEndTime = b.EndTime,
                    Purpose = b.Purpose ?? "",
                    Priority = BookingPriority.Medium,
                    Status = SlotRequestStatus.Pending,
                    IsFlexible = false,
                    RequestedAt = b.CreatedAt ?? DateTime.UtcNow,
                    AvailabilityStatus = SlotAvailabilityStatus.RequiresApproval,
                    Metadata = new BookingSlotRequestMetadata
                    {
                        RequiresCoOwnerApproval = true
                    }
                }).ToList();

                var response = new PendingSlotRequestsResponse
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    PendingRequests = pendingRequests,
                    TotalPendingCount = pendingRequests.Count,
                    OldestRequestDate = pendingRequests.Any()
                        ? pendingRequests.Min(r => r.RequestedAt)
                        : DateTime.UtcNow
                };

                return new BaseResponse<PendingSlotRequestsResponse>
                {
                    StatusCode = 200,
                    Message = "PENDING_REQUESTS_RETRIEVED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PendingSlotRequestsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<SlotRequestAnalytics>> GetSlotRequestAnalyticsAsync(
            int vehicleId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Validate user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                    vco.CoOwnerId == userId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<SlotRequestAnalytics>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Default to last 90 days
                startDate ??= DateTime.UtcNow.AddDays(-90);
                endDate ??= DateTime.UtcNow;

                var bookings = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner).ThenInclude(co => co!.User)
                    .Where(b => b.VehicleId == vehicleId &&
                               b.CreatedAt >= startDate &&
                               b.CreatedAt <= endDate)
                    .ToListAsync();

                var totalRequests = bookings.Count;
                var approvedCount = bookings.Count(b => b.StatusEnum == EBookingStatus.Confirmed);
                var rejectedCount = bookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled);
                var autoConfirmedCount = bookings.Count(b =>
                    b.StatusEnum == EBookingStatus.Confirmed && b.ApprovedBy == null);

                var approvalRate = totalRequests > 0
                    ? (decimal)approvedCount / totalRequests * 100
                    : 0;

                // Calculate average processing time
                var processedBookings = bookings
                    .Where(b => b.UpdatedAt.HasValue && b.StatusEnum != EBookingStatus.Pending)
                    .ToList();

                var avgProcessingTime = processedBookings.Any()
                    ? (decimal)processedBookings
                        .Average(b => (b.UpdatedAt!.Value - b.CreatedAt!.Value).TotalHours)
                    : 0;

                // Most requested time slots
                var timeSlotGroups = bookings
                    .GroupBy(b => new { b.StartTime.DayOfWeek, b.StartTime.Hour })
                    .Select(g => new PopularTimeSlot
                    {
                        DayOfWeek = g.Key.DayOfWeek,
                        HourOfDay = g.Key.Hour,
                        RequestCount = g.Count(),
                        ApprovalRate = g.Count(b => b.StatusEnum == EBookingStatus.Confirmed) * 100m / g.Count()
                    })
                    .OrderByDescending(t => t.RequestCount)
                    .Take(10)
                    .ToList();

                // Requests by co-owner
                var coOwnerStats = bookings
                    .GroupBy(b => new { b.CoOwnerId, CoOwnerName = $"{b.CoOwner?.User?.FirstName} {b.CoOwner?.User?.LastName}".Trim() })
                    .Select(g => new CoOwnerRequestStats
                    {
                        CoOwnerId = g.Key.CoOwnerId ?? 0,
                        CoOwnerName = g.Key.CoOwnerName,
                        TotalRequests = g.Count(),
                        ApprovedRequests = g.Count(b => b.StatusEnum == EBookingStatus.Confirmed),
                        RejectedRequests = g.Count(b => b.StatusEnum == EBookingStatus.Cancelled),
                        ApprovalRate = g.Count() > 0
                            ? g.Count(b => b.StatusEnum == EBookingStatus.Confirmed) * 100m / g.Count()
                            : 0
                    })
                    .OrderByDescending(s => s.TotalRequests)
                    .ToList();

                var analytics = new SlotRequestAnalytics
                {
                    TotalRequests = totalRequests,
                    ApprovedCount = approvedCount,
                    RejectedCount = rejectedCount,
                    AutoConfirmedCount = autoConfirmedCount,
                    CancelledCount = rejectedCount,
                    AverageProcessingTimeHours = avgProcessingTime,
                    ApprovalRate = approvalRate,
                    MostRequestedTimeSlots = timeSlotGroups,
                    RequestsByCoOwner = coOwnerStats
                };

                return new BaseResponse<SlotRequestAnalytics>
                {
                    StatusCode = 200,
                    Message = "ANALYTICS_RETRIEVED",
                    Data = analytics
                };
            }
            catch (Exception)
            {
                return new BaseResponse<SlotRequestAnalytics>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        #endregion

        #region Helper Methods

        private decimal CalculateOverlapHours(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            var overlapStart = start1 > start2 ? start1 : start2;
            var overlapEnd = end1 < end2 ? end1 : end2;

            if (overlapEnd <= overlapStart)
                return 0;

            return (decimal)(overlapEnd - overlapStart).TotalHours;
        }

        private async Task<List<AlternativeSlotSuggestion>> GenerateAlternativeSlotSuggestionsAsync(
            int vehicleId,
            DateTime preferredStart,
            DateTime preferredEnd,
            List<AlternativeTimeSlot>? userProvidedAlternatives)
        {
            var suggestions = new List<AlternativeSlotSuggestion>();
            var duration = (preferredEnd - preferredStart).TotalHours;

            // Check user-provided alternatives first
            if (userProvidedAlternatives != null && userProvidedAlternatives.Any())
            {
                foreach (var alt in userProvidedAlternatives.OrderBy(a => a.PreferenceRank).Take(3))
                {
                    var conflicts = await _unitOfWork.BookingRepository.GetConflictingBookingsAsync(
                        vehicleId, alt.StartTime, alt.EndTime);

                    var isAvailable = !conflicts.Any(b => b.StatusEnum != EBookingStatus.Cancelled);

                    suggestions.Add(new AlternativeSlotSuggestion
                    {
                        StartTime = alt.StartTime,
                        EndTime = alt.EndTime,
                        DurationHours = (decimal)(alt.EndTime - alt.StartTime).TotalHours,
                        IsAvailable = isAvailable,
                        Reason = isAvailable ? "User-provided alternative slot" : "Has conflicts",
                        ConflictProbability = isAvailable ? 0 : 1,
                        RecommendationScore = isAvailable ? 90 - (alt.PreferenceRank * 10) : 30
                    });
                }
            }

            // Generate system suggestions (nearby times)
            var systemSuggestions = new[]
            {
                preferredStart.AddHours(-duration * 1.5),  // Before
                preferredStart.AddHours(duration * 1.5),   // After
                preferredStart.AddDays(1),                 // Next day same time
                preferredStart.AddDays(-1)                 // Previous day same time
            };

            foreach (var suggestedStart in systemSuggestions.Where(s => s > DateTime.Now))
            {
                var suggestedEnd = suggestedStart.AddHours(duration);
                var conflicts = await _unitOfWork.BookingRepository.GetConflictingBookingsAsync(
                    vehicleId, suggestedStart, suggestedEnd);

                var isAvailable = !conflicts.Any(b => b.StatusEnum != EBookingStatus.Cancelled);
                var conflictProbability = conflicts.Count * 0.2m;

                if (suggestions.Count < 5)
                {
                    suggestions.Add(new AlternativeSlotSuggestion
                    {
                        StartTime = suggestedStart,
                        EndTime = suggestedEnd,
                        DurationHours = (decimal)duration,
                        IsAvailable = isAvailable,
                        Reason = GenerateAlternativeReason(suggestedStart, preferredStart),
                        ConflictProbability = Math.Min(conflictProbability, 1m),
                        RecommendationScore = isAvailable ? 70 : 40
                    });
                }
            }

            return suggestions.OrderByDescending(s => s.RecommendationScore).ToList();
        }

        private string GenerateAlternativeReason(DateTime suggested, DateTime preferred)
        {
            var diff = (suggested - preferred).TotalHours;

            if (Math.Abs(diff) < 3)
                return "Similar time, just shifted slightly";
            else if (diff > 0 && diff < 24)
                return "Later the same day";
            else if (diff < 0 && diff > -24)
                return "Earlier the same day";
            else if (diff >= 24)
                return "Next day at same time";
            else
                return "Previous day at same time";
        }

        private string GenerateSystemRecommendation(
            SlotAvailabilityStatus status,
            int conflictCount,
            int alternativeCount)
        {
            return status switch
            {
                SlotAvailabilityStatus.Available => "Your preferred slot is available and can be confirmed",
                SlotAvailabilityStatus.RequiresApproval =>
                    $"Your slot conflicts with {conflictCount} booking(s). Co-owner approval required.",
                SlotAvailabilityStatus.Unavailable =>
                    $"Slot unavailable due to {conflictCount} confirmed booking(s). Please choose an alternative.",
                SlotAvailabilityStatus.PartiallyAvailable =>
                    $"Partial overlap detected. {alternativeCount} alternatives suggested.",
                _ => "Please review the booking details"
            };
        }

        #endregion

        #region Booking Conflict Resolution (Advanced Approve/Reject)

        public async Task<BaseResponse<BookingConflictResolutionResponse>> ResolveBookingConflictAsync(
            int bookingId,
            int userId,
            ResolveBookingConflictRequest request)
        {
            try
            {
                // Get the booking with all related data
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                        .ThenInclude(v => v.VehicleCoOwners)
                            .ThenInclude(vco => vco.CoOwner)
                                .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<BookingConflictResolutionResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Validate user is co-owner of this vehicle
                var isCoOwner = booking.Vehicle?.VehicleCoOwners
                    .Any(vco => vco.CoOwner?.UserId == userId && vco.StatusEnum == EEContractStatus.Active) ?? false;

                if (!isCoOwner)
                {
                    return new BaseResponse<BookingConflictResolutionResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Check if booking is still pending
                if (booking.StatusEnum != EBookingStatus.Pending)
                {
                    return new BaseResponse<BookingConflictResolutionResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_ALREADY_PROCESSED"
                    };
                }

                // Get all co-owners for this vehicle
                var allCoOwners = booking.Vehicle?.VehicleCoOwners
                    .Where(vco => vco.StatusEnum == EEContractStatus.Active)
                    .Select(vco => new
                    {
                        UserId = vco.CoOwner?.UserId ?? 0,
                        Name = $"{vco.CoOwner?.User?.FirstName} {vco.CoOwner?.User?.LastName}".Trim(),
                        OwnershipPercentage = vco.OwnershipPercentage,
                        CoOwnerId = vco.CoOwnerId
                    })
                    .ToList() ?? new();

                // Get conflicting bookings
                var conflicts = await _unitOfWork.BookingRepository.GetQueryable()
                    .Where(b => b.VehicleId == booking.VehicleId &&
                               b.Id != booking.Id &&
                               b.StatusEnum != EBookingStatus.Cancelled &&
                               ((booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime) ||
                                (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime) ||
                                (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)))
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .ToListAsync();

                // Calculate stakeholders
                var stakeholders = new List<ConflictStakeholder>();
                foreach (var coOwner in allCoOwners)
                {
                    // Get usage hours this month for this co-owner
                    var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    var monthBookings = await _unitOfWork.BookingRepository.GetQueryable()
                        .Where(b => b.CoOwnerId == coOwner.CoOwnerId &&
                                   b.VehicleId == booking.VehicleId &&
                                   b.StatusEnum != EBookingStatus.Cancelled &&
                                   b.StartTime >= startOfMonth)
                        .ToListAsync();
                    var usageHours = monthBookings.Sum(b => (int)(b.EndTime - b.StartTime).TotalHours);

                    var hasConflict = conflicts.Any(c => c.CoOwnerId == coOwner.CoOwnerId);

                    stakeholders.Add(new ConflictStakeholder
                    {
                        UserId = coOwner.UserId,
                        Name = coOwner.Name,
                        OwnershipPercentage = coOwner.OwnershipPercentage,
                        UsageHoursThisMonth = usageHours,
                        HasApproved = false, // Will be updated below
                        HasRejected = false,
                        ResponseDate = null,
                        ResponseNotes = null,
                        PriorityWeight = CalculatePriorityWeight(
                            coOwner.OwnershipPercentage,
                            usageHours,
                            hasConflict)
                    });
                }

                // Determine resolution outcome based on request type
                ConflictResolutionOutcome outcome;
                EBookingStatus finalStatus = booking.StatusEnum ?? EBookingStatus.Pending;
                string resolutionExplanation = "";
                AutoResolutionInfo? autoResolution = null;
                CounterOfferInfo? counterOffer = null;
                List<string> recommendedActions = new();

                // Update stakeholder who responded
                var respondingStakeholder = stakeholders.FirstOrDefault(s => s.UserId == userId);
                if (respondingStakeholder != null)
                {
                    respondingStakeholder.HasApproved = request.IsApproved;
                    respondingStakeholder.HasRejected = !request.IsApproved;
                    respondingStakeholder.ResponseDate = DateTime.UtcNow;
                    respondingStakeholder.ResponseNotes = request.Notes;
                }

                switch (request.ResolutionType)
                {
                    case ConflictResolutionType.SimpleApproval:
                        if (request.IsApproved)
                        {
                            // Cancel conflicting bookings from this co-owner
                            foreach (var conflict in conflicts.Where(c => c.CoOwnerId == respondingStakeholder?.UserId))
                            {
                                conflict.StatusEnum = EBookingStatus.Cancelled;
                                conflict.UpdatedAt = DateTime.UtcNow;
                                await _unitOfWork.BookingRepository.UpdateAsync(conflict);
                            }

                            finalStatus = EBookingStatus.Confirmed;
                            booking.StatusEnum = finalStatus;
                            booking.ApprovedBy = userId;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.BookingRepository.UpdateAsync(booking);

                            outcome = ConflictResolutionOutcome.Approved;
                            resolutionExplanation = $"Booking approved by {respondingStakeholder?.Name}. Conflicting bookings cancelled.";
                        }
                        else
                        {
                            finalStatus = EBookingStatus.Cancelled;
                            booking.StatusEnum = finalStatus;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.BookingRepository.UpdateAsync(booking);

                            outcome = ConflictResolutionOutcome.Rejected;
                            resolutionExplanation = $"Booking rejected by {respondingStakeholder?.Name}. Reason: {request.RejectionReason}";
                            recommendedActions.Add("Consider requesting an alternative time slot");
                        }
                        break;

                    case ConflictResolutionType.CounterOffer:
                        if (request.CounterOfferStartTime.HasValue && request.CounterOfferEndTime.HasValue)
                        {
                            counterOffer = new CounterOfferInfo
                            {
                                SuggestedStartTime = request.CounterOfferStartTime.Value,
                                SuggestedEndTime = request.CounterOfferEndTime.Value,
                                Reason = request.RejectionReason ?? "Alternative time suggested",
                                IsRequesterAccepted = false
                            };

                            outcome = ConflictResolutionOutcome.CounterOfferMade;
                            resolutionExplanation = $"Counter-offer made: {request.CounterOfferStartTime:g} - {request.CounterOfferEndTime:g}";
                            recommendedActions.Add($"Requester should review counter-offer from {respondingStakeholder?.Name}");
                            recommendedActions.Add("Accept counter-offer or request different time");
                        }
                        else
                        {
                            outcome = ConflictResolutionOutcome.Rejected;
                            resolutionExplanation = "Counter-offer rejected: Invalid time range provided";
                        }
                        break;

                    case ConflictResolutionType.PriorityOverride:
                        // Use priority weight to determine winner
                        var requesterCoOwner = allCoOwners.FirstOrDefault(co => co.UserId == booking.CoOwnerId);
                        var responderCoOwner = allCoOwners.FirstOrDefault(co => co.UserId == userId);

                        if (requesterCoOwner != null && responderCoOwner != null)
                        {
                            var requesterWeight = CalculatePriorityWeight(
                                requesterCoOwner.OwnershipPercentage,
                                stakeholders.First(s => s.UserId == booking.CoOwnerId).UsageHoursThisMonth,
                                false);

                            var responderWeight = respondingStakeholder?.PriorityWeight ?? 0;

                            if (request.IsApproved || requesterWeight > responderWeight)
                            {
                                finalStatus = EBookingStatus.Confirmed;
                                booking.StatusEnum = finalStatus;
                                booking.ApprovedBy = userId;
                                booking.UpdatedAt = DateTime.UtcNow;

                                outcome = ConflictResolutionOutcome.Approved;
                                resolutionExplanation = $"Priority override: Requester has higher priority (Weight: {requesterWeight} vs {responderWeight})";
                            }
                            else
                            {
                                finalStatus = EBookingStatus.Cancelled;
                                booking.StatusEnum = finalStatus;
                                booking.UpdatedAt = DateTime.UtcNow;

                                outcome = ConflictResolutionOutcome.Rejected;
                                resolutionExplanation = $"Priority override: Responder claimed higher priority. {request.PriorityJustification}";
                            }

                            await _unitOfWork.BookingRepository.UpdateAsync(booking);
                        }
                        else
                        {
                            outcome = ConflictResolutionOutcome.Rejected;
                            resolutionExplanation = "Priority override failed: Co-owner data not found";
                        }
                        break;

                    case ConflictResolutionType.AutoNegotiation:
                        if (request.EnableAutoNegotiation)
                        {
                            // Auto-resolve based on ownership weight, usage fairness, and priority
                            var autoResolveResult = await AutoResolveConflictAsync(
                                booking,
                                conflicts,
                                stakeholders,
                                allCoOwners.FirstOrDefault(co => co.UserId == booking.CoOwnerId)?.OwnershipPercentage ?? 0);

                            outcome = autoResolveResult.Outcome;
                            finalStatus = autoResolveResult.FinalStatus;
                            autoResolution = autoResolveResult.AutoResolution;
                            resolutionExplanation = autoResolveResult.Explanation;

                            booking.StatusEnum = finalStatus;
                            if (finalStatus == EBookingStatus.Confirmed)
                            {
                                booking.ApprovedBy = userId;
                            }
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.BookingRepository.UpdateAsync(booking);
                        }
                        else
                        {
                            outcome = ConflictResolutionOutcome.Rejected;
                            resolutionExplanation = "Auto-negotiation disabled";
                        }
                        break;

                    case ConflictResolutionType.ConsensusRequired:
                        // Check if all stakeholders with conflicts have approved
                        var conflictingStakeholders = stakeholders
                            .Where(s => conflicts.Any(c => c.CoOwnerId == s.UserId))
                            .ToList();

                        var allApproved = conflictingStakeholders.All(s => s.HasApproved);

                        if (allApproved && conflictingStakeholders.Any())
                        {
                            finalStatus = EBookingStatus.Confirmed;
                            booking.StatusEnum = finalStatus;
                            booking.ApprovedBy = userId;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.BookingRepository.UpdateAsync(booking);

                            outcome = ConflictResolutionOutcome.Approved;
                            resolutionExplanation = "Consensus reached: All conflicting co-owners approved";
                        }
                        else
                        {
                            outcome = ConflictResolutionOutcome.AwaitingMoreApprovals;
                            resolutionExplanation = $"Awaiting approval from {conflictingStakeholders.Count(s => !s.HasApproved)} more co-owner(s)";
                            recommendedActions.Add("Pending responses from: " +
                                string.Join(", ", conflictingStakeholders.Where(s => !s.HasApproved).Select(s => s.Name)));
                        }
                        break;

                    default:
                        outcome = ConflictResolutionOutcome.Rejected;
                        resolutionExplanation = "Unknown resolution type";
                        break;
                }

                await _unitOfWork.SaveChangesAsync();

                // Calculate approval status
                var approvalStatus = CalculateApprovalStatus(stakeholders, conflicts);

                // Build response
                var response = new BookingConflictResolutionResponse
                {
                    BookingId = booking.Id,
                    Outcome = outcome,
                    FinalStatus = finalStatus,
                    ResolvedBy = respondingStakeholder?.Name ?? "System",
                    ResolvedAt = DateTime.UtcNow,
                    ResolutionExplanation = resolutionExplanation,
                    CounterOffer = counterOffer,
                    Stakeholders = stakeholders,
                    ApprovalStatus = approvalStatus,
                    AutoResolution = autoResolution,
                    RecommendedActions = recommendedActions
                };

                return new BaseResponse<BookingConflictResolutionResponse>
                {
                    StatusCode = 200,
                    Message = outcome switch
                    {
                        ConflictResolutionOutcome.Approved => "BOOKING_CONFLICT_RESOLVED_APPROVED",
                        ConflictResolutionOutcome.Rejected => "BOOKING_CONFLICT_RESOLVED_REJECTED",
                        ConflictResolutionOutcome.CounterOfferMade => "COUNTER_OFFER_MADE",
                        ConflictResolutionOutcome.AutoResolved => "CONFLICT_AUTO_RESOLVED",
                        ConflictResolutionOutcome.AwaitingMoreApprovals => "AWAITING_MORE_APPROVALS",
                        _ => "CONFLICT_RESOLUTION_PROCESSED"
                    },
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingConflictResolutionResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<PendingConflictsResponse>> GetPendingConflictsAsync(
            int userId,
            GetPendingConflictsRequest request)
        {
            try
            {
                // Get user's co-owner record
                var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<PendingConflictsResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_A_CO_OWNER"
                    };
                }

                // Get vehicles user is co-owner of
                var vehicleIds = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .Where(vco => vco.CoOwnerId == coOwner.UserId && vco.StatusEnum == EEContractStatus.Active)
                    .Select(vco => vco.VehicleId)
                    .ToListAsync();

                if (!vehicleIds.Any())
                {
                    return new BaseResponse<PendingConflictsResponse>
                    {
                        StatusCode = 200,
                        Message = "NO_VEHICLES_FOUND",
                        Data = new PendingConflictsResponse
                        {
                            TotalConflicts = 0,
                            RequiringMyAction = 0,
                            Conflicts = new()
                        }
                    };
                }

                // Filter by specific vehicle if requested
                if (request.VehicleId.HasValue)
                {
                    if (!vehicleIds.Contains(request.VehicleId.Value))
                    {
                        return new BaseResponse<PendingConflictsResponse>
                        {
                            StatusCode = 403,
                            Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                        };
                    }
                    vehicleIds = new List<int> { request.VehicleId.Value };
                }

                // Get all pending bookings for these vehicles
                var pendingBookings = await _unitOfWork.BookingRepository.GetQueryable()
                    .Where(b => vehicleIds.Contains(b.VehicleId ?? 0) &&
                               b.StatusEnum == EBookingStatus.Pending)
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                    .OrderBy(b => b.CreatedAt)
                    .ToListAsync();

                var conflicts = new List<ConflictSummary>();
                var requiresMyAction = 0;
                var autoResolvableCount = 0;

                foreach (var booking in pendingBookings)
                {
                    // Get conflicting bookings
                    var conflictingBookings = await _unitOfWork.BookingRepository.GetQueryable()
                        .Where(b => b.VehicleId == booking.VehicleId &&
                                   b.Id != booking.Id &&
                                   b.StatusEnum != EBookingStatus.Cancelled &&
                                   ((booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime) ||
                                    (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime) ||
                                    (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)))
                        .Include(b => b.CoOwner)
                            .ThenInclude(co => co.User)
                        .ToListAsync();

                    if (!conflictingBookings.Any())
                        continue;

                    // Check if I have a conflicting booking
                    var myConflict = conflictingBookings.Any(c => c.CoOwnerId == userId);

                    if (request.OnlyMyConflicts && !myConflict)
                        continue;

                    if (myConflict)
                        requiresMyAction++;

                    // Get all co-owners for auto-resolution preview
                    var vehicleCoOwners = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                        .Where(vco => vco.VehicleId == booking.VehicleId && vco.StatusEnum == EEContractStatus.Active)
                        .Include(vco => vco.CoOwner)
                            .ThenInclude(co => co.User)
                        .ToListAsync();

                    var stakeholders = new List<ConflictStakeholder>();
                    foreach (var vco in vehicleCoOwners)
                    {
                        var usageHours = await GetMonthlyUsageHoursAsync(vco.CoOwnerId, booking.VehicleId ?? 0);
                        stakeholders.Add(new ConflictStakeholder
                        {
                            UserId = vco.CoOwner?.UserId ?? 0,
                            Name = $"{vco.CoOwner?.User?.FirstName} {vco.CoOwner?.User?.LastName}".Trim(),
                            OwnershipPercentage = vco.OwnershipPercentage,
                            UsageHoursThisMonth = usageHours,
                            PriorityWeight = CalculatePriorityWeight(
                                vco.OwnershipPercentage,
                                usageHours,
                                conflictingBookings.Any(c => c.CoOwnerId == vco.CoOwnerId))
                        });
                    }

                    var approvalStatus = CalculateApprovalStatus(stakeholders, conflictingBookings);

                    // Calculate auto-resolution preview
                    var requesterStakeholder = stakeholders.FirstOrDefault(s => s.UserId == booking.CoOwnerId);
                    var canAutoResolve = requesterStakeholder != null;
                    AutoResolutionPreview? autoResolutionPreview = null;

                    if (canAutoResolve && request.IncludeAutoResolvable)
                    {
                        autoResolvableCount++;
                        autoResolutionPreview = GenerateAutoResolutionPreview(
                            stakeholders,
                            booking.CoOwnerId ?? 0,
                            conflictingBookings);
                    }

                    var conflictSummary = new ConflictSummary
                    {
                        BookingId = booking.Id,
                        RequesterName = $"{booking.CoOwner?.User?.FirstName} {booking.CoOwner?.User?.LastName}".Trim(),
                        RequestedStartTime = booking.StartTime,
                        RequestedEndTime = booking.EndTime,
                        Purpose = booking.Purpose ?? "",
                        Priority = BookingPriority.Medium, // Default, would come from request if stored
                        ConflictsWith = conflictingBookings.Select(c =>
                        {
                            var conflictInfo = new DetailedConflictingBookingInfo();
                            conflictInfo.BookingId = c.Id;
                            conflictInfo.CoOwnerName = $"{c.CoOwner?.User?.FirstName} {c.CoOwner?.User?.LastName}".Trim();
                            conflictInfo.StartTime = c.StartTime;
                            conflictInfo.EndTime = c.EndTime;
                            conflictInfo.Status = c.StatusEnum ?? EBookingStatus.Pending;
                            conflictInfo.Purpose = c.Purpose ?? "";
                            conflictInfo.OverlapHours = CalculateOverlapHours(
                                booking.StartTime, booking.EndTime,
                                c.StartTime, c.EndTime);
                            conflictInfo.CoOwnerOwnershipPercentage = stakeholders
                                .FirstOrDefault(s => s.UserId == c.CoOwnerId)?.OwnershipPercentage ?? 0m;
                            conflictInfo.HasResponded = false;
                            return conflictInfo;
                        }).ToList(),
                        RequestedAt = booking.CreatedAt ?? DateTime.UtcNow,
                        DaysPending = (int)(DateTime.UtcNow - (booking.CreatedAt ?? DateTime.UtcNow)).TotalDays,
                        ApprovalStatus = approvalStatus,
                        CanAutoResolve = canAutoResolve,
                        AutoResolutionPreview = autoResolutionPreview
                    };

                    conflicts.Add(conflictSummary);
                }

                var actionItems = new List<string>();
                if (requiresMyAction > 0)
                {
                    actionItems.Add($"You have {requiresMyAction} conflict(s) requiring your response");
                }
                if (autoResolvableCount > 0)
                {
                    actionItems.Add($"{autoResolvableCount} conflict(s) can be auto-resolved");
                }

                var response = new PendingConflictsResponse
                {
                    TotalConflicts = conflicts.Count,
                    RequiringMyAction = requiresMyAction,
                    AutoResolvable = autoResolvableCount,
                    Conflicts = conflicts,
                    OldestConflictDate = conflicts.Any() ? conflicts.Min(c => c.RequestedAt) : DateTime.UtcNow,
                    ActionItems = actionItems
                };

                return new BaseResponse<PendingConflictsResponse>
                {
                    StatusCode = 200,
                    Message = "PENDING_CONFLICTS_RETRIEVED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PendingConflictsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<BookingConflictAnalyticsResponse>> GetConflictAnalyticsAsync(
            int vehicleId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Validate co-owner access
                var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<BookingConflictAnalyticsResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_A_CO_OWNER"
                    };
                }

                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                    vco.CoOwnerId == coOwner.UserId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<BookingConflictAnalyticsResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Set date range (default: last 90 days)
                var start = startDate ?? DateTime.UtcNow.AddDays(-90);
                var end = endDate ?? DateTime.UtcNow;

                // Get all bookings in date range
                var allBookings = await _unitOfWork.BookingRepository.GetQueryable()
                    .Where(b => b.VehicleId == vehicleId &&
                               b.CreatedAt >= start &&
                               b.CreatedAt <= end)
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .ToListAsync();

                // Identify conflicts (bookings that had overlapping pending requests)
                var pendingBookings = allBookings.Where(b => b.StatusEnum == EBookingStatus.Pending).ToList();
                var resolvedBookings = allBookings.Where(b => b.StatusEnum != EBookingStatus.Pending).ToList();

                var totalConflictsResolved = 0;
                var totalConflictsPending = pendingBookings.Count;
                var totalApproved = allBookings.Count(b => b.StatusEnum == EBookingStatus.Confirmed);
                var totalRejected = allBookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled);

                var totalResolutions = totalApproved + totalRejected;
                var approvalRate = totalResolutions > 0 ? (decimal)totalApproved / totalResolutions * 100 : 0;
                var rejectionRate = totalResolutions > 0 ? (decimal)totalRejected / totalResolutions * 100 : 0;

                // Calculate average resolution time
                var resolutionTimes = resolvedBookings
                    .Where(b => b.CreatedAt.HasValue && b.UpdatedAt.HasValue)
                    .Select(b => (b.UpdatedAt!.Value - b.CreatedAt!.Value).TotalHours)
                    .ToList();

                var avgResolutionTime = resolutionTimes.Any() ? (decimal)resolutionTimes.Average() : 0;

                // Statistics by co-owner
                var coOwnerStats = await GetCoOwnerConflictStatsAsync(vehicleId, start, end);

                // Identify common patterns
                var patterns = IdentifyConflictPatterns(allBookings);

                // Generate recommendations
                var recommendations = GenerateConflictRecommendations(patterns, coOwnerStats);

                var response = new BookingConflictAnalyticsResponse
                {
                    TotalConflictsResolved = totalConflictsResolved,
                    TotalConflictsPending = totalConflictsPending,
                    AverageResolutionTimeHours = avgResolutionTime,
                    ApprovalRate = approvalRate,
                    RejectionRate = rejectionRate,
                    AutoResolutionRate = 0, // Would need tracking
                    StatsByCoOwner = coOwnerStats,
                    CommonPatterns = patterns,
                    Recommendations = recommendations
                };

                return new BaseResponse<BookingConflictAnalyticsResponse>
                {
                    StatusCode = 200,
                    Message = "CONFLICT_ANALYTICS_RETRIEVED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<BookingConflictAnalyticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        #endregion

        #region Helper Methods for Conflict Resolution

        private int CalculatePriorityWeight(decimal ownershipPercentage, int usageHoursThisMonth, bool hasConflict)
        {
            // Base weight from ownership (0-50 points)
            var ownershipWeight = (int)(ownershipPercentage / 2);

            // Fairness weight: Less usage = higher priority (0-30 points)
            var fairnessWeight = Math.Max(0, 30 - (usageHoursThisMonth / 10));

            // Conflict penalty (-10 points if has existing booking)
            var conflictPenalty = hasConflict ? -10 : 0;

            return ownershipWeight + fairnessWeight + conflictPenalty;
        }

        private async Task<(ConflictResolutionOutcome Outcome, EBookingStatus FinalStatus, AutoResolutionInfo AutoResolution, string Explanation)>
            AutoResolveConflictAsync(
                Booking requestedBooking,
                List<Booking> conflictingBookings,
                List<ConflictStakeholder> stakeholders,
                decimal requesterOwnership)
        {
            var requesterStakeholder = stakeholders.FirstOrDefault(s => s.UserId == requestedBooking.CoOwnerId);
            if (requesterStakeholder == null)
            {
                return (
                    ConflictResolutionOutcome.Rejected,
                    EBookingStatus.Cancelled,
                    new AutoResolutionInfo
                    {
                        WasAutoResolved = false,
                        Reason = AutoResolutionReason.FirstComeFirstServed,
                        Explanation = "Auto-resolution failed: Requester not found",
                        RequesterPriorityScore = 0,
                        ConflictingOwnerPriorityScore = 0,
                        WinnerName = "Unknown",
                        FactorsConsidered = new()
                    },
                    "Auto-resolution failed: Requester not found");
            }

            var conflictingStakeholders = stakeholders
                .Where(s => conflictingBookings.Any(c => c.CoOwnerId == s.UserId))
                .ToList();

            if (!conflictingStakeholders.Any())
            {
                return (
                    ConflictResolutionOutcome.Approved,
                    EBookingStatus.Confirmed,
                    new AutoResolutionInfo
                    {
                        WasAutoResolved = true,
                        Reason = AutoResolutionReason.FirstComeFirstServed,
                        Explanation = "No conflicts found",
                        RequesterPriorityScore = requesterStakeholder.PriorityWeight,
                        ConflictingOwnerPriorityScore = 0,
                        WinnerName = requesterStakeholder.Name,
                        FactorsConsidered = new() { "No conflicts" }
                    },
                    "Automatically approved - no conflicts");
            }

            // Calculate average conflicting stakeholder priority
            var avgConflictPriority = conflictingStakeholders.Average(s => s.PriorityWeight);

            var factors = new List<string>();
            AutoResolutionReason reason;
            ConflictResolutionOutcome outcome;
            EBookingStatus finalStatus;
            string winner;
            string explanation;

            // Decision logic
            if (requesterStakeholder.PriorityWeight > avgConflictPriority + 10)
            {
                // Requester has significantly higher priority
                if (requesterStakeholder.OwnershipPercentage > 50)
                {
                    reason = AutoResolutionReason.OwnershipWeight;
                    factors.Add($"Requester owns {requesterStakeholder.OwnershipPercentage}% (majority)");
                }
                else if (requesterStakeholder.UsageHoursThisMonth < conflictingStakeholders.Average(s => s.UsageHoursThisMonth))
                {
                    reason = AutoResolutionReason.UsageFairness;
                    factors.Add($"Requester has lower usage this month ({requesterStakeholder.UsageHoursThisMonth}h)");
                }
                else
                {
                    reason = AutoResolutionReason.PriorityLevel;
                    factors.Add($"Requester has higher priority weight ({requesterStakeholder.PriorityWeight})");
                }

                outcome = ConflictResolutionOutcome.AutoResolved;
                finalStatus = EBookingStatus.Confirmed;
                winner = requesterStakeholder.Name;
                explanation = $"Auto-approved: {requesterStakeholder.Name} has priority";

                // Cancel conflicting bookings
                foreach (var conflict in conflictingBookings)
                {
                    conflict.StatusEnum = EBookingStatus.Cancelled;
                    conflict.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.BookingRepository.UpdateAsync(conflict);
                }
            }
            else
            {
                // Conflicting bookings have priority
                reason = AutoResolutionReason.FirstComeFirstServed;
                outcome = ConflictResolutionOutcome.AutoResolved;
                finalStatus = EBookingStatus.Cancelled;
                winner = conflictingStakeholders.First().Name;
                explanation = $"Auto-rejected: Existing bookings have priority";
                factors.Add("Existing bookings take precedence");
            }

            var autoResolution = new AutoResolutionInfo
            {
                WasAutoResolved = true,
                Reason = reason,
                Explanation = explanation,
                RequesterPriorityScore = requesterStakeholder.PriorityWeight,
                ConflictingOwnerPriorityScore = (decimal)avgConflictPriority,
                WinnerName = winner,
                FactorsConsidered = factors
            };

            return (outcome, finalStatus, autoResolution, explanation);
        }

        private ConflictApprovalStatus CalculateApprovalStatus(
            List<ConflictStakeholder> stakeholders,
            List<Booking> conflictingBookings)
        {
            var conflictingStakeholders = stakeholders
                .Where(s => conflictingBookings.Any(c => c.CoOwnerId == s.UserId))
                .ToList();

            var totalStakeholders = conflictingStakeholders.Count;
            var approvalsReceived = conflictingStakeholders.Count(s => s.HasApproved);
            var rejectionsReceived = conflictingStakeholders.Count(s => s.HasRejected);
            var pendingResponses = totalStakeholders - approvalsReceived - rejectionsReceived;

            var approvalPercentage = totalStakeholders > 0
                ? (decimal)approvalsReceived / totalStakeholders * 100
                : 0;

            var weightedApprovalPercentage = totalStakeholders > 0
                ? conflictingStakeholders.Where(s => s.HasApproved).Sum(s => s.OwnershipPercentage)
                : 0;

            var pendingFrom = conflictingStakeholders
                .Where(s => !s.HasApproved && !s.HasRejected)
                .Select(s => s.Name)
                .ToList();

            return new ConflictApprovalStatus
            {
                TotalStakeholders = totalStakeholders,
                ApprovalsReceived = approvalsReceived,
                RejectionsReceived = rejectionsReceived,
                PendingResponses = pendingResponses,
                ApprovalPercentage = approvalPercentage,
                WeightedApprovalPercentage = weightedApprovalPercentage,
                IsFullyApproved = approvalsReceived == totalStakeholders && totalStakeholders > 0,
                IsRejected = rejectionsReceived > 0,
                RequiresMoreApprovals = pendingResponses > 0,
                PendingFrom = pendingFrom
            };
        }

        private AutoResolutionPreview GenerateAutoResolutionPreview(
            List<ConflictStakeholder> stakeholders,
            int requesterId,
            List<Booking> conflictingBookings)
        {
            var requester = stakeholders.FirstOrDefault(s => s.UserId == requesterId);
            if (requester == null)
            {
                return new AutoResolutionPreview
                {
                    PredictedOutcome = ConflictResolutionOutcome.Rejected,
                    WinnerName = "Unknown",
                    Explanation = "Requester not found",
                    Confidence = 0,
                    Factors = new()
                };
            }

            var conflictingStakeholders = stakeholders
                .Where(s => conflictingBookings.Any(c => c.CoOwnerId == s.UserId))
                .ToList();

            var avgConflictPriority = conflictingStakeholders.Any()
                ? conflictingStakeholders.Average(s => s.PriorityWeight)
                : 0;

            var factors = new List<string>
            {
                $"Requester priority: {requester.PriorityWeight}",
                $"Average conflict priority: {avgConflictPriority:F0}",
                $"Requester ownership: {requester.OwnershipPercentage}%",
                $"Requester usage: {requester.UsageHoursThisMonth}h"
            };

            ConflictResolutionOutcome predicted;
            string winner;
            string explanation;
            decimal confidence;

            if (requester.PriorityWeight > avgConflictPriority + 10)
            {
                predicted = ConflictResolutionOutcome.Approved;
                winner = requester.Name;
                explanation = $"{requester.Name} likely to be approved (higher priority)";
                confidence = 0.8m;
            }
            else if (requester.PriorityWeight < avgConflictPriority - 10)
            {
                predicted = ConflictResolutionOutcome.Rejected;
                winner = conflictingStakeholders.FirstOrDefault()?.Name ?? "Existing bookings";
                explanation = "Request likely to be rejected (lower priority)";
                confidence = 0.7m;
            }
            else
            {
                predicted = ConflictResolutionOutcome.AwaitingMoreApprovals;
                winner = "Undecided";
                explanation = "Close call - manual review recommended";
                confidence = 0.5m;
            }

            return new AutoResolutionPreview
            {
                PredictedOutcome = predicted,
                WinnerName = winner,
                Explanation = explanation,
                Confidence = confidence,
                Factors = factors
            };
        }

        private async Task<int> GetMonthlyUsageHoursAsync(int coOwnerId, int vehicleId)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var bookings = await _unitOfWork.BookingRepository.GetQueryable()
                .Where(b => b.CoOwnerId == coOwnerId &&
                           b.VehicleId == vehicleId &&
                           b.StatusEnum != EBookingStatus.Cancelled &&
                           b.StartTime >= startOfMonth)
                .ToListAsync();

            return (int)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
        }

        private async Task<List<CoOwnerConflictStats>> GetCoOwnerConflictStatsAsync(
            int vehicleId,
            DateTime startDate,
            DateTime endDate)
        {
            var stats = new List<CoOwnerConflictStats>();

            var coOwners = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                .Where(vco => vco.VehicleId == vehicleId && vco.StatusEnum == EEContractStatus.Active)
                .Include(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                .ToListAsync();

            foreach (var vco in coOwners)
            {
                var bookings = await _unitOfWork.BookingRepository.GetQueryable()
                    .Where(b => b.CoOwnerId == vco.CoOwnerId &&
                               b.VehicleId == vehicleId &&
                               b.CreatedAt >= startDate &&
                               b.CreatedAt <= endDate)
                    .ToListAsync();

                var initiated = bookings.Count;
                var approved = bookings.Count(b => b.StatusEnum == EBookingStatus.Confirmed);
                var rejected = bookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled);

                stats.Add(new CoOwnerConflictStats
                {
                    UserId = vco.CoOwner?.UserId ?? 0,
                    Name = $"{vco.CoOwner?.User?.FirstName} {vco.CoOwner?.User?.LastName}".Trim(),
                    ConflictsInitiated = initiated,
                    ConflictsReceived = 0, // Would need to track
                    ApprovalsGiven = 0, // Would need to track
                    RejectionsGiven = 0, // Would need to track
                    ApprovalRateAsResponder = 0,
                    SuccessRateAsRequester = initiated > 0 ? (decimal)approved / initiated * 100 : 0,
                    AverageResponseTimeHours = 0
                });
            }

            return stats;
        }

        private List<ConflictPattern> IdentifyConflictPatterns(List<Booking> bookings)
        {
            var patterns = new List<ConflictPattern>();

            // Weekend conflicts
            var weekendConflicts = bookings.Count(b =>
                b.StartTime.DayOfWeek == DayOfWeek.Saturday ||
                b.StartTime.DayOfWeek == DayOfWeek.Sunday);

            if (weekendConflicts > 5)
            {
                patterns.Add(new ConflictPattern
                {
                    Pattern = "High weekend conflict rate",
                    Occurrences = weekendConflicts,
                    Recommendation = "Consider implementing weekend rotation schedule"
                });
            }

            // Morning rush hour conflicts (7-9 AM)
            var morningConflicts = bookings.Count(b =>
                b.StartTime.Hour >= 7 && b.StartTime.Hour <= 9);

            if (morningConflicts > 5)
            {
                patterns.Add(new ConflictPattern
                {
                    Pattern = "Morning commute conflicts (7-9 AM)",
                    Occurrences = morningConflicts,
                    Recommendation = "Establish priority system for commute times"
                });
            }

            return patterns;
        }

        private List<ConflictResolutionRecommendation> GenerateConflictRecommendations(
            List<ConflictPattern> patterns,
            List<CoOwnerConflictStats> stats)
        {
            var recommendations = new List<ConflictResolutionRecommendation>();

            if (patterns.Any(p => p.Pattern.Contains("weekend")))
            {
                recommendations.Add(new ConflictResolutionRecommendation
                {
                    Recommendation = "Implement weekend rotation schedule",
                    Rationale = "High weekend conflict rate detected",
                    SuggestedApproach = ConflictResolutionType.ConsensusRequired
                });
            }

            if (stats.Any(s => s.SuccessRateAsRequester < 50))
            {
                recommendations.Add(new ConflictResolutionRecommendation
                {
                    Recommendation = "Review fairness policies for low-success co-owners",
                    Rationale = "Some co-owners have low approval rates",
                    SuggestedApproach = ConflictResolutionType.AutoNegotiation
                });
            }

            return recommendations;
        }

        #endregion

        #region Booking Modification and Cancellation (Enhanced)

        public async Task<BaseResponse<ModifyBookingResponse>> ModifyBookingAsync(
            int bookingId,
            int userId,
            ModifyBookingRequest request)
        {
            try
            {
                // Get booking with full details
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                        .ThenInclude(v => v.VehicleCoOwners)
                            .ThenInclude(vco => vco.CoOwner)
                                .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<ModifyBookingResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Validate ownership
                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<ModifyBookingResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Only pending or confirmed bookings can be modified
                if (booking.StatusEnum == EBookingStatus.Completed || booking.StatusEnum == EBookingStatus.Cancelled)
                {
                    return new BaseResponse<ModifyBookingResponse>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_MODIFY_COMPLETED_OR_CANCELLED_BOOKING"
                    };
                }

                // Store original booking data
                var originalBooking = new BookingModificationSummary
                {
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Purpose = booking.Purpose ?? "",
                    DurationHours = (int)(booking.EndTime - booking.StartTime).TotalHours,
                    Status = booking.StatusEnum ?? EBookingStatus.Pending
                };

                // Determine new values
                var newStartTime = request.NewStartTime ?? booking.StartTime;
                var newEndTime = request.NewEndTime ?? booking.EndTime;
                var newPurpose = request.NewPurpose ?? booking.Purpose;

                // Analyze impact
                var hasTimeChange = request.NewStartTime.HasValue || request.NewEndTime.HasValue;
                var conflicts = new List<ConflictingBookingInfo>();
                var warnings = new List<string>();

                if (hasTimeChange && !request.SkipConflictCheck)
                {
                    // Check for conflicts
                    var conflictingBookings = await _unitOfWork.BookingRepository.GetQueryable()
                        .Where(b => b.VehicleId == booking.VehicleId &&
                                   b.Id != booking.Id &&
                                   b.StatusEnum != EBookingStatus.Cancelled &&
                                   ((newStartTime >= b.StartTime && newStartTime < b.EndTime) ||
                                    (newEndTime > b.StartTime && newEndTime <= b.EndTime) ||
                                    (newStartTime <= b.StartTime && newEndTime >= b.EndTime)))
                        .Include(b => b.CoOwner)
                            .ThenInclude(co => co.User)
                        .ToListAsync();

                    conflicts = conflictingBookings.Select(c => new ConflictingBookingInfo
                    {
                        BookingId = c.Id,
                        CoOwnerName = $"{c.CoOwner?.User?.FirstName} {c.CoOwner?.User?.LastName}".Trim(),
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        Status = c.StatusEnum ?? EBookingStatus.Pending,
                        Purpose = c.Purpose ?? "",
                        OverlapHours = CalculateOverlapHours(newStartTime, newEndTime, c.StartTime, c.EndTime)
                    }).ToList();

                    if (conflicts.Any())
                    {
                        warnings.Add($"Modification creates {conflicts.Count} conflict(s) with other bookings");
                    }
                }

                // Check if modification is close to booking time
                var hoursUntilBooking = (booking.StartTime - DateTime.UtcNow).TotalHours;
                if (hoursUntilBooking < 2 && hasTimeChange)
                {
                    warnings.Add("Modification within 2 hours of booking start time");
                }

                // Determine status and required approvals
                ModificationStatus modificationStatus;
                var requiredApprovals = new List<string>();

                if (conflicts.Any() && request.RequestApprovalIfConflict)
                {
                    // Requires approval from conflicting co-owners
                    modificationStatus = ModificationStatus.PendingApproval;
                    requiredApprovals = conflicts
                        .Select(c => c.CoOwnerName)
                        .Distinct()
                        .ToList();

                    // Don't apply changes yet, wait for approval
                }
                else if (conflicts.Any() && !request.RequestApprovalIfConflict)
                {
                    // Conflict detected but user doesn't want to request approval
                    modificationStatus = ModificationStatus.ConflictDetected;

                    return new BaseResponse<ModifyBookingResponse>
                    {
                        StatusCode = 409,
                        Message = "MODIFICATION_CREATES_CONFLICTS",
                        Data = new ModifyBookingResponse
                        {
                            BookingId = bookingId,
                            Status = modificationStatus,
                            Message = "Modification creates conflicts. Set RequestApprovalIfConflict=true or choose different time.",
                            OriginalBooking = originalBooking,
                            ImpactAnalysis = new ModificationImpactAnalysis
                            {
                                HasTimeChange = hasTimeChange,
                                HasConflicts = true,
                                ConflictCount = conflicts.Count,
                                ConflictingBookings = conflicts,
                                RequiresCoOwnerApproval = true,
                                ImpactSummary = $"{conflicts.Count} conflict(s) detected"
                            },
                            Warnings = warnings,
                            SuggestedAlternatives = await GenerateAlternativeSlotSuggestionsAsync(
                                booking.VehicleId ?? 0,
                                newStartTime,
                                newEndTime,
                                null)
                        }
                    };
                }
                else
                {
                    // No conflicts, apply changes
                    modificationStatus = ModificationStatus.Success;

                    booking.StartTime = newStartTime;
                    booking.EndTime = newEndTime;
                    booking.Purpose = newPurpose;
                    booking.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.BookingRepository.UpdateAsync(booking);
                    await _unitOfWork.SaveChangesAsync();
                }

                var modifiedBooking = new BookingModificationSummary
                {
                    StartTime = newStartTime,
                    EndTime = newEndTime,
                    Purpose = newPurpose ?? "",
                    DurationHours = (int)(newEndTime - newStartTime).TotalHours,
                    Status = booking.StatusEnum ?? EBookingStatus.Pending
                };

                // Calculate impact
                var timeDelta = Math.Abs((int)(
                    ((newEndTime - newStartTime).TotalHours) -
                    ((originalBooking.EndTime - originalBooking.StartTime).TotalHours)));

                var impactAnalysis = new ModificationImpactAnalysis
                {
                    HasTimeChange = hasTimeChange,
                    HasConflicts = conflicts.Any(),
                    ConflictCount = conflicts.Count,
                    ConflictingBookings = conflicts.Any() ? conflicts : null,
                    TimeDeltaHours = timeDelta,
                    RequiresCoOwnerApproval = requiredApprovals.Any(),
                    ImpactSummary = GenerateModificationImpactSummary(
                        hasTimeChange, conflicts.Count, timeDelta, requiredApprovals.Count)
                };

                // Get co-owners to notify
                var notifiedCoOwners = new List<string>();
                if (request.NotifyAffectedCoOwners && (hasTimeChange || conflicts.Any()))
                {
                    notifiedCoOwners = booking.Vehicle?.VehicleCoOwners
                        .Where(vco => vco.CoOwner?.UserId != userId)
                        .Select(vco => $"{vco.CoOwner?.User?.FirstName} {vco.CoOwner?.User?.LastName}".Trim())
                        .ToList() ?? new();
                }

                var response = new ModifyBookingResponse
                {
                    BookingId = bookingId,
                    Status = modificationStatus,
                    Message = modificationStatus switch
                    {
                        ModificationStatus.Success => "Booking modified successfully",
                        ModificationStatus.PendingApproval => $"Modification pending approval from {requiredApprovals.Count} co-owner(s)",
                        _ => "Modification processed"
                    },
                    OriginalBooking = originalBooking,
                    ModifiedBooking = modifiedBooking,
                    ImpactAnalysis = impactAnalysis,
                    RequiredApprovals = requiredApprovals,
                    NotifiedCoOwners = notifiedCoOwners,
                    ModifiedAt = DateTime.UtcNow,
                    Warnings = warnings
                };

                return new BaseResponse<ModifyBookingResponse>
                {
                    StatusCode = modificationStatus == ModificationStatus.Success ? 200 : 202,
                    Message = modificationStatus == ModificationStatus.Success
                        ? "BOOKING_MODIFIED_SUCCESSFULLY"
                        : "MODIFICATION_PENDING_APPROVAL",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<ModifyBookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<CancelBookingResponse>> CancelBookingEnhancedAsync(
            int bookingId,
            int userId,
            CancelBookingRequest request)
        {
            try
            {
                // Get booking
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(b => b.Vehicle)
                        .ThenInclude(v => v.VehicleCoOwners)
                            .ThenInclude(vco => vco.CoOwner)
                                .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return new BaseResponse<CancelBookingResponse>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Validate ownership
                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<CancelBookingResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                // Cannot cancel completed booking
                if (booking.StatusEnum == EBookingStatus.Completed)
                {
                    return new BaseResponse<CancelBookingResponse>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_CANCEL_COMPLETED_BOOKING"
                    };
                }

                // Already cancelled
                if (booking.StatusEnum == EBookingStatus.Cancelled)
                {
                    return new BaseResponse<CancelBookingResponse>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_ALREADY_CANCELLED"
                    };
                }

                // Calculate cancellation policy
                var hoursUntilBooking = (booking.StartTime - DateTime.UtcNow).TotalHours;
                var policyInfo = CalculateCancellationPolicy(hoursUntilBooking, request.CancellationType);

                // Check if cancellation is allowed
                if (!policyInfo.IsCancellationAllowed && request.CancellationType == CancellationType.UserInitiated)
                {
                    return new BaseResponse<CancelBookingResponse>
                    {
                        StatusCode = 400,
                        Message = "CANCELLATION_NOT_ALLOWED",
                        Data = new CancelBookingResponse
                        {
                            BookingId = bookingId,
                            Status = CancellationStatus.Failed,
                            Message = policyInfo.PolicyRule,
                            PolicyInfo = policyInfo
                        }
                    };
                }

                var cancelledBooking = new BookingModificationSummary
                {
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Purpose = booking.Purpose ?? "",
                    DurationHours = (int)(booking.EndTime - booking.StartTime).TotalHours,
                    Status = booking.StatusEnum ?? EBookingStatus.Pending
                };

                // Handle reschedule request
                List<AlternativeSlotSuggestion>? rescheduleOptions = null;
                if (request.RequestReschedule)
                {
                    if (request.PreferredRescheduleStart.HasValue && request.PreferredRescheduleEnd.HasValue)
                    {
                        // Check availability for preferred reschedule time
                        var hasConflict = await _unitOfWork.BookingRepository.GetQueryable()
                            .AnyAsync(b => b.VehicleId == booking.VehicleId &&
                                          b.Id != booking.Id &&
                                          b.StatusEnum != EBookingStatus.Cancelled &&
                                          ((request.PreferredRescheduleStart >= b.StartTime && request.PreferredRescheduleStart < b.EndTime) ||
                                           (request.PreferredRescheduleEnd > b.StartTime && request.PreferredRescheduleEnd <= b.EndTime)));

                        if (!hasConflict)
                        {
                            // Can reschedule - create new booking
                            var newBooking = new Booking
                            {
                                CoOwnerId = booking.CoOwnerId,
                                VehicleId = booking.VehicleId,
                                StartTime = request.PreferredRescheduleStart.Value,
                                EndTime = request.PreferredRescheduleEnd.Value,
                                Purpose = booking.Purpose + " (Rescheduled)",
                                StatusEnum = EBookingStatus.Pending,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.BookingRepository.AddAsync(newBooking);

                            // Cancel old booking
                            booking.StatusEnum = EBookingStatus.Cancelled;
                            booking.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.BookingRepository.UpdateAsync(booking);
                            await _unitOfWork.SaveChangesAsync();

                            return new BaseResponse<CancelBookingResponse>
                            {
                                StatusCode = 200,
                                Message = "BOOKING_RESCHEDULED_SUCCESSFULLY",
                                Data = new CancelBookingResponse
                                {
                                    BookingId = bookingId,
                                    Status = CancellationStatus.Rescheduled,
                                    Message = $"Booking rescheduled successfully to {request.PreferredRescheduleStart:g}",
                                    CancelledAt = DateTime.UtcNow,
                                    PolicyInfo = policyInfo,
                                    CancelledBooking = cancelledBooking,
                                    RefundInfo = new RefundInfo
                                    {
                                        IsRefundable = true,
                                        RefundAmount = 0,
                                        RefundPercentage = 100,
                                        RefundReason = "Rescheduled - no penalty"
                                    }
                                }
                            };
                        }
                    }

                    // Generate reschedule options
                    rescheduleOptions = await GenerateAlternativeSlotSuggestionsAsync(
                        booking.VehicleId ?? 0,
                        booking.StartTime,
                        booking.EndTime,
                        null);
                }

                // Calculate refund
                var refundInfo = CalculateRefundInfo(
                    booking.TotalCost ?? 0,
                    hoursUntilBooking,
                    request.CancellationType);

                // Cancel booking
                booking.StatusEnum = EBookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Notify co-owners
                var notifiedCoOwners = booking.Vehicle?.VehicleCoOwners
                    .Where(vco => vco.CoOwner?.UserId != userId)
                    .Select(vco => $"{vco.CoOwner?.User?.FirstName} {vco.CoOwner?.User?.LastName}".Trim())
                    .ToList() ?? new();

                var cancellationStatus = policyInfo.CancellationFee > 0
                    ? CancellationStatus.CancelledWithFee
                    : refundInfo.IsRefundable
                        ? CancellationStatus.CancelledWithRefund
                        : CancellationStatus.Cancelled;

                var response = new CancelBookingResponse
                {
                    BookingId = bookingId,
                    Status = cancellationStatus,
                    Message = GenerateCancellationMessage(cancellationStatus, policyInfo),
                    CancelledAt = DateTime.UtcNow,
                    PolicyInfo = policyInfo,
                    CancelledBooking = cancelledBooking,
                    RescheduleOptions = rescheduleOptions,
                    NotifiedCoOwners = notifiedCoOwners,
                    RefundInfo = refundInfo
                };

                return new BaseResponse<CancelBookingResponse>
                {
                    StatusCode = 200,
                    Message = "BOOKING_CANCELLED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<CancelBookingResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<ModificationValidationResult>> ValidateModificationAsync(
            int userId,
            ValidateModificationRequest request)
        {
            try
            {
                var booking = await _unitOfWork.BookingRepository.GetQueryable()
                    .Include(b => b.CoOwner)
                    .Include(b => b.Vehicle)
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId);

                if (booking == null)
                {
                    return new BaseResponse<ModificationValidationResult>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    };
                }

                // Validate ownership
                if (booking.CoOwner?.UserId != userId)
                {
                    return new BaseResponse<ModificationValidationResult>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var validationErrors = new List<string>();
                var warnings = new List<string>();

                // Check if booking can be modified
                if (booking.StatusEnum == EBookingStatus.Completed)
                {
                    validationErrors.Add("Cannot modify completed booking");
                }

                if (booking.StatusEnum == EBookingStatus.Cancelled)
                {
                    validationErrors.Add("Cannot modify cancelled booking");
                }

                var newStartTime = request.NewStartTime ?? booking.StartTime;
                var newEndTime = request.NewEndTime ?? booking.EndTime;

                // Check for conflicts
                var conflicts = await _unitOfWork.BookingRepository.GetQueryable()
                    .Where(b => b.VehicleId == booking.VehicleId &&
                               b.Id != booking.Id &&
                               b.StatusEnum != EBookingStatus.Cancelled &&
                               ((newStartTime >= b.StartTime && newStartTime < b.EndTime) ||
                                (newEndTime > b.StartTime && newEndTime <= b.EndTime) ||
                                (newStartTime <= b.StartTime && newEndTime >= b.EndTime)))
                    .Include(b => b.CoOwner)
                        .ThenInclude(co => co.User)
                    .ToListAsync();

                var hasConflicts = conflicts.Any();
                if (hasConflicts)
                {
                    warnings.Add($"Modification creates {conflicts.Count} conflict(s)");
                }

                // Time validations
                var hoursUntilBooking = (booking.StartTime - DateTime.UtcNow).TotalHours;
                if (hoursUntilBooking < 1)
                {
                    warnings.Add("Modification very close to booking start time (less than 1 hour)");
                }

                // Generate alternatives if conflicts
                List<AlternativeSlotSuggestion>? alternatives = null;
                if (hasConflicts)
                {
                    alternatives = await GenerateAlternativeSlotSuggestionsAsync(
                        booking.VehicleId ?? 0,
                        newStartTime,
                        newEndTime,
                        null);
                }

                var impactAnalysis = new ModificationImpactAnalysis
                {
                    HasTimeChange = request.NewStartTime.HasValue || request.NewEndTime.HasValue,
                    HasConflicts = hasConflicts,
                    ConflictCount = conflicts.Count,
                    ConflictingBookings = conflicts.Select(c => new ConflictingBookingInfo
                    {
                        BookingId = c.Id,
                        CoOwnerName = $"{c.CoOwner?.User?.FirstName} {c.CoOwner?.User?.LastName}".Trim(),
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        Status = c.StatusEnum ?? EBookingStatus.Pending,
                        Purpose = c.Purpose ?? "",
                        OverlapHours = CalculateOverlapHours(newStartTime, newEndTime, c.StartTime, c.EndTime)
                    }).ToList(),
                    RequiresCoOwnerApproval = hasConflicts,
                    ImpactSummary = hasConflicts
                        ? $"{conflicts.Count} conflict(s) - requires co-owner approval"
                        : "No conflicts - modification can proceed"
                };

                var recommendation = GenerateModificationRecommendation(
                    hasConflicts, hoursUntilBooking, conflicts.Count);

                var result = new ModificationValidationResult
                {
                    IsValid = !validationErrors.Any(),
                    HasConflicts = hasConflicts,
                    ValidationErrors = validationErrors,
                    Warnings = warnings,
                    ImpactAnalysis = impactAnalysis,
                    AlternativeSuggestions = alternatives,
                    Recommendation = recommendation
                };

                return new BaseResponse<ModificationValidationResult>
                {
                    StatusCode = 200,
                    Message = result.IsValid ? "VALIDATION_PASSED" : "VALIDATION_FAILED",
                    Data = result
                };
            }
            catch (Exception)
            {
                return new BaseResponse<ModificationValidationResult>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        public async Task<BaseResponse<ModificationHistoryResponse>> GetModificationHistoryAsync(
            GetModificationHistoryRequest request)
        {
            try
            {
                // This would require a BookingHistory table to track modifications
                // For now, return a placeholder response
                var response = new ModificationHistoryResponse
                {
                    TotalModifications = 0,
                    TotalCancellations = 0,
                    History = new()
                };

                return new BaseResponse<ModificationHistoryResponse>
                {
                    StatusCode = 200,
                    Message = "MODIFICATION_HISTORY_RETRIEVED",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<ModificationHistoryResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = "An error occurred while processing the request."
                };
            }
        }

        #endregion

        #region Helper Methods for Modification & Cancellation

        private CancellationPolicyInfo CalculateCancellationPolicy(
            double hoursUntilBooking,
            CancellationType cancellationType)
        {
            // Cancellation policy rules
            if (cancellationType != CancellationType.UserInitiated)
            {
                // Emergency/system cancellations have no penalty
                return new CancellationPolicyInfo
                {
                    IsCancellationAllowed = true,
                    CancellationFee = 0,
                    HoursUntilBooking = (int)hoursUntilBooking,
                    PolicyRule = "Emergency cancellation - no penalty",
                    IsWithinGracePeriod = true,
                    GracePeriodInfo = "Emergency cancellations are always allowed"
                };
            }

            // User-initiated cancellation policies
            if (hoursUntilBooking >= 24)
            {
                // Free cancellation if more than 24h before booking
                return new CancellationPolicyInfo
                {
                    IsCancellationAllowed = true,
                    CancellationFee = 0,
                    HoursUntilBooking = (int)hoursUntilBooking,
                    PolicyRule = "Free cancellation (24+ hours before booking)",
                    IsWithinGracePeriod = true,
                    GracePeriodInfo = "You can cancel free of charge up to 24 hours before booking"
                };
            }
            else if (hoursUntilBooking >= 2)
            {
                // 25% fee if 2-24h before booking
                return new CancellationPolicyInfo
                {
                    IsCancellationAllowed = true,
                    CancellationFee = 0.25m, // 25% of booking cost
                    HoursUntilBooking = (int)hoursUntilBooking,
                    PolicyRule = "25% cancellation fee (2-24 hours before booking)",
                    IsWithinGracePeriod = false,
                    GracePeriodInfo = "Cancellation within 24h incurs 25% fee"
                };
            }
            else if (hoursUntilBooking >= 0)
            {
                // 50% fee if less than 2h before booking
                return new CancellationPolicyInfo
                {
                    IsCancellationAllowed = true,
                    CancellationFee = 0.50m, // 50% of booking cost
                    HoursUntilBooking = (int)hoursUntilBooking,
                    PolicyRule = "50% cancellation fee (less than 2 hours before booking)",
                    IsWithinGracePeriod = false,
                    GracePeriodInfo = "Late cancellation incurs 50% fee"
                };
            }
            else
            {
                // Cannot cancel after booking has started
                return new CancellationPolicyInfo
                {
                    IsCancellationAllowed = false,
                    CancellationFee = 1.0m, // 100% penalty
                    HoursUntilBooking = (int)hoursUntilBooking,
                    PolicyRule = "Cannot cancel booking after it has started",
                    IsWithinGracePeriod = false,
                    GracePeriodInfo = "Active bookings cannot be cancelled"
                };
            }
        }

        private RefundInfo CalculateRefundInfo(
            decimal totalCost,
            double hoursUntilBooking,
            CancellationType cancellationType)
        {
            if (cancellationType != CancellationType.UserInitiated)
            {
                // Full refund for non-user cancellations
                return new RefundInfo
                {
                    IsRefundable = true,
                    RefundAmount = totalCost,
                    RefundPercentage = 100,
                    RefundReason = $"{cancellationType} - full refund",
                    EstimatedRefundDate = DateTime.UtcNow.AddDays(3)
                };
            }

            // User-initiated refund based on timing
            if (hoursUntilBooking >= 24)
            {
                return new RefundInfo
                {
                    IsRefundable = true,
                    RefundAmount = totalCost,
                    RefundPercentage = 100,
                    RefundReason = "Cancelled 24+ hours in advance",
                    EstimatedRefundDate = DateTime.UtcNow.AddDays(3)
                };
            }
            else if (hoursUntilBooking >= 2)
            {
                return new RefundInfo
                {
                    IsRefundable = true,
                    RefundAmount = totalCost * 0.75m,
                    RefundPercentage = 75,
                    RefundReason = "Cancelled 2-24 hours in advance (25% fee)",
                    EstimatedRefundDate = DateTime.UtcNow.AddDays(5)
                };
            }
            else if (hoursUntilBooking >= 0)
            {
                return new RefundInfo
                {
                    IsRefundable = true,
                    RefundAmount = totalCost * 0.50m,
                    RefundPercentage = 50,
                    RefundReason = "Late cancellation (50% fee)",
                    EstimatedRefundDate = DateTime.UtcNow.AddDays(7)
                };
            }
            else
            {
                return new RefundInfo
                {
                    IsRefundable = false,
                    RefundAmount = 0,
                    RefundPercentage = 0,
                    RefundReason = "No refund for cancellations after booking start",
                    EstimatedRefundDate = null
                };
            }
        }

        private string GenerateModificationImpactSummary(
            bool hasTimeChange,
            int conflictCount,
            int timeDelta,
            int approvalCount)
        {
            var parts = new List<string>();

            if (hasTimeChange)
            {
                parts.Add($"Time changed by {timeDelta} hour(s)");
            }

            if (conflictCount > 0)
            {
                parts.Add($"{conflictCount} conflict(s) detected");
            }

            if (approvalCount > 0)
            {
                parts.Add($"Requires {approvalCount} approval(s)");
            }

            return parts.Any()
                ? string.Join(". ", parts)
                : "Minor modification with no significant impact";
        }

        private string GenerateCancellationMessage(
            CancellationStatus status,
            CancellationPolicyInfo policy)
        {
            return status switch
            {
                CancellationStatus.Cancelled => "Booking cancelled successfully",
                CancellationStatus.CancelledWithFee =>
                    $"Booking cancelled with {policy.CancellationFee * 100}% cancellation fee",
                CancellationStatus.CancelledWithRefund =>
                    $"Booking cancelled. Refund will be processed within 3-7 business days",
                CancellationStatus.Rescheduled => "Booking rescheduled successfully",
                _ => "Booking cancellation processed"
            };
        }

        private string GenerateModificationRecommendation(
            bool hasConflicts,
            double hoursUntilBooking,
            int conflictCount)
        {
            if (hoursUntilBooking < 1)
            {
                return "⚠️ Very close to booking time. Contact vehicle owner directly for urgent changes.";
            }

            if (hasConflicts)
            {
                return $"⚠️ Modification creates {conflictCount} conflict(s). Consider alternative time slots or request co-owner approval.";
            }

            if (hoursUntilBooking < 24)
            {
                return "⚠️ Modification within 24 hours. Changes may incur fees or require approval.";
            }

            return "✅ Modification can proceed without issues.";
        }

        #endregion
    }
}

