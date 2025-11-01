using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.MaintenanceVoteDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service implementation for maintenance expenditure voting operations
    /// </summary>
    public class MaintenanceVoteService : IMaintenanceVoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MaintenanceVoteService> _logger;

        public MaintenanceVoteService(IUnitOfWork unitOfWork, ILogger<MaintenanceVoteService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Proposes a maintenance expenditure that requires co-owner voting approval
        /// </summary>
        public async Task<BaseResponse<MaintenanceExpenditureProposalResponse>> ProposeMaintenanceExpenditureAsync(
            ProposeMaintenanceExpenditureRequest request,
            int proposerUserId)
        {
            try
            {
                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if proposer is a co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == request.VehicleId && 
                                    vco.CoOwner.UserId == proposerUserId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "ONLY_CO_OWNERS_CAN_PROPOSE_MAINTENANCE_EXPENDITURE",
                        Data = null
                    };
                }

                // Validate maintenance cost exists
                var maintenanceCost = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(request.MaintenanceCostId);
                if (maintenanceCost == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_COST_NOT_FOUND",
                        Data = null
                    };
                }

                // Validate amount
                if (request.Amount <= 0)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_AMOUNT",
                        Data = null
                    };
                }

                // Get fund
                if (!vehicle.FundId.HasValue)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_HAS_NO_FUND",
                        Data = null
                    };
                }

                var fund = await _unitOfWork.FundRepository.GetByIdAsync(vehicle.FundId.Value);
                if (fund == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if fund has sufficient balance (warning only, not blocking)
                if ((fund.CurrentBalance ?? 0) < request.Amount)
                {
                    _logger.LogWarning("Fund balance insufficient for proposal. Current: {Balance}, Required: {Amount}", 
                        fund.CurrentBalance, request.Amount);
                }

                // Create FundUsage record with pending status (Amount = 0 initially, will be deducted after approval)
                var fundUsage = new FundUsage
                {
                    FundId = vehicle.FundId,
                    UsageTypeEnum = EUsageType.Maintenance,
                    Amount = 0, // Set to 0, will be updated to actual amount after approval
                    Description = $"[PENDING VOTE] {request.Reason}",
                    ImageUrl = request.ImageUrl,
                    MaintenanceCostId = request.MaintenanceCostId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FundUsageRepository.AddAsync(fundUsage);
                await _unitOfWork.SaveChangesAsync();

                // Auto-approve by proposer
                var proposerVote = new FundUsageVote
                {
                    FundUsageId = fundUsage.Id,
                    UserId = proposerUserId,
                    IsAgree = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FundUsageVoteRepository.AddAsync(proposerVote);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Maintenance expenditure proposal created: FundUsageId={FundUsageId}, Amount={Amount}, ProposedBy={UserId}",
                    fundUsage.Id, request.Amount, proposerUserId);

                // Build response
                var response = await BuildProposalResponseAsync(fundUsage.Id, request.Amount);

                return new BaseResponse<MaintenanceExpenditureProposalResponse>
                {
                    StatusCode = 201,
                    Message = "MAINTENANCE_EXPENDITURE_PROPOSAL_CREATED_SUCCESSFULLY",
                    Data = response,
                    AdditionalData = new
                    {
                        PendingAmount = request.Amount,
                        Note = "Proposal created. Waiting for other co-owners to vote."
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing maintenance expenditure");
                return new BaseResponse<MaintenanceExpenditureProposalResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Votes (approve/reject) on a proposed maintenance expenditure
        /// </summary>
        public async Task<BaseResponse<MaintenanceExpenditureProposalResponse>> VoteOnMaintenanceExpenditureAsync(
            int fundUsageId,
            VoteMaintenanceExpenditureRequest request,
            int voterUserId)
        {
            try
            {
                // Get fund usage
                var fundUsage = await _unitOfWork.FundUsageRepository.GetQueryable()
                    .Include(fu => fu.MaintenanceCost)
                    .FirstOrDefaultAsync(fu => fu.Id == fundUsageId);

                if (fundUsage == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_PROPOSAL_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if this is a pending proposal (Amount = 0 means still in voting)
                if (fundUsage.Amount != 0)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "PROPOSAL_ALREADY_FINALIZED",
                        Data = null
                    };
                }

                // Get fund and vehicle
                var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundUsage.FundId ?? 0);
                if (fund == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .FirstOrDefaultAsync(v => v.FundId == fund.Id);

                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if voter is a co-owner
                var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                    .AnyAsync(vco => vco.VehicleId == vehicle.Id &&
                                    vco.CoOwner.UserId == voterUserId &&
                                    vco.StatusEnum == EEContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "ONLY_CO_OWNERS_CAN_VOTE",
                        Data = null
                    };
                }

                // Check if user already voted
                var existingVote = await _unitOfWork.FundUsageVoteRepository.GetQueryable()
                    .FirstOrDefaultAsync(v => v.FundUsageId == fundUsageId && v.UserId == voterUserId);

                if (existingVote != null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_VOTED",
                        Data = null
                    };
                }

                // Create vote
                var vote = new FundUsageVote
                {
                    FundUsageId = fundUsageId,
                    UserId = voterUserId,
                    IsAgree = request.Approve,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FundUsageVoteRepository.AddAsync(vote);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Vote recorded: FundUsageId={FundUsageId}, UserId={UserId}, Approve={Approve}",
                    fundUsageId, voterUserId, request.Approve);

                // If rejected, mark proposal as rejected
                if (!request.Approve)
                {
                    fundUsage.Description = fundUsage.Description?.Replace("[PENDING VOTE]", "[REJECTED]") ?? "[REJECTED]";
                    await _unitOfWork.FundUsageRepository.UpdateAsync(fundUsage);
                    await _unitOfWork.SaveChangesAsync();

                    var rejectedResponse = await BuildProposalResponseAsync(fundUsageId, 0);
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 200,
                        Message = "VOTE_RECORDED_PROPOSAL_REJECTED",
                        Data = rejectedResponse
                    };
                }

                // Check voting consensus
                var (isApproved, approvalCount, totalCoOwners, requiredApprovals) = await CheckVotingConsensusAsync(vehicle.Id, fundUsageId);

                if (isApproved)
                {
                    // Extract actual amount from description or use maintenance cost
                    decimal actualAmount = fundUsage.MaintenanceCost?.Cost ?? 0;

                    // Check fund balance one more time before deducting
                    if ((fund.CurrentBalance ?? 0) < actualAmount)
                    {
                        fundUsage.Description = fundUsage.Description?.Replace("[PENDING VOTE]", "[APPROVED - INSUFFICIENT FUNDS]") ?? "[APPROVED - INSUFFICIENT FUNDS]";
                        await _unitOfWork.FundUsageRepository.UpdateAsync(fundUsage);
                        await _unitOfWork.SaveChangesAsync();

                        var insufficientResponse = await BuildProposalResponseAsync(fundUsageId, actualAmount);
                        return new BaseResponse<MaintenanceExpenditureProposalResponse>
                        {
                            StatusCode = 400,
                            Message = "PROPOSAL_APPROVED_BUT_INSUFFICIENT_FUND_BALANCE",
                            Data = insufficientResponse,
                            AdditionalData = new
                            {
                                CurrentBalance = fund.CurrentBalance,
                                RequiredAmount = actualAmount,
                                Shortfall = actualAmount - (fund.CurrentBalance ?? 0)
                            }
                        };
                    }

                    // Approve and deduct from fund
                    fundUsage.Amount = actualAmount;
                    fundUsage.Description = fundUsage.Description?.Replace("[PENDING VOTE]", "[APPROVED]") ?? "[APPROVED]";
                    
                    fund.CurrentBalance -= actualAmount;
                    fund.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.FundUsageRepository.UpdateAsync(fundUsage);
                    await _unitOfWork.FundRepository.UpdateAsync(fund);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Proposal approved and executed: FundUsageId={FundUsageId}, Amount={Amount}",
                        fundUsageId, actualAmount);

                    var approvedResponse = await BuildProposalResponseAsync(fundUsageId, actualAmount);
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 200,
                        Message = "PROPOSAL_APPROVED_AND_EXECUTED",
                        Data = approvedResponse,
                        AdditionalData = new
                        {
                            DeductedAmount = actualAmount,
                            RemainingBalance = fund.CurrentBalance
                        }
                    };
                }
                else
                {
                    // Still waiting for more votes
                    var pendingResponse = await BuildProposalResponseAsync(fundUsageId, fundUsage.MaintenanceCost?.Cost ?? 0);
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 200,
                        Message = "VOTE_RECORDED_WAITING_FOR_MORE_APPROVALS",
                        Data = pendingResponse,
                        AdditionalData = new
                        {
                            CurrentApprovals = approvalCount,
                            RequiredApprovals = requiredApprovals,
                            TotalCoOwners = totalCoOwners
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on maintenance expenditure");
                return new BaseResponse<MaintenanceExpenditureProposalResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets details of a specific maintenance expenditure proposal
        /// </summary>
        public async Task<BaseResponse<MaintenanceExpenditureProposalResponse>> GetMaintenanceProposalDetailsAsync(
            int fundUsageId,
            int requestingUserId)
        {
            try
            {
                var fundUsage = await _unitOfWork.FundUsageRepository.GetByIdAsync(fundUsageId);
                if (fundUsage == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_PROPOSAL_NOT_FOUND",
                        Data = null
                    };
                }

                // Get fund and vehicle for access check
                var fund = await _unitOfWork.FundRepository.GetByIdAsync(fundUsage.FundId ?? 0);
                if (fund == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "FUND_NOT_FOUND",
                        Data = null
                    };
                }

                var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                    .FirstOrDefaultAsync(v => v.FundId == fund.Id);

                if (vehicle == null)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Check access
                var hasAccess = await CheckUserAccessToVehicleAsync(vehicle.Id, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<MaintenanceExpenditureProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                var maintenanceCost = await _unitOfWork.MaintenanceCostRepository.GetByIdAsync(fundUsage.MaintenanceCostId ?? 0);
                var response = await BuildProposalResponseAsync(fundUsageId, fundUsage.Amount > 0 ? fundUsage.Amount : (maintenanceCost?.Cost ?? 0));

                return new BaseResponse<MaintenanceExpenditureProposalResponse>
                {
                    StatusCode = 200,
                    Message = "PROPOSAL_DETAILS_RETRIEVED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting proposal details");
                return new BaseResponse<MaintenanceExpenditureProposalResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets all pending maintenance expenditure proposals for a vehicle
        /// </summary>
        public async Task<BaseResponse<PendingMaintenanceProposalsSummary>> GetPendingProposalsForVehicleAsync(
            int vehicleId,
            int requestingUserId)
        {
            try
            {
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<PendingMaintenanceProposalsSummary>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                var hasAccess = await CheckUserAccessToVehicleAsync(vehicleId, requestingUserId);
                if (!hasAccess)
                {
                    return new BaseResponse<PendingMaintenanceProposalsSummary>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED_NOT_VEHICLE_CO_OWNER",
                        Data = null
                    };
                }

                // Get pending proposals (Amount = 0 and Description contains [PENDING VOTE])
                var pendingUsages = await _unitOfWork.FundUsageRepository.GetQueryable()
                    .Where(fu => fu.FundId == vehicle.FundId &&
                                fu.Amount == 0 &&
                                fu.Description.Contains("[PENDING VOTE]") &&
                                fu.MaintenanceCostId.HasValue)
                    .Include(fu => fu.MaintenanceCost)
                    .ToListAsync();

                var proposals = new List<MaintenanceExpenditureProposalResponse>();
                decimal totalPendingAmount = 0;

                foreach (var usage in pendingUsages)
                {
                    var amount = usage.MaintenanceCost?.Cost ?? 0;
                    totalPendingAmount += amount;
                    var proposal = await BuildProposalResponseAsync(usage.Id, amount);
                    proposals.Add(proposal);
                }

                var summary = new PendingMaintenanceProposalsSummary
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicle.Name ?? "",
                    TotalPendingProposals = proposals.Count,
                    TotalPendingAmount = totalPendingAmount,
                    Proposals = proposals
                };

                return new BaseResponse<PendingMaintenanceProposalsSummary>
                {
                    StatusCode = 200,
                    Message = "PENDING_PROPOSALS_RETRIEVED_SUCCESSFULLY",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending proposals");
                return new BaseResponse<PendingMaintenanceProposalsSummary>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Gets voting history for the requesting user
        /// </summary>
        public async Task<BaseResponse<UserVotingHistoryResponse>> GetUserVotingHistoryAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new BaseResponse<UserVotingHistoryResponse>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                var votes = await _unitOfWork.FundUsageVoteRepository.GetQueryable()
                    .Where(v => v.UserId == userId)
                    .Include(v => v.FundUsage)
                        .ThenInclude(fu => fu.MaintenanceCost)
                    .ToListAsync();

                var history = new UserVotingHistoryResponse
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}".Trim(),
                    TotalVotesCast = votes.Count,
                    ApprovalsGiven = votes.Count(v => v.IsAgree),
                    RejectionsGiven = votes.Count(v => !v.IsAgree),
                    PendingVotes = 0, // Will calculate
                    VotingHistory = new List<MaintenanceExpenditureProposalResponse>()
                };

                foreach (var vote in votes)
                {
                    if (vote.FundUsage?.MaintenanceCostId.HasValue == true)
                    {
                        var amount = vote.FundUsage.Amount > 0 ? vote.FundUsage.Amount : (vote.FundUsage.MaintenanceCost?.Cost ?? 0);
                        var proposal = await BuildProposalResponseAsync(vote.FundUsageId, amount);
                        history.VotingHistory.Add(proposal);
                    }
                }

                return new BaseResponse<UserVotingHistoryResponse>
                {
                    StatusCode = 200,
                    Message = "VOTING_HISTORY_RETRIEVED_SUCCESSFULLY",
                    Data = history
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voting history");
                return new BaseResponse<UserVotingHistoryResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Cancels a pending maintenance expenditure proposal
        /// </summary>
        public async Task<BaseResponse<object>> CancelMaintenanceProposalAsync(
            int fundUsageId,
            int requestingUserId)
        {
            try
            {
                var fundUsage = await _unitOfWork.FundUsageRepository.GetByIdAsync(fundUsageId);
                if (fundUsage == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "MAINTENANCE_PROPOSAL_NOT_FOUND",
                        Data = null
                    };
                }

                // Check if still pending
                if (fundUsage.Amount != 0)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 400,
                        Message = "PROPOSAL_ALREADY_FINALIZED_CANNOT_CANCEL",
                        Data = null
                    };
                }

                // Get proposer (first voter)
                var firstVote = await _unitOfWork.FundUsageVoteRepository.GetQueryable()
                    .Where(v => v.FundUsageId == fundUsageId)
                    .OrderBy(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstVote == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "PROPOSAL_DATA_INCOMPLETE",
                        Data = null
                    };
                }

                // Check if requesting user is proposer or admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(requestingUserId);
                if (user == null)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 404,
                        Message = "USER_NOT_FOUND",
                        Data = null
                    };
                }

                bool isProposer = firstVote.UserId == requestingUserId;
                bool isAdmin = user.RoleEnum == EUserRole.Admin || user.RoleEnum == EUserRole.Staff;

                if (!isProposer && !isAdmin)
                {
                    return new BaseResponse<object>
                    {
                        StatusCode = 403,
                        Message = "ONLY_PROPOSER_OR_ADMIN_CAN_CANCEL",
                        Data = null
                    };
                }

                // Mark as cancelled
                fundUsage.Description = fundUsage.Description?.Replace("[PENDING VOTE]", "[CANCELLED]") ?? "[CANCELLED]";
                await _unitOfWork.FundUsageRepository.UpdateAsync(fundUsage);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Proposal cancelled: FundUsageId={FundUsageId}, CancelledBy={UserId}",
                    fundUsageId, requestingUserId);

                return new BaseResponse<object>
                {
                    StatusCode = 200,
                    Message = "PROPOSAL_CANCELLED_SUCCESSFULLY",
                    Data = new { CancelledProposalId = fundUsageId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling proposal");
                return new BaseResponse<object>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Checks voting consensus (majority approval)
        /// </summary>
        private async Task<(bool isApproved, int approvalCount, int totalCoOwners, int requiredApprovals)> CheckVotingConsensusAsync(
            int vehicleId,
            int fundUsageId)
        {
            // Get total co-owners
            var totalCoOwners = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                .CountAsync(vco => vco.VehicleId == vehicleId && vco.StatusEnum == EEContractStatus.Active);

            // Get approval votes
            var approvalCount = await _unitOfWork.FundUsageVoteRepository.GetQueryable()
                .CountAsync(v => v.FundUsageId == fundUsageId && v.IsAgree);

            // Calculate required approvals (majority: > 50%)
            int requiredApprovals = (int)Math.Ceiling(totalCoOwners / 2.0);

            bool isApproved = approvalCount >= requiredApprovals;

            return (isApproved, approvalCount, totalCoOwners, requiredApprovals);
        }

        /// <summary>
        /// Builds proposal response with voting details
        /// </summary>
        private async Task<MaintenanceExpenditureProposalResponse> BuildProposalResponseAsync(int fundUsageId, decimal proposedAmount)
        {
            var fundUsage = await _unitOfWork.FundUsageRepository.GetQueryable()
                .Include(fu => fu.MaintenanceCost)
                .Include(fu => fu.Fund)
                .FirstOrDefaultAsync(fu => fu.Id == fundUsageId);

            if (fundUsage == null)
            {
                return new MaintenanceExpenditureProposalResponse();
            }

            var vehicle = await _unitOfWork.VehicleRepository.GetQueryable()
                .FirstOrDefaultAsync(v => v.FundId == fundUsage.FundId);

            if (vehicle == null)
            {
                return new MaintenanceExpenditureProposalResponse();
            }

            // Get all co-owners
            var coOwners = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                .Where(vco => vco.VehicleId == vehicle.Id && vco.StatusEnum == EEContractStatus.Active)
                .Include(vco => vco.CoOwner)
                    .ThenInclude(co => co.User)
                .ToListAsync();

            int totalCoOwners = coOwners.Count;

            // Get votes
            var votes = await _unitOfWork.FundUsageVoteRepository.GetQueryable()
                .Where(v => v.FundUsageId == fundUsageId)
                .Include(v => v.User)
                .ToListAsync();

            int approvals = votes.Count(v => v.IsAgree);
            int rejections = votes.Count(v => !v.IsAgree);
            int requiredApprovals = (int)Math.Ceiling(totalCoOwners / 2.0);

            // Get proposer (first voter)
            var proposer = votes.OrderBy(v => v.CreatedAt).FirstOrDefault();

            // Build vote details
            var voteDetails = new List<VoteDetailResponse>();
            foreach (var coOwner in coOwners)
            {
                var vote = votes.FirstOrDefault(v => v.UserId == coOwner.CoOwner.UserId);
                var coOwnerUser = coOwner.CoOwner.User;
                voteDetails.Add(new VoteDetailResponse
                {
                    UserId = coOwner.CoOwner.UserId,
                    UserName = coOwnerUser != null ? $"{coOwnerUser.FirstName} {coOwnerUser.LastName}".Trim() : "",
                    UserEmail = coOwnerUser?.Email ?? "",
                    HasVoted = vote != null,
                    IsAgree = vote?.IsAgree,
                    VotedAt = vote?.CreatedAt
                });
            }

            string votingStatus = "Pending";
            bool isApproved = false;
            bool isRejected = false;

            if (fundUsage.Description?.Contains("[APPROVED]") == true)
            {
                votingStatus = "Approved";
                isApproved = true;
            }
            else if (fundUsage.Description?.Contains("[REJECTED]") == true)
            {
                votingStatus = "Rejected";
                isRejected = true;
            }
            else if (fundUsage.Description?.Contains("[CANCELLED]") == true)
            {
                votingStatus = "Cancelled";
            }

            decimal approvalPercentage = totalCoOwners > 0 ? (decimal)approvals / totalCoOwners * 100 : 0;

            return new MaintenanceExpenditureProposalResponse
            {
                FundUsageId = fundUsageId,
                VehicleId = vehicle.Id,
                VehicleName = vehicle.Name ?? "",
                MaintenanceCostId = fundUsage.MaintenanceCostId ?? 0,
                MaintenanceDescription = fundUsage.MaintenanceCost?.Description ?? "",
                MaintenanceType = fundUsage.MaintenanceCost?.MaintenanceTypeEnum ?? EMaintenanceType.Routine,
                Amount = proposedAmount,
                Reason = fundUsage.Description?.Replace("[PENDING VOTE]", "").Replace("[APPROVED]", "").Replace("[REJECTED]", "").Replace("[CANCELLED]", "").Trim() ?? "",
                ImageUrl = fundUsage.ImageUrl,
                ProposedByUserId = proposer?.UserId ?? 0,
                ProposedByUserName = proposer?.User != null ? $"{proposer.User.FirstName} {proposer.User.LastName}".Trim() : "",
                ProposedAt = fundUsage.CreatedAt ?? DateTime.UtcNow,
                TotalCoOwners = totalCoOwners,
                RequiredApprovals = requiredApprovals,
                CurrentApprovals = approvals,
                CurrentRejections = rejections,
                ApprovalPercentage = approvalPercentage,
                VotingStatus = votingStatus,
                IsApproved = isApproved,
                IsRejected = isRejected,
                Votes = voteDetails
            };
        }

        /// <summary>
        /// Checks if user has access to vehicle (is co-owner or admin)
        /// </summary>
        private async Task<bool> CheckUserAccessToVehicleAsync(int vehicleId, int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (user.RoleEnum == EUserRole.Admin || user.RoleEnum == EUserRole.Staff)
            {
                return true;
            }

            var isCoOwner = await _unitOfWork.VehicleCoOwnerRepository.GetQueryable()
                .AnyAsync(vco => vco.VehicleId == vehicleId &&
                                vco.CoOwner.UserId == userId &&
                                vco.StatusEnum == EEContractStatus.Active);

            return isCoOwner;
        }

        #endregion
    }
}
