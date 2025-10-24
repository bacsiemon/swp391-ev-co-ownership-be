using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.CheckInCheckOutDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service for vehicle check-in/check-out operations
    /// Supports both QR code scanning (self-service) and manual staff verification
    /// </summary>
    public interface ICheckInCheckOutService
    {
        #region QR Code Check-In/Out (CoOwner Self-Service)

        /// <summary>
        /// QR scan check-in - CoOwner confirms vehicle pickup using QR code
        /// </summary>
        Task<BaseResponse<CheckInResponse>> QRScanCheckInAsync(int userId, QRScanCheckInRequest request);

        /// <summary>
        /// QR scan check-out - CoOwner confirms vehicle return using QR code
        /// </summary>
        Task<BaseResponse<CheckOutResponse>> QRScanCheckOutAsync(int userId, QRScanCheckOutRequest request);

        /// <summary>
        /// Generates QR code data for a confirmed booking
        /// </summary>
        Task<BaseResponse<VehicleQRCodeData>> GenerateBookingQRCodeAsync(int bookingId, int userId);

        #endregion

        #region Manual Check-In/Out (Staff Verification)

        /// <summary>
        /// Manual check-in - Staff verifies and confirms vehicle pickup
        /// </summary>
        Task<BaseResponse<CheckInResponse>> ManualCheckInAsync(int staffId, ManualCheckInRequest request);

        /// <summary>
        /// Manual check-out - Staff verifies and confirms vehicle return
        /// </summary>
        Task<BaseResponse<CheckOutResponse>> ManualCheckOutAsync(int staffId, ManualCheckOutRequest request);

        #endregion

        #region Validation & Utilities

        /// <summary>
        /// Validates if booking is ready for check-in
        /// </summary>
        Task<BaseResponse<object>> ValidateCheckInEligibilityAsync(int bookingId, int userId);

        /// <summary>
        /// Validates if booking is ready for check-out
        /// </summary>
        Task<BaseResponse<object>> ValidateCheckOutEligibilityAsync(int bookingId, int userId);

        /// <summary>
        /// Gets check-in/check-out history for a booking
        /// </summary>
        Task<BaseResponse<object>> GetBookingCheckInOutHistoryAsync(int bookingId, int userId);

        #endregion
    }
}
