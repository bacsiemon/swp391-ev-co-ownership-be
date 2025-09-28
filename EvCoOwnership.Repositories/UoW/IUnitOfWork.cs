using EvCoOwnership.Repositories.Interfaces;

namespace EvCoOwnership.Repositories.UoW
{
    public interface IUnitOfWork
    {
        // Repository properties
        IUserRepository UserRepository { get; }
        IBookingRepository BookingRepository { get; }
        ICheckInRepository CheckInRepository { get; }
        ICheckOutRepository CheckOutRepository { get; }
        ICoOwnerRepository CoOwnerRepository { get; }
        ICoOwnerGroupRepository CoOwnerGroupRepository { get; }
        IDrivingLicenseRepository DrivingLicenseRepository { get; }
        IFundRepository FundRepository { get; }
        IFundAdditionRepository FundAdditionRepository { get; }
        IFundUsageRepository FundUsageRepository { get; }
        IFundUsageVoteRepository FundUsageVoteRepository { get; }
        IGroupRepository GroupRepository { get; }
        IMaintenanceCostRepository MaintenanceCostRepository { get; }
        IPaymentRepository PaymentRepository { get; }
        IRoleRepository RoleRepository { get; }
        IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
        IVehicleRepository VehicleRepository { get; }
        IVehicleConditionRepository VehicleConditionRepository { get; }
        IVehicleStationRepository VehicleStationRepository { get; }

        Task<int> SaveChangesAsync();
    }
}