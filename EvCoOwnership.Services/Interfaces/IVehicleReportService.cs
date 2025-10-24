using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ReportDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service interface for generating and exporting vehicle usage and cost reports
    /// </summary>
    public interface IVehicleReportService
    {
        /// <summary>
        /// Generate monthly report for a vehicle
        /// </summary>
        /// <param name="request">Monthly report request parameters</param>
        /// <param name="userId">User ID requesting the report</param>
        /// <returns>Comprehensive monthly report</returns>
        Task<BaseResponse<MonthlyReportResponse>> GenerateMonthlyReportAsync(GenerateMonthlyReportRequest request, Guid userId);

        /// <summary>
        /// Generate quarterly report for a vehicle
        /// </summary>
        /// <param name="request">Quarterly report request parameters</param>
        /// <param name="userId">User ID requesting the report</param>
        /// <returns>Comprehensive quarterly report</returns>
        Task<BaseResponse<QuarterlyReportResponse>> GenerateQuarterlyReportAsync(GenerateQuarterlyReportRequest request, Guid userId);

        /// <summary>
        /// Generate yearly report for a vehicle
        /// </summary>
        /// <param name="request">Yearly report request parameters</param>
        /// <param name="userId">User ID requesting the report</param>
        /// <returns>Comprehensive yearly report</returns>
        Task<BaseResponse<YearlyReportResponse>> GenerateYearlyReportAsync(GenerateYearlyReportRequest request, Guid userId);

        /// <summary>
        /// Export report to PDF or Excel format
        /// </summary>
        /// <param name="request">Export report request parameters</param>
        /// <param name="userId">User ID requesting the export</param>
        /// <returns>File download response with byte array</returns>
        Task<BaseResponse<ExportReportResponse>> ExportReportAsync(ExportReportRequest request, Guid userId);

        /// <summary>
        /// Get list of available report periods for a vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="userId">User ID requesting the list</param>
        /// <returns>List of available report periods</returns>
        Task<BaseResponse<AvailableReportsResponse>> GetAvailableReportPeriodsAsync(Guid vehicleId, Guid userId);
    }
}
