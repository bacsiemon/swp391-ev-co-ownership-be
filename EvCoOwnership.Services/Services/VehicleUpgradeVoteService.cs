using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    public class VehicleUpgradeVoteService : IVehicleUpgradeVoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VehicleUpgradeVoteService> _logger;

        public VehicleUpgradeVoteService(IUnitOfWork unitOfWork, ILogger<VehicleUpgradeVoteService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<VehicleUpgradeProposalResponse>> ProposeVehicleUpgradeAsync(ProposeVehicleUpgradeRequest request, Guid userId)
        {
            try
            {
                // Convert Guid to int (matching actual schema)
                int userIdInt = Math.Abs(userId.GetHashCode());

                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Validate user is a co-owner through CoOwner -> VehicleCoOwner relationship
                var isCoOwner = await _unitOfWork.DbContext.CoOwners
                    .AnyAsync(co => co.UserId == userIdInt && 
                        co.VehicleCoOwners.Any(vco => vco.VehicleId == request.VehicleId));

                if (!isCoOwner)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "You must be a co-owner of this vehicle to propose upgrades",
                        Data = null
                    };
                }

                // Validate estimated cost
                if (request.EstimatedCost <= 0)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "Estimated cost must be greater than 0",
                        Data = null
                    };
                }

                // Create the proposal
                var proposal = new VehicleUpgradeProposal
                {
                    VehicleId = request.VehicleId,
                    UpgradeType = request.UpgradeType,
                    Title = request.Title,
                    Description = request.Description,
                    EstimatedCost = request.EstimatedCost,
                    Justification = request.Justification,
                    ImageUrl = request.ImageUrl,
                    VendorName = request.VendorName,
                    VendorContact = request.VendorContact,
                    ProposedInstallationDate = request.ProposedInstallationDate,
                    EstimatedDurationDays = request.EstimatedDurationDays,
                    ProposedByUserId = userIdInt,
                    ProposedAt = DateTime.UtcNow,
                    Status = "Pending",
                    IsExecuted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.VehicleUpgradeProposalRepository.AddAsync(proposal);
                await _unitOfWork.SaveChangesAsync();

                // Automatically add proposer's approval vote
                var proposerVote = new VehicleUpgradeVote
                {
                    ProposalId = proposal.Id,
                    UserId = userIdInt,
                    IsAgree = true,
                    Comments = "Auto-approved as proposer",
                    VotedAt = DateTime.UtcNow
                };

                await _unitOfWork.VehicleUpgradeVoteRepository.AddAsync(proposerVote);
                await _unitOfWork.SaveChangesAsync();

                // Check if consensus reached
                await CheckAndUpdateProposalStatusAsync(proposal.Id);

                // Get the full proposal details to return
                var detailsResponse = await GetProposalDetailsInternalAsync(proposal.Id, userIdInt);
                
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 201,
                    Message = "Upgrade proposal created successfully",
                    Data = detailsResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating upgrade proposal");
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the upgrade proposal",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleUpgradeProposalResponse>> VoteOnUpgradeAsync(Guid proposalId, VoteVehicleUpgradeRequest request, Guid userId)
        {
            try
            {
                int proposalIdInt = Math.Abs(proposalId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Get the proposal
                var proposal = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .Include(p => p.Vehicle)
                    .FirstOrDefaultAsync(p => p.Id == proposalIdInt);

                if (proposal == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Upgrade proposal not found",
                        Data = null
                    };
                }

                // Check if proposal is still pending
                if (proposal.Status != "Pending")
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = $"Cannot vote on proposal with status: {proposal.Status}",
                        Data = null
                    };
                }

                // Validate user is a co-owner
                var isCoOwner = await _unitOfWork.DbContext.CoOwners
                    .AnyAsync(co => co.UserId == userIdInt && 
                        co.VehicleCoOwners.Any(vco => vco.VehicleId == proposal.VehicleId));

                if (!isCoOwner)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "You must be a co-owner of this vehicle to vote on upgrades",
                        Data = null
                    };
                }

                // Check if user already voted
                var existingVote = await _unitOfWork.DbContext.VehicleUpgradeVotes
                    .FirstOrDefaultAsync(v => v.ProposalId == proposalIdInt && v.UserId == userIdInt);

                if (existingVote != null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "You have already voted on this proposal",
                        Data = null
                    };
                }

                // Add the vote
                var vote = new VehicleUpgradeVote
                {
                    ProposalId = proposalIdInt,
                    UserId = userIdInt,
                    IsAgree = request.Approve,
                    Comments = request.Comments,
                    VotedAt = DateTime.UtcNow
                };

                await _unitOfWork.VehicleUpgradeVoteRepository.AddAsync(vote);
                await _unitOfWork.SaveChangesAsync();

                // If rejection, instantly reject the proposal
                if (!request.Approve)
                {
                    proposal.Status = "Rejected";
                    proposal.RejectedAt = DateTime.UtcNow;
                    proposal.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.VehicleUpgradeProposalRepository.Update(proposal);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    // Check if consensus reached
                    await CheckAndUpdateProposalStatusAsync(proposalIdInt);
                }

                // Get updated proposal details
                var detailsResponse = await GetProposalDetailsInternalAsync(proposalIdInt, userIdInt);
                
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 200,
                    Message = "Vote recorded successfully",
                    Data = detailsResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on upgrade proposal");
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while voting on the proposal",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleUpgradeProposalResponse>> GetUpgradeProposalDetailsAsync(Guid proposalId, Guid userId)
        {
            try
            {
                int proposalIdInt = Math.Abs(proposalId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Validate user is a co-owner of the vehicle (check through proposal)
                var proposal = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .FirstOrDefaultAsync(p => p.Id == proposalIdInt);

                if (proposal == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Upgrade proposal not found",
                        Data = null
                    };
                }

                var isCoOwner = await _unitOfWork.DbContext.CoOwners
                    .AnyAsync(co => co.UserId == userIdInt && 
                        co.VehicleCoOwners.Any(vco => vco.VehicleId == proposal.VehicleId));

                if (!isCoOwner)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "You must be a co-owner of this vehicle to view this proposal",
                        Data = null
                    };
                }

                var detailsResponse = await GetProposalDetailsInternalAsync(proposalIdInt, userIdInt);
                
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 200,
                    Message = "Proposal details retrieved successfully",
                    Data = detailsResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upgrade proposal details");
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving the proposal details",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<PendingUpgradeProposalsSummary>> GetPendingUpgradesForVehicleAsync(Guid vehicleId, Guid userId)
        {
            try
            {
                int vehicleIdInt = Math.Abs(vehicleId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleIdInt);
                if (vehicle == null)
                {
                    return new BaseResponse<PendingUpgradeProposalsSummary>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Validate user is a co-owner
                var isCoOwner = await _unitOfWork.DbContext.CoOwners
                    .AnyAsync(co => co.UserId == userIdInt && 
                        co.VehicleCoOwners.Any(vco => vco.VehicleId == vehicleIdInt));

                if (!isCoOwner)
                {
                    return new BaseResponse<PendingUpgradeProposalsSummary>
                    {
                        StatusCode = 403,
                        Message = "You must be a co-owner of this vehicle to view upgrade proposals",
                        Data = null
                    };
                }

                // Get pending proposals
                var pendingProposals = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .Include(p => p.ProposedByUser)
                    .Include(p => p.Votes)
                    .Where(p => p.VehicleId == vehicleIdInt && p.Status == "Pending")
                    .OrderByDescending(p => p.ProposedAt)
                    .ToListAsync();

                var totalCoOwners = await _unitOfWork.DbContext.VehicleCoOwners
                    .CountAsync(vco => vco.VehicleId == vehicleIdInt);

                var proposalSummaries = pendingProposals.Select(p => new VehicleUpgradeProposalResponse
                {
                    ProposalId = p.Id,
                    VehicleId = p.VehicleId,
                    VehicleName = vehicle.Name,
                    UpgradeType = p.UpgradeType,
                    UpgradeTypeName = p.UpgradeType.ToString(),
                    Title = p.Title,
                    Description = p.Description,
                    EstimatedCost = p.EstimatedCost,
                    Justification = p.Justification,
                    ImageUrl = p.ImageUrl,
                    VendorName = p.VendorName,
                    VendorContact = p.VendorContact,
                    ProposedInstallationDate = p.ProposedInstallationDate,
                    EstimatedDurationDays = p.EstimatedDurationDays,
                    ProposedByUserId = p.ProposedByUserId,
                    ProposedByUserName = p.ProposedByUser != null ? $"{p.ProposedByUser.FirstName} {p.ProposedByUser.LastName}".Trim() : "Unknown",
                    ProposedAt = p.ProposedAt,
                    TotalCoOwners = totalCoOwners,
                    RequiredApprovals = (totalCoOwners / 2) + 1,
                    CurrentApprovals = p.Votes?.Count(v => v.IsAgree) ?? 0,
                    CurrentRejections = p.Votes?.Count(v => !v.IsAgree) ?? 0,
                    ApprovalPercentage = totalCoOwners > 0 ? (decimal)(p.Votes?.Count(v => v.IsAgree) ?? 0) / totalCoOwners * 100 : 0,
                    VotingStatus = p.Status,
                    IsApproved = false,
                    IsRejected = false,
                    IsCancelled = false,
                    IsExecuted = false,
                    Votes = new List<UpgradeVoteDetailResponse>()
                }).ToList();

                var summary = new PendingUpgradeProposalsSummary
                {
                    VehicleId = vehicleIdInt,
                    VehicleName = vehicle.Name,
                    TotalPendingProposals = proposalSummaries.Count,
                    TotalPendingCost = proposalSummaries.Sum(p => p.EstimatedCost),
                    Proposals = proposalSummaries
                };

                return new BaseResponse<PendingUpgradeProposalsSummary>
                {
                    StatusCode = 200,
                    Message = "Pending upgrade proposals retrieved successfully",
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending upgrade proposals");
                return new BaseResponse<PendingUpgradeProposalsSummary>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving pending proposals",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleUpgradeProposalResponse>> MarkUpgradeAsExecutedAsync(Guid proposalId, MarkUpgradeExecutedRequest request, Guid userId)
        {
            try
            {
                int proposalIdInt = Math.Abs(proposalId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Get the proposal
                var proposal = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .Include(p => p.Vehicle)
                    .FirstOrDefaultAsync(p => p.Id == proposalIdInt);

                if (proposal == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Upgrade proposal not found",
                        Data = null
                    };
                }

                // Validate user is either admin or the proposer
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userIdInt);
                bool isAdmin = user?.RoleEnum == EUserRole.Admin;
                bool isProposer = proposal.ProposedByUserId == userIdInt;

                if (!isAdmin && !isProposer)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "Only admins or the proposer can mark upgrades as executed",
                        Data = null
                    };
                }

                // Check if proposal is approved
                if (proposal.Status != "Approved")
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "Only approved proposals can be marked as executed",
                        Data = null
                    };
                }

                // Check if already executed
                if (proposal.IsExecuted)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "This proposal has already been marked as executed",
                        Data = null
                    };
                }

                // Validate actual cost
                if (request.ActualCost <= 0)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "Actual cost must be greater than 0",
                        Data = null
                    };
                }

                // Get the vehicle's fund (via Vehicle.FundId)
                if (proposal.Vehicle?.FundId == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Vehicle does not have an associated fund",
                        Data = null
                    };
                }

                var fund = await _unitOfWork.FundRepository.GetByIdAsync(proposal.Vehicle.FundId.Value);
                if (fund == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Fund not found for this vehicle",
                        Data = null
                    };
                }

                // Check if fund has sufficient balance
                if (fund.CurrentBalance < request.ActualCost)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = $"Insufficient fund balance. Available: {fund.CurrentBalance:C}, Required: {request.ActualCost:C}",
                        Data = null
                    };
                }

                // Determine usage type based on upgrade type
                EUsageType usageType = proposal.UpgradeType switch
                {
                    EUpgradeType.BatteryUpgrade => EUsageType.Maintenance,
                    EUpgradeType.InsurancePackage => EUsageType.Insurance,
                    EUpgradeType.SafetyUpgrade => EUsageType.Maintenance,
                    EUpgradeType.PerformanceUpgrade => EUsageType.Maintenance,
                    _ => EUsageType.Other
                };

                // Create fund usage record
                var fundUsage = new FundUsage
                {
                    FundId = fund.Id,
                    UsageTypeEnum = usageType,
                    Amount = request.ActualCost,
                    Description = $"Upgrade: {proposal.Title}",
                    ImageUrl = request.InvoiceImageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.FundUsageRepository.AddAsync(fundUsage);
                await _unitOfWork.SaveChangesAsync();

                // Update fund balance
                fund.CurrentBalance -= request.ActualCost;
                fund.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.FundRepository.Update(fund);

                // Update proposal execution status
                proposal.IsExecuted = true;
                proposal.ExecutedAt = DateTime.UtcNow;
                proposal.ActualCost = request.ActualCost;
                proposal.ExecutionNotes = request.ExecutionNotes;
                proposal.FundUsageId = fundUsage.Id;
                proposal.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.VehicleUpgradeProposalRepository.Update(proposal);
                await _unitOfWork.SaveChangesAsync();

                // Get updated proposal details
                var detailsResponse = await GetProposalDetailsInternalAsync(proposalIdInt, userIdInt);
                
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 200,
                    Message = $"Upgrade marked as executed. Fund deducted: {request.ActualCost:C}",
                    Data = detailsResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking upgrade as executed");
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while marking the upgrade as executed",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleUpgradeProposalResponse>> CancelUpgradeProposalAsync(Guid proposalId, Guid userId)
        {
            try
            {
                int proposalIdInt = Math.Abs(proposalId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Get the proposal
                var proposal = await _unitOfWork.VehicleUpgradeProposalRepository.GetByIdAsync(proposalIdInt);
                if (proposal == null)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 404,
                        Message = "Upgrade proposal not found",
                        Data = null
                    };
                }

                // Validate user is either admin or the proposer
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userIdInt);
                bool isAdmin = user?.RoleEnum == EUserRole.Admin;
                bool isProposer = proposal.ProposedByUserId == userIdInt;

                if (!isAdmin && !isProposer)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 403,
                        Message = "Only admins or the proposer can cancel this proposal",
                        Data = null
                    };
                }

                // Check if proposal can be cancelled
                if (proposal.Status != "Pending" && proposal.Status != "Approved")
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = $"Cannot cancel proposal with status: {proposal.Status}",
                        Data = null
                    };
                }

                // Check if already executed
                if (proposal.IsExecuted)
                {
                    return new BaseResponse<VehicleUpgradeProposalResponse>
                    {
                        StatusCode = 400,
                        Message = "Cannot cancel an executed proposal",
                        Data = null
                    };
                }

                // Cancel the proposal
                proposal.Status = "Cancelled";
                proposal.CancelledAt = DateTime.UtcNow;
                proposal.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.VehicleUpgradeProposalRepository.Update(proposal);
                await _unitOfWork.SaveChangesAsync();

                // Get updated proposal details
                var detailsResponse = await GetProposalDetailsInternalAsync(proposalIdInt, userIdInt);
                
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 200,
                    Message = "Proposal cancelled successfully",
                    Data = detailsResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling upgrade proposal");
                return new BaseResponse<VehicleUpgradeProposalResponse>
                {
                    StatusCode = 500,
                    Message = "An error occurred while cancelling the proposal",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<List<UserUpgradeVotingHistoryResponse>>> GetUserUpgradeVotingHistoryAsync(Guid userId)
        {
            try
            {
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userIdInt);
                if (user == null)
                {
                    return new BaseResponse<List<UserUpgradeVotingHistoryResponse>>
                    {
                        StatusCode = 404,
                        Message = "User not found",
                        Data = new List<UserUpgradeVotingHistoryResponse>()
                    };
                }

                // Get proposals created by user
                var proposalsCreated = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .Include(p => p.Vehicle)
                    .Include(p => p.Votes)
                    .Where(p => p.ProposedByUserId == userIdInt)
                    .ToListAsync();

                // Get votes cast by user
                var votesCast = await _unitOfWork.DbContext.VehicleUpgradeVotes
                    .CountAsync(v => v.UserId == userIdInt);

                var approvalsGiven = await _unitOfWork.DbContext.VehicleUpgradeVotes
                    .CountAsync(v => v.UserId == userIdInt && v.IsAgree);

                var rejectionsGiven = votesCast - approvalsGiven;

                var history = new UserUpgradeVotingHistoryResponse
                {
                    UserId = userIdInt,
                    UserName = $"{user.FirstName} {user.LastName}".Trim(),
                    TotalProposalsCreated = proposalsCreated.Count,
                    TotalVotesCast = votesCast,
                    ApprovalsGiven = approvalsGiven,
                    RejectionsGiven = rejectionsGiven,
                    PendingVotes = proposalsCreated.Count(p => p.Status == "Pending"),
                    ProposalHistory = proposalsCreated.Select(p => new VehicleUpgradeProposalResponse
                    {
                        ProposalId = p.Id,
                        VehicleId = p.VehicleId,
                        VehicleName = p.Vehicle?.Name ?? "Unknown",
                        UpgradeType = p.UpgradeType,
                        UpgradeTypeName = p.UpgradeType.ToString(),
                        Title = p.Title,
                        Description = p.Description,
                        EstimatedCost = p.EstimatedCost,
                        ProposedByUserId = p.ProposedByUserId,
                        ProposedByUserName = $"{user.FirstName} {user.LastName}".Trim(),
                        ProposedAt = p.ProposedAt,
                        VotingStatus = p.Status,
                        IsApproved = p.Status == "Approved",
                        IsRejected = p.Status == "Rejected",
                        IsCancelled = p.Status == "Cancelled",
                        IsExecuted = p.IsExecuted,
                        Votes = new List<UpgradeVoteDetailResponse>()
                    }).OrderByDescending(p => p.ProposedAt).ToList()
                };

                return new BaseResponse<List<UserUpgradeVotingHistoryResponse>>
                {
                    StatusCode = 200,
                    Message = "Voting history retrieved successfully",
                    Data = new List<UserUpgradeVotingHistoryResponse> { history }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user voting history");
                return new BaseResponse<List<UserUpgradeVotingHistoryResponse>>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving voting history",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        public async Task<BaseResponse<VehicleUpgradeStatistics>> GetVehicleUpgradeStatisticsAsync(Guid vehicleId, Guid userId)
        {
            try
            {
                int vehicleIdInt = Math.Abs(vehicleId.GetHashCode());
                int userIdInt = Math.Abs(userId.GetHashCode());
                
                // Validate vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(vehicleIdInt);
                if (vehicle == null)
                {
                    return new BaseResponse<VehicleUpgradeStatistics>
                    {
                        StatusCode = 404,
                        Message = "Vehicle not found",
                        Data = null
                    };
                }

                // Validate user is a co-owner
                var isCoOwner = await _unitOfWork.DbContext.CoOwners
                    .AnyAsync(co => co.UserId == userIdInt && 
                        co.VehicleCoOwners.Any(vco => vco.VehicleId == vehicleIdInt));

                if (!isCoOwner)
                {
                    return new BaseResponse<VehicleUpgradeStatistics>
                    {
                        StatusCode = 403,
                        Message = "You must be a co-owner of this vehicle to view upgrade statistics",
                        Data = null
                    };
                }

                // Get all proposals for this vehicle
                var allProposals = await _unitOfWork.DbContext.VehicleUpgradeProposals
                    .Include(p => p.ProposedByUser)
                    .Include(p => p.Votes)
                    .Where(p => p.VehicleId == vehicleIdInt)
                    .ToListAsync();

                // Calculate upgrade counts by type
                var upgradesByType = new Dictionary<string, int>
                {
                    ["BatteryUpgrade"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.BatteryUpgrade && p.IsExecuted),
                    ["InsurancePackage"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.InsurancePackage && p.IsExecuted),
                    ["TechnologyUpgrade"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.TechnologyUpgrade && p.IsExecuted),
                    ["InteriorUpgrade"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.InteriorUpgrade && p.IsExecuted),
                    ["PerformanceUpgrade"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.PerformanceUpgrade && p.IsExecuted),
                    ["SafetyUpgrade"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.SafetyUpgrade && p.IsExecuted),
                    ["Other"] = allProposals.Count(p => p.UpgradeType == EUpgradeType.Other && p.IsExecuted)
                };

                var recentUpgrades = allProposals
                    .Where(p => p.IsExecuted)
                    .OrderByDescending(p => p.ExecutedAt)
                    .Take(10)
                    .Select(p => new VehicleUpgradeProposalResponse
                    {
                        ProposalId = p.Id,
                        VehicleId = p.VehicleId,
                        VehicleName = vehicle.Name,
                        UpgradeType = p.UpgradeType,
                        UpgradeTypeName = p.UpgradeType.ToString(),
                        Title = p.Title,
                        Description = p.Description,
                        EstimatedCost = p.EstimatedCost,
                        ProposedByUserId = p.ProposedByUserId,
                        ProposedByUserName = p.ProposedByUser != null ? $"{p.ProposedByUser.FirstName} {p.ProposedByUser.LastName}".Trim() : "Unknown",
                        ProposedAt = p.ProposedAt,
                        VotingStatus = p.Status,
                        IsApproved = true,
                        IsExecuted = true,
                        ExecutedAt = p.ExecutedAt,
                        ActualCost = p.ActualCost,
                        Votes = new List<UpgradeVoteDetailResponse>()
                    }).ToList();

                var statistics = new VehicleUpgradeStatistics
                {
                    VehicleId = vehicleIdInt,
                    VehicleName = vehicle.Name,
                    TotalUpgradesCompleted = allProposals.Count(p => p.IsExecuted),
                    TotalUpgradeCost = allProposals.Where(p => p.IsExecuted).Sum(p => p.ActualCost ?? 0),
                    PendingProposals = allProposals.Count(p => p.Status == "Pending"),
                    RejectedProposals = allProposals.Count(p => p.Status == "Rejected"),
                    UpgradesByType = upgradesByType,
                    RecentUpgrades = recentUpgrades
                };

                return new BaseResponse<VehicleUpgradeStatistics>
                {
                    StatusCode = 200,
                    Message = "Upgrade statistics retrieved successfully",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle upgrade statistics");
                return new BaseResponse<VehicleUpgradeStatistics>
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving upgrade statistics",
                    Data = null,
                    Errors = ex.Message
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Internal method to get proposal details (used by multiple public methods)
        /// </summary>
        private async Task<VehicleUpgradeProposalResponse?> GetProposalDetailsInternalAsync(int proposalId, int userId)
        {
            var proposal = await _unitOfWork.DbContext.VehicleUpgradeProposals
                .Include(p => p.Vehicle)
                .Include(p => p.ProposedByUser)
                .Include(p => p.Votes)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null)
                return null;

            var totalCoOwners = await _unitOfWork.DbContext.VehicleCoOwners
                .CountAsync(vco => vco.VehicleId == proposal.VehicleId);

            int approvalVotes = proposal.Votes?.Count(v => v.IsAgree) ?? 0;
            int rejectionVotes = proposal.Votes?.Count(v => !v.IsAgree) ?? 0;
            int requiredApprovals = (totalCoOwners / 2) + 1;

            return new VehicleUpgradeProposalResponse
            {
                ProposalId = proposal.Id,
                VehicleId = proposal.VehicleId,
                VehicleName = proposal.Vehicle?.Name ?? "Unknown",
                UpgradeType = proposal.UpgradeType,
                UpgradeTypeName = proposal.UpgradeType.ToString(),
                Title = proposal.Title,
                Description = proposal.Description,
                EstimatedCost = proposal.EstimatedCost,
                Justification = proposal.Justification,
                ImageUrl = proposal.ImageUrl,
                VendorName = proposal.VendorName,
                VendorContact = proposal.VendorContact,
                ProposedInstallationDate = proposal.ProposedInstallationDate,
                EstimatedDurationDays = proposal.EstimatedDurationDays,
                ProposedByUserId = proposal.ProposedByUserId,
                ProposedByUserName = proposal.ProposedByUser != null ? $"{proposal.ProposedByUser.FirstName} {proposal.ProposedByUser.LastName}".Trim() : "Unknown",
                ProposedAt = proposal.ProposedAt,
                TotalCoOwners = totalCoOwners,
                RequiredApprovals = requiredApprovals,
                CurrentApprovals = approvalVotes,
                CurrentRejections = rejectionVotes,
                ApprovalPercentage = totalCoOwners > 0 ? (decimal)approvalVotes / totalCoOwners * 100 : 0,
                VotingStatus = proposal.Status,
                IsApproved = proposal.Status == "Approved",
                IsRejected = proposal.Status == "Rejected",
                IsCancelled = proposal.Status == "Cancelled",
                IsExecuted = proposal.IsExecuted,
                ExecutedAt = proposal.ExecutedAt,
                ActualCost = proposal.ActualCost,
                ExecutionNotes = proposal.ExecutionNotes,
                Votes = proposal.Votes?.Select(v => new UpgradeVoteDetailResponse
                {
                    UserId = v.UserId,
                    UserName = v.User != null ? $"{v.User.FirstName} {v.User.LastName}".Trim() : "Unknown",
                    UserEmail = v.User?.Email ?? "",
                    HasVoted = true,
                    IsAgree = v.IsAgree,
                    Comments = v.Comments,
                    VotedAt = v.VotedAt
                }).OrderByDescending(v => v.VotedAt).ToList() ?? new List<UpgradeVoteDetailResponse>()
            };
        }

        /// <summary>
        /// Check if proposal has reached consensus and update status accordingly
        /// </summary>
        private async Task CheckAndUpdateProposalStatusAsync(int proposalId)
        {
            var proposal = await _unitOfWork.DbContext.VehicleUpgradeProposals
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Id == proposalId);

            if (proposal == null || proposal.Status != "Pending")
                return;

            // Get total co-owners
            var totalCoOwners = await _unitOfWork.DbContext.VehicleCoOwners
                .CountAsync(vco => vco.VehicleId == proposal.VehicleId);

            var approvalVotes = proposal.Votes?.Count(v => v.IsAgree) ?? 0;

            // Check if majority reached (> 50%)
            if (approvalVotes > totalCoOwners / 2.0m)
            {
                proposal.Status = "Approved";
                proposal.ApprovedAt = DateTime.UtcNow;
                proposal.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.VehicleUpgradeProposalRepository.Update(proposal);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        #endregion
    }
}
