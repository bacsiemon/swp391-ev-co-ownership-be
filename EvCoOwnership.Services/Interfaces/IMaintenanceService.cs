using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.MaintenanceDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    public interface IMaintenanceService
    {
        Task<BaseResponse<MaintenanceResponse>> CreateMaintenanceAsync(CreateMaintenanceRequest request);
        Task<BaseResponse<MaintenanceResponse>> GetMaintenanceByIdAsync(int maintenanceId);
        Task<BaseResponse<PagedResult<MaintenanceResponse>>> GetVehicleMaintenancesAsync(int vehicleId, int pageIndex, int pageSize);
        Task<BaseResponse<VehicleMaintenanceHistoryResponse>> GetVehicleMaintenanceHistoryAsync(int vehicleId);
        Task<BaseResponse<PagedResult<MaintenanceResponse>>> GetAllMaintenancesAsync(int pageIndex, int pageSize);
        Task<BaseResponse<MaintenanceResponse>> UpdateMaintenanceAsync(int maintenanceId, UpdateMaintenanceRequest request);
        Task<BaseResponse<string>> DeleteMaintenanceAsync(int maintenanceId);
        Task<BaseResponse<string>> MarkMaintenanceAsPaidAsync(int maintenanceId);
        Task<BaseResponse<MaintenanceStatisticsResponse>> GetMaintenanceStatisticsAsync();
        Task<BaseResponse<MaintenanceStatisticsResponse>> GetVehicleMaintenanceStatisticsAsync(int vehicleId);
    }
}
