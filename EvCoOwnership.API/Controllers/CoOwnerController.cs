using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.DTOs.FundDTOs;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;
using EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs;
using EvCoOwnership.Repositories.DTOs.ScheduleDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;
using EvCoOwnership.Repositories.DTOs.UserDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing Co-owner operations in the EV Co-ownership system
    /// </summary>
    [Route("api/coowner")]
    [ApiController]
    [AuthorizeRoles(EUserRole.CoOwner)]
    public class CoOwnerController : ControllerBase
    {
        private readonly ICoOwnerEligibilityService _coOwnerEligibilityService;
        private readonly IUserProfileService _userProfileService;
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        private readonly IGroupService _groupService;
        private readonly IUsageAnalyticsService _analyticsService;
        private readonly IFundService _fundService;
        // private readonly IOwnershipChangeService _ownershipService;
        private readonly IScheduleService _scheduleService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CoOwnerController> _logger;

        /// <summary>
        /// Constructor for CoOwnerController
        /// </summary>
        /// <param name="coOwnerEligibilityService">Co-owner eligibility service</param>
        /// <param name="userProfileService">User profile service</param>
        /// <param name="bookingService">Booking service</param>
        /// <param name="paymentService">Payment service</param>
        /// <param name="groupService">Group service</param>
        /// <param name="analyticsService">Analytics service</param>
        /// <param name="fundService">Fund service for managing group funds</param>
        /// <param name="scheduleService">Schedule service for managing vehicle schedules</param>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger</param>
        public CoOwnerController(
            ICoOwnerEligibilityService coOwnerEligibilityService,
            IUserProfileService userProfileService,
            IBookingService bookingService,
            IPaymentService paymentService,
            IGroupService groupService,
            IUsageAnalyticsService analyticsService,
            IFundService fundService,
            // IOwnershipChangeService ownershipService,
            IScheduleService scheduleService,
            IUnitOfWork unitOfWork,
            ILogger<CoOwnerController> logger)
        {
            _coOwnerEligibilityService = coOwnerEligibilityService;
            _userProfileService = userProfileService;
            _bookingService = bookingService;
            _paymentService = paymentService;
            _groupService = groupService;
            _analyticsService = analyticsService;
            _fundService = fundService;
            // _ownershipService = ownershipService;
            _scheduleService = scheduleService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // --- Tài khoản & xác thực ---
        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Đăng ký tài khoản CoOwner mới
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "fullName": "John Doe",
        ///   "email": "john.doe@example.com",
        ///   "phoneNumber": "0123456789",
        ///   "dateOfBirth": "1990-01-01",
        ///   "address": "123 Main St, City"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Đăng ký thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="409">Email đã tồn tại</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] object registrationData)
        {
            try
            {
                // Mock response - cần implement AuthService.RegisterCoOwnerAsync
                return StatusCode(201, new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "CO_OWNER_REGISTRATION_SUCCESS",
                    Data = new
                    {
                        UserId = new Random().Next(1000, 9999),
                        Message = "Co-owner registration successful. Please verify your email."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during co-owner registration");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy thông tin profile của CoOwner hiện tại
        /// </remarks>
        /// <response code="200">Lấy profile thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Không tìm thấy profile</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var profile = await _userProfileService.GetUserProfileAsync(userId);

                return profile.StatusCode switch
                {
                    200 => Ok(profile),
                    404 => NotFound(profile),
                    _ => StatusCode(500, profile)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Cập nhật thông tin profile
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "fullName": "John Doe Updated",
        ///   "phoneNumber": "0987654321",
        ///   "address": "456 New St, City",
        ///   "avatar": "base64_image_string"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get user from database
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    });
                }

                // Update user properties if provided
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.Phone))
                    user.Phone = request.Phone;

                if (!string.IsNullOrEmpty(request.Address))
                    user.Address = request.Address;

                if (request.DateOfBirth.HasValue)
                    user.DateOfBirth = DateOnly.FromDateTime(request.DateOfBirth.Value);

                if (!string.IsNullOrEmpty(request.AvatarUrl))
                    user.ProfileImageUrl = request.AvatarUrl;

                // Note: Bio field doesn't exist in User model, skip it
                // if (!string.IsNullOrEmpty(request.Bio))
                //     user.Bio = request.Bio;

                user.UpdatedAt = DateTime.UtcNow;

                // Save changes
                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PROFILE_UPDATED_SUCCESS",
                    Data = new
                    {
                        UserId = userId,
                        UpdatedAt = user.UpdatedAt,
                        Message = "Profile updated successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy thông tin ownership của người dùng hiện tại
        /// </remarks>
        /// <response code="200">Lấy thông tin ownership thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("ownership")]
        public async Task<IActionResult> GetOwnership()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get real ownership data from database
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                var vehicleOwnerships = await _unitOfWork.VehicleCoOwnerRepository.GetAllAsync();
                var userVehicleOwnerships = vehicleOwnerships
                    .Where(vco => vco.CoOwnerId == coOwner.UserId)
                    .ToList();

                var vehicles = await _unitOfWork.VehicleRepository.GetAllAsync();
                var ownedVehicles = userVehicleOwnerships
                    .Join(vehicles, vco => vco.VehicleId, v => v.Id, (vco, v) => new
                    {
                        Id = v.Id,
                        Model = $"{v.Brand} {v.Model}",
                        Share = vco.OwnershipPercentage,
                        Status = vco.StatusEnum?.ToString() ?? "Unknown",
                        InvestmentAmount = vco.InvestmentAmount
                    })
                    .ToArray();

                var totalShares = userVehicleOwnerships.Sum(vco => vco.OwnershipPercentage);
                var totalValue = userVehicleOwnerships.Sum(vco => vco.InvestmentAmount);

                var ownership = new
                {
                    UserId = userId,
                    Vehicles = ownedVehicles,
                    TotalShares = totalShares,
                    TotalValue = totalValue
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_INFO_RETRIEVED_SUCCESS",
                    Data = ownership
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ownership info");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Đặt lịch & sử dụng xe ---
        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy lịch trình sử dụng xe của CoOwner
        /// </remarks>
        /// <response code="200">Lấy lịch trình thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get real booking data from database
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var userBookings = allBookings
                    .Where(b => b.CoOwnerId == coOwner.UserId &&
                               b.StartTime > DateTime.UtcNow) // Only upcoming bookings
                    .OrderBy(b => b.StartTime)
                    .ToList();

                var vehicles = await _unitOfWork.VehicleRepository.GetAllAsync();
                var bookingList = userBookings
                    .Join(vehicles, b => b.VehicleId, v => v.Id, (b, v) => new
                    {
                        Id = b.Id,
                        VehicleModel = $"{v.Brand} {v.Model}",
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Status = b.StatusEnum?.ToString() ?? "Unknown",
                        Purpose = b.Purpose ?? "No purpose specified"
                    })
                    .ToArray();

                var schedule = new
                {
                    UserId = userId,
                    Bookings = bookingList,
                    TotalBookings = bookingList.Length
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SCHEDULE_RETRIEVED_SUCCESS",
                    Data = schedule
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user schedule");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Đặt lịch sử dụng xe
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "startTime": "2025-11-02T09:00:00",
        ///   "endTime": "2025-11-02T17:00:00",
        ///   "purpose": "Business trip",
        ///   "notes": "Need to pick up clients"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Đặt lịch thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc xe không khả dụng</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("booking")]
        public async Task<IActionResult> BookVehicle([FromBody] CreateBookingRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get co-owner info
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                // Check vehicle exists and is available
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    });
                }

                if (vehicle.StatusEnum != EVehicleStatus.Available)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "VEHICLE_NOT_AVAILABLE"
                    });
                }

                // Check for booking conflicts
                var existingBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var conflictingBooking = existingBookings.Any(b =>
                    b.VehicleId == request.VehicleId &&
                    b.StatusEnum != EBookingStatus.Cancelled &&
                    b.StatusEnum != EBookingStatus.Completed &&
                    ((request.StartTime >= b.StartTime && request.StartTime < b.EndTime) ||
                     (request.EndTime > b.StartTime && request.EndTime <= b.EndTime) ||
                     (request.StartTime <= b.StartTime && request.EndTime >= b.EndTime)));

                if (conflictingBooking)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "BOOKING_TIME_CONFLICT"
                    });
                }

                // Create new booking
                var booking = new Booking
                {
                    CoOwnerId = coOwner.UserId,
                    VehicleId = request.VehicleId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Purpose = request.Purpose,
                    StatusEnum = EBookingStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.BookingRepository.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return StatusCode(201, new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "BOOKING_CREATED_SUCCESS",
                    Data = new
                    {
                        BookingId = booking.Id,
                        UserId = userId,
                        VehicleId = request.VehicleId,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime,
                        Purpose = request.Purpose,
                        Status = booking.StatusEnum.ToString(),
                        BookedAt = booking.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy lịch sử đặt xe của CoOwner
        /// </remarks>
        /// <response code="200">Lấy lịch sử thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("booking/history")]
        public async Task<IActionResult> GetBookingHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get real booking history from database
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var userBookings = allBookings
                    .Where(b => b.CoOwnerId == coOwner.UserId)
                    .OrderByDescending(b => b.StartTime)
                    .ToList();

                var totalCount = userBookings.Count;
                var pagedBookings = userBookings
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var vehicles = await _unitOfWork.VehicleRepository.GetAllAsync();
                var bookingHistoryItems = pagedBookings
                    .Join(vehicles, b => b.VehicleId, v => v.Id, (b, v) => new
                    {
                        Id = b.Id,
                        VehicleModel = $"{v.Brand} {v.Model}",
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Status = b.StatusEnum?.ToString() ?? "Unknown",
                        Cost = b.TotalCost ?? 0,
                        Purpose = b.Purpose ?? "No purpose specified"
                    })
                    .ToArray();

                var history = new
                {
                    Items = bookingHistoryItems,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "BOOKING_HISTORY_RETRIEVED_SUCCESS",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking history");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Chi phí & thanh toán ---
        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy thông tin chi phí của CoOwner
        /// </remarks>
        /// <response code="200">Lấy thông tin chi phí thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("costs")]
        public async Task<IActionResult> GetCosts([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get payments from database for this user
                var payments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var userPayments = payments.Where(p => p.UserId == userId);

                // Get maintenance costs
                var maintenanceCosts = (await _unitOfWork.MaintenanceCostRepository.GetAllAsync()).AsQueryable();

                // Filter by date range if provided
                if (fromDate.HasValue && toDate.HasValue)
                {
                    userPayments = userPayments.Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate);
                    maintenanceCosts = maintenanceCosts.Where(mc => mc.ServiceDate >= DateOnly.FromDateTime(fromDate.Value) &&
                                                                   mc.ServiceDate <= DateOnly.FromDateTime(toDate.Value));
                }

                // Calculate totals
                var totalPaid = userPayments.Where(p => p.StatusEnum == EPaymentStatus.Completed).Sum(p => p.Amount);
                var totalDue = maintenanceCosts.Sum(mc => mc.Cost);
                var remainingBalance = totalDue - totalPaid;

                // Group by month for breakdown
                var monthlyBreakdown = userPayments
                    .GroupBy(p => new { p.CreatedAt?.Year, p.CreatedAt?.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                        Amount = g.Sum(p => p.Amount),
                        Status = g.All(p => p.StatusEnum == EPaymentStatus.Completed) ? "Paid" : "Pending"
                    })
                    .ToArray();

                // Calculate categories based on maintenance types
                var maintenanceAmount = maintenanceCosts.Where(mc => mc.MaintenanceTypeEnum == EMaintenanceType.Routine).Sum(mc => mc.Cost);
                var repairAmount = maintenanceCosts.Where(mc => mc.MaintenanceTypeEnum == EMaintenanceType.Repair).Sum(mc => mc.Cost);

                var costs = new
                {
                    UserId = userId,
                    TotalDue = totalDue,
                    PaidAmount = totalPaid,
                    RemainingBalance = remainingBalance,
                    MonthlyBreakdown = monthlyBreakdown,
                    Categories = new
                    {
                        Maintenance = maintenanceAmount,
                        Repairs = repairAmount,
                        Insurance = 0, // Can be calculated from specific cost categories
                        Other = totalDue - maintenanceAmount - repairAmount
                    }
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "COST_INFO_RETRIEVED_SUCCESS",
                    Data = costs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost information");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Thực hiện thanh toán
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "amount": 500000,
        ///   "paymentMethod": "VnPay",
        ///   "description": "Monthly maintenance fee"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Thanh toán thành công</response>
        /// <response code="400">Dữ liệu thanh toán không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("payment")]
        public async Task<IActionResult> MakePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    });
                }

                // Create new payment record
                var payment = new Payment
                {
                    UserId = userId,
                    Amount = request.Amount,
                    PaymentGateway = request.PaymentGateway.ToString(),
                    StatusEnum = EPaymentStatus.Pending,
                    FundAdditionId = request.FundAdditionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // For real implementation, you would integrate with payment gateway here
                // For now, simulate immediate success
                payment.StatusEnum = EPaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;

                _unitOfWork.PaymentRepository.Update(payment);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PAYMENT_PROCESSED_SUCCESS",
                    Data = new
                    {
                        PaymentId = payment.Id,
                        UserId = userId,
                        Amount = payment.Amount,
                        PaymentGateway = payment.PaymentGateway,
                        Status = payment.StatusEnum.ToString(),
                        PaidAt = payment.PaidAt,
                        CreatedAt = payment.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Nhóm đồng sở hữu ---
        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy thông tin nhóm đồng sở hữu của CoOwner
        /// </remarks>
        /// <response code="200">Lấy thông tin nhóm thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="404">Chưa tham gia nhóm nào</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("group")]
        public async Task<IActionResult> GetGroup()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get co-owner from database
                var coOwners = await _unitOfWork.CoOwnerRepository.GetAllAsync();
                var currentCoOwner = coOwners.FirstOrDefault(co => co.UserId == userId);

                if (currentCoOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                // Get users and group information from database
                var users = await _unitOfWork.UserRepository.GetAllAsync();
                var coOwnersInGroup = coOwners.ToList();

                var members = coOwnersInGroup.Select(co => new
                {
                    Id = co.UserId,
                    Name = users.FirstOrDefault(u => u.Id == co.UserId)?.FirstName + " " +
                           users.FirstOrDefault(u => u.Id == co.UserId)?.LastName,
                    Role = co.UserId == userId ? "Member" : "Member", // You can enhance this based on your group role system
                    SharePercentage = 100 / coOwnersInGroup.Count // Equal shares for now
                }).ToArray();

                // Get vehicles from database
                var vehicles = await _unitOfWork.VehicleRepository.GetAllAsync();
                var vehicleList = vehicles.Select(v => new
                {
                    Id = v.Id,
                    Model = $"{v.Brand} {v.Model}",
                    Status = v.StatusEnum?.ToString() ?? "Unknown"
                }).ToArray();

                // Get fund information
                var funds = await _unitOfWork.FundRepository.GetAllAsync();
                var groupFund = funds.FirstOrDefault();

                var fundInfo = new
                {
                    TotalAmount = groupFund?.CurrentBalance ?? 0,
                    AvailableAmount = groupFund?.CurrentBalance ?? 0,
                    PendingExpenses = 0 // Calculate from pending fund usages
                };

                var group = new
                {
                    Id = 1,
                    Name = "EV Owners Group", // You can enhance this to get actual group name
                    Members = members,
                    Vehicles = vehicleList,
                    Fund = fundInfo
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_INFO_RETRIEVED_SUCCESS",
                    Data = group
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group information");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Mời thành viên mới vào nhóm
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "email": "newmember@example.com",
        ///   "sharePercentage": 10,
        ///   "message": "Welcome to our EV group!"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Gửi lời mời thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền mời thành viên</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("group/invite")]
        public async Task<IActionResult> InviteMember([FromBody] object inviteData)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MEMBER_INVITE_SENT_SUCCESS",
                    Data = new
                    {
                        InviteId = new Random().Next(1000, 9999),
                        SentBy = userId,
                        SentAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(7)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending member invite");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Xóa thành viên khỏi nhóm (chỉ leader)
        /// </remarks>
        /// <response code="200">Xóa thành viên thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền xóa thành viên</response>
        /// <response code="404">Không tìm thấy thành viên</response>
        /// <response code="500">Lỗi server</response>
        [HttpDelete("group/member/{id}")]
        public async Task<IActionResult> RemoveMember(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MEMBER_REMOVED_SUCCESS",
                    Data = new
                    {
                        RemovedMemberId = id,
                        RemovedBy = userId,
                        RemovedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {MemberId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Tham gia bỏ phiếu trong nhóm
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "voteId": 123,
        ///   "choice": "approve",
        ///   "comment": "I agree with this proposal"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Bỏ phiếu thành công</response>
        /// <response code="400">Dữ liệu bỏ phiếu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền bỏ phiếu</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("group/vote")]
        public async Task<IActionResult> Vote([FromBody] object voteData)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "VOTE_SUBMITTED_SUCCESS",
                    Data = new
                    {
                        VoteId = new Random().Next(1000, 9999),
                        VotedBy = userId,
                        VotedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting vote");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy thông tin quỹ nhóm
        /// </remarks>
        /// <response code="200">Lấy thông tin quỹ thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("group/fund")]
        public async Task<IActionResult> GetGroupFund()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get co-owner info
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                // Get all funds (assuming co-owner is part of a group with shared funds)
                var funds = await _unitOfWork.FundRepository.GetAllAsync();
                var groupFunds = funds.ToList();

                // Calculate total amounts
                var totalAmount = groupFunds.Sum(f => f.CurrentBalance ?? 0);
                var availableAmount = totalAmount; // All funds are available since no status filtering

                // Get fund additions for monthly contributions
                var fundAdditions = await _unitOfWork.FundAdditionRepository.GetAllAsync();
                var monthlyContributions = fundAdditions
                    .Where(fa => fa.CreatedAt.HasValue && fa.CreatedAt.Value >= DateTime.UtcNow.AddMonths(-6))
                    .GroupBy(fa => new { Year = fa.CreatedAt!.Value.Year, Month = fa.CreatedAt!.Value.Month })
                    .Select(g => new
                    {
                        Month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMMM yyyy}",
                        Amount = g.Sum(fa => fa.Amount),
                        Status = "Collected"
                    })
                    .OrderByDescending(x => x.Month)
                    .Take(6)
                    .ToArray();

                // Get recent payments as transactions
                var payments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var recentTransactions = payments
                    .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .Select(p => new
                    {
                        Date = p.CreatedAt,
                        Description = $"Payment - {p.StatusEnum}",
                        Amount = p.Amount,
                        Type = "Income" // Payments are typically income to the fund
                    })
                    .ToArray();

                var fund = new
                {
                    TotalAmount = totalAmount,
                    AvailableAmount = availableAmount,
                    PendingExpenses = totalAmount - availableAmount, // Difference as pending
                    MonthlyContributions = monthlyContributions,
                    RecentTransactions = recentTransactions
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_FUND_INFO_RETRIEVED_SUCCESS",
                    Data = fund
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group fund information");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Phân tích & AI ---
        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Lấy phân tích sử dụng và đề xuất AI
        /// </remarks>
        /// <response code="200">Lấy phân tích thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get bookings from database
                var bookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var userBookings = bookings.Where(b => b.CoOwnerId != null);

                // Get co-owner to match user (note: CoOwner uses UserId as primary key)
                var coOwners = await _unitOfWork.CoOwnerRepository.GetAllAsync();
                var currentCoOwner = coOwners.FirstOrDefault(co => co.UserId == userId);

                if (currentCoOwner != null)
                {
                    userBookings = userBookings.Where(b => b.CoOwnerId == currentCoOwner.UserId);
                }

                // Filter by date range if provided
                if (fromDate.HasValue && toDate.HasValue)
                {
                    userBookings = userBookings.Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate);
                }

                // Calculate usage statistics
                var totalBookings = userBookings.Count();
                var completedBookings = userBookings.Where(b => b.StatusEnum == EBookingStatus.Completed);

                // Calculate total hours based on StartTime and EndTime
                var totalHours = completedBookings
                    .Sum(b => (b.EndTime - b.StartTime).TotalHours);

                // For distance, we'll need to calculate from checkin/checkout or use TotalCost as proxy
                var totalCost = completedBookings.Sum(b => b.TotalCost ?? 0);
                var estimatedDistance = (double)(totalCost / 1000); // Rough estimate: 1000 VND per km

                // Get payments for cost analysis
                var payments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var userPayments = payments.Where(p => p.UserId == userId);

                if (fromDate.HasValue && toDate.HasValue)
                {
                    userPayments = userPayments.Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate);
                }

                var totalPaymentAmount = userPayments.Sum(p => p.Amount);
                var monthlyAverage = userPayments.Any() ? totalPaymentAmount / Math.Max(1, userPayments.GroupBy(p => new { p.CreatedAt?.Year, p.CreatedAt?.Month }).Count()) : 0;
                var costPerKm = estimatedDistance > 0 ? (double)(totalPaymentAmount / (decimal)estimatedDistance) : 0;

                // Get peak usage times from bookings
                var peakUsageTimes = userBookings
                    .GroupBy(b => b.StartTime.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToArray();

                var analytics = new
                {
                    UserId = userId,
                    UsageStats = new
                    {
                        TotalBookings = totalBookings,
                        TotalHours = Math.Round(totalHours, 1),
                        TotalDistance = Math.Round(estimatedDistance, 1),
                        AverageRating = 4.5 // Could be calculated from actual ratings
                    },
                    CostAnalysis = new
                    {
                        MonthlyAverage = Math.Round(monthlyAverage, 0),
                        CostPerKm = Math.Round(costPerKm, 0),
                        SavingsVsOwnership = Math.Round(monthlyAverage * 0.3m, 0) // Estimated 30% savings
                    },
                    Recommendations = new[]
                    {
                        totalBookings > 10 ? "You're an active user! Consider premium membership." : "Try booking more frequently for better rates",
                        peakUsageTimes.Any() && peakUsageTimes.First().Hour >= 17 ? "Consider booking during off-peak hours for better rates" : "Great timing on your bookings!",
                        estimatedDistance > 1000 ? "You could save more with longer booking periods" : "Short trips are cost-effective with your current pattern"
                    },
                    PeakUsageTimes = peakUsageTimes
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "ANALYTICS_RETRIEVED_SUCCESS",
                    Data = analytics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user analytics");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Checks eligibility for Co-owner status
        /// </summary>
        /// <param name="userId">User ID to check (optional, defaults to current user)</param>
        /// <response code="200">Eligibility check completed. Possible messages:  
        /// - ELIGIBLE_FOR_CO_OWNERSHIP  
        /// - NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - can only check own eligibility unless admin</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Checks if a user meets all requirements to become a Co-owner:  
        /// - Active account status  
        /// - Age 18 or older  
        /// - Valid driving license registered  
        /// 
        /// Users can only check their own eligibility unless they are admin/staff.
        /// </remarks>
        [HttpGet("eligibility")]
        public async Task<IActionResult> CheckEligibility([FromQuery] int? userId = null)
        {
            // Get current user ID from token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Determine target user ID
            var targetUserId = userId ?? currentUserId;

            // Check if user has permission to check other users' eligibility
            if (targetUserId != currentUserId)
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if (!userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
                {
                    return Forbid("ACCESS_DENIED");
                }
            }

            var response = await _coOwnerEligibilityService.CheckCoOwnerEligibilityAsync(targetUserId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Promotes current user to Co-owner status
        /// </summary>
        /// <response code="200">Promotion successful. Possible messages:  
        /// - PROMOTION_TO_CO_OWNER_SUCCESS  
        /// </response>
        /// <response code="400">Promotion failed - requirements not met. Possible messages:  
        /// - USER_NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">User already has Co-owner status. Possible messages:  
        /// - USER_ALREADY_CO_OWNER  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// - CO_OWNER_ROLE_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Promotes the current authenticated user to Co-owner status after verifying eligibility.  
        /// User must meet all requirements: active account, age 18+, and valid driving license.
        /// </remarks>
        [HttpPost("promote")]
        public async Task<IActionResult> PromoteToCoOwner()
        {
            // Get current user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _coOwnerEligibilityService.PromoteToCoOwnerAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Promotes a specific user to Co-owner status (Admin only)
        /// </summary>
        /// <param name="userId">User ID to promote</param>
        /// <response code="200">Promotion successful. Possible messages:  
        /// - PROMOTION_TO_CO_OWNER_SUCCESS  
        /// </response>
        /// <response code="400">Promotion failed - requirements not met. Possible messages:  
        /// - USER_NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="409">User already has Co-owner status. Possible messages:  
        /// - USER_ALREADY_CO_OWNER  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// - CO_OWNER_ROLE_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Allows administrators to promote any eligible user to Co-owner status.  
        /// Target user must meet all eligibility requirements.
        /// </remarks>
        [HttpPost("promote/{userId:int}")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> PromoteUserToCoOwner(int userId)
        {
            var response = await _coOwnerEligibilityService.PromoteToCoOwnerAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets Co-ownership system statistics (Admin only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully. Possible messages:  
        /// - CO_OWNERSHIP_STATS_RETRIEVED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Provides comprehensive statistics about the Co-ownership system including:  
        /// - Total users and Co-owners  
        /// - Co-ownership adoption rate  
        /// - License registration statistics  
        /// - Expired and expiring licenses  
        /// </remarks>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetCoOwnershipStats()
        {
            var response = await _coOwnerEligibilityService.GetCoOwnershipStatsAsync();
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #region Development/Testing Endpoints

        /// <summary>
        /// Test eligibility checking with various scenarios (Development only)
        /// </summary>
        /// <response code="200">Test scenarios completed</response>
        /// <remarks>
        /// Provides mock eligibility scenarios for testing purposes.
        /// </remarks>
        [HttpGet("test/eligibility-scenarios")]
        public IActionResult TestEligibilityScenarios()
        {
            var scenarios = new
            {
                EligibleUser = new
                {
                    Description = "User with all requirements met",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "25 years old",
                        DrivingLicense = "Valid, not expired",
                        Result = "ELIGIBLE"
                    }
                },
                TooYoung = new
                {
                    Description = "User under 18 years old",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "17 years old",
                        DrivingLicense = "Valid",
                        Result = "NOT_ELIGIBLE - MINIMUM_AGE_NOT_MET"
                    }
                },
                NoLicense = new
                {
                    Description = "User without driving license",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "25 years old",
                        DrivingLicense = "Not registered",
                        Result = "NOT_ELIGIBLE - NO_DRIVING_LICENSE_REGISTERED"
                    }
                },
                ExpiredLicense = new
                {
                    Description = "User with expired license",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "30 years old",
                        DrivingLicense = "Expired",
                        Result = "NOT_ELIGIBLE - NO_VALID_DRIVING_LICENSE"
                    }
                },
                InactiveAccount = new
                {
                    Description = "User with inactive account",
                    Requirements = new
                    {
                        AccountStatus = "Inactive/Suspended",
                        Age = "25 years old",
                        DrivingLicense = "Valid",
                        Result = "NOT_ELIGIBLE - ACCOUNT_NOT_ACTIVE"
                    }
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "ELIGIBILITY_SCENARIOS_GENERATED",
                Data = scenarios
            });
        }

        /// <summary>
        /// Test promotion workflow (Development only)
        /// </summary>
        /// <response code="200">Test workflow completed</response>
        /// <remarks>
        /// Demonstrates the promotion workflow steps and requirements.
        /// </remarks>
        [HttpGet("test/promotion-workflow")]
        public IActionResult TestPromotionWorkflow()
        {
            var workflow = new
            {
                Steps = new[]
                {
                    new
                    {
                        Step = 1,
                        Action = "Register Account",
                        Description = "User creates account with basic information"
                    },
                    new
                    {
                        Step = 2,
                        Action = "Verify Identity",
                        Description = "Upload and verify ID/Passport documents"
                    },
                    new
                    {
                        Step = 3,
                        Action = "Register Driving License",
                        Description = "Submit driving license for verification"
                    },
                    new
                    {
                        Step = 4,
                        Action = "Check Eligibility",
                        Description = "System verifies all requirements are met"
                    },
                    new
                    {
                        Step = 5,
                        Action = "Promote to Co-owner",
                        Description = "User or Admin triggers promotion process"
                    },
                    new
                    {
                        Step = 6,
                        Action = "Access Co-ownership Features",
                        Description = "User can now create/join groups and book vehicles"
                    }
                },
                Requirements = new
                {
                    MinimumAge = "18 years old",
                    AccountStatus = "Active",
                    DrivingLicense = "Valid and not expired",
                    IdentityVerification = "Completed"
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "PROMOTION_WORKFLOW_GENERATED",
                Data = workflow
            });
        }

        #endregion

        #region Fund Management

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets current fund balance for a vehicle
        /// 
        /// **VIEW FUND BALANCE - Role-Based Access**
        /// 
        /// **Access Control:**
        /// - **Co-owners**: Can view fund balance of their vehicles
        /// 
        /// **Response Includes:**
        /// - Current balance amount
        /// - Total amounts added and used
        /// - Number of additions and usages
        /// - Balance status (Healthy/Warning/Low)
        /// - Recommended minimum balance (based on 2x average monthly expenses)
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/coowner/fund/balance/1
        /// ```
        /// </remarks>
        /// <response code="200">Fund balance retrieved successfully. Possible messages: FUND_BALANCE_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpGet("fund/balance/{vehicleId}")]
        public async Task<IActionResult> GetFundBalance(int vehicleId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundBalanceAsync(vehicleId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundBalance for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets fund additions (deposits) history for a vehicle
        /// 
        /// **VIEW FUND ADDITIONS HISTORY**
        /// 
        /// **Returns list of fund deposits with:**
        /// - Deposit amount and payment method
        /// - Co-owner who made the deposit
        /// - Transaction ID and status
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/coowner/fund/additions/1?pageNumber=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Fund additions retrieved successfully. Possible messages: FUND_ADDITIONS_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpGet("fund/additions/{vehicleId}")]
        public async Task<IActionResult> GetFundAdditions(
            int vehicleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundAdditionsAsync(vehicleId, userId, pageNumber, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundAdditions for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets fund usages (expenses) history for a vehicle
        /// 
        /// **VIEW FUND USAGES (EXPENSES) HISTORY**
        /// 
        /// **Returns list of fund expenses with:**
        /// - Usage amount and type (Maintenance, Insurance, Fuel, Parking, Other)
        /// - Expense description and image proof
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/coowner/fund/usages/1?pageNumber=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Fund usages retrieved successfully. Possible messages: FUND_USAGES_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpGet("fund/usages/{vehicleId}")]
        public async Task<IActionResult> GetFundUsages(
            int vehicleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundUsagesAsync(vehicleId, userId, pageNumber, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundUsages for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets comprehensive fund summary with balance, history, and statistics
        /// 
        /// **VIEW COMPREHENSIVE FUND SUMMARY**
        /// 
        /// **Includes all fund information:**
        /// 1. Current Balance
        /// 2. Recent Additions (last 10 deposits)
        /// 3. Recent Usages (last 10 expenses)
        /// 4. Fund Statistics and analytics
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/coowner/fund/summary/1?monthsToAnalyze=6
        /// ```
        /// </remarks>
        /// <response code="200">Fund summary retrieved successfully. Possible messages: FUND_SUMMARY_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpGet("fund/summary/{vehicleId}")]
        public async Task<IActionResult> GetFundSummary(
            int vehicleId,
            [FromQuery] int monthsToAnalyze = 6)
        {
            try
            {
                if (monthsToAnalyze < 1) monthsToAnalyze = 6;
                if (monthsToAnalyze > 24) monthsToAnalyze = 24;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundSummaryAsync(vehicleId, userId, monthsToAnalyze);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundSummary for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Creates a new fund usage record (expense)
        /// 
        /// **CREATE FUND USAGE (EXPENSE) RECORD**
        /// 
        /// **Purpose:**
        /// Record expenses from the vehicle fund with automatic balance deduction.
        /// 
        /// **Categories:**
        /// - Maintenance (0): Regular or emergency maintenance
        /// - Insurance (1): Insurance premium payments
        /// - Fuel (2): Charging/fuel expenses
        /// - Parking (3): Parking and storage fees
        /// - Other (4): Miscellaneous expenses
        /// 
        /// **Sample Request:**  
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "usageType": 0,
        ///   "amount": 1500000,
        ///   "description": "Brake pad replacement"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Fund usage created successfully. Possible messages: FUND_USAGE_CREATED_SUCCESSFULLY</response>
        /// <response code="400">Bad request. Possible messages: INVALID_AMOUNT, INSUFFICIENT_FUND_BALANCE</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpPost("fund/usage")]
        public async Task<IActionResult> CreateFundUsage([FromBody] CreateFundUsageRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.CreateFundUsageAsync(request, userId);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(GetFundUsages), new { vehicleId = request.VehicleId }, response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFundUsage");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets fund usages filtered by category type
        /// 
        /// **GET FUND USAGES BY CATEGORY**
        /// 
        /// **Category Enum Values:**
        /// - 0: Maintenance
        /// - 1: Insurance
        /// - 2: Fuel (Charging)
        /// - 3: Parking
        /// - 4: Other
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/coowner/fund/category/1/usages/0?startDate=2024-10-01&amp;endDate=2024-10-31
        /// ```
        /// </remarks>
        /// <response code="200">Category usages retrieved successfully. Possible messages: FUND_USAGES_BY_CATEGORY_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="403">Access denied. Possible messages: ACCESS_DENIED_NOT_VEHICLE_CO_OWNER</response>
        /// <response code="404">Not found. Possible messages: VEHICLE_NOT_FOUND, FUND_NOT_FOUND_FOR_VEHICLE</response>
        [HttpGet("fund/category/{vehicleId}/usages/{category}")]
        public async Task<IActionResult> GetFundUsagesByCategory(
            int vehicleId,
            int category,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (!Enum.IsDefined(typeof(EvCoOwnership.Repositories.Enums.EUsageType), category))
                {
                    return BadRequest(new { message = "INVALID_CATEGORY" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var usageType = (EvCoOwnership.Repositories.Enums.EUsageType)category;
                var response = await _fundService.GetFundUsagesByCategoryAsync(vehicleId, usageType, userId, startDate, endDate);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundUsagesByCategory for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Usage Analytics

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Get usage vs ownership comparison data for a vehicle
        /// 
        /// **USAGE VS OWNERSHIP COMPARISON**
        /// 
        /// **Returns comprehensive comparison data:**
        /// - Actual vehicle usage vs ownership percentages for all co-owners
        /// - Shows who is using the vehicle more or less than their ownership share
        /// - Usage patterns: Balanced, Overutilized, Underutilized
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/analytics/vehicle/1/usage-vs-ownership?usageMetric=Hours&amp;startDate=2024-01-01
        /// ```
        /// </remarks>
        /// <response code="200">Usage vs ownership data retrieved successfully</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("analytics/vehicle/{vehicleId}/usage-vs-ownership")]
        public async Task<IActionResult> GetUsageVsOwnershipComparison(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string usageMetric = "Hours")
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _analyticsService.GetUsageVsOwnershipAsync(vehicleId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsageVsOwnershipComparison for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Get usage trends over time for a vehicle
        /// 
        /// **USAGE TRENDS ANALYSIS**
        /// 
        /// **Returns:**
        /// - Usage trends over time for all co-owners
        /// - Monthly/weekly breakdown of usage patterns
        /// - Trend analysis and forecasting
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/analytics/vehicle/1/usage-trends?period=monthly&amp;months=6
        /// ```
        /// </remarks>
        /// <response code="200">Usage trends retrieved successfully</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("analytics/vehicle/{vehicleId}/usage-trends")]
        public async Task<IActionResult> GetUsageTrends(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string period = "monthly")
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _analyticsService.GetUsageVsOwnershipTrendsAsync(
                    vehicleId, userId, startDate, endDate, period);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsageTrends for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Get personal usage history and analytics
        /// 
        /// **MY USAGE HISTORY**
        /// 
        /// **Returns:**
        /// - Personal usage statistics across all vehicles
        /// - Booking patterns and trends
        /// - Usage efficiency metrics
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/analytics/my-usage-history?vehicleId=1&amp;months=3
        /// ```
        /// </remarks>
        /// <response code="200">Personal usage history retrieved successfully</response>
        [HttpGet("analytics/my-usage-history")]
        public async Task<IActionResult> GetMyUsageHistory(
            [FromQuery] int? vehicleId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var request = new GetPersonalUsageHistoryRequest
                {
                    VehicleId = vehicleId,
                    StartDate = startDate,
                    EndDate = endDate
                };
                var response = await _analyticsService.GetPersonalUsageHistoryAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyUsageHistory");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Get group usage summary for all vehicles
        /// 
        /// **GROUP USAGE SUMMARY**
        /// 
        /// **Returns:**
        /// - Summary of usage across all user's co-owned vehicles
        /// - Group efficiency metrics
        /// - Comparative analysis between vehicles
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/analytics/group-summary
        /// ```
        /// </remarks>
        /// <response code="200">Group usage summary retrieved successfully</response>
        [HttpGet("analytics/group-summary")]
        public async Task<IActionResult> GetGroupUsageSummary()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var request = new GetGroupUsageSummaryRequest();
                var response = await _analyticsService.GetGroupUsageSummaryAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGroupUsageSummary");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Payment Management

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Creates a new payment and returns payment URL
        /// 
        /// **CREATE PAYMENT**
        /// 
        /// **Sample Request for VNPay:**
        /// ```json
        /// {
        ///   "amount": 500000,
        ///   "paymentGateway": 0,
        ///   "paymentMethod": 1,
        ///   "paymentType": 0,
        ///   "bookingId": 123,
        ///   "description": "Payment for vehicle booking"
        /// }
        /// ```
        /// 
        /// **Sample Request for Fund Contribution:**
        /// ```json
        /// {
        ///   "amount": 1000000,
        ///   "paymentGateway": 0,
        ///   "paymentType": 2,
        ///   "vehicleId": 5,
        ///   "description": "Monthly fund contribution"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Payment created successfully with payment URL</response>
        /// <response code="400">Validation error. Invalid amount, gateway, or parameters</response>
        /// <response code="404">Related entity not found (booking, vehicle, etc.)</response>
        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _paymentService.CreatePaymentAsync(userId, request);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(GetPayment), new { id = response.Data }, response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePayment");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets details of a specific payment
        /// 
        /// **VIEW PAYMENT DETAILS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/payments/123
        /// ```
        /// </remarks>
        /// <response code="200">Payment details retrieved successfully</response>
        /// <response code="403">Access denied - can only view own payments</response>
        /// <response code="404">Payment not found</response>
        [HttpGet("payments/{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _paymentService.GetPaymentByIdAsync(id);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPayment for payment {PaymentId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets current user's payment history
        /// 
        /// **VIEW MY PAYMENTS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/payments/my-payments?status=Completed&amp;page=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Payment history retrieved successfully</response>
        [HttpGet("payments/my-payments")]
        public async Task<IActionResult> GetMyPayments(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _paymentService.GetUserPaymentsAsync(userId, page, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyPayments");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Cancels a pending payment
        /// 
        /// **CANCEL PAYMENT**
        /// 
        /// **Sample Request:**
        /// ```
        /// POST /api/coowner/payments/123/cancel
        /// ```
        /// </remarks>
        /// <response code="200">Payment cancelled successfully</response>
        /// <response code="400">Cannot cancel completed or already cancelled payment</response>
        /// <response code="403">Access denied - can only cancel own payments</response>
        /// <response code="404">Payment not found</response>
        [HttpPost("payments/{id}/cancel")]
        public async Task<IActionResult> CancelPayment(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _paymentService.CancelPaymentAsync(id, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelPayment for payment {PaymentId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets available payment gateways and methods
        /// 
        /// **VIEW PAYMENT GATEWAYS**
        /// 
        /// **Returns:**
        /// - Available payment gateways (VNPay, Momo, etc.)
        /// - Supported payment methods for each gateway
        /// - Gateway status and configuration
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/payments/gateways
        /// ```
        /// </remarks>
        /// <response code="200">Payment gateways retrieved successfully</response>
        [HttpGet("payments/gateways")]
        public async Task<IActionResult> GetPaymentGateways()
        {
            try
            {
                var response = await _paymentService.GetAvailableGatewaysAsync();

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentGateways");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Booking Management

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Creates a new booking for a vehicle
        /// 
        /// **CREATE BOOKING**
        /// 
        /// **Purpose:**
        /// Allow co-owners to book vehicles they have ownership rights to
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "startDate": "2024-11-01T09:00:00Z",
        ///   "endDate": "2024-11-01T17:00:00Z",
        ///   "purpose": "Business meeting",
        ///   "notes": "Will pick up client from airport"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Booking created successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="409">Booking time conflict</response>
        [HttpPost("bookings")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.CreateBookingAsync(userId, request);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(GetBooking), new { id = response.Data }, response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    409 => Conflict(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateBooking");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets details of a specific booking
        /// 
        /// **VIEW BOOKING DETAILS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/bookings/123
        /// ```
        /// </remarks>
        /// <response code="200">Booking retrieved successfully</response>
        /// <response code="403">Access denied - can only view own bookings</response>
        /// <response code="404">Booking not found</response>
        [HttpGet("bookings/{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.GetBookingByIdAsync(id, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBooking for booking {BookingId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets current user's bookings with optional filtering
        /// 
        /// **VIEW MY BOOKINGS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/bookings/my-bookings?status=Confirmed&amp;page=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Bookings retrieved successfully</response>
        [HttpGet("bookings/my-bookings")]
        public async Task<IActionResult> GetMyBookings(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.GetUserBookingsAsync(userId, page, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyBookings");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets bookings for a specific vehicle
        /// 
        /// **VIEW VEHICLE BOOKINGS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/bookings/vehicle/1?startDate=2024-11-01&amp;endDate=2024-11-30
        /// ```
        /// </remarks>
        /// <response code="200">Vehicle bookings retrieved successfully</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("bookings/vehicle/{vehicleId}")]
        public async Task<IActionResult> GetVehicleBookings(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.GetVehicleBookingsAsync(vehicleId, 1, 100);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVehicleBookings for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Updates an existing booking
        /// 
        /// **UPDATE BOOKING**
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "startDate": "2024-11-01T10:00:00Z",
        ///   "endDate": "2024-11-01T18:00:00Z",
        ///   "purpose": "Updated purpose",
        ///   "notes": "Updated notes"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Booking updated successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="403">Access denied - can only update own bookings</response>
        /// <response code="404">Booking not found</response>
        /// <response code="409">Booking time conflict</response>
        [HttpPut("bookings/{id}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] UpdateBookingRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.UpdateBookingAsync(id, userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    409 => Conflict(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateBooking for booking {BookingId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Cancels a booking
        /// 
        /// **CANCEL BOOKING**
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "reason": "Plans changed"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Booking cancelled successfully</response>
        /// <response code="400">Cannot cancel completed or already cancelled booking</response>
        /// <response code="403">Access denied - can only cancel own bookings</response>
        /// <response code="404">Booking not found</response>
        [HttpPost("bookings/{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.CancelBookingEnhancedAsync(id, userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBooking for booking {BookingId}", id);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets vehicle availability for booking
        /// 
        /// **CHECK VEHICLE AVAILABILITY**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/bookings/availability?vehicleId=1&amp;startDate=2024-11-01&amp;endDate=2024-11-30
        /// ```
        /// </remarks>
        /// <response code="200">Availability information retrieved successfully</response>
        /// <response code="403">Access denied - must be co-owner of vehicle</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("bookings/availability")]
        public async Task<IActionResult> GetVehicleAvailability(
            [FromQuery] int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _bookingService.CheckVehicleAvailabilityAsync(vehicleId, startDate, endDate);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVehicleAvailability for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets the current user's profile information
        /// 
        /// **VIEW MY PROFILE**
        /// 
        /// **Returns comprehensive profile information:**
        /// - Basic information (name, email, phone, address)
        /// - Profile statistics (vehicles owned, bookings, payments)
        /// - Role and status information
        /// - Account creation and update dates
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/profile
        /// ```
        /// </remarks>
        /// <response code="200">Profile retrieved successfully. Message: USER_PROFILE_RETRIEVED_SUCCESSFULLY</response>
        /// <response code="404">User not found. Message: USER_NOT_FOUND</response>
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _userProfileService.GetUserProfileAsync(userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyProfile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Updates the current user's profile information
        /// 
        /// **UPDATE MY PROFILE**
        /// 
        /// **Validation Rules:**
        /// - First/Last Name: Required, max 50 chars, only letters and spaces
        /// - Phone: Optional, must match Vietnam format (+84 or 0 followed by valid mobile number)
        /// - Date of Birth: Optional, user must be at least 18 years old
        /// - Address: Optional, max 200 characters
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "phoneNumber": "0912345678",
        ///   "dateOfBirth": "1990-01-01",
        ///   "address": "123 Main St, Ho Chi Minh City"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Profile updated successfully. Message: USER_PROFILE_UPDATED_SUCCESSFULLY</response>
        /// <response code="400">Validation error. Possible messages: FIRST_NAME_REQUIRED, INVALID_VIETNAM_PHONE_FORMAT</response>
        /// <response code="404">User not found. Message: USER_NOT_FOUND</response>
        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _userProfileService.UpdateUserProfileAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMyProfile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Changes the current user's password
        /// 
        /// **CHANGE PASSWORD**
        /// 
        /// **Password Requirements:**
        /// - Minimum 8 characters
        /// - At least 1 uppercase letter (A-Z)
        /// - At least 1 lowercase letter (a-z)
        /// - At least 1 number (0-9)
        /// - At least 1 special character (@$!%*?&amp;)
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "currentPassword": "oldPassword123!",
        ///   "newPassword": "newPassword456@"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Password changed successfully. Message: PASSWORD_CHANGED_SUCCESSFULLY</response>
        /// <response code="400">Validation error. Possible messages: CURRENT_PASSWORD_INCORRECT, PASSWORD_TOO_WEAK</response>
        /// <response code="404">User not found. Message: USER_NOT_FOUND</response>
        [HttpPut("my-profile/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _userProfileService.ChangePasswordAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChangePassword");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets user's vehicles summary (owned, co-owned, invitations)
        /// 
        /// **VIEW MY VEHICLES SUMMARY**
        /// 
        /// **Returns comprehensive summary including:**
        /// - Owned Vehicles: Vehicles created by the user (primary owner)
        /// - Co-owned Vehicles: Vehicles where user has accepted co-ownership
        /// - Pending Invitations: Outstanding invitations to become co-owner
        /// 
        /// Each vehicle entry includes ownership percentage, investment amount, and status.
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/profile/vehicles
        /// ```
        /// </remarks>
        /// <response code="200">Vehicles summary retrieved successfully. Message: USER_VEHICLES_SUMMARY_RETRIEVED_SUCCESSFULLY</response>
        [HttpGet("my-profile/vehicles")]
        public async Task<IActionResult> GetMyVehiclesSummary()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _userProfileService.GetUserVehiclesSummaryAsync(userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyVehiclesSummary");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets user's activity summary (recent bookings, payments, license info)
        /// 
        /// **VIEW MY ACTIVITY SUMMARY**
        /// 
        /// **Returns user's recent activity:**
        /// - Recent Bookings: Last 5 vehicle bookings with status and amounts
        /// - Recent Payments: Last 5 payments with methods and status
        /// - Driving License: Current license status, expiry, and validity
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/coowner/profile/activity
        /// ```
        /// </remarks>
        /// <response code="200">Activity summary retrieved successfully. Message: USER_ACTIVITY_SUMMARY_RETRIEVED_SUCCESSFULLY</response>
        [HttpGet("my-profile/activity")]
        public async Task<IActionResult> GetMyActivitySummary()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _userProfileService.GetUserActivitySummaryAsync(userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyActivitySummary");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        /*
        // REMOVED: Ownership Change Management section
        // All ownership change related endpoints have been removed
        // due to missing OwnershipChangeService and related DTOs
        */

        #region Schedule Management

        /// <summary>
        /// Get vehicle schedule for a specific period
        /// </summary>
        /// <remarks>
        /// Retrieves detailed vehicle schedule showing booked and available time slots.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/coowner/schedule/vehicle/123?startDate=2025-01-17&amp;endDate=2025-01-24&amp;statusFilter=Confirmed
        /// ```
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to get schedule for</param>
        /// <param name="startDate">Start date for schedule view (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date for schedule view (format: yyyy-MM-dd)</param>
        /// <param name="statusFilter">Optional filter by booking status</param>
        /// <response code="200">Vehicle schedule retrieved successfully</response>
        /// <response code="400">Invalid date range or parameters</response>
        /// <response code="403">Access denied - not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("schedule/vehicle/{vehicleId}")]
        public async Task<IActionResult> GetVehicleSchedule(
            int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? statusFilter = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var request = new GetVehicleScheduleRequest
                {
                    VehicleId = vehicleId,
                    StartDate = startDate,
                    EndDate = endDate,
                    StatusFilter = statusFilter
                };

                var response = await _scheduleService.GetVehicleScheduleAsync(request, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle schedule for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Check availability for a specific time slot
        /// </summary>
        /// <remarks>
        /// Checks if a vehicle is available for booking during a specific time period and provides alternative suggestions if not available.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleId": 123,
        ///   "startTime": "2025-01-17T09:00:00",
        ///   "endTime": "2025-01-17T17:00:00",
        ///   "excludeBookingId": null
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Availability check request</param>
        /// <response code="200">Availability check completed successfully</response>
        /// <response code="400">Invalid time slot parameters</response>
        /// <response code="403">Access denied - not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("schedule/check-availability")]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckAvailabilityRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _scheduleService.CheckAvailabilityAsync(request, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for vehicle {VehicleId}", request.VehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Find optimal booking slots based on preferences
        /// </summary>
        /// <remarks>
        /// Analyzes vehicle availability and usage patterns to suggest optimal booking times that match your preferences.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleId": 123,
        ///   "preferredStartDate": "2025-01-17",
        ///   "preferredEndDate": "2025-01-24",
        ///   "minimumDurationHours": 4,
        ///   "maximumDurationHours": 8,
        ///   "preferredTimeOfDay": "Morning",
        ///   "fullDayOnly": false
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Optimal slots search request</param>
        /// <response code="200">Optimal slots found successfully</response>
        /// <response code="400">Invalid search parameters</response>
        /// <response code="403">Access denied - not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("schedule/find-optimal-slots")]
        public async Task<IActionResult> FindOptimalSlots([FromBody] FindOptimalSlotsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _scheduleService.FindOptimalSlotsAsync(request, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding optimal slots for vehicle {VehicleId}", request.VehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Get your personal booking schedule
        /// </summary>
        /// <remarks>
        /// Retrieves your personal booking schedule across all vehicles you co-own.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/coowner/schedule/my-schedule?startDate=2025-01-17&amp;endDate=2025-01-24
        /// ```
        /// </remarks>
        /// <param name="startDate">Start date for schedule view (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date for schedule view (format: yyyy-MM-dd)</param>
        /// <response code="200">Personal schedule retrieved successfully</response>
        /// <response code="400">Invalid date range</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("schedule/my-schedule")]
        public async Task<IActionResult> GetMySchedule(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var response = await _scheduleService.GetUserScheduleAsync(userId, startDate, endDate);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal schedule for user {UserId}", userId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Get schedule conflicts for your bookings
        /// </summary>
        /// <remarks>
        /// Identifies any scheduling conflicts in your current bookings that need to be resolved.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/coowner/schedule/conflicts?startDate=2025-01-17&amp;endDate=2025-01-24
        /// ```
        /// </remarks>
        /// <param name="startDate">Start date to check for conflicts (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date to check for conflicts (format: yyyy-MM-dd)</param>
        /// <response code="200">Schedule conflicts retrieved successfully</response>
        /// <response code="400">Invalid date range</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("schedule/conflicts")]
        public async Task<IActionResult> GetScheduleConflicts(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var response = await _scheduleService.GetScheduleConflictsAsync(userId, startDate, endDate);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule conflicts for user {UserId}", userId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion
    }
}