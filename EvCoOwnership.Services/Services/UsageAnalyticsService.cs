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
                    Pagination = new PaginationInfo
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
    }
}
