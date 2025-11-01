#nullable disable
using Microsoft.EntityFrameworkCore;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.DTOs;

namespace EvCoOwnership.Repositories.Context;

public partial class EvCoOwnershipDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Configure VehicleUpgradeProposal
        modelBuilder.Entity<VehicleUpgradeProposal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("vehicle_upgrade_proposals");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");
            entity.Property(e => e.UpgradeType)
                .HasColumnName("upgrade_type")
                .HasConversion<int>();
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Description)
                .IsRequired()
                .HasColumnName("description");
            entity.Property(e => e.EstimatedCost)
                .HasPrecision(15, 2)
                .HasColumnName("estimated_cost");
            entity.Property(e => e.Justification)
                .HasColumnName("justification");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.VendorName)
                .HasMaxLength(200)
                .HasColumnName("vendor_name");
            entity.Property(e => e.VendorContact)
                .HasMaxLength(100)
                .HasColumnName("vendor_contact");
            entity.Property(e => e.ProposedInstallationDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("proposed_installation_date");
            entity.Property(e => e.EstimatedDurationDays)
                .HasColumnName("estimated_duration_days");
            entity.Property(e => e.ProposedByUserId)
                .HasColumnName("proposed_by_user_id");
            entity.Property(e => e.ProposedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("proposed_at");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.ApprovedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("approved_at");
            entity.Property(e => e.RejectedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("rejected_at");
            entity.Property(e => e.CancelledAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("cancelled_at");
            entity.Property(e => e.IsExecuted)
                .HasDefaultValue(false)
                .HasColumnName("is_executed");
            entity.Property(e => e.ExecutedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("executed_at");
            entity.Property(e => e.ActualCost)
                .HasPrecision(15, 2)
                .HasColumnName("actual_cost");
            entity.Property(e => e.ExecutionNotes)
                .HasColumnName("execution_notes");
            entity.Property(e => e.FundUsageId)
                .HasColumnName("fund_usage_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Vehicle)
                .WithMany()
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ProposedByUser)
                .WithMany()
                .HasForeignKey(d => d.ProposedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.FundUsage)
                .WithMany()
                .HasForeignKey(d => d.FundUsageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.VehicleId);
            entity.HasIndex(e => e.Status);
        });

        // Configure VehicleUpgradeVote with composite primary key
        modelBuilder.Entity<VehicleUpgradeVote>(entity =>
        {
            // Define composite primary key (ProposalId + UserId)
            entity.HasKey(e => new { e.ProposalId, e.UserId });

            entity.ToTable("vehicle_upgrade_votes");

            entity.Property(e => e.ProposalId).HasColumnName("proposal_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsAgree).HasColumnName("is_agree");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .HasColumnName("comments");
            entity.Property(e => e.VotedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("voted_at");

            entity.HasOne(d => d.Proposal)
                .WithMany()
                .HasForeignKey(d => d.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProposalId);
            entity.HasIndex(e => e.UserId);
        });
    }
}
