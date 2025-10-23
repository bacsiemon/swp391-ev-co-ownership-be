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
    }
}
