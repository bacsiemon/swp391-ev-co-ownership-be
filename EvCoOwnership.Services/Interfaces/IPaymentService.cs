using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<BaseResponse<PaymentUrlResponse>> CreatePaymentAsync(int userId, CreatePaymentRequest request);
        Task<BaseResponse<PaymentResponse>> ProcessPaymentAsync(ProcessPaymentRequest request);
        Task<BaseResponse<PaymentResponse>> GetPaymentByIdAsync(int paymentId);
        Task<BaseResponse<PagedResult<PaymentResponse>>> GetUserPaymentsAsync(int userId, int pageIndex, int pageSize);
        Task<BaseResponse<PagedResult<PaymentResponse>>> GetAllPaymentsAsync(int pageIndex, int pageSize);
        Task<BaseResponse<string>> CancelPaymentAsync(int paymentId, int userId);
        Task<BaseResponse<PaymentStatisticsResponse>> GetPaymentStatisticsAsync();
    }
}
