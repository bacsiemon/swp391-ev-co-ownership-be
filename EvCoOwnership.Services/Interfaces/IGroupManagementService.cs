using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.GroupManagementDTOs;

namespace EvCoOwnership.Services.Interfaces
{
    /// <summary>
    /// Service for managing group operations by staff and admin
    /// </summary>
    public interface IGroupManagementService
    {
        #region Staff Group Management

        /// <summary>
        /// Get groups assigned to staff member
        /// </summary>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>List of assigned groups</returns>
        Task<BaseResponse<List<AssignedGroupResponse>>> GetAssignedGroupsAsync(int staffId);

        /// <summary>
        /// Get detailed information about a specific group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>Detailed group information</returns>
        Task<BaseResponse<StaffGroupDetailResponse>> GetGroupDetailsAsync(int groupId, int staffId);

        /// <summary>
        /// Get disputes within a group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>List of group disputes</returns>
        Task<BaseResponse<List<GroupDisputeResponse>>> GetGroupDisputesAsync(int groupId, int staffId);

        /// <summary>
        /// Create support ticket for a group
        /// </summary>
        /// <param name="request">Support ticket request</param>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>Created support ticket information</returns>
        Task<BaseResponse<SupportTicketInfo>> CreateSupportTicketAsync(CreateSupportTicketRequest request, int staffId);

        /// <summary>
        /// Update dispute status and add resolution notes
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="newStatus">New dispute status</param>
        /// <param name="resolutionNotes">Resolution notes</param>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>Updated dispute information</returns>
        Task<BaseResponse<GroupDisputeResponse>> UpdateDisputeStatusAsync(int disputeId, string newStatus, string? resolutionNotes, int staffId);

        /// <summary>
        /// Add message to existing dispute
        /// </summary>
        /// <param name="disputeId">Dispute ID</param>
        /// <param name="message">Message content</param>
        /// <param name="staffId">Staff member ID</param>
        /// <returns>Updated dispute with new message</returns>
        Task<BaseResponse<GroupDisputeResponse>> AddDisputeMessageAsync(int disputeId, string message, int staffId);

        #endregion

        #region Admin Group Management

        /// <summary>
        /// Get overview of all groups in the system
        /// </summary>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Groups overview with statistics</returns>
        Task<BaseResponse<AdminGroupOverviewResponse>> GetGroupsOverviewAsync(int adminId);

        /// <summary>
        /// Create a new group
        /// </summary>
        /// <param name="request">Create group request</param>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Created group information</returns>
        Task<BaseResponse<StaffGroupDetailResponse>> CreateGroupAsync(CreateGroupRequest request, int adminId);

        /// <summary>
        /// Update group status (activate, suspend, terminate)
        /// </summary>
        /// <param name="request">Status update request</param>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Updated group information</returns>
        Task<BaseResponse<StaffGroupDetailResponse>> UpdateGroupStatusAsync(UpdateGroupStatusRequest request, int adminId);

        /// <summary>
        /// Get comprehensive group analytics
        /// </summary>
        /// <param name="startDate">Analysis start date</param>
        /// <param name="endDate">Analysis end date</param>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Group analytics data</returns>
        Task<BaseResponse<GroupAnalyticsResponse>> GetGroupAnalyticsAsync(DateTime startDate, DateTime endDate, int adminId);

        /// <summary>
        /// Get groups with filters and pagination
        /// </summary>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="searchTerm">Search term for group name (optional)</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Filtered and paginated groups</returns>
        Task<BaseResponse<List<GroupSummaryInfo>>> GetGroupsWithFiltersAsync(string? status, string? searchTerm, int page, int pageSize, int adminId);

        /// <summary>
        /// Assign staff member to a group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="staffId">Staff ID to assign</param>
        /// <param name="adminId">Admin ID performing the assignment</param>
        /// <returns>Assignment result</returns>
        Task<BaseResponse<bool>> AssignStaffToGroupAsync(int groupId, int staffId, int adminId);

        /// <summary>
        /// Remove staff assignment from a group
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="staffId">Staff ID to remove</param>
        /// <param name="adminId">Admin ID performing the removal</param>
        /// <returns>Removal result</returns>
        Task<BaseResponse<bool>> RemoveStaffFromGroupAsync(int groupId, int staffId, int adminId);

        /// <summary>
        /// Get system-wide group statistics
        /// </summary>
        /// <param name="adminId">Admin ID</param>
        /// <returns>System statistics</returns>
        Task<BaseResponse<GroupSystemStatistics>> GetSystemStatisticsAsync(int adminId);

        /// <summary>
        /// Export group data for reporting
        /// </summary>
        /// <param name="groupIds">List of group IDs to export (empty for all)</param>
        /// <param name="format">Export format (CSV, Excel, JSON)</param>
        /// <param name="adminId">Admin ID</param>
        /// <returns>Export file information</returns>
        Task<BaseResponse<string>> ExportGroupDataAsync(List<int> groupIds, string format, int adminId);

        #endregion
    }
}