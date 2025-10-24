using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.DepositDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for deposit/fund management with multiple payment methods
    /// </summary>
    /// <remarks>
    /// Supports deposits via:
    /// - Credit Card (Visa, Mastercard, JCB via VNPay)
    /// - E-Wallet (Momo, ZaloPay)
    /// - Online Banking (Vietnamese banks via VNPay)
    /// - QR Code Payment
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles]
    public class DepositController : ControllerBase
    {
        private readonly IDepositService _depositService;

        public DepositController(IDepositService depositService)
        {
            _depositService = depositService;
        }

        /// <summary>
        /// Creates a new deposit transaction and returns payment URL
        /// </summary>
        /// <remarks>
        /// Sample request for **Credit Card**:
        /// ```json
        /// {
        ///   "amount": 500000,
        ///   "depositMethod": 0,
        ///   "description": "Deposit for vehicle maintenance"
        /// }
        /// ```
        /// 
        /// Sample request for **E-Wallet (Momo)**:
        /// ```json
        /// {
        ///   "amount": 200000,
        ///   "depositMethod": 1,
        ///   "eWalletProvider": "MOMO",
        ///   "description": "Deposit via Momo"
        /// }
        /// ```
        /// 
        /// Sample request for **Online Banking**:
        /// ```json
        /// {
        ///   "amount": 1000000,
        ///   "depositMethod": 2,
        ///   "bankCode": "VIETCOMBANK",
        ///   "description": "Deposit via Vietcombank"
        /// }
        /// ```
        /// 
        /// **Deposit Methods:**
        /// - 0 = CreditCard
        /// - 1 = EWallet
        /// - 2 = OnlineBanking
        /// - 3 = QRCode
        /// 
        /// **Supported Banks:** VIETCOMBANK, VIETINBANK, BIDV, AGRIBANK, TECHCOMBANK, MBBANK, TPBANK, ACB, VPBank, SHB, SACOMBANK
        /// 
        /// **Supported E-Wallets:** MOMO, ZALOPAY
        /// </remarks>
        /// <param name="request">Deposit creation request</param>
        /// <response code="201">Deposit created successfully. Redirect user to PaymentUrl</response>
        /// <response code="400">Validation error (invalid amount, method, etc.)</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _depositService.CreateDepositAsync(userId, request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets deposit transaction by ID
        /// </summary>
        /// <remarks>
        /// Returns detailed information about a specific deposit transaction.
        /// Users can only view their own deposits. Admins can view all deposits.
        /// </remarks>
        /// <param name="id">Deposit ID</param>
        /// <response code="200">Deposit details retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="403">Forbidden - user cannot access this deposit</response>
        /// <response code="404">Deposit not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDeposit(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _depositService.GetDepositByIdAsync(id, userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets current user's deposit history with filters
        /// </summary>
        /// <remarks>
        /// Returns paginated list of user's deposit transactions with filtering options.
        /// 
        /// **Query Parameters:**
        /// - `depositMethod`: Filter by method (0=CreditCard, 1=EWallet, 2=OnlineBanking, 3=QRCode)
        /// - `status`: Filter by status (0=Pending, 1=Processing, 2=Completed, 3=Failed, 4=Cancelled, 5=Expired, 6=Refunded)
        /// - `fromDate`: Filter deposits from this date (ISO 8601 format)
        /// - `toDate`: Filter deposits to this date (ISO 8601 format)
        /// - `minAmount`: Minimum deposit amount
        /// - `maxAmount`: Maximum deposit amount
        /// - `pageNumber`: Page number (default: 1)
        /// - `pageSize`: Items per page (1-100, default: 20)
        /// - `sortBy`: Sort field (CreatedAt, Amount, Status, CompletedAt)
        /// - `sortOrder`: Sort order (asc, desc)
        /// 
        /// Example: `/api/deposit/my-deposits?status=2&amp;pageNumber=1&amp;pageSize=20&amp;sortBy=CreatedAt&amp;sortOrder=desc`
        /// </remarks>
        /// <param name="request">Filter and pagination request</param>
        /// <response code="200">Deposits retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("my-deposits")]
        public async Task<IActionResult> GetMyDeposits([FromQuery] GetDepositsRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _depositService.GetUserDepositsAsync(userId, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// [Admin/Staff] Gets all deposits with filters
        /// </summary>
        /// <remarks>
        /// **Admin and Staff only.**
        /// 
        /// Returns paginated list of all deposit transactions with filtering options.
        /// Same filtering parameters as `my-deposits` endpoint.
        /// </remarks>
        /// <param name="request">Filter and pagination request</param>
        /// <response code="200">Deposits retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="403">Forbidden - requires Admin or Staff role</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetAllDeposits([FromQuery] GetDepositsRequest request)
        {
            var response = await _depositService.GetAllDepositsAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Cancels a pending deposit
        /// </summary>
        /// <remarks>
        /// Users can only cancel deposits with status = Pending.
        /// Once cancelled, the deposit cannot be resumed.
        /// </remarks>
        /// <param name="id">Deposit ID to cancel</param>
        /// <response code="200">Deposit cancelled successfully</response>
        /// <response code="400">Cannot cancel - deposit is not pending</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="403">Forbidden - user cannot cancel this deposit</response>
        /// <response code="404">Deposit not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelDeposit(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _depositService.CancelDepositAsync(id, userId);
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
        /// Gets current user's deposit statistics
        /// </summary>
        /// <remarks>
        /// Returns aggregated statistics about user's deposit history:
        /// - Total deposits count
        /// - Deposits by status (pending, completed, failed, cancelled)
        /// - Total amounts by status
        /// - Breakdown by deposit method
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("my-statistics")]
        public async Task<IActionResult> GetMyStatistics()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _depositService.GetUserDepositStatisticsAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets available payment methods information
        /// </summary>
        /// <remarks>
        /// Returns list of supported deposit methods with:
        /// - Method name and description
        /// - Availability status
        /// - Min/max deposit amounts
        /// - Supported banks/e-wallets
        /// 
        /// **No authentication required** - public endpoint.
        /// </remarks>
        /// <response code="200">Payment methods retrieved successfully</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("payment-methods")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var response = await _depositService.GetAvailablePaymentMethodsAsync();
            return response.StatusCode switch
            {
                200 => Ok(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Payment gateway callback endpoint
        /// </summary>
        /// <remarks>
        /// **Internal endpoint** - called by payment gateway (VNPay, Momo, ZaloPay) after payment completion.
        /// 
        /// **Do not require authentication** - payment gateway cannot send JWT token.
        /// 
        /// Verifies payment signature and updates deposit status accordingly.
        /// </remarks>
        /// <param name="depositId">Deposit ID</param>
        /// <param name="gatewayTransactionId">Gateway transaction ID</param>
        /// <param name="isSuccess">Payment success status</param>
        /// <param name="responseCode">Gateway response code</param>
        /// <param name="secureHash">Signature hash for verification</param>
        /// <response code="200">Callback processed successfully</response>
        /// <response code="400">Invalid callback data or signature</response>
        /// <response code="404">Deposit not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(
            [FromQuery] int depositId,
            [FromQuery] string gatewayTransactionId,
            [FromQuery] bool isSuccess,
            [FromQuery] string? responseCode,
            [FromQuery] string? secureHash)
        {
            var request = new VerifyDepositCallbackRequest
            {
                DepositId = depositId,
                GatewayTransactionId = gatewayTransactionId,
                IsSuccess = isSuccess,
                ResponseCode = responseCode,
                SecureHash = secureHash
            };

            var response = await _depositService.VerifyDepositCallbackAsync(request);

            if (response.StatusCode == 200 && response.Data != null)
            {
                // Redirect to frontend based on deposit status
                var frontendUrl = GetFrontendUrl();
                var deposit = response.Data;

                if (deposit.Status == EDepositStatus.Completed)
                {
                    return Redirect($"{frontendUrl}/deposit/success?depositId={deposit.DepositId}&amount={deposit.Amount}");
                }
                else
                {
                    return Redirect($"{frontendUrl}/deposit/failure?depositId={deposit.DepositId}&reason={Uri.EscapeDataString(deposit.FailureReason ?? "Payment failed")}");
                }
            }

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Verifies deposit callback (alternative POST endpoint)
        /// </summary>
        /// <remarks>
        /// **Internal endpoint** - alternative POST version for payment gateway callbacks.
        /// 
        /// Some payment gateways (like Momo, ZaloPay) use POST callbacks.
        /// </remarks>
        /// <param name="request">Callback verification request</param>
        /// <response code="200">Callback processed successfully</response>
        /// <response code="400">Invalid callback data or already processed</response>
        /// <response code="404">Deposit not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("verify-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCallback([FromBody] VerifyDepositCallbackRequest request)
        {
            var response = await _depositService.VerifyDepositCallbackAsync(request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        private string GetFrontendUrl()
        {
            // TODO: Get from configuration
            return "http://localhost:3000"; // Default frontend URL
        }
    }
}
