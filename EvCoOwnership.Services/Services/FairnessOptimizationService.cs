using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.FairnessOptimizationDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for AI-powered fairness analysis and optimization
    /// </summary>
    public class FairnessOptimizationService : IFairnessOptimizationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FairnessOptimizationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Fairness Report

        public async Task<BaseResponse<FairnessReportResponse>> GetFairnessReportAsync(
            int vehicleId,
            int userId,
            GetFairnessReportRequest request)
        {
            try
            {
                // Validate user authorization
                var isCoOwner = await ValidateCoOwnerAccess(vehicleId, userId);
                if (!isCoOwner)
                {
                    return new BaseResponse<FairnessReportResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_FAIRNESS_REPORT"
                    };
                }

                // Get vehicle with all related data
                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .Include(v => v.VehicleCoOwners.Where(vco => vco.StatusEnum == EContractStatus.Active))
                        .ThenInclude(vco => vco.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckIns)
                        .ThenInclude(ci => ci.VehicleCondition)
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckOuts)
                        .ThenInclude(co => co.VehicleCondition)
                    .Include(v => v.MaintenanceCosts)
                    .Include(v => v.Fund)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<FairnessReportResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                var startDate = request.StartDate ?? vehicle.CreatedAt ?? DateTime.UtcNow.AddMonths(-3);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Get bookings in date range
                var bookings = vehicle.Bookings
                    .Where(b => b.StartTime >= startDate && b.EndTime <= endDate &&
                               b.StatusEnum == EBookingStatus.Completed)
                    .ToList();

                // Calculate usage metrics for each co-owner
                var coOwnerDetails = new List<CoOwnerFairnessDetail>();
                decimal totalHours = 0, totalDistance = 0;
                int totalBookings = bookings.Count;

                foreach (var vco in vehicle.VehicleCoOwners)
                {
                    var coOwnerBookings = bookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();

                    var hours = coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    var distance = coOwnerBookings.Sum(b => CalculateBookingDistance(b));
                    var bookingCount = coOwnerBookings.Count;

                    totalHours += (decimal)hours;
                    totalDistance += distance;

                    coOwnerDetails.Add(new CoOwnerFairnessDetail
                    {
                        CoOwnerId = vco.CoOwnerId,
                        UserId = vco.CoOwner.UserId,
                        CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}".Trim(),
                        Email = vco.CoOwner.User.Email,
                        OwnershipPercentage = vco.OwnershipPercentage,
                        InvestmentAmount = vco.InvestmentAmount
                    });
                }

                // Calculate usage percentages
                foreach (var detail in coOwnerDetails)
                {
                    var coOwnerBookings = bookings.Where(b => b.CoOwnerId == detail.CoOwnerId).ToList();

                    var hours = coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    var distance = coOwnerBookings.Sum(b => CalculateBookingDistance(b));
                    var bookingCount = coOwnerBookings.Count;

                    detail.UsageHoursPercentage = totalHours > 0 ? (decimal)hours / totalHours * 100 : 0;
                    detail.UsageDistancePercentage = totalDistance > 0 ? distance / totalDistance * 100 : 0;
                    detail.UsageBookingsPercentage = totalBookings > 0 ? (decimal)bookingCount / totalBookings * 100 : 0;

                    detail.AverageUsagePercentage = (detail.UsageHoursPercentage +
                                                     detail.UsageDistancePercentage +
                                                     detail.UsageBookingsPercentage) / 3;

                    detail.UsageVsOwnershipDelta = detail.AverageUsagePercentage - detail.OwnershipPercentage;
                    detail.UsagePattern = ClassifyUsagePattern(detail.UsageVsOwnershipDelta);
                    detail.FairnessScore = CalculateFairnessScore(detail.UsageVsOwnershipDelta);

                    // Calculate cost shares
                    var totalMaintenanceCost = vehicle.MaintenanceCosts
                        .Where(mc => mc.CreatedAt >= startDate && mc.CreatedAt <= endDate)
                        .Sum(mc => mc.Cost);

                    detail.ExpectedCostShare = totalMaintenanceCost * (detail.OwnershipPercentage / 100);
                    detail.ActualCostShare = totalMaintenanceCost * (detail.AverageUsagePercentage / 100);
                    detail.CostAdjustmentNeeded = detail.ActualCostShare - detail.ExpectedCostShare;

                    // Generate recommendations
                    detail.Recommendations = GenerateCoOwnerRecommendations(detail);
                }

                // Calculate overall metrics
                var overview = CalculateFairnessOverview(coOwnerDetails);
                var metrics = CalculateFairnessMetrics(vehicle, bookings, startDate, endDate);
                var recommendations = request.IncludeRecommendations
                    ? GenerateFairnessRecommendations(coOwnerDetails, overview, metrics)
                    : new List<FairnessRecommendation>();

                var response = new FairnessReportResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    ReportStartDate = startDate,
                    ReportEndDate = endDate,
                    Overview = overview,
                    CoOwnersDetails = coOwnerDetails,
                    Recommendations = recommendations,
                    Metrics = metrics,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<FairnessReportResponse>
                {
                    StatusCode = 200,
                    Message = "FAIRNESS_REPORT_GENERATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<FairnessReportResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = new { Exception = ex.Message }
                };
            }
        }

        #endregion

        #region Fair Schedule Suggestions

        public async Task<BaseResponse<FairScheduleSuggestionsResponse>> GetFairScheduleSuggestionsAsync(
            int vehicleId,
            int userId,
            GetFairScheduleSuggestionsRequest request)
        {
            try
            {
                var isCoOwner = await ValidateCoOwnerAccess(vehicleId, userId);
                if (!isCoOwner)
                {
                    return new BaseResponse<FairScheduleSuggestionsResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_SCHEDULE_SUGGESTIONS"
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .Include(v => v.VehicleCoOwners.Where(vco => vco.StatusEnum == EContractStatus.Active))
                        .ThenInclude(vco => vco.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(v => v.Bookings)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<FairScheduleSuggestionsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Analyze historical usage patterns
                var historicalBookings = vehicle.Bookings
                    .Where(b => b.StartTime < request.StartDate &&
                               b.StatusEnum != EBookingStatus.Cancelled)
                    .ToList();

                // Calculate current usage percentages
                var totalHours = historicalBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var coOwnerCurrentUsage = vehicle.VehicleCoOwners.ToDictionary(
                    vco => vco.CoOwnerId,
                    vco =>
                    {
                        var bookings = historicalBookings.Where(b => b.CoOwnerId == vco.CoOwnerId);
                        var hours = bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                        return totalHours > 0 ? (decimal)(hours / totalHours * 100) : 0;
                    }
                );

                // Generate suggestions for each co-owner
                var coOwnerSuggestions = new List<CoOwnerScheduleSuggestion>();
                var totalDays = (request.EndDate - request.StartDate).Days;
                var totalAvailableHours = totalDays * 24;

                foreach (var vco in vehicle.VehicleCoOwners)
                {
                    var currentUsage = coOwnerCurrentUsage[vco.CoOwnerId];
                    var recommendedUsage = vco.OwnershipPercentage;
                    var usageGap = recommendedUsage - currentUsage;

                    var suggestedHours = (decimal)(totalAvailableHours * 0.3m * (vco.OwnershipPercentage / 100));
                    var suggestedBookings = (int)Math.Ceiling(suggestedHours / (request.PreferredDurationHours ?? 4));

                    var slots = GenerateSuggestedSlots(
                        request.StartDate,
                        request.EndDate,
                        suggestedBookings,
                        request.PreferredDurationHours ?? 4,
                        historicalBookings,
                        request.UsageType
                    );

                    coOwnerSuggestions.Add(new CoOwnerScheduleSuggestion
                    {
                        CoOwnerId = vco.CoOwnerId,
                        CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}".Trim(),
                        OwnershipPercentage = vco.OwnershipPercentage,
                        CurrentUsagePercentage = currentUsage,
                        RecommendedUsagePercentage = recommendedUsage,
                        SuggestedBookingsCount = suggestedBookings,
                        SuggestedTotalHours = suggestedHours,
                        SuggestedSlots = slots,
                        Rationale = GenerateScheduleRationale(currentUsage, recommendedUsage, usageGap)
                    });
                }

                // Generate optimal time slots
                var optimalSlots = AnalyzeOptimalTimeSlots(historicalBookings, vehicle.VehicleCoOwners.ToList());

                // Generate insights
                var insights = GenerateScheduleInsights(historicalBookings, totalAvailableHours);

                var response = new FairScheduleSuggestionsResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    SuggestionPeriodStart = request.StartDate,
                    SuggestionPeriodEnd = request.EndDate,
                    CoOwnerSuggestions = coOwnerSuggestions,
                    OptimalTimeSlots = optimalSlots,
                    Insights = insights,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<FairScheduleSuggestionsResponse>
                {
                    StatusCode = 200,
                    Message = "SCHEDULE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<FairScheduleSuggestionsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = new { Exception = ex.Message }
                };
            }
        }

        #endregion

        #region Maintenance Suggestions

        public async Task<BaseResponse<MaintenanceSuggestionsResponse>> GetMaintenanceSuggestionsAsync(
            int vehicleId,
            int userId,
            GetMaintenanceSuggestionsRequest request)
        {
            try
            {
                var isCoOwner = await ValidateCoOwnerAccess(vehicleId, userId);
                if (!isCoOwner)
                {
                    return new BaseResponse<MaintenanceSuggestionsResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_MAINTENANCE_SUGGESTIONS"
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .Include(v => v.MaintenanceCosts)
                    .Include(v => v.VehicleConditions.OrderByDescending(vc => vc.CreatedAt))
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckIns)
                        .ThenInclude(ci => ci.VehicleCondition)
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckOuts)
                        .ThenInclude(co => co.VehicleCondition)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceSuggestionsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Calculate vehicle health status
                var currentOdometer = vehicle.VehicleConditions.FirstOrDefault()?.OdometerReading ?? 0;
                var healthStatus = CalculateVehicleHealth(vehicle, currentOdometer);

                // Generate maintenance suggestions
                var suggestions = new List<MaintenanceSuggestion>();

                if (request.IncludePredictive)
                {
                    suggestions.AddRange(GeneratePredictiveMaintenanceSuggestions(
                        vehicle,
                        currentOdometer,
                        healthStatus,
                        request.LookaheadDays
                    ));
                }

                // Add rule-based suggestions
                suggestions.AddRange(GenerateRuleBasedMaintenanceSuggestions(
                    vehicle,
                    currentOdometer
                ));

                // Get upcoming scheduled maintenance
                var upcomingMaintenance = GetUpcomingMaintenance(vehicle, currentOdometer);

                // Generate cost forecast
                var costForecast = GenerateMaintenanceCostForecast(
                    vehicle,
                    suggestions,
                    request.LookaheadDays
                );

                var response = new MaintenanceSuggestionsResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    HealthStatus = healthStatus,
                    Suggestions = suggestions.OrderByDescending(s => GetUrgencyScore(s.Urgency)).ToList(),
                    UpcomingMaintenance = upcomingMaintenance,
                    CostForecast = costForecast,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<MaintenanceSuggestionsResponse>
                {
                    StatusCode = 200,
                    Message = "MAINTENANCE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<MaintenanceSuggestionsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = new { Exception = ex.Message }
                };
            }
        }

        #endregion

        #region Cost-Saving Recommendations

        public async Task<BaseResponse<CostSavingRecommendationsResponse>> GetCostSavingRecommendationsAsync(
            int vehicleId,
            int userId,
            GetCostSavingRecommendationsRequest request)
        {
            try
            {
                var isCoOwner = await ValidateCoOwnerAccess(vehicleId, userId);
                if (!isCoOwner)
                {
                    return new BaseResponse<CostSavingRecommendationsResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_COST_RECOMMENDATIONS"
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .Include(v => v.MaintenanceCosts)
                    .Include(v => v.Fund)
                        .ThenInclude(f => f.FundAdditions)
                    .Include(v => v.Fund)
                        .ThenInclude(f => f.FundUsages)
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckIns)
                        .ThenInclude(ci => ci.VehicleCondition)
                    .Include(v => v.Bookings)
                        .ThenInclude(b => b.CheckOuts)
                        .ThenInclude(co => co.VehicleCondition)
                    .Include(v => v.VehicleCoOwners)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<CostSavingRecommendationsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                var analysisStartDate = DateTime.UtcNow.AddDays(-request.AnalysisPeriodDays);

                // Generate cost analysis summary
                var summary = GenerateCostAnalysisSummary(vehicle, analysisStartDate, request.AnalysisPeriodDays);

                // Generate cost-saving recommendations
                var recommendations = new List<CostSavingRecommendation>();

                if (request.IncludeMaintenanceOptimization)
                {
                    recommendations.AddRange(GenerateMaintenanceCostRecommendations(vehicle, summary));
                }

                if (request.IncludeFundOptimization)
                {
                    recommendations.AddRange(GenerateFundOptimizationRecommendations(vehicle, summary));
                }

                // Add general cost-saving tips
                recommendations.AddRange(GenerateGeneralCostSavingRecommendations(vehicle, summary));

                // Generate fund insights
                var fundInsights = GenerateFundOptimizationInsights(vehicle, summary);

                // Generate maintenance insights
                var maintenanceInsights = GenerateMaintenanceOptimizationInsights(vehicle, analysisStartDate);

                var response = new CostSavingRecommendationsResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    Summary = summary,
                    Recommendations = recommendations.OrderByDescending(r => r.PotentialSavingsAmount).ToList(),
                    FundInsights = fundInsights,
                    MaintenanceInsights = maintenanceInsights,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<CostSavingRecommendationsResponse>
                {
                    StatusCode = 200,
                    Message = "COST_SAVING_RECOMMENDATIONS_GENERATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CostSavingRecommendationsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Errors = new { Exception = ex.Message }
                };
            }
        }

        #endregion

        #region Helper Methods - Common

        private async Task<bool> ValidateCoOwnerAccess(int vehicleId, int userId)
        {
            var coOwner = await _unitOfWork.CoOwnerRepository.GetQueryable()
                .FirstOrDefaultAsync(co => co.UserId == userId);

            if (coOwner == null) return false;

            return await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                vco.CoOwnerId == coOwner.UserId &&
                                vco.StatusEnum == EContractStatus.Active);
        }

        private decimal CalculateBookingDistance(Repositories.Models.Booking booking)
        {
            var checkIn = booking.CheckIns?.FirstOrDefault();
            var checkOut = booking.CheckOuts?.FirstOrDefault();

            if (checkIn?.VehicleCondition?.OdometerReading != null &&
                checkOut?.VehicleCondition?.OdometerReading != null)
            {
                return checkOut.VehicleCondition.OdometerReading.Value -
                       checkIn.VehicleCondition.OdometerReading.Value;
            }

            return 0;
        }

        private string ClassifyUsagePattern(decimal delta)
        {
            return delta switch
            {
                > 5 => "Overutilized",
                < -5 => "Underutilized",
                _ => "Balanced"
            };
        }

        private decimal CalculateFairnessScore(decimal delta)
        {
            var absDelta = Math.Abs(delta);
            if (absDelta <= 5) return 100;
            if (absDelta <= 10) return 85;
            if (absDelta <= 15) return 70;
            if (absDelta <= 20) return 55;
            if (absDelta <= 30) return 40;
            return 25;
        }

        #endregion

        #region Helper Methods - Fairness Report

        private List<string> GenerateCoOwnerRecommendations(CoOwnerFairnessDetail detail)
        {
            var recommendations = new List<string>();

            if (detail.UsagePattern == "Overutilized")
            {
                recommendations.Add($"Consider reducing usage by {Math.Abs(detail.UsageVsOwnershipDelta):F1}% to match ownership share");
                if (detail.CostAdjustmentNeeded > 0)
                {
                    recommendations.Add($"Additional cost contribution of {detail.CostAdjustmentNeeded:N0} VND may be fair");
                }
            }
            else if (detail.UsagePattern == "Underutilized")
            {
                recommendations.Add($"You have {Math.Abs(detail.UsageVsOwnershipDelta):F1}% unused capacity available");
                if (detail.CostAdjustmentNeeded < 0)
                {
                    recommendations.Add($"Cost savings of {Math.Abs(detail.CostAdjustmentNeeded):N0} VND may be justified");
                }
            }
            else
            {
                recommendations.Add("Your usage is well-balanced with ownership share");
            }

            return recommendations;
        }

        private FairnessOverview CalculateFairnessOverview(List<CoOwnerFairnessDetail> details)
        {
            var balanced = details.Count(d => d.UsagePattern == "Balanced");
            var overutilized = details.Count(d => d.UsagePattern == "Overutilized");
            var underutilized = details.Count(d => d.UsagePattern == "Underutilized");

            var avgVariance = details.Average(d => Math.Abs(d.UsageVsOwnershipDelta));
            var avgScore = details.Average(d => d.FairnessScore);

            var status = avgScore switch
            {
                >= 90 => "Excellent",
                >= 75 => "Good",
                >= 60 => "Fair",
                _ => "Poor"
            };

            var mainIssue = overutilized > underutilized
                ? $"{overutilized} co-owner(s) are using vehicle more than ownership share"
                : underutilized > 0
                ? $"{underutilized} co-owner(s) are using vehicle less than ownership share"
                : "Usage distribution is balanced among all co-owners";

            return new FairnessOverview
            {
                OverallFairnessStatus = status,
                FairnessScore = avgScore,
                AverageUsageVariance = avgVariance,
                BalancedCoOwnersCount = balanced,
                OverutilizedCoOwnersCount = overutilized,
                UnderutilizedCoOwnersCount = underutilized,
                MainIssue = mainIssue
            };
        }

        private FairnessMetrics CalculateFairnessMetrics(
            Repositories.Models.Vehicle vehicle,
            List<Repositories.Models.Booking> bookings,
            DateTime startDate,
            DateTime endDate)
        {
            var totalHours = (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalDistance = bookings.Sum(b => CalculateBookingDistance(b));
            var totalBookings = bookings.Count;

            var maintenanceCosts = vehicle.MaintenanceCosts
                .Where(mc => mc.CreatedAt >= startDate && mc.CreatedAt <= endDate)
                .Sum(mc => mc.Cost);

            var fundBalance = vehicle.Fund?.CurrentBalance ?? 0;

            var usagePercentages = vehicle.VehicleCoOwners.Select(vco =>
            {
                var coBookings = bookings.Where(b => b.CoOwnerId == vco.CoOwnerId);
                var hours = coBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                return totalHours > 0 ? (decimal)(hours / (double)totalHours * 100) : 0;
            }).ToList();

            var avgUsage = usagePercentages.Any() ? usagePercentages.Average() : 0;
            var usageVariance = usagePercentages.Any()
                ? (decimal)Math.Sqrt(usagePercentages.Sum(p => Math.Pow((double)(p - avgUsage), 2)) / usagePercentages.Count)
                : 0;

            return new FairnessMetrics
            {
                TotalUsageHours = totalHours,
                TotalUsageDistance = totalDistance,
                TotalBookings = totalBookings,
                TotalMaintenanceCost = maintenanceCosts,
                TotalFundBalance = fundBalance,
                UsageVariance = usageVariance,
                CostVariance = 0, // Simplified for now
                OptimalRebalanceFrequencyDays = 30
            };
        }

        private List<FairnessRecommendation> GenerateFairnessRecommendations(
            List<CoOwnerFairnessDetail> details,
            FairnessOverview overview,
            FairnessMetrics metrics)
        {
            var recommendations = new List<FairnessRecommendation>();

            // High priority: Address significant imbalances
            if (overview.OverutilizedCoOwnersCount > 0 && overview.UnderutilizedCoOwnersCount > 0)
            {
                var overutilized = details.Where(d => d.UsagePattern == "Overutilized").ToList();
                var underutilized = details.Where(d => d.UsagePattern == "Underutilized").ToList();

                recommendations.Add(new FairnessRecommendation
                {
                    Type = "Usage",
                    Priority = "High",
                    Title = "Rebalance Usage Distribution",
                    Description = $"{overutilized.Count} co-owner(s) are overutilizing while {underutilized.Count} are underutilizing",
                    ActionItems = new List<string>
                    {
                        "Schedule group meeting to discuss fair usage",
                        "Implement booking rotation system",
                        "Consider adjusting ownership percentages if pattern persists"
                    },
                    ExpectedImpact = metrics.UsageVariance,
                    AffectedCoOwnerIds = overutilized.Concat(underutilized).Select(d => d.CoOwnerId).ToList()
                });
            }

            // Cost-related recommendations
            var costImbalances = details.Where(d => Math.Abs(d.CostAdjustmentNeeded) > 500000).ToList();
            if (costImbalances.Any())
            {
                recommendations.Add(new FairnessRecommendation
                {
                    Type = "Cost",
                    Priority = "High",
                    Title = "Adjust Cost Sharing Based on Actual Usage",
                    Description = $"Cost allocation mismatches detected for {costImbalances.Count} co-owner(s)",
                    ActionItems = new List<string>
                    {
                        "Review maintenance cost distribution",
                        "Implement usage-based cost allocation",
                        "Set up monthly cost reconciliation"
                    },
                    ExpectedImpact = costImbalances.Sum(d => Math.Abs(d.CostAdjustmentNeeded)),
                    AffectedCoOwnerIds = costImbalances.Select(d => d.CoOwnerId).ToList()
                });
            }

            // Schedule optimization
            if (metrics.UsageVariance > 15)
            {
                recommendations.Add(new FairnessRecommendation
                {
                    Type = "Schedule",
                    Priority = "Medium",
                    Title = "Implement Fair Scheduling System",
                    Description = "High usage variance indicates need for better scheduling coordination",
                    ActionItems = new List<string>
                    {
                        "Use the Fair Schedule Suggestions feature",
                        "Set up weekly booking rotation",
                        "Create shared calendar for vehicle availability"
                    },
                    ExpectedImpact = metrics.UsageVariance * 0.6m,
                    AffectedCoOwnerIds = details.Select(d => d.CoOwnerId).ToList()
                });
            }

            return recommendations;
        }

        #endregion

        #region Helper Methods - Schedule Suggestions

        private List<SuggestedBookingSlot> GenerateSuggestedSlots(
            DateTime startDate,
            DateTime endDate,
            int bookingCount,
            int durationHours,
            List<Repositories.Models.Booking> historicalBookings,
            EUsageType? usageType)
        {
            var slots = new List<SuggestedBookingSlot>();
            var totalDays = (endDate - startDate).Days;
            var interval = totalDays / Math.Max(bookingCount, 1);

            for (int i = 0; i < bookingCount; i++)
            {
                var slotDate = startDate.AddDays(i * interval);

                // Analyze best time based on historical data
                var preferredHour = AnalyzeBestBookingTime(historicalBookings, slotDate.DayOfWeek, usageType);

                var slotStart = new DateTime(slotDate.Year, slotDate.Month, slotDate.Day, preferredHour, 0, 0);
                var slotEnd = slotStart.AddHours(durationHours);

                // Check conflict probability
                var conflictProb = CalculateConflictProbability(slotStart, slotEnd, historicalBookings);

                slots.Add(new SuggestedBookingSlot
                {
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    DurationHours = durationHours,
                    Reason = GenerateSlotReason(slotDate.DayOfWeek, preferredHour, usageType),
                    ConflictProbability = conflictProb,
                    Benefits = new List<string>
                    {
                        conflictProb < 0.3m ? "Low conflict risk" : "Moderate conflict risk",
                        "Balanced usage distribution",
                        "Optimal time slot based on historical data"
                    }
                });
            }

            return slots;
        }

        private int AnalyzeBestBookingTime(
            List<Repositories.Models.Booking> bookings,
            DayOfWeek dayOfWeek,
            EUsageType? usageType)
        {
            var dayBookings = bookings
                .Where(b => b.StartTime.DayOfWeek == dayOfWeek)
                .ToList();

            if (!dayBookings.Any())
            {
                return usageType == EUsageType.Maintenance ? 8 : 10; // Default times
            }

            // Find least busy hour
            var hourlyUsage = new int[24];
            foreach (var booking in dayBookings)
            {
                var start = booking.StartTime.Hour;
                var end = booking.EndTime.Hour;
                for (int h = start; h <= end && h < 24; h++)
                {
                    hourlyUsage[h]++;
                }
            }

            var minUsageHour = Array.IndexOf(hourlyUsage, hourlyUsage.Min());
            return Math.Max(6, Math.Min(20, minUsageHour)); // Between 6 AM and 8 PM
        }

        private decimal CalculateConflictProbability(
            DateTime start,
            DateTime end,
            List<Repositories.Models.Booking> historicalBookings)
        {
            var similarBookings = historicalBookings
                .Where(b => b.StartTime.DayOfWeek == start.DayOfWeek &&
                           Math.Abs((b.StartTime.TimeOfDay - start.TimeOfDay).TotalHours) < 2)
                .Count();

            var totalSimilarPeriods = historicalBookings
                .Count(b => b.StartTime.DayOfWeek == start.DayOfWeek);

            return totalSimilarPeriods > 0
                ? (decimal)similarBookings / totalSimilarPeriods
                : 0.2m;
        }

        private string GenerateSlotReason(DayOfWeek day, int hour, EUsageType? usageType)
        {
            var dayName = day.ToString();
            var timeOfDay = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
            var purpose = usageType?.ToString() ?? "general use";

            return $"Optimal {timeOfDay} slot on {dayName} for {purpose}";
        }

        private string GenerateScheduleRationale(decimal current, decimal recommended, decimal gap)
        {
            if (Math.Abs(gap) <= 5)
            {
                return "Your usage is well-balanced. Suggested bookings maintain this balance.";
            }
            else if (gap > 5)
            {
                return $"You're currently underutilizing by {gap:F1}%. Suggested bookings will help you use your fair share.";
            }
            else
            {
                return $"You're currently overutilizing by {Math.Abs(gap):F1}%. Consider reducing bookings to allow other co-owners fair access.";
            }
        }

        private List<OptimalTimeSlot> AnalyzeOptimalTimeSlots(
            List<Repositories.Models.Booking> bookings,
            List<Repositories.Models.VehicleCoOwner> coOwners)
        {
            var slots = new List<OptimalTimeSlot>();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var dayBookings = bookings.Where(b => b.StartTime.DayOfWeek == day).ToList();

                // Morning slot (6-12)
                var morningBookings = dayBookings.Count(b => b.StartTime.Hour >= 6 && b.StartTime.Hour < 12);
                slots.Add(new OptimalTimeSlot
                {
                    DayOfWeek = day,
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    UtilizationRate = dayBookings.Any() ? (decimal)morningBookings / dayBookings.Count * 100 : 0,
                    PeakType = morningBookings > 3 ? "High" : morningBookings > 1 ? "Medium" : "Low",
                    RecommendedForCoOwnerIds = coOwners.Select(co => co.CoOwnerId).ToList()
                });

                // Afternoon slot (12-18)
                var afternoonBookings = dayBookings.Count(b => b.StartTime.Hour >= 12 && b.StartTime.Hour < 18);
                slots.Add(new OptimalTimeSlot
                {
                    DayOfWeek = day,
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    UtilizationRate = dayBookings.Any() ? (decimal)afternoonBookings / dayBookings.Count * 100 : 0,
                    PeakType = afternoonBookings > 3 ? "High" : afternoonBookings > 1 ? "Medium" : "Low",
                    RecommendedForCoOwnerIds = coOwners.Select(co => co.CoOwnerId).ToList()
                });
            }

            return slots.OrderBy(s => s.UtilizationRate).Take(10).ToList();
        }

        private ScheduleOptimizationInsights GenerateScheduleInsights(
            List<Repositories.Models.Booking> bookings,
            double totalAvailableHours)
        {
            var totalUsedHours = bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var currentUtilization = totalAvailableHours > 0
                ? (decimal)(totalUsedHours / totalAvailableHours * 100)
                : 0;

            var conflicts = bookings
                .GroupBy(b => new { Date = b.StartTime.Date, Hour = b.StartTime.Hour })
                .Count(g => g.Count() > 1);

            return new ScheduleOptimizationInsights
            {
                CurrentUtilizationRate = currentUtilization,
                OptimalUtilizationRate = 40m, // 40% is considered optimal
                ConflictingBookingsCount = conflicts,
                PeakUsagePeriods = new List<string> { "Weekends", "Weekday evenings" },
                UnderutilizedPeriods = new List<string> { "Weekday mornings", "Tuesday-Thursday afternoons" },
                PotentialEfficiencyGain = Math.Max(0, 40m - currentUtilization)
            };
        }

        #endregion

        #region Helper Methods - Maintenance Suggestions

        private VehicleHealthStatus CalculateVehicleHealth(
            Repositories.Models.Vehicle vehicle,
            int currentOdometer)
        {
            var lastMaintenance = vehicle.MaintenanceCosts
                .OrderByDescending(mc => mc.ServiceDate)
                .FirstOrDefault();

            var daysSinceLast = lastMaintenance != null
                ? (DateTime.UtcNow - lastMaintenance.ServiceDate.ToDateTime(TimeOnly.MinValue)).Days
                : 999;

            var distanceSinceLast = lastMaintenance != null
                ? currentOdometer - (lastMaintenance.OdometerReading ?? 0)
                : currentOdometer;

            var avgDailyDistance = vehicle.CreatedAt.HasValue && vehicle.CreatedAt.Value < DateTime.UtcNow
                ? (decimal)currentOdometer / Math.Max(1, (DateTime.UtcNow - vehicle.CreatedAt.Value).Days)
                : 50m;

            var healthIssues = new List<string>();
            var healthScore = 100;

            if (daysSinceLast > 180) // 6 months
            {
                healthIssues.Add("No maintenance in over 6 months");
                healthScore -= 20;
            }

            if (distanceSinceLast > 10000) // 10,000 km
            {
                healthIssues.Add("Over 10,000 km since last service");
                healthScore -= 15;
            }

            var overallHealth = healthScore switch
            {
                >= 90 => "Excellent",
                >= 75 => "Good",
                >= 60 => "Fair",
                _ => "Poor"
            };

            return new VehicleHealthStatus
            {
                CurrentOdometer = currentOdometer,
                AverageDailyDistance = avgDailyDistance,
                DaysSinceLastMaintenance = daysSinceLast,
                DistanceSinceLastMaintenance = distanceSinceLast,
                OverallHealth = overallHealth,
                HealthScore = healthScore,
                HealthIssues = healthIssues
            };
        }

        private List<MaintenanceSuggestion> GeneratePredictiveMaintenanceSuggestions(
            Repositories.Models.Vehicle vehicle,
            int currentOdometer,
            VehicleHealthStatus health,
            int lookaheadDays)
        {
            var suggestions = new List<MaintenanceSuggestion>();

            // Battery maintenance (EVs need regular battery checks)
            if (health.DaysSinceLastMaintenance > 90)
            {
                suggestions.Add(new MaintenanceSuggestion
                {
                    MaintenanceType = EMaintenanceType.Routine,
                    Title = "Battery Health Check",
                    Description = "Regular battery inspection recommended for optimal EV performance",
                    Urgency = health.DaysSinceLastMaintenance > 180 ? "High" : "Medium",
                    Reason = $"{health.DaysSinceLastMaintenance} days since last maintenance",
                    RecommendedDate = DateTime.UtcNow.AddDays(7),
                    DaysUntilRecommended = 7,
                    EstimatedCost = 500000m,
                    CostSavingIfDoneNow = 200000m,
                    Consequences = new List<string>
                    {
                        "Reduced battery life if neglected",
                        "Potential range reduction",
                        "Higher costs for emergency repairs"
                    },
                    Benefits = new List<string>
                    {
                        "Extended battery lifespan",
                        "Maintained vehicle range",
                        "Early detection of issues"
                    }
                });
            }

            // Tire rotation based on distance
            if (health.DistanceSinceLastMaintenance > 8000)
            {
                suggestions.Add(new MaintenanceSuggestion
                {
                    MaintenanceType = EMaintenanceType.Routine,
                    Title = "Tire Rotation and Inspection",
                    Description = "Tire rotation recommended every 8,000-10,000 km",
                    Urgency = health.DistanceSinceLastMaintenance > 10000 ? "High" : "Medium",
                    Reason = $"{health.DistanceSinceLastMaintenance} km since last service",
                    RecommendedOdometerReading = currentOdometer + 2000,
                    RecommendedDate = DateTime.UtcNow.AddDays(14),
                    DaysUntilRecommended = 14,
                    EstimatedCost = 800000m,
                    CostSavingIfDoneNow = 300000m,
                    Consequences = new List<string>
                    {
                        "Uneven tire wear",
                        "Reduced safety and handling",
                        "Need for premature tire replacement"
                    },
                    Benefits = new List<string>
                    {
                        "Extended tire life",
                        "Improved safety",
                        "Better fuel efficiency"
                    }
                });
            }

            return suggestions;
        }

        private List<MaintenanceSuggestion> GenerateRuleBasedMaintenanceSuggestions(
            Repositories.Models.Vehicle vehicle,
            int currentOdometer)
        {
            var suggestions = new List<MaintenanceSuggestion>();

            // Annual inspection
            var vehicleAge = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - vehicle.PurchaseDate.DayNumber) / 365;

            if (vehicleAge >= 1)
            {
                var lastInspection = vehicle.MaintenanceCosts
                    .Where(mc => mc.MaintenanceTypeEnum == EMaintenanceType.Routine)
                    .OrderByDescending(mc => mc.ServiceDate)
                    .FirstOrDefault();

                var daysSinceInspection = lastInspection != null
                    ? (DateTime.UtcNow - lastInspection.ServiceDate.ToDateTime(TimeOnly.MinValue)).Days
                    : 999;

                if (daysSinceInspection > 330) // Almost a year
                {
                    suggestions.Add(new MaintenanceSuggestion
                    {
                        MaintenanceType = EMaintenanceType.Routine,
                        Title = "Annual Vehicle Inspection",
                        Description = "Mandatory annual safety and emissions inspection",
                        Urgency = daysSinceInspection > 365 ? "Critical" : "High",
                        Reason = "Required by law annually",
                        RecommendedDate = DateTime.UtcNow.AddDays(30),
                        DaysUntilRecommended = 30,
                        EstimatedCost = 1500000m,
                        CostSavingIfDoneNow = 0,
                        Consequences = new List<string>
                        {
                            "Legal penalties if overdue",
                            "Insurance issues",
                            "Cannot legally operate vehicle"
                        },
                        Benefits = new List<string>
                        {
                            "Legal compliance",
                            "Safety assurance",
                            "Valid insurance coverage"
                        }
                    });
                }
            }

            return suggestions;
        }

        private List<UpcomingMaintenance> GetUpcomingMaintenance(
            Repositories.Models.Vehicle vehicle,
            int currentOdometer)
        {
            var upcoming = new List<UpcomingMaintenance>();

            // Simplified - in real app, this would be based on maintenance schedule
            upcoming.Add(new UpcomingMaintenance
            {
                MaintenanceType = EMaintenanceType.Routine,
                DueDate = DateTime.UtcNow.AddDays(30),
                DaysUntilDue = 30,
                OdometerDue = currentOdometer + 5000,
                EstimatedCost = 2000000m,
                IsOverdue = false
            });

            return upcoming;
        }

        private MaintenanceCostForecast GenerateMaintenanceCostForecast(
            Repositories.Models.Vehicle vehicle,
            List<MaintenanceSuggestion> suggestions,
            int forecastDays)
        {
            var forecastMonths = forecastDays / 30;
            var totalEstimatedCost = suggestions
                .Where(s => s.DaysUntilRecommended <= forecastDays)
                .Sum(s => s.EstimatedCost);

            var avgMonthlyCost = forecastMonths > 0 ? totalEstimatedCost / forecastMonths : 0;
            var coOwnerCount = vehicle.VehicleCoOwners.Count(vco => vco.StatusEnum == EContractStatus.Active);
            var costPerCoOwner = coOwnerCount > 0 ? totalEstimatedCost / coOwnerCount : 0;

            var monthlyForecasts = new List<MonthlyMaintenanceForecast>();
            for (int i = 1; i <= Math.Min(forecastMonths, 6); i++)
            {
                var monthSuggestions = suggestions
                    .Where(s => s.DaysUntilRecommended > (i - 1) * 30 && s.DaysUntilRecommended <= i * 30)
                    .ToList();

                monthlyForecasts.Add(new MonthlyMaintenanceForecast
                {
                    Month = DateTime.UtcNow.AddMonths(i).ToString("MMM yyyy"),
                    EstimatedCost = monthSuggestions.Sum(s => s.EstimatedCost),
                    ExpectedMaintenanceTypes = monthSuggestions.Select(s => s.MaintenanceType.ToString()).ToList()
                });
            }

            return new MaintenanceCostForecast
            {
                ForecastPeriodDays = forecastDays,
                EstimatedTotalCost = totalEstimatedCost,
                AverageMonthlyCost = avgMonthlyCost,
                CostPerCoOwnerAverage = costPerCoOwner,
                MonthlyForecasts = monthlyForecasts,
                CostDrivers = new List<string>
                {
                    "Regular scheduled maintenance",
                    "Battery health checks",
                    "Tire maintenance"
                }
            };
        }

        private int GetUrgencyScore(string urgency)
        {
            return urgency switch
            {
                "Critical" => 4,
                "High" => 3,
                "Medium" => 2,
                "Low" => 1,
                _ => 0
            };
        }

        #endregion

        #region Helper Methods - Cost Saving

        private CostAnalysisSummary GenerateCostAnalysisSummary(
            Repositories.Models.Vehicle vehicle,
            DateTime startDate,
            int analysisDays)
        {
            var maintenanceCosts = vehicle.MaintenanceCosts
                .Where(mc => mc.CreatedAt >= startDate)
                .ToList();

            var totalCost = maintenanceCosts.Sum(mc => mc.Cost);
            var avgMonthlyCost = analysisDays > 0 ? totalCost / (analysisDays / 30m) : 0;

            var totalDistance = vehicle.Bookings
                .Where(b => b.StartTime >= startDate)
                .Sum(b => CalculateBookingDistance(b));

            var totalBookings = vehicle.Bookings.Count(b => b.StartTime >= startDate);

            var costPerKm = totalDistance > 0 ? totalCost / totalDistance : 0;
            var costPerBooking = totalBookings > 0 ? totalCost / totalBookings : 0;

            var costBreakdowns = maintenanceCosts
                .GroupBy(mc => mc.MaintenanceTypeEnum)
                .Select(g => new CostBreakdown
                {
                    Category = g.Key?.ToString() ?? "Unknown",
                    Amount = g.Sum(mc => mc.Cost),
                    Percentage = totalCost > 0 ? g.Sum(mc => mc.Cost) / totalCost * 100 : 0,
                    Trend = "Stable"
                })
                .ToList();

            return new CostAnalysisSummary
            {
                AnalysisPeriodDays = analysisDays,
                TotalCostsIncurred = totalCost,
                AverageMonthlyCost = avgMonthlyCost,
                CostPerKm = costPerKm,
                CostPerBooking = costPerBooking,
                PotentialSavings = 0, // Calculated by recommendations
                SavingsPercentage = 0,
                CostBreakdowns = costBreakdowns
            };
        }

        private List<CostSavingRecommendation> GenerateMaintenanceCostRecommendations(
            Repositories.Models.Vehicle vehicle,
            CostAnalysisSummary summary)
        {
            var recommendations = new List<CostSavingRecommendation>();

            var routineCost = summary.CostBreakdowns
                .FirstOrDefault(cb => cb.Category == "Routine")?.Amount ?? 0;

            if (routineCost > 0)
            {
                recommendations.Add(new CostSavingRecommendation
                {
                    Category = "Maintenance",
                    Priority = "High",
                    Title = "Switch to Preventive Maintenance Schedule",
                    Description = "Regular preventive maintenance costs less than reactive repairs",
                    PotentialSavingsAmount = routineCost * 0.3m,
                    PotentialSavingsPercentage = 30m,
                    TimeframeForSavings = "6-12 months",
                    ActionSteps = new List<string>
                    {
                        "Create maintenance schedule based on odometer readings",
                        "Book services during off-peak times for discounts",
                        "Establish relationship with preferred service provider",
                        "Keep detailed maintenance records"
                    },
                    Difficulty = "Easy",
                    ImplementationCost = 0,
                    ROI = 300m
                });
            }

            return recommendations;
        }

        private List<CostSavingRecommendation> GenerateFundOptimizationRecommendations(
            Repositories.Models.Vehicle vehicle,
            CostAnalysisSummary summary)
        {
            var recommendations = new List<CostSavingRecommendation>();

            var currentBalance = vehicle.Fund?.CurrentBalance ?? 0;
            var recommendedBalance = summary.AverageMonthlyCost * 3; // 3 months buffer

            if (currentBalance > recommendedBalance * 2)
            {
                recommendations.Add(new CostSavingRecommendation
                {
                    Category = "Fund",
                    Priority = "Medium",
                    Title = "Optimize Fund Balance",
                    Description = "Fund balance exceeds recommended amount - consider redistributing excess",
                    PotentialSavingsAmount = currentBalance - recommendedBalance,
                    PotentialSavingsPercentage = ((currentBalance - recommendedBalance) / currentBalance) * 100,
                    TimeframeForSavings = "Immediate",
                    ActionSteps = new List<string>
                    {
                        "Review fund balance with all co-owners",
                        "Distribute excess funds proportionally to ownership",
                        "Maintain 3-month expense buffer only",
                        "Reinvest excess in vehicle improvements"
                    },
                    Difficulty = "Easy",
                    ImplementationCost = 0,
                    ROI = 100m
                });
            }
            else if (currentBalance < recommendedBalance)
            {
                recommendations.Add(new CostSavingRecommendation
                {
                    Category = "Fund",
                    Priority = "High",
                    Title = "Increase Fund Balance to Avoid Emergency Expenses",
                    Description = "Low fund balance may lead to unexpected individual payments",
                    PotentialSavingsAmount = 0,
                    PotentialSavingsPercentage = 0,
                    TimeframeForSavings = "1-3 months",
                    ActionSteps = new List<string>
                    {
                        "Set up automatic monthly contributions",
                        "Target 3-month expense buffer",
                        "Review and adjust contribution amounts",
                        "Notify co-owners of funding needs"
                    },
                    Difficulty = "Medium",
                    ImplementationCost = recommendedBalance - currentBalance,
                    ROI = 0
                });
            }

            return recommendations;
        }

        private List<CostSavingRecommendation> GenerateGeneralCostSavingRecommendations(
            Repositories.Models.Vehicle vehicle,
            CostAnalysisSummary summary)
        {
            var recommendations = new List<CostSavingRecommendation>();

            recommendations.Add(new CostSavingRecommendation
            {
                Category = "General",
                Priority = "Medium",
                Title = "Optimize Charging Costs",
                Description = "Charge during off-peak hours to reduce electricity costs",
                PotentialSavingsAmount = summary.AverageMonthlyCost * 0.15m,
                PotentialSavingsPercentage = 15m,
                TimeframeForSavings = "Ongoing",
                ActionSteps = new List<string>
                {
                    "Use time-of-use electricity rates",
                    "Schedule charging for overnight (off-peak)",
                    "Install home charging station for lower rates",
                    "Track charging costs per co-owner"
                },
                Difficulty = "Easy",
                ImplementationCost = 0,
                ROI = 150m
            });

            return recommendations;
        }

        private FundOptimizationInsights GenerateFundOptimizationInsights(
            Repositories.Models.Vehicle vehicle,
            CostAnalysisSummary summary)
        {
            var currentBalance = vehicle.Fund?.CurrentBalance ?? 0;
            var avgMonthlyExpense = summary.AverageMonthlyCost;
            var recommendedMin = avgMonthlyExpense * 2;
            var recommendedOptimal = avgMonthlyExpense * 3;

            var monthsCovered = avgMonthlyExpense > 0 ? (int)(currentBalance / avgMonthlyExpense) : 0;

            var healthIssues = new List<string>();
            if (currentBalance < recommendedMin)
                healthIssues.Add("Fund balance below recommended minimum");
            if (monthsCovered < 2)
                healthIssues.Add("Insufficient buffer for unexpected expenses");

            return new FundOptimizationInsights
            {
                CurrentFundBalance = currentBalance,
                RecommendedMinimumBalance = recommendedMin,
                RecommendedOptimalBalance = recommendedOptimal,
                IsUnderfunded = currentBalance < recommendedMin,
                IsOverfunded = currentBalance > recommendedOptimal * 2,
                AverageMonthlyExpenses = avgMonthlyExpense,
                MonthsCovered = monthsCovered,
                FundHealthIssues = healthIssues,
                FundOptimizationTips = new List<string>
                {
                    "Maintain 2-3 months of expenses as buffer",
                    "Set up automatic contributions from all co-owners",
                    "Review fund quarterly and adjust as needed"
                }
            };
        }

        private MaintenanceOptimizationInsights GenerateMaintenanceOptimizationInsights(
            Repositories.Models.Vehicle vehicle,
            DateTime startDate)
        {
            var maintenanceCosts = vehicle.MaintenanceCosts
                .Where(mc => mc.CreatedAt >= startDate)
                .ToList();

            var avgCost = maintenanceCosts.Any() ? maintenanceCosts.Average(mc => mc.Cost) : 0;

            var preventive = maintenanceCosts.Count(mc =>
                mc.MaintenanceTypeEnum == EMaintenanceType.Routine ||
                mc.MaintenanceTypeEnum == EMaintenanceType.Upgrade);

            var reactive = maintenanceCosts.Count - preventive;
            var total = maintenanceCosts.Count;

            var preventiveRatio = total > 0 ? (decimal)preventive / total * 100 : 0;
            var reactiveRatio = total > 0 ? (decimal)reactive / total * 100 : 0;

            var potentialSavings = reactive * avgCost * 0.4m; // Reactive costs 40% more

            var highCostTypes = maintenanceCosts
                .GroupBy(mc => mc.MaintenanceTypeEnum)
                .OrderByDescending(g => g.Sum(mc => mc.Cost))
                .Take(3)
                .Select(g => g.Key?.ToString() ?? "Unknown")
                .ToList();

            return new MaintenanceOptimizationInsights
            {
                AverageMaintenanceCost = avgCost,
                PreventiveMaintenanceRatio = preventiveRatio,
                ReactiveMaintenanceRatio = reactiveRatio,
                PotentialSavingsFromPreventive = potentialSavings,
                HighCostMaintenanceTypes = highCostTypes,
                HasMaintenanceSchedule = preventiveRatio > 50,
                OptimizationOpportunities = new List<string>
                {
                    "Increase preventive maintenance ratio to reduce costs",
                    "Establish regular maintenance schedule",
                    "Monitor high-cost maintenance types closely"
                }
            };
        }

        #endregion
    }
}

