using EvCoOwnership.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

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
        IDrivingLicenseRepository DrivingLicenseRepository { get; }
        IFileUploadRepository FileUploadRepository { get; }
        IFundRepository FundRepository { get; }
        IFundAdditionRepository FundAdditionRepository { get; }
        IFundUsageRepository FundUsageRepository { get; }
        IFundUsageVoteRepository FundUsageVoteRepository { get; }
        IMaintenanceCostRepository MaintenanceCostRepository { get; }
        IPaymentRepository PaymentRepository { get; }
        IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
        IVehicleRepository VehicleRepository { get; }
        IVehicleCoOwnerRepository VehicleCoOwnerRepository { get; }
        IVehicleConditionRepository VehicleConditionRepository { get; }
        IVehicleStationRepository VehicleStationRepository { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}