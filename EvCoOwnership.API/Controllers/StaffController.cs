using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Helpers.BaseClasses;

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
        private readonly IServiceService _serviceService;
        private readonly IDisputeService _disputeService;
        private readonly ILogger<StaffController> _logger;

        /// <summary>
        /// Constructor for StaffController
        /// </summary>
        /// <param name="groupService">Group service for group management</param>
        /// <param name="contractService">Contract service for contract management</param>
        /// <param name="checkInCheckOutService">Check-in/out service</param>
        /// <param name="serviceService">Service management service</param>
        /// <param name="disputeService">Dispute management service</param>
        /// <param name="logger">Logger for logging</param>
        public StaffController(
            IGroupService groupService,
            IContractService contractService,
            ICheckInCheckOutService checkInCheckOutService,
            IServiceService serviceService,
            IDisputeService disputeService,
            ILogger<StaffController> logger)
        {
            _groupService = groupService;
            _contractService = contractService;
            _checkInCheckOutService = checkInCheckOutService;
            _serviceService = serviceService;
            _disputeService = disputeService;
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
                var request = new { Status = status, PageIndex = pageIndex, PageSize = pageSize };
                
                // Tạm thời sử dụng mock data vì cần update DTO
                var contracts = new
                {
                    Items = new[]
                    {
                        new { Id = 1, Title = "Vehicle Co-ownership Agreement", Status = "Pending", CreatedDate = DateTime.UtcNow.AddDays(-2) },
                        new { Id = 2, Title = "Maintenance Contract", Status = "Active", CreatedDate = DateTime.UtcNow.AddDays(-5) }
                    },
                    TotalCount = 2,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
                
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_LIST_RETRIEVED_SUCCESS",
                    Data = contracts
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
        public async Task<IActionResult> CheckIn([FromBody] object checkInData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Mock response vì service method chưa tồn tại
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CHECK_IN_SUCCESS",
                    Data = new 
                    { 
                        CheckInId = new Random().Next(1000, 9999),
                        StaffId = staffUserId, 
                        CheckInTime = DateTime.UtcNow,
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
        public async Task<IActionResult> CheckOut([FromBody] object checkOutData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Mock response vì service method chưa tồn tại
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "CHECK_OUT_SUCCESS",
                    Data = new 
                    { 
                        CheckOutId = new Random().Next(1000, 9999),
                        StaffId = staffUserId, 
                        CheckOutTime = DateTime.UtcNow,
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
                // Mock data vì service method chưa tồn tại
                var services = new
                {
                    Items = new[]
                    {
                        new { Id = 1, Type = "Maintenance", VehicleId = 1, Status = "Scheduled", Cost = 500000, Date = DateTime.UtcNow.AddDays(3) },
                        new { Id = 2, Type = "Repair", VehicleId = 2, Status = "In Progress", Cost = 1200000, Date = DateTime.UtcNow.AddDays(-1) }
                    },
                    TotalCount = 2,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
                
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "SERVICE_LIST_RETRIEVED_SUCCESS",
                    Data = services
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
        public async Task<IActionResult> CreateService([FromBody] object serviceData)
        {
            try
            {
                var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Mock response vì service method chưa tồn tại
                return StatusCode(201, new BaseResponse<object>
                {
                    StatusCode = 201,
                    Message = "SERVICE_CREATED_SUCCESS",
                    Data = new 
                    { 
                        ServiceId = new Random().Next(1000, 9999),
                        CreatedBy = staffUserId, 
                        CreatedAt = DateTime.UtcNow,
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
                // Mock data vì service method signature khác
                var disputes = new
                {
                    Items = new[]
                    {
                        new { Id = 1, Title = "Billing Dispute", Status = "Open", CreatedDate = DateTime.UtcNow.AddDays(-2), Priority = "High" },
                        new { Id = 2, Title = "Vehicle Damage Claim", Status = "In Review", CreatedDate = DateTime.UtcNow.AddDays(-5), Priority = "Medium" }
                    },
                    TotalCount = 2,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
                
                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "DISPUTE_LIST_RETRIEVED_SUCCESS",
                    Data = disputes
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
                
                var reports = new
                {
                    CheckInsToday = 15,
                    CheckOutsToday = 12,
                    PendingServices = 8,
                    ResolvedDisputes = 5,
                    ContractsProcessed = 3,
                    Period = new
                    {
                        From = fromDate ?? DateTime.UtcNow.Date,
                        To = toDate ?? DateTime.UtcNow.Date.AddDays(1)
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
    }
}
