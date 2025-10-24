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
        /// Also creates ownership history records for audit trail
        /// </summary>
        private async Task ApplyOwnershipChangesAsync(OwnershipChangeRequest changeRequest)
        {
            foreach (var detail in changeRequest.OwnershipChangeDetails)
            {
                var vehicleCoOwner = changeRequest.Vehicle.VehicleCoOwners
                    .First(vco => vco.CoOwnerId == detail.CoOwnerId);

                // Create ownership history record
                var historyRecord = new OwnershipHistory
                {
                    VehicleId = changeRequest.VehicleId,
                    CoOwnerId = detail.CoOwnerId,
                    UserId = detail.CoOwner.UserId,
                    OwnershipChangeRequestId = changeRequest.Id,
                    PreviousPercentage = detail.CurrentPercentage,
                    NewPercentage = detail.ProposedPercentage,
                    PercentageChange = detail.ProposedPercentage - detail.CurrentPercentage,
                    PreviousInvestment = detail.CurrentInvestment,
                    NewInvestment = detail.ProposedInvestment,
                    InvestmentChange = detail.ProposedInvestment - detail.CurrentInvestment,
                    ChangeTypeEnum = EOwnershipChangeType.Adjustment,
                    Reason = changeRequest.Reason,
                    ChangedByUserId = changeRequest.ProposedByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.DbContext.Set<OwnershipHistory>().Add(historyRecord);

                // Apply the ownership change
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

        #region Ownership History Methods

        public async Task<BaseResponse<List<OwnershipHistoryResponse>>> GetVehicleOwnershipHistoryAsync(
            int vehicleId,
            int userId,
            GetOwnershipHistoryRequest? request = null)
        {
            try
            {
                // Validate user is authorized to view this vehicle's history
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<List<OwnershipHistoryResponse>>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_HISTORY",
                        Data = null
                    };
                }

                request ??= new GetOwnershipHistoryRequest();

                var query = _unitOfWork.DbContext.Set<OwnershipHistory>()
                    .Include(oh => oh.Vehicle)
                    .Include(oh => oh.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(oh => oh.ChangedByUser)
                    .Where(oh => oh.VehicleId == vehicleId);

                // Apply filters
                if (!string.IsNullOrEmpty(request.ChangeType))
                {
                    if (Enum.TryParse<EOwnershipChangeType>(request.ChangeType, true, out var changeType))
                    {
                        query = query.Where(oh => oh.ChangeTypeEnum == changeType);
                    }
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(oh => oh.CreatedAt >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(oh => oh.CreatedAt <= request.EndDate.Value);
                }

                if (request.CoOwnerId.HasValue)
                {
                    query = query.Where(oh => oh.CoOwnerId == request.CoOwnerId.Value);
                }

                var totalCount = await query.CountAsync();

                var history = await query
                    .OrderByDescending(oh => oh.CreatedAt)
                    .Skip(request.Offset)
                    .Take(request.Limit)
                    .ToListAsync();

                var responses = history.Select(MapHistoryToResponse).ToList();

                return new BaseResponse<List<OwnershipHistoryResponse>>
                {
                    StatusCode = 200,
                    Message = $"FOUND_{responses.Count}_OWNERSHIP_HISTORY_RECORDS",
                    Data = responses,
                    AdditionalData = new { TotalCount = totalCount, Offset = request.Offset, Limit = request.Limit }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ownership history for vehicle {vehicleId}");
                return new BaseResponse<List<OwnershipHistoryResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleOwnershipTimelineResponse>> GetVehicleOwnershipTimelineAsync(
            int vehicleId,
            int userId)
        {
            try
            {
                // Validate user is authorized
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<VehicleOwnershipTimelineResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_TIMELINE",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .Include(v => v.VehicleCoOwners)
                    .ThenInclude(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<VehicleOwnershipTimelineResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var allHistory = await _unitOfWork.DbContext.Set<OwnershipHistory>()
                    .Include(oh => oh.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(oh => oh.ChangedByUser)
                    .Include(oh => oh.Vehicle)
                    .Where(oh => oh.VehicleId == vehicleId)
                    .OrderBy(oh => oh.CreatedAt)
                    .ToListAsync();

                var timeline = new VehicleOwnershipTimelineResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    VehicleCreatedAt = vehicle.CreatedAt,
                    TotalHistoryRecords = allHistory.Count,
                    AllChanges = allHistory.Select(MapHistoryToResponse).OrderByDescending(h => h.CreatedAt).ToList()
                };

                // Group by co-owner
                var coOwnerTimelines = new List<CoOwnerOwnershipTimeline>();
                foreach (var coOwner in vehicle.VehicleCoOwners)
                {
                    var coOwnerHistory = allHistory.Where(h => h.CoOwnerId == coOwner.CoOwnerId).ToList();

                    var coOwnerTimeline = new CoOwnerOwnershipTimeline
                    {
                        CoOwnerId = coOwner.CoOwnerId,
                        UserId = coOwner.CoOwner.UserId,
                        CoOwnerName = $"{coOwner.CoOwner.User.FirstName} {coOwner.CoOwner.User.LastName}",
                        Email = coOwner.CoOwner.User.Email,
                        CurrentPercentage = coOwner.OwnershipPercentage,
                        InitialPercentage = coOwnerHistory.FirstOrDefault()?.PreviousPercentage ?? coOwner.OwnershipPercentage,
                        TotalChange = coOwner.OwnershipPercentage - (coOwnerHistory.FirstOrDefault()?.PreviousPercentage ?? coOwner.OwnershipPercentage),
                        JoinedAt = coOwner.CreatedAt,
                        ChangeCount = coOwnerHistory.Count,
                        Changes = coOwnerHistory.Select(MapHistoryToResponse).ToList()
                    };

                    coOwnerTimelines.Add(coOwnerTimeline);
                }

                timeline.CoOwnersTimeline = coOwnerTimelines;

                return new BaseResponse<VehicleOwnershipTimelineResponse>
                {
                    StatusCode = 200,
                    Message = "VEHICLE_OWNERSHIP_TIMELINE_RETRIEVED_SUCCESSFULLY",
                    Data = timeline
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ownership timeline for vehicle {vehicleId}");
                return new BaseResponse<VehicleOwnershipTimelineResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<OwnershipSnapshotResponse>> GetOwnershipSnapshotAsync(
            int vehicleId,
            DateTime snapshotDate,
            int userId)
        {
            try
            {
                // Validate user is authorized
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<OwnershipSnapshotResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_SNAPSHOT",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<OwnershipSnapshotResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Get all history records up to the snapshot date
                var historyUpToDate = await _unitOfWork.DbContext.Set<OwnershipHistory>()
                    .Include(oh => oh.CoOwner)
                    .ThenInclude(co => co.User)
                    .Where(oh => oh.VehicleId == vehicleId && oh.CreatedAt <= snapshotDate)
                    .OrderBy(oh => oh.CreatedAt)
                    .ToListAsync();

                // Get current co-owners
                var currentCoOwners = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .Include(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                    .Where(vco => vco.VehicleId == vehicleId)
                    .ToListAsync();

                var snapshot = new OwnershipSnapshotResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    LicensePlate = vehicle.LicensePlate,
                    SnapshotDate = snapshotDate,
                    CoOwners = new List<CoOwnerSnapshot>()
                };

                // Calculate ownership at snapshot date for each co-owner
                foreach (var coOwner in currentCoOwners)
                {
                    var coOwnerHistoryUpToDate = historyUpToDate
                        .Where(h => h.CoOwnerId == coOwner.CoOwnerId)
                        .OrderByDescending(h => h.CreatedAt)
                        .FirstOrDefault();

                    var ownershipAtDate = coOwnerHistoryUpToDate?.NewPercentage ?? coOwner.OwnershipPercentage;
                    var investmentAtDate = coOwnerHistoryUpToDate?.NewInvestment ?? coOwner.InvestmentAmount;

                    // Only include if co-owner existed at snapshot date
                    if (coOwner.CreatedAt <= snapshotDate)
                    {
                        snapshot.CoOwners.Add(new CoOwnerSnapshot
                        {
                            CoOwnerId = coOwner.CoOwnerId,
                            UserId = coOwner.CoOwner.UserId,
                            CoOwnerName = $"{coOwner.CoOwner.User.FirstName} {coOwner.CoOwner.User.LastName}",
                            Email = coOwner.CoOwner.User.Email,
                            OwnershipPercentage = ownershipAtDate,
                            InvestmentAmount = investmentAtDate
                        });
                    }
                }

                snapshot.TotalPercentage = snapshot.CoOwners.Sum(co => co.OwnershipPercentage);

                return new BaseResponse<OwnershipSnapshotResponse>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_SNAPSHOT_RETRIEVED_SUCCESSFULLY",
                    Data = snapshot
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ownership snapshot for vehicle {vehicleId} at {snapshotDate}");
                return new BaseResponse<OwnershipSnapshotResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<OwnershipHistoryStatisticsResponse>> GetOwnershipHistoryStatisticsAsync(
            int vehicleId,
            int userId)
        {
            try
            {
                // Validate user is authorized
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.VehicleId == vehicleId && vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<OwnershipHistoryStatisticsResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED_TO_VIEW_VEHICLE_STATISTICS",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.DbContext.Set<Vehicle>()
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    return new BaseResponse<OwnershipHistoryStatisticsResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var allHistory = await _unitOfWork.DbContext.Set<OwnershipHistory>()
                    .Include(oh => oh.CoOwner)
                    .ThenInclude(co => co.User)
                    .Where(oh => oh.VehicleId == vehicleId)
                    .ToListAsync();

                var currentCoOwners = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .Where(vco => vco.VehicleId == vehicleId)
                    .CountAsync();

                var uniqueCoOwners = allHistory.Select(h => h.CoOwnerId).Distinct().Count();

                var changeTypeBreakdown = allHistory
                    .GroupBy(h => h.ChangeTypeEnum?.ToString() ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                var mostActiveCoOwner = allHistory
                    .GroupBy(h => new { h.CoOwnerId, h.UserId, CoOwnerName = $"{h.CoOwner.User.FirstName} {h.CoOwner.User.LastName}" })
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                var averageOwnership = currentCoOwners > 0 ? 100m / currentCoOwners : 0;

                var stats = new OwnershipHistoryStatisticsResponse
                {
                    VehicleId = vehicle.Id,
                    VehicleName = vehicle.Name,
                    TotalChanges = allHistory.Count,
                    TotalCoOwners = uniqueCoOwners,
                    CurrentCoOwners = currentCoOwners,
                    FirstChange = allHistory.Min(h => h.CreatedAt),
                    LastChange = allHistory.Max(h => h.CreatedAt),
                    AverageOwnershipPercentage = averageOwnership,
                    MostActiveCoOwnerId = mostActiveCoOwner?.Key.CoOwnerId ?? 0,
                    MostActiveCoOwnerName = mostActiveCoOwner?.Key.CoOwnerName,
                    MostActiveCoOwnerChanges = mostActiveCoOwner?.Count() ?? 0,
                    ChangeTypeBreakdown = changeTypeBreakdown,
                    StatisticsGeneratedAt = DateTime.UtcNow
                };

                return new BaseResponse<OwnershipHistoryStatisticsResponse>
                {
                    StatusCode = 200,
                    Message = "OWNERSHIP_HISTORY_STATISTICS_RETRIEVED_SUCCESSFULLY",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ownership history statistics for vehicle {vehicleId}");
                return new BaseResponse<OwnershipHistoryStatisticsResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<List<OwnershipHistoryResponse>>> GetCoOwnerOwnershipHistoryAsync(int userId)
        {
            try
            {
                var coOwner = await _unitOfWork.DbContext.Set<CoOwner>()
                    .FirstOrDefaultAsync(co => co.UserId == userId);

                if (coOwner == null)
                {
                    return new BaseResponse<List<OwnershipHistoryResponse>>
                    {
                        StatusCode = 404,
                        Message = "CO_OWNER_NOT_FOUND",
                        Data = new List<OwnershipHistoryResponse>()
                    };
                }

                var history = await _unitOfWork.DbContext.Set<OwnershipHistory>()
                    .Include(oh => oh.Vehicle)
                    .Include(oh => oh.CoOwner)
                    .ThenInclude(co => co.User)
                    .Include(oh => oh.ChangedByUser)
                    .Where(oh => oh.UserId == userId)
                    .OrderByDescending(oh => oh.CreatedAt)
                    .ToListAsync();

                var responses = history.Select(MapHistoryToResponse).ToList();

                return new BaseResponse<List<OwnershipHistoryResponse>>
                {
                    StatusCode = 200,
                    Message = $"FOUND_{responses.Count}_OWNERSHIP_HISTORY_RECORDS",
                    Data = responses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving co-owner ownership history for user {userId}");
                return new BaseResponse<List<OwnershipHistoryResponse>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        /// <summary>
        /// Maps OwnershipHistory entity to response DTO
        /// </summary>
        private OwnershipHistoryResponse MapHistoryToResponse(OwnershipHistory history)
        {
            return new OwnershipHistoryResponse
            {
                Id = history.Id,
                VehicleId = history.VehicleId,
                VehicleName = history.Vehicle?.Name ?? string.Empty,
                LicensePlate = history.Vehicle?.LicensePlate ?? string.Empty,
                CoOwnerId = history.CoOwnerId,
                UserId = history.UserId,
                CoOwnerName = $"{history.CoOwner?.User?.FirstName} {history.CoOwner?.User?.LastName}",
                Email = history.CoOwner?.User?.Email ?? string.Empty,
                OwnershipChangeRequestId = history.OwnershipChangeRequestId,
                PreviousPercentage = history.PreviousPercentage,
                NewPercentage = history.NewPercentage,
                PercentageChange = history.PercentageChange,
                PreviousInvestment = history.PreviousInvestment,
                NewInvestment = history.NewInvestment,
                InvestmentChange = history.InvestmentChange,
                ChangeType = history.ChangeTypeEnum?.ToString() ?? "Unknown",
                Reason = history.Reason,
                ChangedByUserId = history.ChangedByUserId,
                ChangedByName = history.ChangedByUser != null
                    ? $"{history.ChangedByUser.FirstName} {history.ChangedByUser.LastName}"
                    : null,
                CreatedAt = history.CreatedAt
            };
        }

        #endregion
    }
}
