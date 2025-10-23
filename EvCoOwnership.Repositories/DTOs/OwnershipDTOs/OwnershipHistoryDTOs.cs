using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.DTOs.OwnershipDTOs
{
    /// <summary>
    /// Response containing ownership history for a vehicle
    /// </summary>
    public class OwnershipHistoryResponse
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int? OwnershipChangeRequestId { get; set; }
        public decimal PreviousPercentage { get; set; }
        public decimal NewPercentage { get; set; }
        public decimal PercentageChange { get; set; }
        public decimal PreviousInvestment { get; set; }
        public decimal NewInvestment { get; set; }
        public decimal InvestmentChange { get; set; }
        public string ChangeType { get; set; } = null!;
        public string? Reason { get; set; }
        public int? ChangedByUserId { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Response containing ownership timeline for a vehicle
    /// Shows all co-owners and their ownership evolution over time
    /// </summary>
    public class VehicleOwnershipTimelineResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public DateTime? VehicleCreatedAt { get; set; }
        public int TotalHistoryRecords { get; set; }
        public List<CoOwnerOwnershipTimeline> CoOwnersTimeline { get; set; } = new List<CoOwnerOwnershipTimeline>();
        public List<OwnershipHistoryResponse> AllChanges { get; set; } = new List<OwnershipHistoryResponse>();
    }

    /// <summary>
    /// Timeline of ownership changes for a specific co-owner
    /// </summary>
    public class CoOwnerOwnershipTimeline
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal CurrentPercentage { get; set; }
        public decimal InitialPercentage { get; set; }
        public decimal TotalChange { get; set; }
        public DateTime? JoinedAt { get; set; }
        public int ChangeCount { get; set; }
        public List<OwnershipHistoryResponse> Changes { get; set; } = new List<OwnershipHistoryResponse>();
    }

    /// <summary>
    /// Response containing ownership snapshot at a specific point in time
    /// </summary>
    public class OwnershipSnapshotResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public DateTime SnapshotDate { get; set; }
        public List<CoOwnerSnapshot> CoOwners { get; set; } = new List<CoOwnerSnapshot>();
        public decimal TotalPercentage { get; set; }
    }

    /// <summary>
    /// Snapshot of a co-owner's ownership at a specific time
    /// </summary>
    public class CoOwnerSnapshot
    {
        public int CoOwnerId { get; set; }
        public int UserId { get; set; }
        public string CoOwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal OwnershipPercentage { get; set; }
        public decimal InvestmentAmount { get; set; }
    }

    /// <summary>
    /// Statistics about ownership history for a vehicle
    /// </summary>
    public class OwnershipHistoryStatisticsResponse
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = null!;
        public int TotalChanges { get; set; }
        public int TotalCoOwners { get; set; }
        public int CurrentCoOwners { get; set; }
        public DateTime? FirstChange { get; set; }
        public DateTime? LastChange { get; set; }
        public decimal AverageOwnershipPercentage { get; set; }
        public int MostActiveCoOwnerId { get; set; }
        public string? MostActiveCoOwnerName { get; set; }
        public int MostActiveCoOwnerChanges { get; set; }
        public Dictionary<string, int> ChangeTypeBreakdown { get; set; } = new Dictionary<string, int>();
        public DateTime? StatisticsGeneratedAt { get; set; }
    }

    /// <summary>
    /// Request to get ownership history with filters
    /// </summary>
    public class GetOwnershipHistoryRequest
    {
        /// <summary>
        /// Filter by change type (optional)
        /// </summary>
        public string? ChangeType { get; set; }

        /// <summary>
        /// Filter by start date (optional)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Filter by end date (optional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by co-owner ID (optional)
        /// </summary>
        public int? CoOwnerId { get; set; }

        /// <summary>
        /// Number of records to return (default: 50)
        /// </summary>
        public int Limit { get; set; } = 50;

        /// <summary>
        /// Offset for pagination
        /// </summary>
        public int Offset { get; set; } = 0;
    }
}
