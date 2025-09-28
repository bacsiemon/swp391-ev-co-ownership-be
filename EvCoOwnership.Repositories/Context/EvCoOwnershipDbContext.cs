using System;
using System.Collections.Generic;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Enums;
using Microsoft.EntityFrameworkCore;

namespace EvCoOwnership.Repositories.Context;

public partial class EvCoOwnershipDbContext : DbContext
{
    public EvCoOwnershipDbContext()
    {
    }

    public EvCoOwnershipDbContext(DbContextOptions<EvCoOwnershipDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<CheckIn> CheckIns { get; set; }

    public virtual DbSet<CheckOut> CheckOuts { get; set; }

    public virtual DbSet<CoOwner> CoOwners { get; set; }

    public virtual DbSet<CoOwnerGroup> CoOwnerGroups { get; set; }

    public virtual DbSet<DrivingLicense> DrivingLicenses { get; set; }

    public virtual DbSet<Fund> Funds { get; set; }

    public virtual DbSet<FundAddition> FundAdditions { get; set; }

    public virtual DbSet<FundUsage> FundUsages { get; set; }

    public virtual DbSet<FundUsageVote> FundUsageVotes { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<MaintenanceCost> MaintenanceCosts { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleCondition> VehicleConditions { get; set; }

    public virtual DbSet<VehicleStation> VehicleStations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.ApprovedBy, "idx_bookings_approved_by");

            entity.HasIndex(e => e.CoOwnerId, "idx_bookings_co_owner_id");

            entity.HasIndex(e => new { e.CoOwnerId, e.StatusEnum, e.StartTime }, "idx_bookings_co_owner_status_enum_time");

            entity.HasIndex(e => e.EndTime, "idx_bookings_end_time");

            entity.HasIndex(e => e.StartTime, "idx_bookings_start_time");

            entity.HasIndex(e => e.StatusEnum, "idx_bookings_status_enum");

            entity.HasIndex(e => e.VehicleId, "idx_bookings_vehicle_id");

            entity.HasIndex(e => new { e.VehicleId, e.StatusEnum, e.StartTime }, "idx_bookings_vehicle_status_enum_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CoOwnerId).HasColumnName("co_owner_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.Purpose)
                .HasMaxLength(500)
                .HasColumnName("purpose");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(EBookingStatus.Confirmed)
                .HasColumnName("status_enum");
            entity.Property(e => e.TotalCost)
                .HasPrecision(10, 2)
                .HasColumnName("total_cost");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("bookings_approved_by_fkey");

            entity.HasOne(d => d.CoOwner).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CoOwnerId)
                .HasConstraintName("bookings_co_owner_id_fkey");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("bookings_vehicle_id_fkey");
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("check_ins_pkey");

            entity.ToTable("check_ins");

            entity.HasIndex(e => e.BookingId, "idx_check_ins_booking_id");

            entity.HasIndex(e => e.CheckTime, "idx_check_ins_check_time");

            entity.HasIndex(e => e.StaffId, "idx_check_ins_staff_id");

            entity.HasIndex(e => e.VehicleConditionId, "idx_check_ins_vehicle_condition_id");

            entity.HasIndex(e => e.VehicleStationId, "idx_check_ins_vehicle_station_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CheckTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("check_time");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleConditionId).HasColumnName("vehicle_condition_id");
            entity.Property(e => e.VehicleStationId).HasColumnName("vehicle_station_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("check_ins_booking_id_fkey");

            entity.HasOne(d => d.Staff).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("check_ins_staff_id_fkey");

            entity.HasOne(d => d.VehicleCondition).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.VehicleConditionId)
                .HasConstraintName("check_ins_vehicle_condition_id_fkey");

            entity.HasOne(d => d.VehicleStation).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.VehicleStationId)
                .HasConstraintName("check_ins_vehicle_station_id_fkey");
        });

        modelBuilder.Entity<CheckOut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("check_outs_pkey");

            entity.ToTable("check_outs");

            entity.HasIndex(e => e.BookingId, "idx_check_outs_booking_id");

            entity.HasIndex(e => e.CheckTime, "idx_check_outs_check_time");

            entity.HasIndex(e => e.StaffId, "idx_check_outs_staff_id");

            entity.HasIndex(e => e.VehicleConditionId, "idx_check_outs_vehicle_condition_id");

            entity.HasIndex(e => e.VehicleStationId, "idx_check_outs_vehicle_station_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CheckTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("check_time");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleConditionId).HasColumnName("vehicle_condition_id");
            entity.Property(e => e.VehicleStationId).HasColumnName("vehicle_station_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.CheckOuts)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("check_outs_booking_id_fkey");

            entity.HasOne(d => d.Staff).WithMany(p => p.CheckOuts)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("check_outs_staff_id_fkey");

            entity.HasOne(d => d.VehicleCondition).WithMany(p => p.CheckOuts)
                .HasForeignKey(d => d.VehicleConditionId)
                .HasConstraintName("check_outs_vehicle_condition_id_fkey");

            entity.HasOne(d => d.VehicleStation).WithMany(p => p.CheckOuts)
                .HasForeignKey(d => d.VehicleStationId)
                .HasConstraintName("check_outs_vehicle_station_id_fkey");
        });

        modelBuilder.Entity<CoOwner>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("co_owners_pkey");

            entity.ToTable("co_owners");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.CoOwner)
                .HasForeignKey<CoOwner>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("co_owners_user_id_fkey");
        });

        modelBuilder.Entity<CoOwnerGroup>(entity =>
        {
            entity.HasKey(e => new { e.CoOwnerId, e.GroupId }).HasName("co_owner_groups_pkey");

            entity.ToTable("co_owner_groups");

            entity.HasIndex(e => e.CoOwnerId, "idx_co_owner_groups_co_owner_id");

            entity.HasIndex(e => e.GroupId, "idx_co_owner_groups_group_id");

            entity.HasIndex(e => new { e.GroupId, e.StatusEnum }, "idx_co_owner_groups_group_status_enum");

            entity.HasIndex(e => e.JoinDate, "idx_co_owner_groups_join_date");

            entity.HasIndex(e => e.StatusEnum, "idx_co_owner_groups_status_enum");

            entity.Property(e => e.CoOwnerId).HasColumnName("co_owner_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.InvestmentAmount)
                .HasPrecision(15, 2)
                .HasColumnName("investment_amount");
            entity.Property(e => e.JoinDate).HasColumnName("join_date");
            entity.Property(e => e.OwnershipPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("ownership_percentage");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(ECoOwnerStatus.Left)
                .HasColumnName("status_enum");

            entity.HasOne(d => d.CoOwner).WithMany(p => p.CoOwnerGroups)
                .HasForeignKey(d => d.CoOwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("co_owner_groups_co_owner_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.CoOwnerGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("co_owner_groups_group_id_fkey");
        });

        modelBuilder.Entity<DrivingLicense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("driving_licenses_pkey");

            entity.ToTable("driving_licenses");

            entity.HasIndex(e => e.LicenseNumber, "driving_licenses_license_number_key").IsUnique();

            entity.HasIndex(e => e.CoOwnerId, "idx_driving_licenses_co_owner_id");

            entity.HasIndex(e => e.ExpiryDate, "idx_driving_licenses_expiry_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoOwnerId).HasColumnName("co_owner_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");
            entity.Property(e => e.IssuedBy)
                .HasMaxLength(100)
                .HasColumnName("issued_by");
            entity.Property(e => e.LicenseImageUrl)
                .HasMaxLength(500)
                .HasColumnName("license_image_url");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .HasColumnName("license_number");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CoOwner).WithMany(p => p.DrivingLicenses)
                .HasForeignKey(d => d.CoOwnerId)
                .HasConstraintName("driving_licenses_co_owner_id_fkey");
        });

        modelBuilder.Entity<Fund>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("funds_pkey");

            entity.ToTable("funds");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentBalance)
                .HasPrecision(15, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("current_balance");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<FundAddition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fund_additions_pkey");

            entity.ToTable("fund_additions");

            entity.HasIndex(e => e.CoOwnerId, "idx_fund_additions_co_owner_id");

            entity.HasIndex(e => new { e.CoOwnerId, e.StatusEnum, e.CreatedAt }, "idx_fund_additions_co_owner_status_enum");

            entity.HasIndex(e => e.FundId, "idx_fund_additions_fund_id");

            entity.HasIndex(e => e.StatusEnum, "idx_fund_additions_status_enum");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CoOwnerId).HasColumnName("co_owner_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.PaymentMethodEnum).HasColumnName("payment_method_enum");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(EFundAdditionStatus.Completed)
                .HasColumnName("status_enum");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");

            entity.HasOne(d => d.CoOwner).WithMany(p => p.FundAdditions)
                .HasForeignKey(d => d.CoOwnerId)
                .HasConstraintName("fund_additions_co_owner_id_fkey");

            entity.HasOne(d => d.Fund).WithMany(p => p.FundAdditions)
                .HasForeignKey(d => d.FundId)
                .HasConstraintName("fund_additions_fund_id_fkey");
        });

        modelBuilder.Entity<FundUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fund_usage_pkey");

            entity.ToTable("fund_usage");

            entity.HasIndex(e => new { e.FundId, e.Amount, e.CreatedAt }, "idx_fund_usage_fund_amount");

            entity.HasIndex(e => e.FundId, "idx_fund_usage_fund_id");

            entity.HasIndex(e => e.MaintenanceCostId, "idx_fund_usage_maintenance_cost_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.MaintenanceCostId).HasColumnName("maintenance_cost_id");
            entity.Property(e => e.UsageTypeEnum).HasColumnName("usage_type_enum");

            entity.HasOne(d => d.Fund).WithMany(p => p.FundUsages)
                .HasForeignKey(d => d.FundId)
                .HasConstraintName("fund_usage_fund_id_fkey");

            entity.HasOne(d => d.MaintenanceCost).WithMany(p => p.FundUsages)
                .HasForeignKey(d => d.MaintenanceCostId)
                .HasConstraintName("fund_usage_maintenance_cost_id_fkey");
        });

        modelBuilder.Entity<FundUsageVote>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("fund_usage_votes");

            entity.HasIndex(e => new { e.FundUsageId, e.IsAgree }, "idx_fund_usage_votes_composite");

            entity.HasIndex(e => e.FundUsageId, "idx_fund_usage_votes_fund_usage_id");

            entity.HasIndex(e => e.UserId, "idx_fund_usage_votes_user_id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FundUsageId).HasColumnName("fund_usage_id");
            entity.Property(e => e.IsAgree).HasColumnName("is_agree");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.FundUsage).WithMany()
                .HasForeignKey(d => d.FundUsageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fund_usage_votes_fund_usage_id_fkey");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fund_usage_votes_user_id_fkey");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.ToTable("groups");

            entity.HasIndex(e => e.CreatedBy, "idx_groups_created_by");

            entity.HasIndex(e => e.FundId, "idx_groups_fund_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Groups)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("groups_created_by_fkey");

            entity.HasOne(d => d.Fund).WithMany(p => p.Groups)
                .HasForeignKey(d => d.FundId)
                .HasConstraintName("groups_fund_id_fkey");
        });

        modelBuilder.Entity<MaintenanceCost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("maintenance_costs_pkey");

            entity.ToTable("maintenance_costs");

            entity.HasIndex(e => e.BookingId, "idx_maintenance_costs_booking_id");

            entity.HasIndex(e => new { e.Cost, e.ServiceDate }, "idx_maintenance_costs_cost_date");

            entity.HasIndex(e => e.IsPaid, "idx_maintenance_costs_is_paid");

            entity.HasIndex(e => e.ServiceDate, "idx_maintenance_costs_service_date");

            entity.HasIndex(e => e.VehicleId, "idx_maintenance_costs_vehicle_id");

            entity.HasIndex(e => new { e.VehicleId, e.IsPaid, e.ServiceDate }, "idx_maintenance_costs_vehicle_paid");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Cost)
                .HasPrecision(10, 2)
                .HasColumnName("cost");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.IsPaid)
                .HasDefaultValue(false)
                .HasColumnName("is_paid");
            entity.Property(e => e.MaintenanceTypeEnum).HasColumnName("maintenance_type_enum");
            entity.Property(e => e.OdometerReading).HasColumnName("odometer_reading");
            entity.Property(e => e.ServiceDate).HasColumnName("service_date");
            entity.Property(e => e.ServiceProvider)
                .HasMaxLength(200)
                .HasColumnName("service_provider");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.MaintenanceCosts)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("maintenance_costs_booking_id_fkey");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.MaintenanceCosts)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("maintenance_costs_vehicle_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => new { e.Amount, e.PaidAt }, "idx_payments_amount_date");

            entity.HasIndex(e => e.FundAdditionId, "idx_payments_fund_addition_id");

            entity.HasIndex(e => e.PaidAt, "idx_payments_paid_at");

            entity.HasIndex(e => e.StatusEnum, "idx_payments_status_enum");

            entity.HasIndex(e => e.UserId, "idx_payments_user_id");

            entity.HasIndex(e => new { e.UserId, e.StatusEnum, e.CreatedAt }, "idx_payments_user_status_enum");

            entity.HasIndex(e => e.TransactionId, "payments_transaction_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FundAdditionId).HasColumnName("fund_addition_id");
            entity.Property(e => e.PaidAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentGateway)
                .HasMaxLength(50)
                .HasColumnName("payment_gateway");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(EPaymentStatus.Completed)
                .HasColumnName("status_enum");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.FundAddition).WithMany(p => p.Payments)
                .HasForeignKey(d => d.FundAdditionId)
                .HasConstraintName("payments_fund_addition_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("payments_user_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleNameEnum, "roles_role_name_enum_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleNameEnum).HasColumnName("role_name_enum");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.CreatedAt, "idx_users_created_at");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.Phone, "idx_users_phone");

            entity.HasIndex(e => e.StatusEnum, "idx_users_status_enum");

            entity.HasIndex(e => new { e.StatusEnum, e.CreatedAt }, "idx_users_status_enum_created");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ProfileImageUrl)
                .HasMaxLength(500)
                .HasColumnName("profile_image_url");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(EUserStatus.Inactive)
                .HasColumnName("status_enum");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("user_roles_role_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("user_roles_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_roles_pkey");
                        j.ToTable("user_roles");
                        j.HasIndex(new[] { "RoleId" }, "idx_user_roles_role_id");
                        j.HasIndex(new[] { "UserId" }, "idx_user_roles_user_id");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_refresh_tokens_pkey");

            entity.ToTable("user_refresh_tokens");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(255)
                .HasColumnName("refresh_token");

            entity.HasOne(d => d.User).WithOne(p => p.UserRefreshToken)
                .HasForeignKey<UserRefreshToken>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vehicles_pkey");

            entity.ToTable("vehicles");

            entity.HasIndex(e => e.GroupId, "idx_vehicles_group_id");

            entity.HasIndex(e => e.LicensePlate, "idx_vehicles_license_plate");

            entity.HasIndex(e => new { e.LocationLatitude, e.LocationLongitude }, "idx_vehicles_location");

            entity.HasIndex(e => e.PurchaseDate, "idx_vehicles_purchase_date");

            entity.HasIndex(e => e.StatusEnum, "idx_vehicles_status_enum");

            entity.HasIndex(e => e.Vin, "idx_vehicles_vin");

            entity.HasIndex(e => e.WarrantyUntil, "idx_vehicles_warranty_until");

            entity.HasIndex(e => e.LicensePlate, "vehicles_license_plate_key").IsUnique();

            entity.HasIndex(e => e.Vin, "vehicles_vin_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatteryCapacity)
                .HasPrecision(6, 2)
                .HasColumnName("battery_capacity");
            entity.Property(e => e.Brand)
                .HasMaxLength(100)
                .HasColumnName("brand");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DistanceTravelled)
                .HasDefaultValue(0)
                .HasColumnName("distance_travelled");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .HasColumnName("license_plate");
            entity.Property(e => e.LocationLatitude)
                .HasPrecision(10, 8)
                .HasColumnName("location_latitude");
            entity.Property(e => e.LocationLongitude)
                .HasPrecision(11, 8)
                .HasColumnName("location_longitude");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("model");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.PurchasePrice)
                .HasPrecision(15, 2)
                .HasColumnName("purchase_price");
            entity.Property(e => e.RangeKm).HasColumnName("range_km");
            entity.Property(e => e.StatusEnum)
                .HasDefaultValue(EVehicleStatus.InUse)
                .HasColumnName("status_enum");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Vin)
                .HasMaxLength(17)
                .HasColumnName("vin");
            entity.Property(e => e.WarrantyUntil).HasColumnName("warranty_until");
            entity.Property(e => e.Year).HasColumnName("year");

            entity.HasOne(d => d.Group).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("vehicles_group_id_fkey");
        });

        modelBuilder.Entity<VehicleCondition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vehicle_conditions_pkey");

            entity.ToTable("vehicle_conditions");

            entity.HasIndex(e => e.ConditionTypeEnum, "idx_vehicle_conditions_condition_type_enum");

            entity.HasIndex(e => e.DamageReported, "idx_vehicle_conditions_damage_reported");

            entity.HasIndex(e => e.ReportedBy, "idx_vehicle_conditions_reported_by");

            entity.HasIndex(e => new { e.VehicleId, e.DamageReported, e.CreatedAt }, "idx_vehicle_conditions_vehicle_damage");

            entity.HasIndex(e => e.VehicleId, "idx_vehicle_conditions_vehicle_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConditionTypeEnum).HasColumnName("condition_type_enum");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DamageReported)
                .HasDefaultValue(false)
                .HasColumnName("damage_reported");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FuelLevel)
                .HasPrecision(5, 2)
                .HasColumnName("fuel_level");
            entity.Property(e => e.OdometerReading).HasColumnName("odometer_reading");
            entity.Property(e => e.PhotoUrls).HasColumnName("photo_urls");
            entity.Property(e => e.ReportedBy).HasColumnName("reported_by");
            entity.Property(e => e.VehicleId).HasColumnName("vehicle_id");

            entity.HasOne(d => d.ReportedByNavigation).WithMany(p => p.VehicleConditions)
                .HasForeignKey(d => d.ReportedBy)
                .HasConstraintName("vehicle_conditions_reported_by_fkey");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.VehicleConditions)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("vehicle_conditions_vehicle_id_fkey");
        });

        modelBuilder.Entity<VehicleStation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vehicle_stations_pkey");

            entity.ToTable("vehicle_stations");

            entity.HasIndex(e => new { e.LocationLatitude, e.LocationLongitude }, "idx_vehicle_stations_location");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .HasColumnName("contact_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LocationLatitude)
                .HasPrecision(10, 8)
                .HasColumnName("location_latitude");
            entity.Property(e => e.LocationLongitude)
                .HasPrecision(11, 8)
                .HasColumnName("location_longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
