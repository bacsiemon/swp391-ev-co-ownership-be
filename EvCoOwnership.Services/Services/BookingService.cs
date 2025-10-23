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
    }
}
