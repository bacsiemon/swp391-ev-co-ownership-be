using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ReportDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Helpers;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace EvCoOwnership.Services.Services
{
    public class VehicleReportService : IVehicleReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VehicleReportService> _logger;

        public VehicleReportService(IUnitOfWork unitOfWork, ILogger<VehicleReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<MonthlyReportResponse>> GenerateMonthlyReportAsync(GenerateMonthlyReportRequest request, Guid userId)
        {
            try
            {
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate month
                if (request.Month < 1 || request.Month > 12)
                {
                    return new BaseResponse<MonthlyReportResponse>
                    {
                        StatusCode = 400,
                        Message = "Month must be between 1 and 12",
                        Data = null
                    };
                }

                // Validate vehicle and co-ownership
                var validationResult = await ValidateVehicleAccessAsync(request.VehicleId, userIdInt);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<MonthlyReportResponse>
                    {
                        StatusCode = validationResult.StatusCode,
                        Message = validationResult.Message,
                        Data = null
                    };
                }

                var vehicle = validationResult.Vehicle!;

                // Calculate period dates
                var startDate = new DateTime(request.Year, request.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Generate the report
                var report = new MonthlyReportResponse
                {
                    VehicleId = request.VehicleId,
                    VehicleName = vehicle.Name,
                    Year = request.Year,
                    Month = request.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month),
                    PeriodDescription = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)} {request.Year}",
                    GeneratedAt = DateTime.UtcNow,
                    UsageSummary = await GenerateUsageSummaryAsync(request.VehicleId, startDate, endDate),
                    CostSummary = await GenerateCostSummaryAsync(request.VehicleId, vehicle.FundId, startDate, endDate),
                    MaintenanceSummary = await GenerateMaintenanceSummaryAsync(request.VehicleId, startDate, endDate),
                    FundStatus = await GenerateFundStatusAsync(vehicle.FundId, request.VehicleId, startDate, endDate)
                };

                return new BaseResponse<MonthlyReportResponse>
                {
                    StatusCode = 200,
                    Message = "Monthly report generated successfully",
                    Data = report
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                return new BaseResponse<MonthlyReportResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while generating the monthly report",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<QuarterlyReportResponse>> GenerateQuarterlyReportAsync(GenerateQuarterlyReportRequest request, Guid userId)
        {
            try
            {
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate quarter
                if (request.Quarter < 1 || request.Quarter > 4)
                {
                    return new BaseResponse<QuarterlyReportResponse>
                    {
                        StatusCode = 400,
                        Message = "Quarter must be between 1 and 4",
                        Data = null
                    };
                }

                // Validate vehicle and co-ownership
                var validationResult = await ValidateVehicleAccessAsync(request.VehicleId, userIdInt);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<QuarterlyReportResponse>
                    {
                        StatusCode = validationResult.StatusCode,
                        Message = validationResult.Message,
                        Data = null
                    };
                }

                var vehicle = validationResult.Vehicle!;

                // Calculate quarter dates
                int startMonth = (request.Quarter - 1) * 3 + 1;
                var startDate = new DateTime(request.Year, startMonth, 1);
                var endDate = startDate.AddMonths(3).AddDays(-1);

                // Generate the report
                var report = new QuarterlyReportResponse
                {
                    VehicleId = request.VehicleId,
                    VehicleName = vehicle.Name,
                    Year = request.Year,
                    Quarter = request.Quarter,
                    QuarterName = $"Q{request.Quarter}",
                    PeriodDescription = $"Q{request.Quarter} {request.Year} ({GetQuarterMonthRange(request.Quarter)})",
                    GeneratedAt = DateTime.UtcNow,
                    UsageSummary = await GenerateUsageSummaryAsync(request.VehicleId, startDate, endDate),
                    CostSummary = await GenerateCostSummaryAsync(request.VehicleId, vehicle.FundId, startDate, endDate),
                    MaintenanceSummary = await GenerateMaintenanceSummaryAsync(request.VehicleId, startDate, endDate),
                    FundStatus = await GenerateFundStatusAsync(vehicle.FundId, request.VehicleId, startDate, endDate)
                };

                // Generate monthly breakdown
                report.MonthlyBreakdown = new List<MonthlyReportSummary>();
                for (int monthOffset = 0; monthOffset < 3; monthOffset++)
                {
                    int month = startMonth + monthOffset;
                    var monthStart = new DateTime(request.Year, month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlySummary = await GenerateMonthlyReportSummaryAsync(request.VehicleId, vehicle.FundId, request.Year, month, monthStart, monthEnd);
                    report.MonthlyBreakdown.Add(monthlySummary);
                }

                return new BaseResponse<QuarterlyReportResponse>
                {
                    StatusCode = 200,
                    Message = "Quarterly report generated successfully",
                    Data = report
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quarterly report");
                return new BaseResponse<QuarterlyReportResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while generating the quarterly report",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<YearlyReportResponse>> GenerateYearlyReportAsync(GenerateYearlyReportRequest request, Guid userId)
        {
            try
            {
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate vehicle and co-ownership
                var validationResult = await ValidateVehicleAccessAsync(request.VehicleId, userIdInt);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<YearlyReportResponse>
                    {
                        StatusCode = validationResult.StatusCode,
                        Message = validationResult.Message,
                        Data = null
                    };
                }

                var vehicle = validationResult.Vehicle!;

                // Calculate year dates
                var startDate = new DateTime(request.Year, 1, 1);
                var endDate = new DateTime(request.Year, 12, 31);

                // Generate the report
                var report = new YearlyReportResponse
                {
                    VehicleId = request.VehicleId,
                    VehicleName = vehicle.Name,
                    Year = request.Year,
                    PeriodDescription = $"Year {request.Year}",
                    GeneratedAt = DateTime.UtcNow,
                    UsageSummary = await GenerateUsageSummaryAsync(request.VehicleId, startDate, endDate),
                    CostSummary = await GenerateCostSummaryAsync(request.VehicleId, vehicle.FundId, startDate, endDate),
                    MaintenanceSummary = await GenerateMaintenanceSummaryAsync(request.VehicleId, startDate, endDate),
                    FundStatus = await GenerateFundStatusAsync(vehicle.FundId, request.VehicleId, startDate, endDate)
                };

                // Generate quarterly breakdown
                report.QuarterlyBreakdown = new List<QuarterlyReportSummary>();
                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    int startMonth = (quarter - 1) * 3 + 1;
                    var quarterStart = new DateTime(request.Year, startMonth, 1);
                    var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

                    var quarterlySummary = await GenerateQuarterlyReportSummaryAsync(request.VehicleId, vehicle.FundId, request.Year, quarter, quarterStart, quarterEnd);
                    report.QuarterlyBreakdown.Add(quarterlySummary);
                }

                // Generate monthly breakdown
                report.MonthlyBreakdown = new List<MonthlyReportSummary>();
                for (int month = 1; month <= 12; month++)
                {
                    var monthStart = new DateTime(request.Year, month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlySummary = await GenerateMonthlyReportSummaryAsync(request.VehicleId, vehicle.FundId, request.Year, month, monthStart, monthEnd);
                    report.MonthlyBreakdown.Add(monthlySummary);
                }

                return new BaseResponse<YearlyReportResponse>
                {
                    StatusCode = 200,
                    Message = "Yearly report generated successfully",
                    Data = report
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating yearly report");
                return new BaseResponse<YearlyReportResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while generating the yearly report",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<ExportReportResponse>> ExportReportAsync(ExportReportRequest request, Guid userId)
        {
            try
            {
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate vehicle and co-ownership
                var validationResult = await ValidateVehicleAccessAsync(request.VehicleId, userIdInt);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<ExportReportResponse>
                    {
                        StatusCode = validationResult.StatusCode,
                        Message = validationResult.Message,
                        Data = null
                    };
                }

                // Determine report type and generate appropriate report
                object? reportData = null;
                string fileName;

                if (request.Month.HasValue)
                {
                    // Monthly report
                    var monthlyRequest = new GenerateMonthlyReportRequest
                    {
                        VehicleId = request.VehicleId,
                        Year = request.Year,
                        Month = request.Month.Value
                    };
                    var monthlyResult = await GenerateMonthlyReportAsync(monthlyRequest, userId);
                    reportData = monthlyResult.Data;
                    fileName = $"Monthly_Report_{request.Year}_{request.Month:D2}";
                }
                else if (request.Quarter.HasValue)
                {
                    // Quarterly report
                    var quarterlyRequest = new GenerateQuarterlyReportRequest
                    {
                        VehicleId = request.VehicleId,
                        Year = request.Year,
                        Quarter = request.Quarter.Value
                    };
                    var quarterlyResult = await GenerateQuarterlyReportAsync(quarterlyRequest, userId);
                    reportData = quarterlyResult.Data;
                    fileName = $"Quarterly_Report_{request.Year}_Q{request.Quarter}";
                }
                else
                {
                    // Yearly report
                    var yearlyRequest = new GenerateYearlyReportRequest
                    {
                        VehicleId = request.VehicleId,
                        Year = request.Year
                    };
                    var yearlyResult = await GenerateYearlyReportAsync(yearlyRequest, userId);
                    reportData = yearlyResult.Data;
                    fileName = $"Yearly_Report_{request.Year}";
                }

                if (reportData == null)
                {
                    return new BaseResponse<ExportReportResponse>
                    {
                        StatusCode = 500,
                        Message = "Failed to generate report data",
                        Data = null
                    };
                }

                // Export based on format
                byte[] fileContent;
                string contentType;
                string fileExtension;

                if (request.ExportFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    fileContent = await ExportToExcelAsync(reportData);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileExtension = ".xlsx";
                }
                else // Default to PDF
                {
                    fileContent = await ExportToPdfAsync(reportData);
                    contentType = "application/pdf";
                    fileExtension = ".pdf";
                }

                var response = new ExportReportResponse
                {
                    FileName = $"{fileName}{fileExtension}",
                    FileContent = fileContent,
                    ContentType = contentType,
                    FileSizeBytes = fileContent.Length,
                    GeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<ExportReportResponse>
                {
                    StatusCode = 200,
                    Message = $"Report exported to {request.ExportFormat} successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return new BaseResponse<ExportReportResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while exporting the report",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<AvailableReportsResponse>> GetAvailableReportPeriodsAsync(Guid vehicleId, Guid userId)
        {
            try
            {
                int vehicleIdInt = Math.Abs(vehicleId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate vehicle and co-ownership
                var validationResult = await ValidateVehicleAccessAsync(vehicleIdInt, userIdInt);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<AvailableReportsResponse>
                    {
                        StatusCode = validationResult.StatusCode,
                        Message = validationResult.Message,
                        Data = null
                    };
                }

                var vehicle = validationResult.Vehicle!;

                // Get earliest booking date
                var earliestBooking = await _unitOfWork.DbContext.Bookings
                    .Where(b => b.VehicleId == vehicleIdInt)
                    .OrderBy(b => b.CreatedAt)
                    .FirstOrDefaultAsync();

                if (earliestBooking == null)
                {
                    return new BaseResponse<AvailableReportsResponse>
                    {
                        StatusCode = 200,
                        Message = "No bookings found for this vehicle",
                        Data = new AvailableReportsResponse
                        {
                            VehicleId = vehicleIdInt,
                            VehicleName = vehicle.Name,
                            AvailablePeriods = new List<ReportPeriod>()
                        }
                    };
                }

                var startDate = earliestBooking.CreatedAt ?? DateTime.UtcNow;
                var endDate = DateTime.UtcNow;

                var availablePeriods = new List<ReportPeriod>();

                // Generate list of available months
                var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
                var lastDate = new DateTime(endDate.Year, endDate.Month, 1);

                while (currentDate <= lastDate)
                {
                    availablePeriods.Add(new ReportPeriod
                    {
                        Year = currentDate.Year,
                        Month = currentDate.Month,
                        PeriodDescription = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentDate.Month)} {currentDate.Year}",
                        HasData = true
                    });

                    currentDate = currentDate.AddMonths(1);
                }

                var response = new AvailableReportsResponse
                {
                    VehicleId = vehicleIdInt,
                    VehicleName = vehicle.Name,
                    AvailablePeriods = availablePeriods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList()
                };

                return new BaseResponse<AvailableReportsResponse>
                {
                    StatusCode = 200,
                    Message = "Available report periods retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available report periods");
                return new BaseResponse<AvailableReportsResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving available report periods",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #region Private Helper Methods

        private async Task<(bool IsValid, int StatusCode, string Message, Vehicle? Vehicle)> ValidateVehicleAccessAsync(int vehicleId, int userId)
        {
            // Validate vehicle exists
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return (false, 404, "Vehicle not found", null);
            }

            // Validate user is a co-owner
            var isCoOwner = await _unitOfWork.DbContext.CoOwners
                .AnyAsync(co => co.UserId == userId &&
                    co.VehicleCoOwners.Any(vco => vco.VehicleId == vehicleId));

            if (!isCoOwner)
            {
                return (false, 403, "You must be a co-owner of this vehicle to access reports", null);
            }

            return (true, 200, "Valid", vehicle);
        }

        private async Task<UsageSummary> GenerateUsageSummaryAsync(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var bookings = await _unitOfWork.DbContext.Bookings
                .Include(b => b.CoOwner)
                    .ThenInclude(co => co.User)
                .Include(b => b.CheckIns)
                    .ThenInclude(ci => ci.VehicleCondition)
                .Include(b => b.CheckOuts)
                    .ThenInclude(co => co.VehicleCondition)
                .Where(b => b.VehicleId == vehicleId &&
                    b.CreatedAt >= startDate &&
                    b.CreatedAt <= endDate)
                .ToListAsync();

            var totalBookings = bookings.Count;
            var completedBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Completed);
            var cancelledBookings = bookings.Count(b => b.StatusEnum == EBookingStatus.Cancelled);

            decimal totalHours = 0;
            decimal totalDistance = 0;

            foreach (var booking in bookings)
            {
                var duration = (booking.EndTime - booking.StartTime).TotalHours;
                totalHours += (decimal)duration;

                // Calculate distance from check-in/check-out odometer readings
                var checkIn = booking.CheckIns.FirstOrDefault();
                var checkOut = booking.CheckOuts.FirstOrDefault();

                if (checkIn?.VehicleCondition != null && checkOut?.VehicleCondition != null &&
                    checkIn.VehicleCondition.OdometerReading.HasValue && checkOut.VehicleCondition.OdometerReading.HasValue)
                {
                    totalDistance += checkOut.VehicleCondition.OdometerReading.Value - checkIn.VehicleCondition.OdometerReading.Value;
                }
            }

            var averageUsage = totalBookings > 0 ? totalHours / totalBookings : 0;

            // Usage by co-owner
            var usageByCoOwner = bookings
                .GroupBy(b => new { b.CoOwnerId, b.CoOwner })
                .Select(g => new CoOwnerUsageDetail
                {
                    CoOwnerId = g.Key.CoOwnerId ?? 0,
                    UserId = g.Key.CoOwner?.UserId ?? 0,
                    UserName = g.Key.CoOwner?.User != null
                        ? $"{g.Key.CoOwner.User.FirstName} {g.Key.CoOwner.User.LastName}".Trim()
                        : "Unknown",
                    UserEmail = g.Key.CoOwner?.User?.Email ?? "",
                    BookingCount = g.Count(),
                    TotalHours = (decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours),
                    UsagePercentage = totalHours > 0
                        ? (decimal)((decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours) / totalHours * 100)
                        : 0,
                    TotalCost = g.Sum(b => b.TotalCost ?? 0)
                })
                .OrderByDescending(u => u.TotalHours)
                .ToList();

            // Usage by day of week
            var usageByDayOfWeek = bookings
                .GroupBy(b => b.StartTime.DayOfWeek)
                .Select(g => new UsageByDayOfWeek
                {
                    DayOfWeek = g.Key.ToString(),
                    BookingCount = g.Count(),
                    TotalHours = (decimal)g.Sum(b => (b.EndTime - b.StartTime).TotalHours)
                })
                .OrderBy(u => (int)Enum.Parse<DayOfWeek>(u.DayOfWeek))
                .ToList();

            return new UsageSummary
            {
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalHoursUsed = totalHours,
                TotalDistanceTraveled = totalDistance,
                AverageUsagePerBooking = averageUsage,
                UsageByCoOwner = usageByCoOwner,
                UsageByDayOfWeek = usageByDayOfWeek
            };
        }

        private async Task<CostSummary> GenerateCostSummaryAsync(int vehicleId, int? fundId, DateTime startDate, DateTime endDate)
        {
            if (!fundId.HasValue)
            {
                return new CostSummary();
            }

            // Get fund additions (income)
            var fundAdditions = await _unitOfWork.DbContext.FundAdditions
                .Include(fa => fa.CoOwner)
                    .ThenInclude(co => co.User)
                .Where(fa => fa.FundId == fundId.Value &&
                    fa.CreatedAt >= startDate &&
                    fa.CreatedAt <= endDate &&
                    fa.StatusEnum == EFundAdditionStatus.Completed)
                .ToListAsync();

            var totalIncome = fundAdditions.Sum(fa => fa.Amount);

            // Get fund usages (expenses)
            var fundUsages = await _unitOfWork.DbContext.FundUsages
                .Where(fu => fu.FundId == fundId.Value &&
                    fu.CreatedAt >= startDate &&
                    fu.CreatedAt <= endDate)
                .ToListAsync();

            var totalExpenses = fundUsages.Sum(fu => fu.Amount);

            // Get opening balance (balance at start of period)
            var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundId.Value);
            var currentBalance = fund?.CurrentBalance ?? 0;
            var openingBalance = currentBalance - totalIncome + totalExpenses;
            var closingBalance = currentBalance;

            // Expenses by category
            var expensesByCategory = fundUsages
                .GroupBy(fu => fu.UsageTypeEnum ?? EUsageType.Other)
                .Select(g => new ExpenseByCategory
                {
                    Category = g.Key,
                    CategoryName = g.Key.ToString(),
                    Amount = g.Sum(fu => fu.Amount),
                    TransactionCount = g.Count(),
                    Percentage = totalExpenses > 0 ? g.Sum(fu => fu.Amount) / totalExpenses * 100 : 0
                })
                .OrderByDescending(e => e.Amount)
                .ToList();

            // Income by co-owner
            var incomeByCoOwner = fundAdditions
                .GroupBy(fa => new { fa.CoOwnerId, fa.CoOwner })
                .Select(g => new IncomeByCoOwner
                {
                    CoOwnerId = g.Key.CoOwnerId ?? 0,
                    UserId = g.Key.CoOwner?.UserId ?? 0,
                    UserName = g.Key.CoOwner?.User != null
                        ? $"{g.Key.CoOwner.User.FirstName} {g.Key.CoOwner.User.LastName}".Trim()
                        : "Unknown",
                    TotalContribution = g.Sum(fa => fa.Amount),
                    ContributionCount = g.Count(),
                    ContributionPercentage = totalIncome > 0 ? g.Sum(fa => fa.Amount) / totalIncome * 100 : 0
                })
                .OrderByDescending(i => i.TotalContribution)
                .ToList();

            // Monthly trends (only for multi-month periods)
            var monthlyTrends = new List<MonthlyTrend>();
            var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var lastMonth = new DateTime(endDate.Year, endDate.Month, 1);

            while (currentMonth <= lastMonth)
            {
                var monthStart = currentMonth;
                var monthEnd = currentMonth.AddMonths(1).AddDays(-1);

                var monthIncome = fundAdditions
                    .Where(fa => fa.CreatedAt >= monthStart && fa.CreatedAt <= monthEnd)
                    .Sum(fa => fa.Amount);

                var monthExpense = fundUsages
                    .Where(fu => fu.CreatedAt >= monthStart && fu.CreatedAt <= monthEnd)
                    .Sum(fu => fu.Amount);

                monthlyTrends.Add(new MonthlyTrend
                {
                    Year = currentMonth.Year,
                    Month = currentMonth.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentMonth.Month),
                    Income = monthIncome,
                    Expense = monthExpense,
                    NetChange = monthIncome - monthExpense
                });

                currentMonth = currentMonth.AddMonths(1);
            }

            return new CostSummary
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetBalance = totalIncome - totalExpenses,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                ExpensesByCategory = expensesByCategory,
                IncomeByCoOwner = incomeByCoOwner,
                MonthlyTrends = monthlyTrends
            };
        }

        private async Task<MaintenanceSummary> GenerateMaintenanceSummaryAsync(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var maintenanceEvents = await _unitOfWork.DbContext.MaintenanceCosts
                .Where(mc => mc.VehicleId == vehicleId &&
                    mc.CreatedAt >= startDate &&
                    mc.CreatedAt <= endDate)
                .ToListAsync();

            var totalEvents = maintenanceEvents.Count;
            var totalCost = maintenanceEvents.Sum(mc => mc.Cost);

            // Maintenance by type
            var maintenanceByType = maintenanceEvents
                .GroupBy(mc => mc.MaintenanceTypeEnum ?? EMaintenanceType.Routine)
                .Select(g => new MaintenanceByType
                {
                    MaintenanceType = g.Key,
                    TypeName = g.Key.ToString(),
                    EventCount = g.Count(),
                    TotalCost = g.Sum(mc => mc.Cost),
                    Percentage = totalCost > 0 ? g.Sum(mc => mc.Cost) / totalCost * 100 : 0
                })
                .OrderByDescending(m => m.TotalCost)
                .ToList();

            // Recent maintenance events
            var recentEvents = maintenanceEvents
                .OrderByDescending(mc => mc.ServiceDate)
                .Take(10)
                .Select(mc => new MaintenanceEvent
                {
                    MaintenanceId = mc.Id,
                    MaintenanceType = mc.MaintenanceTypeEnum ?? EMaintenanceType.Routine,
                    TypeName = (mc.MaintenanceTypeEnum ?? EMaintenanceType.Routine).ToString(),
                    Description = mc.Description,
                    Cost = mc.Cost,
                    ServiceDate = mc.ServiceDate,
                    ServiceProvider = mc.ServiceProvider,
                    OdometerReading = mc.OdometerReading
                })
                .ToList();

            return new MaintenanceSummary
            {
                TotalMaintenanceEvents = totalEvents,
                TotalMaintenanceCost = totalCost,
                MaintenanceByType = maintenanceByType,
                RecentMaintenanceEvents = recentEvents
            };
        }

        private async Task<FundStatus> GenerateFundStatusAsync(int? fundId, int vehicleId, DateTime startDate, DateTime endDate)
        {
            if (!fundId.HasValue)
            {
                return new FundStatus();
            }

            var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundId.Value);

            // Period income and expenses
            var periodIncome = await _unitOfWork.DbContext.FundAdditions
                .Where(fa => fa.FundId == fundId.Value &&
                    fa.CreatedAt >= startDate &&
                    fa.CreatedAt <= endDate &&
                    fa.StatusEnum == EFundAdditionStatus.Completed)
                .SumAsync(fa => fa.Amount);

            var periodExpenses = await _unitOfWork.DbContext.FundUsages
                .Where(fu => fu.FundId == fundId.Value &&
                    fu.CreatedAt >= startDate &&
                    fu.CreatedAt <= endDate)
                .SumAsync(fu => fu.Amount);

            // Total co-owners
            var totalCoOwners = await _unitOfWork.DbContext.VehicleCoOwners
                .CountAsync(vco => vco.VehicleId == vehicleId);

            var averageContribution = totalCoOwners > 0 ? periodIncome / totalCoOwners : 0;

            return new FundStatus
            {
                FundId = fundId.Value,
                CurrentBalance = fund?.CurrentBalance ?? 0,
                PeriodIncome = periodIncome,
                PeriodExpenses = periodExpenses,
                PeriodNetChange = periodIncome - periodExpenses,
                TotalCoOwners = totalCoOwners,
                AverageContributionPerCoOwner = averageContribution
            };
        }

        private async Task<MonthlyReportSummary> GenerateMonthlyReportSummaryAsync(int vehicleId, int? fundId, int year, int month, DateTime startDate, DateTime endDate)
        {
            var bookings = await _unitOfWork.DbContext.Bookings
                .Where(b => b.VehicleId == vehicleId &&
                    b.CreatedAt >= startDate &&
                    b.CreatedAt <= endDate)
                .ToListAsync();

            var totalHours = (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);

            decimal income = 0;
            decimal expenses = 0;

            if (fundId.HasValue)
            {
                income = await _unitOfWork.DbContext.FundAdditions
                    .Where(fa => fa.FundId == fundId.Value &&
                        fa.CreatedAt >= startDate &&
                        fa.CreatedAt <= endDate &&
                        fa.StatusEnum == EFundAdditionStatus.Completed)
                    .SumAsync(fa => fa.Amount);

                expenses = await _unitOfWork.DbContext.FundUsages
                    .Where(fu => fu.FundId == fundId.Value &&
                        fu.CreatedAt >= startDate &&
                        fu.CreatedAt <= endDate)
                    .SumAsync(fu => fu.Amount);
            }

            return new MonthlyReportSummary
            {
                Year = year,
                Month = month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                TotalBookings = bookings.Count,
                TotalHours = totalHours,
                TotalIncome = income,
                TotalExpenses = expenses,
                NetChange = income - expenses
            };
        }

        private async Task<QuarterlyReportSummary> GenerateQuarterlyReportSummaryAsync(int vehicleId, int? fundId, int year, int quarter, DateTime startDate, DateTime endDate)
        {
            var bookings = await _unitOfWork.DbContext.Bookings
                .Where(b => b.VehicleId == vehicleId &&
                    b.CreatedAt >= startDate &&
                    b.CreatedAt <= endDate)
                .ToListAsync();

            var totalHours = (decimal)bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);

            decimal income = 0;
            decimal expenses = 0;

            if (fundId.HasValue)
            {
                income = await _unitOfWork.DbContext.FundAdditions
                    .Where(fa => fa.FundId == fundId.Value &&
                        fa.CreatedAt >= startDate &&
                        fa.CreatedAt <= endDate &&
                        fa.StatusEnum == EFundAdditionStatus.Completed)
                    .SumAsync(fa => fa.Amount);

                expenses = await _unitOfWork.DbContext.FundUsages
                    .Where(fu => fu.FundId == fundId.Value &&
                        fu.CreatedAt >= startDate &&
                        fu.CreatedAt <= endDate)
                    .SumAsync(fu => fu.Amount);
            }

            return new QuarterlyReportSummary
            {
                Year = year,
                Quarter = quarter,
                QuarterName = $"Q{quarter}",
                TotalBookings = bookings.Count,
                TotalHours = totalHours,
                TotalIncome = income,
                TotalExpenses = expenses,
                NetChange = income - expenses
            };
        }

        private string GetQuarterMonthRange(int quarter)
        {
            return quarter switch
            {
                1 => "Jan-Mar",
                2 => "Apr-Jun",
                3 => "Jul-Sep",
                4 => "Oct-Dec",
                _ => ""
            };
        }

        private async Task<byte[]> ExportToPdfAsync(object reportData)
        {
            if (reportData is MonthlyReportResponse monthlyReport)
            {
                return await PdfExportHelper.ExportMonthlyReportToPdfAsync(monthlyReport);
            }
            else if (reportData is QuarterlyReportResponse quarterlyReport)
            {
                return await PdfExportHelper.ExportQuarterlyReportToPdfAsync(quarterlyReport);
            }
            else if (reportData is YearlyReportResponse yearlyReport)
            {
                return await PdfExportHelper.ExportYearlyReportToPdfAsync(yearlyReport);
            }

            throw new ArgumentException("Unsupported report type for PDF export");
        }

        private async Task<byte[]> ExportToExcelAsync(object reportData)
        {
            if (reportData is MonthlyReportResponse monthlyReport)
            {
                return await ExcelExportHelper.ExportMonthlyReportToExcelAsync(monthlyReport);
            }
            else if (reportData is QuarterlyReportResponse quarterlyReport)
            {
                return await ExcelExportHelper.ExportQuarterlyReportToExcelAsync(quarterlyReport);
            }
            else if (reportData is YearlyReportResponse yearlyReport)
            {
                return await ExcelExportHelper.ExportYearlyReportToExcelAsync(yearlyReport);
            }

            throw new ArgumentException("Unsupported report type for Excel export");
        }

        #endregion
    }
}
