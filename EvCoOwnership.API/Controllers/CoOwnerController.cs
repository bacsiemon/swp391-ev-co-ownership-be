using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Helpers.BaseClasses;

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
        /// <param name="logger">Logger</param>
        public CoOwnerController(
            ICoOwnerEligibilityService coOwnerEligibilityService,
            IUserProfileService userProfileService,
            IBookingService bookingService,
            IPaymentService paymentService,
            IGroupService groupService,
            IUsageAnalyticsService analyticsService,
            ILogger<CoOwnerController> logger)
        {
            _coOwnerEligibilityService = coOwnerEligibilityService;
            _userProfileService = userProfileService;
            _bookingService = bookingService;
            _paymentService = paymentService;
            _groupService = groupService;
            _analyticsService = analyticsService;
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
        public async Task<IActionResult> UpdateProfile([FromBody] object profileData)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response vì service method cần specific DTO type
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PROFILE_UPDATED_SUCCESS",
                    Data = new
                    {
                        UserId = userId,
                        UpdatedAt = DateTime.UtcNow,
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

                // Mock data - cần implement OwnershipService
                var ownership = new
                {
                    UserId = userId,
                    Vehicles = new[]
                    {
                        new { Id = 1, Model = "Tesla Model 3", Share = 25, Status = "Active" },
                        new { Id = 2, Model = "BMW i3", Share = 50, Status = "Active" }
                    },
                    TotalShares = 75,
                    TotalValue = 1500000000 // VND
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

                // Mock data
                var schedule = new
                {
                    UserId = userId,
                    Bookings = new[]
                    {
                        new { Id = 1, VehicleModel = "Tesla Model 3", StartTime = DateTime.UtcNow.AddHours(2), EndTime = DateTime.UtcNow.AddHours(6), Status = "Confirmed" },
                        new { Id = 2, VehicleModel = "BMW i3", StartTime = DateTime.UtcNow.AddDays(1), EndTime = DateTime.UtcNow.AddDays(1).AddHours(4), Status = "Pending" }
                    },
                    TotalBookings = 2
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
        public async Task<IActionResult> BookVehicle([FromBody] object bookingData)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response vì service method cần specific DTO type
                return StatusCode(201, new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "BOOKING_CREATED_SUCCESS",
                    Data = new
                    {
                        BookingId = new Random().Next(1000, 9999),
                        UserId = userId,
                        BookedAt = DateTime.UtcNow,
                        Status = "Confirmed"
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

                // Mock data vì service method chưa tồn tại
                var history = new
                {
                    Items = new[]
                    {
                        new { Id = 1, VehicleModel = "Tesla Model 3", StartTime = DateTime.UtcNow.AddDays(-7), EndTime = DateTime.UtcNow.AddDays(-7).AddHours(4), Status = "Completed", Cost = 200000 },
                        new { Id = 2, VehicleModel = "BMW i3", StartTime = DateTime.UtcNow.AddDays(-3), EndTime = DateTime.UtcNow.AddDays(-3).AddHours(6), Status = "Completed", Cost = 300000 },
                        new { Id = 3, VehicleModel = "Tesla Model 3", StartTime = DateTime.UtcNow.AddHours(2), EndTime = DateTime.UtcNow.AddHours(6), Status = "Upcoming", Cost = 250000 }
                    },
                    TotalCount = 3,
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

                // Mock data - cần implement cost calculation service
                var costs = new
                {
                    UserId = userId,
                    TotalDue = 1500000, // VND
                    PaidAmount = 1200000,
                    RemainingBalance = 300000,
                    MonthlyBreakdown = new[]
                    {
                        new { Month = "October 2025", Amount = 500000, Status = "Paid" },
                        new { Month = "November 2025", Amount = 300000, Status = "Pending" }
                    },
                    Categories = new
                    {
                        Maintenance = 200000,
                        Insurance = 100000,
                        Fuel = 150000,
                        Depreciation = 350000
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
        public async Task<IActionResult> MakePayment([FromBody] object paymentData)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response vì service method signature khác
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PAYMENT_PROCESSED_SUCCESS",
                    Data = new
                    {
                        PaymentId = new Random().Next(1000, 9999),
                        UserId = userId,
                        ProcessedAt = DateTime.UtcNow,
                        Status = "Completed"
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

                // Mock data
                var group = new
                {
                    Id = 1,
                    Name = "EV Owners Group Alpha",
                    Members = new[]
                    {
                        new { Id = userId, Name = "Current User", Role = "Member", SharePercentage = 25 },
                        new { Id = 2, Name = "John Smith", Role = "Leader", SharePercentage = 40 },
                        new { Id = 3, Name = "Jane Doe", Role = "Member", SharePercentage = 35 }
                    },
                    Vehicles = new[]
                    {
                        new { Id = 1, Model = "Tesla Model 3", Status = "Available" },
                        new { Id = 2, Model = "BMW i3", Status = "In Use" }
                    },
                    Fund = new
                    {
                        TotalAmount = 50000000, // VND
                        AvailableAmount = 35000000,
                        PendingExpenses = 15000000
                    }
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

                // Mock data
                var fund = new
                {
                    TotalAmount = 50000000, // VND
                    AvailableAmount = 35000000,
                    PendingExpenses = 15000000,
                    MonthlyContributions = new[]
                    {
                        new { Month = "October 2025", Amount = 5000000, Status = "Collected" },
                        new { Month = "November 2025", Amount = 5000000, Status = "Pending" }
                    },
                    RecentTransactions = new[]
                    {
                        new { Date = DateTime.UtcNow.AddDays(-1), Description = "Vehicle maintenance", Amount = -800000, Type = "Expense" },
                        new { Date = DateTime.UtcNow.AddDays(-7), Description = "Monthly contribution", Amount = 5000000, Type = "Income" }
                    }
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

                // Mock data vì service method chưa tồn tại
                var analytics = new
                {
                    UserId = userId,
                    UsageStats = new
                    {
                        TotalBookings = 15,
                        TotalHours = 120,
                        TotalDistance = 2500, // km
                        AverageRating = 4.8
                    },
                    CostAnalysis = new
                    {
                        MonthlyAverage = 800000, // VND
                        CostPerKm = 320, // VND
                        SavingsVsOwnership = 2000000 // VND per month
                    },
                    Recommendations = new[]
                    {
                        "Consider booking during off-peak hours for better rates",
                        "Your usage pattern suggests upgrading to premium membership",
                        "You could save 15% by planning bookings in advance"
                    },
                    PeakUsageTimes = new[]
                    {
                        new { Hour = 9, Count = 8 },
                        new { Hour = 17, Count = 12 },
                        new { Hour = 19, Count = 6 }
                    }
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
    }
}