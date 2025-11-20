using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Services.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnPayService _vnPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IVnPayService vnPayService,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _vnPayService = vnPayService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BaseResponse<PaymentUrlResponse>> CreatePaymentAsync(int userId, CreatePaymentRequest request)
        {
            try
            {
                // Verify user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<PaymentUrlResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                // Create payment record with extended information
                var payment = new Payment
                {
                    UserId = userId,
                    Amount = request.Amount,
                    PaymentGateway = GetGatewayString(request.PaymentGateway),
                    StatusEnum = EPaymentStatus.Pending,
                    FundAdditionId = request.FundAdditionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // Generate transaction reference
                var transactionRef = GenerateTransactionRef(payment.Id, request.PaymentGateway, request.PaymentType);

                // Generate payment URL based on gateway and method
                var paymentUrl = GenerateAdvancedPaymentUrl(
                    payment,
                    request.PaymentGateway,
                    request.PaymentMethod,
                    request.BankCode,
                    request.EWalletProvider,
                    request.Description ?? GetDefaultDescription(request.PaymentType),
                    request.ReturnUrl,
                    request.CancelUrl
                );

                var expiryTime = DateTime.UtcNow.AddMinutes(15); // 15 minutes expiry

                var response = new PaymentUrlResponse
                {
                    PaymentUrl = paymentUrl,
                    PaymentId = payment.Id,
                    TransactionRef = transactionRef,
                    Gateway = request.PaymentGateway,
                    GatewayName = GetGatewayName(request.PaymentGateway),
                    Amount = request.Amount,
                    ExpiryTime = expiryTime
                };

                return new BaseResponse<PaymentUrlResponse>
                {
                    StatusCode = 201,
                    Message = "PAYMENT_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PaymentUrlResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PaymentResponse>> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(request.PaymentId);
                if (payment == null)
                {
                    return new BaseResponse<PaymentResponse>
                    {
                        StatusCode = 404,
                        Message = "PAYMENT_NOT_FOUND"
                    };
                }

                if (payment.StatusEnum != EPaymentStatus.Pending)
                {
                    return new BaseResponse<PaymentResponse>
                    {
                        StatusCode = 400,
                        Message = "PAYMENT_ALREADY_PROCESSED"
                    };
                }

                payment.TransactionId = request.TransactionId;
                payment.StatusEnum = request.IsSuccess ? EPaymentStatus.Completed : EPaymentStatus.Failed;
                payment.PaidAt = request.IsSuccess ? DateTime.UtcNow : null;

                // Update FundAddition status if payment is successful
                if (request.IsSuccess && payment.FundAdditionId.HasValue)
                {
                    var fundAddition = await _unitOfWork.FundAdditionRepository.GetByIdAsync(payment.FundAdditionId.Value);
                    if (fundAddition != null)
                    {
                        fundAddition.StatusEnum = EFundAdditionStatus.Completed;
                        await _unitOfWork.FundAdditionRepository.UpdateAsync(fundAddition);
                    }
                }

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                var paymentResponse = await GetPaymentResponseAsync(payment.Id);

                return new BaseResponse<PaymentResponse>
                {
                    StatusCode = 200,
                    Message = request.IsSuccess ? "PAYMENT_COMPLETED_SUCCESSFULLY" : "PAYMENT_FAILED",
                    Data = paymentResponse
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PaymentResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PaymentResponse>> GetPaymentByIdAsync(int paymentId)
        {
            try
            {
                var paymentResponse = await GetPaymentResponseAsync(paymentId);
                if (paymentResponse == null)
                {
                    return new BaseResponse<PaymentResponse>
                    {
                        StatusCode = 404,
                        Message = "PAYMENT_NOT_FOUND"
                    };
                }

                return new BaseResponse<PaymentResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = paymentResponse
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PaymentResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<PaymentResponse>>> GetUserPaymentsAsync(int userId, int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.PaymentRepository.GetQueryable()
                    .Include(p => p.User)
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentResponses = payments.Select(MapToPaymentResponse).ToList();
                var pagedResult = new PagedResult<PaymentResponse>(paymentResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<PaymentResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PagedResult<PaymentResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<PaymentResponse>>> GetAllPaymentsAsync(int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.PaymentRepository.GetQueryable()
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt);

                var totalCount = await query.CountAsync();
                var payments = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var paymentResponses = payments.Select(MapToPaymentResponse).ToList();
                var pagedResult = new PagedResult<PaymentResponse>(paymentResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<PaymentResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PagedResult<PaymentResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<string>> CancelPaymentAsync(int paymentId, int userId)
        {
            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "PAYMENT_NOT_FOUND"
                    };
                }

                if (payment.UserId != userId)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                if (payment.StatusEnum != EPaymentStatus.Pending)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 400,
                        Message = "CANNOT_CANCEL_PROCESSED_PAYMENT"
                    };
                }

                payment.StatusEnum = EPaymentStatus.Failed;
                await _unitOfWork.PaymentRepository.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "PAYMENT_CANCELLED_SUCCESSFULLY"
                };
            }
            catch (Exception)
            {
                return new BaseResponse<string>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PaymentStatisticsResponse>> GetPaymentStatisticsAsync()
        {
            try
            {
                var allPayments = await _unitOfWork.PaymentRepository.GetAllAsync();

                var statistics = new PaymentStatisticsResponse
                {
                    TotalPayments = allPayments.Count(),
                    CompletedPayments = allPayments.Count(p => p.StatusEnum == EPaymentStatus.Completed),
                    PendingPayments = allPayments.Count(p => p.StatusEnum == EPaymentStatus.Pending),
                    FailedPayments = allPayments.Count(p => p.StatusEnum == EPaymentStatus.Failed),
                    TotalAmount = allPayments.Sum(p => p.Amount),
                    CompletedAmount = allPayments.Where(p => p.StatusEnum == EPaymentStatus.Completed).Sum(p => p.Amount),
                    PendingAmount = allPayments.Where(p => p.StatusEnum == EPaymentStatus.Pending).Sum(p => p.Amount)
                };

                return new BaseResponse<PaymentStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = statistics
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PaymentStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PaymentGatewaysResponse>> GetAvailableGatewaysAsync()
        {
            try
            {
                var gateways = new List<PaymentGatewayInfo>
                {
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.VNPay,
                        Name = "VNPay",
                        Description = "Vietnam's leading payment gateway - supports cards, banking, e-wallets",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 1000000000,
                        Logo = "vnpay-logo.png",
                        SupportedMethods = new List<string> { "Credit Card", "Debit Card", "Online Banking", "QR Code", "E-Wallet" },
                        SupportedBanks = new List<string>
                        {
                            "VIETCOMBANK", "VIETINBANK", "BIDV", "AGRIBANK", "TECHCOMBANK",
                            "MBBANK", "TPBANK", "ACB", "VPBank", "SHB", "SACOMBANK", "MSB"
                        }
                    },
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.Momo,
                        Name = "Momo",
                        Description = "Leading e-wallet in Vietnam - fast and secure mobile payment",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 50000000,
                        Logo = "momo-logo.png",
                        SupportedMethods = new List<string> { "Momo Wallet", "Linked Bank Card" }
                    },
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.ZaloPay,
                        Name = "ZaloPay",
                        Description = "Popular e-wallet by Zalo - convenient payment solution",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 50000000,
                        Logo = "zalopay-logo.png",
                        SupportedMethods = new List<string> { "ZaloPay Wallet", "Linked Bank Card" }
                    },
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.ShopeePay,
                        Name = "ShopeePay",
                        Description = "E-wallet from Shopee platform",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 20000000,
                        Logo = "shopeepay-logo.png",
                        SupportedMethods = new List<string> { "ShopeePay Wallet" }
                    },
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.ViettelPay,
                        Name = "ViettelPay",
                        Description = "E-wallet by Viettel Group",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 30000000,
                        Logo = "viettelpay-logo.png",
                        SupportedMethods = new List<string> { "ViettelPay Wallet", "Linked Bank Card" }
                    },
                    new PaymentGatewayInfo
                    {
                        Gateway = EPaymentGateway.BankTransfer,
                        Name = "Direct Bank Transfer",
                        Description = "Transfer directly via banking app",
                        IsAvailable = true,
                        MinAmount = 10000,
                        MaxAmount = 2000000000,
                        Logo = "bank-transfer-logo.png",
                        SupportedMethods = new List<string> { "QR Code", "Account Transfer" }
                    }
                };

                var response = new PaymentGatewaysResponse
                {
                    Gateways = gateways
                };

                return new BaseResponse<PaymentGatewaysResponse>
                {
                    StatusCode = 200,
                    Message = "GATEWAYS_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception)
            {
                return new BaseResponse<PaymentGatewaysResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        private async Task<PaymentResponse?> GetPaymentResponseAsync(int paymentId)
        {
            var payment = await _unitOfWork.PaymentRepository.GetQueryable()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            return payment == null ? null : MapToPaymentResponse(payment);
        }

        private PaymentResponse MapToPaymentResponse(Payment payment)
        {
            var gateway = ParseGateway(payment.PaymentGateway);

            return new PaymentResponse
            {
                Id = payment.Id,
                UserId = payment.UserId ?? 0,
                UserName = payment.User != null
                    ? $"{payment.User.FirstName} {payment.User.LastName}".Trim()
                    : "",
                UserEmail = payment.User?.Email ?? "",
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
                PaymentGateway = gateway,
                PaymentGatewayName = GetGatewayName(gateway),
                PaymentMethod = null, // TODO: Store in database
                PaymentMethodName = null,
                PaymentType = EPaymentType.Other, // TODO: Store in database
                PaymentTypeName = "Other",
                Status = payment.StatusEnum ?? EPaymentStatus.Pending,
                StatusName = GetStatusName(payment.StatusEnum ?? EPaymentStatus.Pending),
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                FundAdditionId = payment.FundAdditionId,
                BankCode = null,
                EWalletProvider = null,
                Description = null,
                FailureReason = null
            };
        }

        private string GenerateTransactionRef(int paymentId, EPaymentGateway gateway, EPaymentType type)
        {
            var gatewayPrefix = gateway switch
            {
                EPaymentGateway.VNPay => "VNP",
                EPaymentGateway.Momo => "MOMO",
                EPaymentGateway.ZaloPay => "ZALO",
                EPaymentGateway.ShopeePay => "SHOP",
                EPaymentGateway.ViettelPay => "VTEL",
                EPaymentGateway.BankTransfer => "BANK",
                _ => "PAY"
            };

            var typePrefix = type switch
            {
                EPaymentType.Booking => "BK",
                EPaymentType.Maintenance => "MT",
                EPaymentType.FundAddition => "FD",
                EPaymentType.Fuel => "FL",
                EPaymentType.Insurance => "IN",
                EPaymentType.Parking => "PK",
                EPaymentType.Toll => "TL",
                EPaymentType.Upgrade => "UP",
                EPaymentType.Dispute => "DP",
                EPaymentType.Contract => "CT",
                _ => "OT"
            };

            var timestamp = DateTime.UtcNow.Ticks;
            return $"{gatewayPrefix}_{typePrefix}_{paymentId}_{timestamp}";
        }

        private string GetDefaultDescription(EPaymentType type)
        {
            return type switch
            {
                EPaymentType.Booking => "Payment for vehicle booking",
                EPaymentType.Maintenance => "Payment for vehicle maintenance",
                EPaymentType.FundAddition => "Payment for fund addition",
                EPaymentType.Fuel => "Payment for fuel costs",
                EPaymentType.Insurance => "Payment for insurance",
                EPaymentType.Parking => "Payment for parking fees",
                EPaymentType.Toll => "Payment for toll fees",
                EPaymentType.Upgrade => "Payment for vehicle upgrade",
                EPaymentType.Dispute => "Payment for dispute resolution",
                EPaymentType.Contract => "Payment for contract fees",
                _ => "Payment for EV co-ownership service"
            };
        }

        private string GetGatewayString(EPaymentGateway gateway)
        {
            return gateway switch
            {
                EPaymentGateway.VNPay => "VNPay",
                EPaymentGateway.Momo => "Momo",
                EPaymentGateway.ZaloPay => "ZaloPay",
                EPaymentGateway.ShopeePay => "ShopeePay",
                EPaymentGateway.ViettelPay => "ViettelPay",
                EPaymentGateway.BankTransfer => "BankTransfer",
                _ => "VNPay"
            };
        }

        private EPaymentGateway ParseGateway(string? gatewayString)
        {
            if (string.IsNullOrEmpty(gatewayString))
                return EPaymentGateway.VNPay;

            return gatewayString.ToLower() switch
            {
                "vnpay" => EPaymentGateway.VNPay,
                "momo" => EPaymentGateway.Momo,
                "zalopay" => EPaymentGateway.ZaloPay,
                "shopeepay" => EPaymentGateway.ShopeePay,
                "viettelpay" => EPaymentGateway.ViettelPay,
                "banktransfer" => EPaymentGateway.BankTransfer,
                _ => EPaymentGateway.VNPay
            };
        }

        private string GetGatewayName(EPaymentGateway gateway)
        {
            return gateway switch
            {
                EPaymentGateway.VNPay => "VNPay",
                EPaymentGateway.Momo => "Momo",
                EPaymentGateway.ZaloPay => "ZaloPay",
                EPaymentGateway.ShopeePay => "ShopeePay",
                EPaymentGateway.ViettelPay => "ViettelPay",
                EPaymentGateway.BankTransfer => "Direct Bank Transfer",
                _ => "Unknown"
            };
        }

        private string GetStatusName(EPaymentStatus status)
        {
            return status switch
            {
                EPaymentStatus.Pending => "Pending",
                EPaymentStatus.Completed => "Completed",
                EPaymentStatus.Failed => "Failed",
                EPaymentStatus.Refunded => "Refunded",
                _ => "Unknown"
            };
        }

        private string GenerateAdvancedPaymentUrl(
            Payment payment,
            EPaymentGateway gateway,
            EPaymentMethod? method,
            string? bankCode,
            string? eWalletProvider,
            string orderInfo,
            string? returnUrl,
            string? cancelUrl)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            switch (gateway)
            {
                case EPaymentGateway.VNPay:
                    return GenerateVNPayUrl(payment, method, bankCode, orderInfo, ipAddress, returnUrl);

                case EPaymentGateway.Momo:
                    return GenerateMomoUrl(payment, orderInfo, returnUrl);

                case EPaymentGateway.ZaloPay:
                    return GenerateZaloPayUrl(payment, orderInfo, returnUrl);

                case EPaymentGateway.ShopeePay:
                    return GenerateShopeePayUrl(payment, orderInfo, returnUrl);

                case EPaymentGateway.ViettelPay:
                    return GenerateViettelPayUrl(payment, orderInfo, returnUrl);

                case EPaymentGateway.BankTransfer:
                    return GenerateBankTransferUrl(payment, bankCode, orderInfo);

                default:
                    return GenerateVNPayUrl(payment, method, bankCode, orderInfo, ipAddress, returnUrl);
            }
        }

        private string GenerateVNPayUrl(
            Payment payment,
            EPaymentMethod? method,
            string? bankCode,
            string orderInfo,
            string ipAddress,
            string? returnUrl)
        {
            var baseUrl = _vnPayService.CreatePaymentUrl(payment.Id, payment.Amount, orderInfo, ipAddress);

            // Add method-specific parameters
            if (method.HasValue)
            {
                switch (method.Value)
                {
                    case EPaymentMethod.CreditCard:
                    case EPaymentMethod.DebitCard:
                        baseUrl += "&vnp_CardType=CREDIT";
                        break;
                    case EPaymentMethod.BankTransfer:
                        if (!string.IsNullOrEmpty(bankCode))
                        {
                            baseUrl += $"&vnp_BankCode={bankCode}";
                        }
                        break;
                }
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                baseUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            return baseUrl;
        }

        private string GenerateMomoUrl(Payment payment, string orderInfo, string? returnUrl)
        {
            // In production, integrate with Momo SDK
            var baseUrl = "https://test-payment.momo.vn/v2/gateway/api/create";
            return $"{baseUrl}?partnerCode=MOMO&orderId=PAY{payment.Id}&amount={payment.Amount}&orderInfo={Uri.EscapeDataString(orderInfo)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "http://localhost:3000/payment/callback")}";
        }

        private string GenerateZaloPayUrl(Payment payment, string orderInfo, string? returnUrl)
        {
            // In production, integrate with ZaloPay SDK
            var baseUrl = "https://sb-openapi.zalopay.vn/v2/create";
            return $"{baseUrl}?app_id=2553&app_trans_id=PAY{payment.Id}_{DateTime.Now:yyMMdd}_{DateTime.Now.Ticks}&amount={payment.Amount}&item={Uri.EscapeDataString(orderInfo)}&callback_url={Uri.EscapeDataString(returnUrl ?? "http://localhost:3000/payment/callback")}";
        }

        private string GenerateShopeePayUrl(Payment payment, string orderInfo, string? returnUrl)
        {
            // In production, integrate with ShopeePay SDK
            var baseUrl = "https://payment.shopeepay.vn/api/v1/payment";
            return $"{baseUrl}?merchant_id=SHOPEE&order_id=PAY{payment.Id}&amount={payment.Amount}&description={Uri.EscapeDataString(orderInfo)}&return_url={Uri.EscapeDataString(returnUrl ?? "http://localhost:3000/payment/callback")}";
        }

        private string GenerateViettelPayUrl(Payment payment, string orderInfo, string? returnUrl)
        {
            // In production, integrate with ViettelPay SDK
            var baseUrl = "https://payment.viettelpay.vn/api/v1/payment";
            return $"{baseUrl}?merchant_code=VIETTEL&transaction_id=PAY{payment.Id}&amount={payment.Amount}&description={Uri.EscapeDataString(orderInfo)}&return_url={Uri.EscapeDataString(returnUrl ?? "http://localhost:3000/payment/callback")}";
        }

        private string GenerateBankTransferUrl(Payment payment, string? bankCode, string orderInfo)
        {
            // Generate QR code or bank transfer instructions page
            var baseUrl = "http://localhost:3000/payment/bank-transfer";
            return $"{baseUrl}?paymentId={payment.Id}&amount={payment.Amount}&bankCode={bankCode ?? "VIETCOMBANK"}&description={Uri.EscapeDataString(orderInfo)}";
        }

        private string GeneratePaymentUrl(Payment payment, string orderInfo)
        {
            // Legacy method - kept for backward compatibility
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (payment.PaymentGateway?.ToLower() == "vnpay")
            {
                return _vnPayService.CreatePaymentUrl(payment.Id, payment.Amount, orderInfo, ipAddress);
            }

            var baseUrl = payment.PaymentGateway?.ToLower() switch
            {
                "momo" => "https://test-payment.momo.vn/v2/gateway/api/create",
                "zalopay" => "https://sb-openapi.zalopay.vn/v2/create",
                _ => "https://payment-gateway.example.com/pay"
            };

            return $"{baseUrl}?amount={payment.Amount}&paymentId={payment.Id}";
        }
    }
}

