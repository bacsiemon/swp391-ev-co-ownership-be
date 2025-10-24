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

        #region Personal Usage History & Group Usage Summary

        public async Task<BaseResponse<PersonalUsageHistoryResponse>> GetPersonalUsageHistoryAsync(
            int userId,
            GetPersonalUsageHistoryRequest request)
        {
            try
            {
                _logger.LogInformation("Getting personal usage history for user {UserId}", userId);

                // Set default date range
                var startDate = request.StartDate ?? DateTime.UtcNow.AddYears(-1);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Get all co-owners for this user
                var userCoOwners = await _unitOfWork.CoOwnerRepository
                    .GetQueryable()
                    .Where(co => co.UserId == userId)
                    .Include(co => co.VehicleCoOwners)
                    .ToListAsync();

                if (!userCoOwners.Any())
                {
                    return new BaseResponse<PersonalUsageHistoryResponse>
                    {
                        StatusCode = 404,
                        Message = "User is not a co-owner of any vehicle",
                        Data = null
                    };
                }

                var coOwnerIds = userCoOwners.Select(co => co.UserId).ToList();

                // Get all bookings query
                var bookingsQuery = _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Where(b => b.CoOwnerId.HasValue && coOwnerIds.Contains(b.CoOwnerId.Value) &&
                               b.StartTime >= startDate &&
                               b.StartTime <= endDate);

                // Apply vehicle filter if specified
                if (request.VehicleId.HasValue)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.VehicleId == request.VehicleId.Value);
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(request.Status) && request.Status != "All")
                {
                    var statusEnum = request.Status.ToLower() switch
                    {
                        "completed" => EBookingStatus.Completed,
                        "cancelled" => EBookingStatus.Cancelled,
                        "pending" => EBookingStatus.Pending,
                        _ => EBookingStatus.Completed
                    };
                    bookingsQuery = bookingsQuery.Where(b => b.StatusEnum == statusEnum);
                }

                // Include related entities
                bookingsQuery = bookingsQuery
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns).ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts).ThenInclude(co => co.VehicleCondition)
                    .Include(b => b.MaintenanceCosts);

                // Get total count for pagination
                var totalBookings = await bookingsQuery.CountAsync();

                // Apply sorting (simplified - distance sorting requires complex logic)
                bookingsQuery = request.SortBy.ToLower() switch
                {
                    "endtime" => request.SortOrder.ToLower() == "asc"
                        ? bookingsQuery.OrderBy(b => b.EndTime)
                        : bookingsQuery.OrderByDescending(b => b.EndTime),
                    "durationhours" => request.SortOrder.ToLower() == "asc"
                        ? bookingsQuery.OrderBy(b => (b.EndTime - b.StartTime).TotalHours)
                        : bookingsQuery.OrderByDescending(b => (b.EndTime - b.StartTime).TotalHours),
                    _ => request.SortOrder.ToLower() == "asc"
                        ? bookingsQuery.OrderBy(b => b.StartTime)
                        : bookingsQuery.OrderByDescending(b => b.StartTime)
                };

                // Apply pagination
                var skip = (request.PageNumber - 1) * request.PageSize;
                var pagedBookings = await bookingsQuery
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Get all bookings for statistics (without pagination)
                var allBookings = await _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Where(b => b.CoOwnerId.HasValue && coOwnerIds.Contains(b.CoOwnerId.Value) &&
                                b.StartTime >= startDate &&
                                b.StartTime <= endDate)
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns).ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts).ThenInclude(co => co.VehicleCondition)
                    .ToListAsync();

                // Get user details
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<PersonalUsageHistoryResponse>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = null
                    };
                }

                // Calculate summary statistics
                var summary = CalculatePersonalSummary(allBookings, userCoOwners, userId);

                // Build paginated booking history
                var bookingHistory = pagedBookings.Select(b => new PersonalBookingHistory
                {
                    BookingId = b.Id,
                    VehicleId = b.VehicleId ?? 0,
                    VehicleName = $"{b.Vehicle.Brand} {b.Vehicle.Model}",
                    VehicleLicensePlate = b.Vehicle.LicensePlate,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    DurationHours = (decimal)(b.EndTime - b.StartTime).TotalHours,
                    DistanceTraveled = CalculateBookingDistance(b),
                    FuelLevelStart = b.CheckIns.FirstOrDefault()?.VehicleCondition?.FuelLevel,
                    FuelLevelEnd = b.CheckOuts.FirstOrDefault()?.VehicleCondition?.FuelLevel,
                    Status = b.StatusEnum?.ToString() ?? "Unknown",
                    Purpose = b.Purpose ?? string.Empty,
                    TotalCost = b.TotalCost,
                    HasCheckIn = b.CheckIns.Any(),
                    HasCheckOut = b.CheckOuts.Any(),
                    CheckInTime = b.CheckIns.FirstOrDefault()?.CheckTime,
                    CheckOutTime = b.CheckOuts.FirstOrDefault()?.CheckTime,
                    HasDamageReport = false, // TODO: Add damage report relation if exists
                    HasMaintenanceIssue = b.MaintenanceCosts.Any()
                }).ToList();

                // Build vehicle breakdown
                var vehicleBreakdown = BuildVehicleBreakdown(allBookings, userCoOwners);

                // Build period statistics
                var periodStats = BuildPeriodStatistics(allBookings, startDate, endDate);

                var response = new PersonalUsageHistoryResponse
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Summary = summary,
                    Bookings = bookingHistory,
                    Pagination = new UsageAnalyticsPaginationInfo
                    {
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling((double)totalBookings / request.PageSize),
                        TotalItems = totalBookings,
                        HasPreviousPage = request.PageNumber > 1,
                        HasNextPage = request.PageNumber < (int)Math.Ceiling((double)totalBookings / request.PageSize)
                    },
                    VehicleBreakdown = vehicleBreakdown,
                    PeriodStatistics = periodStats,
                    AnalysisStartDate = startDate,
                    AnalysisEndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<PersonalUsageHistoryResponse>
                {
                    StatusCode = 200,
                    Message = "Personal usage history retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal usage history for user {UserId}", userId);
                return new BaseResponse<PersonalUsageHistoryResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving personal usage history",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<GroupUsageSummaryResponse>> GetGroupUsageSummaryAsync(
            int userId,
            GetGroupUsageSummaryRequest request)
        {
            try
            {
                _logger.LogInformation("Getting group usage summary for vehicle {VehicleId}", request.VehicleId);

                // Verify user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<GroupUsageSummaryResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. User is not a co-owner of this vehicle.",
                        Data = null
                    };
                }

                // Get vehicle
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<GroupUsageSummaryResponse>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Set date range
                var startDate = request.StartDate ?? vehicle.CreatedAt;
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Get all co-owners
                var coOwners = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .Where(vco => vco.VehicleId == request.VehicleId)
                    .Include(vco => vco.CoOwner).ThenInclude(co => co.User)
                    .ToListAsync();

                // Get all bookings
                var bookings = await _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Where(b => b.VehicleId == request.VehicleId &&
                                b.StartTime >= startDate &&
                                b.StartTime <= endDate)
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .Include(b => b.CheckIns).ThenInclude(ci => ci.VehicleCondition)
                    .Include(b => b.CheckOuts).ThenInclude(co => co.VehicleCondition)
                    .ToListAsync();

                // Get shared fund
                var sharedFund = await _unitOfWork.VehicleRepository
                    .GetQueryable()
                    .Where(v => v.Id == request.VehicleId)
                    .Select(v => v.Fund)
                    .FirstOrDefaultAsync();

                // Calculate group statistics
                var groupStats = CalculateGroupStatistics(bookings, coOwners, sharedFund, startDate ?? DateTime.UtcNow, endDate);

                // Calculate co-owner breakdown
                var coOwnerUsage = CalculateCoOwnerGroupUsage(bookings, coOwners, sharedFund);

                // Calculate usage distribution
                var distribution = CalculateUsageDistribution(bookings, coOwners, coOwnerUsage);

                // Calculate period breakdown
                var periodBreakdown = request.IncludeTimeBreakdown
                    ? CalculateGroupPeriodBreakdown(bookings, coOwners, startDate ?? DateTime.UtcNow, endDate, request.Granularity)
                    : new List<GroupPeriodUsage>();

                // Calculate popular time slots
                var popularTimeSlots = CalculatePopularTimeSlots(bookings);

                // Calculate vehicle utilization
                var utilization = CalculateVehicleUtilization(bookings, startDate ?? DateTime.UtcNow, endDate);

                var response = new GroupUsageSummaryResponse
                {
                    VehicleId = request.VehicleId,
                    VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                    LicensePlate = vehicle.LicensePlate,
                    GroupStats = groupStats,
                    CoOwners = coOwnerUsage,
                    Distribution = distribution,
                    PeriodBreakdown = periodBreakdown,
                    PopularTimeSlots = popularTimeSlots,
                    Utilization = utilization,
                    AnalysisStartDate = startDate ?? DateTime.UtcNow,
                    AnalysisEndDate = endDate,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<GroupUsageSummaryResponse>
                {
                    StatusCode = 200,
                    Message = "Group usage summary retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group usage summary for vehicle {VehicleId}", request.VehicleId);
                return new BaseResponse<GroupUsageSummaryResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving group usage summary",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Helper Methods for Personal Usage & Group Summary

        private PersonalUsageSummary CalculatePersonalSummary(
            List<Booking> bookings,
            List<CoOwner> userCoOwners,
            int userId)
        {
            var vehicleIds = bookings.Select(b => b.VehicleId).Distinct().ToList();
            var completedBookings = bookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
            var totalHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalDistance = completedBookings.Sum(b => CalculateBookingDistance(b) ?? 0);

            // Group by day of week
            var bookingsByDay = bookings.GroupBy(b => b.StartTime.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            // Group by time slot
            var timeSlots = bookings.GroupBy(b => GetTimeSlot(b.StartTime.Hour))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            // Favorite vehicle
            var favoriteVehicle = bookings.GroupBy(b => b.VehicleId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var totalInvestment = userCoOwners.Sum(co => co.VehicleCoOwners.Sum(vco => vco.InvestmentAmount));

            return new PersonalUsageSummary
            {
                TotalVehicles = vehicleIds.Count,
                TotalBookings = bookings.Count,
                CompletedBookings = completedBookings.Count,
                CancelledBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled),
                PendingBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Pending),
                TotalHoursUsed = Math.Round(totalHours, 2),
                TotalDistanceTraveled = totalDistance,
                AverageBookingDuration = completedBookings.Any()
                    ? Math.Round(totalHours / completedBookings.Count, 2)
                    : 0,
                AverageTripDistance = completedBookings.Any() && totalDistance > 0
                    ? Math.Round((decimal)totalDistance / completedBookings.Count, 2)
                    : 0,
                TotalCostPaid = 0, // TODO: Calculate from payments
                TotalInvestment = totalInvestment,
                MostActiveDay = bookingsByDay?.Key.ToString() ?? "N/A",
                MostActiveTimeSlot = timeSlots?.Key ?? "N/A",
                FavoriteVehicleId = favoriteVehicle?.FirstOrDefault()?.VehicleId,
                FavoriteVehicleName = favoriteVehicle?.FirstOrDefault()?.Vehicle != null
                    ? $"{favoriteVehicle.First().Vehicle.Brand} {favoriteVehicle.First().Vehicle.Model}"
                    : string.Empty,
                FavoriteVehicleBookingCount = favoriteVehicle?.Count() ?? 0
            };
        }

        private List<VehicleUsageSummary> BuildVehicleBreakdown(
            List<Booking> bookings,
            List<CoOwner> userCoOwners)
        {
            var vehicleBreakdown = new List<VehicleUsageSummary>();
            var vehicleGroups = bookings.GroupBy(b => b.VehicleId);

            foreach (var group in vehicleGroups)
            {
                var vehicleBookings = group.ToList();
                var completedBookings = vehicleBookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
                var vehicle = vehicleBookings.First().Vehicle;

                var coOwnership = userCoOwners
                    .SelectMany(co => co.VehicleCoOwners)
                    .FirstOrDefault(vco => vco.VehicleId == group.Key);

                var totalHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var totalDistance = completedBookings.Sum(b => CalculateBookingDistance(b) ?? 0);

                // Calculate usage percentage (simplified - would need all vehicle bookings for accuracy)
                var ownershipPct = coOwnership?.OwnershipPercentage ?? 0;

                vehicleBreakdown.Add(new VehicleUsageSummary
                {
                    VehicleId = group.Key ?? 0,
                    VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                    LicensePlate = vehicle.LicensePlate,
                    OwnershipPercentage = ownershipPct,
                    InvestmentAmount = coOwnership?.InvestmentAmount ?? 0,
                    TotalBookings = vehicleBookings.Count,
                    CompletedBookings = completedBookings.Count,
                    TotalHours = Math.Round(totalHours, 2),
                    TotalDistance = totalDistance,
                    TotalCost = 0, // TODO: Calculate from payments
                    UsagePercentage = 0, // Would need all vehicle bookings
                    UsageVsOwnershipDelta = 0,
                    UsagePattern = "N/A",
                    FirstBooking = vehicleBookings.Min(b => b.StartTime),
                    LastBooking = vehicleBookings.Max(b => b.StartTime)
                });
            }

            return vehicleBreakdown;
        }

        private List<UsagePeriodStatistics> BuildPeriodStatistics(
            List<Booking> bookings,
            DateTime startDate,
            DateTime endDate)
        {
            var periods = GeneratePeriods(startDate, endDate, "Monthly");
            var stats = new List<UsagePeriodStatistics>();

            foreach (var (periodStart, periodEnd, label) in periods)
            {
                var periodBookings = bookings.Where(b =>
                    b.StartTime >= periodStart && b.StartTime < periodEnd).ToList();

                var completedBookings = periodBookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
                var totalHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var totalDistance = completedBookings.Sum(b => CalculateBookingDistance(b) ?? 0);

                var dayOfWeekDist = periodBookings
                    .GroupBy(b => b.StartTime.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.Add(new UsagePeriodStatistics
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    PeriodLabel = label,
                    BookingCount = periodBookings.Count,
                    TotalHours = Math.Round(totalHours, 2),
                    TotalDistance = totalDistance,
                    AverageDuration = completedBookings.Any()
                        ? Math.Round(totalHours / completedBookings.Count, 2)
                        : 0,
                    BookingsByDayOfWeek = dayOfWeekDist
                });
            }

            return stats;
        }

        private GroupStatistics CalculateGroupStatistics(
            List<Booking> bookings,
            List<VehicleCoOwner> coOwners,
            Fund? sharedFund,
            DateTime startDate,
            DateTime endDate)
        {
            var completedBookings = bookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
            var totalHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalDistance = completedBookings.Sum(b => CalculateBookingDistance(b) ?? 0);

            var activeCoOwnerIds = bookings.Select(b => b.CoOwnerId).Distinct().Count();

            // Calculate utilization rate
            var totalDays = (endDate - startDate).TotalDays;
            var totalAvailableHours = (decimal)(totalDays * 24);
            var utilizationRate = totalAvailableHours > 0
                ? Math.Round((totalHours / totalAvailableHours) * 100, 2)
                : 0;

            // Calculate fairness score (0-100, based on variance of usage percentages)
            var usagePercentages = coOwners.Select(vco =>
            {
                var coOwnerBookings = completedBookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();
                var coOwnerHours = (decimal)coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                return totalHours > 0 ? (coOwnerHours / totalHours) * 100 : 0;
            }).ToList();

            var variance = CalculateVariance(usagePercentages);
            var fairnessScore = Math.Max(0, 100 - (variance * 10)); // Simple fairness calculation

            return new GroupStatistics
            {
                TotalCoOwners = coOwners.Count,
                TotalBookings = bookings.Count,
                CompletedBookings = completedBookings.Count,
                CancelledBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled),
                ActiveCoOwners = activeCoOwnerIds,
                TotalHoursUsed = Math.Round(totalHours, 2),
                TotalDistanceTraveled = totalDistance,
                AverageHoursPerBooking = completedBookings.Any()
                    ? Math.Round(totalHours / completedBookings.Count, 2)
                    : 0,
                AverageDistancePerTrip = completedBookings.Any() && totalDistance > 0
                    ? Math.Round((decimal)totalDistance / completedBookings.Count, 2)
                    : 0,
                TotalFundBalance = sharedFund?.CurrentBalance ?? 0,
                TotalIncome = 0, // TODO: Calculate from fund additions
                TotalExpenses = 0, // TODO: Calculate from payments
                UtilizationRate = utilizationRate,
                AverageBookingsPerCoOwner = coOwners.Any()
                    ? Math.Round((decimal)bookings.Count / coOwners.Count, 2)
                    : 0,
                FairnessScore = Math.Round(fairnessScore, 2)
            };
        }

        private List<CoOwnerGroupUsage> CalculateCoOwnerGroupUsage(
            List<Booking> bookings,
            List<VehicleCoOwner> coOwners,
            Fund? sharedFund)
        {
            var completedBookings = bookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
            var totalHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalDistance = completedBookings.Sum(b => CalculateBookingDistance(b) ?? 0);
            var totalBookings = completedBookings.Count;

            var coOwnerUsage = new List<CoOwnerGroupUsage>();

            foreach (var vco in coOwners)
            {
                var coOwnerBookings = completedBookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();
                var coOwnerHours = (decimal)coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var coOwnerDistance = coOwnerBookings.Sum(b => CalculateBookingDistance(b) ?? 0);
                var coOwnerBookingCount = coOwnerBookings.Count;

                var bookingPct = totalBookings > 0
                    ? Math.Round(((decimal)coOwnerBookingCount / totalBookings) * 100, 2)
                    : 0;
                var hoursPct = totalHours > 0
                    ? Math.Round((coOwnerHours / totalHours) * 100, 2)
                    : 0;
                var distancePct = totalDistance > 0
                    ? Math.Round(((decimal)coOwnerDistance / totalDistance) * 100, 2)
                    : 0;

                var usageVsOwnershipDelta = hoursPct - vco.OwnershipPercentage;

                // Classify usage pattern (inline from existing pattern)
                var delta = Math.Abs(usageVsOwnershipDelta);
                var usagePattern = delta <= 5 ? "Balanced"
                    : usageVsOwnershipDelta > 5 ? "Overutilized"
                    : "Underutilized";

                var lastBooking = bookings
                    .Where(b => b.CoOwnerId == vco.CoOwnerId)
                    .OrderByDescending(b => b.StartTime)
                    .FirstOrDefault();

                coOwnerUsage.Add(new CoOwnerGroupUsage
                {
                    CoOwnerId = vco.CoOwnerId,
                    UserId = vco.CoOwner.UserId,
                    CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}",
                    Email = vco.CoOwner.User.Email,
                    OwnershipPercentage = vco.OwnershipPercentage,
                    InvestmentAmount = vco.InvestmentAmount,
                    BookingCount = coOwnerBookingCount,
                    BookingPercentage = bookingPct,
                    TotalHours = Math.Round(coOwnerHours, 2),
                    HoursPercentage = hoursPct,
                    TotalDistance = coOwnerDistance,
                    DistancePercentage = distancePct,
                    UsageVsOwnershipDelta = Math.Round(usageVsOwnershipDelta, 2),
                    UsagePattern = usagePattern,
                    ContributionToFund = 0, // TODO: Calculate from fund additions
                    ContributionPercentage = 0,
                    IsActive = coOwnerBookingCount > 0,
                    LastBookingDate = lastBooking?.StartTime
                });
            }

            return coOwnerUsage;
        }

        private UsageDistributionAnalysis CalculateUsageDistribution(
            List<Booking> bookings,
            List<VehicleCoOwner> coOwners,
            List<CoOwnerGroupUsage> coOwnerUsage)
        {
            var usagePercentages = coOwnerUsage.Select(cu => cu.HoursPercentage).ToList();
            var variance = CalculateVariance(usagePercentages);

            var pattern = variance switch
            {
                < 5 => "Equal",
                > 20 => "Dominated",
                _ => "Varied"
            };

            var mostActive = coOwnerUsage.OrderByDescending(cu => cu.TotalHours).FirstOrDefault();
            var leastActive = coOwnerUsage.OrderBy(cu => cu.TotalHours).FirstOrDefault();

            // Distribution by day of week
            var completedBookings = bookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
            var hoursByDay = completedBookings
                .GroupBy(b => b.StartTime.DayOfWeek.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round((decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours), 2));

            // Distribution by time of day
            var bookingsByTime = bookings
                .GroupBy(b => GetTimeSlot(b.StartTime.Hour))
                .ToDictionary(g => g.Key, g => g.Count());

            // Distribution by purpose
            var bookingsByPurpose = bookings
                .Where(b => !string.IsNullOrEmpty(b.Purpose))
                .GroupBy(b => b.Purpose!)
                .ToDictionary(g => g.Key, g => g.Count());

            return new UsageDistributionAnalysis
            {
                DistributionVariance = Math.Round(variance, 2),
                DistributionPattern = pattern,
                MostActiveCoOwner = mostActive,
                LeastActiveCoOwner = leastActive,
                HoursByDayOfWeek = hoursByDay,
                BookingsByTimeOfDay = bookingsByTime,
                BookingsByPurpose = bookingsByPurpose
            };
        }

        private List<GroupPeriodUsage> CalculateGroupPeriodBreakdown(
            List<Booking> bookings,
            List<VehicleCoOwner> coOwners,
            DateTime startDate,
            DateTime endDate,
            string granularity)
        {
            var periods = GeneratePeriods(startDate, endDate, granularity);
            var periodUsage = new List<GroupPeriodUsage>();

            foreach (var (periodStart, periodEnd, label) in periods)
            {
                var periodBookings = bookings.Where(b =>
                    b.StartTime >= periodStart &&
                    b.StartTime < periodEnd &&
                    b.StatusEnum == EBookingStatus.Completed).ToList();

                var totalHours = (decimal)periodBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var totalDistance = periodBookings.Sum(b => CalculateBookingDistance(b) ?? 0);
                var activeCoOwners = periodBookings.Select(b => b.CoOwnerId).Distinct().Count();

                var totalDays = (periodEnd - periodStart).TotalDays;
                var availableHours = (decimal)(totalDays * 24);
                var utilizationRate = availableHours > 0
                    ? Math.Round((totalHours / availableHours) * 100, 2)
                    : 0;

                var topCoOwner = periodBookings
                    .GroupBy(b => b.CoOwnerId)
                    .OrderByDescending(g => g.Sum(b => (b.EndTime - b.StartTime).TotalHours))
                    .FirstOrDefault();

                var topCoOwnerVco = topCoOwner != null
                    ? coOwners.FirstOrDefault(vco => vco.CoOwnerId == topCoOwner.Key)
                    : null;

                periodUsage.Add(new GroupPeriodUsage
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    PeriodLabel = label,
                    TotalBookings = periodBookings.Count,
                    TotalHours = Math.Round(totalHours, 2),
                    TotalDistance = totalDistance,
                    ActiveCoOwners = activeCoOwners,
                    UtilizationRate = utilizationRate,
                    TopCoOwnerName = topCoOwnerVco != null
                        ? $"{topCoOwnerVco.CoOwner.User.FirstName} {topCoOwnerVco.CoOwner.User.LastName}"
                        : "N/A",
                    TopCoOwnerUsageHours = topCoOwner != null
                        ? Math.Round((decimal)topCoOwner.Sum(b => (b.EndTime - b.StartTime).TotalHours), 2)
                        : 0
                });
            }

            return periodUsage;
        }

        private List<PopularTimeSlot> CalculatePopularTimeSlots(List<Booking> bookings)
        {
            var timeSlots = bookings
                .GroupBy(b => new
                {
                    DayOfWeek = b.StartTime.DayOfWeek.ToString(),
                    TimeSlot = GetTimeSlot(b.StartTime.Hour)
                })
                .Select(g => new PopularTimeSlot
                {
                    TimeSlot = $"{g.Key.DayOfWeek} {g.Key.TimeSlot}",
                    BookingCount = g.Count(),
                    PercentageOfTotal = bookings.Any()
                        ? Math.Round(((decimal)g.Count() / bookings.Count) * 100, 2)
                        : 0,
                    AverageDuration = Math.Round((decimal)g.Average(b => (b.EndTime - b.StartTime).TotalHours), 2)
                })
                .OrderByDescending(ts => ts.BookingCount)
                .Take(10)
                .ToList();

            return timeSlots;
        }

        private VehicleUtilizationMetrics CalculateVehicleUtilization(
            List<Booking> bookings,
            DateTime startDate,
            DateTime endDate)
        {
            var completedBookings = bookings.Where(b => b.StatusEnum == EBookingStatus.Completed).ToList();
            var totalDays = (endDate - startDate).TotalDays;
            var totalAvailableHours = (decimal)(totalDays * 24);
            var totalBookedHours = (decimal)completedBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);

            var utilizationPct = totalAvailableHours > 0
                ? Math.Round((totalBookedHours / totalAvailableHours) * 100, 2)
                : 0;

            var bookingsByDay = completedBookings
                .GroupBy(b => b.StartTime.Day)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var daysWithBookings = completedBookings
                .Select(b => b.StartTime.Date)
                .Distinct()
                .Count();

            var idleDays = (int)totalDays - daysWithBookings;
            var idlePercentage = totalDays > 0
                ? Math.Round((idleDays / (decimal)totalDays) * 100, 2)
                : 0;

            return new VehicleUtilizationMetrics
            {
                TotalAvailableHours = Math.Round(totalAvailableHours, 2),
                TotalBookedHours = Math.Round(totalBookedHours, 2),
                UtilizationPercentage = utilizationPct,
                AverageBookingsPerDay = totalDays > 0
                    ? Math.Round(completedBookings.Count / (decimal)totalDays, 2)
                    : 0,
                AverageBookingsPerWeek = totalDays > 0
                    ? Math.Round((completedBookings.Count / (decimal)totalDays) * 7, 2)
                    : 0,
                AverageBookingsPerMonth = totalDays > 0
                    ? Math.Round((completedBookings.Count / (decimal)totalDays) * 30, 2)
                    : 0,
                PeakUsageDay = bookingsByDay?.Key ?? 0,
                PeakUsageDayName = bookingsByDay != null && bookingsByDay.Any()
                    ? bookingsByDay.First().StartTime.ToString("MMMM dd")
                    : "N/A",
                IdleDays = idleDays,
                IdlePercentage = idlePercentage
            };
        }

        private string GetTimeSlot(int hour)
        {
            return hour switch
            {
                >= 6 and < 12 => "Morning",
                >= 12 and < 18 => "Afternoon",
                >= 18 and < 22 => "Evening",
                _ => "Night"
            };
        }

        #endregion

        #region Compare Usage Over Time

        public async Task<BaseResponse<CoOwnersUsageComparisonResponse>> CompareCoOwnersUsageAsync(
            int userId,
            CompareCoOwnersUsageRequest request)
        {
            try
            {
                _logger.LogInformation("Comparing co-owners usage for vehicle {VehicleId}", request.VehicleId);

                // Verify user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<CoOwnersUsageComparisonResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_CO_OWNER",
                        Data = null
                    };
                }

                // Get vehicle
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<CoOwnersUsageComparisonResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Set date range
                var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-3);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Get all co-owners (or filter by specific IDs)
                var coOwnersQuery = _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .Where(vco => vco.VehicleId == request.VehicleId);

                if (request.CoOwnerIds != null && request.CoOwnerIds.Any())
                {
                    coOwnersQuery = coOwnersQuery.Where(vco => request.CoOwnerIds.Contains(vco.CoOwnerId));
                }

                var coOwners = await coOwnersQuery
                    .Include(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                    .ToListAsync();

                if (!coOwners.Any())
                {
                    return new BaseResponse<CoOwnersUsageComparisonResponse>
                    {
                        StatusCode = 404,
                        Message = "NO_CO_OWNERS_FOUND",
                        Data = null
                    };
                }

                // Get all bookings for the period
                var bookings = await _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Where(b => b.VehicleId == request.VehicleId &&
                               b.StartTime >= startDate &&
                               b.StartTime <= endDate &&
                               b.StatusEnum == EBookingStatus.Completed)
                    .Include(b => b.CheckIns.OrderBy(ci => ci.CheckTime).Take(1))
                    .Include(b => b.CheckOuts.OrderByDescending(co => co.CheckTime).Take(1))
                    .ToListAsync();

                // Generate time periods
                var periods = GeneratePeriods(startDate, endDate, request.Granularity);

                // Build time-series for each co-owner
                var coOwnersSeries = new List<CoOwnerUsageTimeSeries>();

                foreach (var vco in coOwners)
                {
                    var coOwnerBookings = bookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();
                    var dataPoints = BuildUsageDataPoints(coOwnerBookings, periods);
                    var trendAnalysis = AnalyzeTrend(dataPoints);

                    var totalHours = (decimal)coOwnerBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    var totalDistance = coOwnerBookings.Sum(b => CalculateBookingDistance(b) ?? 0);
                    var peakDataPoint = dataPoints.OrderByDescending(dp => dp.Hours).FirstOrDefault();

                    coOwnersSeries.Add(new CoOwnerUsageTimeSeries
                    {
                        CoOwnerId = vco.CoOwnerId,
                        CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}",
                        Email = vco.CoOwner.User.Email,
                        OwnershipPercentage = vco.OwnershipPercentage,
                        DataPoints = dataPoints,
                        Trend = trendAnalysis,
                        TotalHours = Math.Round(totalHours, 2),
                        TotalDistance = totalDistance,
                        TotalBookings = coOwnerBookings.Count,
                        AveragePerPeriod = dataPoints.Any() ? Math.Round(totalHours / dataPoints.Count, 2) : 0,
                        PeakUsage = peakDataPoint?.Hours ?? 0,
                        PeakPeriod = peakDataPoint?.PeriodLabel ?? "N/A"
                    });
                }

                // Calculate statistics
                var statistics = CalculateComparisonStatistics(coOwnersSeries);

                // Generate rankings
                var rankings = GenerateRankings(coOwnersSeries);

                // Generate insights
                var insights = GenerateUsageInsights(coOwnersSeries, coOwners.ToList());

                var response = new CoOwnersUsageComparisonResponse
                {
                    VehicleId = request.VehicleId,
                    VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                    CoOwnersSeries = coOwnersSeries,
                    Statistics = statistics,
                    Rankings = rankings,
                    Insights = insights,
                    AnalysisStartDate = startDate,
                    AnalysisEndDate = endDate,
                    Granularity = request.Granularity,
                    TotalPeriods = periods.Count,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<CoOwnersUsageComparisonResponse>
                {
                    StatusCode = 200,
                    Message = "CO_OWNERS_USAGE_COMPARISON_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing co-owners usage for vehicle {VehicleId}", request.VehicleId);
                return new BaseResponse<CoOwnersUsageComparisonResponse>
                {
                    StatusCode = 500,
                    Message = "ERROR_COMPARING_CO_OWNERS_USAGE",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehiclesUsageComparisonResponse>> CompareVehiclesUsageAsync(
            int userId,
            CompareVehiclesUsageRequest request)
        {
            try
            {
                _logger.LogInformation("Comparing vehicles usage for user {UserId}", userId);

                // Verify user is co-owner of all vehicles
                foreach (var vehicleId in request.VehicleIds)
                {
                    var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                        .GetQueryable()
                        .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                        vco.CoOwner.UserId == userId);

                    if (!isCoOwner)
                    {
                        return new BaseResponse<VehiclesUsageComparisonResponse>
                        {
                            StatusCode = 403,
                            Message = $"ACCESS_DENIED_NOT_CO_OWNER_OF_VEHICLE_{vehicleId}",
                            Data = null
                        };
                    }
                }

                // Get vehicles
                var vehicles = await _unitOfWork.VehicleRepository
                    .GetQueryable()
                    .Where(v => request.VehicleIds.Contains(v.Id))
                    .ToListAsync();

                if (vehicles.Count != request.VehicleIds.Count)
                {
                    return new BaseResponse<VehiclesUsageComparisonResponse>
                    {
                        StatusCode = 404,
                        Message = "SOME_VEHICLES_NOT_FOUND",
                        Data = null
                    };
                }

                // Set date range
                var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-3);
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Generate time periods
                var periods = GeneratePeriods(startDate, endDate, request.Granularity);

                // Build time-series for each vehicle
                var vehiclesSeries = new List<VehicleUsageTimeSeries>();

                foreach (var vehicle in vehicles)
                {
                    var bookings = await _unitOfWork.BookingRepository
                        .GetQueryable()
                        .Where(b => b.VehicleId == vehicle.Id &&
                                   b.StartTime >= startDate &&
                                   b.StartTime <= endDate &&
                                   b.StatusEnum == EBookingStatus.Completed)
                        .Include(b => b.CheckIns.OrderBy(ci => ci.CheckTime).Take(1))
                        .Include(b => b.CheckOuts.OrderByDescending(co => co.CheckTime).Take(1))
                        .ToListAsync();

                    var dataPoints = BuildUsageDataPoints(bookings, periods);
                    var trendAnalysis = AnalyzeTrend(dataPoints);

                    var totalHours = (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    var totalDistance = bookings.Sum(b => CalculateBookingDistance(b) ?? 0);
                    var avgUtilization = dataPoints.Any() ? dataPoints.Average(dp => dp.UtilizationRate) : 0;
                    var peakDataPoint = dataPoints.OrderByDescending(dp => dp.UtilizationRate).FirstOrDefault();

                    vehiclesSeries.Add(new VehicleUsageTimeSeries
                    {
                        VehicleId = vehicle.Id,
                        VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                        LicensePlate = vehicle.LicensePlate,
                        DataPoints = dataPoints,
                        Trend = trendAnalysis,
                        TotalHours = Math.Round(totalHours, 2),
                        TotalDistance = totalDistance,
                        TotalBookings = bookings.Count,
                        AverageUtilization = Math.Round(avgUtilization, 2),
                        PeakUtilization = peakDataPoint?.UtilizationRate ?? 0,
                        PeakPeriod = peakDataPoint?.PeriodLabel ?? "N/A"
                    });
                }

                // Calculate statistics
                var statistics = CalculateVehicleComparisonStatistics(vehiclesSeries);

                // Generate rankings
                var vehicleRankings = GenerateVehicleRankings(vehiclesSeries);

                // Generate insights
                var insights = GenerateVehicleInsights(vehiclesSeries);

                var response = new VehiclesUsageComparisonResponse
                {
                    VehiclesSeries = vehiclesSeries,
                    Statistics = statistics,
                    Rankings = vehicleRankings,
                    Insights = insights,
                    AnalysisStartDate = startDate,
                    AnalysisEndDate = endDate,
                    Granularity = request.Granularity,
                    TotalPeriods = periods.Count,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<VehiclesUsageComparisonResponse>
                {
                    StatusCode = 200,
                    Message = "VEHICLES_USAGE_COMPARISON_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing vehicles usage for user {UserId}", userId);
                return new BaseResponse<VehiclesUsageComparisonResponse>
                {
                    StatusCode = 500,
                    Message = "ERROR_COMPARING_VEHICLES_USAGE",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<PeriodUsageComparisonResponse>> ComparePeriodUsageAsync(
            int userId,
            ComparePeriodUsageRequest request)
        {
            try
            {
                _logger.LogInformation("Comparing period usage for user {UserId}", userId);

                // Get user's co-owners
                var userCoOwners = await _unitOfWork.CoOwnerRepository
                    .GetQueryable()
                    .Where(co => co.UserId == userId)
                    .Include(co => co.VehicleCoOwners)
                    .ThenInclude(vco => vco.Vehicle)
                    .ToListAsync();

                if (!userCoOwners.Any())
                {
                    return new BaseResponse<PeriodUsageComparisonResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_CO_OWNER",
                        Data = null
                    };
                }

                var coOwnerIds = userCoOwners.Select(co => co.UserId).ToList();

                // Build query for bookings
                var bookingsQuery = _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Where(b => coOwnerIds.Contains(b.CoOwnerId ?? 0) &&
                               b.StatusEnum == EBookingStatus.Completed);

                // Apply vehicle filter if specified
                if (request.VehicleId.HasValue)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.VehicleId == request.VehicleId.Value);
                }

                bookingsQuery = bookingsQuery
                    .Include(b => b.Vehicle)
                    .Include(b => b.CheckIns.OrderBy(ci => ci.CheckTime).Take(1))
                    .Include(b => b.CheckOuts.OrderByDescending(co => co.CheckTime).Take(1));

                // Get bookings for both periods
                var period1Bookings = await bookingsQuery
                    .Where(b => b.StartTime >= request.Period1Start && b.StartTime <= request.Period1End)
                    .ToListAsync();

                var period2Bookings = await bookingsQuery
                    .Where(b => b.StartTime >= request.Period2Start && b.StartTime <= request.Period2End)
                    .ToListAsync();

                // Build period data
                var period1Data = BuildPeriodData(
                    period1Bookings,
                    request.Period1Start,
                    request.Period1End,
                    request.Period1Label ?? $"{request.Period1Start:MMM dd} - {request.Period1End:MMM dd}");

                var period2Data = BuildPeriodData(
                    period2Bookings,
                    request.Period2Start,
                    request.Period2End,
                    request.Period2Label ?? $"{request.Period2Start:MMM dd} - {request.Period2End:MMM dd}");

                // Calculate comparison metrics
                var comparison = CalculatePeriodComparison(period1Data, period2Data);

                // Vehicle-specific comparison if single vehicle
                VehiclePeriodComparison? vehicleComparison = null;
                if (request.VehicleId.HasValue)
                {
                    vehicleComparison = await BuildVehiclePeriodComparison(
                        request.VehicleId.Value,
                        period1Bookings,
                        period2Bookings,
                        request.Period1Start,
                        request.Period1End,
                        request.Period2Start,
                        request.Period2End);
                }

                // Generate insights
                var insights = GeneratePeriodInsights(period1Data, period2Data, comparison);

                var response = new PeriodUsageComparisonResponse
                {
                    Period1 = period1Data,
                    Period2 = period2Data,
                    Comparison = comparison,
                    VehicleComparison = vehicleComparison,
                    Insights = insights,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<PeriodUsageComparisonResponse>
                {
                    StatusCode = 200,
                    Message = "PERIOD_USAGE_COMPARISON_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing period usage for user {UserId}", userId);
                return new BaseResponse<PeriodUsageComparisonResponse>
                {
                    StatusCode = 500,
                    Message = "ERROR_COMPARING_PERIOD_USAGE",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Helper Methods for Usage Comparison

        private List<UsageDataPoint> BuildUsageDataPoints(
            List<Booking> bookings,
            List<(DateTime Start, DateTime End, string Label)> periods)
        {
            var dataPoints = new List<UsageDataPoint>();
            UsageDataPoint? previousPoint = null;

            foreach (var (periodStart, periodEnd, label) in periods)
            {
                var periodBookings = bookings.Where(b =>
                    b.StartTime >= periodStart && b.StartTime < periodEnd).ToList();

                var hours = (decimal)periodBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var distance = periodBookings.Sum(b => CalculateBookingDistance(b) ?? 0);
                var bookingCount = periodBookings.Count;

                // Calculate utilization rate
                var periodDays = (periodEnd - periodStart).TotalDays;
                var availableHours = (decimal)(periodDays * 24);
                var utilizationRate = availableHours > 0 ? Math.Round((hours / availableHours) * 100, 2) : 0;

                var dataPoint = new UsageDataPoint
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    PeriodLabel = label,
                    Hours = Math.Round(hours, 2),
                    Distance = distance,
                    BookingCount = bookingCount,
                    UtilizationRate = utilizationRate
                };

                // Calculate changes from previous period
                if (previousPoint != null)
                {
                    dataPoint.HoursChange = Math.Round(hours - previousPoint.Hours, 2);
                    dataPoint.DistanceChange = distance - previousPoint.Distance;
                    dataPoint.BookingCountChange = bookingCount - previousPoint.BookingCount;
                }

                dataPoints.Add(dataPoint);
                previousPoint = dataPoint;
            }

            return dataPoints;
        }

        private TrendAnalysis AnalyzeTrend(List<UsageDataPoint> dataPoints)
        {
            if (!dataPoints.Any())
            {
                return new TrendAnalysis
                {
                    Direction = "Unknown",
                    GrowthRate = 0,
                    AverageChange = 0,
                    Volatility = 0,
                    IsConsistent = false,
                    Pattern = "Insufficient Data"
                };
            }

            var values = dataPoints.Select(dp => dp.Hours).ToList();
            var changes = dataPoints.Where(dp => dp.HoursChange.HasValue)
                                   .Select(dp => dp.HoursChange!.Value)
                                   .ToList();

            // Calculate growth rate
            var firstValue = values.First();
            var lastValue = values.Last();
            var growthRate = firstValue > 0
                ? Math.Round(((lastValue - firstValue) / firstValue) * 100, 2)
                : 0;

            // Determine direction
            var direction = growthRate switch
            {
                > 10 => "Increasing",
                < -10 => "Decreasing",
                _ => "Stable"
            };

            // Calculate average change
            var avgChange = changes.Any() ? Math.Round(changes.Average(), 2) : 0;

            // Calculate volatility (standard deviation)
            var volatility = CalculateVariance(changes);

            // Determine if consistent
            var isConsistent = volatility < 5;

            // Determine pattern (simplified)
            var pattern = volatility switch
            {
                < 3 => direction == "Stable" ? "Stable" : "Linear",
                < 10 => "Moderate Variation",
                _ => "Volatile"
            };

            return new TrendAnalysis
            {
                Direction = direction,
                GrowthRate = growthRate,
                AverageChange = avgChange,
                Volatility = volatility,
                IsConsistent = isConsistent,
                Pattern = pattern
            };
        }

        private UsageComparisonStatistics CalculateComparisonStatistics(
            List<CoOwnerUsageTimeSeries> coOwnersSeries)
        {
            var totalHours = coOwnersSeries.Sum(s => s.TotalHours);
            var totalDistance = coOwnersSeries.Sum(s => s.TotalDistance);
            var totalBookings = coOwnersSeries.Sum(s => s.TotalBookings);

            var avgHours = coOwnersSeries.Any() ? totalHours / coOwnersSeries.Count : 0;
            var avgDistance = coOwnersSeries.Any() ? totalDistance / coOwnersSeries.Count : 0;
            var avgBookings = coOwnersSeries.Any() ? (decimal)totalBookings / coOwnersSeries.Count : 0;

            // Calculate Gini coefficient (measure of inequality)
            var gini = CalculateGiniCoefficient(coOwnersSeries.Select(s => s.TotalHours).ToList());

            // Calculate dispersion
            var hoursVariance = CalculateVariance(coOwnersSeries.Select(s => s.TotalHours).ToList());

            var mostActive = coOwnersSeries.OrderByDescending(s => s.TotalHours).FirstOrDefault();
            var leastActive = coOwnersSeries.OrderBy(s => s.TotalHours).FirstOrDefault();
            var fastestGrowing = coOwnersSeries.OrderByDescending(s => s.Trend.GrowthRate).FirstOrDefault();

            return new UsageComparisonStatistics
            {
                TotalHoursAllCoOwners = Math.Round(totalHours, 2),
                TotalDistanceAllCoOwners = totalDistance,
                TotalBookingsAllCoOwners = totalBookings,
                AverageHoursPerCoOwner = Math.Round(avgHours, 2),
                AverageDistancePerCoOwner = Math.Round(avgDistance, 2),
                AverageBookingsPerCoOwner = Math.Round(avgBookings, 2),
                UsageDispersion = Math.Round(hoursVariance, 2),
                GiniCoefficient = Math.Round(gini, 3),
                MostActiveCoOwner = mostActive?.CoOwnerName ?? "N/A",
                MostActiveHours = mostActive?.TotalHours ?? 0,
                LeastActiveCoOwner = leastActive?.CoOwnerName ?? "N/A",
                LeastActiveHours = leastActive?.TotalHours ?? 0,
                FastestGrowingCoOwner = fastestGrowing?.CoOwnerName ?? "N/A",
                FastestGrowthRate = fastestGrowing?.Trend.GrowthRate ?? 0
            };
        }

        private decimal CalculateGiniCoefficient(List<decimal> values)
        {
            if (!values.Any()) return 0;

            var n = values.Count;
            var sortedValues = values.OrderBy(v => v).ToList();
            var sumOfProducts = sortedValues.Select((v, i) => (i + 1) * v).Sum();
            var totalValue = values.Sum();

            if (totalValue == 0) return 0;

            var gini = (2 * sumOfProducts) / (n * totalValue) - (n + 1) / (decimal)n;
            return Math.Max(0, gini); // Ensure non-negative
        }

        private List<CoOwnerRanking> GenerateRankings(List<CoOwnerUsageTimeSeries> series)
        {
            var rankings = new List<CoOwnerRanking>();

            // Hours ranking
            var hoursRanking = series
                .OrderByDescending(s => s.TotalHours)
                .Select((s, index) => new RankingEntry
                {
                    Rank = index + 1,
                    CoOwnerId = s.CoOwnerId,
                    CoOwnerName = s.CoOwnerName,
                    Value = s.TotalHours,
                    PercentageOfTotal = series.Sum(x => x.TotalHours) > 0
                        ? Math.Round((s.TotalHours / series.Sum(x => x.TotalHours)) * 100, 2)
                        : 0
                })
                .ToList();

            rankings.Add(new CoOwnerRanking
            {
                Metric = "Hours",
                Rankings = hoursRanking
            });

            // Distance ranking
            var distanceRanking = series
                .OrderByDescending(s => s.TotalDistance)
                .Select((s, index) => new RankingEntry
                {
                    Rank = index + 1,
                    CoOwnerId = s.CoOwnerId,
                    CoOwnerName = s.CoOwnerName,
                    Value = s.TotalDistance,
                    PercentageOfTotal = series.Sum(x => x.TotalDistance) > 0
                        ? Math.Round((s.TotalDistance / series.Sum(x => x.TotalDistance)) * 100, 2)
                        : 0
                })
                .ToList();

            rankings.Add(new CoOwnerRanking
            {
                Metric = "Distance",
                Rankings = distanceRanking
            });

            // Booking count ranking
            var bookingRanking = series
                .OrderByDescending(s => s.TotalBookings)
                .Select((s, index) => new RankingEntry
                {
                    Rank = index + 1,
                    CoOwnerId = s.CoOwnerId,
                    CoOwnerName = s.CoOwnerName,
                    Value = s.TotalBookings,
                    PercentageOfTotal = series.Sum(x => x.TotalBookings) > 0
                        ? Math.Round(((decimal)s.TotalBookings / series.Sum(x => x.TotalBookings)) * 100, 2)
                        : 0
                })
                .ToList();

            rankings.Add(new CoOwnerRanking
            {
                Metric = "BookingCount",
                Rankings = bookingRanking
            });

            // Growth rate ranking
            var growthRanking = series
                .OrderByDescending(s => s.Trend.GrowthRate)
                .Select((s, index) => new RankingEntry
                {
                    Rank = index + 1,
                    CoOwnerId = s.CoOwnerId,
                    CoOwnerName = s.CoOwnerName,
                    Value = s.Trend.GrowthRate,
                    PercentageOfTotal = 0 // N/A for growth rate
                })
                .ToList();

            rankings.Add(new CoOwnerRanking
            {
                Metric = "GrowthRate",
                Rankings = growthRanking
            });

            return rankings;
        }

        private List<UsageInsight> GenerateUsageInsights(
            List<CoOwnerUsageTimeSeries> series,
            List<VehicleCoOwner> coOwners)
        {
            var insights = new List<UsageInsight>();

            // Check for significant imbalance
            var gini = CalculateGiniCoefficient(series.Select(s => s.TotalHours).ToList());
            if (gini > 0.4m)
            {
                insights.Add(new UsageInsight
                {
                    Type = "Imbalance",
                    Severity = gini > 0.6m ? "Critical" : "Warning",
                    Title = "Significant Usage Imbalance Detected",
                    Description = $"Usage distribution shows significant inequality (Gini: {gini:F3}). Some co-owners may be using the vehicle much more than others relative to their ownership.",
                    AffectedCoOwners = series.Select(s => s.CoOwnerName).ToList(),
                    Data = new Dictionary<string, object> { { "GiniCoefficient", gini } }
                });
            }

            // Check for underutilization
            foreach (var s in series)
            {
                if (s.TotalHours < 10 && s.OwnershipPercentage > 10)
                {
                    insights.Add(new UsageInsight
                    {
                        Type = "UnusualPattern",
                        Severity = "Info",
                        Title = $"{s.CoOwnerName} - Low Usage Despite Ownership",
                        Description = $"{s.CoOwnerName} has {s.OwnershipPercentage}% ownership but very low usage ({s.TotalHours} hours). Consider encouraging more usage or adjusting ownership.",
                        AffectedCoOwners = new List<string> { s.CoOwnerName },
                        Data = new Dictionary<string, object>
                        {
                            { "OwnershipPercentage", s.OwnershipPercentage },
                            { "TotalHours", s.TotalHours }
                        }
                    });
                }
            }

            // Check for rapid growth
            var rapidGrowthOwners = series.Where(s => s.Trend.GrowthRate > 50).ToList();
            if (rapidGrowthOwners.Any())
            {
                insights.Add(new UsageInsight
                {
                    Type = "Recommendation",
                    Severity = "Info",
                    Title = "Rapid Usage Growth Detected",
                    Description = $"{rapidGrowthOwners.Count} co-owner(s) showing rapid usage growth. Monitor for potential booking conflicts.",
                    AffectedCoOwners = rapidGrowthOwners.Select(s => s.CoOwnerName).ToList(),
                    Data = new Dictionary<string, object>
                    {
                        { "AffectedCount", rapidGrowthOwners.Count },
                        { "AvgGrowthRate", rapidGrowthOwners.Average(s => s.Trend.GrowthRate) }
                    }
                });
            }

            return insights;
        }

        private VehicleComparisonStatistics CalculateVehicleComparisonStatistics(
            List<VehicleUsageTimeSeries> vehiclesSeries)
        {
            var totalHours = vehiclesSeries.Sum(v => v.TotalHours);
            var totalDistance = vehiclesSeries.Sum(v => v.TotalDistance);
            var totalBookings = vehiclesSeries.Sum(v => v.TotalBookings);

            var mostUtilized = vehiclesSeries.OrderByDescending(v => v.TotalHours).FirstOrDefault();
            var leastUtilized = vehiclesSeries.OrderBy(v => v.TotalHours).FirstOrDefault();
            var mostEfficient = vehiclesSeries.OrderByDescending(v => v.AverageUtilization).FirstOrDefault();

            return new VehicleComparisonStatistics
            {
                TotalHoursAllVehicles = Math.Round(totalHours, 2),
                TotalDistanceAllVehicles = totalDistance,
                TotalBookingsAllVehicles = totalBookings,
                AverageHoursPerVehicle = vehiclesSeries.Any() ? Math.Round(totalHours / vehiclesSeries.Count, 2) : 0,
                AverageDistancePerVehicle = vehiclesSeries.Any() ? Math.Round(totalDistance / vehiclesSeries.Count, 2) : 0,
                AverageUtilizationRate = vehiclesSeries.Any() ? Math.Round(vehiclesSeries.Average(v => v.AverageUtilization), 2) : 0,
                MostUtilizedVehicle = mostUtilized?.VehicleName ?? "N/A",
                MostUtilizedHours = mostUtilized?.TotalHours ?? 0,
                LeastUtilizedVehicle = leastUtilized?.VehicleName ?? "N/A",
                LeastUtilizedHours = leastUtilized?.TotalHours ?? 0,
                MostEfficientVehicle = mostEfficient?.VehicleName ?? "N/A",
                BestUtilizationRate = mostEfficient?.AverageUtilization ?? 0
            };
        }

        private List<VehicleRanking> GenerateVehicleRankings(List<VehicleUsageTimeSeries> series)
        {
            var rankings = new List<VehicleRanking>();

            // Hours ranking
            rankings.Add(new VehicleRanking
            {
                Metric = "Hours",
                Rankings = series.OrderByDescending(v => v.TotalHours)
                    .Select((v, i) => new VehicleRankingEntry
                    {
                        Rank = i + 1,
                        VehicleId = v.VehicleId,
                        VehicleName = v.VehicleName,
                        Value = v.TotalHours,
                        PercentageOfTotal = series.Sum(x => x.TotalHours) > 0
                            ? Math.Round((v.TotalHours / series.Sum(x => x.TotalHours)) * 100, 2)
                            : 0
                    }).ToList()
            });

            // Utilization ranking
            rankings.Add(new VehicleRanking
            {
                Metric = "UtilizationRate",
                Rankings = series.OrderByDescending(v => v.AverageUtilization)
                    .Select((v, i) => new VehicleRankingEntry
                    {
                        Rank = i + 1,
                        VehicleId = v.VehicleId,
                        VehicleName = v.VehicleName,
                        Value = v.AverageUtilization,
                        PercentageOfTotal = 0
                    }).ToList()
            });

            return rankings;
        }

        private List<UsageInsight> GenerateVehicleInsights(List<VehicleUsageTimeSeries> series)
        {
            var insights = new List<UsageInsight>();

            // Check for underutilized vehicles
            var underutilized = series.Where(v => v.AverageUtilization < 20).ToList();
            if (underutilized.Any())
            {
                insights.Add(new UsageInsight
                {
                    Type = "Recommendation",
                    Severity = "Warning",
                    Title = "Underutilized Vehicles Detected",
                    Description = $"{underutilized.Count} vehicle(s) have low utilization rates (<20%). Consider promoting their usage or reviewing ownership structure.",
                    AffectedCoOwners = underutilized.Select(v => v.VehicleName).ToList(),
                    Data = new Dictionary<string, object>
                    {
                        { "VehicleCount", underutilized.Count },
                        { "AvgUtilization", underutilized.Average(v => v.AverageUtilization) }
                    }
                });
            }

            return insights;
        }

        private PeriodUsageData BuildPeriodData(
            List<Booking> bookings,
            DateTime startDate,
            DateTime endDate,
            string label)
        {
            var totalHours = (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalDistance = bookings.Sum(b => CalculateBookingDistance(b) ?? 0);
            var avgDuration = bookings.Any() ? totalHours / bookings.Count : 0;
            var avgDistance = bookings.Any() && totalDistance > 0 ? (decimal)totalDistance / bookings.Count : 0;

            var durationDays = (int)(endDate - startDate).TotalDays;
            var availableHours = durationDays * 24;
            var utilizationRate = availableHours > 0 ? (totalHours / availableHours) * 100 : 0;

            var mostActiveDay = bookings.GroupBy(b => b.StartTime.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key.ToString() ?? "N/A";

            var mostActiveTime = bookings.GroupBy(b => GetTimeSlot(b.StartTime.Hour))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "N/A";

            var vehicleBreakdown = bookings.GroupBy(b => b.VehicleId)
                .Select(g =>
                {
                    var vehicle = g.First().Vehicle;
                    var vehHours = (decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                    return new VehicleUsageSummary
                    {
                        VehicleId = g.Key ?? 0,
                        VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                        LicensePlate = vehicle.LicensePlate,
                        TotalHours = Math.Round(vehHours, 2),
                        TotalBookings = g.Count(),
                        OwnershipPercentage = 0,
                        InvestmentAmount = 0,
                        CompletedBookings = g.Count(),
                        TotalDistance = g.Sum(b => CalculateBookingDistance(b) ?? 0),
                        TotalCost = 0,
                        UsagePercentage = 0,
                        UsageVsOwnershipDelta = 0,
                        UsagePattern = "N/A",
                        FirstBooking = g.Min(b => b.StartTime),
                        LastBooking = g.Max(b => b.StartTime)
                    };
                }).ToList();

            return new PeriodUsageData
            {
                StartDate = startDate,
                EndDate = endDate,
                Label = label,
                DurationDays = durationDays,
                TotalHours = Math.Round(totalHours, 2),
                TotalDistance = totalDistance,
                TotalBookings = bookings.Count,
                AverageBookingDuration = Math.Round(avgDuration, 2),
                AverageTripDistance = Math.Round(avgDistance, 2),
                UtilizationRate = Math.Round(utilizationRate, 2),
                VehicleBreakdown = vehicleBreakdown,
                MostActiveDay = mostActiveDay,
                MostActiveTimeSlot = mostActiveTime
            };
        }

        private PeriodComparisonMetrics CalculatePeriodComparison(
            PeriodUsageData period1,
            PeriodUsageData period2)
        {
            var hoursChange = period2.TotalHours - period1.TotalHours;
            var distanceChange = period2.TotalDistance - period1.TotalDistance;
            var bookingChange = period2.TotalBookings - period1.TotalBookings;
            var utilizationChange = period2.UtilizationRate - period1.UtilizationRate;

            var hoursChangePct = period1.TotalHours > 0 ? (hoursChange / period1.TotalHours) * 100 : 0;
            var distanceChangePct = period1.TotalDistance > 0 ? ((decimal)distanceChange / period1.TotalDistance) * 100 : 0;
            var bookingChangePct = period1.TotalBookings > 0 ? ((decimal)bookingChange / period1.TotalBookings) * 100 : 0;
            var utilizationChangePct = period1.UtilizationRate > 0 ? (utilizationChange / period1.UtilizationRate) * 100 : 0;

            var hoursPerDayChange = hoursChange / Math.Max(1, period2.DurationDays);
            var distancePerDayChange = distanceChange / Math.Max(1, period2.DurationDays);
            var bookingsPerDayChange = (decimal)bookingChange / Math.Max(1, period2.DurationDays);

            var trend = hoursChangePct switch
            {
                > 5 => "Increased",
                < -5 => "Decreased",
                _ => "Stable"
            };

            var strength = Math.Abs(hoursChangePct) switch
            {
                > 30 => "Significant",
                > 15 => "Moderate",
                _ => "Slight"
            };

            return new PeriodComparisonMetrics
            {
                HoursChange = Math.Round(hoursChange, 2),
                DistanceChange = distanceChange,
                BookingCountChange = bookingChange,
                UtilizationRateChange = Math.Round(utilizationChange, 2),
                HoursChangePercentage = Math.Round(hoursChangePct, 2),
                DistanceChangePercentage = Math.Round(distanceChangePct, 2),
                BookingCountChangePercentage = Math.Round(bookingChangePct, 2),
                UtilizationRateChangePercentage = Math.Round(utilizationChangePct, 2),
                HoursPerDayChange = Math.Round(hoursPerDayChange, 2),
                DistancePerDayChange = Math.Round(distancePerDayChange, 2),
                BookingsPerDayChange = Math.Round(bookingsPerDayChange, 2),
                OverallTrend = trend,
                TrendStrength = strength
            };
        }

        private async Task<VehiclePeriodComparison> BuildVehiclePeriodComparison(
            int vehicleId,
            List<Booking> period1Bookings,
            List<Booking> period2Bookings,
            DateTime period1Start,
            DateTime period1End,
            DateTime period2Start,
            DateTime period2End)
        {
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
            var coOwners = await _unitOfWork.VehicleCoOwnerRepository
                .GetQueryable()
                .Where(vco => vco.VehicleId == vehicleId)
                .Include(vco => vco.CoOwner)
                .ThenInclude(co => co.User)
                .ToListAsync();

            var coOwnerChanges = coOwners.Select(vco =>
            {
                var p1Bookings = period1Bookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();
                var p2Bookings = period2Bookings.Where(b => b.CoOwnerId == vco.CoOwnerId).ToList();

                var p1Hours = (decimal)p1Bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var p2Hours = (decimal)p2Bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
                var change = p2Hours - p1Hours;
                var changePct = p1Hours > 0 ? (change / p1Hours) * 100 : 0;

                return new CoOwnerPeriodChange
                {
                    CoOwnerId = vco.CoOwnerId,
                    CoOwnerName = $"{vco.CoOwner.User.FirstName} {vco.CoOwner.User.LastName}",
                    Period1Hours = Math.Round(p1Hours, 2),
                    Period2Hours = Math.Round(p2Hours, 2),
                    HoursChange = Math.Round(change, 2),
                    HoursChangePercentage = Math.Round(changePct, 2),
                    ChangeDirection = changePct switch
                    {
                        > 5 => "Increased",
                        < -5 => "Decreased",
                        _ => "Stable"
                    }
                };
            }).ToList();

            var patternComparison = new BookingPatternComparison
            {
                Period1DayDistribution = period1Bookings.GroupBy(b => b.StartTime.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => (decimal)g.Count()),
                Period2DayDistribution = period2Bookings.GroupBy(b => b.StartTime.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => (decimal)g.Count()),
                Period1TimeDistribution = period1Bookings.GroupBy(b => GetTimeSlot(b.StartTime.Hour))
                    .ToDictionary(g => g.Key, g => g.Count()),
                Period2TimeDistribution = period2Bookings.GroupBy(b => GetTimeSlot(b.StartTime.Hour))
                    .ToDictionary(g => g.Key, g => g.Count()),
                PatternChanges = new List<string>()
            };

            return new VehiclePeriodComparison
            {
                VehicleId = vehicleId,
                VehicleName = $"{vehicle!.Brand} {vehicle.Model}",
                CoOwnerChanges = coOwnerChanges,
                PatternComparison = patternComparison
            };
        }

        private List<UsageInsight> GeneratePeriodInsights(
            PeriodUsageData period1,
            PeriodUsageData period2,
            PeriodComparisonMetrics comparison)
        {
            var insights = new List<UsageInsight>();

            if (Math.Abs(comparison.HoursChangePercentage) > 30)
            {
                insights.Add(new UsageInsight
                {
                    Type = comparison.HoursChangePercentage > 0 ? "Recommendation" : "UnusualPattern",
                    Severity = "Warning",
                    Title = $"Significant Usage {comparison.OverallTrend}",
                    Description = $"Usage has {comparison.OverallTrend.ToLower()} by {Math.Abs(comparison.HoursChangePercentage):F1}% between periods. " +
                                 (comparison.HoursChangePercentage > 0
                                     ? "Monitor for potential booking conflicts."
                                     : "Consider investigating reasons for decreased usage."),
                    AffectedCoOwners = new List<string>(),
                    Data = new Dictionary<string, object>
                    {
                        { "ChangePercentage", comparison.HoursChangePercentage },
                        { "Period1Hours", period1.TotalHours },
                        { "Period2Hours", period2.TotalHours }
                    }
                });
            }

            return insights;
        }

        #endregion
    }
}
