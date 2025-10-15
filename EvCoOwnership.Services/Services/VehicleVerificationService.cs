using AutoMapper;
using EvCoOwnership.Repositories.DTOs.VehicleDTOs;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.UoW;
using Newtonsoft.Json;

namespace EvCoOwnership.Services.Services
{
    public class VehicleVerificationService : IVehicleVerificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VehicleVerificationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponse<VehicleDetailResponse>> GetVehicleDetailAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetVehicleWithVerificationHistoryAsync(vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<VehicleDetailResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var response = _mapper.Map<VehicleDetailResponse>(vehicle);

                return new BaseResponse<VehicleDetailResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<VehicleDetailResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<IEnumerable<VehicleDetailResponse>>> GetVehiclesPendingVerificationAsync()
        {
            try
            {
                var vehicles = await _unitOfWork.VehicleRepository.GetVehiclesPendingVerificationAsync();
                var response = _mapper.Map<IEnumerable<VehicleDetailResponse>>(vehicles);

                return new BaseResponse<IEnumerable<VehicleDetailResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<VehicleDetailResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<IEnumerable<VehicleDetailResponse>>> GetVehiclesByVerificationStatusAsync(EVehicleVerificationStatus status)
        {
            try
            {
                var vehicles = await _unitOfWork.VehicleRepository.GetVehiclesByVerificationStatusAsync(status);
                var response = _mapper.Map<IEnumerable<VehicleDetailResponse>>(vehicles);

                return new BaseResponse<IEnumerable<VehicleDetailResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<VehicleDetailResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<VehicleVerificationResponse>> VerifyVehicleAsync(VehicleVerificationRequest request, int staffId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<VehicleVerificationResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if vehicle is in a verifiable state
                if (vehicle.VerificationStatusEnum == EVehicleVerificationStatus.Verified &&
                    request.Status != EVehicleVerificationStatus.RequiresRecheck)
                {
                    return new BaseResponse<VehicleVerificationResponse>
                    {
                        StatusCode = 400,
                        Message = "VEHICLE_ALREADY_VERIFIED",
                        Data = null
                    };
                }

                // Update vehicle verification status
                vehicle.VerificationStatusEnum = request.Status;
                vehicle.UpdatedAt = DateTime.Now;

                // If rejected, set vehicle status to unavailable
                if (request.Status == EVehicleVerificationStatus.Rejected)
                {
                    vehicle.StatusEnum = EVehicleStatus.Unavailable;
                }
                // If verified, set vehicle status to available (if not already in use)
                else if (request.Status == EVehicleVerificationStatus.Verified &&
                         vehicle.StatusEnum != EVehicleStatus.InUse)
                {
                    vehicle.StatusEnum = EVehicleStatus.Available;
                }

                _unitOfWork.VehicleRepository.Update(vehicle);

                // Create verification history record
                var verificationHistory = _mapper.Map<VehicleVerificationHistory>(request);
                verificationHistory.StaffId = staffId;

                _unitOfWork.VehicleVerificationHistoryRepository.Create(verificationHistory);
                await _unitOfWork.SaveChangesAsync();

                // Get updated vehicle with staff info for response
                var updatedVehicle = await _unitOfWork.VehicleRepository.GetVehicleWithVerificationHistoryAsync(request.VehicleId);
                var latestHistory = updatedVehicle?.VehicleVerificationHistories
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefault();

                var response = _mapper.Map<VehicleVerificationResponse>(latestHistory);

                return new BaseResponse<VehicleVerificationResponse>
                {
                    StatusCode = 200,
                    Message = "VEHICLE_VERIFICATION_COMPLETED",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<VehicleVerificationResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<IEnumerable<VehicleVerificationResponse>>> GetVerificationHistoryAsync(int vehicleId)
        {
            try
            {
                var history = await _unitOfWork.VehicleVerificationHistoryRepository.GetVerificationHistoryByVehicleIdAsync(vehicleId);
                var response = _mapper.Map<IEnumerable<VehicleVerificationResponse>>(history);

                return new BaseResponse<IEnumerable<VehicleVerificationResponse>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<IEnumerable<VehicleVerificationResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<VehicleDetailResponse>> CreateVehicleAsync(VehicleCreateRequest request, int userId)
        {
            try
            {
                // Check VIN uniqueness
                var isVinUnique = await _unitOfWork.VehicleRepository.IsVinUniqueAsync(request.Vin);
                if (!isVinUnique)
                {
                    return new BaseResponse<VehicleDetailResponse>
                    {
                        StatusCode = 400,
                        Message = "VIN_ALREADY_EXISTS",
                        Data = null
                    };
                }

                // Check license plate uniqueness
                var isLicensePlateUnique = await _unitOfWork.VehicleRepository.IsLicensePlateUniqueAsync(request.LicensePlate);
                if (!isLicensePlateUnique)
                {
                    return new BaseResponse<VehicleDetailResponse>
                    {
                        StatusCode = 400,
                        Message = "LICENSE_PLATE_ALREADY_EXISTS",
                        Data = null
                    };
                }

                // Map and create vehicle
                var vehicle = _mapper.Map<Vehicle>(request);
                vehicle.CreatedBy = userId;

                _unitOfWork.VehicleRepository.Create(vehicle);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<VehicleDetailResponse>(vehicle);

                return new BaseResponse<VehicleDetailResponse>
                {
                    StatusCode = 201,
                    Message = "VEHICLE_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<VehicleDetailResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<bool>> RequestVerificationAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = false
                    };
                }

                if (vehicle.VerificationStatusEnum == EVehicleVerificationStatus.Verified)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 400,
                        Message = "VEHICLE_ALREADY_VERIFIED",
                        Data = false
                    };
                }

                if (vehicle.VerificationStatusEnum == EVehicleVerificationStatus.VerificationRequested)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 400,
                        Message = "VERIFICATION_ALREADY_REQUESTED",
                        Data = false
                    };
                }

                vehicle.VerificationStatusEnum = EVehicleVerificationStatus.VerificationRequested;
                vehicle.UpdatedAt = DateTime.Now;

                _unitOfWork.VehicleRepository.Update(vehicle);
                await _unitOfWork.SaveChangesAsync();

                return new BaseResponse<bool>
                {
                    StatusCode = 200,
                    Message = "VERIFICATION_REQUESTED_SUCCESSFULLY",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = false
                };
            }
        }
    }
}