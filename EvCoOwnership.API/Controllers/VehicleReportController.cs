using EvCoOwnership.Repositories.DTOs.ReportDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class VehicleReportController : ControllerBase
    {
        private readonly IVehicleReportService _vehicleReportService;
        private readonly ILogger<VehicleReportController> _logger;

        public VehicleReportController(IVehicleReportService vehicleReportService, ILogger<VehicleReportController> logger)
        {
            _vehicleReportService = vehicleReportService;
            _logger = logger;
        }

        /// <summary>
        /// Generate monthly usage and cost report for a vehicle
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a comprehensive monthly report including:
        /// - Usage statistics (bookings, hours, distance)
        /// - Cost breakdown (income, expenses by category)
        /// - Maintenance summary
        /// - Fund status
        /// 
        /// Sample request:
        /// 
        ///     POST /api/reports/monthly
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025,
        ///         "month": 10
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Monthly report generated successfully</response>
        /// <response code="400">Invalid month value (must be 1-12)</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("monthly")]
        public async Task<IActionResult> GenerateMonthlyReport([FromBody] GenerateMonthlyReportRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleReportService.GenerateMonthlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Generate quarterly usage and cost report for a vehicle
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a comprehensive quarterly report including:
        /// - Usage statistics across 3 months
        /// - Cost breakdown with monthly trends
        /// - Maintenance summary
        /// - Fund status
        /// - Monthly breakdown within the quarter
        /// 
        /// Quarter mapping:
        /// - Q1: January - March
        /// - Q2: April - June
        /// - Q3: July - September
        /// - Q4: October - December
        /// 
        /// Sample request:
        /// 
        ///     POST /api/reports/quarterly
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025,
        ///         "quarter": 4
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Quarterly report generated successfully</response>
        /// <response code="400">Invalid quarter value (must be 1-4)</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("quarterly")]
        public async Task<IActionResult> GenerateQuarterlyReport([FromBody] GenerateQuarterlyReportRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleReportService.GenerateQuarterlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Generate yearly usage and cost report for a vehicle
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a comprehensive yearly report including:
        /// - Full year usage statistics
        /// - Complete cost analysis
        /// - Maintenance summary
        /// - Fund status
        /// - Quarterly breakdown
        /// - Monthly breakdown (all 12 months)
        /// 
        /// Sample request:
        /// 
        ///     POST /api/reports/yearly
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Yearly report generated successfully</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("yearly")]
        public async Task<IActionResult> GenerateYearlyReport([FromBody] GenerateYearlyReportRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleReportService.GenerateYearlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Export report to PDF or Excel format
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Exports a report in PDF or Excel format for download.
        /// 
        /// Report types (determined by parameters):
        /// - **Monthly**: Provide `month` parameter (1-12)
        /// - **Quarterly**: Provide `quarter` parameter (1-4)
        /// - **Yearly**: Provide neither `month` nor `quarter`
        /// 
        /// Export formats:
        /// - **PDF**: Standard PDF document
        /// - **Excel**: Excel spreadsheet (.xlsx)
        /// 
        /// Sample request (Monthly PDF):
        /// 
        ///     POST /api/reports/export
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025,
        ///         "month": 10,
        ///         "exportFormat": "PDF"
        ///     }
        /// 
        /// Sample request (Quarterly Excel):
        /// 
        ///     POST /api/reports/export
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025,
        ///         "quarter": 4,
        ///         "exportFormat": "Excel"
        ///     }
        /// 
        /// Sample request (Yearly PDF):
        /// 
        ///     POST /api/reports/export
        ///     {
        ///         "vehicleId": 1,
        ///         "year": 2025,
        ///         "exportFormat": "PDF"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Report exported successfully - returns file for download</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error or export failed</response>
        [HttpPost("export")]
        public async Task<IActionResult> ExportReport([FromBody] ExportReportRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleReportService.ExportReportAsync(request, userId);

            if (response.StatusCode == 200 && response.Data != null)
            {
                // Return file for download
                return File(
                    response.Data.FileContent,
                    response.Data.ContentType,
                    response.Data.FileName
                );
            }

            return response.StatusCode switch
            {
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Get list of available report periods for a vehicle
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Returns a list of all months that have data available for reporting,
        /// based on the vehicle's booking history.
        /// 
        /// Useful for:
        /// - Displaying available report periods in UI dropdowns
        /// - Determining date range of available data
        /// - Validating report generation requests
        /// 
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to check report periods for</param>
        /// <response code="200">Available report periods retrieved successfully</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/available-periods")]
        public async Task<IActionResult> GetAvailableReportPeriods(Guid vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _vehicleReportService.GetAvailableReportPeriodsAsync(vehicleId, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Get current month report for a vehicle (convenience endpoint)
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a report for the current month automatically.
        /// This is a convenience endpoint that doesn't require month/year parameters.
        /// 
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to generate report for</param>
        /// <response code="200">Current month report generated successfully</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/current-month")]
        public async Task<IActionResult> GetCurrentMonthReport(int vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var now = DateTime.UtcNow;
            var request = new GenerateMonthlyReportRequest
            {
                VehicleId = vehicleId,
                Year = now.Year,
                Month = now.Month
            };

            var response = await _vehicleReportService.GenerateMonthlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Get current quarter report for a vehicle (convenience endpoint)
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a report for the current quarter automatically.
        /// This is a convenience endpoint that doesn't require quarter/year parameters.
        /// 
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to generate report for</param>
        /// <response code="200">Current quarter report generated successfully</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/current-quarter")]
        public async Task<IActionResult> GetCurrentQuarterReport(int vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var now = DateTime.UtcNow;
            var currentQuarter = (now.Month - 1) / 3 + 1;

            var request = new GenerateQuarterlyReportRequest
            {
                VehicleId = vehicleId,
                Year = now.Year,
                Quarter = currentQuarter
            };

            var response = await _vehicleReportService.GenerateQuarterlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Get current year report for a vehicle (convenience endpoint)
        /// </summary>
        /// <remarks>
        /// **User**: Must be a co-owner of the vehicle
        /// 
        /// Generates a report for the current year automatically.
        /// This is a convenience endpoint that doesn't require year parameter.
        /// 
        /// </remarks>
        /// <param name="vehicleId">Vehicle ID to generate report for</param>
        /// <response code="200">Current year report generated successfully</response>
        /// <response code="403">User is not a co-owner of this vehicle</response>
        /// <response code="404">Vehicle not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId}/current-year")]
        public async Task<IActionResult> GetCurrentYearReport(int vehicleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var now = DateTime.UtcNow;
            var request = new GenerateYearlyReportRequest
            {
                VehicleId = vehicleId,
                Year = now.Year
            };

            var response = await _vehicleReportService.GenerateYearlyReportAsync(request, userId);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
