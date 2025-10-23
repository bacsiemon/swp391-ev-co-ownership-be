using EvCoOwnership.Repositories.DTOs.FundDTOs;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for fund management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FundController : ControllerBase
    {
        private readonly IFundService _fundService;
        private readonly ILogger<FundController> _logger;

        /// <summary>
        /// Initializes a new instance of the FundController
        /// </summary>
        public FundController(IFundService fundService, ILogger<FundController> logger)
        {
            _fundService = fundService;
            _logger = logger;
        }

        /// <summary>
        /// Gets current fund balance for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <response code="200">Fund balance retrieved successfully. Possible messages:  
        /// - FUND_BALANCE_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - FUND_NOT_FOUND_FOR_VEHICLE  
        /// - FUND_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// **VIEW FUND BALANCE - Role-Based Access**
        /// 
        /// **Access Control:**
        /// - **Co-owners**: Can view fund balance of their vehicles
        /// - **Admin/Staff**: Can view any vehicle's fund balance
        /// 
        /// **Response Includes:**
        /// - Current balance amount
        /// - Total amounts added and used
        /// - Number of additions and usages
        /// - Balance status (Healthy/Warning/Low)
        /// - Recommended minimum balance (based on 2x average monthly expenses)
        /// - Fund creation and last update timestamps
        /// 
        /// **Balance Status:**
        /// - **Healthy**: Balance ≥ 1.5x recommended minimum
        /// - **Warning**: Balance between 1x and 1.5x recommended minimum
        /// - **Low**: Balance below recommended minimum
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/balance/1
        /// Authorization: Bearer {token}
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FUND_BALANCE_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "fundId": 5,
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "licensePlate": "51A-12345",
        ///     "currentBalance": 8500000,
        ///     "totalAddedAmount": 15000000,
        ///     "totalUsedAmount": 6500000,
        ///     "totalAdditions": 12,
        ///     "totalUsages": 8,
        ///     "createdAt": "2024-01-15T10:30:00Z",
        ///     "updatedAt": "2024-10-20T14:25:00Z",
        ///     "balanceStatus": "Healthy",
        ///     "recommendedMinBalance": 5000000
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpGet("balance/{vehicleId}")]
        public async Task<IActionResult> GetFundBalance(int vehicleId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundBalanceAsync(vehicleId, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundBalance for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets fund additions (deposits) history for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <response code="200">Fund additions retrieved successfully. Possible messages:  
        /// - FUND_ADDITIONS_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - FUND_NOT_FOUND_FOR_VEHICLE  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **VIEW FUND ADDITIONS HISTORY**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Returns list of fund deposits with:**
        /// - Deposit amount and payment method
        /// - Co-owner who made the deposit
        /// - Transaction ID and status
        /// - Deposit description
        /// - Created timestamp
        /// 
        /// **Pagination:**
        /// - Default page size: 20 records
        /// - Maximum page size: 100 records
        /// - Results ordered by most recent first
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/additions/1?pageNumber=1&amp;pageSize=10
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FUND_ADDITIONS_RETRIEVED_SUCCESSFULLY",
        ///   "data": [
        ///     {
        ///       "id": 101,
        ///       "fundId": 5,
        ///       "coOwnerId": 3,
        ///       "coOwnerName": "John Doe",
        ///       "amount": 2000000,
        ///       "paymentMethod": "BankTransfer",
        ///       "transactionId": "TXN123456789",
        ///       "description": "Monthly contribution",
        ///       "status": "Completed",
        ///       "createdAt": "2024-10-20T10:00:00Z"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpGet("additions/{vehicleId}")]
        public async Task<IActionResult> GetFundAdditions(
            int vehicleId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundAdditionsAsync(vehicleId, userId, pageNumber, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundAdditions for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets fund usages (expenses) history for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <response code="200">Fund usages retrieved successfully. Possible messages:  
        /// - FUND_USAGES_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - FUND_NOT_FOUND_FOR_VEHICLE  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **VIEW FUND USAGES (EXPENSES) HISTORY**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Returns list of fund expenses with:**
        /// - Usage amount and type (Maintenance, Insurance, Fuel, Parking, Other)
        /// - Expense description
        /// - Image proof URL (if available)
        /// - Linked maintenance cost ID (if applicable)
        /// - Created timestamp
        /// 
        /// **Usage Types:**
        /// - **Maintenance**: Regular or emergency vehicle maintenance
        /// - **Insurance**: Insurance premium payments
        /// - **Fuel**: Charging/fuel expenses
        /// - **Parking**: Parking and storage fees
        /// - **Other**: Miscellaneous expenses
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/usages/1?pageNumber=1&amp;pageSize=10
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FUND_USAGES_RETRIEVED_SUCCESSFULLY",
        ///   "data": [
        ///     {
        ///       "id": 201,
        ///       "fundId": 5,
        ///       "usageType": "Maintenance",
        ///       "amount": 1500000,
        ///       "description": "Brake pad replacement",
        ///       "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
        ///       "maintenanceCostId": 45,
        ///       "createdAt": "2024-10-18T14:30:00Z"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpGet("usages/{vehicleId}")]
        public async Task<IActionResult> GetFundUsages(
            int vehicleId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundUsagesAsync(vehicleId, userId, pageNumber, pageSize);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundUsages for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets comprehensive fund summary with balance, history, and statistics
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="monthsToAnalyze">Number of months to analyze for statistics (default: 6, max: 24)</param>
        /// <response code="200">Fund summary retrieved successfully. Possible messages:  
        /// - FUND_SUMMARY_RETRIEVED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - FUND_NOT_FOUND_FOR_VEHICLE  
        /// - FUND_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **VIEW COMPREHENSIVE FUND SUMMARY**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Includes all fund information:**
        /// 1. **Current Balance**: Same as GET /api/fund/balance/{vehicleId}
        /// 2. **Recent Additions**: Last 10 deposits
        /// 3. **Recent Usages**: Last 10 expenses
        /// 4. **Fund Statistics**:
        ///    - Average monthly addition
        ///    - Average monthly usage
        ///    - Net monthly cash flow
        ///    - Months covered by current balance
        ///    - Usage breakdown by type (Maintenance, Insurance, etc.)
        ///    - Month-by-month cash flow history
        /// 
        /// **Use Cases:**
        /// - Dashboard overview
        /// - Financial planning
        /// - Budget analysis
        /// - Expense pattern identification
        /// - Fund health monitoring
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/summary/1?monthsToAnalyze=6
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FUND_SUMMARY_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "balance": {
        ///       "fundId": 5,
        ///       "vehicleId": 1,
        ///       "currentBalance": 8500000,
        ///       "balanceStatus": "Healthy",
        ///       "recommendedMinBalance": 5000000
        ///     },
        ///     "recentAdditions": [ /* last 10 additions */ ],
        ///     "recentUsages": [ /* last 10 usages */ ],
        ///     "statistics": {
        ///       "averageMonthlyAddition": 2500000,
        ///       "averageMonthlyUsage": 1800000,
        ///       "netMonthlyFlow": 700000,
        ///       "monthsCovered": 4,
        ///       "usageByType": {
        ///         "Maintenance": 3500000,
        ///         "Insurance": 2000000,
        ///         "Fuel": 1200000,
        ///         "Parking": 500000
        ///       },
        ///       "monthlyFlows": [
        ///         {
        ///           "year": 2024,
        ///           "month": 10,
        ///           "totalAdded": 3000000,
        ///           "totalUsed": 2200000,
        ///           "netFlow": 800000,
        ///           "endingBalance": 0
        ///         }
        ///       ]
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpGet("summary/{vehicleId}")]
        public async Task<IActionResult> GetFundSummary(
            int vehicleId, 
            [FromQuery] int monthsToAnalyze = 6)
        {
            try
            {
                // Validate monthsToAnalyze
                if (monthsToAnalyze < 1) monthsToAnalyze = 6;
                if (monthsToAnalyze > 24) monthsToAnalyze = 24;

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetFundSummaryAsync(vehicleId, userId, monthsToAnalyze);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFundSummary for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }
    }
}
