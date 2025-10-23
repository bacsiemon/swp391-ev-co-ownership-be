using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for payment management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        /// <summary>
        /// Initializes a new instance of the PaymentController
        /// </summary>
        /// <param name="paymentService">Payment service</param>
        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Creates a new payment and returns payment URL
        /// </summary>
        /// <param name="request">Create payment request</param>
        /// <response code="201">Payment created successfully, payment URL returned</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _paymentService.CreatePaymentAsync(userId, request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Processes payment callback from payment gateway
        /// </summary>
        /// <param name="request">Process payment request</param>
        /// <response code="200">Payment processed successfully</response>
        /// <response code="400">Payment already processed</response>
        /// <response code="404">Payment not found</response>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            var response = await _paymentService.ProcessPaymentAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets payment by ID
        /// </summary>
        /// <param name="id">Payment ID</param>
        /// <response code="200">Payment retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Payment not found</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var response = await _paymentService.GetPaymentByIdAsync(id);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets current user's payments
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Payments retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments(int pageIndex = 1, int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _paymentService.GetUserPaymentsAsync(userId, pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets all payments (Admin/Staff only)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Payments retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetAllPayments(int pageIndex = 1, int pageSize = 10)
        {
            var response = await _paymentService.GetAllPaymentsAsync(pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Cancels a pending payment
        /// </summary>
        /// <param name="id">Payment ID</param>
        /// <response code="200">Payment cancelled successfully</response>
        /// <response code="400">Cannot cancel processed payment</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Payment not found</response>
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelPayment(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _paymentService.CancelPaymentAsync(id, userId);
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
        /// Gets payment statistics (Admin/Staff only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _paymentService.GetPaymentStatisticsAsync();
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }
    }
}
