using EvCoOwnership.Repositories.Context;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Repositories;

namespace EvCoOwnership.Repositories.UoW
{
    public class UnitOfWork : IUnitOfWork
    {
        #region base
        private EvCoOwnershipDbContext _context;

        public UnitOfWork(EvCoOwnershipDbContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        #endregion

        // Repository backing fields
        private UserRepository _userRepository;
        private BookingRepository _bookingRepository;
        private CheckInRepository _checkInRepository;
        private CheckOutRepository _checkOutRepository;
        private CoOwnerRepository _coOwnerRepository;
        private CoOwnerGroupRepository _coOwnerGroupRepository;
        private DrivingLicenseRepository _drivingLicenseRepository;
        private FundRepository _fundRepository;
        private FundAdditionRepository _fundAdditionRepository;
        private FundUsageRepository _fundUsageRepository;
        private FundUsageVoteRepository _fundUsageVoteRepository;
        private GroupRepository _groupRepository;
        private MaintenanceCostRepository _maintenanceCostRepository;
        private PaymentRepository _paymentRepository;
        private RoleRepository _roleRepository;
        private UserRefreshTokenRepository _userRefreshTokenRepository;
        private VehicleRepository _vehicleRepository;
        private VehicleConditionRepository _vehicleConditionRepository;
        private VehicleStationRepository _vehicleStationRepository;

        // Repository properties with lazy initialization
        public IUserRepository UserRepository { get { return _userRepository ??= new UserRepository(_context); } }
        public IBookingRepository BookingRepository { get { return _bookingRepository ??= new BookingRepository(_context); } }
        public ICheckInRepository CheckInRepository { get { return _checkInRepository ??= new CheckInRepository(_context); } }
        public ICheckOutRepository CheckOutRepository { get { return _checkOutRepository ??= new CheckOutRepository(_context); } }
        public ICoOwnerRepository CoOwnerRepository { get { return _coOwnerRepository ??= new CoOwnerRepository(_context); } }
        public ICoOwnerGroupRepository CoOwnerGroupRepository { get { return _coOwnerGroupRepository ??= new CoOwnerGroupRepository(_context); } }
        public IDrivingLicenseRepository DrivingLicenseRepository { get { return _drivingLicenseRepository ??= new DrivingLicenseRepository(_context); } }
        public IFundRepository FundRepository { get { return _fundRepository ??= new FundRepository(_context); } }
        public IFundAdditionRepository FundAdditionRepository { get { return _fundAdditionRepository ??= new FundAdditionRepository(_context); } }
        public IFundUsageRepository FundUsageRepository { get { return _fundUsageRepository ??= new FundUsageRepository(_context); } }
        public IFundUsageVoteRepository FundUsageVoteRepository { get { return _fundUsageVoteRepository ??= new FundUsageVoteRepository(_context); } }
        public IGroupRepository GroupRepository { get { return _groupRepository ??= new GroupRepository(_context); } }
        public IMaintenanceCostRepository MaintenanceCostRepository { get { return _maintenanceCostRepository ??= new MaintenanceCostRepository(_context); } }
        public IPaymentRepository PaymentRepository { get { return _paymentRepository ??= new PaymentRepository(_context); } }
        public IRoleRepository RoleRepository { get { return _roleRepository ??= new RoleRepository(_context); } }
        public IUserRefreshTokenRepository UserRefreshTokenRepository { get { return _userRefreshTokenRepository ??= new UserRefreshTokenRepository(_context); } }
        public IVehicleRepository VehicleRepository { get { return _vehicleRepository ??= new VehicleRepository(_context); } }
        public IVehicleConditionRepository VehicleConditionRepository { get { return _vehicleConditionRepository ??= new VehicleConditionRepository(_context); } }
        public IVehicleStationRepository VehicleStationRepository { get { return _vehicleStationRepository ??= new VehicleStationRepository(_context); } }
    }
}
