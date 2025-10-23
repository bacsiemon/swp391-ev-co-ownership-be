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

        /// <summary>
        /// Creates a new fund usage record (expense)
        /// </summary>
        /// <param name="request">Fund usage creation request</param>
        /// <response code="201">Fund usage created successfully. Possible messages:  
        /// - FUND_USAGE_CREATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - INVALID_AMOUNT  
        /// - INSUFFICIENT_FUND_BALANCE  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - VEHICLE_NOT_FOUND  
        /// - FUND_NOT_FOUND_FOR_VEHICLE  
        /// - FUND_NOT_FOUND  
        /// - MAINTENANCE_COST_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **CREATE FUND USAGE (EXPENSE) RECORD**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Purpose:**
        /// Record expenses from the vehicle fund with automatic balance deduction.
        /// 
        /// **Categories:**
        /// - **Maintenance** (0): Regular or emergency maintenance
        /// - **Insurance** (1): Insurance premium payments
        /// - **Fuel** (2): Charging/fuel expenses
        /// - **Parking** (3): Parking and storage fees
        /// - **Other** (4): Miscellaneous expenses
        /// 
        /// **Validation:**
        /// - Amount must be positive
        /// - Fund balance must be sufficient
        /// - Maintenance cost ID must exist if provided
        /// 
        /// **Sample Request:**  
        /// ```json
        /// {
        ///   "vehicleId": 1,
        ///   "usageType": 0,
        ///   "amount": 1500000,
        ///   "description": "Brake pad replacement",
        ///   "imageUrl": "https://storage.example.com/receipts/receipt123.jpg",
        ///   "maintenanceCostId": 45
        /// }
        /// ```
        /// </remarks>
        [HttpPost("usage")]
        public async Task<IActionResult> CreateFundUsage([FromBody] CreateFundUsageRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.CreateFundUsageAsync(request, userId);

                return response.StatusCode switch
                {
                    201 => CreatedAtAction(nameof(GetFundUsages), new { vehicleId = request.VehicleId }, response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFundUsage");
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Updates an existing fund usage record
        /// </summary>
        /// <param name="usageId">ID of the fund usage to update</param>
        /// <param name="request">Update request</param>
        /// <response code="200">Fund usage updated successfully. Possible messages:  
        /// - FUND_USAGE_UPDATED_SUCCESSFULLY  
        /// </response>
        /// <response code="400">Bad request. Possible messages:  
        /// - INVALID_AMOUNT  
        /// - INSUFFICIENT_FUND_BALANCE  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - FUND_USAGE_NOT_FOUND  
        /// - FUND_NOT_FOUND  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **UPDATE FUND USAGE RECORD**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Updatable Fields:**
        /// - Usage type (category)
        /// - Amount (fund balance auto-adjusted)
        /// - Description
        /// - Image URL
        /// - Maintenance cost ID link
        /// 
        /// **Sample Request:**  
        /// ```json
        /// {
        ///   "amount": 1800000,
        ///   "description": "Brake pad and rotor replacement (updated)"
        /// }
        /// ```
        /// </remarks>
        [HttpPut("usage/{usageId}")]
        public async Task<IActionResult> UpdateFundUsage(int usageId, [FromBody] UpdateFundUsageRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.UpdateFundUsageAsync(usageId, request, userId);

                return response.StatusCode switch
                {
                    200 => Ok(response),
                    400 => BadRequest(response),
                    403 => StatusCode(403, response),
                    404 => NotFound(response),
                    _ => StatusCode(500, response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateFundUsage for usage {UsageId}", usageId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Deletes a fund usage record and refunds amount to fund
        /// </summary>
        /// <param name="usageId">ID of the fund usage to delete</param>
        /// <response code="200">Fund usage deleted successfully. Possible messages:  
        /// - FUND_USAGE_DELETED_SUCCESSFULLY  
        /// </response>
        /// <response code="403">Access denied. Possible messages:  
        /// - ACCESS_DENIED_NOT_VEHICLE_CO_OWNER  
        /// </response>
        /// <response code="404">Not found. Possible messages:  
        /// - FUND_USAGE_NOT_FOUND  
        /// - FUND_NOT_FOUND  
        /// - VEHICLE_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error</response>
        /// <remarks>
        /// **DELETE FUND USAGE RECORD**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Effects:**
        /// - Deletes the usage record
        /// - Refunds the amount back to fund balance
        /// - Updates fund's UpdatedAt timestamp
        /// 
        /// **Use Cases:**
        /// - Correcting mistaken entries
        /// - Removing duplicate records
        /// - Cancelling unverified expenses
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FUND_USAGE_DELETED_SUCCESSFULLY",
        ///   "data": {
        ///     "deletedId": 201,
        ///     "refundedAmount": 1500000
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpDelete("usage/{usageId}")]
        public async Task<IActionResult> DeleteFundUsage(int usageId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.DeleteFundUsageAsync(usageId, userId);

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
                _logger.LogError(ex, "Error in DeleteFundUsage for usage {UsageId}", usageId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets fund usages filtered by category type
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="category">Usage category (0=Maintenance, 1=Insurance, 2=Fuel, 3=Parking, 4=Other)</param>
        /// <param name="startDate">Optional start date filter (ISO 8601)</param>
        /// <param name="endDate">Optional end date filter (ISO 8601)</param>
        /// <response code="200">Category usages retrieved successfully. Possible messages:  
        /// - FUND_USAGES_BY_CATEGORY_RETRIEVED_SUCCESSFULLY  
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
        /// **GET FUND USAGES BY CATEGORY**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Category Enum Values:**
        /// - **0**: Maintenance
        /// - **1**: Insurance
        /// - **2**: Fuel (Charging)
        /// - **3**: Parking
        /// - **4**: Other
        /// 
        /// **Optional Date Filtering:**
        /// - Filter by date range using startDate and/or endDate
        /// - Format: ISO 8601 (e.g., 2024-10-01T00:00:00Z)
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/category/1/usages/0?startDate=2024-10-01&amp;endDate=2024-10-31
        /// ```
        /// 
        /// **Use Cases:**
        /// - View all maintenance expenses
        /// - Analyze insurance payment history
        /// - Track fuel/charging costs over time
        /// </remarks>
        [HttpGet("category/{vehicleId}/usages/{category}")]
        public async Task<IActionResult> GetFundUsagesByCategory(
            int vehicleId, 
            int category,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (!Enum.IsDefined(typeof(EvCoOwnership.Repositories.Enums.EUsageType), category))
                {
                    return BadRequest(new { message = "INVALID_CATEGORY" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var usageType = (EvCoOwnership.Repositories.Enums.EUsageType)category;
                var response = await _fundService.GetFundUsagesByCategoryAsync(vehicleId, usageType, userId, startDate, endDate);

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
                _logger.LogError(ex, "Error in GetFundUsagesByCategory for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }

        /// <summary>
        /// Gets category-based budget analysis for current month
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <response code="200">Budget analysis retrieved successfully. Possible messages:  
        /// - CATEGORY_BUDGET_ANALYSIS_RETRIEVED_SUCCESSFULLY  
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
        /// **CATEGORY BUDGET ANALYSIS**
        /// 
        /// **Access Control:**
        /// - Co-owners of the vehicle
        /// - Admin/Staff
        /// 
        /// **Provides:**
        /// - Per-category budget limits
        /// - Current month spending by category
        /// - Remaining budget per category
        /// - Budget utilization percentage
        /// - Budget status (OnTrack/Warning/Exceeded)
        /// - Transaction count and average amount
        /// - Overall monthly budget utilization
        /// 
        /// **Default Monthly Budget Limits:**
        /// - Maintenance: 3,000,000 VND
        /// - Insurance: 1,000,000 VND
        /// - Fuel: 2,000,000 VND
        /// - Parking: 500,000 VND
        /// - Other: 1,000,000 VND
        /// - **Total: 7,500,000 VND/month**
        /// 
        /// **Budget Status Logic:**
        /// - **OnTrack**: Spending &lt; 80% of budget
        /// - **Warning**: Spending 80-100% of budget
        /// - **Exceeded**: Spending &gt; budget
        /// 
        /// **Sample Request:**  
        /// ```
        /// GET /api/fund/category/1/analysis
        /// ```
        /// 
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "CATEGORY_BUDGET_ANALYSIS_RETRIEVED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "analysisMonth": 10,
        ///     "analysisYear": 2024,
        ///     "categoryBudgets": [
        ///       {
        ///         "category": "Maintenance",
        ///         "monthlyBudgetLimit": 3000000,
        ///         "currentMonthSpending": 2500000,
        ///         "remainingBudget": 500000,
        ///         "budgetUtilizationPercent": 83.33,
        ///         "budgetStatus": "Warning",
        ///         "transactionCount": 3,
        ///         "averageTransactionAmount": 833333
        ///       }
        ///     ],
        ///     "totalBudget": 7500000,
        ///     "totalSpending": 5200000,
        ///     "overallUtilizationPercent": 69.33
        ///   }
        /// }
        /// ```
        /// 
        /// **Use Cases:**
        /// - Monthly budget monitoring
        /// - Expense category analysis
        /// - Overspending detection
        /// - Financial planning
        /// </remarks>
        [HttpGet("category/{vehicleId}/analysis")]
        public async Task<IActionResult> GetCategoryBudgetAnalysis(int vehicleId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var response = await _fundService.GetCategoryBudgetAnalysisAsync(vehicleId, userId);

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
                _logger.LogError(ex, "Error in GetCategoryBudgetAnalysis for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { message = "INTERNAL_SERVER_ERROR" });
            }
        }
    }
}
