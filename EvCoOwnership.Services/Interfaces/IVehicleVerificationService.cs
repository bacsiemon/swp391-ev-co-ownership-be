using EvCoOwnership.Repositories.DTOs.VehicleDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IVehicleVerificationService
    {
        Task<BaseResponse<VehicleDetailResponse>> GetVehicleDetailAsync(int vehicleId);
        Task<BaseResponse<IEnumerable<VehicleDetailResponse>>> GetVehiclesPendingVerificationAsync();
        Task<BaseResponse<IEnumerable<VehicleDetailResponse>>> GetVehiclesByVerificationStatusAsync(EVehicleVerificationStatus status);
        Task<BaseResponse<VehicleVerificationResponse>> VerifyVehicleAsync(VehicleVerificationRequest request, int staffId);
        Task<BaseResponse<IEnumerable<VehicleVerificationResponse>>> GetVerificationHistoryAsync(int vehicleId);
        Task<BaseResponse<VehicleDetailResponse>> CreateVehicleAsync(VehicleCreateRequest request, int userId);
        Task<BaseResponse<bool>> RequestVerificationAsync(int vehicleId);
    }
}