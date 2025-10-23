using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UsageAnalyticsDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    public class UsageAnalyticsService : IUsageAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsageAnalyticsService> _logger;

        public UsageAnalyticsService(
            IUnitOfWork unitOfWork,
            ILogger<UsageAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<UsageVsOwnershipResponse>> GetUsageVsOwnershipAsync(
            int vehicleId,
            int userId,
            GetUsageVsOwnershipRequest? request = null)
        {
            try
            {
                // Validate user is authorized (must be co-owner of vehicle)
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<UsageVsOwnershipResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS",
                        Data = null
                    };
                }

                request ??= new GetUsageVsOwnershipRequest();

                // Get vehicle with co-owners
                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .Include(v => v.VehicleCoOwners)
                        .ThenInclude(vco => vco.CoOwner)
                            .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<UsageVsOwnershipResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Set date range
                var startDate = request.StartDate ?? vehicle.CreatedAt ?? DateTime.UtcNow.AddYears(-1);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Get all bookings in date range
                var bookings = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CoOwner)
                    .Include(b => b.CheckIns)
                        .ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts)
                        .ThenInclude(co => co.VehicleCondition)
                    .Where(b => b.VehicleId == vehicleId &&
                                b.StartTime >= startDate &&
                                b.StartTime <= endDate)
                    .ToListAsync();

                // Calculate usage data for each co-owner
                var coOwnersData = new List<CoOwnerUsageVsOwnership>();
                decimal totalUsageValue = 0;

                foreach (var vco in vehicle.VehicleCoOwners)
                {
                    var coOwnerBookings = bookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();

                    decimal usageValue = CalculateUsageValue(coOwnerBookings, request.UsageMetric);
                    totalUsageValue += usageValue;

                    var completedBookings = coOwnerBookings.Count(b => b.StatusEnum == EBookingStatus.Completed);

                    coOwnersData.Add(new CoOwnerUsageVsOwnership
                    {
                        CoOwnerId = vco.CoOwnerId,
                        UserId = vco.CoOwner.UserId,
                        CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}",
                        Email = vco.CoOwner.User.Email,
                        OwnershipPercentage = vco.OwnershipPercentage,
                        InvestmentAmount = vco.InvestmentAmount,
                        ActualUsageValue = usageValue,
                        TotalBookings = coOwnerBookings.Count,
                        CompletedBookings = completedBookings,
                        UsagePercentage = 0, // Will calculate after we know total
                        UsageVsOwnershipDelta = 0,
                        UsagePattern = string.Empty,
                        FairUsageValue = 0
                    });
                }

                // Calculate percentages and patterns
                foreach (var data in coOwnersData)
                {
                    data.UsagePercentage = totalUsageValue > 0
                        ? Math.Round((data.ActualUsageValue / totalUsageValue) * 100, 2)
                        : 0;

                    data.FairUsageValue = totalUsageValue * (data.OwnershipPercentage / 100);
                    data.UsageVsOwnershipDelta = Math.Round(data.UsagePercentage - data.OwnershipPercentage, 2);

                    // Classify usage pattern
                    var delta = Math.Abs(data.UsageVsOwnershipDelta);
                    if (delta <= 5) // Within 5% tolerance
                        data.UsagePattern = "Balanced";
                    else if (data.UsageVsOwnershipDelta > 5)
                        data.UsagePattern = "Overutilized";
                    else
                        data.UsagePattern = "Underutilized";
                }

                // Calculate summary statistics
                var summary = new UsageOwnershipSummary
                {
                    TotalUsageValue = totalUsageValue,
                    AverageOwnershipPercentage = coOwnersData.Average(c => c.OwnershipPercentage),
                    AverageUsagePercentage = coOwnersData.Average(c => c.UsagePercentage),
                    UsageVariance = CalculateVariance(coOwnersData.Select(c => c.UsageVsOwnershipDelta).ToList()),
                    TotalBookings = bookings.Count,
                    CompletedBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Completed),
                    MostActiveCoOwner = coOwnersData.OrderByDescending(c => c.UsagePercentage).FirstOrDefault(),
                    LeastActiveCoOwner = coOwnersData.OrderBy(c => c.UsagePercentage).FirstOrDefault(),
                    BalancedCoOwnersCount = coOwnersData.Count(c => c.UsagePattern == "Balanced"),
                    OverutilizedCoOwnersCount = coOwnersData.Count(c => c.UsagePattern == "Overutilized"),
                    UnderutilizedCoOwnersCount = coOwnersData.Count(c => c.UsagePattern == "Underutilized")
                };

                var response = new UsageVsOwnershipResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    AnalysisStartDate = startDate,
                    AnalysisEndDate = endDate,
                    UsageMetric = request.UsageMetric,
                    CoOwnersData = coOwnersData,
                    Summary = summary,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<UsageVsOwnershipResponse>
                {
                    StatusCode = 200,
                    Message = "USAGE_VS_OWNERSHIP_DATA_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving usage vs ownership data for vehicle {vehicleId}");
                return new BaseResponse<UsageVsOwnershipResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<UsageVsOwnershipTrendsResponse>> GetUsageVsOwnershipTrendsAsync(
            int vehicleId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string granularity = "Monthly")
        {
            try
            {
                // Validate user is authorized
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<UsageVsOwnershipTrendsResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .Include(v => v.VehicleCoOwners)
                        .ThenInclude(vco => vco.CoOwner)
                            .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<UsageVsOwnershipTrendsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                startDate ??= vehicle.CreatedAt ?? DateTime.UtcNow.AddYears(-1);
                endDate ??= DateTime.UtcNow;

                // Get all bookings in date range
                var bookings = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CoOwner)
                    .Include(b => b.CheckIns).ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts).ThenInclude(co => co.VehicleCondition)
                    .Where(b => b.VehicleId == vehicleId &&
                                b.StartTime >= startDate &&
                                b.StartTime <= endDate)
                    .ToListAsync();

                // Generate time periods
                var trendData = GenerateTrendDataPoints(
                    bookings,
                    vehicle.VehicleCoOwners.ToList(),
                    startDate.Value,
                    endDate.Value,
                    granularity);

                var response = new UsageVsOwnershipTrendsResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    AnalysisStartDate = startDate.Value,
                    AnalysisEndDate = endDate.Value,
                    Granularity = granularity,
                    TrendData = trendData,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<UsageVsOwnershipTrendsResponse>
                {
                    StatusCode = 200,
                    Message = "USAGE_VS_OWNERSHIP_TRENDS_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving usage vs ownership trends for vehicle {vehicleId}");
                return new BaseResponse<UsageVsOwnershipTrendsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<CoOwnerUsageDetailResponse>> GetCoOwnerUsageDetailAsync(
            int vehicleId,
            int coOwnerId,
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Validate user is authorized (must be the co-owner or another co-owner of same vehicle)
                var userCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .Include(vco => vco.CoOwner)
                    .FirstOrDefaultAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (userCoOwner == null)
                {
                    return new BaseResponse<CoOwnerUsageDetailResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_ANALYTICS",
                        Data = null
                    };
                }

                var targetCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .Include(vco => vco.CoOwner)
                        .ThenInclude(co => co.User)
                    .Include(vco => vco.Vehicle)
                    .FirstOrDefaultAsync(vco => vco.VehicleId == vehicleId && vco.CoOwnerId == coOwnerId);

                if (targetCoOwner == null)
                {
                    return new BaseResponse<CoOwnerUsageDetailResponse>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND",
                        Data = null
                    };
                }

                startDate ??= targetCoOwner.Vehicle.CreatedAt ?? DateTime.UtcNow.AddYears(-1);
                endDate ??= DateTime.UtcNow;

                // Get all bookings for the vehicle
                var allVehicleBookings = await _unitOfWork.DbContext.Set<Booking>()
                    .Include(b => b.CheckIns).ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts).ThenInclude(co => co.VehicleCondition)
                    .Where(b => b.VehicleId == vehicleId &&
                                b.StartTime >= startDate &&
                                b.StartTime <= endDate)
                    .ToListAsync();

                var coOwnerBookings = allVehicleBookings
                    .Where(b => b.CoOwnerId == coOwnerId)
                    .OrderByDescending(b => b.StartTime)
                    .ToList();

                // Calculate metrics
                var totalHours = CalculateUsageValue(allVehicleBookings, "Hours");
                var coOwnerHours = CalculateUsageValue(coOwnerBookings, "Hours");
                var totalDistance = CalculateUsageValue(allVehicleBookings, "Distance");
                var coOwnerDistance = CalculateUsageValue(coOwnerBookings, "Distance");
                var totalBookings = allVehicleBookings.Count;
                var coOwnerBookingsCount = coOwnerBookings.Count;

                var usageMetrics = new UsageMetricsBreakdown
                {
                    TotalHours = coOwnerHours,
                    HoursPercentage = totalHours > 0 ? Math.Round((coOwnerHours / totalHours) * 100, 2) : 0,
                    TotalDistance = coOwnerDistance,
                    DistancePercentage = totalDistance > 0 ? Math.Round((coOwnerDistance / totalDistance) * 100, 2) : 0,
                    TotalBookings = coOwnerBookingsCount,
                    BookingsPercentage = totalBookings > 0 ? Math.Round((decimal)coOwnerBookingsCount / totalBookings * 100, 2) : 0,
                    CompletedBookings = coOwnerBookings.Count(b => b.StatusEnum == EBookingStatus.Completed),
                    CancelledBookings = coOwnerBookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled),
                    AverageBookingDuration = coOwnerBookings.Any()
                        ? Math.Round(coOwnerBookings.Average(b => (decimal)(b.EndTime - b.StartTime).TotalHours), 2)
                        : 0
                };

                var recentBookings = coOwnerBookings.Take(10).Select(b => new BookingUsageSummary
                {
                    BookingId = b.Id,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    DurationHours = Math.Round((decimal)(b.EndTime - b.StartTime).TotalHours, 2),
                    DistanceTravelled = CalculateBookingDistance(b),
                    Status = b.StatusEnum?.ToString() ?? "Unknown",
                    Purpose = b.Purpose ?? string.Empty
                }).ToList();

                // Use hours as primary metric for overall usage percentage
                var usagePercentage = usageMetrics.HoursPercentage;
                var ownershipPercentage = targetCoOwner.OwnershipPercentage;

                var response = new CoOwnerUsageDetailResponse
                {
                    CoOwnerId = targetCoOwner.CoOwnerId,
                    UserId = targetCoOwner.CoOwner.UserId,
                    CoOwnerName = $"{targetCoOwner.CoOwner.User.FirstName} {targetCoOwner.CoOwner.User.LastName}",
                    Email = targetCoOwner.CoOwner.User.Email,
                    VehicleId = targetCoOwner.VehicleId,
                    VehicleName = targetCoOwner.Vehicle.Name,
                    OwnershipPercentage = ownershipPercentage,
                    UsagePercentage = usagePercentage,
                    UsageVsOwnershipDelta = Math.Round(usagePercentage - ownershipPercentage, 2),
                    UsageMetrics = usageMetrics,
                    RecentBookings = recentBookings,
                    AnalysisStartDate = startDate.Value,
                    AnalysisEndDate = endDate.Value,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<CoOwnerUsageDetailResponse>
                {
                    StatusCode = 200,
                    Message = "CO_OWNER_USAGE_DETAIL_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving co-owner usage detail for vehicle {vehicleId}, co-owner {coOwnerId}");
                return new BaseResponse<CoOwnerUsageDetailResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #region Helper Methods

        private decimal CalculateUsageValue(List<Booking> bookings, string metric)
        {
            return metric switch
            {
                "Hours" => (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours),
                "Distance" => bookings.Sum(b => (decimal)(CalculateBookingDistance(b) ?? 0)),
                "BookingCount" => bookings.Count,
                _ => (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours)
            };
        }

        private int? CalculateBookingDistance(Booking booking)
        {
            var checkIn = booking.CheckIns.FirstOrDefault();
            var checkOut = booking.CheckOuts.FirstOrDefault();

            if (checkIn?.VehicleCondition?.OdometerReading != null &&
                checkOut?.VehicleCondition?.OdometerReading != null)
            {
                return checkOut.VehicleCondition.OdometerReading - checkIn.VehicleCondition.OdometerReading;
            }

            return null;
        }

        private decimal CalculateVariance(List<decimal> values)
        {
            if (!values.Any()) return 0;

            var mean = values.Average();
            var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
            return Math.Round((decimal)Math.Sqrt((double)(sumOfSquares / values.Count)), 2);
        }

        private List<TrendDataPoint> GenerateTrendDataPoints(
            List<Booking> bookings,
            List<VehicleCoOwner> coOwners,
            DateTime startDate,
            DateTime endDate,
            string granularity)
        {
            var trendData = new List<TrendDataPoint>();
            var periods = GeneratePeriods(startDate, endDate, granularity);

            foreach (var (periodStart, periodEnd, label) in periods)
            {
                var periodBookings = bookings.Where(b =>
                    b.StartTime >= periodStart &&
                    b.StartTime < periodEnd).ToList();

                var totalUsage = (decimal)periodBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);

                var coOwnersTrendData = new List<CoOwnerTrendData>();

                foreach (var vco in coOwners)
                {
                    var coOwnerBookings = periodBookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();
                    var coOwnerUsage = (decimal)coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    var usagePercentage = totalUsage > 0
                        ? Math.Round((coOwnerUsage / totalUsage) * 100, 2)
                        : 0;

                    coOwnersTrendData.Add(new CoOwnerTrendData
                    {
                        CoOwnerId = vco.CoOwnerId,
                        CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}",
                        OwnershipPercentage = vco.OwnershipPercentage,
                        UsagePercentage = usagePercentage,
                        UsageValue = coOwnerUsage
                    });
                }

                trendData.Add(new TrendDataPoint
                {
                    Date = periodStart,
                    Period = label,
                    CoOwnersData = coOwnersTrendData
                });
            }

            return trendData;
        }

        private List<(DateTime Start, DateTime End, string Label)> GeneratePeriods(
            DateTime startDate,
            DateTime endDate,
            string granularity)
        {
            var periods = new List<(DateTime, DateTime, string)>();

            switch (granularity.ToLower())
            {
                case "daily":
                    var currentDay = startDate.Date;
                    while (currentDay <= endDate)
                    {
                        periods.Add((currentDay, currentDay.AddDays(1), currentDay.ToString("MMM dd, yyyy")));
                        currentDay = currentDay.AddDays(1);
                    }
                    break;

                case "weekly":
                    var currentWeek = startDate.Date;
                    int weekNumber = 1;
                    while (currentWeek <= endDate)
                    {
                        var weekEnd = currentWeek.AddDays(7);
                        periods.Add((currentWeek, weekEnd, $"Week {weekNumber} ({currentWeek:MMM dd})"));
                        currentWeek = weekEnd;
                        weekNumber++;
                    }
                    break;

                case "monthly":
                default:
                    var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
                    while (currentMonth <= endDate)
                    {
                        var monthEnd = currentMonth.AddMonths(1);
                        periods.Add((currentMonth, monthEnd, currentMonth.ToString("MMM yyyy")));
                        currentMonth = monthEnd;
                    }
                    break;
            }

            return periods;
        }

        #endregion
    }
}
