using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.DisputeDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Interfaces;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for managing disputes
    /// NOTE: This is a mock implementation using in-memory storage
    /// In production, this should use a proper Dispute database table
    /// </summary>
    public class DisputeService : IDisputeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DisputeService> _logger;

        // In-memory storage (for demo purposes - should be replaced with database)
        private static readonly List<DisputeData> _disputes = new();
        private static readonly List<DisputeResponseData> _disputeResponses = new();
        private static int _nextDisputeId = 1;
        private static int _nextResponseId = 1;

        public DisputeService(
            IUnitOfWork unitOfWork,
            ILogger<DisputeService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Raise Disputes

        public async Task<BaseResponse<DisputeResponse>> RaiseBookingDisputeAsync(
            int userId,
            RaiseBookingDisputeRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} raising booking dispute for booking {BookingId}", 
                    userId, request.BookingId);

                // Validate user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. User is not a co-owner of this vehicle.",
                        Data = null
                    };
                }

                // Validate booking exists
                var booking = await _unitOfWork.BookingRepository
                    .GetQueryable()
                    .Include(b => b.Vehicle)
                    .Include(b => b.CoOwner).ThenInclude(co => co.User)
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.VehicleId == request.VehicleId);

                if (booking == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Booking not found",
                        Data = null
                    };
                }

                // Get user details
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = null
                    };
                }

                // Create dispute
                var dispute = new DisputeData
                {
                    DisputeId = _nextDisputeId++,
                    VehicleId = request.VehicleId,
                    DisputeType = EDisputeType.Booking,
                    Status = EDisputeStatus.Open,
                    Priority = ParsePriority(request.Priority),
                    Category = request.Category,
                    Title = request.Title,
                    Description = request.Description,
                    RequestedResolution = request.RequestedResolution,
                    InitiatorUserId = userId,
                    RespondentUserIds = request.RespondentUserIds,
                    EvidenceUrls = request.EvidenceUrls,
                    RelatedBookingId = request.BookingId,
                    CreatedAt = DateTime.UtcNow
                };

                _disputes.Add(dispute);

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 201,
                    Message = "Booking dispute raised successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising booking dispute");
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while raising the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<DisputeResponse>> RaiseCostSharingDisputeAsync(
            int userId,
            RaiseCostSharingDisputeRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} raising cost sharing dispute for vehicle {VehicleId}", 
                    userId, request.VehicleId);

                // Validate user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. User is not a co-owner of this vehicle.",
                        Data = null
                    };
                }

                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Get user details
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = null
                    };
                }

                // Create dispute
                var dispute = new DisputeData
                {
                    DisputeId = _nextDisputeId++,
                    VehicleId = request.VehicleId,
                    DisputeType = EDisputeType.CostSharing,
                    Status = EDisputeStatus.Open,
                    Priority = ParsePriority(request.Priority),
                    Category = request.Category,
                    Title = request.Title,
                    Description = request.Description,
                    RequestedResolution = request.RequestedResolution,
                    InitiatorUserId = userId,
                    RespondentUserIds = request.RespondentUserIds,
                    EvidenceUrls = request.EvidenceUrls,
                    RelatedPaymentId = request.PaymentId,
                    RelatedMaintenanceCostId = request.MaintenanceCostId,
                    RelatedFundUsageId = request.FundUsageId,
                    DisputedAmount = request.DisputedAmount,
                    ExpectedAmount = request.ExpectedAmount,
                    CreatedAt = DateTime.UtcNow
                };

                _disputes.Add(dispute);

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 201,
                    Message = "Cost sharing dispute raised successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising cost sharing dispute");
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while raising the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<DisputeResponse>> RaiseGroupDecisionDisputeAsync(
            int userId,
            RaiseGroupDecisionDisputeRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} raising group decision dispute for vehicle {VehicleId}", 
                    userId, request.VehicleId);

                // Validate user is co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. User is not a co-owner of this vehicle.",
                        Data = null
                    };
                }

                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Get user details
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = null
                    };
                }

                // Create dispute
                var dispute = new DisputeData
                {
                    DisputeId = _nextDisputeId++,
                    VehicleId = request.VehicleId,
                    DisputeType = EDisputeType.GroupDecision,
                    Status = EDisputeStatus.Open,
                    Priority = ParsePriority(request.Priority),
                    Category = request.Category,
                    Title = request.Title,
                    Description = request.Description,
                    RequestedResolution = request.RequestedResolution,
                    InitiatorUserId = userId,
                    RespondentUserIds = request.RespondentUserIds,
                    EvidenceUrls = request.EvidenceUrls,
                    RelatedFundUsageVoteId = request.FundUsageVoteId,
                    RelatedVehicleUpgradeProposalId = request.VehicleUpgradeProposalId,
                    RelatedOwnershipChangeId = request.OwnershipChangeId,
                    ViolatedPolicy = request.ViolatedPolicy,
                    CreatedAt = DateTime.UtcNow
                };

                _disputes.Add(dispute);

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 201,
                    Message = "Group decision dispute raised successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising group decision dispute");
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while raising the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Get Disputes

        public async Task<BaseResponse<DisputeResponse>> GetDisputeByIdAsync(
            int disputeId,
            int userId)
        {
            try
            {
                var dispute = _disputes.FirstOrDefault(d => d.DisputeId == disputeId);
                if (dispute == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Dispute not found",
                        Data = null
                    };
                }

                // Verify user has access (initiator, respondent, or co-owner)
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == dispute.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                var hasAccess = dispute.InitiatorUserId == userId ||
                               dispute.RespondentUserIds.Contains(userId) ||
                               isCoOwner;

                if (!hasAccess)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied",
                        Data = null
                    };
                }

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 200,
                    Message = "Dispute retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dispute {DisputeId}", disputeId);
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<DisputeListResponse>> GetDisputesAsync(
            int userId,
            GetDisputesRequest request)
        {
            try
            {
                // Get user's vehicles
                var userVehicleIds = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .Where(vco => vco.CoOwner.UserId == userId)
                    .Select(vco => vco.VehicleId)
                    .ToListAsync();

                // Filter disputes
                var query = _disputes.AsQueryable();

                // Filter by vehicle
                if (request.VehicleId.HasValue)
                {
                    query = query.Where(d => d.VehicleId == request.VehicleId.Value);
                }
                else
                {
                    query = query.Where(d => userVehicleIds.Contains(d.VehicleId));
                }

                // Filter by type
                if (!string.IsNullOrEmpty(request.DisputeType))
                {
                    var type = Enum.Parse<EDisputeType>(request.DisputeType);
                    query = query.Where(d => d.DisputeType == type);
                }

                // Filter by status
                if (!string.IsNullOrEmpty(request.Status))
                {
                    var status = Enum.Parse<EDisputeStatus>(request.Status);
                    query = query.Where(d => d.Status == status);
                }

                // Filter by priority
                if (!string.IsNullOrEmpty(request.Priority))
                {
                    var priority = ParsePriority(request.Priority);
                    query = query.Where(d => d.Priority == priority);
                }

                // Filter by initiator/respondent
                if (request.IsInitiator == true)
                {
                    query = query.Where(d => d.InitiatorUserId == userId);
                }
                if (request.IsRespondent == true)
                {
                    query = query.Where(d => d.RespondentUserIds.Contains(userId));
                }

                // Filter by date range
                if (request.StartDate.HasValue)
                {
                    query = query.Where(d => d.CreatedAt >= request.StartDate.Value);
                }
                if (request.EndDate.HasValue)
                {
                    query = query.Where(d => d.CreatedAt <= request.EndDate.Value);
                }

                // Calculate statistics
                var allDisputes = query.ToList();
                var statistics = CalculateDisputeStatistics(allDisputes);

                // Sort
                query = request.SortBy.ToLower() switch
                {
                    "updateddate" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.UpdatedAt ?? d.CreatedAt)
                        : query.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt),
                    "priority" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.Priority)
                        : query.OrderByDescending(d => d.Priority),
                    _ => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(d => d.CreatedAt)
                        : query.OrderByDescending(d => d.CreatedAt)
                };

                // Pagination
                var totalItems = query.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);
                var skip = (request.PageNumber - 1) * request.PageSize;
                var pagedDisputes = query.Skip(skip).Take(request.PageSize).ToList();

                // Map to summaries
                var summaries = new List<DisputeSummary>();
                foreach (var dispute in pagedDisputes)
                {
                    summaries.Add(await MapToDisputeSummary(dispute));
                }

                var response = new DisputeListResponse
                {
                    Disputes = summaries,
                    Statistics = statistics,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        HasPreviousPage = request.PageNumber > 1,
                        HasNextPage = request.PageNumber < totalPages
                    }
                };

                return new BaseResponse<DisputeListResponse>
                {
                    StatusCode = 200,
                    Message = "Disputes retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disputes");
                return new BaseResponse<DisputeListResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving disputes",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Respond and Update

        public async Task<BaseResponse<DisputeResponse>> RespondToDisputeAsync(
            int disputeId,
            int userId,
            RespondToDisputeRequest request)
        {
            try
            {
                var dispute = _disputes.FirstOrDefault(d => d.DisputeId == disputeId);
                if (dispute == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Dispute not found",
                        Data = null
                    };
                }

                // Verify user is respondent or co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository
                    .GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == dispute.VehicleId &&
                                    vco.CoOwner.UserId == userId);

                if (!isCoOwner && !dispute.RespondentUserIds.Contains(userId))
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. You are not authorized to respond to this dispute.",
                        Data = null
                    };
                }

                // Create response
                var responseData = new DisputeResponseData
                {
                    ResponseId = _nextResponseId++,
                    DisputeId = disputeId,
                    UserId = userId,
                    Message = request.Message,
                    EvidenceUrls = request.EvidenceUrls,
                    AgreesWithDispute = request.AgreesWithDispute,
                    ProposedSolution = request.ProposedSolution,
                    CreatedAt = DateTime.UtcNow
                };

                _disputeResponses.Add(responseData);
                dispute.UpdatedAt = DateTime.UtcNow;

                // Auto-update status if needed
                if (dispute.Status == EDisputeStatus.Open)
                {
                    dispute.Status = EDisputeStatus.UnderReview;
                }

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 200,
                    Message = "Response added successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to dispute {DisputeId}", disputeId);
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while responding to the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<DisputeResponse>> UpdateDisputeStatusAsync(
            int disputeId,
            int userId,
            UpdateDisputeStatusRequest request)
        {
            try
            {
                // Check if user is admin (this should check user role)
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleEnum != EUserRole.Admin)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. Only administrators can update dispute status.",
                        Data = null
                    };
                }

                var dispute = _disputes.FirstOrDefault(d => d.DisputeId == disputeId);
                if (dispute == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Dispute not found",
                        Data = null
                    };
                }

                // Update status
                dispute.Status = Enum.Parse<EDisputeStatus>(request.Status);
                dispute.ResolutionNotes = request.ResolutionNotes;
                dispute.ActionsRequired = request.ActionsRequired;
                dispute.UpdatedAt = DateTime.UtcNow;

                if (request.Status == "Resolved" || request.Status == "Rejected")
                {
                    dispute.ResolvedByUserId = userId;
                    dispute.ResolvedAt = DateTime.UtcNow;
                }

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 200,
                    Message = $"Dispute status updated to {request.Status}",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dispute status {DisputeId}", disputeId);
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while updating the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<DisputeResponse>> WithdrawDisputeAsync(
            int disputeId,
            int userId,
            string reason)
        {
            try
            {
                var dispute = _disputes.FirstOrDefault(d => d.DisputeId == disputeId);
                if (dispute == null)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 404,
                        Message = "Dispute not found",
                        Data = null
                    };
                }

                // Verify user is initiator
                if (dispute.InitiatorUserId != userId)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 403,
                        Message = "Access denied. Only the initiator can withdraw the dispute.",
                        Data = null
                    };
                }

                // Check if dispute can be withdrawn
                if (dispute.Status == EDisputeStatus.Resolved || 
                    dispute.Status == EDisputeStatus.Rejected)
                {
                    return new BaseResponse<DisputeResponse>
                    {
                        StatusCode = 400,
                        Message = "Cannot withdraw a resolved or rejected dispute",
                        Data = null
                    };
                }

                dispute.Status = EDisputeStatus.Withdrawn;
                dispute.ResolutionNotes = $"Withdrawn by initiator. Reason: {reason}";
                dispute.UpdatedAt = DateTime.UtcNow;

                var response = await MapToDisputeResponse(dispute);

                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 200,
                    Message = "Dispute withdrawn successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing dispute {DisputeId}", disputeId);
                return new BaseResponse<DisputeResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while withdrawing the dispute",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #endregion

        #region Helper Methods

        private async Task<DisputeResponse> MapToDisputeResponse(DisputeData dispute)
        {
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(dispute.VehicleId);
            var initiator = await _unitOfWork.UserRepository.GetByIdAsync(dispute.InitiatorUserId);

            var respondents = new List<DisputeParticipant>();
            foreach (var respondentId in dispute.RespondentUserIds)
            {
                var respondent = await _unitOfWork.UserRepository.GetByIdAsync(respondentId);
                if (respondent != null)
                {
                    var hasResponded = _disputeResponses.Any(r => 
                        r.DisputeId == dispute.DisputeId && r.UserId == respondentId);
                    var respondedAt = _disputeResponses
                        .Where(r => r.DisputeId == dispute.DisputeId && r.UserId == respondentId)
                        .OrderBy(r => r.CreatedAt)
                        .FirstOrDefault()?.CreatedAt;

                    respondents.Add(new DisputeParticipant
                    {
                        UserId = respondentId,
                        Name = $"{respondent.FirstName} {respondent.LastName}",
                        Email = respondent.Email,
                        HasResponded = hasResponded,
                        RespondedAt = respondedAt
                    });
                }
            }

            var responses = new List<DisputeResponseItem>();
            var disputeResponsesList = _disputeResponses
                .Where(r => r.DisputeId == dispute.DisputeId)
                .OrderBy(r => r.CreatedAt)
                .ToList();

            foreach (var resp in disputeResponsesList)
            {
                var respUser = await _unitOfWork.UserRepository.GetByIdAsync(resp.UserId);
                responses.Add(new DisputeResponseItem
                {
                    ResponseId = resp.ResponseId,
                    UserId = resp.UserId,
                    UserName = respUser != null ? $"{respUser.FirstName} {respUser.LastName}" : "Unknown",
                    Message = resp.Message,
                    EvidenceUrls = resp.EvidenceUrls,
                    AgreesWithDispute = resp.AgreesWithDispute,
                    ProposedSolution = resp.ProposedSolution,
                    CreatedAt = resp.CreatedAt
                });
            }

            string? resolvedByName = null;
            if (dispute.ResolvedByUserId.HasValue)
            {
                var resolvedBy = await _unitOfWork.UserRepository.GetByIdAsync(dispute.ResolvedByUserId.Value);
                resolvedByName = resolvedBy != null ? $"{resolvedBy.FirstName} {resolvedBy.LastName}" : null;
            }

            return new DisputeResponse
            {
                DisputeId = dispute.DisputeId,
                VehicleId = dispute.VehicleId,
                VehicleName = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "Unknown",
                VehicleLicensePlate = vehicle?.LicensePlate ?? "Unknown",
                DisputeType = dispute.DisputeType.ToString(),
                Status = dispute.Status.ToString(),
                Priority = dispute.Priority.ToString(),
                Category = dispute.Category,
                Title = dispute.Title,
                Description = dispute.Description,
                RequestedResolution = dispute.RequestedResolution,
                InitiatorUserId = dispute.InitiatorUserId,
                InitiatorName = initiator != null ? $"{initiator.FirstName} {initiator.LastName}" : "Unknown",
                InitiatorEmail = initiator?.Email ?? "Unknown",
                Respondents = respondents,
                RelatedBookingId = dispute.RelatedBookingId,
                RelatedPaymentId = dispute.RelatedPaymentId,
                RelatedMaintenanceCostId = dispute.RelatedMaintenanceCostId,
                RelatedFundUsageId = dispute.RelatedFundUsageId,
                RelatedFundUsageVoteId = dispute.RelatedFundUsageVoteId,
                RelatedVehicleUpgradeProposalId = dispute.RelatedVehicleUpgradeProposalId,
                RelatedOwnershipChangeId = dispute.RelatedOwnershipChangeId,
                EvidenceUrls = dispute.EvidenceUrls,
                DisputedAmount = dispute.DisputedAmount,
                ExpectedAmount = dispute.ExpectedAmount,
                ViolatedPolicy = dispute.ViolatedPolicy,
                ResolutionNotes = dispute.ResolutionNotes,
                ActionsRequired = dispute.ActionsRequired,
                ResolvedByUserId = dispute.ResolvedByUserId,
                ResolvedByName = resolvedByName,
                ResolvedAt = dispute.ResolvedAt,
                Responses = responses,
                CreatedAt = dispute.CreatedAt,
                UpdatedAt = dispute.UpdatedAt,
                DaysOpen = (int)(DateTime.UtcNow - dispute.CreatedAt).TotalDays
            };
        }

        private async Task<DisputeSummary> MapToDisputeSummary(DisputeData dispute)
        {
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(dispute.VehicleId);
            var initiator = await _unitOfWork.UserRepository.GetByIdAsync(dispute.InitiatorUserId);
            var responseCount = _disputeResponses.Count(r => r.DisputeId == dispute.DisputeId);

            return new DisputeSummary
            {
                DisputeId = dispute.DisputeId,
                VehicleId = dispute.VehicleId,
                VehicleName = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "Unknown",
                DisputeType = dispute.DisputeType.ToString(),
                Status = dispute.Status.ToString(),
                Priority = dispute.Priority.ToString(),
                Category = dispute.Category,
                Title = dispute.Title,
                InitiatorName = initiator != null ? $"{initiator.FirstName} {initiator.LastName}" : "Unknown",
                RespondentCount = dispute.RespondentUserIds.Count,
                ResponseCount = responseCount,
                DisputedAmount = dispute.DisputedAmount,
                CreatedAt = dispute.CreatedAt,
                UpdatedAt = dispute.UpdatedAt,
                DaysOpen = (int)(DateTime.UtcNow - dispute.CreatedAt).TotalDays
            };
        }

        private DisputeStatistics CalculateDisputeStatistics(List<DisputeData> disputes)
        {
            var resolved = disputes.Where(d => 
                d.Status == EDisputeStatus.Resolved && d.ResolvedAt.HasValue).ToList();

            var avgResolutionDays = resolved.Any()
                ? resolved.Average(d => (d.ResolvedAt!.Value - d.CreatedAt).TotalDays)
                : 0;

            var resolutionRate = disputes.Any()
                ? (decimal)disputes.Count(d => d.Status == EDisputeStatus.Resolved) / disputes.Count * 100
                : 0;

            return new DisputeStatistics
            {
                TotalDisputes = disputes.Count,
                OpenDisputes = disputes.Count(d => d.Status == EDisputeStatus.Open),
                UnderReviewDisputes = disputes.Count(d => d.Status == EDisputeStatus.UnderReview),
                InMediationDisputes = disputes.Count(d => d.Status == EDisputeStatus.InMediation),
                ResolvedDisputes = disputes.Count(d => d.Status == EDisputeStatus.Resolved),
                RejectedDisputes = disputes.Count(d => d.Status == EDisputeStatus.Rejected),
                BookingDisputes = disputes.Count(d => d.DisputeType == EDisputeType.Booking),
                CostSharingDisputes = disputes.Count(d => d.DisputeType == EDisputeType.CostSharing),
                GroupDecisionDisputes = disputes.Count(d => d.DisputeType == EDisputeType.GroupDecision),
                HighPriorityDisputes = disputes.Count(d => d.Priority == EDisputePriority.High),
                CriticalPriorityDisputes = disputes.Count(d => d.Priority == EDisputePriority.Critical),
                AverageResolutionDays = Math.Round((decimal)avgResolutionDays, 2),
                ResolutionRate = Math.Round(resolutionRate, 2)
            };
        }

        private EDisputePriority ParsePriority(string priority)
        {
            return priority.ToLower() switch
            {
                "low" => EDisputePriority.Low,
                "medium" => EDisputePriority.Medium,
                "high" => EDisputePriority.High,
                "critical" => EDisputePriority.Critical,
                _ => EDisputePriority.Medium
            };
        }

        #endregion

        #region Internal Data Models (for in-memory storage)

        private class DisputeData
        {
            public int DisputeId { get; set; }
            public int VehicleId { get; set; }
            public EDisputeType DisputeType { get; set; }
            public EDisputeStatus Status { get; set; }
            public EDisputePriority Priority { get; set; }
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string RequestedResolution { get; set; } = string.Empty;
            public int InitiatorUserId { get; set; }
            public List<int> RespondentUserIds { get; set; } = new();
            public List<string> EvidenceUrls { get; set; } = new();
            public int? RelatedBookingId { get; set; }
            public int? RelatedPaymentId { get; set; }
            public int? RelatedMaintenanceCostId { get; set; }
            public int? RelatedFundUsageId { get; set; }
            public int? RelatedFundUsageVoteId { get; set; }
            public int? RelatedVehicleUpgradeProposalId { get; set; }
            public int? RelatedOwnershipChangeId { get; set; }
            public decimal? DisputedAmount { get; set; }
            public decimal? ExpectedAmount { get; set; }
            public string? ViolatedPolicy { get; set; }
            public string? ResolutionNotes { get; set; }
            public string? ActionsRequired { get; set; }
            public int? ResolvedByUserId { get; set; }
            public DateTime? ResolvedAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        private class DisputeResponseData
        {
            public int ResponseId { get; set; }
            public int DisputeId { get; set; }
            public int UserId { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<string> EvidenceUrls { get; set; } = new();
            public bool AgreesWithDispute { get; set; }
            public string? ProposedSolution { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        #endregion
    }
}
