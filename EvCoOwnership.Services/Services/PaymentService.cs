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
                // Create payment record
                var payment = new Payment
                {
                    UserId = userId,
                    Amount = request.Amount,
                    PaymentGateway = request.PaymentGateway,
                    StatusEnum = EPaymentStatus.Pending,
                    FundAdditionId = request.FundAdditionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // Generate payment URL using VNPay service
                var transactionRef = $"PAY_{payment.Id}_{DateTime.UtcNow.Ticks}";
                var paymentUrl = GeneratePaymentUrl(payment, request.Description ?? "Thanh toán dịch vụ");

                var response = new PaymentUrlResponse
                {
                    PaymentUrl = paymentUrl,
                    PaymentId = payment.Id,
                    TransactionRef = transactionRef
                };

                return new BaseResponse<PaymentUrlResponse>
                {
                    StatusCode = 201,
                    Message = "PAYMENT_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return new BaseResponse<PaymentStatisticsResponse>
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
            return new PaymentResponse
            {
                Id = payment.Id,
                UserId = payment.UserId ?? 0,
                UserName = payment.User != null 
                    ? $"{payment.User.FirstName} {payment.User.LastName}".Trim() 
                    : "",
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
                PaymentGateway = payment.PaymentGateway ?? "",
                Status = payment.StatusEnum ?? EPaymentStatus.Pending,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt ?? DateTime.UtcNow,
                FundAdditionId = payment.FundAdditionId
            };
        }

        private string GeneratePaymentUrl(Payment payment, string orderInfo)
        {
            // Get client IP address
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Use VNPay service for VNPay gateway
            if (payment.PaymentGateway?.ToLower() == "vnpay")
            {
                return _vnPayService.CreatePaymentUrl(payment.Id, payment.Amount, orderInfo, ipAddress);
            }

            // Fallback for other gateways (MoMo, ZaloPay, etc.)
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
