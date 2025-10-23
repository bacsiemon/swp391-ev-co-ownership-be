using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.FundDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for fund management operations
    /// </summary>
    public class FundService : IFundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FundService> _logger;

        public FundService(IUnitOfWork unitOfWork, ILogger<FundService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets current fund balance for a vehicle
        /// </summary>
        public async Task<BaseResponse<FundBalanceResponse>> GetFundBalanceAsync(int vehicleId, int requestingUserId)
        {
            try
            {
                // Verify vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<FundBalanceResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if user is co-owner or admin
                var hasAccess = await CheckUserAccessToVehicleAsync(vehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<FundBalanceResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                // Check if vehicle has a fund
                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<FundBalanceResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND_FOR_VEHICLE",
                        Data = null
                    };
                }

                var fund = await _unitOfWork.FundRepository.GetByIdAsync(vehicle.FundId.Value);
                if (fund == null)
                {
                    return new BaseResponse<FundBalanceResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                // Get fund additions and usages
                var fundAdditions = await _unitOfWork.FundAdditionRepository
                    .GetQueryable()
                    .Where(fa => fa.FundId == fund.Id && fa.StatusEnum == EFundAdditionStatus.Completed)
                    .ToListAsync();

                var fundUsages = await _unitOfWork.FundUsageRepository
                    .GetQueryable()
                    .Where(fu => fu.FundId == fund.Id)
                    .ToListAsync();

                var totalAdded = fundAdditions.Sum(fa => fa.Amount);
                var totalUsed = fundUsages.Sum(fu => fu.Amount);
                var currentBalance = fund.CurrentBalance ?? 0;

                // Calculate recommended minimum balance (2-3 months of average expenses)
                var avgMonthlyExpense = await CalculateAverageMonthlyExpenseAsync(fund.Id);
                var recommendedMinBalance = avgMonthlyExpense * 2;

                // Determine balance status
                var balanceStatus = DetermineBalanceStatus(currentBalance, recommendedMinBalance);

                var response = new FundBalanceResponse
                {
                    FundId = fund.Id,
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    CurrentBalance = currentBalance,
                    TotalAddedAmount = totalAdded,
                    TotalUsedAmount = totalUsed,
                    TotalAdditions = fundAdditions.Count,
                    TotalUsages = fundUsages.Count,
                    CreatedAt = fund.CreatedAt,
                    UpdatedAt = fund.UpdatedAt,
                    BalanceStatus = balanceStatus,
                    RecommendedMinBalance = recommendedMinBalance
                };

                return new BaseResponse<FundBalanceResponse>
                {
                    StatusCode = 200,
                    Message = "FUND_BALANCE_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund balance for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<FundBalanceResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets fund additions history for a vehicle
        /// </summary>
        public async Task<BaseResponse<List<FundAdditionResponse>>> GetFundAdditionsAsync(
            int vehicleId, 
            int requestingUserId, 
            int pageNumber = 1, 
            int pageSize = 20)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<List<FundAdditionResponse>>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<List<FundAdditionResponse>>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<List<FundAdditionResponse>>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND_FOR_VEHICLE",
                        Data = new List<FundAdditionResponse>()
                    };
                }

                var fundAdditions = await _unitOfWork.FundAdditionRepository
                    .GetQueryable()
                    .Include(fa => fa.CoOwner)
                        .ThenInclude(co => co.User)
                    .Where(fa => fa.FundId == vehicle.FundId.Value)
                    .OrderByDescending(fa => fa.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = fundAdditions.Select(fa => new FundAdditionResponse
                {
                    Id = fa.Id,
                    FundId = fa.FundId ?? 0,
                    CoOwnerId = fa.CoOwnerId,
                    CoOwnerName = fa.CoOwner?.User != null 
                        ? $"{fa.CoOwner.User.FirstName} {fa.CoOwner.User.LastName}".Trim()
                        : "Unknown",
                    Amount = fa.Amount,
                    PaymentMethod = fa.PaymentMethodEnum?.ToString() ?? "Unknown",
                    TransactionId = fa.TransactionId,
                    Description = fa.Description,
                    Status = fa.StatusEnum?.ToString() ?? "Unknown",
                    CreatedAt = fa.CreatedAt
                }).ToList();

                return new BaseResponse<List<FundAdditionResponse>>
                {
                    StatusCode = 200,
                    Message = "FUND_ADDITIONS_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund additions for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<List<FundAdditionResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets fund usages history for a vehicle
        /// </summary>
        public async Task<BaseResponse<List<FundUsageResponse>>> GetFundUsagesAsync(
            int vehicleId, 
            int requestingUserId, 
            int pageNumber = 1, 
            int pageSize = 20)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<List<FundUsageResponse>>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<List<FundUsageResponse>>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<List<FundUsageResponse>>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND_FOR_VEHICLE",
                        Data = new List<FundUsageResponse>()
                    };
                }

                var fundUsages = await _unitOfWork.FundUsageRepository
                    .GetQueryable()
                    .Where(fu => fu.FundId == vehicle.FundId.Value)
                    .OrderByDescending(fu => fu.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = fundUsages.Select(fu => new FundUsageResponse
                {
                    Id = fu.Id,
                    FundId = fu.FundId ?? 0,
                    UsageType = fu.UsageTypeEnum?.ToString() ?? "Other",
                    Amount = fu.Amount,
                    Description = fu.Description ?? "",
                    ImageUrl = fu.ImageUrl,
                    MaintenanceCostId = fu.MaintenanceCostId,
                    CreatedAt = fu.CreatedAt
                }).ToList();

                return new BaseResponse<List<FundUsageResponse>>
                {
                    StatusCode = 200,
                    Message = "FUND_USAGES_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund usages for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<List<FundUsageResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets comprehensive fund summary with statistics
        /// </summary>
        public async Task<BaseResponse<FundSummaryResponse>> GetFundSummaryAsync(
            int vehicleId, 
            int requestingUserId, 
            int monthsToAnalyze = 6)
        {
            try
            {
                // Get balance first
                var balanceResponse = await GetFundBalanceAsync(vehicleId, requestingUserId);
                if (balanceResponse.StatusCode != 200 || balanceResponse.Data == null)
                {
                    return new BaseResponse<FundSummaryResponse>
                    {
                        StatusCode = balanceResponse.StatusCode,
                        Message = balanceResponse.Message,
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                var fundId = vehicle!.FundId!.Value;

                // Get recent additions (last 10)
                var recentAdditionsResponse = await GetFundAdditionsAsync(vehicleId, requestingUserId, 1, 10);
                var recentAdditions = recentAdditionsResponse.Data ?? new List<FundAdditionResponse>();

                // Get recent usages (last 10)
                var recentUsagesResponse = await GetFundUsagesAsync(vehicleId, requestingUserId, 1, 10);
                var recentUsages = recentUsagesResponse.Data ?? new List<FundUsageResponse>();

                // Calculate statistics
                var statistics = await CalculateFundStatisticsAsync(fundId, monthsToAnalyze);

                var summary = new FundSummaryResponse
                {
                    Balance = balanceResponse.Data,
                    RecentAdditions = recentAdditions,
                    RecentUsages = recentUsages,
                    Statistics = statistics
                };

                return new BaseResponse<FundSummaryResponse>
                {
                    StatusCode = 200,
                    Message = "FUND_SUMMARY_RETRIEVED_SUCCESSFULLY",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund summary for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<FundSummaryResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Checks if user has access to vehicle (is co-owner or admin)
        /// </summary>
        private async Task<bool> CheckUserAccessToVehicleAsync(int vehicleId, int userId)
        {
            // Check if user is admin
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user?.RoleEnum == EUserRole.Admin || user?.RoleEnum == EUserRole.Staff)
            {
                return true;
            }

            // Check if user is co-owner
            var coOwner = await _unitOfWork.CoOwnerRepository
                .GetQueryable()
                .FirstOrDefaultAsync(co => co.UserId == userId);

            if (coOwner == null)
            {
                return false;
            }

            var vehicleCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                .GetQueryable()
                .FirstOrDefaultAsync(vco => vco.VehicleId == vehicleId && vco.CoOwnerId == coOwner.UserId);

            return vehicleCoOwner != null;
        }

        /// <summary>
        /// Calculates average monthly expense for a fund
        /// </summary>
        private async Task<decimal> CalculateAverageMonthlyExpenseAsync(int fundId)
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            
            var recentUsages = await _unitOfWork.FundUsageRepository
                .GetQueryable()
                .Where(fu => fu.FundId == fundId && fu.CreatedAt >= sixMonthsAgo)
                .ToListAsync();

            if (!recentUsages.Any())
            {
                return 1000000; // Default 1M VND if no data
            }

            var totalExpense = recentUsages.Sum(fu => fu.Amount);
            var monthsCovered = 6;
            
            return totalExpense / monthsCovered;
        }

        /// <summary>
        /// Determines balance status based on current balance vs recommended
        /// </summary>
        private string DetermineBalanceStatus(decimal currentBalance, decimal recommendedMinBalance)
        {
            if (currentBalance >= recommendedMinBalance * 1.5m)
            {
                return "Healthy";
            }
            else if (currentBalance >= recommendedMinBalance)
            {
                return "Warning";
            }
            else
            {
                return "Low";
            }
        }

        /// <summary>
        /// Calculates comprehensive fund statistics
        /// </summary>
        private async Task<FundStatistics> CalculateFundStatisticsAsync(int fundId, int monthsToAnalyze)
        {
            var startDate = DateTime.UtcNow.AddMonths(-monthsToAnalyze);

            // Get all additions and usages within period
            var additions = await _unitOfWork.FundAdditionRepository
                .GetQueryable()
                .Where(fa => fa.FundId == fundId && fa.CreatedAt >= startDate && fa.StatusEnum == EFundAdditionStatus.Completed)
                .ToListAsync();

            var usages = await _unitOfWork.FundUsageRepository
                .GetQueryable()
                .Where(fu => fu.FundId == fundId && fu.CreatedAt >= startDate)
                .ToListAsync();

            // Calculate averages
            var totalAdded = additions.Sum(a => a.Amount);
            var totalUsed = usages.Sum(u => u.Amount);
            var avgMonthlyAddition = monthsToAnalyze > 0 ? totalAdded / monthsToAnalyze : 0;
            var avgMonthlyUsage = monthsToAnalyze > 0 ? totalUsed / monthsToAnalyze : 0;
            var netMonthlyFlow = avgMonthlyAddition - avgMonthlyUsage;

            // Calculate months covered
            var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundId);
            var currentBalance = fund?.CurrentBalance ?? 0;
            var monthsCovered = avgMonthlyUsage > 0 ? (int)(currentBalance / avgMonthlyUsage) : 0;

            // Group usage by type
            var usageByType = usages
                .GroupBy(u => u.UsageTypeEnum?.ToString() ?? "Other")
                .ToDictionary(g => g.Key, g => g.Sum(u => u.Amount));

            // Calculate monthly flows
            var monthlyFlows = new List<MonthlyFundFlow>();
            for (int i = monthsToAnalyze - 1; i >= 0; i--)
            {
                var monthStart = DateTime.UtcNow.AddMonths(-i).Date;
                var monthEnd = monthStart.AddMonths(1);

                var monthAdditions = additions
                    .Where(a => a.CreatedAt >= monthStart && a.CreatedAt < monthEnd)
                    .Sum(a => a.Amount);

                var monthUsages = usages
                    .Where(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd)
                    .Sum(u => u.Amount);

                monthlyFlows.Add(new MonthlyFundFlow
                {
                    Year = monthStart.Year,
                    Month = monthStart.Month,
                    TotalAdded = monthAdditions,
                    TotalUsed = monthUsages,
                    NetFlow = monthAdditions - monthUsages,
                    EndingBalance = 0 // Will be calculated cumulatively if needed
                });
            }

            return new FundStatistics
            {
                AverageMonthlyAddition = avgMonthlyAddition,
                AverageMonthlyUsage = avgMonthlyUsage,
                NetMonthlyFlow = netMonthlyFlow,
                MonthsCovered = monthsCovered,
                UsageByType = usageByType,
                MonthlyFlows = monthlyFlows
            };
        }

        #endregion
    }
}
