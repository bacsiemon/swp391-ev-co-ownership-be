using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.GroupManagementDTOs
{
    #region Staff Group Management DTOs

    /// <summary>
    /// Response for assigned groups to staff
    /// </summary>
    public class AssignedGroupResponse
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int VehicleCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public int OpenDisputeCount { get; set; }
        public int PendingRequestCount { get; set; }
        public decimal TotalFundAmount { get; set; }
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    }

    /// <summary>
    /// Detailed group information for staff
    /// </summary>
    public class StaffGroupDetailResponse
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Members
        public List<GroupMemberInfo> Members { get; set; } = new();

        // Vehicles
        public List<GroupVehicleInfo> Vehicles { get; set; } = new();

        // Recent activities
        public List<GroupActivityInfo> RecentActivities { get; set; } = new();

        // Financial summary
        public GroupFinancialSummary FinancialSummary { get; set; } = new();

        // Support history
        public List<SupportTicketInfo> SupportHistory { get; set; } = new();
    }

    /// <summary>
    /// Group dispute information
    /// </summary>
    public class GroupDisputeResponse
    {
        public int DisputeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public int ReportedByUserId { get; set; }
        public string ReportedByUserName { get; set; } = string.Empty;
        public int? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }
        public List<DisputeMessageInfo> Messages { get; set; } = new();
    }

    /// <summary>
    /// Support ticket creation request
    /// </summary>
    public class CreateSupportTicketRequest
    {
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string Category { get; set; } = string.Empty; // Technical, Financial, Dispute, General
        public bool NotifyMembers { get; set; } = false;
    }

    public class CreateSupportTicketRequestValidator : AbstractValidator<CreateSupportTicketRequest>
    {
        public CreateSupportTicketRequestValidator()
        {
            RuleFor(x => x.GroupId)
                .GreaterThan(0)
                .WithMessage("Group ID is required");

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Title is required and must not exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(2000)
                .WithMessage("Description is required and must not exceed 2000 characters");

            RuleFor(x => x.Priority)
                .Must(p => new[] { "Low", "Normal", "High", "Critical" }.Contains(p))
                .WithMessage("Priority must be Low, Normal, High, or Critical");

            RuleFor(x => x.Category)
                .Must(c => new[] { "Technical", "Financial", "Dispute", "General" }.Contains(c))
                .WithMessage("Category must be Technical, Financial, Dispute, or General");
        }
    }

    #endregion

    #region Admin Group Management DTOs

    /// <summary>
    /// Admin group overview response
    /// </summary>
    public class AdminGroupOverviewResponse
    {
        public List<GroupSummaryInfo> Groups { get; set; } = new();
        public GroupSystemStatistics Statistics { get; set; } = new();
        public List<GroupTrendInfo> Trends { get; set; } = new();
    }

    /// <summary>
    /// Create new group request
    /// </summary>
    public class CreateGroupRequest
    {
        public string GroupName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public List<GroupMemberRequest> InitialMembers { get; set; } = new();
        public GroupSettingsRequest Settings { get; set; } = new();
    }

    public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupRequestValidator()
        {
            RuleFor(x => x.GroupName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Group name is required and must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.CreatedByUserId)
                .GreaterThan(0)
                .WithMessage("Creator user ID is required");

            RuleFor(x => x.InitialMembers)
                .NotEmpty()
                .WithMessage("At least one initial member is required");

            RuleForEach(x => x.InitialMembers)
                .SetValidator(new GroupMemberRequestValidator());
        }
    }

    /// <summary>
    /// Update group status request
    /// </summary>
    public class UpdateGroupStatusRequest
    {
        public int GroupId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // Active, Suspended, Terminated
        public string Reason { get; set; } = string.Empty;
        public bool NotifyMembers { get; set; } = true;
        public DateTime? EffectiveDate { get; set; }
    }

    public class UpdateGroupStatusRequestValidator : AbstractValidator<UpdateGroupStatusRequest>
    {
        public UpdateGroupStatusRequestValidator()
        {
            RuleFor(x => x.GroupId)
                .GreaterThan(0)
                .WithMessage("Group ID is required");

            RuleFor(x => x.NewStatus)
                .Must(s => new[] { "Active", "Suspended", "Terminated" }.Contains(s))
                .WithMessage("Status must be Active, Suspended, or Terminated");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .MaximumLength(500)
                .WithMessage("Reason is required and must not exceed 500 characters");
        }
    }

    /// <summary>
    /// Group analytics response
    /// </summary>
    public class GroupAnalyticsResponse
    {
        public int TotalGroups { get; set; }
        public int ActiveGroups { get; set; }
        public int SuspendedGroups { get; set; }
        public int TerminatedGroups { get; set; }
        public int TotalMembers { get; set; }
        public int TotalVehicles { get; set; }
        public decimal TotalFundsAmount { get; set; }

        // Performance metrics
        public List<GroupPerformanceMetric> TopPerformingGroups { get; set; } = new();
        public List<GroupPerformanceMetric> UnderperformingGroups { get; set; } = new();

        // Financial analytics
        public List<MonthlyFinancialData> FinancialTrends { get; set; } = new();

        // Usage analytics
        public List<VehicleUtilizationData> VehicleUtilization { get; set; } = new();

        // Issues analytics
        public List<IssueStatistic> IssueStatistics { get; set; } = new();
    }

    #endregion

    #region Supporting Data Classes

    /// <summary>
    /// Group member information
    /// </summary>
    public class GroupMemberInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public string Role { get; set; } = string.Empty; // Owner, CoOwner
        public DateTime JoinedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ContributedAmount { get; set; }
    }

    /// <summary>
    /// Group vehicle information
    /// </summary>
    public class GroupVehicleInfo
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public DateTime AcquisitionDate { get; set; }
        public int TotalBookings { get; set; }
        public decimal UtilizationRate { get; set; }
    }

    /// <summary>
    /// Group activity information
    /// </summary>
    public class GroupActivityInfo
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
    }

    /// <summary>
    /// Group financial summary
    /// </summary>
    public class GroupFinancialSummary
    {
        public decimal TotalFunds { get; set; }
        public decimal AvailableFunds { get; set; }
        public decimal ReservedFunds { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal MaintenanceCosts { get; set; }
        public decimal InsuranceCosts { get; set; }
        public List<RecentTransactionInfo> RecentTransactions { get; set; } = new();
    }

    /// <summary>
    /// Support ticket information
    /// </summary>
    public class SupportTicketInfo
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public int CreatedByStaffId { get; set; }
        public string CreatedByStaffName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dispute message information
    /// </summary>
    public class DisputeMessageInfo
    {
        public int MessageId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty; // Member, Staff, Admin
    }

    /// <summary>
    /// Group member request for creation
    /// </summary>
    public class GroupMemberRequest
    {
        public int UserId { get; set; }
        public decimal OwnershipPercentage { get; set; }
        public string Role { get; set; } = "CoOwner";
    }

    public class GroupMemberRequestValidator : AbstractValidator<GroupMemberRequest>
    {
        public GroupMemberRequestValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .WithMessage("User ID is required");

            RuleFor(x => x.OwnershipPercentage)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .WithMessage("Ownership percentage must be between 0 and 100");

            RuleFor(x => x.Role)
                .Must(r => new[] { "Owner", "CoOwner" }.Contains(r))
                .WithMessage("Role must be Owner or CoOwner");
        }
    }

    /// <summary>
    /// Group settings request
    /// </summary>
    public class GroupSettingsRequest
    {
        public bool AutoApproveBookings { get; set; } = false;
        public int MaxBookingDays { get; set; } = 7;
        public decimal MinimumFundBalance { get; set; } = 0;
        public bool AllowMemberInvites { get; set; } = true;
        public bool RequireUnanimousVoting { get; set; } = false;
    }

    /// <summary>
    /// Group summary information
    /// </summary>
    public class GroupSummaryInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int VehicleCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public decimal TotalFunds { get; set; }
        public int ActiveDisputeCount { get; set; }
        public decimal UtilizationRate { get; set; }
        public string HealthScore { get; set; } = string.Empty; // Excellent, Good, Fair, Poor
    }

    /// <summary>
    /// Group system statistics
    /// </summary>
    public class GroupSystemStatistics
    {
        public int TotalGroups { get; set; }
        public int NewGroupsThisMonth { get; set; }
        public int ActiveDisputes { get; set; }
        public decimal AverageGroupSize { get; set; }
        public decimal AverageVehiclesPerGroup { get; set; }
        public decimal SystemUtilizationRate { get; set; }
        public decimal TotalSystemFunds { get; set; }
    }

    /// <summary>
    /// Group trend information
    /// </summary>
    public class GroupTrendInfo
    {
        public DateTime Date { get; set; }
        public int NewGroups { get; set; }
        public int TerminatedGroups { get; set; }
        public int ActiveMembers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int BookingCount { get; set; }
    }

    /// <summary>
    /// Group performance metric
    /// </summary>
    public class GroupPerformanceMetric
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal PerformanceScore { get; set; }
        public decimal UtilizationRate { get; set; }
        public decimal RevenueGenerated { get; set; }
        public int MemberSatisfactionScore { get; set; }
        public int DisputeCount { get; set; }
    }

    /// <summary>
    /// Monthly financial data
    /// </summary>
    public class MonthlyFinancialData
    {
        public DateTime Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public int ActiveGroups { get; set; }
    }

    /// <summary>
    /// Vehicle utilization data
    /// </summary>
    public class VehicleUtilizationData
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal UtilizationRate { get; set; }
        public int TotalBookings { get; set; }
        public decimal RevenueGenerated { get; set; }
    }

    /// <summary>
    /// Issue statistic
    /// </summary>
    public class IssueStatistic
    {
        public string IssueType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public string Trend { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    }

    /// <summary>
    /// Recent transaction information
    /// </summary>
    public class RecentTransactionInfo
    {
        public int TransactionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // Income, Expense
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update dispute status request
    /// </summary>
    public class UpdateDisputeStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? ResolutionNotes { get; set; }
    }

    public class UpdateDisputeStatusRequestValidator : AbstractValidator<UpdateDisputeStatusRequest>
    {
        public UpdateDisputeStatusRequestValidator()
        {
            RuleFor(x => x.NewStatus)
                .NotEmpty().WithMessage("New status is required")
                .Must(status => new[] { "Open", "In Progress", "Resolved", "Closed" }.Contains(status))
                .WithMessage("New status must be one of: Open, In Progress, Resolved, Closed");

            RuleFor(x => x.ResolutionNotes)
                .MaximumLength(1000).WithMessage("Resolution notes cannot exceed 1000 characters");
        }
    }

    /// <summary>
    /// Add dispute message request
    /// </summary>
    public class AddDisputeMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AddDisputeMessageRequestValidator : AbstractValidator<AddDisputeMessageRequest>
    {
        public AddDisputeMessageRequestValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters");
        }
    }

    #endregion
}