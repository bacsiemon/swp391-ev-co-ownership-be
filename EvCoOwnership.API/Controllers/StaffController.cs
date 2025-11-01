using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs;
using EvCoOwnership.Repositories.DTOs.GroupManagementDTOs;
using EvCoOwnership.Repositories.DTOs.ProfileDTOs;
using EvCoOwnership.Repositories.DTOs.LicenseDTOs;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for staff-specific operations in the EV Co-ownership system
    /// </summary>
    [Route("api/staff")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Staff)]
    public class StaffController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IContractService _contractService;
        private readonly ICheckInCheckOutService _checkInCheckOutService;
        private readonly IDisputeService _disputeService;
        private readonly IMaintenanceService _maintenanceService;
        private readonly IProfileService _profileService;
        private readonly IGroupManagementService _groupManagementService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StaffController> _logger;

        /// <summary>
        /// Constructor for StaffController
        /// </summary>
        /// <param name="groupService">Group service for group management</param>
        /// <param name="contractService">Contract service for contract management</param>
        /// <param name="checkInCheckOutService">Check-in/out service</param>
        /// <param name="disputeService">Dispute management service</param>
        /// <param name="maintenanceService">Maintenance service for vehicle maintenance</param>
        /// <param name="profileService">Profile service for user profile management</param>
        /// <param name="groupManagementService">Group management service for staff operations</param>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for logging</param>
        public StaffController(
            IGroupService groupService,
            IContractService contractService,
            ICheckInCheckOutService checkInCheckOutService,
            IDisputeService disputeService,
            IMaintenanceService maintenanceService,
            IProfileService profileService,
            IGroupManagementService groupManagementService,
            IUnitOfWork unitOfWork,
            ILogger<StaffController> logger)
        {
            _groupService = groupService;
            _contractService = contractService;
            _checkInCheckOutService = checkInCheckOutService;
            _disputeService = disputeService;
            _maintenanceService = maintenanceService;
            _profileService = profileService;
            _groupManagementService = groupManagementService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // --- Quản lý nhóm xe đồng sở hữu ---
        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả nhóm xe để quản lý
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/groups?status=active&amp;pageIndex=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Lấy danh sách nhóm thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups([FromQuery] string? status = null, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = new { Status = status, PageIndex = pageIndex, PageSize = pageSize };
                var groups = await _groupService.ListAsync(query);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_LIST_RETRIEVED_SUCCESS",
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy thông tin chi tiết của một nhóm
        /// </remarks>
        /// <response code="200">Lấy thông tin nhóm thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy nhóm</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("group/{id}")]
        public async Task<IActionResult> GetGroup(int id)
        {
            try
            {
                var group = await _groupService.GetAsync(id);
                if (group == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "GROUP_NOT_FOUND"
                    });
                }

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_RETRIEVED_SUCCESS",
                    Data = group
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group {GroupId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Quản lý hợp đồng pháp lý ---
        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy danh sách hợp đồng cần xử lý
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/contracts?status=pending&amp;pageIndex=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Lấy danh sách hợp đồng thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("contracts")]
        public async Task<IActionResult> GetContracts([FromQuery] string? status = null, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get real contract data from database (using Fund as a proxy for contracts)
                var allFunds = await _unitOfWork.FundRepository.GetAllAsync();
                var filteredFunds = allFunds.AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    // For demo purposes, we'll use fund status mapping
                    filteredFunds = status.ToLower() switch
                    {
                        "pending" => filteredFunds.Where(f => f.CreatedAt > DateTime.UtcNow.AddDays(-7)), // Recent funds as pending
                        "active" => filteredFunds.Where(f => f.CreatedAt <= DateTime.UtcNow.AddDays(-7)), // Older funds as active
                        _ => filteredFunds
                    };
                }

                // Apply pagination
                var totalCount = filteredFunds.Count();
                var contracts = filteredFunds
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        Id = f.Id,
                        Title = $"Co-ownership Agreement for Fund #{f.Id}",
                        Status = f.CreatedAt > DateTime.UtcNow.AddDays(-7) ? "Pending" : "Active",
                        CreatedDate = f.CreatedAt ?? DateTime.UtcNow,
                        CurrentBalance = f.CurrentBalance ?? 0,
                        Description = "Vehicle co-ownership contract"
                    })
                    .ToList();

                var result = new
                {
                    Items = contracts,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contracts list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Cập nhật trạng thái hợp đồng (Approved/Rejected/Processing)
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Approved",
        ///   "notes": "All documents verified"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật trạng thái thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy hợp đồng</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("contract/{id}/status")]
        public async Task<IActionResult> UpdateContractStatus(int id, [FromBody] object statusUpdate)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Tạm thời mock response vì cần implement service method
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_STATUS_UPDATED_SUCCESS",
                    Data = new { ContractId = id, UpdatedBy = staffUserId, UpdatedAt = DateTime.UtcNow }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract status {ContractId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Check-in/Check-out ---
        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Thực hiện check-in xe cho người dùng
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "vehicleCondition": "Good",
        ///   "fuelLevel": 85,
        ///   "notes": "Vehicle in excellent condition"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Check-in thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy booking</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] Dictionary<string, object> checkInData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Extract booking ID from request
                if (!checkInData.TryGetValue("bookingId", out var bookingIdObj) || !int.TryParse(bookingIdObj.ToString(), out int bookingId))
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Invalid or missing bookingId"
                    });
                }

                // Check if booking exists
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    });
                }

                // Create check-in record
                var notes = checkInData.TryGetValue("notes", out var notesObj) ? notesObj.ToString() : "";

                var checkIn = new CheckIn
                {
                    BookingId = bookingId,
                    CheckTime = DateTime.UtcNow,
                    StaffId = staffUserId,
                    VehicleConditionId = 1, // Default condition ID - should be dynamic based on actual condition
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<CheckIn>().AddAsync(checkIn);
                await _unitOfWork.SaveChangesAsync();

                // Update booking status if needed
                booking.StatusEnum = EBookingStatus.Active; // Use Active instead of InProgress
                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Check-in completed for booking {BookingId} by staff {StaffId}", bookingId, staffUserId);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CHECK_IN_SUCCESS",
                    Data = new
                    {
                        CheckInId = checkIn.Id,
                        BookingId = bookingId,
                        StaffId = staffUserId,
                        CheckInTime = checkIn.CheckTime,
                        Notes = notes,
                        Message = "Vehicle checked in successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Thực hiện check-out xe cho người dùng
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "returnCondition": "Good",
        ///   "fuelLevel": 75,
        ///   "damageNotes": "",
        ///   "extraCharges": 0
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Check-out thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy booking</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] Dictionary<string, object> checkOutData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Extract booking ID from request
                if (!checkOutData.TryGetValue("bookingId", out var bookingIdObj) || !int.TryParse(bookingIdObj.ToString(), out int bookingId))
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Invalid or missing bookingId"
                    });
                }

                // Check if booking exists
                var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "BOOKING_NOT_FOUND"
                    });
                }

                // Create check-out record
                var damageNotes = checkOutData.TryGetValue("damageNotes", out var damageObj) ? damageObj.ToString() : "";
                var extraCharges = checkOutData.TryGetValue("extraCharges", out var chargesObj) && decimal.TryParse(chargesObj.ToString(), out decimal charges) ? charges : 0;

                var checkOut = new CheckOut
                {
                    BookingId = bookingId,
                    CheckTime = DateTime.UtcNow,
                    StaffId = staffUserId,
                    VehicleConditionId = 1, // Default condition ID
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<CheckOut>().AddAsync(checkOut);
                await _unitOfWork.SaveChangesAsync();

                // Update booking status to completed
                booking.StatusEnum = EBookingStatus.Completed;
                await _unitOfWork.BookingRepository.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Check-out completed for booking {BookingId} by staff {StaffId}", bookingId, staffUserId);

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CHECK_OUT_SUCCESS",
                    Data = new
                    {
                        CheckOutId = checkOut.Id,
                        BookingId = bookingId,
                        StaffId = staffUserId,
                        CheckOutTime = checkOut.CheckTime,
                        ExtraCharges = extraCharges,
                        DamageNotes = damageNotes,
                        Message = "Vehicle checked out successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-out");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Dịch vụ xe ---
        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy danh sách dịch vụ xe (bảo trì, sửa chữa, ...)
        /// </remarks>
        /// <response code="200">Lấy danh sách dịch vụ thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("services")]
        public async Task<IActionResult> GetServices([FromQuery] string? status = null, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get maintenance costs as service records
                var allMaintenanceCosts = await _unitOfWork.MaintenanceCostRepository.GetAllAsync();
                var filteredServices = allMaintenanceCosts.AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    // Map status to date ranges for demo purposes
                    filteredServices = status.ToLower() switch
                    {
                        "scheduled" => filteredServices.Where(m => m.CreatedAt > DateTime.UtcNow.AddDays(-3)), // Recent ones as scheduled
                        "in progress" => filteredServices.Where(m => m.CreatedAt <= DateTime.UtcNow.AddDays(-3) && m.CreatedAt > DateTime.UtcNow.AddDays(-10)), // Mid-range as in progress
                        "completed" => filteredServices.Where(m => m.CreatedAt <= DateTime.UtcNow.AddDays(-10)), // Older ones as completed
                        _ => filteredServices
                    };
                }

                // Apply pagination
                var totalCount = filteredServices.Count();
                var services = filteredServices
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        Id = m.Id,
                        Type = m.CreatedAt > DateTime.UtcNow.AddDays(-3) ? "Maintenance" : "Repair",
                        VehicleId = m.BookingId, // Using BookingId as vehicle reference
                        Status = m.CreatedAt > DateTime.UtcNow.AddDays(-3) ? "Scheduled" :
                                m.CreatedAt > DateTime.UtcNow.AddDays(-10) ? "In Progress" : "Completed",
                        Cost = m.Cost, // Cost is not nullable
                        Date = m.CreatedAt ?? DateTime.UtcNow,
                        Description = m.Description ?? "Vehicle maintenance service"
                    })
                    .ToList();

                var result = new
                {
                    Items = services,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SERVICE_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Tạo yêu cầu dịch vụ mới cho xe
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "serviceType": "Maintenance",
        ///   "description": "Regular maintenance service",
        ///   "estimatedCost": 500000,
        ///   "scheduledDate": "2025-11-15T09:00:00"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Tạo dịch vụ thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpPost("service")]
        public async Task<IActionResult> CreateService([FromBody] Dictionary<string, object> serviceData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Extract required fields
                if (!serviceData.TryGetValue("vehicleId", out var vehicleIdObj) || !int.TryParse(vehicleIdObj.ToString(), out int vehicleId))
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "Invalid or missing vehicleId"
                    });
                }

                var serviceType = serviceData.TryGetValue("serviceType", out var typeObj) ? typeObj.ToString() : "Maintenance";
                var description = serviceData.TryGetValue("description", out var descObj) ? descObj.ToString() : "Service request";
                var estimatedCost = serviceData.TryGetValue("estimatedCost", out var costObj) && decimal.TryParse(costObj.ToString(), out decimal cost) ? cost : 0;
                var scheduledDate = serviceData.TryGetValue("scheduledDate", out var dateObj) && DateTime.TryParse(dateObj.ToString(), out DateTime date) ? date : DateTime.UtcNow;

                // Check if vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    });
                }

                // Create maintenance cost record
                var maintenanceCost = new MaintenanceCost
                {
                    VehicleId = vehicleId,
                    Description = description,
                    Cost = estimatedCost,
                    ServiceDate = DateOnly.FromDateTime(scheduledDate),
                    ServiceProvider = "Internal Service",
                    IsPaid = false,
                    MaintenanceTypeEnum = serviceType?.ToLower() == "repair" ? EMaintenanceType.Repair : EMaintenanceType.Routine,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<MaintenanceCost>().AddAsync(maintenanceCost);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Service created for vehicle {VehicleId} by staff {StaffId}", vehicleId, staffUserId);

                return StatusCode(201, new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "SERVICE_CREATED_SUCCESS",
                    Data = new
                    {
                        ServiceId = maintenanceCost.Id,
                        VehicleId = vehicleId,
                        ServiceType = serviceType,
                        Description = description,
                        EstimatedCost = estimatedCost,
                        ScheduledDate = scheduledDate,
                        CreatedBy = staffUserId,
                        CreatedAt = maintenanceCost.CreatedAt,
                        Status = "Scheduled"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Cập nhật trạng thái dịch vụ xe
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Completed",
        ///   "actualCost": 450000,
        ///   "notes": "Service completed successfully"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật trạng thái thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy dịch vụ</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("service/{id}/status")]
        public async Task<IActionResult> UpdateServiceStatus(int id, [FromBody] object statusUpdate)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response vì service method chưa tồn tại
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SERVICE_STATUS_UPDATED_SUCCESS",
                    Data = new
                    {
                        ServiceId = id,
                        UpdatedBy = staffUserId,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service status {ServiceId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- Tranh chấp & báo cáo ---
        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tranh chấp cần xử lý
        /// </remarks>
        /// <response code="200">Lấy danh sách tranh chấp thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("disputes")]
        public async Task<IActionResult> GetDisputes([FromQuery] string? status = null, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // For now, we'll use FundUsage as a proxy for disputes since there's no explicit Dispute entity
                var allFundUsages = await _unitOfWork.FundUsageRepository.GetAllAsync();
                var filteredDisputes = allFundUsages.AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    filteredDisputes = status.ToLower() switch
                    {
                        "open" => filteredDisputes.Where(f => f.CreatedAt > DateTime.UtcNow.AddDays(-7)), // Recent ones as open
                        "in review" => filteredDisputes.Where(f => f.CreatedAt <= DateTime.UtcNow.AddDays(-7) && f.CreatedAt > DateTime.UtcNow.AddDays(-14)), // Mid-range as in review
                        "resolved" => filteredDisputes.Where(f => f.CreatedAt <= DateTime.UtcNow.AddDays(-14)), // Older ones as resolved
                        _ => filteredDisputes
                    };
                }

                // Apply pagination
                var totalCount = filteredDisputes.Count();
                var disputes = filteredDisputes
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        Id = f.Id,
                        Title = $"Fund Usage Dispute #{f.Id}",
                        Status = f.CreatedAt > DateTime.UtcNow.AddDays(-7) ? "Open" :
                                f.CreatedAt > DateTime.UtcNow.AddDays(-14) ? "In Review" : "Resolved",
                        CreatedDate = f.CreatedAt ?? DateTime.UtcNow,
                        Priority = f.Amount > 1000000 ? "High" : f.Amount > 500000 ? "Medium" : "Low",
                        Amount = f.Amount,
                        Description = f.Description ?? "Fund usage dispute"
                    })
                    .ToList();

                var result = new
                {
                    Items = disputes,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "DISPUTE_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disputes list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Cập nhật trạng thái tranh chấp
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Resolved",
        ///   "resolution": "Refund issued to customer",
        ///   "notes": "Customer satisfied with resolution"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cập nhật trạng thái thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy tranh chấp</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("dispute/{id}/status")]
        public async Task<IActionResult> UpdateDisputeStatus(int id, [FromBody] object statusUpdate)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock response vì service method signature khác
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "DISPUTE_STATUS_UPDATED_SUCCESS",
                    Data = new
                    {
                        DisputeId = id,
                        UpdatedBy = staffUserId,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dispute status {DisputeId}", id);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy báo cáo hoạt động của staff
        /// </remarks>
        /// <response code="200">Lấy báo cáo thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var startDate = fromDate ?? DateTime.UtcNow.Date;
                var endDate = toDate ?? DateTime.UtcNow.Date.AddDays(1);

                // Get real data from database
                var allCheckIns = await _unitOfWork.CheckInRepository.GetAllAsync();
                var allCheckOuts = await _unitOfWork.CheckOutRepository.GetAllAsync();
                var allMaintenanceCosts = await _unitOfWork.MaintenanceCostRepository.GetAllAsync();
                var allFundUsages = await _unitOfWork.FundUsageRepository.GetAllAsync();

                // Calculate stats for the period
                var checkInsToday = allCheckIns.Count(c => c.CreatedAt >= startDate && c.CreatedAt < endDate);
                var checkOutsToday = allCheckOuts.Count(c => c.CreatedAt >= startDate && c.CreatedAt < endDate);

                // Pending services (recent maintenance costs)
                var pendingServices = allMaintenanceCosts.Count(m => !m.IsPaid.HasValue || !m.IsPaid.Value);

                // Resolved disputes (completed fund usages)
                var resolvedDisputes = allFundUsages.Count(f => f.CreatedAt < DateTime.UtcNow.AddDays(-7));

                // Staff-related fund operations (using fund operations as proxy for contracts)
                var contractsProcessed = allFundUsages.Count(f => f.CreatedAt >= startDate && f.CreatedAt < endDate);

                var reports = new
                {
                    CheckInsToday = checkInsToday,
                    CheckOutsToday = checkOutsToday,
                    PendingServices = pendingServices,
                    ResolvedDisputes = resolvedDisputes,
                    ContractsProcessed = contractsProcessed,
                    Period = new
                    {
                        From = startDate,
                        To = endDate
                    },
                    StaffProductivity = new
                    {
                        TotalActivities = checkInsToday + checkOutsToday + contractsProcessed,
                        AverageProcessingTime = "45 minutes", // Could be calculated from actual data
                        EfficiencyScore = Math.Min(100, (checkInsToday + checkOutsToday) * 10) // Simple calculation
                    }
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "STAFF_REPORTS_RETRIEVED_SUCCESS",
                    Data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff reports");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- VEHICLE CHECK-IN/CHECK-OUT OPERATIONS ---

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Get all pending check-ins that require staff assistance
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/checkins/pending
        /// ```
        /// </remarks>
        /// <response code="200">Pending check-ins retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Staff role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("checkins/pending")]
        public async Task<IActionResult> GetPendingCheckIns()
        {
            try
            {
                // Get confirmed bookings that don't have check-ins yet
                var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
                var allCheckIns = await _unitOfWork.CheckInRepository.GetAllAsync();

                // Find bookings that are confirmed but haven't been checked in
                var confirmedBookings = allBookings.Where(b => b.StatusEnum == EBookingStatus.Confirmed).ToList();
                var checkedInBookingIds = allCheckIns.Select(c => c.BookingId).ToHashSet();

                var pendingCheckIns = new List<object>();

                foreach (var booking in confirmedBookings.Where(b => !checkedInBookingIds.Contains(b.Id)))
                {
                    // Get co-owner information
                    var coOwner = booking.CoOwnerId.HasValue ? await _unitOfWork.CoOwnerRepository.GetByIdAsync(booking.CoOwnerId.Value) : null;
                    var user = coOwner != null ? await _unitOfWork.DbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == coOwner.UserId) : null;

                    pendingCheckIns.Add(new
                    {
                        BookingId = booking.Id,
                        VehicleId = booking.VehicleId,
                        CoOwnerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown User",
                        CoOwnerEmail = user?.Email ?? "Unknown",
                        ScheduledTime = booking.StartTime,
                        Status = "Pending",
                        Purpose = booking.Purpose ?? "No purpose specified",
                        TotalCost = booking.TotalCost ?? 0
                    });
                }

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PENDING_CHECKINS_RETRIEVED_SUCCESS",
                    Data = pendingCheckIns
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending check-ins");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Perform staff-assisted check-in for co-owner
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "vehicleCondition": "Good",
        ///   "notes": "Vehicle ready for pickup"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Check-in completed successfully</response>
        /// <response code="400">Invalid booking data</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Staff role required</response>
        /// <response code="404">Booking not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("checkins/staff-assisted")]
        public async Task<IActionResult> PerformStaffAssistedCheckIn([FromBody] object request)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock implementation - replace with actual service call
                var result = new
                {
                    CheckInId = new Random().Next(1000, 9999),
                    BookingId = 123,
                    CompletedAt = DateTime.UtcNow,
                    StaffId = staffUserId,
                    Status = "Completed"
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "STAFF_CHECKIN_COMPLETED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing staff-assisted check-in");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Perform staff-assisted check-out for co-owner
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "vehicleCondition": "Good",
        ///   "mileage": 45230,
        ///   "fuelLevel": 85,
        ///   "notes": "Vehicle returned in good condition"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Check-out completed successfully</response>
        /// <response code="400">Invalid booking data</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Staff role required</response>
        /// <response code="404">Booking not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("checkouts/staff-assisted")]
        public async Task<IActionResult> PerformStaffAssistedCheckOut([FromBody] object request)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock implementation - replace with actual service call
                var result = new
                {
                    CheckOutId = new Random().Next(1000, 9999),
                    BookingId = 123,
                    CompletedAt = DateTime.UtcNow,
                    StaffId = staffUserId,
                    Status = "Completed"
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "STAFF_CHECKOUT_COMPLETED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing staff-assisted check-out");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        // --- VEHICLE MAINTENANCE MANAGEMENT ---

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Get all maintenance requests that require staff attention
        /// </remarks>
        /// <response code="200">Maintenance requests retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Staff role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("maintenance/requests")]
        public async Task<IActionResult> GetMaintenanceRequests([FromQuery] string? status = null)
        {
            try
            {
                // Get maintenance costs as maintenance requests
                var allMaintenanceCosts = await _unitOfWork.MaintenanceCostRepository.GetAllAsync();
                var filteredRequests = allMaintenanceCosts.AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    filteredRequests = status.ToLower() switch
                    {
                        "pending" => filteredRequests.Where(m => !m.IsPaid.HasValue || !m.IsPaid.Value), // Unpaid as pending
                        "in progress" => filteredRequests.Where(m => m.ServiceDate > DateOnly.FromDateTime(DateTime.UtcNow) && (m.IsPaid.HasValue && m.IsPaid.Value)), // Future service date and paid
                        "completed" => filteredRequests.Where(m => m.ServiceDate <= DateOnly.FromDateTime(DateTime.UtcNow) && (m.IsPaid.HasValue && m.IsPaid.Value)), // Past service date and paid
                        _ => filteredRequests
                    };
                }

                var requests = filteredRequests
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList() // Execute query first
                    .Select(m => new
                    {
                        Id = m.Id,
                        VehicleId = m.VehicleId,
                        Type = m.MaintenanceTypeEnum?.ToString() ?? "Routine",
                        Status = (!m.IsPaid.HasValue || !m.IsPaid.Value) ? "Pending" :
                                m.ServiceDate > DateOnly.FromDateTime(DateTime.UtcNow) ? "In Progress" : "Completed",
                        RequestedDate = m.CreatedAt ?? DateTime.UtcNow,
                        ServiceDate = m.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Priority = m.MaintenanceTypeEnum == EMaintenanceType.Emergency ? "High" :
                                  m.MaintenanceTypeEnum == EMaintenanceType.Repair ? "Medium" : "Low",
                        Cost = m.Cost,
                        Description = m.Description ?? "Maintenance request",
                        ServiceProvider = m.ServiceProvider ?? "Internal",
                        IsPaid = m.IsPaid ?? false
                    })
                    .ToList();

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_REQUESTS_RETRIEVED_SUCCESS",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maintenance requests");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Update maintenance request status
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Completed",
        ///   "notes": "Oil change completed successfully",
        ///   "cost": 150.00
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Maintenance request updated successfully</response>
        /// <response code="400">Invalid maintenance data</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Forbidden - Staff role required</response>
        /// <response code="404">Maintenance request not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("maintenance/{maintenanceId}/status")]
        public async Task<IActionResult> UpdateMaintenanceStatus(int maintenanceId, [FromBody] object request)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Mock implementation - replace with actual service call
                var result = new
                {
                    MaintenanceId = maintenanceId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = staffUserId,
                    NewStatus = "Completed"
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_STATUS_UPDATED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating maintenance status for ID {MaintenanceId}", maintenanceId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #region Profile Management

        /// <summary>
        /// Get your staff profile information
        /// </summary>
        /// <remarks>
        /// Retrieve your complete staff profile with statistics and settings.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/profile
        /// ```
        /// </remarks>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetProfileAsync(userId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff profile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Update your staff profile information
        /// </summary>
        /// <remarks>
        /// Update personal information including name, contact details, and bio.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "phone": "+1-555-0123",
        ///   "address": "123 Main St, City, Country",
        ///   "dateOfBirth": "1990-01-15",
        ///   "bio": "Experienced EV maintenance specialist"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Profile update request</param>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdateProfileAsync(userId, request);

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
                _logger.LogError(ex, "Error updating staff profile");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Change your password
        /// </summary>
        /// <remarks>
        /// Change your account password for security.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "currentPassword": "currentPassword123!",
        ///   "newPassword": "newPassword456@",
        ///   "confirmPassword": "newPassword456@"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Password change request</param>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid current password or validation error</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("profile/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.ChangePasswordAsync(userId, request);

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
                _logger.LogError(ex, "Error changing staff password");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        /// <remarks>
        /// Configure which notifications you want to receive.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "emailNotifications": true,
        ///   "pushNotifications": true,
        ///   "bookingReminders": false,
        ///   "maintenanceAlerts": true,
        ///   "paymentNotifications": true,
        ///   "systemAnnouncements": true
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Notification settings request</param>
        /// <response code="200">Notification settings updated successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile/notification-settings")]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdateNotificationSettingsAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff notification settings");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Update privacy settings
        /// </summary>
        /// <remarks>
        /// Configure your privacy preferences and data sharing settings.
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "profileVisibility": true,
        ///   "showEmail": false,
        ///   "showPhone": false,
        ///   "shareUsageData": true,
        ///   "allowDataAnalytics": true
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Privacy settings request</param>
        /// <response code="200">Privacy settings updated successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("profile/privacy-settings")]
        public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.UpdatePrivacySettingsAsync(userId, request);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff privacy settings");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Get your activity log
        /// </summary>
        /// <remarks>
        /// View your account activity history and actions performed.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/profile/activity-log?page=1&amp;pageSize=50&amp;category=maintenance
        /// ```
        /// </remarks>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50)</param>
        /// <param name="category">Optional activity category filter</param>
        /// <response code="200">Activity log retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile/activity-log")]
        public async Task<IActionResult> GetActivityLog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? category = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetActivityLogAsync(userId, page, pageSize, category);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff activity log");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Get your security log
        /// </summary>
        /// <remarks>
        /// View security events and login history for your account.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/profile/security-log?days=30
        /// ```
        /// </remarks>
        /// <param name="days">Number of days to look back (default: 30)</param>
        /// <response code="200">Security log retrieved successfully</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile/security-log")]
        public async Task<IActionResult> GetSecurityLog([FromQuery] int days = 30)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _profileService.GetSecurityLogAsync(userId, days);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff security log");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Get list of groups assigned to the current staff member for support and management.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/groups/assigned
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "ASSIGNED_GROUPS_RETRIEVED_SUCCESSFULLY",
        ///   "data": [
        ///     {
        ///       "groupId": 1,
        ///       "groupName": "EV Enthusiasts Group",
        ///       "memberCount": 5,
        ///       "vehicleCount": 2,
        ///       "status": "Active",
        ///       "assignedDate": "2024-01-15T10:00:00Z",
        ///       "openDisputeCount": 1,
        ///       "pendingRequestCount": 3,
        ///       "totalFundAmount": 50000.00,
        ///       "priority": "High"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Assigned groups retrieved successfully</response>
        /// <response code="403">Access denied - not authorized</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("groups/assigned")]
        public async Task<IActionResult> GetAssignedGroups()
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.GetAssignedGroupsAsync(staffId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => Forbid(),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned groups for staff");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Get detailed information about a specific group including members, vehicles, activities, and financial summary.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/group/1/details
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "GROUP_DETAILS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "groupId": 1,
        ///     "groupName": "EV Enthusiasts Group",
        ///     "description": "A group of EV enthusiasts sharing vehicles",
        ///     "createdDate": "2024-01-01T00:00:00Z",
        ///     "status": "Active",
        ///     "members": [
        ///       {
        ///         "userId": 101,
        ///         "fullName": "John Doe",
        ///         "email": "john@example.com",
        ///         "ownershipPercentage": 40.0,
        ///         "role": "Owner",
        ///         "joinedDate": "2024-01-01T00:00:00Z",
        ///         "status": "Active",
        ///         "contributedAmount": 20000.00
        ///       }
        ///     ],
        ///     "vehicles": [
        ///       {
        ///         "vehicleId": 201,
        ///         "vehicleName": "Tesla Model 3",
        ///         "brand": "Tesla",
        ///         "model": "Model 3",
        ///         "year": 2023,
        ///         "licensePlate": "ABC-123",
        ///         "status": "Active",
        ///         "purchasePrice": 45000.00,
        ///         "acquisitionDate": "2024-01-15T00:00:00Z",
        ///         "totalBookings": 25,
        ///         "utilizationRate": 75.5
        ///       }
        ///     ],
        ///     "financialSummary": {
        ///       "totalFunds": 50000.00,
        ///       "availableFunds": 35000.00,
        ///       "reservedFunds": 15000.00,
        ///       "monthlyIncome": 3000.00,
        ///       "monthlyExpenses": 1500.00
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">ID of the group to get details for</param>
        /// <response code="200">Group details retrieved successfully</response>
        /// <response code="403">Access denied - not assigned to this group</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("group/{groupId:int}/details")]
        public async Task<IActionResult> GetGroupDetails([FromRoute] int groupId)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.GetGroupDetailsAsync(groupId, staffId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group details for group {GroupId}", groupId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Get all disputes within a specific group for resolution and management.
        /// 
        /// Sample request:
        /// ```
        /// GET /api/staff/group/1/disputes
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "GROUP_DISPUTES_RETRIEVED_SUCCESSFULLY",
        ///   "data": [
        ///     {
        ///       "disputeId": 301,
        ///       "title": "Vehicle Maintenance Cost Dispute",
        ///       "description": "Disagreement about maintenance cost allocation",
        ///       "status": "Open",
        ///       "priority": "High",
        ///       "createdDate": "2024-11-01T10:00:00Z",
        ///       "reportedByUserId": 101,
        ///       "reportedByUserName": "John Doe",
        ///       "assignedStaffId": 501,
        ///       "assignedStaffName": "Staff Member",
        ///       "messages": [
        ///         {
        ///           "messageId": 1001,
        ///           "message": "Initial dispute report",
        ///           "createdDate": "2024-11-01T10:00:00Z",
        ///           "createdByUserId": 101,
        ///           "createdByUserName": "John Doe",
        ///           "userRole": "Member"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="groupId">ID of the group to get disputes for</param>
        /// <response code="200">Group disputes retrieved successfully</response>
        /// <response code="403">Access denied - not assigned to this group</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("group/{groupId:int}/disputes")]
        public async Task<IActionResult> GetGroupDisputes([FromRoute] int groupId)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.GetGroupDisputesAsync(groupId, staffId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving disputes for group {GroupId}", groupId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Create a support ticket for a group to track and resolve issues.
        /// 
        /// Sample request:
        /// ```
        /// POST /api/staff/group/support
        /// Content-Type: application/json
        /// 
        /// {
        ///   "groupId": 1,
        ///   "title": "Vehicle Maintenance Issue",
        ///   "description": "The group's Tesla Model 3 needs immediate brake inspection",
        ///   "priority": "High",
        ///   "category": "Technical",
        ///   "notifyMembers": true
        /// }
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 201,
        ///   "message": "SUPPORT_TICKET_CREATED_SUCCESSFULLY",
        ///   "data": {
        ///     "ticketId": 1001,
        ///     "title": "Vehicle Maintenance Issue",
        ///     "status": "Open",
        ///     "priority": "High",
        ///     "createdDate": "2024-11-01T14:30:00Z",
        ///     "createdByStaffId": 501,
        ///     "createdByStaffName": "Staff Member"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Support ticket creation details</param>
        /// <response code="201">Support ticket created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied - not assigned to this group</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("group/support")]
        public async Task<IActionResult> CreateSupportTicket([FromBody] CreateSupportTicketRequest request)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.CreateSupportTicketAsync(request, staffId);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(CreateSupportTicket), response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket for group {GroupId}", request.GroupId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Update the status of a dispute and add resolution notes.
        /// 
        /// Sample request:
        /// ```
        /// PUT /api/staff/dispute/301/status
        /// Content-Type: application/json
        /// 
        /// {
        ///   "newStatus": "Resolved",
        ///   "resolutionNotes": "Issue resolved through mediation. Cost split agreed upon by all parties."
        /// }
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "DISPUTE_STATUS_UPDATED_SUCCESSFULLY",
        ///   "data": {
        ///     "disputeId": 301,
        ///     "title": "Vehicle Maintenance Cost Dispute",
        ///     "status": "Resolved",
        ///     "priority": "High",
        ///     "resolvedDate": "2024-11-01T16:45:00Z",
        ///     "assignedStaffId": 501,
        ///     "assignedStaffName": "Staff Member"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="disputeId">ID of the dispute to update</param>
        /// <param name="request">Update dispute status request containing new status and resolution notes</param>
        /// <response code="200">Dispute status updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied - not assigned to this dispute</response>
        /// <response code="404">Dispute not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("dispute/{disputeId:int}/status")]
        public async Task<IActionResult> UpdateDisputeStatus([FromRoute] int disputeId, [FromBody] UpdateDisputeStatusRequest request)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.UpdateDisputeStatusAsync(disputeId, request.NewStatus, request.ResolutionNotes, staffId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dispute {DisputeId} status", disputeId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Add a message to an existing dispute for communication and progress tracking.
        /// 
        /// Sample request:
        /// ```
        /// POST /api/staff/dispute/301/message
        /// Content-Type: application/json
        /// 
        /// {
        ///   "message": "I have reviewed the maintenance receipts and will schedule a mediation session for next Tuesday."
        /// }
        /// ```
        /// 
        /// Sample response:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "DISPUTE_MESSAGE_ADDED_SUCCESSFULLY",
        ///   "data": {
        ///     "disputeId": 301,
        ///     "title": "Vehicle Maintenance Cost Dispute",
        ///     "status": "In Progress",
        ///     "messages": [
        ///       {
        ///         "messageId": 1002,
        ///         "message": "I have reviewed the maintenance receipts and will schedule a mediation session for next Tuesday.",
        ///         "createdDate": "2024-11-01T15:20:00Z",
        ///         "createdByUserId": 501,
        ///         "createdByUserName": "Staff Member",
        ///         "userRole": "Staff"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="disputeId">ID of the dispute to add message to</param>
        /// <param name="request">Message content</param>
        /// <response code="200">Message added successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="403">Access denied - not assigned to this dispute</response>
        /// <response code="404">Dispute not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("dispute/{disputeId:int}/message")]
        public async Task<IActionResult> AddDisputeMessage([FromRoute] int disputeId, [FromBody] AddDisputeMessageRequest request)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _groupManagementService.AddDisputeMessageAsync(disputeId, request.Message, staffId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => Forbid(),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message to dispute {DisputeId}", disputeId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        #endregion

        #region License Management

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy danh sách tất cả license theo trạng thái (Staff có quyền như Admin)
        /// 
        /// **Query Parameters:**
        /// - status: pending, verified, rejected, expired (optional - lấy tất cả nếu không có)
        /// - page: Số trang (mặc định 1)
        /// - pageSize: Số item per page (mặc định 10)
        /// </remarks>
        /// <response code="200">Lấy danh sách license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("licenses")]
        public async Task<IActionResult> GetLicenses(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get driving licenses with user information from database
                var coOwners = await _unitOfWork.CoOwnerRepository.GetAllAsync();
                var licenses = coOwners
                    .Where(co => co.DrivingLicenses != null && co.DrivingLicenses.Any())
                    .SelectMany(co => co.DrivingLicenses.Select(dl => new LicenseListResponse
                    {
                        Id = dl.Id,
                        LicenseNumber = dl.LicenseNumber,
                        IssuedBy = dl.IssuedBy,
                        IssueDate = dl.IssueDate,
                        ExpiryDate = dl.ExpiryDate,
                        LicenseImageUrl = dl.LicenseImageUrl,
                        VerificationStatus = dl.VerificationStatus,
                        RejectReason = dl.RejectReason,
                        UserName = $"{co.User?.FirstName} {co.User?.LastName}".Trim(),
                        UserId = co.UserId,
                        SubmittedAt = dl.CreatedAt,
                        VerifiedByUserName = dl.VerifiedByUser != null ? $"{dl.VerifiedByUser.FirstName} {dl.VerifiedByUser.LastName}".Trim() : null,
                        VerifiedAt = dl.VerifiedAt,
                        IsExpired = dl.ExpiryDate.HasValue && dl.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Now)
                    }))
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<EDrivingLicenseVerificationStatus>(status, true, out var statusEnum))
                    {
                        licenses = licenses.Where(l => l.VerificationStatus == statusEnum);
                    }
                }

                // Apply pagination
                var totalCount = licenses.Count();
                var paginatedLicenses = licenses
                    .OrderByDescending(x => x.SubmittedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new
                {
                    Items = paginatedLicenses,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = page * pageSize < totalCount,
                    HasPreviousPage = page > 1
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "LICENSE_LIST_RETRIEVED_SUCCESS",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses list");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Duyệt license cho người dùng (Staff có quyền như Admin)
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "licenseId": 1,
        ///   "notes": "License verified successfully by staff"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Duyệt license thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/approve")]
        public async Task<IActionResult> ApproveLicense([FromBody] ApproveLicenseRequest request)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get the license from database with details
                var license = await _unitOfWork.DrivingLicenseRepository.GetByIdWithDetailsAsync(request.LicenseId);
                if (license == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    });
                }

                // Update license status
                license.VerificationStatus = EDrivingLicenseVerificationStatus.Verified;
                license.VerifiedByUserId = staffUserId;
                license.VerifiedAt = DateTime.UtcNow;
                license.UpdatedAt = DateTime.UtcNow;
                license.RejectReason = null; // Clear any previous reject reason

                await _unitOfWork.DrivingLicenseRepository.UpdateAsync(license);
                await _unitOfWork.SaveChangesAsync();

                // Get staff user for response
                var staffUser = await _unitOfWork.UserRepository.GetByIdAsync(staffUserId);

                var response = new LicenseApprovalResponse
                {
                    LicenseId = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    VerificationStatus = license.VerificationStatus,
                    VerifiedByUserName = $"{staffUser?.FirstName} {staffUser?.LastName}".Trim(),
                    VerifiedAt = license.VerifiedAt.Value
                };

                _logger.LogInformation("License {LicenseId} approved by staff user {StaffUserId}", request.LicenseId, staffUserId);

                return Ok(new BaseResponse<LicenseApprovalResponse>
                {
                    StatusCode = 200,
                    Message = "LICENSE_APPROVED_SUCCESSFULLY",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving license {LicenseId}", request.LicenseId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Từ chối license cho người dùng với lý do cụ thể (Staff có quyền như Admin)
        /// 
        /// **Request Body:**
        /// ```json
        /// {
        ///   "licenseId": 1,
        ///   "rejectReason": "License đã hết hạn",
        ///   "notes": "Vui lòng cập nhật license mới"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Từ chối license thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpPatch("license/reject")]
        public async Task<IActionResult> RejectLicense([FromBody] RejectLicenseRequest request)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get the license from database with details
                var license = await _unitOfWork.DrivingLicenseRepository.GetByIdWithDetailsAsync(request.LicenseId);
                if (license == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    });
                }

                // Update license status
                license.VerificationStatus = EDrivingLicenseVerificationStatus.Rejected;
                license.RejectReason = request.RejectReason;
                license.VerifiedByUserId = staffUserId;
                license.VerifiedAt = DateTime.UtcNow;
                license.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.DrivingLicenseRepository.UpdateAsync(license);
                await _unitOfWork.SaveChangesAsync();

                // Get staff user for response
                var staffUser = await _unitOfWork.UserRepository.GetByIdAsync(staffUserId);

                var response = new LicenseApprovalResponse
                {
                    LicenseId = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    VerificationStatus = license.VerificationStatus,
                    RejectReason = license.RejectReason,
                    VerifiedByUserName = $"{staffUser?.FirstName} {staffUser?.LastName}".Trim(),
                    VerifiedAt = license.VerifiedAt.Value
                };

                _logger.LogInformation("License {LicenseId} rejected by staff user {StaffUserId} with reason: {RejectReason}", 
                    request.LicenseId, staffUserId, request.RejectReason);

                return Ok(new BaseResponse<LicenseApprovalResponse>
                {
                    StatusCode = 200,
                    Message = "LICENSE_REJECTED_SUCCESSFULLY",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting license {LicenseId}", request.LicenseId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Staff
        /// </summary>
        /// <remarks>
        /// Lấy chi tiết license theo ID (Staff có quyền xem để review)
        /// </remarks>
        /// <response code="200">Lấy chi tiết license thành công</response>
        /// <response code="401">Chưa đăng nhập</response>
        /// <response code="403">Không có quyền truy cập - chỉ Staff</response>
        /// <response code="404">Không tìm thấy license</response>
        /// <response code="500">Lỗi server</response>
        [HttpGet("license/{licenseId:int}")]
        public async Task<IActionResult> GetLicenseDetails(int licenseId)
        {
            try
            {
                var license = await _unitOfWork.DrivingLicenseRepository.GetByIdWithDetailsAsync(licenseId);
                if (license == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "LICENSE_NOT_FOUND"
                    });
                }

                var response = new LicenseListResponse
                {
                    Id = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    IssuedBy = license.IssuedBy,
                    IssueDate = license.IssueDate,
                    ExpiryDate = license.ExpiryDate,
                    LicenseImageUrl = license.LicenseImageUrl,
                    VerificationStatus = license.VerificationStatus,
                    RejectReason = license.RejectReason,
                    UserName = license.CoOwner != null ? $"{license.CoOwner.User?.FirstName} {license.CoOwner.User?.LastName}".Trim() : "",
                    UserId = license.CoOwner?.UserId ?? 0,
                    SubmittedAt = license.CreatedAt,
                    VerifiedByUserName = license.VerifiedByUser != null ? $"{license.VerifiedByUser.FirstName} {license.VerifiedByUser.LastName}".Trim() : null,
                    VerifiedAt = license.VerifiedAt,
                    IsExpired = license.ExpiryDate.HasValue && license.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Now)
                };

                return Ok(new BaseResponse<LicenseListResponse>
                {
                    StatusCode = 200,
                    Message = "LICENSE_DETAILS_RETRIEVED_SUCCESS",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license details for ID {LicenseId}", licenseId);
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion
    }
}
