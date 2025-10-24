using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.DTOs.FundDTOs
{
    /// <summary>
    /// Response DTO for fund balance information
    /// </summary>
    public class FundBalanceResponse
    {
        public int FundId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal TotalAddedAmount { get; set; }
        public decimal TotalUsedAmount { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalUsages { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string BalanceStatus { get; set; } = string.Empty; // Healthy, Warning, Low
        public decimal RecommendedMinBalance { get; set; }
    }

    /// <summary>
    /// Response DTO for fund addition (deposit) record
    /// </summary>
    public class FundAdditionResponse
    {
        public int Id { get; set; }
        public int FundId { get; set; }
        public int? CoOwnerId { get; set; }
        public string CoOwnerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Response DTO for fund usage (expense) record
    /// </summary>
    public class FundUsageResponse
    {
        public int Id { get; set; }
        public int FundId { get; set; }
        public string UsageType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? MaintenanceCostId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Response DTO for comprehensive fund summary with history
    /// </summary>
    public class FundSummaryResponse
    {
        public FundBalanceResponse Balance { get; set; } = null!;
        public List<FundAdditionResponse> RecentAdditions { get; set; } = new();
        public List<FundUsageResponse> RecentUsages { get; set; } = new();
        public FundStatistics Statistics { get; set; } = null!;
    }

    /// <summary>
    /// Fund statistics and analytics
    /// </summary>
    public class FundStatistics
    {
        public decimal AverageMonthlyAddition { get; set; }
        public decimal AverageMonthlyUsage { get; set; }
        public decimal NetMonthlyFlow { get; set; }
        public int MonthsCovered { get; set; } // How many months can current balance cover
        public Dictionary<string, decimal> UsageByType { get; set; } = new();
        public List<MonthlyFundFlow> MonthlyFlows { get; set; } = new();
    }

    /// <summary>
    /// Monthly fund flow data
    /// </summary>
    public class MonthlyFundFlow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAdded { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal NetFlow { get; set; }
        public decimal EndingBalance { get; set; }
    }

    /// <summary>
    /// Request DTO for creating fund addition
    /// </summary>
    public class CreateFundAdditionRequest
    {
        public int VehicleId { get; set; }
        public decimal Amount { get; set; }
        public EPaymentMethod PaymentMethod { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request DTO for creating fund usage
    /// </summary>
    public class CreateFundUsageRequest
    {
        public int VehicleId { get; set; }
        public EUsageType UsageType { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? MaintenanceCostId { get; set; }
    }

    /// <summary>
    /// Request DTO for updating fund usage
    /// </summary>
    public class UpdateFundUsageRequest
    {
        public EUsageType? UsageType { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? MaintenanceCostId { get; set; }
    }

    /// <summary>
    /// Fund category budget information
    /// </summary>
    public class FundCategoryBudget
    {
        public EUsageType Category { get; set; }
        public decimal MonthlyBudgetLimit { get; set; }
        public decimal CurrentMonthSpending { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal BudgetUtilizationPercent { get; set; }
        public string BudgetStatus { get; set; } = string.Empty; // OnTrack, Warning, Exceeded
        public int TransactionCount { get; set; }
        public decimal AverageTransactionAmount { get; set; }
    }

    /// <summary>
    /// Response DTO for category-based fund analysis
    /// </summary>
    public class FundCategoryAnalysisResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int AnalysisMonth { get; set; }
        public int AnalysisYear { get; set; }
        public List<FundCategoryBudget> CategoryBudgets { get; set; } = new();
        public decimal TotalBudget { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal OverallUtilizationPercent { get; set; }
    }

    /// <summary>
    /// Request DTO for setting category budget limit
    /// </summary>
    public class SetCategoryBudgetRequest
    {
        public int VehicleId { get; set; }
        public EUsageType Category { get; set; }
        public decimal MonthlyBudgetLimit { get; set; }
    }

    /// <summary>
    /// Category budget limit configuration
    /// </summary>
    public class CategoryBudgetConfig
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public EUsageType Category { get; set; }
        public decimal MonthlyBudgetLimit { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
