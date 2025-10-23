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

        /// <summary>
        /// Creates a new fund usage record (expense)
        /// </summary>
        public async Task<BaseResponse<FundUsageResponse>> CreateFundUsageAsync(
            CreateFundUsageRequest request, 
            int requestingUserId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(request.VehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND_FOR_VEHICLE",
                        Data = null
                    };
                }

                // Validate amount
                if (request.Amount <= 0)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_AMOUNT",
                        Data = null
                    };
                }

                // Check if fund has sufficient balance
                var fund = await _unitOfWork.FundRepository.GetByIdAsync(vehicle.FundId.Value);
                if (fund == null)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                if ((fund.CurrentBalance ?? 0) < request.Amount)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 400,
                        Message = "INSUFFICIENT_FUND_BALANCE",
                        Data = null
                    };
                }

                // Validate maintenance cost if provided
                if (request.MaintenanceCostId.HasValue)
                {
                    var maintenance = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(request.MaintenanceCostId.Value);
                    if (maintenance == null)
                    {
                        return new BaseResponse<FundUsageResponse>
                        {
                            StatusCode = 404,
                            Message = "MAINTENANCE_COST_NOT_FOUND",
                            Data = null
                        };
                    }
                }

                // Create fund usage
                var fundUsage = new EvCoOwnership.Repositories.Models.FundUsage
                {
                    FundId = vehicle.FundId.Value,
                    UsageTypeEnum = request.UsageType,
                    Amount = request.Amount,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    MaintenanceCostId = request.MaintenanceCostId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FundUsageRepository.AddAsync(fundUsage);

                // Update fund balance
                fund.CurrentBalance -= request.Amount;
                fund.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.FundRepository.UpdateAsync(fund);

                await _unitOfWork.SaveChangesAsync();

                var response = new FundUsageResponse
                {
                    Id = fundUsage.Id,
                    FundId = fundUsage.FundId ?? 0,
                    UsageType = fundUsage.UsageTypeEnum?.ToString() ?? "Other",
                    Amount = fundUsage.Amount,
                    Description = fundUsage.Description ?? "",
                    ImageUrl = fundUsage.ImageUrl,
                    MaintenanceCostId = fundUsage.MaintenanceCostId,
                    CreatedAt = fundUsage.CreatedAt
                };

                return new BaseResponse<FundUsageResponse>
                {
                    StatusCode = 201,
                    Message = "FUND_USAGE_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fund usage for vehicle {VehicleId}", request.VehicleId);
                return new BaseResponse<FundUsageResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Updates an existing fund usage record
        /// </summary>
        public async Task<BaseResponse<FundUsageResponse>> UpdateFundUsageAsync(
            int usageId, 
            UpdateFundUsageRequest request, 
            int requestingUserId)
        {
            try
            {
                var fundUsage = await _unitOfWork.FundUsageRepository.GetByIdAsync(usageId);
                if (fundUsage == null)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_USAGE_NOT_FOUND",
                        Data = null
                    };
                }

                var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundUsage.FundId ?? 0);
                if (fund == null)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                // Get vehicle from fund
                var vehicle = await _unitOfWork.VehicleRepository
                    .GetQueryable()
                    .FirstOrDefaultAsync(v => v.FundId == fund.Id);

                if (vehicle == null)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicle.Id, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                var oldAmount = fundUsage.Amount;
                var newAmount = request.Amount ?? oldAmount;

                // Validate new amount
                if (request.Amount.HasValue && request.Amount.Value <= 0)
                {
                    return new BaseResponse<FundUsageResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_AMOUNT",
                        Data = null
                    };
                }

                // Check if fund has sufficient balance for increased amount
                if (newAmount > oldAmount)
                {
                    var difference = newAmount - oldAmount;
                    if ((fund.CurrentBalance ?? 0) < difference)
                    {
                        return new BaseResponse<FundUsageResponse>
                        {
                            StatusCode = 400,
                            Message = "INSUFFICIENT_FUND_BALANCE",
                            Data = null
                        };
                    }
                }

                // Update fund usage
                if (request.UsageType.HasValue)
                    fundUsage.UsageTypeEnum = request.UsageType.Value;
                
                if (request.Amount.HasValue)
                    fundUsage.Amount = request.Amount.Value;
                
                if (request.Description != null)
                    fundUsage.Description = request.Description;
                
                if (request.ImageUrl != null)
                    fundUsage.ImageUrl = request.ImageUrl;
                
                if (request.MaintenanceCostId.HasValue)
                    fundUsage.MaintenanceCostId = request.MaintenanceCostId;

                await _unitOfWork.FundUsageRepository.UpdateAsync(fundUsage);

                // Update fund balance if amount changed
                if (newAmount != oldAmount)
                {
                    fund.CurrentBalance += oldAmount; // Refund old amount
                    fund.CurrentBalance -= newAmount; // Deduct new amount
                    fund.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.FundRepository.UpdateAsync(fund);
                }

                await _unitOfWork.SaveChangesAsync();

                var response = new FundUsageResponse
                {
                    Id = fundUsage.Id,
                    FundId = fundUsage.FundId ?? 0,
                    UsageType = fundUsage.UsageTypeEnum?.ToString() ?? "Other",
                    Amount = fundUsage.Amount,
                    Description = fundUsage.Description ?? "",
                    ImageUrl = fundUsage.ImageUrl,
                    MaintenanceCostId = fundUsage.MaintenanceCostId,
                    CreatedAt = fundUsage.CreatedAt
                };

                return new BaseResponse<FundUsageResponse>
                {
                    StatusCode = 200,
                    Message = "FUND_USAGE_UPDATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fund usage {UsageId}", usageId);
                return new BaseResponse<FundUsageResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Deletes a fund usage record
        /// </summary>
        public async Task<BaseResponse<object>> DeleteFundUsageAsync(
            int usageId, 
            int requestingUserId)
        {
            try
            {
                var fundUsage = await _unitOfWork.FundUsageRepository.GetByIdAsync(usageId);
                if (fundUsage == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "FUND_USAGE_NOT_FOUND",
                        Data = null
                    };
                }

                var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundUsage.FundId ?? 0);
                if (fund == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                // Get vehicle from fund
                var vehicle = await _unitOfWork.VehicleRepository
                    .GetQueryable()
                    .FirstOrDefaultAsync(v => v.FundId == fund.Id);

                if (vehicle == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicle.Id, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                // Refund the amount back to fund
                fund.CurrentBalance += fundUsage.Amount;
                fund.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.FundRepository.UpdateAsync(fund);

                // Delete the usage record
                await _unitOfWork.FundUsageRepository.DeleteAsync(fundUsage);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "FUND_USAGE_DELETED_SUCCESSFULLY",
                    Data = new { deletedId = usageId, refundedAmount = fundUsage.Amount }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fund usage {UsageId}", usageId);
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets fund usages by category type
        /// </summary>
        public async Task<BaseResponse<List<FundUsageResponse>>> GetFundUsagesByCategoryAsync(
            int vehicleId, 
            EUsageType category, 
            int requestingUserId, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
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

                var query = _unitOfWork.FundUsageRepository
                    .GetQueryable()
                    .Where(fu => fu.FundId == vehicle.FundId.Value && fu.UsageTypeEnum == category);

                if (startDate.HasValue)
                {
                    query = query.Where(fu => fu.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(fu => fu.CreatedAt <= endDate.Value);
                }

                var fundUsages = await query
                    .OrderByDescending(fu => fu.CreatedAt)
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
                    Message = "FUND_USAGES_BY_CATEGORY_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fund usages by category for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<List<FundUsageResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets category-based budget analysis for current month
        /// </summary>
        public async Task<BaseResponse<FundCategoryAnalysisResponse>> GetCategoryBudgetAnalysisAsync(
            int vehicleId, 
            int requestingUserId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<FundCategoryAnalysisResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<FundCategoryAnalysisResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<FundCategoryAnalysisResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND_FOR_VEHICLE",
                        Data = null
                    };
                }

                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Get current month usages
                var monthUsages = await _unitOfWork.FundUsageRepository
                    .GetQueryable()
                    .Where(fu => fu.FundId == vehicle.FundId.Value && 
                                 fu.CreatedAt >= monthStart && 
                                 fu.CreatedAt < monthEnd)
                    .ToListAsync();

                // Define default budget limits (can be customized per vehicle)
                var defaultBudgets = new Dictionary<EUsageType, decimal>
                {
                    { EUsageType.Maintenance, 3000000 },  // 3M VND
                    { EUsageType.Insurance, 1000000 },    // 1M VND
                    { EUsageType.Fuel, 2000000 },         // 2M VND (charging)
                    { EUsageType.Parking, 500000 },       // 500K VND
                    { EUsageType.Other, 1000000 }         // 1M VND
                };

                var categoryBudgets = new List<FundCategoryBudget>();
                decimal totalBudget = 0;
                decimal totalSpending = 0;

                foreach (var category in Enum.GetValues<EUsageType>())
                {
                    var categoryUsages = monthUsages.Where(u => u.UsageTypeEnum == category).ToList();
                    var spending = categoryUsages.Sum(u => u.Amount);
                    var budget = defaultBudgets[category];
                    var remaining = budget - spending;
                    var utilization = budget > 0 ? (spending / budget) * 100 : 0;

                    string status;
                    if (spending > budget)
                        status = "Exceeded";
                    else if (utilization >= 80)
                        status = "Warning";
                    else
                        status = "OnTrack";

                    categoryBudgets.Add(new FundCategoryBudget
                    {
                        Category = category,
                        MonthlyBudgetLimit = budget,
                        CurrentMonthSpending = spending,
                        RemainingBudget = remaining,
                        BudgetUtilizationPercent = utilization,
                        BudgetStatus = status,
                        TransactionCount = categoryUsages.Count,
                        AverageTransactionAmount = categoryUsages.Any() ? spending / categoryUsages.Count : 0
                    });

                    totalBudget += budget;
                    totalSpending += spending;
                }

                var analysis = new FundCategoryAnalysisResponse
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name,
                    AnalysisMonth = now.Month,
                    AnalysisYear = now.Year,
                    CategoryBudgets = categoryBudgets,
                    TotalBudget = totalBudget,
                    TotalSpending = totalSpending,
                    OverallUtilizationPercent = totalBudget > 0 ? (totalSpending / totalBudget) * 100 : 0
                };

                return new BaseResponse<FundCategoryAnalysisResponse>
                {
                    StatusCode = 200,
                    Message = "CATEGORY_BUDGET_ANALYSIS_RETRIEVED_SUCCESSFULLY",
                    Data = analysis
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category budget analysis for vehicle {VehicleId}", vehicleId);
                return new BaseResponse<FundCategoryAnalysisResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion
    }
}
