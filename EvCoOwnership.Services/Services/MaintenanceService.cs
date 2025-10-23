using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.MaintenanceDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Services.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<MaintenanceResponse>> CreateMaintenanceAsync(CreateMaintenanceRequest request)
        {
            try
            {
                // Check if vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // If booking ID provided, verify it exists
                if (request.BookingId.HasValue)
                {
                    var booking = await _unitOfWork.BookingRepository.GetByIdAsync(request.BookingId.Value);
                    if (booking == null)
                    {
                        return new BaseResponse<MaintenanceResponse>
                        {
                            StatusCode = 404,
                            Message = "BOOKING_NOT_FOUND"
                        };
                    }
                }

                var maintenance = new MaintenanceCost
                {
                    VehicleId = request.VehicleId,
                    BookingId = request.BookingId,
                    MaintenanceTypeEnum = request.MaintenanceType,
                    Description = request.Description,
                    Cost = request.Cost,
                    IsPaid = false,
                    ServiceProvider = request.ServiceProvider,
                    ServiceDate = request.ServiceDate,
                    OdometerReading = request.OdometerReading,
                    ImageUrl = request.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.MaintenanceCostRepository.AddAsync(maintenance);
                await _unitOfWork.SaveChangesAsync();

                var maintenanceResponse = await GetMaintenanceResponseAsync(maintenance.Id);

                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 201,
                    Message = "MAINTENANCE_CREATED_SUCCESSFULLY",
                    Data = maintenanceResponse
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<MaintenanceResponse>> GetMaintenanceByIdAsync(int maintenanceId)
        {
            try
            {
                var maintenanceResponse = await GetMaintenanceResponseAsync(maintenanceId);
                if (maintenanceResponse == null)
                {
                    return new BaseResponse<MaintenanceResponse>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_NOT_FOUND"
                    };
                }

                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = maintenanceResponse
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<MaintenanceResponse>>> GetVehicleMaintenancesAsync(int vehicleId, int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.MaintenanceCostRepository.GetQueryable()
                    .Include(m => m.Vehicle)
                    .Where(m => m.VehicleId == vehicleId)
                    .OrderByDescending(m => m.ServiceDate);

                var totalCount = await query.CountAsync();
                var maintenances = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var maintenanceResponses = maintenances.Select(MapToMaintenanceResponse).ToList();
                var pagedResult = new PagedResult<MaintenanceResponse>(maintenanceResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<MaintenanceResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<PagedResult<MaintenanceResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<VehicleMaintenanceHistoryResponse>> GetVehicleMaintenanceHistoryAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<VehicleMaintenanceHistoryResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                var maintenances = await _unitOfWork.MaintenanceCostRepository.GetQueryable()
                    .Include(m => m.Vehicle)
                    .Where(m => m.VehicleId == vehicleId)
                    .OrderByDescending(m => m.ServiceDate)
                    .ToListAsync();

                var maintenanceResponses = maintenances.Select(MapToMaintenanceResponse).ToList();
                var totalCost = maintenances.Sum(m => m.Cost);
                var lastMaintenanceDate = maintenances.Any() 
                    ? maintenances.Max(m => m.ServiceDate).ToDateTime(TimeOnly.MinValue) 
                    : (DateTime?)null;

                // Calculate next scheduled maintenance (example: every 6 months or 10,000 km)
                DateTime? nextScheduled = null;
                if (lastMaintenanceDate.HasValue)
                {
                    nextScheduled = lastMaintenanceDate.Value.AddMonths(6);
                }

                var history = new VehicleMaintenanceHistoryResponse
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name ?? "",
                    LicensePlate = vehicle.LicensePlate ?? "",
                    MaintenanceHistory = maintenanceResponses,
                    TotalMaintenanceCost = totalCost,
                    LastMaintenanceDate = lastMaintenanceDate,
                    NextScheduledMaintenance = nextScheduled
                };

                return new BaseResponse<VehicleMaintenanceHistoryResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = history
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<VehicleMaintenanceHistoryResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<PagedResult<MaintenanceResponse>>> GetAllMaintenancesAsync(int pageIndex, int pageSize)
        {
            try
            {
                var query = _unitOfWork.MaintenanceCostRepository.GetQueryable()
                    .Include(m => m.Vehicle)
                    .OrderByDescending(m => m.ServiceDate);

                var totalCount = await query.CountAsync();
                var maintenances = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var maintenanceResponses = maintenances.Select(MapToMaintenanceResponse).ToList();
                var pagedResult = new PagedResult<MaintenanceResponse>(maintenanceResponses, totalCount, pageIndex, pageSize);

                return new BaseResponse<PagedResult<MaintenanceResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<PagedResult<MaintenanceResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<MaintenanceResponse>> UpdateMaintenanceAsync(int maintenanceId, UpdateMaintenanceRequest request)
        {
            try
            {
                var maintenance = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(maintenanceId);
                if (maintenance == null)
                {
                    return new BaseResponse<MaintenanceResponse>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_NOT_FOUND"
                    };
                }

                // Update fields
                if (request.MaintenanceType.HasValue)
                    maintenance.MaintenanceTypeEnum = request.MaintenanceType.Value;
                if (!string.IsNullOrWhiteSpace(request.Description))
                    maintenance.Description = request.Description;
                if (request.Cost.HasValue)
                    maintenance.Cost = request.Cost.Value;
                if (!string.IsNullOrWhiteSpace(request.ServiceProvider))
                    maintenance.ServiceProvider = request.ServiceProvider;
                if (request.ServiceDate.HasValue)
                    maintenance.ServiceDate = request.ServiceDate.Value;
                if (request.OdometerReading.HasValue)
                    maintenance.OdometerReading = request.OdometerReading.Value;
                if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                    maintenance.ImageUrl = request.ImageUrl;
                if (request.IsPaid.HasValue)
                    maintenance.IsPaid = request.IsPaid.Value;

                await _unitOfWork.MaintenanceCostRepository.UpdateAsync(maintenance);
                await _unitOfWork.SaveChangesAsync();

                var maintenanceResponse = await GetMaintenanceResponseAsync(maintenanceId);

                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_UPDATED_SUCCESSFULLY",
                    Data = maintenanceResponse
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<string>> DeleteMaintenanceAsync(int maintenanceId)
        {
            try
            {
                var maintenance = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(maintenanceId);
                if (maintenance == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_NOT_FOUND"
                    };
                }

                await _unitOfWork.MaintenanceCostRepository.DeleteAsync(maintenance);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_DELETED_SUCCESSFULLY"
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

        public async Task<BaseResponse<string>> MarkMaintenanceAsPaidAsync(int maintenanceId)
        {
            try
            {
                var maintenance = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(maintenanceId);
                if (maintenance == null)
                {
                    return new BaseResponse<string>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_NOT_FOUND"
                    };
                }

                maintenance.IsPaid = true;
                await _unitOfWork.MaintenanceCostRepository.UpdateAsync(maintenance);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<string>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_MARKED_AS_PAID"
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

        public async Task<BaseResponse<MaintenanceStatisticsResponse>> GetMaintenanceStatisticsAsync()
        {
            try
            {
                var allMaintenances = await _unitOfWork.MaintenanceCostRepository.GetAllAsync();

                var statistics = new MaintenanceStatisticsResponse
                {
                    TotalMaintenances = allMaintenances.Count(),
                    PaidMaintenances = allMaintenances.Count(m => m.IsPaid == true),
                    UnpaidMaintenances = allMaintenances.Count(m => m.IsPaid == false),
                    TotalCost = allMaintenances.Sum(m => m.Cost),
                    PaidAmount = allMaintenances.Where(m => m.IsPaid == true).Sum(m => m.Cost),
                    UnpaidAmount = allMaintenances.Where(m => m.IsPaid == false).Sum(m => m.Cost),
                    MaintenanceTypeCount = allMaintenances
                        .GroupBy(m => m.MaintenanceTypeEnum)
                        .ToDictionary(g => g.Key ?? EMaintenanceType.Routine, g => g.Count()),
                    MaintenanceTypeCost = allMaintenances
                        .GroupBy(m => m.MaintenanceTypeEnum)
                        .ToDictionary(g => g.Key ?? EMaintenanceType.Routine, g => g.Sum(m => m.Cost))
                };

                return new BaseResponse<MaintenanceStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        public async Task<BaseResponse<MaintenanceStatisticsResponse>> GetVehicleMaintenanceStatisticsAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceStatisticsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                var maintenances = await _unitOfWork.MaintenanceCostRepository.GetQueryable()
                    .Where(m => m.VehicleId == vehicleId)
                    .ToListAsync();

                var statistics = new MaintenanceStatisticsResponse
                {
                    TotalMaintenances = maintenances.Count,
                    PaidMaintenances = maintenances.Count(m => m.IsPaid == true),
                    UnpaidMaintenances = maintenances.Count(m => m.IsPaid == false),
                    TotalCost = maintenances.Sum(m => m.Cost),
                    PaidAmount = maintenances.Where(m => m.IsPaid == true).Sum(m => m.Cost),
                    UnpaidAmount = maintenances.Where(m => m.IsPaid == false).Sum(m => m.Cost),
                    MaintenanceTypeCount = maintenances
                        .GroupBy(m => m.MaintenanceTypeEnum)
                        .ToDictionary(g => g.Key ?? EMaintenanceType.Routine, g => g.Count()),
                    MaintenanceTypeCost = maintenances
                        .GroupBy(m => m.MaintenanceTypeEnum)
                        .ToDictionary(g => g.Key ?? EMaintenanceType.Routine, g => g.Sum(m => m.Cost))
                };

                return new BaseResponse<MaintenanceStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        private async Task<MaintenanceResponse?> GetMaintenanceResponseAsync(int maintenanceId)
        {
            var maintenance = await _unitOfWork.MaintenanceCostRepository.GetQueryable()
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == maintenanceId);

            return maintenance == null ? null : MapToMaintenanceResponse(maintenance);
        }

        private MaintenanceResponse MapToMaintenanceResponse(MaintenanceCost maintenance)
        {
            return new MaintenanceResponse
            {
                Id = maintenance.Id,
                VehicleId = maintenance.VehicleId ?? 0,
                VehicleName = maintenance.Vehicle?.Name ?? "",
                LicensePlate = maintenance.Vehicle?.LicensePlate ?? "",
                BookingId = maintenance.BookingId,
                MaintenanceType = maintenance.MaintenanceTypeEnum ?? EMaintenanceType.Routine,
                Description = maintenance.Description ?? "",
                Cost = maintenance.Cost,
                IsPaid = maintenance.IsPaid ?? false,
                ServiceProvider = maintenance.ServiceProvider ?? "",
                ServiceDate = maintenance.ServiceDate,
                OdometerReading = maintenance.OdometerReading,
                ImageUrl = maintenance.ImageUrl,
                CreatedAt = maintenance.CreatedAt ?? DateTime.UtcNow
            };
        }
    }
}
