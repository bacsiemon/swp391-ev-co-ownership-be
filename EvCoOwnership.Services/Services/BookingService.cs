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
                                    vco.StatusEnum == EContractStatus.Active);

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
            catch (Exception ex)
            {
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return new BaseResponse<BookingCalendarResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
            catch (Exception ex)
            {
                return new BaseResponse<VehicleAvailabilityResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
                    vco.CoOwnerId == userId && vco.StatusEnum == EContractStatus.Active);

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
            catch (Exception ex)
            {
                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
                                    vco.StatusEnum == EContractStatus.Active);

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
            catch (Exception ex)
            {
                return new BaseResponse<BookingSlotRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
            catch (Exception ex)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
                                    vco.StatusEnum == EContractStatus.Active);

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
            catch (Exception ex)
            {
                return new BaseResponse<PendingSlotRequestsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
                                    vco.StatusEnum == EContractStatus.Active);

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
            catch (Exception ex)
            {
                return new BaseResponse<SlotRequestAnalytics>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
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
    }
}
