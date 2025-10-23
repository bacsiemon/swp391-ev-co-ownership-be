using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.DepositDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for managing deposit transactions with multiple payment methods
    /// </summary>
    public class DepositService : IDepositService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnPayService _vnPayService;
        private static int _nextDepositId = 1;

        // In-memory storage for deposits (production: use database)
        private static readonly List<DepositData> _deposits = new List<DepositData>();

        public DepositService(IUnitOfWork unitOfWork, IVnPayService vnPayService)
        {
            _unitOfWork = unitOfWork;
            _vnPayService = vnPayService;
        }

        #region Public Methods

        public async Task<BaseResponse<DepositPaymentUrlResponse>> CreateDepositAsync(int userId, CreateDepositRequest request)
        {
            try
            {
                // Verify user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<DepositPaymentUrlResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Create deposit record
                var depositId = _nextDepositId++;
                var transactionRef = GenerateTransactionRef(depositId, request.DepositMethod);
                var expiryTime = DateTime.UtcNow.AddMinutes(15); // 15 minutes to complete payment

                var deposit = new DepositData
                {
                    DepositId = depositId,
                    UserId = userId,
                    Amount = request.Amount,
                    DepositMethod = request.DepositMethod,
                    Status = EDepositStatus.Pending,
                    TransactionRef = transactionRef,
                    BankCode = request.BankCode,
                    EWalletProvider = request.EWalletProvider?.ToUpper(),
                    Description = request.Description ?? GetDefaultDescription(request.DepositMethod),
                    CreatedAt = DateTime.UtcNow,
                    ExpiryTime = expiryTime,
                    IpAddress = "127.0.0.1" // TODO: Get from HttpContext
                };

                _deposits.Add(deposit);

                // Generate payment URL based on deposit method
                var paymentUrl = GeneratePaymentUrl(deposit, request.ReturnUrl);

                var response = new DepositPaymentUrlResponse
                {
                    PaymentUrl = paymentUrl,
                    DepositId = depositId,
                    TransactionRef = transactionRef,
                    DepositMethod = request.DepositMethod,
                    Amount = request.Amount,
                    ExpiryTime = expiryTime
                };

                return new BaseResponse<DepositPaymentUrlResponse>
                {
                    StatusCode = 201,
                    Message = "DEPOSIT_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositPaymentUrlResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<DepositResponse>> GetDepositByIdAsync(int depositId, int userId)
        {
            try
            {
                var deposit = _deposits.FirstOrDefault(d => d.DepositId == depositId);
                if (deposit == null)
                {
                    return new BaseResponse<DepositResponse>
                    {
                        StatusCode = 404,
                        Message = "DEPOSIT_NOT_FOUND",
                        Data = null
                    };
                }

                // Check authorization (user can only view their own deposits)
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                var isAdmin = user?.RoleEnum == EUserRole.Admin || user?.RoleEnum == EUserRole.Staff;

                if (deposit.UserId != userId && !isAdmin)
                {
                    return new BaseResponse<DepositResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED",
                        Data = null
                    };
                }

                var response = await MapToDepositResponse(deposit);

                return new BaseResponse<DepositResponse>
                {
                    StatusCode = 200,
                    Message = "DEPOSIT_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<DepositListResponse>> GetUserDepositsAsync(int userId, GetDepositsRequest request)
        {
            try
            {
                // Verify user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<DepositListResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Filter user's deposits
                var query = _deposits.Where(d => d.UserId == userId).AsQueryable();

                var result = await ApplyFiltersAndPagination(query, request);

                return new BaseResponse<DepositListResponse>
                {
                    StatusCode = 200,
                    Message = "DEPOSITS_RETRIEVED_SUCCESSFULLY",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositListResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<DepositListResponse>> GetAllDepositsAsync(GetDepositsRequest request)
        {
            try
            {
                // Get all deposits
                var query = _deposits.AsQueryable();

                var result = await ApplyFiltersAndPagination(query, request);

                return new BaseResponse<DepositListResponse>
                {
                    StatusCode = 200,
                    Message = "DEPOSITS_RETRIEVED_SUCCESSFULLY",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositListResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<DepositResponse>> VerifyDepositCallbackAsync(VerifyDepositCallbackRequest request)
        {
            try
            {
                var deposit = _deposits.FirstOrDefault(d => d.DepositId == request.DepositId);
                if (deposit == null)
                {
                    return new BaseResponse<DepositResponse>
                    {
                        StatusCode = 404,
                        Message = "DEPOSIT_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if deposit is already processed
                if (deposit.Status != EDepositStatus.Pending && deposit.Status != EDepositStatus.Processing)
                {
                    return new BaseResponse<DepositResponse>
                    {
                        StatusCode = 400,
                        Message = "DEPOSIT_ALREADY_PROCESSED",
                        Data = await MapToDepositResponse(deposit)
                    };
                }

                // Update deposit status
                deposit.GatewayTransactionId = request.GatewayTransactionId;

                if (request.IsSuccess)
                {
                    deposit.Status = EDepositStatus.Completed;
                    deposit.CompletedAt = DateTime.UtcNow;

                    // TODO: Add funds to user wallet/balance
                    // This would typically create a FundAddition or update user balance
                    await CreatePaymentRecordForDeposit(deposit);
                }
                else
                {
                    deposit.Status = EDepositStatus.Failed;
                    deposit.FailedAt = DateTime.UtcNow;
                    deposit.FailureReason = $"Payment gateway error: {request.ResponseCode}";
                }

                var response = await MapToDepositResponse(deposit);

                return new BaseResponse<DepositResponse>
                {
                    StatusCode = 200,
                    Message = request.IsSuccess ? "DEPOSIT_COMPLETED_SUCCESSFULLY" : "DEPOSIT_FAILED",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<string>> CancelDepositAsync(int depositId, int userId)
        {
            try
            {
                var deposit = _deposits.FirstOrDefault(d => d.DepositId == depositId);
                if (deposit == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "DEPOSIT_NOT_FOUND",
                        Data = null
                    };
                }

                // Check authorization
                if (deposit.UserId != userId)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED",
                        Data = null
                    };
                }

                // Can only cancel pending deposits
                if (deposit.Status != EDepositStatus.Pending)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_CANCEL_NON_PENDING_DEPOSIT",
                        Data = null
                    };
                }

                deposit.Status = EDepositStatus.Cancelled;
                deposit.CancelledAt = DateTime.UtcNow;

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "DEPOSIT_CANCELLED_SUCCESSFULLY",
                    Data = "Deposit has been cancelled"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<DepositStatistics>> GetUserDepositStatisticsAsync(int userId)
        {
            try
            {
                var userDeposits = _deposits.Where(d => d.UserId == userId).ToList();

                var statistics = CalculateStatistics(userDeposits);

                return new BaseResponse<DepositStatistics>
                {
                    StatusCode = 200,
                    Message = "STATISTICS_RETRIEVED_SUCCESSFULLY",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<DepositStatistics>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        public async Task<BaseResponse<PaymentMethodsResponse>> GetAvailablePaymentMethodsAsync()
        {
            try
            {
                var methods = new List<DepositMethodInfo>
                {
                    new DepositMethodInfo
                    {
                        Method = EDepositMethod.CreditCard,
                        Name = "Credit Card",
                        Description = "Pay with Visa, Mastercard, JCB via VNPay gateway",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 500000000,
                        Icon = "credit-card",
                        SupportedBanks = new List<string> { "VISA", "MASTERCARD", "JCB", "AMEX" }
                    },
                    new DepositMethodInfo
                    {
                        Method = EDepositMethod.EWallet,
                        Name = "E-Wallet",
                        Description = "Pay with Momo, ZaloPay e-wallets",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 50000000,
                        Icon = "wallet",
                        SupportedEWallets = new List<string> { "MOMO", "ZALOPAY" }
                    },
                    new DepositMethodInfo
                    {
                        Method = EDepositMethod.OnlineBanking,
                        Name = "Online Banking",
                        Description = "Pay via internet banking from major Vietnamese banks",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 1000000000,
                        Icon = "bank",
                        SupportedBanks = new List<string>
                        {
                            "VIETCOMBANK", "VIETINBANK", "BIDV", "AGRIBANK", "TECHCOMBANK",
                            "MBBANK", "TPBANK", "ACB", "VPBank", "SHB", "SACOMBANK"
                        }
                    },
                    new DepositMethodInfo
                    {
                        Method = EDepositMethod.QRCode,
                        Name = "QR Code Payment",
                        Description = "Scan QR code to pay via VNPay",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 500000000,
                        Icon = "qrcode"
                    }
                };

                var response = new PaymentMethodsResponse
                {
                    Methods = methods
                };

                return new BaseResponse<PaymentMethodsResponse>
                {
                    StatusCode = 200,
                    Message = "PAYMENT_METHODS_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<PaymentMethodsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = new { Details = ex.Message }
                };
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateTransactionRef(int depositId, EDepositMethod method)
        {
            var prefix = method switch
            {
                EDepositMethod.CreditCard => "CC",
                EDepositMethod.EWallet => "EW",
                EDepositMethod.OnlineBanking => "OB",
                EDepositMethod.QRCode => "QR",
                _ => "DP"
            };

            var timestamp = DateTime.UtcNow.Ticks;
            return $"{prefix}_{depositId}_{timestamp}";
        }

        private string GetDefaultDescription(EDepositMethod method)
        {
            return method switch
            {
                EDepositMethod.CreditCard => "Deposit via Credit Card",
                EDepositMethod.EWallet => "Deposit via E-Wallet",
                EDepositMethod.OnlineBanking => "Deposit via Online Banking",
                EDepositMethod.QRCode => "Deposit via QR Code",
                _ => "Deposit to Account"
            };
        }

        private string GeneratePaymentUrl(DepositData deposit, string? returnUrl)
        {
            // Use VNPay service to generate payment URL
            var orderInfo = deposit.Description ?? $"Deposit {deposit.TransactionRef}";
            var ipAddress = deposit.IpAddress ?? "127.0.0.1";

            // Create base payment URL with VNPay
            var baseUrl = _vnPayService.CreatePaymentUrl(
                deposit.DepositId,
                deposit.Amount,
                orderInfo,
                ipAddress
            );

            // Customize URL based on deposit method
            var urlWithMethod = baseUrl;

            switch (deposit.DepositMethod)
            {
                case EDepositMethod.CreditCard:
                    // VNPay credit card payment
                    urlWithMethod += "&vnp_CardType=CREDIT";
                    break;

                case EDepositMethod.OnlineBanking:
                    // VNPay online banking
                    if (!string.IsNullOrEmpty(deposit.BankCode))
                    {
                        urlWithMethod += $"&vnp_BankCode={deposit.BankCode}";
                    }
                    break;

                case EDepositMethod.EWallet:
                    // For e-wallets, redirect to specific provider
                    if (deposit.EWalletProvider == "MOMO")
                    {
                        // In production, integrate with Momo API
                        urlWithMethod = $"https://test-payment.momo.vn/gw_payment/checkout?amount={deposit.Amount}&orderId={deposit.TransactionRef}";
                    }
                    else if (deposit.EWalletProvider == "ZALOPAY")
                    {
                        // In production, integrate with ZaloPay API
                        urlWithMethod = $"https://sb-openapi.zalopay.vn/v2/create?amount={deposit.Amount}&appTransId={deposit.TransactionRef}";
                    }
                    else
                    {
                        urlWithMethod += "&vnp_CardType=EWALLET";
                    }
                    break;

                case EDepositMethod.QRCode:
                    // VNPay QR code payment
                    urlWithMethod += "&vnp_CardType=QRCODE";
                    break;
            }

            // Add return URL if provided
            if (!string.IsNullOrEmpty(returnUrl))
            {
                urlWithMethod += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            return urlWithMethod;
        }

        private async Task CreatePaymentRecordForDeposit(DepositData deposit)
        {
            // Create a Payment record to track the deposit in the payment system
            var payment = new Payment
            {
                UserId = deposit.UserId,
                Amount = deposit.Amount,
                TransactionId = deposit.GatewayTransactionId ?? deposit.TransactionRef,
                PaymentGateway = GetPaymentGatewayName(deposit.DepositMethod),
                StatusEnum = EPaymentStatus.Completed,
                PaidAt = deposit.CompletedAt,
                CreatedAt = deposit.CreatedAt
            };

            await _unitOfWork.PaymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            deposit.PaymentId = payment.Id;
        }

        private string GetPaymentGatewayName(EDepositMethod method)
        {
            return method switch
            {
                EDepositMethod.CreditCard => "VNPay-CreditCard",
                EDepositMethod.OnlineBanking => "VNPay-Banking",
                EDepositMethod.EWallet => "E-Wallet",
                EDepositMethod.QRCode => "VNPay-QRCode",
                _ => "VNPay"
            };
        }

        private async Task<DepositResponse> MapToDepositResponse(DepositData deposit)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(deposit.UserId);

            return new DepositResponse
            {
                DepositId = deposit.DepositId,
                UserId = deposit.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                UserEmail = user?.Email ?? string.Empty,
                Amount = deposit.Amount,
                DepositMethod = deposit.DepositMethod,
                DepositMethodName = GetDepositMethodName(deposit.DepositMethod),
                Status = deposit.Status,
                StatusName = GetStatusName(deposit.Status),
                PaymentGateway = GetPaymentGatewayName(deposit.DepositMethod),
                GatewayTransactionId = deposit.GatewayTransactionId,
                BankCode = deposit.BankCode,
                EWalletProvider = deposit.EWalletProvider,
                TransactionRef = deposit.TransactionRef,
                Description = deposit.Description,
                CreatedAt = deposit.CreatedAt,
                CompletedAt = deposit.CompletedAt,
                FailedAt = deposit.FailedAt,
                CancelledAt = deposit.CancelledAt,
                ExpiryTime = deposit.ExpiryTime,
                FailureReason = deposit.FailureReason,
                IpAddress = deposit.IpAddress,
                PaymentId = deposit.PaymentId
            };
        }

        private DepositSummary MapToDepositSummary(DepositData deposit)
        {
            return new DepositSummary
            {
                DepositId = deposit.DepositId,
                Amount = deposit.Amount,
                DepositMethod = deposit.DepositMethod,
                DepositMethodName = GetDepositMethodName(deposit.DepositMethod),
                Status = deposit.Status,
                StatusName = GetStatusName(deposit.Status),
                CreatedAt = deposit.CreatedAt,
                CompletedAt = deposit.CompletedAt,
                TransactionRef = deposit.TransactionRef
            };
        }

        private string GetDepositMethodName(EDepositMethod method)
        {
            return method switch
            {
                EDepositMethod.CreditCard => "Credit Card",
                EDepositMethod.EWallet => "E-Wallet",
                EDepositMethod.OnlineBanking => "Online Banking",
                EDepositMethod.QRCode => "QR Code",
                _ => "Unknown"
            };
        }

        private string GetStatusName(EDepositStatus status)
        {
            return status switch
            {
                EDepositStatus.Pending => "Pending",
                EDepositStatus.Processing => "Processing",
                EDepositStatus.Completed => "Completed",
                EDepositStatus.Failed => "Failed",
                EDepositStatus.Cancelled => "Cancelled",
                EDepositStatus.Expired => "Expired",
                EDepositStatus.Refunded => "Refunded",
                _ => "Unknown"
            };
        }

        private async Task<DepositListResponse> ApplyFiltersAndPagination(IQueryable<DepositData> query, GetDepositsRequest request)
        {
            // Apply filters
            if (request.DepositMethod.HasValue)
            {
                query = query.Where(d => d.DepositMethod == request.DepositMethod.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(d => d.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= request.ToDate.Value);
            }

            if (request.MinAmount.HasValue)
            {
                query = query.Where(d => d.Amount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                query = query.Where(d => d.Amount <= request.MaxAmount.Value);
            }

            // Get total count before pagination
            var totalItems = query.Count();

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "amount" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(d => d.Amount)
                    : query.OrderByDescending(d => d.Amount),
                "status" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(d => d.Status)
                    : query.OrderByDescending(d => d.Status),
                "completedat" => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(d => d.CompletedAt ?? DateTime.MaxValue)
                    : query.OrderByDescending(d => d.CompletedAt ?? DateTime.MinValue),
                _ => request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(d => d.CreatedAt)
                    : query.OrderByDescending(d => d.CreatedAt)
            };

            // Apply pagination
            var deposits = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Calculate statistics
            var allDepositsForStats = query.ToList();
            var statistics = CalculateStatistics(allDepositsForStats);

            // Map to summaries
            var summaries = deposits.Select(MapToDepositSummary).ToList();

            // Pagination info
            var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);
            var pagination = new DepositPaginationInfo
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < totalPages
            };

            return new DepositListResponse
            {
                Deposits = summaries,
                Statistics = statistics,
                Pagination = pagination
            };
        }

        private DepositStatistics CalculateStatistics(List<DepositData> deposits)
        {
            var statistics = new DepositStatistics
            {
                TotalDeposits = deposits.Count,
                PendingDeposits = deposits.Count(d => d.Status == EDepositStatus.Pending),
                CompletedDeposits = deposits.Count(d => d.Status == EDepositStatus.Completed),
                FailedDeposits = deposits.Count(d => d.Status == EDepositStatus.Failed),
                CancelledDeposits = deposits.Count(d => d.Status == EDepositStatus.Cancelled),
                TotalAmount = deposits.Sum(d => d.Amount),
                CompletedAmount = deposits.Where(d => d.Status == EDepositStatus.Completed).Sum(d => d.Amount),
                PendingAmount = deposits.Where(d => d.Status == EDepositStatus.Pending).Sum(d => d.Amount),
                ByMethod = new Dictionary<string, decimal>()
            };

            // Group by deposit method
            var methodGroups = deposits.GroupBy(d => d.DepositMethod);
            foreach (var group in methodGroups)
            {
                var methodName = GetDepositMethodName(group.Key);
                statistics.ByMethod[methodName] = group.Where(d => d.Status == EDepositStatus.Completed).Sum(d => d.Amount);
            }

            return statistics;
        }

        #endregion

        #region Internal Data Model

        private class DepositData
        {
            public int DepositId { get; set; }
            public int UserId { get; set; }
            public decimal Amount { get; set; }
            public EDepositMethod DepositMethod { get; set; }
            public EDepositStatus Status { get; set; }
            public string TransactionRef { get; set; } = string.Empty;
            public string? GatewayTransactionId { get; set; }
            public string? BankCode { get; set; }
            public string? EWalletProvider { get; set; }
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public DateTime? FailedAt { get; set; }
            public DateTime? CancelledAt { get; set; }
            public DateTime? ExpiryTime { get; set; }
            public string? FailureReason { get; set; }
            public string? IpAddress { get; set; }
            public int? PaymentId { get; set; }
        }

        #endregion
    }
}
