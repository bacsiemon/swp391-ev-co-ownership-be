using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Payment and Invoice Management API
    /// </summary>
    [Route("api/payment")]
    [ApiController]
    [AuthorizeRoles(EUserRole.CoOwner)]
    public class PaymentController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
        }

        #region Invoice Management

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Gets list of invoices for the current user
        /// 
        /// **GET INVOICES**
        /// 
        /// **Returns:**
        /// - List of user's invoices with status and details
        /// - Can filter by status (Pending, Paid, Overdue)
        /// - Pagination support
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/payment/invoices?status=Pending&amp;page=1&amp;pageSize=10
        /// ```
        /// </remarks>
        /// <response code="200">Invoices retrieved successfully</response>
        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get user's payments (invoices are linked through payments)
                var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var userPayments = allPayments.Where(p => p.UserId == userId);

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<EPaymentStatus>(status, out var statusEnum))
                {
                    userPayments = userPayments.Where(p => p.StatusEnum == statusEnum);
                }

                // Get related data
                var users = await _unitOfWork.UserRepository.GetAllAsync();
                var fundAdditions = await _unitOfWork.FundAdditionRepository.GetAllAsync();

                // Map to invoice responses
                var invoices = userPayments.Select(p =>
                {
                    var user = users.FirstOrDefault(u => u.Id == p.UserId);
                    var fundAddition = p.FundAdditionId.HasValue
                        ? fundAdditions.FirstOrDefault(fa => fa.Id == p.FundAdditionId)
                        : null;

                    return new InvoiceResponse
                    {
                        Id = p.Id,
                        InvoiceNumber = $"INV-{p.Id:D6}",
                        UserId = p.UserId ?? 0,
                        UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                        UserEmail = user?.Email ?? "",
                        Amount = p.Amount,
                        TotalAmount = p.Amount,
                        InvoiceType = fundAddition != null ? EInvoiceType.FundContribution : EInvoiceType.Booking,
                        InvoiceTypeName = fundAddition != null ? "Fund Contribution" : "Booking",
                        Status = MapPaymentStatusToInvoiceStatus(p.StatusEnum),
                        StatusName = p.StatusEnum?.ToString() ?? "Unknown",
                        IssueDate = p.CreatedAt ?? DateTime.UtcNow,
                        DueDate = (p.CreatedAt ?? DateTime.UtcNow).AddDays(7),
                        PaidDate = p.PaidAt,
                        Description = fundAddition?.Description ?? "Payment",
                        PaymentId = p.Id,
                        PaymentMethod = p.PaymentGateway,
                        TransactionId = p.TransactionId,
                        FundAdditionId = p.FundAdditionId,
                        CreatedAt = p.CreatedAt ?? DateTime.UtcNow
                    };
                }).OrderByDescending(i => i.CreatedAt);

                // Pagination
                var totalCount = invoices.Count();
                var pagedInvoices = invoices
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "INVOICES_RETRIEVED_SUCCESS",
                    Data = new
                    {
                        Invoices = pagedInvoices,
                        Pagination = new
                        {
                            CurrentPage = page,
                            PageSize = pageSize,
                            TotalCount = totalCount,
                            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                        },
                        Summary = new
                        {
                            TotalInvoices = totalCount,
                            PendingCount = invoices.Count(i => i.Status == EInvoiceStatus.Pending),
                            PaidCount = invoices.Count(i => i.Status == EInvoiceStatus.Paid),
                            OverdueCount = invoices.Count(i => i.Status == EInvoiceStatus.Overdue),
                            TotalAmount = invoices.Sum(i => i.TotalAmount),
                            PendingAmount = invoices.Where(i => i.Status == EInvoiceStatus.Pending).Sum(i => i.TotalAmount)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
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
        /// Gets detailed information about a specific invoice
        /// 
        /// **GET INVOICE DETAILS**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/payment/invoices/123
        /// ```
        /// </remarks>
        /// <response code="200">Invoice details retrieved successfully</response>
        /// <response code="403">Access denied - not your invoice</response>
        /// <response code="404">Invoice not found</response>
        [HttpGet("invoices/{invoiceId}")]
        public async Task<IActionResult> GetInvoiceById(int invoiceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(invoiceId);
                if (payment == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "INVOICE_NOT_FOUND"
                    });
                }

                // Check ownership
                if (payment.UserId != userId)
                {
                    return StatusCode(403, new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_YOUR_INVOICE"
                    });
                }

                var user = await _unitOfWork.UserRepository.GetByIdAsync(payment.UserId ?? 0);
                var fundAddition = payment.FundAdditionId.HasValue
                    ? await _unitOfWork.FundAdditionRepository.GetByIdAsync(payment.FundAdditionId.Value)
                    : null;

                var invoice = new InvoiceResponse
                {
                    Id = payment.Id,
                    InvoiceNumber = $"INV-{payment.Id:D6}",
                    UserId = payment.UserId ?? 0,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    UserEmail = user?.Email ?? "",
                    Amount = payment.Amount,
                    TotalAmount = payment.Amount,
                    InvoiceType = fundAddition != null ? EInvoiceType.FundContribution : EInvoiceType.Booking,
                    InvoiceTypeName = fundAddition != null ? "Fund Contribution" : "Booking",
                    Status = MapPaymentStatusToInvoiceStatus(payment.StatusEnum),
                    StatusName = payment.StatusEnum?.ToString() ?? "Unknown",
                    IssueDate = payment.CreatedAt ?? DateTime.UtcNow,
                    DueDate = (payment.CreatedAt ?? DateTime.UtcNow).AddDays(7),
                    PaidDate = payment.PaidAt,
                    Description = fundAddition?.Description ?? "Payment",
                    PaymentId = payment.Id,
                    PaymentMethod = payment.PaymentGateway,
                    TransactionId = payment.TransactionId,
                    FundAdditionId = payment.FundAdditionId,
                    CreatedAt = payment.CreatedAt ?? DateTime.UtcNow
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "INVOICE_DETAILS_RETRIEVED_SUCCESS",
                    Data = invoice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice details");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Payment Processing

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Process payment for an invoice (one-time payment)
        /// 
        /// **PAY INVOICE**
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "invoiceId": 123,
        ///   "paymentGateway": 0,
        ///   "paymentMethod": 1,
        ///   "bankCode": "NCB",
        ///   "returnUrl": "https://yourapp.com/payment/result"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Payment URL generated successfully</response>
        /// <response code="400">Invoice already paid or invalid</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Invoice not found</response>
        [HttpPost("pay")]
        public async Task<IActionResult> PayInvoice([FromBody] PayInvoiceRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(request.InvoiceId);
                if (payment == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "INVOICE_NOT_FOUND"
                    });
                }

                // Check ownership
                if (payment.UserId != userId)
                {
                    return StatusCode(403, new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_YOUR_INVOICE"
                    });
                }

                // Check if already paid
                if (payment.StatusEnum == EPaymentStatus.Completed)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "INVOICE_ALREADY_PAID"
                    });
                }

                // Create payment request
                var paymentRequest = new CreatePaymentRequest
                {
                    Amount = payment.Amount,
                    PaymentGateway = request.PaymentGateway,
                    PaymentMethod = request.PaymentMethod,
                    PaymentType = payment.FundAdditionId.HasValue ? EPaymentType.FundAddition : EPaymentType.Booking,
                    FundAdditionId = payment.FundAdditionId,
                    BankCode = request.BankCode,
                    ReturnUrl = request.ReturnUrl,
                    Description = $"Payment for Invoice {payment.Id:D6}"
                };

                var response = await _paymentService.CreatePaymentAsync(userId, paymentRequest);

                if (response.StatusCode == 201 && response.Data is PaymentUrlResponse paymentUrl)
                {
                    return Ok(new BaseResponse<object>
                    {
                        StatusCode = 200,
                        Message = "PAYMENT_URL_GENERATED_SUCCESS",
                        Data = new InvoicePaymentResponse
                        {
                            InvoiceId = request.InvoiceId,
                            InvoiceNumber = $"INV-{request.InvoiceId:D6}",
                            PaymentId = paymentUrl.PaymentId,
                            PaymentUrl = paymentUrl.PaymentUrl,
                            Amount = paymentUrl.Amount,
                            ExpiryTime = paymentUrl.ExpiryTime
                        }
                    });
                }

                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invoice payment");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Receipt Generation

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Generate receipt for a paid invoice
        /// 
        /// **GET RECEIPT**
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/payment/receipt/123
        /// ```
        /// </remarks>
        /// <response code="200">Receipt generated successfully</response>
        /// <response code="400">Invoice not paid yet</response>
        /// <response code="403">Access denied</response>
        /// <response code="404">Invoice not found</response>
        [HttpGet("receipt/{invoiceId}")]
        public async Task<IActionResult> GetReceipt(int invoiceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(invoiceId);
                if (payment == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "INVOICE_NOT_FOUND"
                    });
                }

                // Check ownership
                if (payment.UserId != userId)
                {
                    return StatusCode(403, new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_YOUR_INVOICE"
                    });
                }

                // Check if paid
                if (payment.StatusEnum != EPaymentStatus.Completed)
                {
                    return BadRequest(new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "INVOICE_NOT_PAID_YET"
                    });
                }

                var user = await _unitOfWork.UserRepository.GetByIdAsync(payment.UserId ?? 0);
                var fundAddition = payment.FundAdditionId.HasValue
                    ? await _unitOfWork.FundAdditionRepository.GetByIdAsync(payment.FundAdditionId.Value)
                    : null;

                var receipt = new ReceiptResponse
                {
                    InvoiceId = payment.Id,
                    InvoiceNumber = $"INV-{payment.Id:D6}",
                    ReceiptNumber = $"RCP-{payment.Id:D6}",
                    Company = new CompanyInfo(),
                    Customer = new CustomerInfo
                    {
                        Name = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                        Email = user?.Email ?? "",
                        Phone = user?.Phone,
                        Address = user?.Address
                    },
                    LineItems = new List<ReceiptLineItem>
                    {
                        new ReceiptLineItem
                        {
                            Description = fundAddition?.Description ?? "Payment",
                            Quantity = 1,
                            UnitPrice = payment.Amount,
                            Amount = payment.Amount
                        }
                    },
                    SubTotal = payment.Amount,
                    TotalAmount = payment.Amount,
                    IssueDate = payment.CreatedAt ?? DateTime.UtcNow,
                    PaidDate = payment.PaidAt,
                    PaymentMethod = payment.PaymentGateway,
                    TransactionId = payment.TransactionId,
                    Notes = "Thank you for your payment!"
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "RECEIPT_GENERATED_SUCCESS",
                    Data = receipt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Payment Reminders

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Send payment reminders for overdue invoices
        /// 
        /// **SEND PAYMENT REMINDER**
        /// 
        /// **Sample Request:**
        /// ```json
        /// {
        ///   "invoiceId": 123,
        ///   "customMessage": "Please pay your invoice by end of day"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Reminder sent successfully</response>
        /// <response code="404">Invoice not found</response>
        [HttpPost("remind")]
        public async Task<IActionResult> SendPaymentReminder([FromBody] PaymentReminderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                List<Payment> paymentsToRemind = new();

                if (request.InvoiceId.HasValue)
                {
                    var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(request.InvoiceId.Value);
                    if (payment != null && payment.StatusEnum == EPaymentStatus.Pending)
                    {
                        paymentsToRemind.Add(payment);
                    }
                }
                else if (request.InvoiceIds?.Any() == true)
                {
                    var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                    paymentsToRemind = allPayments
                        .Where(p => request.InvoiceIds.Contains(p.Id) && p.StatusEnum == EPaymentStatus.Pending)
                        .ToList();
                }
                else
                {
                    // Get all pending payments for user
                    var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                    var targetUserId = request.UserId ?? userId;
                    paymentsToRemind = allPayments
                        .Where(p => p.UserId == targetUserId &&
                                   p.StatusEnum == EPaymentStatus.Pending &&
                                   p.CreatedAt < DateTime.UtcNow.AddDays(-7))
                        .ToList();
                }

                if (!paymentsToRemind.Any())
                {
                    return Ok(new BaseResponse<object>
                    {
                        StatusCode = 200,
                        Message = "NO_OVERDUE_INVOICES_FOUND",
                        Data = new { RemindersSent = 0 }
                    });
                }

                // TODO: Send actual email/notification reminders
                // For now, just log and return success
                _logger.LogInformation($"Sending payment reminders for {paymentsToRemind.Count} invoices");

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PAYMENT_REMINDERS_SENT_SUCCESS",
                    Data = new
                    {
                        RemindersSent = paymentsToRemind.Count,
                        InvoiceIds = paymentsToRemind.Select(p => p.Id).ToArray(),
                        CustomMessage = request.CustomMessage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment reminders");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Group Finance

        /// <summary>
        /// CoOwner
        /// </summary>
        /// <remarks>
        /// Get group finance summary including maintenance fund and common fund
        /// 
        /// **GROUP FINANCE DASHBOARD**
        /// 
        /// **Returns:**
        /// - Maintenance fund balance and transactions
        /// - Common fund for general operations
        /// - Recent fund activities
        /// - Pending invoices
        /// 
        /// **Sample Request:**
        /// ```
        /// GET /api/payment/group-finance
        /// ```
        /// </remarks>
        /// <response code="200">Finance summary retrieved successfully</response>
        [HttpGet("group-finance")]
        public async Task<IActionResult> GetGroupFinance()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get co-owner
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return NotFound(new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND"
                    });
                }

                // Get user's vehicles
                var allOwnerships = await _unitOfWork.VehicleCoOwnerRepository.GetAllAsync();
                var userOwnerships = allOwnerships.Where(vco => vco.CoOwnerId == coOwner.UserId).ToList();
                var vehicleIds = userOwnerships.Select(vo => vo.VehicleId).ToList();

                var vehicles = await _unitOfWork.VehicleRepository.GetAllAsync();
                var userVehicles = vehicles.Where(v => vehicleIds.Contains(v.Id)).ToList();

                // Get all funds
                var allFunds = await _unitOfWork.FundRepository.GetAllAsync();
                var vehicleFunds = userVehicles
                    .Where(v => v.FundId.HasValue)
                    .Select(v => allFunds.FirstOrDefault(f => f.Id == v.FundId))
                    .Where(f => f != null)
                    .ToList();

                // Get fund transactions
                var fundAdditions = await _unitOfWork.FundAdditionRepository.GetAllAsync();
                var fundUsages = await _unitOfWork.FundUsageRepository.GetAllAsync();

                // Calculate maintenance fund
                var maintenanceFund = new FundSummary
                {
                    FundName = "Maintenance Fund",
                    CurrentBalance = vehicleFunds.Sum(f => f!.CurrentBalance ?? 0),
                    VehicleCount = userVehicles.Count,
                    VehicleBreakdown = userVehicles.Select(v =>
                    {
                        var fund = v.FundId.HasValue ? allFunds.FirstOrDefault(f => f.Id == v.FundId) : null;
                        return new VehicleFundBreakdown
                        {
                            VehicleId = v.Id,
                            VehicleName = $"{v.Brand} {v.Model}",
                            Balance = fund?.CurrentBalance ?? 0,
                            CoOwnerCount = allOwnerships.Count(vco => vco.VehicleId == v.Id)
                        };
                    }).ToList()
                };

                // Get recent transactions
                var fundIds = vehicleFunds.Select(f => f!.Id).ToList();
                var recentAdditions = fundAdditions.Where(fa => fundIds.Contains(fa.FundId ?? 0))
                    .OrderByDescending(fa => fa.CreatedAt)
                    .Take(10);
                var recentUsages = fundUsages.Where(fu => fundIds.Contains(fu.FundId ?? 0))
                    .OrderByDescending(fu => fu.CreatedAt)
                    .Take(10);

                var recentTransactions = recentAdditions.Select(fa => new FundTransaction
                {
                    Id = fa.Id,
                    TransactionDate = fa.CreatedAt ?? DateTime.UtcNow,
                    Type = "Income",
                    Category = "Fund Addition",
                    Amount = fa.Amount,
                    Description = fa.Description ?? "Fund contribution"
                }).Concat(recentUsages.Select(fu => new FundTransaction
                {
                    Id = fu.Id,
                    TransactionDate = fu.CreatedAt ?? DateTime.UtcNow,
                    Type = "Expense",
                    Category = fu.UsageTypeEnum?.ToString() ?? "Other",
                    Amount = fu.Amount,
                    Description = fu.Description ?? "Fund usage"
                })).OrderByDescending(t => t.TransactionDate)
                .Take(20)
                .ToList();

                // Get pending invoices
                var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();
                var userPendingPayments = allPayments
                    .Where(p => p.UserId == userId && p.StatusEnum == EPaymentStatus.Pending)
                    .ToList();

                var users = await _unitOfWork.UserRepository.GetAllAsync();
                var pendingInvoices = userPendingPayments.Select(p =>
                {
                    var user = users.FirstOrDefault(u => u.Id == p.UserId);
                    return new InvoiceResponse
                    {
                        Id = p.Id,
                        InvoiceNumber = $"INV-{p.Id:D6}",
                        UserId = p.UserId ?? 0,
                        UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                        Amount = p.Amount,
                        TotalAmount = p.Amount,
                        Status = EInvoiceStatus.Pending,
                        StatusName = "Pending",
                        IssueDate = p.CreatedAt ?? DateTime.UtcNow,
                        DueDate = (p.CreatedAt ?? DateTime.UtcNow).AddDays(7)
                    };
                }).ToList();

                var summary = new GroupFinanceSummary
                {
                    MaintenanceFund = maintenanceFund,
                    CommonFund = new FundSummary
                    {
                        FundName = "Common Fund",
                        CurrentBalance = 0, // To be implemented
                        VehicleCount = userVehicles.Count
                    },
                    TotalBalance = maintenanceFund.CurrentBalance,
                    TotalIncome = fundAdditions.Where(fa => fundIds.Contains(fa.FundId ?? 0)).Sum(fa => fa.Amount),
                    TotalExpenses = fundUsages.Where(fu => fundIds.Contains(fu.FundId ?? 0)).Sum(fu => fu.Amount),
                    RecentTransactions = recentTransactions,
                    PendingInvoices = pendingInvoices,
                    TotalPendingAmount = pendingInvoices.Sum(i => i.TotalAmount)
                };

                return Ok(new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "GROUP_FINANCE_RETRIEVED_SUCCESS",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group finance");
                return StatusCode(500, new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Helper Methods

        private EInvoiceStatus MapPaymentStatusToInvoiceStatus(EPaymentStatus? paymentStatus)
        {
            return paymentStatus switch
            {
                EPaymentStatus.Pending => EInvoiceStatus.Pending,
                EPaymentStatus.Completed => EInvoiceStatus.Paid,
                EPaymentStatus.Failed => EInvoiceStatus.Cancelled,
                EPaymentStatus.Refunded => EInvoiceStatus.Cancelled,
                _ => EInvoiceStatus.Pending
            };
        }

        #endregion
    }
}
