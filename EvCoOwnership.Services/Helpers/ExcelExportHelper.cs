using EvCoOwnership.Repositories.DTOs.ReportDTOs;
using System.Text;

namespace EvCoOwnership.Services.Helpers
{
    /// <summary>
    /// Helper class for exporting reports to Excel format
    /// </summary>
    public static class ExcelExportHelper
    {
        /// <summary>
        /// Export monthly report to Excel
        /// </summary>
        /// <param name="report">Monthly report data</param>
        /// <returns>Excel file as byte array</returns>
        public static async Task<byte[]> ExportMonthlyReportToExcelAsync(MonthlyReportResponse report)
        {
            // TODO: Implement with an Excel library like EPPlus, ClosedXML, or NPOI
            // This is a placeholder implementation that returns CSV-like data

            var sb = new StringBuilder();

            // Header information
            sb.AppendLine("EV Co-Ownership System - Monthly Report");
            sb.AppendLine($"Vehicle,{report.VehicleName}");
            sb.AppendLine($"Period,{report.PeriodDescription}");
            sb.AppendLine($"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Usage Summary
            sb.AppendLine("USAGE SUMMARY");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Total Bookings,{report.UsageSummary.TotalBookings}");
            sb.AppendLine($"Completed Bookings,{report.UsageSummary.CompletedBookings}");
            sb.AppendLine($"Cancelled Bookings,{report.UsageSummary.CancelledBookings}");
            sb.AppendLine($"Total Hours Used,{report.UsageSummary.TotalHoursUsed:F2}");
            sb.AppendLine($"Total Distance (km),{report.UsageSummary.TotalDistanceTraveled:F2}");
            sb.AppendLine($"Average Usage per Booking,{report.UsageSummary.AverageUsagePerBooking:F2}");
            sb.AppendLine();

            // Usage by Co-Owner
            if (report.UsageSummary.UsageByCoOwner.Any())
            {
                sb.AppendLine("USAGE BY CO-OWNER");
                sb.AppendLine("User Name,Email,Bookings,Total Hours,Usage %,Total Cost");
                foreach (var usage in report.UsageSummary.UsageByCoOwner)
                {
                    sb.AppendLine($"{usage.UserName},{usage.UserEmail},{usage.BookingCount}," +
                        $"{usage.TotalHours:F2},{usage.UsagePercentage:F1},{usage.TotalCost:F2}");
                }
                sb.AppendLine();
            }

            // Cost Summary
            sb.AppendLine("COST SUMMARY");
            sb.AppendLine("Metric,Amount");
            sb.AppendLine($"Total Income,{report.CostSummary.TotalIncome:F2}");
            sb.AppendLine($"Total Expenses,{report.CostSummary.TotalExpenses:F2}");
            sb.AppendLine($"Net Balance,{report.CostSummary.NetBalance:F2}");
            sb.AppendLine($"Opening Balance,{report.CostSummary.OpeningBalance:F2}");
            sb.AppendLine($"Closing Balance,{report.CostSummary.ClosingBalance:F2}");
            sb.AppendLine();

            // Expenses by Category
            if (report.CostSummary.ExpensesByCategory.Any())
            {
                sb.AppendLine("EXPENSES BY CATEGORY");
                sb.AppendLine("Category,Amount,Transaction Count,Percentage");
                foreach (var expense in report.CostSummary.ExpensesByCategory)
                {
                    sb.AppendLine($"{expense.CategoryName},{expense.Amount:F2}," +
                        $"{expense.TransactionCount},{expense.Percentage:F1}");
                }
                sb.AppendLine();
            }

            // Maintenance Summary
            sb.AppendLine("MAINTENANCE SUMMARY");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Total Events,{report.MaintenanceSummary.TotalMaintenanceEvents}");
            sb.AppendLine($"Total Cost,{report.MaintenanceSummary.TotalMaintenanceCost:F2}");
            sb.AppendLine();

            // Maintenance by Type
            if (report.MaintenanceSummary.MaintenanceByType.Any())
            {
                sb.AppendLine("MAINTENANCE BY TYPE");
                sb.AppendLine("Type,Event Count,Total Cost,Percentage");
                foreach (var maintenance in report.MaintenanceSummary.MaintenanceByType)
                {
                    sb.AppendLine($"{maintenance.TypeName},{maintenance.EventCount}," +
                        $"{maintenance.TotalCost:F2},{maintenance.Percentage:F1}");
                }
                sb.AppendLine();
            }

            // Fund Status
            sb.AppendLine("FUND STATUS");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Current Balance,{report.FundStatus.CurrentBalance:F2}");
            sb.AppendLine($"Period Income,{report.FundStatus.PeriodIncome:F2}");
            sb.AppendLine($"Period Expenses,{report.FundStatus.PeriodExpenses:F2}");
            sb.AppendLine($"Period Net Change,{report.FundStatus.PeriodNetChange:F2}");
            sb.AppendLine($"Total Co-Owners,{report.FundStatus.TotalCoOwners}");
            sb.AppendLine($"Average Contribution per Co-Owner,{report.FundStatus.AverageContributionPerCoOwner:F2}");

            await Task.CompletedTask;
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Export quarterly report to Excel
        /// </summary>
        /// <param name="report">Quarterly report data</param>
        /// <returns>Excel file as byte array</returns>
        public static async Task<byte[]> ExportQuarterlyReportToExcelAsync(QuarterlyReportResponse report)
        {
            // TODO: Implement with an Excel library
            var sb = new StringBuilder();

            sb.AppendLine("EV Co-Ownership System - Quarterly Report");
            sb.AppendLine($"Vehicle,{report.VehicleName}");
            sb.AppendLine($"Period,{report.PeriodDescription}");
            sb.AppendLine($"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("QUARTERLY SUMMARY");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Total Bookings,{report.UsageSummary.TotalBookings}");
            sb.AppendLine($"Total Hours,{report.UsageSummary.TotalHoursUsed:F2}");
            sb.AppendLine($"Total Distance (km),{report.UsageSummary.TotalDistanceTraveled:F2}");
            sb.AppendLine($"Total Income,{report.CostSummary.TotalIncome:F2}");
            sb.AppendLine($"Total Expenses,{report.CostSummary.TotalExpenses:F2}");
            sb.AppendLine($"Net Balance,{report.CostSummary.NetBalance:F2}");
            sb.AppendLine();

            if (report.MonthlyBreakdown.Any())
            {
                sb.AppendLine("MONTHLY BREAKDOWN");
                sb.AppendLine("Month,Bookings,Hours,Income,Expenses,Net Change");
                foreach (var month in report.MonthlyBreakdown)
                {
                    sb.AppendLine($"{month.MonthName},{month.TotalBookings},{month.TotalHours:F2}," +
                        $"{month.TotalIncome:F2},{month.TotalExpenses:F2},{month.NetChange:F2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("FUND STATUS");
            sb.AppendLine("Current Balance,Total Co-Owners");
            sb.AppendLine($"{report.FundStatus.CurrentBalance:F2},{report.FundStatus.TotalCoOwners}");

            await Task.CompletedTask;
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Export yearly report to Excel
        /// </summary>
        /// <param name="report">Yearly report data</param>
        /// <returns>Excel file as byte array</returns>
        public static async Task<byte[]> ExportYearlyReportToExcelAsync(YearlyReportResponse report)
        {
            // TODO: Implement with an Excel library
            var sb = new StringBuilder();

            sb.AppendLine("EV Co-Ownership System - Yearly Report");
            sb.AppendLine($"Vehicle,{report.VehicleName}");
            sb.AppendLine($"Period,{report.PeriodDescription}");
            sb.AppendLine($"Generated,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("YEARLY SUMMARY");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Total Bookings,{report.UsageSummary.TotalBookings}");
            sb.AppendLine($"Total Hours,{report.UsageSummary.TotalHoursUsed:F2}");
            sb.AppendLine($"Total Distance (km),{report.UsageSummary.TotalDistanceTraveled:F2}");
            sb.AppendLine($"Total Income,{report.CostSummary.TotalIncome:F2}");
            sb.AppendLine($"Total Expenses,{report.CostSummary.TotalExpenses:F2}");
            sb.AppendLine($"Net Balance,{report.CostSummary.NetBalance:F2}");
            sb.AppendLine();

            if (report.QuarterlyBreakdown.Any())
            {
                sb.AppendLine("QUARTERLY BREAKDOWN");
                sb.AppendLine("Quarter,Bookings,Hours,Income,Expenses,Net Change");
                foreach (var quarter in report.QuarterlyBreakdown)
                {
                    sb.AppendLine($"{quarter.QuarterName},{quarter.TotalBookings},{quarter.TotalHours:F2}," +
                        $"{quarter.TotalIncome:F2},{quarter.TotalExpenses:F2},{quarter.NetChange:F2}");
                }
                sb.AppendLine();
            }

            if (report.MonthlyBreakdown.Any())
            {
                sb.AppendLine("MONTHLY BREAKDOWN");
                sb.AppendLine("Month,Bookings,Hours,Income,Expenses,Net Change");
                foreach (var month in report.MonthlyBreakdown)
                {
                    sb.AppendLine($"{month.MonthName},{month.TotalBookings},{month.TotalHours:F2}," +
                        $"{month.TotalIncome:F2},{month.TotalExpenses:F2},{month.NetChange:F2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("FUND STATUS");
            sb.AppendLine("Current Balance,Total Co-Owners,Average Contribution");
            sb.AppendLine($"{report.FundStatus.CurrentBalance:F2},{report.FundStatus.TotalCoOwners}," +
                $"{report.FundStatus.AverageContributionPerCoOwner:F2}");

            await Task.CompletedTask;
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
