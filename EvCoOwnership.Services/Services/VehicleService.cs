using EvCoOwnership.DTOs.VehicleDTOs;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for vehicle management operations
    /// </summary>
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(IUnitOfWork unitOfWork, ILogger<VehicleService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new vehicle and assigns the creator as the primary owner
        /// </summary>
        public async Task<BaseResponse> CreateVehicleAsync(CreateVehicleRequest request, int createdById)
        {
            try
            {
                // Validate user eligibility to create vehicle
                var eligibilityResponse = await ValidateVehicleCreationEligibilityAsync(createdById);
                if (eligibilityResponse.StatusCode != 200)
                {
                    return eligibilityResponse;
                }

                // Check if license plate already exists
                var existingVehicle = await _unitOfWork.VehicleRepository
                    .GetAllAsync()
                    .ContinueWith(task => task.Result.FirstOrDefault(v => v.LicensePlate == request.LicensePlate));

                if (existingVehicle != null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "LICENSE_PLATE_ALREADY_EXISTS"
                    };
                }

                // Check if VIN already exists
                var existingVehicleByVin = await _unitOfWork.VehicleRepository
                    .GetAllAsync()
                    .ContinueWith(task => task.Result.FirstOrDefault(v => v.Vin == request.Vin));

                if (existingVehicleByVin != null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 409,
                        Message = "VIN_ALREADY_EXISTS"
                    };
                }

                // Get the co-owner record for the creator
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(createdById);
                if (coOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // Create vehicle
                    var vehicle = new Vehicle
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Brand = request.Brand,
                        Model = request.Model,
                        Year = request.Year,
                        Vin = request.Vin,
                        LicensePlate = request.LicensePlate,
                        Color = request.Color,
                        BatteryCapacity = request.BatteryCapacity,
                        RangeKm = request.RangeKm,
                        PurchaseDate = request.PurchaseDate,
                        PurchasePrice = request.PurchasePrice,
                        WarrantyUntil = request.WarrantyUntil,
                        DistanceTravelled = request.DistanceTravelled ?? 0,
                        StatusEnum = EVehicleStatus.Available,
                        VerificationStatusEnum = EVehicleVerificationStatus.Pending,
                        LocationLatitude = request.LocationLatitude,
                        LocationLongitude = request.LocationLongitude,
                        CreatedBy = createdById,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.VehicleRepository.AddAsync(vehicle);
                    await _unitOfWork.SaveChangesAsync();

                    // Create vehicle co-owner relationship for the creator
                    var vehicleCoOwner = new VehicleCoOwner
                    {
                        CoOwnerId = coOwner.UserId,
                        VehicleId = vehicle.Id,
                        OwnershipPercentage = request.InitialOwnershipPercentage,
                        InvestmentAmount = request.InitialInvestmentAmount,
                        StatusEnum = EContractStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.VehicleCoOwnerRepository.AddAsync(vehicleCoOwner);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var response = new VehicleResponse
                    {
                        Id = vehicle.Id,
                        Name = vehicle.Name,
                        Description = vehicle.Description,
                        Brand = vehicle.Brand,
                        Model = vehicle.Model,
                        Year = vehicle.Year,
                        Vin = vehicle.Vin,
                        LicensePlate = vehicle.LicensePlate,
                        Color = vehicle.Color,
                        BatteryCapacity = vehicle.BatteryCapacity,
                        RangeKm = vehicle.RangeKm,
                        PurchaseDate = vehicle.PurchaseDate,
                        PurchasePrice = vehicle.PurchasePrice,
                        WarrantyUntil = vehicle.WarrantyUntil,
                        DistanceTravelled = vehicle.DistanceTravelled,
                        Status = vehicle.StatusEnum.ToString(),
                        VerificationStatus = vehicle.VerificationStatusEnum.ToString(),
                        LocationLatitude = vehicle.LocationLatitude,
                        LocationLongitude = vehicle.LocationLongitude,
                        CreatedAt = vehicle.CreatedAt
                    };

                    return new BaseResponse
                    {
                        StatusCode = 201,
                        Message = "VEHICLE_CREATED_SUCCESSFULLY",
                        Data = response
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating vehicle for user {UserId}", createdById);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateVehicleAsync for user {UserId}", createdById);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Adds a co-owner to an existing vehicle by sending an invitation
        /// </summary>
        public async Task<BaseResponse> AddCoOwnerAsync(int vehicleId, AddCoOwnerRequest request, int invitedById)
        {
            try
            {
                // Get vehicle with co-owners
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithCoOwnersAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Check if requesting user is a co-owner of this vehicle
                var requestingUserCoOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(invitedById);
                if (requestingUserCoOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_CO_OWNER"
                    };
                }

                var isVehicleCoOwner = vehicle.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == requestingUserCoOwner.UserId &&
                    vco.StatusEnum == EContractStatus.Active);

                if (!isVehicleCoOwner)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Check if target user exists and is eligible to be co-owner
                var targetUser = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                if (targetUser == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "TARGET_USER_NOT_FOUND"
                    };
                }

                var targetCoOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(request.UserId);
                if (targetCoOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "TARGET_USER_NOT_CO_OWNER"
                    };
                }

                // Check if user is already a co-owner of this vehicle
                var existingCoOwnership = vehicle.VehicleCoOwners.FirstOrDefault(vco =>
                    vco.CoOwnerId == targetCoOwner.UserId);

                if (existingCoOwnership != null)
                {
                    if (existingCoOwnership.StatusEnum == EContractStatus.Active)
                    {
                        return new BaseResponse
                        {
                            StatusCode = 409,
                            Message = "USER_ALREADY_CO_OWNER_OF_VEHICLE"
                        };
                    }
                    else if (existingCoOwnership.StatusEnum == EContractStatus.Pending)
                    {
                        return new BaseResponse
                        {
                            StatusCode = 409,
                            Message = "INVITATION_ALREADY_PENDING"
                        };
                    }
                }

                // Validate ownership percentage
                var ownershipValidation = await ValidateOwnershipPercentageAsync(vehicleId, request.OwnershipPercentage);
                if (ownershipValidation.StatusCode != 200)
                {
                    return ownershipValidation;
                }

                // Create invitation
                var vehicleCoOwner = new VehicleCoOwner
                {
                    CoOwnerId = targetCoOwner.UserId,
                    VehicleId = vehicleId,
                    OwnershipPercentage = request.OwnershipPercentage,
                    InvestmentAmount = request.InvestmentAmount,
                    StatusEnum = EContractStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.VehicleCoOwnerRepository.AddAsync(vehicleCoOwner);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "CO_OWNER_INVITATION_SENT_SUCCESSFULLY",
                    Data = new
                    {
                        VehicleId = vehicleId,
                        InvitedUserId = request.UserId,
                        OwnershipPercentage = request.OwnershipPercentage,
                        InvestmentAmount = request.InvestmentAmount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding co-owner to vehicle {VehicleId}", vehicleId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Responds to a co-ownership invitation (accept or reject)
        /// </summary>
        public async Task<BaseResponse> RespondToInvitationAsync(int vehicleId, RespondToInvitationRequest request, int userId)
        {
            try
            {
                // Get user's co-owner record
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                // Find pending invitation
                var invitation = await _unitOfWork.VehicleCoOwnerRepository
                    .GetAllAsync()
                    .ContinueWith(task => task.Result.FirstOrDefault(vco =>
                        vco.VehicleId == vehicleId &&
                        vco.CoOwnerId == coOwner.UserId &&
                        vco.StatusEnum == EContractStatus.Pending));

                if (invitation == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "INVITATION_NOT_FOUND"
                    };
                }

                // Update invitation status
                if (request.Accept)
                {
                    // Re-validate ownership percentage before accepting
                    var ownershipValidation = await ValidateOwnershipPercentageAsync(vehicleId, invitation.OwnershipPercentage);
                    if (ownershipValidation.StatusCode != 200)
                    {
                        return new BaseResponse
                        {
                            StatusCode = 400,
                            Message = "OWNERSHIP_PERCENTAGE_NO_LONGER_VALID"
                        };
                    }

                    invitation.StatusEnum = EContractStatus.Active;
                }
                else
                {
                    invitation.StatusEnum = EContractStatus.Rejected;
                }

                invitation.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.VehicleCoOwnerRepository.UpdateAsync(invitation);
                await _unitOfWork.SaveChangesAsync();

                var message = request.Accept ? "INVITATION_ACCEPTED_SUCCESSFULLY" : "INVITATION_REJECTED_SUCCESSFULLY";

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = message,
                    Data = new
                    {
                        VehicleId = vehicleId,
                        UserId = userId,
                        Status = invitation.StatusEnum.ToString(),
                        OwnershipPercentage = invitation.OwnershipPercentage,
                        InvestmentAmount = invitation.InvestmentAmount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to invitation for vehicle {VehicleId}, user {UserId}", vehicleId, userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets vehicle information including co-owners
        /// </summary>
        public async Task<BaseResponse> GetVehicleAsync(int vehicleId, int userId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithCoOwnersAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Check if user has access to this vehicle
                var userCoOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                var hasAccess = userCoOwner != null && vehicle.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == userCoOwner.UserId &&
                    (vco.StatusEnum == EContractStatus.Active || vco.StatusEnum == EContractStatus.Pending));

                if (!hasAccess)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED"
                    };
                }

                var response = await MapVehicleToResponseAsync(vehicle);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "VEHICLE_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle {VehicleId}", vehicleId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets all vehicles for a specific user (as owner or co-owner)
        /// </summary>
        public async Task<BaseResponse> GetUserVehiclesAsync(int userId)
        {
            try
            {
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                var vehicles = await _unitOfWork.VehicleRepository.GetVehiclesByCoOwnerAsync(coOwner.UserId);
                var vehicleResponses = new List<VehicleResponse>();

                foreach (var vehicle in vehicles)
                {
                    var response = await MapVehicleToResponseAsync(vehicle);
                    vehicleResponses.Add(response);
                }

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "USER_VEHICLES_RETRIEVED_SUCCESSFULLY",
                    Data = vehicleResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles for user {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets pending co-ownership invitations for a user
        /// </summary>
        public async Task<BaseResponse> GetPendingInvitationsAsync(int userId)
        {
            try
            {
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                var invitations = await _unitOfWork.VehicleCoOwnerRepository.GetPendingInvitationsByCoOwnerAsync(coOwner.UserId);
                var invitationResponses = new List<CoOwnershipInvitationResponse>();

                foreach (var invitation in invitations)
                {
                    var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(invitation.VehicleId);
                    if (vehicle == null) continue;

                    // Get inviter information (vehicle creator or another co-owner)
                    var inviter = await _unitOfWork.UserRepository.GetByIdAsync(vehicle.CreatedBy ?? 0);

                    invitationResponses.Add(new CoOwnershipInvitationResponse
                    {
                        VehicleId = vehicle.Id,
                        VehicleName = vehicle.Name,
                        VehicleBrand = vehicle.Brand,
                        VehicleModel = vehicle.Model,
                        VehicleYear = vehicle.Year,
                        LicensePlate = vehicle.LicensePlate,
                        OwnershipPercentage = invitation.OwnershipPercentage,
                        InvestmentAmount = invitation.InvestmentAmount,
                        Status = invitation.StatusEnum.ToString(),
                        CreatedAt = invitation.CreatedAt,
                        Inviter = inviter != null ? new InviterInfo
                        {
                            UserId = inviter.Id,
                            FirstName = inviter.FirstName,
                            LastName = inviter.LastName,
                            Email = inviter.Email
                        } : null!
                    });
                }

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "PENDING_INVITATIONS_RETRIEVED_SUCCESSFULLY",
                    Data = invitationResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending invitations for user {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Validates if a user can create a vehicle
        /// </summary>
        public async Task<BaseResponse> ValidateVehicleCreationEligibilityAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                // Check if user is active
                if (user.StatusEnum != EUserStatus.Active)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_ACCOUNT_NOT_ACTIVE"
                    };
                }

                // Check if user is a co-owner
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (coOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "USER_NOT_CO_OWNER"
                    };
                }

                // Check if user has valid driving license
                var license = await _unitOfWork.DrivingLicenseRepository.GetByUserIdAsync(userId);
                if (license == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "NO_DRIVING_LICENSE_REGISTERED"
                    };
                }

                // Check if license is verified and not expired
                // For now, we assume all licenses are verified if they exist
                // In a real system, you would check a verification status property

                if (license.ExpiryDate.HasValue && license.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "DRIVING_LICENSE_EXPIRED"
                    };
                }

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "USER_ELIGIBLE_TO_CREATE_VEHICLE"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating vehicle creation eligibility for user {UserId}", userId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Validates ownership percentage for a vehicle
        /// </summary>
        public async Task<BaseResponse> ValidateOwnershipPercentageAsync(int vehicleId, decimal newOwnershipPercentage)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithCoOwnersAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Calculate total active ownership percentage
                var totalActiveOwnership = vehicle.VehicleCoOwners
                    .Where(vco => vco.StatusEnum == EContractStatus.Active)
                    .Sum(vco => vco.OwnershipPercentage);

                // Calculate total pending ownership percentage
                var totalPendingOwnership = vehicle.VehicleCoOwners
                    .Where(vco => vco.StatusEnum == EContractStatus.Pending)
                    .Sum(vco => vco.OwnershipPercentage);

                // Check if adding new ownership would exceed 100%
                if (totalActiveOwnership + totalPendingOwnership + newOwnershipPercentage > 100)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "OWNERSHIP_PERCENTAGE_EXCEEDS_LIMIT",
                        Data = new
                        {
                            TotalActiveOwnership = totalActiveOwnership,
                            TotalPendingOwnership = totalPendingOwnership,
                            NewOwnershipPercentage = newOwnershipPercentage,
                            MaxAvailable = 100 - totalActiveOwnership - totalPendingOwnership
                        }
                    };
                }

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_PERCENTAGE_VALID"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ownership percentage for vehicle {VehicleId}", vehicleId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Removes a co-owner from a vehicle
        /// </summary>
        public async Task<BaseResponse> RemoveCoOwnerAsync(int vehicleId, int coOwnerUserId, int requestingUserId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithCoOwnersAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Check if requesting user has permission (vehicle creator or admin)
                if (vehicle.CreatedBy != requestingUserId)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_ONLY_CREATOR_CAN_REMOVE"
                    };
                }

                // Get target co-owner
                var targetCoOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(coOwnerUserId);
                if (targetCoOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "TARGET_USER_NOT_CO_OWNER"
                    };
                }

                // Find the vehicle co-owner relationship
                var vehicleCoOwner = vehicle.VehicleCoOwners.FirstOrDefault(vco => vco.CoOwnerId == targetCoOwner.UserId);
                if (vehicleCoOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_RELATIONSHIP_NOT_FOUND"
                    };
                }

                // Cannot remove yourself if you're the only active owner
                var activeCoOwners = vehicle.VehicleCoOwners.Where(vco => vco.StatusEnum == EContractStatus.Active).ToList();
                if (activeCoOwners.Count == 1 && activeCoOwners.First().CoOwnerId == targetCoOwner.UserId)
                {
                    return new BaseResponse
                    {
                        StatusCode = 400,
                        Message = "CANNOT_REMOVE_LAST_ACTIVE_OWNER"
                    };
                }

                // Remove the co-owner relationship
                await _unitOfWork.VehicleCoOwnerRepository.DeleteAsync(vehicleCoOwner);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "CO_OWNER_REMOVED_SUCCESSFULLY"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing co-owner from vehicle {VehicleId}", vehicleId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Updates vehicle information
        /// </summary>
        public async Task<BaseResponse> UpdateVehicleAsync(int vehicleId, CreateVehicleRequest request, int userId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithCoOwnersAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND"
                    };
                }

                // Check if user is a co-owner of this vehicle
                var userCoOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                if (userCoOwner == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_CO_OWNER"
                    };
                }

                var isVehicleCoOwner = vehicle.VehicleCoOwners.Any(vco =>
                    vco.CoOwnerId == userCoOwner.UserId &&
                    vco.StatusEnum == EContractStatus.Active); if (!isVehicleCoOwner)
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER"
                    };
                }

                // Update vehicle information (excluding VIN and license plate as they shouldn't change)
                vehicle.Name = request.Name;
                vehicle.Description = request.Description;
                vehicle.Color = request.Color;
                vehicle.BatteryCapacity = request.BatteryCapacity;
                vehicle.RangeKm = request.RangeKm;
                vehicle.WarrantyUntil = request.WarrantyUntil;
                vehicle.DistanceTravelled = request.DistanceTravelled;
                vehicle.LocationLatitude = request.LocationLatitude;
                vehicle.LocationLongitude = request.LocationLongitude;
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.VehicleRepository.UpdateAsync(vehicle);
                await _unitOfWork.SaveChangesAsync();

                var response = await MapVehicleToResponseAsync(vehicle);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "VEHICLE_UPDATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle {VehicleId}", vehicleId);
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Gets all available vehicles for co-ownership or booking
        /// </summary>
        /// <remarks>
        /// BUSINESS LOGIC - ROLE-BASED ACCESS:
        /// - **Co-owner**: Can only see vehicles in their co-ownership groups (vehicles they are part of)
        /// - **Staff/Admin**: Can see ALL vehicles in the system
        /// 
        /// This ensures privacy - co-owners only discover vehicles within their existing groups,
        /// while staff/admin have full visibility for management purposes.
        /// 
        /// SECURITY CONSIDERATIONS:
        /// - Only shows verified vehicles by default (safety)
        /// - Co-owner contact info is included for legitimate inquiries
        /// - Role-based filtering prevents unauthorized vehicle discovery
        /// </remarks>
        public async Task<BaseResponse> GetAvailableVehiclesAsync(int userId, int pageIndex = 1, int pageSize = 10, string? filterByStatus = null, string? filterByVerificationStatus = null)
        {
            try
            {
                _logger.LogInformation("Getting available vehicles for userId: {UserId} - pageIndex: {PageIndex}, pageSize: {PageSize}", 
                    userId, pageIndex, pageSize);

                // Get user to check role
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND"
                    };
                }

                // Determine filtering based on role
                int? coOwnerIdFilter = null;
                if (user.RoleEnum == EUserRole.CoOwner)
                {
                    // Co-owner: only show vehicles in their groups
                    var coOwner = await _unitOfWork.CoOwnerRepository.GetByUserIdAsync(userId);
                    if (coOwner == null)
                    {
                        return new BaseResponse
                        {
                            StatusCode = 403,
                            Message = "CO_OWNER_PROFILE_NOT_FOUND"
                        };
                    }
                    coOwnerIdFilter = coOwner.UserId;
                    _logger.LogInformation("Co-owner access: filtering by coOwnerId {CoOwnerId}", coOwner.UserId);
                }
                else if (user.RoleEnum == EUserRole.Staff || user.RoleEnum == EUserRole.Admin)
                {
                    // Staff/Admin: show all vehicles (coOwnerIdFilter remains null)
                    _logger.LogInformation("{Role} access: showing all vehicles", user.RoleEnum);
                }
                else
                {
                    return new BaseResponse
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_INSUFFICIENT_PERMISSIONS"
                    };
                }

                // Parse filter parameters
                EVehicleStatus? statusFilter = null;
                if (!string.IsNullOrEmpty(filterByStatus) && Enum.TryParse<EVehicleStatus>(filterByStatus, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }

                EVehicleVerificationStatus? verificationFilter = null;
                if (!string.IsNullOrEmpty(filterByVerificationStatus) && Enum.TryParse<EVehicleVerificationStatus>(filterByVerificationStatus, true, out var parsedVerification))
                {
                    verificationFilter = parsedVerification;
                }

                // Get vehicles from repository with role-based filtering
                var (vehicles, totalCount) = await _unitOfWork.VehicleRepository.GetAllAvailableVehiclesAsync(
                    pageIndex, 
                    pageSize,
                    coOwnerIdFilter, // null for Staff/Admin, coOwnerId for Co-owner
                    statusFilter, 
                    verificationFilter);

                // Map to response DTOs
                var vehicleResponses = new List<VehicleResponse>();
                foreach (var vehicle in vehicles)
                {
                    var response = await MapVehicleToResponseAsync(vehicle);
                    vehicleResponses.Add(response);
                }

                var pagedResult = new PagedResult<VehicleResponse>(vehicleResponses, totalCount, pageIndex, pageSize);

                _logger.LogInformation("Retrieved {Count} available vehicles out of {Total} total", vehicles.Count, totalCount);

                return new BaseResponse
                {
                    StatusCode = 200,
                    Message = "AVAILABLE_VEHICLES_RETRIEVED_SUCCESSFULLY",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available vehicles");
                return new BaseResponse
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR"
                };
            }
        }

        /// <summary>
        /// Maps Vehicle model to VehicleResponse DTO
        /// </summary>
        private async Task<VehicleResponse> MapVehicleToResponseAsync(Vehicle vehicle)
        {
            var coOwners = new List<VehicleCoOwnerResponse>();

            foreach (var vco in vehicle.VehicleCoOwners)
            {
                var coOwner = await _unitOfWork.CoOwnerRepository.GetByIdAsync(vco.CoOwnerId);
                var user = coOwner != null ? await _unitOfWork.UserRepository.GetByIdAsync(coOwner.UserId) : null;

                if (user != null)
                {
                    coOwners.Add(new VehicleCoOwnerResponse
                    {
                        CoOwnerId = vco.CoOwnerId,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        OwnershipPercentage = vco.OwnershipPercentage,
                        InvestmentAmount = vco.InvestmentAmount,
                        Status = vco.StatusEnum.ToString(),
                        CreatedAt = vco.CreatedAt
                    });
                }
            }

            return new VehicleResponse
            {
                Id = vehicle.Id,
                Name = vehicle.Name,
                Description = vehicle.Description,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Vin = vehicle.Vin,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                BatteryCapacity = vehicle.BatteryCapacity,
                RangeKm = vehicle.RangeKm,
                PurchaseDate = vehicle.PurchaseDate,
                PurchasePrice = vehicle.PurchasePrice,
                WarrantyUntil = vehicle.WarrantyUntil,
                DistanceTravelled = vehicle.DistanceTravelled,
                Status = vehicle.StatusEnum.ToString(),
                VerificationStatus = vehicle.VerificationStatusEnum.ToString(),
                LocationLatitude = vehicle.LocationLatitude,
                LocationLongitude = vehicle.LocationLongitude,
                CreatedAt = vehicle.CreatedAt,
                CoOwners = coOwners
            };
        }
    }
}
