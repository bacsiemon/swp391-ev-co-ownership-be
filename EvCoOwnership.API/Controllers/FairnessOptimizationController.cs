using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.FairnessOptimizationDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for AI-powered fairness analysis and cost optimization
    /// </summary>
    /// <remarks>
    /// **Role Required**: CoOwner
    /// 
    /// This controller provides intelligent recommendations for:
    /// - Usage fairness analysis (comparing actual usage vs ownership percentages)
    /// - Fair scheduling suggestions (optimal booking slots for balanced usage)
    /// - Predictive maintenance recommendations (based on vehicle health and usage patterns)
    /// - Cost-saving opportunities (fund optimization, maintenance scheduling, etc.)
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CoOwner")]
    public class FairnessOptimizationController : ControllerBase
    {
        private readonly IFairnessOptimizationService _fairnessOptimizationService;

        public FairnessOptimizationController(IFairnessOptimizationService fairnessOptimizationService)
        {
            _fairnessOptimizationService = fairnessOptimizationService;
        }

        /// <summary>
        /// Generates comprehensive fairness report for a vehicle
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle to analyze</param>
        /// <param name="startDate">Optional: Analysis start date (defaults to vehicle creation date)</param>
        /// <param name="endDate">Optional: Analysis end date (defaults to current date)</param>
        /// <param name="includeRecommendations">Optional: Include actionable recommendations (default: true)</param>
        /// <remarks>
        /// **Role Required**: CoOwner
        /// 
        /// **Authorization**: User must be an active co-owner of the vehicle
        /// 
        /// **Description**:  
        /// Analyzes usage patterns across all co-owners and compares them with ownership percentages.
        /// The report includes:
        /// - Overall fairness score (0-100) and status (Excellent/Good/Fair/Poor)
        /// - Per co-owner analysis showing usage vs ownership deltas
        /// - Classification of co-owners as Balanced, Overutilized, or Underutilized
        /// - Cost allocation recommendations based on actual usage
        /// - Actionable recommendations to improve fairness
        /// 
        /// **Usage Metrics Analyzed**:
        /// - Hours: Total booking duration
        /// - Distance: Odometer-based km traveled
        /// - Bookings: Number of bookings made
        /// 
        /// **Fairness Classification**:
        /// - **Balanced**: Usage within ±5% of ownership percentage
        /// - **Overutilized**: Usage exceeds ownership by >5%
        /// - **Underutilized**: Usage below ownership by >5%
        /// 
        /// **Sample Request**:
        /// ```
        /// GET /api/fairnessoptimization/vehicle/1/fairness-report?startDate=2024-01-01&amp;endDate=2024-10-23&amp;includeRecommendations=true
        /// ```
        /// 
        /// **Sample Response**:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "FAIRNESS_REPORT_GENERATED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "overview": {
        ///       "overallFairnessStatus": "Good",
        ///       "fairnessScore": 78.5,
        ///       "balancedCoOwnersCount": 1,
        ///       "overutilizedCoOwnersCount": 1,
        ///       "underutilizedCoOwnersCount": 1
        ///     },
        ///     "coOwnersDetails": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "coOwnerName": "John Doe",
        ///         "ownershipPercentage": 40.00,
        ///         "averageUsagePercentage": 55.50,
        ///         "usageVsOwnershipDelta": 15.50,
        ///         "usagePattern": "Overutilized",
        ///         "fairnessScore": 70,
        ///         "expectedCostShare": 4000000,
        ///         "actualCostShare": 5550000,
        ///         "costAdjustmentNeeded": 1550000,
        ///         "recommendations": [
        ///           "Consider reducing usage by 15.5% to match ownership share",
        ///           "Additional cost contribution of 1,550,000 VND may be fair"
        ///         ]
        ///       }
        ///     ],
        ///     "recommendations": [
        ///       {
        ///         "type": "Usage",
        ///         "priority": "High",
        ///         "title": "Rebalance Usage Distribution",
        ///         "description": "1 co-owner(s) are overutilizing while 1 are underutilizing",
        ///         "actionItems": [
        ///           "Schedule group meeting to discuss fair usage",
        ///           "Implement booking rotation system"
        ///         ],
        ///         "affectedCoOwnerIds": [1, 2]
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Fairness report generated successfully. Message: FAIRNESS_REPORT_GENERATED_SUCCESSFULLY</response>
        /// <response code="403">User not authorized to view this vehicle's data. Message: NOT_AUTHORIZED_TO_VIEW_VEHICLE_FAIRNESS_REPORT</response>
        /// <response code="404">Vehicle not found. Message: VEHICLE_NOT_FOUND</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId:int}/fairness-report")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetFairnessReport(
            int vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool includeRecommendations = true)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var request = new GetFairnessReportRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                IncludeRecommendations = includeRecommendations
            };

            var response = await _fairnessOptimizationService.GetFairnessReportAsync(vehicleId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        /// <summary>
        /// Suggests optimal booking schedule for fair usage distribution
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="startDate">Schedule period start date (must be in future)</param>
        /// <param name="endDate">Schedule period end date</param>
        /// <param name="preferredDurationHours">Optional: Preferred booking duration in hours (default: 4)</param>
        /// <param name="usageType">Optional: Type of usage (Maintenance, Insurance, Fuel, Parking, Other)</param>
        /// <remarks>
        /// **Role Required**: CoOwner
        /// 
        /// **Authorization**: User must be an active co-owner of the vehicle
        /// 
        /// **Description**:  
        /// Analyzes historical booking patterns and generates AI-powered scheduling suggestions
        /// to help achieve fair usage distribution among all co-owners.
        /// 
        /// **Features**:
        /// - Personalized booking suggestions for each co-owner based on usage gap
        /// - Optimal time slots identified using historical data analysis
        /// - Conflict probability calculation for each suggested slot
        /// - Peak and off-peak usage period identification
        /// - Scheduling insights to maximize vehicle utilization
        /// 
        /// **Algorithm**:
        /// 1. Calculates current usage percentage per co-owner
        /// 2. Compares with ownership percentage to identify gaps
        /// 3. Recommends bookings to close usage gaps
        /// 4. Optimizes slot timing based on historical patterns
        /// 5. Minimizes booking conflicts
        /// 
        /// **Sample Request**:
        /// ```
        /// GET /api/fairnessoptimization/vehicle/1/schedule-suggestions
        ///     ?startDate=2024-11-01&amp;endDate=2024-11-30
        ///     &amp;preferredDurationHours=4&amp;usageType=Maintenance
        /// ```
        /// 
        /// **Sample Response**:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "SCHEDULE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "coOwnerSuggestions": [
        ///       {
        ///         "coOwnerId": 1,
        ///         "coOwnerName": "John Doe",
        ///         "ownershipPercentage": 40.00,
        ///         "currentUsagePercentage": 25.00,
        ///         "recommendedUsagePercentage": 40.00,
        ///         "suggestedBookingsCount": 5,
        ///         "suggestedTotalHours": 20.0,
        ///         "suggestedSlots": [
        ///           {
        ///             "startTime": "2024-11-05T08:00:00Z",
        ///             "endTime": "2024-11-05T12:00:00Z",
        ///             "durationHours": 4.0,
        ///             "reason": "Optimal morning slot on Tuesday for maintenance",
        ///             "conflictProbability": 0.15,
        ///             "benefits": ["Low conflict risk", "Balanced usage distribution"]
        ///           }
        ///         ],
        ///         "rationale": "You're currently underutilizing by 15.0%. Suggested bookings will help you use your fair share."
        ///       }
        ///     ],
        ///     "optimalTimeSlots": [
        ///       {
        ///         "dayOfWeek": "Tuesday",
        ///         "startTime": "06:00:00",
        ///         "endTime": "12:00:00",
        ///         "utilizationRate": 20.5,
        ///         "peakType": "Low",
        ///         "recommendedForCoOwnerIds": [1, 2, 3]
        ///       }
        ///     ],
        ///     "insights": {
        ///       "currentUtilizationRate": 35.2,
        ///       "optimalUtilizationRate": 40.0,
        ///       "conflictingBookingsCount": 3,
        ///       "peakUsagePeriods": ["Weekends", "Weekday evenings"],
        ///       "underutilizedPeriods": ["Weekday mornings", "Tuesday-Thursday afternoons"],
        ///       "potentialEfficiencyGain": 4.8
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Schedule suggestions generated successfully. Message: SCHEDULE_SUGGESTIONS_GENERATED_SUCCESSFULLY</response>
        /// <response code="400">Validation error. Possible messages:
        /// - START_DATE_REQUIRED
        /// - END_DATE_REQUIRED
        /// - START_DATE_MUST_BE_BEFORE_END_DATE
        /// - END_DATE_MUST_BE_IN_FUTURE
        /// - DURATION_MUST_BE_POSITIVE
        /// </response>
        /// <response code="403">User not authorized. Message: NOT_AUTHORIZED_TO_VIEW_SCHEDULE_SUGGESTIONS</response>
        /// <response code="404">Vehicle not found. Message: VEHICLE_NOT_FOUND</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId:int}/schedule-suggestions")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetFairScheduleSuggestions(
            int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? preferredDurationHours = null,
            [FromQuery] EUsageType? usageType = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var request = new GetFairScheduleSuggestionsRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                PreferredDurationHours = preferredDurationHours,
                UsageType = usageType
            };

            var response = await _fairnessOptimizationService.GetFairScheduleSuggestionsAsync(vehicleId, userId, request);

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
        /// Provides predictive maintenance suggestions based on vehicle health and usage
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="includePredictive">Optional: Include AI-based predictive analysis (default: true)</param>
        /// <param name="lookaheadDays">Optional: Forecast period in days (default: 30, max: 365)</param>
        /// <remarks>
        /// **Role Required**: CoOwner
        /// 
        /// **Authorization**: User must be an active co-owner of the vehicle
        /// 
        /// **Description**:  
        /// Analyzes vehicle health metrics and usage patterns to provide intelligent maintenance
        /// recommendations that can prevent costly breakdowns and optimize maintenance costs.
        /// 
        /// **Analysis Includes**:
        /// - Current vehicle health status with health score (0-100)
        /// - Predictive maintenance suggestions based on odometer and time intervals
        /// - Rule-based recommendations (annual inspection, routine service)
        /// - Upcoming scheduled maintenance
        /// - Cost forecast for next 1-6 months
        /// - Urgency classification (Critical, High, Medium, Low)
        /// 
        /// **Predictive Features**:
        /// - Battery health monitoring for EVs
        /// - Tire rotation scheduling based on km traveled
        /// - Preventive vs reactive maintenance ratio analysis
        /// - Cost-benefit analysis for each recommendation
        /// 
        /// **Sample Request**:
        /// ```
        /// GET /api/fairnessoptimization/vehicle/1/maintenance-suggestions
        ///     ?includePredictive=true&amp;lookaheadDays=60
        /// ```
        /// 
        /// **Sample Response**:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "MAINTENANCE_SUGGESTIONS_GENERATED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "healthStatus": {
        ///       "currentOdometer": 45000,
        ///       "averageDailyDistance": 65.5,
        ///       "daysSinceLastMaintenance": 125,
        ///       "distanceSinceLastMaintenance": 8200,
        ///       "overallHealth": "Good",
        ///       "healthScore": 82,
        ///       "healthIssues": []
        ///     },
        ///     "suggestions": [
        ///       {
        ///         "maintenanceType": "Routine",
        ///         "title": "Tire Rotation and Inspection",
        ///         "description": "Tire rotation recommended every 8,000-10,000 km",
        ///         "urgency": "Medium",
        ///         "reason": "8200 km since last service",
        ///         "recommendedOdometerReading": 47000,
        ///         "recommendedDate": "2024-11-06T00:00:00Z",
        ///         "daysUntilRecommended": 14,
        ///         "estimatedCost": 800000,
        ///         "costSavingIfDoneNow": 300000,
        ///         "consequences": [
        ///           "Uneven tire wear",
        ///           "Reduced safety and handling",
        ///           "Need for premature tire replacement"
        ///         ],
        ///         "benefits": [
        ///           "Extended tire life",
        ///           "Improved safety",
        ///           "Better fuel efficiency"
        ///         ]
        ///       }
        ///     ],
        ///     "costForecast": {
        ///       "forecastPeriodDays": 60,
        ///       "estimatedTotalCost": 3300000,
        ///       "averageMonthlyCost": 1650000,
        ///       "costPerCoOwnerAverage": 1100000,
        ///       "monthlyForecasts": [
        ///         {
        ///           "month": "Nov 2024",
        ///           "estimatedCost": 1300000,
        ///           "expectedMaintenanceTypes": ["Routine", "Routine"]
        ///         }
        ///       ]
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Maintenance suggestions generated successfully. Message: MAINTENANCE_SUGGESTIONS_GENERATED_SUCCESSFULLY</response>
        /// <response code="400">Validation error. Message: LOOKAHEAD_DAYS_MUST_BE_BETWEEN_1_AND_365</response>
        /// <response code="403">User not authorized. Message: NOT_AUTHORIZED_TO_VIEW_MAINTENANCE_SUGGESTIONS</response>
        /// <response code="404">Vehicle not found. Message: VEHICLE_NOT_FOUND</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId:int}/maintenance-suggestions")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetMaintenanceSuggestions(
            int vehicleId,
            [FromQuery] bool includePredictive = true,
            [FromQuery] int lookaheadDays = 30)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var request = new GetMaintenanceSuggestionsRequest
            {
                IncludePredictive = includePredictive,
                LookaheadDays = lookaheadDays
            };

            var response = await _fairnessOptimizationService.GetMaintenanceSuggestionsAsync(vehicleId, userId, request);

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
        /// Analyzes costs and provides actionable recommendations for savings
        /// </summary>
        /// <param name="vehicleId">ID of the vehicle</param>
        /// <param name="analysisPeriodDays">Optional: Analysis period in days (default: 90, min: 7, max: 365)</param>
        /// <param name="includeFundOptimization">Optional: Include fund management recommendations (default: true)</param>
        /// <param name="includeMaintenanceOptimization">Optional: Include maintenance cost optimization (default: true)</param>
        /// <remarks>
        /// **Role Required**: CoOwner
        /// 
        /// **Authorization**: User must be an active co-owner of the vehicle
        /// 
        /// **Description**:  
        /// Performs comprehensive cost analysis and identifies opportunities to reduce expenses
        /// while maintaining vehicle quality and fairness among co-owners.
        /// 
        /// **Analysis Areas**:
        /// - **Maintenance Costs**: Preventive vs reactive maintenance ratio, cost per km/booking
        /// - **Fund Management**: Optimal fund balance, overfunding/underfunding detection
        /// - **General Expenses**: Charging costs, insurance optimization, parking fees
        /// - **Usage Efficiency**: Cost allocation based on actual usage patterns
        /// 
        /// **Recommendation Categories**:
        /// - **High Priority**: Immediate actions with significant savings potential
        /// - **Medium Priority**: Important improvements with moderate impact
        /// - **Low Priority**: Nice-to-have optimizations with minor savings
        /// 
        /// **Metrics Provided**:
        /// - Total costs incurred in analysis period
        /// - Cost per km and cost per booking
        /// - Potential savings amount and percentage
        /// - ROI for each recommendation
        /// - Implementation difficulty and cost
        /// 
        /// **Sample Request**:
        /// ```
        /// GET /api/fairnessoptimization/vehicle/1/cost-saving-recommendations
        ///     ?analysisPeriodDays=90&amp;includeFundOptimization=true&amp;includeMaintenanceOptimization=true
        /// ```
        /// 
        /// **Sample Response**:
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "COST_SAVING_RECOMMENDATIONS_GENERATED_SUCCESSFULLY",
        ///   "data": {
        ///     "vehicleId": 1,
        ///     "vehicleName": "Tesla Model 3",
        ///     "summary": {
        ///       "analysisPeriodDays": 90,
        ///       "totalCostsIncurred": 15000000,
        ///       "averageMonthlyCost": 5000000,
        ///       "costPerKm": 125,
        ///       "costPerBooking": 500000,
        ///       "potentialSavings": 4500000,
        ///       "savingsPercentage": 30.0,
        ///       "costBreakdowns": [
        ///         {"category": "Routine", "amount": 10000000, "percentage": 66.7, "trend": "Stable"},
        ///         {"category": "Repair", "amount": 5000000, "percentage": 33.3, "trend": "Increasing"}
        ///       ]
        ///     },
        ///     "recommendations": [
        ///       {
        ///         "category": "Maintenance",
        ///         "priority": "High",
        ///         "title": "Switch to Preventive Maintenance Schedule",
        ///         "description": "Regular preventive maintenance costs less than reactive repairs",
        ///         "potentialSavingsAmount": 3000000,
        ///         "potentialSavingsPercentage": 30.0,
        ///         "timeframeForSavings": "6-12 months",
        ///         "actionSteps": [
        ///           "Create maintenance schedule based on odometer readings",
        ///           "Book services during off-peak times for discounts",
        ///           "Keep detailed maintenance records"
        ///         ],
        ///         "difficulty": "Easy",
        ///         "implementationCost": 0,
        ///         "roi": 300.0
        ///       },
        ///       {
        ///         "category": "General",
        ///         "priority": "Medium",
        ///         "title": "Optimize Charging Costs",
        ///         "description": "Charge during off-peak hours to reduce electricity costs",
        ///         "potentialSavingsAmount": 750000,
        ///         "potentialSavingsPercentage": 15.0,
        ///         "timeframeForSavings": "Ongoing",
        ///         "actionSteps": [
        ///           "Use time-of-use electricity rates",
        ///           "Schedule charging for overnight (off-peak)",
        ///           "Track charging costs per co-owner"
        ///         ],
        ///         "difficulty": "Easy",
        ///         "implementationCost": 0,
        ///         "roi": 150.0
        ///       }
        ///     ],
        ///     "fundInsights": {
        ///       "currentFundBalance": 8000000,
        ///       "recommendedMinimumBalance": 10000000,
        ///       "recommendedOptimalBalance": 15000000,
        ///       "isUnderfunded": true,
        ///       "averageMonthlyExpenses": 5000000,
        ///       "monthsCovered": 1,
        ///       "fundHealthIssues": ["Fund balance below recommended minimum"],
        ///       "fundOptimizationTips": [
        ///         "Maintain 2-3 months of expenses as buffer",
        ///         "Set up automatic contributions from all co-owners"
        ///       ]
        ///     },
        ///     "maintenanceInsights": {
        ///       "averageMaintenanceCost": 2500000,
        ///       "preventiveMaintenanceRatio": 40.0,
        ///       "reactiveMaintenanceRatio": 60.0,
        ///       "potentialSavingsFromPreventive": 2000000,
        ///       "highCostMaintenanceTypes": ["Repair", "Emergency", "Routine"],
        ///       "hasMaintenanceSchedule": false,
        ///       "optimizationOpportunities": [
        ///         "Increase preventive maintenance ratio to reduce costs",
        ///         "Establish regular maintenance schedule"
        ///       ]
        ///     }
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Cost-saving recommendations generated successfully. Message: COST_SAVING_RECOMMENDATIONS_GENERATED_SUCCESSFULLY</response>
        /// <response code="400">Validation error. Message: ANALYSIS_PERIOD_MUST_BE_BETWEEN_7_AND_365_DAYS</response>
        /// <response code="403">User not authorized. Message: NOT_AUTHORIZED_TO_VIEW_COST_RECOMMENDATIONS</response>
        /// <response code="404">Vehicle not found. Message: VEHICLE_NOT_FOUND</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("vehicle/{vehicleId:int}/cost-saving-recommendations")]
        [AuthorizeRoles(EUserRole.CoOwner)]
        public async Task<IActionResult> GetCostSavingRecommendations(
            int vehicleId,
            [FromQuery] int analysisPeriodDays = 90,
            [FromQuery] bool includeFundOptimization = true,
            [FromQuery] bool includeMaintenanceOptimization = true)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var request = new GetCostSavingRecommendationsRequest
            {
                AnalysisPeriodDays = analysisPeriodDays,
                IncludeFundOptimization = includeFundOptimization,
                IncludeMaintenanceOptimization = includeMaintenanceOptimization
            };

            var response = await _fairnessOptimizationService.GetCostSavingRecommendationsAsync(vehicleId, userId, request);

            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}
