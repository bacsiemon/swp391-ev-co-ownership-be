using EvCoOwnership.Repositories.DTOs.PaymentDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IVnPayService
    {
        /// <summary>
        /// Creates VNPay payment URL
        /// </summary>
        string CreatePaymentUrl(int paymentId, decimal amount, string orderInfo, string ipAddress);

        /// <summary>
        /// Processes VNPay callback/return request
        /// </summary>
        VnPayCallbackResponse ProcessCallback(VnPayCallbackRequest request);
    }
}
