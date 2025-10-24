using EvCoOwnership.Helpers.Configuration;
using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Repositories.DTOs.PaymentDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace EvCoOwnership.Services.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _vnPayConfig;

        public VnPayService(IOptions<VnPayConfig> vnPayConfig)
        {
            _vnPayConfig = vnPayConfig.Value;
        }

        public string CreatePaymentUrl(int paymentId, decimal amount, string orderInfo, string ipAddress)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_vnPayConfig.TimeZoneId);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayHelper();

            var urlCallBack = _vnPayConfig.ReturnUrl;

            // Add required VNPay parameters
            vnpay.AddRequestData("vnp_Version", _vnPayConfig.Version);
            vnpay.AddRequestData("vnp_Command", _vnPayConfig.Command);
            vnpay.AddRequestData("vnp_TmnCode", _vnPayConfig.TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString()); // VNPay requires amount in cents
            vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _vnPayConfig.CurrCode);
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", _vnPayConfig.Locale);
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other"); // Default order type
            vnpay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            vnpay.AddRequestData("vnp_TxnRef", $"{paymentId}_{tick}"); // Unique transaction reference
            vnpay.AddRequestData("vnp_ExpireDate", timeNow.AddMinutes(15).ToString("yyyyMMddHHmmss")); // 15 minutes expiry

            var paymentUrl = vnpay.CreateRequestUrl(_vnPayConfig.BaseUrl, _vnPayConfig.HashSecret);

            return paymentUrl;
        }

        public VnPayCallbackResponse ProcessCallback(VnPayCallbackRequest request)
        {
            var vnpay = new VnPayHelper();

            // Add all response data for signature validation (must match all params VNPay sends)
            vnpay.AddResponseData("vnp_TmnCode", request.vnp_TmnCode);
            vnpay.AddResponseData("vnp_Amount", request.vnp_Amount);
            vnpay.AddResponseData("vnp_BankCode", request.vnp_BankCode);
            vnpay.AddResponseData("vnp_BankTranNo", request.vnp_BankTranNo);
            vnpay.AddResponseData("vnp_CardType", request.vnp_CardType);
            vnpay.AddResponseData("vnp_PayDate", request.vnp_PayDate);
            vnpay.AddResponseData("vnp_OrderInfo", request.vnp_OrderInfo);
            vnpay.AddResponseData("vnp_TransactionNo", request.vnp_TransactionNo);
            vnpay.AddResponseData("vnp_ResponseCode", request.vnp_ResponseCode);
            vnpay.AddResponseData("vnp_TransactionStatus", request.vnp_TransactionStatus);
            vnpay.AddResponseData("vnp_TxnRef", request.vnp_TxnRef);
            vnpay.AddResponseData("vnp_SecureHashType", request.vnp_SecureHashType);
            
            // Add additional fields for signature validation
            vnpay.AddResponseData("vnp_Version", request.vnp_Version);
            vnpay.AddResponseData("vnp_Command", request.vnp_Command);
            vnpay.AddResponseData("vnp_CurrCode", request.vnp_CurrCode);
            vnpay.AddResponseData("vnp_Locale", request.vnp_Locale);
            vnpay.AddResponseData("vnp_IpAddr", request.vnp_IpAddr);
            vnpay.AddResponseData("vnp_CreateDate", request.vnp_CreateDate);

            // Validate signature
            var isValidSignature = vnpay.ValidateSignature(request.vnp_SecureHash, _vnPayConfig.HashSecret);

            if (!isValidSignature)
            {
                return new VnPayCallbackResponse
                {
                    RspCode = "97",
                    Message = "Invalid signature",
                    Success = false
                };
            }

            // Parse payment ID from TxnRef (format: {paymentId}_{tick})
            var txnRefParts = request.vnp_TxnRef.Split('_');
            if (txnRefParts.Length < 1 || !int.TryParse(txnRefParts[0], out var paymentId))
            {
                return new VnPayCallbackResponse
                {
                    RspCode = "99",
                    Message = "Invalid transaction reference",
                    Success = false
                };
            }

            // Parse amount (VNPay returns amount in cents)
            if (!decimal.TryParse(request.vnp_Amount, out var amountInCents))
            {
                return new VnPayCallbackResponse
                {
                    RspCode = "99",
                    Message = "Invalid amount",
                    Success = false
                };
            }

            var amount = amountInCents / 100;

            // Check transaction status
            var isSuccess = request.vnp_ResponseCode == "00" && request.vnp_TransactionStatus == "00";

            return new VnPayCallbackResponse
            {
                RspCode = request.vnp_ResponseCode,
                Message = GetResponseMessage(request.vnp_ResponseCode),
                PaymentId = paymentId,
                Amount = amount,
                TransactionId = request.vnp_TransactionNo,
                BankCode = request.vnp_BankCode,
                BankTranNo = request.vnp_BankTranNo,
                PayDate = request.vnp_PayDate,
                Success = isSuccess
            };
        }

        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => "Giao dịch không thành công"
            };
        }
    }
}
