using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.BookingDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BaseResponse<BookingResponse>> CreateBookingAsync(int userId, CreateBookingRequest request);
        Task<BaseResponse<BookingResponse>> GetBookingByIdAsync(int bookingId, int userId);
        Task<BaseResponse<PagedResult<BookingResponse>>> GetUserBookingsAsync(int userId, int pageIndex, int pageSize);
        Task<BaseResponse<PagedResult<BookingResponse>>> GetVehicleBookingsAsync(int vehicleId, int pageIndex, int pageSize);
        Task<BaseResponse<PagedResult<BookingResponse>>> GetAllBookingsAsync(int pageIndex, int pageSize);
        Task<BaseResponse<BookingResponse>> UpdateBookingAsync(int bookingId, int userId, UpdateBookingRequest request);
        Task<BaseResponse<BookingResponse>> ApproveBookingAsync(int bookingId, int approverId, ApproveBookingRequest request);
        Task<BaseResponse<string>> CancelBookingAsync(int bookingId, int userId);
        Task<BaseResponse<string>> DeleteBookingAsync(int bookingId);
        Task<BaseResponse<BookingStatisticsResponse>> GetBookingStatisticsAsync();

        // Calendar and availability
        Task<BaseResponse<BookingCalendarResponse>> GetBookingCalendarAsync(int userId, DateTime startDate, DateTime endDate, int? vehicleId = null, string? status = null);
        Task<BaseResponse<VehicleAvailabilityResponse>> CheckVehicleAvailabilityAsync(int vehicleId, DateTime startTime, DateTime endTime);

        // Booking slot requests
        Task<BaseResponse<BookingSlotRequestResponse>> RequestBookingSlotAsync(int vehicleId, int userId, RequestBookingSlotRequest request);
        Task<BaseResponse<BookingSlotRequestResponse>> RespondToSlotRequestAsync(int requestId, int userId, RespondToSlotRequestRequest request);
        Task<BaseResponse<string>> CancelSlotRequestAsync(int requestId, int userId, CancelSlotRequestRequest request);
        Task<BaseResponse<PendingSlotRequestsResponse>> GetPendingSlotRequestsAsync(int vehicleId, int userId);
        Task<BaseResponse<SlotRequestAnalytics>> GetSlotRequestAnalyticsAsync(int vehicleId, int userId, DateTime? startDate = null, DateTime? endDate = null);

        // Booking conflict resolution (Advanced approve/reject with intelligence)
        Task<BaseResponse<BookingConflictResolutionResponse>> ResolveBookingConflictAsync(int bookingId, int userId, ResolveBookingConflictRequest request);
        Task<BaseResponse<PendingConflictsResponse>> GetPendingConflictsAsync(int userId, GetPendingConflictsRequest request);
        Task<BaseResponse<BookingConflictAnalyticsResponse>> GetConflictAnalyticsAsync(int vehicleId, int userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
