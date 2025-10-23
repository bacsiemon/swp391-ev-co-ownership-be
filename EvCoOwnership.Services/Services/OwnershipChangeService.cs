using EvCoOwnership.DTOs.Notifications;
using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.OwnershipDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for managing ownership percentage changes with group consensus
    /// </summary>
    public class OwnershipChangeService : IOwnershipChangeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OwnershipChangeService> _logger;

        public OwnershipChangeService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<OwnershipChangeService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<BaseResponse<OwnershipChangeRequestResponse>> ProposeOwnershipChangeAsync(
            ProposeOwnershipChangeRequest request,
            int proposedByUserId)
        {
            try
            {
                // Validate vehicle exists
                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .Include(v => v.VehicleCoOwners)
                    .ThenInclude(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(v => v.Id == request.VehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Validate user is a co-owner of this vehicle
                var proposerCoOwner = vehicle.VehicleCoOwners
                    .FirstOrDefault(vco => vco.CoOwner.UserId == proposedByUserId);

                if (proposerCoOwner == null)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "ONLY_CO_OWNERS_CAN_PROPOSE_OWNERSHIP_CHANGES",
                        Data = null
                    };
                }

                // Validate all co-owners in the proposed changes exist
                var coOwnerIds = request.ProposedChanges.Select(c => c.CoOwnerId).ToList();
                var actualCoOwners = vehicle.VehicleCoOwners
                    .Where(vco => coOwnerIds.Contains(vco.CoOwnerId))
                    .ToList();

                if (actualCoOwners.Count != coOwnerIds.Distinct().Count())
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_CO_OWNER_IDS_IN_PROPOSED_CHANGES",
                        Data = null
                    };
                }

                // Validate all vehicle co-owners are included in the changes
                if (actualCoOwners.Count != vehicle.VehicleCoOwners.Count)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 400,
                        Message = "ALL_CO_OWNERS_MUST_BE_INCLUDED_IN_OWNERSHIP_CHANGE",
                        Data = null
                    };
                }

                // Check for existing pending requests for this vehicle
                var hasPendingRequests = await _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .AnyAsync(ocr => ocr.VehicleId == request.VehicleId &&
                                     ocr.StatusEnum == EOwnershipChangeStatus.Pending);

                if (hasPendingRequests)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 409,
                        Message = "VEHICLE_HAS_PENDING_OWNERSHIP_CHANGE_REQUEST",
                        Data = null
                    };
                }

                // Create ownership change request
                var changeRequest = new OwnershipChangeRequest
                {
                    VehicleId = request.VehicleId,
                    ProposedByUserId = proposedByUserId,
                    Reason = request.Reason,
                    StatusEnum = EOwnershipChangeStatus.Pending,
                    RequiredApprovals = vehicle.VehicleCoOwners.Count,
                    CurrentApprovals = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.DbContext.Set<OwnershipChangeRequest>().Add(changeRequest);
                await _unitOfWork.SaveChangesAsync();

                // Create ownership change details
                foreach (var proposedChange in request.ProposedChanges)
                {
                    var coOwner = actualCoOwners.First(co => co.CoOwnerId == proposedChange.CoOwnerId);

                    var detail = new OwnershipChangeDetail
                    {
                        OwnershipChangeRequestId = changeRequest.Id,
                        CoOwnerId = proposedChange.CoOwnerId,
                        CurrentPercentage = coOwner.OwnershipPercentage,
                        ProposedPercentage = proposedChange.ProposedPercentage,
                        CurrentInvestment = coOwner.InvestmentAmount,
                        ProposedInvestment = proposedChange.ProposedInvestment ?? coOwner.InvestmentAmount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.DbContext.Set<OwnershipChangeDetail>().Add(detail);
                }

                // Create approval records for all co-owners
                foreach (var coOwner in vehicle.VehicleCoOwners)
                {
                    var approval = new OwnershipChangeApproval
                    {
                        OwnershipChangeRequestId = changeRequest.Id,
                        CoOwnerId = coOwner.CoOwnerId,
                        UserId = coOwner.CoOwner.UserId,
                        ApprovalStatusEnum = EApprovalStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.DbContext.Set<OwnershipChangeApproval>().Add(approval);
                }

                await _unitOfWork.SaveChangesAsync();

                // Send notifications to all co-owners (except proposer)
                var proposer = await _unitOfWork.DbContext.Set<User>()
                    .FirstOrDefaultAsync(u => u.Id == proposedByUserId);

                foreach (var coOwner in vehicle.VehicleCoOwners.Where(vco => vco.CoOwner.UserId != proposedByUserId))
                {
                    var changeDetail = request.ProposedChanges.First(c => c.CoOwnerId == coOwner.CoOwnerId);

                    var notificationData = new OwnershipChangeNotificationData
                    {
                        OwnershipChangeRequestId = changeRequest.Id,
                        VehicleId = vehicle.Id,
                        VehicleName = vehicle.Name,
                        LicensePlate = vehicle.LicensePlate,
                        ProposerName = $"{proposer?.FirstName} {proposer?.LastName}",
                        Reason = request.Reason,
                        YourCurrentPercentage = coOwner.OwnershipPercentage,
                        YourProposedPercentage = changeDetail.ProposedPercentage
                    };

                    await _notificationService.SendNotificationToUserAsync(new SendNotificationRequestDto
                    {
                        UserId = coOwner.CoOwner.UserId,
                        NotificationType = "OwnershipChangeProposed",
                        AdditionalData = JsonSerializer.Serialize(notificationData)
                    });
                }

                _logger.LogInformation($"Ownership change request {changeRequest.Id} created for vehicle {vehicle.Id} by user {proposedByUserId}");

                // Return response
                var response = await GetOwnershipChangeRequestAsync(changeRequest.Id, proposedByUserId);
                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 201,
                    Message = "OWNERSHIP_CHANGE_REQUEST_CREATED_SUCCESSFULLY",
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing ownership change");
                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<OwnershipChangeRequestResponse>> GetOwnershipChangeRequestAsync(
            int requestId,
            int userId)
        {
            try
            {
                var changeRequest = await _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .Include(ocr => ocr.Vehicle)
                    .Include(ocr => ocr.ProposedByUser)
                    .Include(ocr => ocr.OwnershipChangeDetails)
                    .ThenInclude(ocd => ocd.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(ocr => ocr.OwnershipChangeApprovals)
                    .ThenInclude(oca => oca.User)
                    .FirstOrDefaultAsync(ocr => ocr.Id == requestId);

                if (changeRequest == null)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 404,
                        Message = "OWNERSHIP_CHANGE_REQUEST_NOT_FOUND",
                        Data = null
                    };
                }

                // Validate user is authorized to view this request
                var isCoOwner = changeRequest.OwnershipChangeApprovals.Any(oca => oca.UserId == userId);
                if (!isCoOwner)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_THIS_REQUEST",
                        Data = null
                    };
                }

                var response = MapToResponse(changeRequest);

                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_CHANGE_REQUEST_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ownership change request {requestId}");
                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetVehicleOwnershipChangeRequestsAsync(
            int vehicleId,
            int userId,
            bool includeCompleted = false)
        {
            try
            {
                // Validate user is a co-owner of this vehicle
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<List<OwnershipChangeRequestResponse>>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_REQUESTS",
                        Data = null
                    };
                }

                var query = _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .Include(ocr => ocr.Vehicle)
                    .Include(ocr => ocr.ProposedByUser)
                    .Include(ocr => ocr.OwnershipChangeDetails)
                    .ThenInclude(ocd => ocd.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(ocr => ocr.OwnershipChangeApprovals)
                    .ThenInclude(oca => oca.User)
                    .Where(ocr => ocr.VehicleId == vehicleId);

                if (!includeCompleted)
                {
                    query = query.Where(ocr => ocr.StatusEnum == EOwnershipChangeStatus.Pending);
                }

                var requests = await query.OrderByDescending(ocr => ocr.CreatedAt).ToListAsync();

                var responses = requests.Select(MapToResponse).ToList();

                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 200,
                    Message = $"FOUND_{responses.Count}_OWNERSHIP_CHANGE_REQUESTS",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving vehicle ownership change requests for vehicle {vehicleId}");
                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetPendingApprovalsAsync(int userId)
        {
            try
            {
                var pendingApprovals = await _unitOfWork.DbContext.Set<OwnershipChangeApproval>()
                    .Include(oca => oca.OwnershipChangeRequest)
                    .ThenInclude(ocr => ocr.Vehicle)
                    .Include(oca => oca.OwnershipChangeRequest)
                    .ThenInclude(ocr => ocr.ProposedByUser)
                    .Include(oca => oca.OwnershipChangeRequest)
                    .ThenInclude(ocr => ocr.OwnershipChangeDetails)
                    .ThenInclude(ocd => ocd.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(oca => oca.OwnershipChangeRequest)
                    .ThenInclude(ocr => ocr.OwnershipChangeApprovals)
                    .ThenInclude(oca => oca.User)
                    .Where(oca => oca.UserId == userId &&
                                  oca.ApprovalStatusEnum == EApprovalStatus.Pending &&
                                  oca.OwnershipChangeRequest.StatusEnum == EOwnershipChangeStatus.Pending)
                    .OrderByDescending(oca => oca.CreatedAt)
                    .ToListAsync();

                var responses = pendingApprovals
                    .Select(pa => MapToResponse(pa.OwnershipChangeRequest))
                    .ToList();

                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 200,
                    Message = $"FOUND_{responses.Count}_PENDING_APPROVALS",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving pending approvals for user {userId}");
                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<OwnershipChangeRequestResponse>> ApproveOrRejectOwnershipChangeAsync(
            int requestId,
            ApproveOwnershipChangeRequest request,
            int userId)
        {
            try
            {
                var changeRequest = await _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .Include(ocr => ocr.Vehicle)
                    .ThenInclude(v => v.VehicleCoOwners)
                    .Include(ocr => ocr.ProposedByUser)
                    .Include(ocr => ocr.OwnershipChangeDetails)
                    .ThenInclude(ocd => ocd.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(ocr => ocr.OwnershipChangeApprovals)
                    .ThenInclude(oca => oca.User)
                    .FirstOrDefaultAsync(ocr => ocr.Id == requestId);

                if (changeRequest == null)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 404,
                        Message = "OWNERSHIP_CHANGE_REQUEST_NOT_FOUND",
                        Data = null
                    };
                }

                // Validate request is still pending
                if (changeRequest.StatusEnum != EOwnershipChangeStatus.Pending)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 400,
                        Message = $"REQUEST_ALREADY_{changeRequest.StatusEnum?.ToString().ToUpper()}",
                        Data = null
                    };
                }

                // Find user's approval record
                var userApproval = changeRequest.OwnershipChangeApprovals
                    .FirstOrDefault(oca => oca.UserId == userId);

                if (userApproval == null)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_APPROVE_THIS_REQUEST",
                        Data = null
                    };
                }

                // Validate user hasn't already responded
                if (userApproval.ApprovalStatusEnum != EApprovalStatus.Pending)
                {
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 400,
                        Message = $"ALREADY_{userApproval.ApprovalStatusEnum?.ToString().ToUpper()}",
                        Data = null
                    };
                }

                // Update approval
                userApproval.ApprovalStatusEnum = request.Approve ? EApprovalStatus.Approved : EApprovalStatus.Rejected;
                userApproval.Comments = request.Comments;
                userApproval.RespondedAt = DateTime.UtcNow;

                // If rejected, mark the entire request as rejected
                if (!request.Approve)
                {
                    changeRequest.StatusEnum = EOwnershipChangeStatus.Rejected;
                    changeRequest.FinalizedAt = DateTime.UtcNow;
                    changeRequest.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.SaveChangesAsync();

                    // Notify all co-owners about rejection
                    await NotifyOwnershipChangeResultAsync(changeRequest, false, userId);

                    _logger.LogInformation($"Ownership change request {requestId} rejected by user {userId}");

                    var rejectedResponse = MapToResponse(changeRequest);
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 200,
                        Message = "OWNERSHIP_CHANGE_REQUEST_REJECTED",
                        Data = rejectedResponse
                    };
                }

                // If approved, check if all co-owners have approved
                changeRequest.CurrentApprovals = changeRequest.OwnershipChangeApprovals
                    .Count(oca => oca.ApprovalStatusEnum == EApprovalStatus.Approved);
                changeRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                // If all approvals received, apply the changes
                if (changeRequest.CurrentApprovals >= changeRequest.RequiredApprovals)
                {
                    await ApplyOwnershipChangesAsync(changeRequest);

                    changeRequest.StatusEnum = EOwnershipChangeStatus.Approved;
                    changeRequest.FinalizedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();

                    // Notify all co-owners about approval and application
                    await NotifyOwnershipChangeResultAsync(changeRequest, true, userId);

                    _logger.LogInformation($"Ownership change request {requestId} approved and applied");

                    var approvedResponse = MapToResponse(changeRequest);
                    return new BaseResponse<OwnershipChangeRequestResponse>
                    {
                        StatusCode = 200,
                        Message = "OWNERSHIP_CHANGE_APPROVED_AND_APPLIED",
                        Data = approvedResponse
                    };
                }

                _logger.LogInformation($"User {userId} approved ownership change request {requestId}. {changeRequest.CurrentApprovals}/{changeRequest.RequiredApprovals} approvals received");

                var response = MapToResponse(changeRequest);
                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 200,
                    Message = "APPROVAL_RECORDED_WAITING_FOR_OTHER_CO_OWNERS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving/rejecting ownership change request {requestId}");
                return new BaseResponse<OwnershipChangeRequestResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<bool>> CancelOwnershipChangeRequestAsync(int requestId, int userId)
        {
            try
            {
                var changeRequest = await _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .FirstOrDefaultAsync(ocr => ocr.Id == requestId);

                if (changeRequest == null)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 404,
                        Message = "OWNERSHIP_CHANGE_REQUEST_NOT_FOUND",
                        Data = false
                    };
                }

                // Only proposer can cancel
                if (changeRequest.ProposedByUserId != userId)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 403,
                        Message = "ONLY_PROPOSER_CAN_CANCEL_REQUEST",
                        Data = false
                    };
                }

                // Can only cancel pending requests
                if (changeRequest.StatusEnum != EOwnershipChangeStatus.Pending)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = 400,
                        Message = $"CANNOT_CANCEL_REQUEST_WITH_STATUS_{changeRequest.StatusEnum?.ToString().ToUpper()}",
                        Data = false
                    };
                }

                changeRequest.StatusEnum = EOwnershipChangeStatus.Cancelled;
                changeRequest.FinalizedAt = DateTime.UtcNow;
                changeRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Ownership change request {requestId} cancelled by user {userId}");

                return new BaseResponse<bool>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_CHANGE_REQUEST_CANCELLED",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling ownership change request {requestId}");
                return new BaseResponse<bool>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = false,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<OwnershipChangeStatisticsResponse>> GetOwnershipChangeStatisticsAsync()
        {
            try
            {
                var allRequests = await _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .Include(ocr => ocr.OwnershipChangeApprovals)
                    .ToListAsync();

                var approvedRequests = allRequests.Where(r => r.StatusEnum == EOwnershipChangeStatus.Approved).ToList();
                var averageApprovalTime = 0.0;

                if (approvedRequests.Any())
                {
                    averageApprovalTime = approvedRequests
                        .Where(r => r.CreatedAt.HasValue && r.FinalizedAt.HasValue)
                        .Select(r => (r.FinalizedAt!.Value - r.CreatedAt!.Value).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average();
                }

                var stats = new OwnershipChangeStatisticsResponse
                {
                    TotalRequests = allRequests.Count,
                    PendingRequests = allRequests.Count(r => r.StatusEnum == EOwnershipChangeStatus.Pending),
                    ApprovedRequests = allRequests.Count(r => r.StatusEnum == EOwnershipChangeStatus.Approved),
                    RejectedRequests = allRequests.Count(r => r.StatusEnum == EOwnershipChangeStatus.Rejected),
                    CancelledRequests = allRequests.Count(r => r.StatusEnum == EOwnershipChangeStatus.Cancelled),
                    ExpiredRequests = allRequests.Count(r => r.StatusEnum == EOwnershipChangeStatus.Expired),
                    AverageApprovalTime = (decimal)averageApprovalTime,
                    LastRequestCreated = allRequests.Max(r => r.CreatedAt),
                    StatisticsGeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<OwnershipChangeStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_CHANGE_STATISTICS_RETRIEVED_SUCCESSFULLY",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ownership change statistics");
                return new BaseResponse<OwnershipChangeStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<List<OwnershipChangeRequestResponse>>> GetUserOwnershipChangeRequestsAsync(
            int userId,
            bool includeCompleted = false)
        {
            try
            {
                var query = _unitOfWork.DbContext.Set<OwnershipChangeRequest>()
                    .Include(ocr => ocr.Vehicle)
                    .Include(ocr => ocr.ProposedByUser)
                    .Include(ocr => ocr.OwnershipChangeDetails)
                    .ThenInclude(ocd => ocd.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(ocr => ocr.OwnershipChangeApprovals)
                    .ThenInclude(oca => oca.User)
                    .Where(ocr => ocr.ProposedByUserId == userId ||
                                  ocr.OwnershipChangeApprovals.Any(oca => oca.UserId == userId));

                if (!includeCompleted)
                {
                    query = query.Where(ocr => ocr.StatusEnum == EOwnershipChangeStatus.Pending);
                }

                var requests = await query.OrderByDescending(ocr => ocr.CreatedAt).ToListAsync();

                var responses = requests.Select(MapToResponse).ToList();

                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 200,
                    Message = $"FOUND_{responses.Count}_OWNERSHIP_CHANGE_REQUESTS",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user ownership change requests for user {userId}");
                return new BaseResponse<List<OwnershipChangeRequestResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Applies approved ownership changes to the actual VehicleCoOwner records
        /// </summary>
        private async Task ApplyOwnershipChangesAsync(OwnershipChangeRequest changeRequest)
        {
            foreach (var detail in changeRequest.OwnershipChangeDetails)
            {
                var vehicleCoOwner = changeRequest.Vehicle.VehicleCoOwners
                    .First(vco => vco.CoOwnerId == detail.CoOwnerId);

                vehicleCoOwner.OwnershipPercentage = detail.ProposedPercentage;
                vehicleCoOwner.InvestmentAmount = detail.ProposedInvestment;
                vehicleCoOwner.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Notifies all co-owners about the result of an ownership change request
        /// </summary>
        private async Task NotifyOwnershipChangeResultAsync(
            OwnershipChangeRequest changeRequest,
            bool approved,
            int respondingUserId)
        {
            var respondingUser = await _unitOfWork.DbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == respondingUserId);

            var notificationType = approved ? "OwnershipChangeApproved" : "OwnershipChangeRejected";

            foreach (var approval in changeRequest.OwnershipChangeApprovals)
            {
                var notificationData = new
                {
                    OwnershipChangeRequestId = changeRequest.Id,
                    VehicleId = changeRequest.VehicleId,
                    VehicleName = changeRequest.Vehicle.Name,
                    LicensePlate = changeRequest.Vehicle.LicensePlate,
                    Approved = approved,
                    RespondedBy = $"{respondingUser?.FirstName} {respondingUser?.LastName}"
                };

                await _notificationService.SendNotificationToUserAsync(new SendNotificationRequestDto
                {
                    UserId = approval.UserId,
                    NotificationType = notificationType,
                    AdditionalData = JsonSerializer.Serialize(notificationData)
                });
            }
        }

        /// <summary>
        /// Maps OwnershipChangeRequest entity to response DTO
        /// </summary>
        private OwnershipChangeRequestResponse MapToResponse(OwnershipChangeRequest changeRequest)
        {
            return new OwnershipChangeRequestResponse
            {
                Id = changeRequest.Id,
                VehicleId = changeRequest.VehicleId,
                VehicleName = changeRequest.Vehicle?.Name ?? string.Empty,
                LicensePlate = changeRequest.Vehicle?.LicensePlate ?? string.Empty,
                ProposedByUserId = changeRequest.ProposedByUserId,
                ProposerName = $"{changeRequest.ProposedByUser?.FirstName} {changeRequest.ProposedByUser?.LastName}",
                ProposerEmail = changeRequest.ProposedByUser?.Email ?? string.Empty,
                Reason = changeRequest.Reason,
                Status = changeRequest.StatusEnum?.ToString() ?? "Unknown",
                RequiredApprovals = changeRequest.RequiredApprovals,
                CurrentApprovals = changeRequest.CurrentApprovals,
                CreatedAt = changeRequest.CreatedAt,
                UpdatedAt = changeRequest.UpdatedAt,
                FinalizedAt = changeRequest.FinalizedAt,
                ProposedChanges = changeRequest.OwnershipChangeDetails?.Select(ocd => new OwnershipChangeDetailResponse
                {
                    Id = ocd.Id,
                    CoOwnerId = ocd.CoOwnerId,
                    UserId = ocd.CoOwner?.UserId ?? 0,
                    CoOwnerName = $"{ocd.CoOwner?.User?.FirstName} {ocd.CoOwner?.User?.LastName}",
                    Email = ocd.CoOwner?.User?.Email ?? string.Empty,
                    CurrentPercentage = ocd.CurrentPercentage,
                    ProposedPercentage = ocd.ProposedPercentage,
                    PercentageChange = ocd.ProposedPercentage - ocd.CurrentPercentage,
                    CurrentInvestment = ocd.CurrentInvestment,
                    ProposedInvestment = ocd.ProposedInvestment,
                    InvestmentChange = ocd.ProposedInvestment - ocd.CurrentInvestment
                }).ToList() ?? new List<OwnershipChangeDetailResponse>(),
                Approvals = changeRequest.OwnershipChangeApprovals?.Select(oca => new ApprovalResponse
                {
                    Id = oca.Id,
                    CoOwnerId = oca.CoOwnerId,
                    UserId = oca.UserId,
                    CoOwnerName = $"{oca.User?.FirstName} {oca.User?.LastName}",
                    Email = oca.User?.Email ?? string.Empty,
                    ApprovalStatus = oca.ApprovalStatusEnum?.ToString() ?? "Unknown",
                    Comments = oca.Comments,
                    RespondedAt = oca.RespondedAt
                }).ToList() ?? new List<ApprovalResponse>()
            };
        }

        #endregion
    }
}
