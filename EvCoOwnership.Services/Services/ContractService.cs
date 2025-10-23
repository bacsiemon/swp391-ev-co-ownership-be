using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.DTOs.ContractDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.UoW;
using EvCoOwnership.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Service for e-contract management
    /// NOTE: This is a mock implementation using in-memory storage
    /// In production, this should use proper Contract and ContractSignature database tables
    /// </summary>
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ContractService> _logger;

        // In-memory storage (for demo purposes - should be replaced with database)
        private static readonly List<ContractData> _contracts = new();
        private static readonly List<ContractSignatureData> _signatures = new();
        private static int _nextContractId = 1;
        private static int _nextSignatureId = 1;

        public ContractService(
            IUnitOfWork unitOfWork,
            ILogger<ContractService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Create Contract

        public async Task<BaseResponse<ContractResponse>> CreateContractAsync(
            int userId,
            CreateContractRequest request)
        {
            try
            {
                // Verify user is co-owner of the vehicle
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.CoOwner!.UserId == userId &&
                                   vco.VehicleId == request.VehicleId &&
                                   vco.StatusEnum == EContractStatus.Active);

                if (!isCoOwner)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED",
                        Data = null
                    };
                }

                // Verify vehicle exists
                var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 404,
                        Message = "VEHICLE_NOT_FOUND",
                        Data = null
                    };
                }

                // Verify all signatories are co-owners
                foreach (var signatoryId in request.SignatoryUserIds)
                {
                    var isSignatoryCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                        .AnyAsync(vco => vco.CoOwner!.UserId == signatoryId &&
                                       vco.VehicleId == request.VehicleId &&
                                       vco.StatusEnum == EContractStatus.Active);

                    if (!isSignatoryCoOwner)
                    {
                        return new BaseResponse<ContractResponse>
                        {
                            StatusCode = 400,
                            Message = $"SIGNATORY_NOT_COOWNER",
                            Data = null,
                            Errors = new { UserId = signatoryId }
                        };
                    }
                }

                // Parse template type
                if (!Enum.TryParse<EContractTemplateType>(request.TemplateType, true, out var templateType))
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "INVALID_TEMPLATE_TYPE",
                        Data = null
                    };
                }

                // Generate contract content from template
                var contractContent = GenerateContractContent(templateType, request, vehicle);

                // Create contract
                var contract = new ContractData
                {
                    ContractId = _nextContractId++,
                    VehicleId = request.VehicleId,
                    TemplateType = templateType,
                    Status = EEContractStatus.PendingSignatures,
                    Title = request.Title,
                    Description = request.Description,
                    ContractContent = contractContent,
                    CustomTerms = request.CustomTerms,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    EffectiveDate = request.EffectiveDate,
                    ExpiryDate = request.ExpiryDate,
                    SignatureDeadline = request.SignatureDeadline ?? DateTime.UtcNow.AddDays(30),
                    AttachmentUrls = request.AttachmentUrls ?? new List<string>(),
                    AutoActivate = request.AutoActivate
                };

                _contracts.Add(contract);

                // Create signature records for all signatories (including creator)
                var allSignatories = new List<int>(request.SignatoryUserIds);
                if (!allSignatories.Contains(userId))
                {
                    allSignatories.Insert(0, userId); // Creator signs first
                }

                int order = 1;
                foreach (var signatoryId in allSignatories)
                {
                    var signature = new ContractSignatureData
                    {
                        SignatureId = _nextSignatureId++,
                        ContractId = contract.ContractId,
                        UserId = signatoryId,
                        Status = signatoryId == userId ? ESignatureStatus.Signed : ESignatureStatus.Pending,
                        IsRequired = true,
                        SignatureOrder = order++,
                        SignedAt = signatoryId == userId ? DateTime.UtcNow : null,
                        IpAddress = signatoryId == userId ? "127.0.0.1" : null // Mock IP
                    };

                    _signatures.Add(signature);
                }

                // Auto-sign by creator
                if (allSignatories.Count == 1 && contract.AutoActivate)
                {
                    contract.Status = EEContractStatus.FullySigned;
                    contract.FullySignedAt = DateTime.UtcNow;
                    contract.ActivatedAt = DateTime.UtcNow;
                    contract.Status = EEContractStatus.Active;
                }

                var response = await MapToContractResponse(contract, userId);

                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 201,
                    Message = "CONTRACT_CREATED_SUCCESSFULLY",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Get Contract

        public async Task<BaseResponse<ContractResponse>> GetContractByIdAsync(
            int contractId,
            int userId)
        {
            try
            {
                var contract = _contracts.FirstOrDefault(c => c.ContractId == contractId);
                if (contract == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_NOT_FOUND",
                        Data = null
                    };
                }

                // Verify user has access (creator, signatory, or co-owner of the vehicle)
                var isCreator = contract.CreatedByUserId == userId;
                var isSignatory = _signatures.Any(s => s.ContractId == contractId && s.UserId == userId);
                var isCoOwner = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .AnyAsync(vco => vco.CoOwner!.UserId == userId &&
                                   vco.VehicleId == contract.VehicleId &&
                                   vco.StatusEnum == EContractStatus.Active);

                if (!isCreator && !isSignatory && !isCoOwner)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 403,
                        Message = "ACCESS_DENIED",
                        Data = null
                    };
                }

                var response = await MapToContractResponse(contract, userId);

                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract");
                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Get Contracts List

        public async Task<BaseResponse<ContractListResponse>> GetContractsAsync(
            int userId,
            GetContractsRequest request)
        {
            try
            {
                // Get user's vehicle IDs
                var userVehicleIds = await _unitOfWork.DbContext.Set<VehicleCoOwner>()
                    .Where(vco => vco.CoOwner!.UserId == userId && vco.StatusEnum == EContractStatus.Active)
                    .Select(vco => vco.VehicleId)
                    .ToListAsync();

                // Filter contracts
                var query = _contracts.AsEnumerable();

                // Filter by vehicle access
                query = query.Where(c => userVehicleIds.Contains(c.VehicleId) ||
                                        c.CreatedByUserId == userId ||
                                        _signatures.Any(s => s.ContractId == c.ContractId && s.UserId == userId));

                // Apply filters
                if (request.VehicleId.HasValue)
                    query = query.Where(c => c.VehicleId == request.VehicleId.Value);

                if (!string.IsNullOrEmpty(request.TemplateType) &&
                    Enum.TryParse<EContractTemplateType>(request.TemplateType, true, out var templateType))
                    query = query.Where(c => c.TemplateType == templateType);

                if (!string.IsNullOrEmpty(request.Status) &&
                    Enum.TryParse<EEContractStatus>(request.Status, true, out var status))
                    query = query.Where(c => c.Status == status);

                if (request.IsCreator == true)
                    query = query.Where(c => c.CreatedByUserId == userId);

                if (request.IsSignatory == true)
                    query = query.Where(c => _signatures.Any(s => s.ContractId == c.ContractId && s.UserId == userId));

                if (request.ActiveOnly == true)
                    query = query.Where(c => c.Status == EEContractStatus.Active);

                if (request.PendingMySignature == true)
                    query = query.Where(c => _signatures.Any(s =>
                        s.ContractId == c.ContractId &&
                        s.UserId == userId &&
                        s.Status == ESignatureStatus.Pending));

                if (request.CreatedFrom.HasValue)
                    query = query.Where(c => c.CreatedAt >= request.CreatedFrom.Value);

                if (request.CreatedTo.HasValue)
                    query = query.Where(c => c.CreatedAt <= request.CreatedTo.Value);

                // Sorting
                query = request.SortBy.ToLower() switch
                {
                    "updatedat" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(c => c.UpdatedAt)
                        : query.OrderByDescending(c => c.UpdatedAt),
                    "effectivedate" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(c => c.EffectiveDate)
                        : query.OrderByDescending(c => c.EffectiveDate),
                    "expirydate" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(c => c.ExpiryDate)
                        : query.OrderByDescending(c => c.ExpiryDate),
                    "title" => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(c => c.Title)
                        : query.OrderByDescending(c => c.Title),
                    _ => request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(c => c.CreatedAt)
                        : query.OrderByDescending(c => c.CreatedAt)
                };

                var totalItems = query.Count();

                // Pagination
                var contracts = query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Map to summaries
                var summaries = new List<ContractSummary>();
                foreach (var contract in contracts)
                {
                    summaries.Add(await MapToContractSummary(contract, userId));
                }

                // Calculate statistics
                var allUserContracts = _contracts.Where(c =>
                    userVehicleIds.Contains(c.VehicleId) ||
                    c.CreatedByUserId == userId ||
                    _signatures.Any(s => s.ContractId == c.ContractId && s.UserId == userId)).ToList();

                var statistics = CalculateStatistics(allUserContracts, userId);

                var response = new ContractListResponse
                {
                    Contracts = summaries,
                    Statistics = statistics,
                    Pagination = new ContractPaginationInfo
                    {
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize),
                        TotalItems = totalItems,
                        HasPreviousPage = request.PageNumber > 1,
                        HasNextPage = request.PageNumber < (int)Math.Ceiling(totalItems / (double)request.PageSize)
                    }
                };

                return new BaseResponse<ContractListResponse>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contracts");
                return new BaseResponse<ContractListResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Sign Contract

        public async Task<BaseResponse<ContractResponse>> SignContractAsync(
            int contractId,
            int userId,
            SignContractRequest request)
        {
            try
            {
                var contract = _contracts.FirstOrDefault(c => c.ContractId == contractId);
                if (contract == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_NOT_FOUND",
                        Data = null
                    };
                }

                // Find user's signature record
                var signature = _signatures.FirstOrDefault(s =>
                    s.ContractId == contractId && s.UserId == userId);

                if (signature == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_A_SIGNATORY",
                        Data = null
                    };
                }

                if (signature.Status == ESignatureStatus.Signed)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_SIGNED",
                        Data = null
                    };
                }

                if (signature.Status == ESignatureStatus.Declined)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_DECLINED",
                        Data = null
                    };
                }

                // Check if contract is still pending signatures
                if (contract.Status != EEContractStatus.PendingSignatures &&
                    contract.Status != EEContractStatus.PartiallySigned)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "CONTRACT_NOT_PENDING_SIGNATURES",
                        Data = null
                    };
                }

                // Check signature deadline
                if (contract.SignatureDeadline.HasValue &&
                    DateTime.UtcNow > contract.SignatureDeadline.Value)
                {
                    signature.Status = ESignatureStatus.Expired;
                    contract.Status = EEContractStatus.Expired;

                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "SIGNATURE_DEADLINE_EXPIRED",
                        Data = null
                    };
                }

                // Update signature
                signature.Status = ESignatureStatus.Signed;
                signature.SignedAt = DateTime.UtcNow;
                signature.Signature = request.Signature;
                signature.IpAddress = request.IpAddress;
                signature.DeviceInfo = request.DeviceInfo;
                signature.Geolocation = request.Geolocation;
                signature.SignerNotes = request.SignerNotes;

                contract.UpdatedAt = DateTime.UtcNow;

                // Check if all required signatures are collected
                var allSignatures = _signatures.Where(s => s.ContractId == contractId).ToList();
                var allSigned = allSignatures.All(s => s.Status == ESignatureStatus.Signed);

                if (allSigned)
                {
                    contract.Status = EEContractStatus.FullySigned;
                    contract.FullySignedAt = DateTime.UtcNow;

                    // Auto-activate if enabled
                    if (contract.AutoActivate)
                    {
                        contract.Status = EEContractStatus.Active;
                        contract.ActivatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    contract.Status = EEContractStatus.PartiallySigned;
                }

                var response = await MapToContractResponse(contract, userId);

                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 200,
                    Message = allSigned ? "CONTRACT_FULLY_SIGNED" : "SIGNATURE_RECORDED",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing contract");
                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Decline Contract

        public async Task<BaseResponse<ContractResponse>> DeclineContractAsync(
            int contractId,
            int userId,
            DeclineContractRequest request)
        {
            try
            {
                var contract = _contracts.FirstOrDefault(c => c.ContractId == contractId);
                if (contract == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_NOT_FOUND",
                        Data = null
                    };
                }

                var signature = _signatures.FirstOrDefault(s =>
                    s.ContractId == contractId && s.UserId == userId);

                if (signature == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_A_SIGNATORY",
                        Data = null
                    };
                }

                if (signature.Status == ESignatureStatus.Signed)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_SIGNED",
                        Data = null
                    };
                }

                // Update signature
                signature.Status = ESignatureStatus.Declined;
                signature.DeclinedAt = DateTime.UtcNow;
                signature.DeclineReason = request.Reason;
                signature.SuggestedChanges = request.SuggestedChanges;

                // Update contract status
                contract.Status = EEContractStatus.Rejected;
                contract.UpdatedAt = DateTime.UtcNow;

                var response = await MapToContractResponse(contract, userId);

                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_DECLINED",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining contract");
                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Terminate Contract

        public async Task<BaseResponse<ContractResponse>> TerminateContractAsync(
            int contractId,
            int userId,
            TerminateContractRequest request)
        {
            try
            {
                var contract = _contracts.FirstOrDefault(c => c.ContractId == contractId);
                if (contract == null)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 404,
                        Message = "CONTRACT_NOT_FOUND",
                        Data = null
                    };
                }

                // Verify user is creator or admin
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                var isCreator = contract.CreatedByUserId == userId;
                var isAdmin = user?.RoleEnum == EUserRole.Admin;

                if (!isCreator && !isAdmin)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 403,
                        Message = "NOT_AUTHORIZED",
                        Data = null
                    };
                }

                if (contract.Status == EEContractStatus.Terminated)
                {
                    return new BaseResponse<ContractResponse>
                    {
                        StatusCode = 400,
                        Message = "ALREADY_TERMINATED",
                        Data = null
                    };
                }

                // Update contract
                contract.Status = EEContractStatus.Terminated;
                contract.TerminatedAt = request.EffectiveDate ?? DateTime.UtcNow;
                contract.TerminationReason = request.Reason;
                contract.TerminatedByUserId = userId;
                contract.UpdatedAt = DateTime.UtcNow;

                var response = await MapToContractResponse(contract, userId);

                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 200,
                    Message = "CONTRACT_TERMINATED",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating contract");
                return new BaseResponse<ContractResponse>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Contract Templates

        public Task<BaseResponse<List<ContractTemplateResponse>>> GetContractTemplatesAsync()
        {
            var templates = GetAllTemplates();

            return Task.FromResult(new BaseResponse<List<ContractTemplateResponse>>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = templates
            });
        }

        public Task<BaseResponse<ContractTemplateResponse>> GetContractTemplateAsync(string templateType)
        {
            if (!Enum.TryParse<EContractTemplateType>(templateType, true, out var type))
            {
                return Task.FromResult(new BaseResponse<ContractTemplateResponse>
                {
                    StatusCode = 400,
                    Message = "INVALID_TEMPLATE_TYPE",
                    Data = null
                });
            }

            var template = GetTemplate(type);

            return Task.FromResult(new BaseResponse<ContractTemplateResponse>
            {
                StatusCode = 200,
                Message = "SUCCESS",
                Data = template
            });
        }

        #endregion

        #region Download PDF

        public async Task<BaseResponse<byte[]>> DownloadContractPdfAsync(int contractId, int userId)
        {
            try
            {
                var contractResponse = await GetContractByIdAsync(contractId, userId);
                if (contractResponse.StatusCode != 200)
                {
                    return new BaseResponse<byte[]>
                    {
                        StatusCode = contractResponse.StatusCode,
                        Message = contractResponse.Message,
                        Data = null
                    };
                }

                // Mock PDF generation (in production, use a PDF library)
                var pdfContent = $"CONTRACT PDF - {contractResponse.Data!.Title}";
                var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);

                return new BaseResponse<byte[]>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = bytes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading contract PDF");
                return new BaseResponse<byte[]>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Pending & Signed Contracts

        public async Task<BaseResponse<List<ContractSummary>>> GetPendingSignatureContractsAsync(int userId)
        {
            try
            {
                var pendingContracts = _contracts.Where(c =>
                    _signatures.Any(s =>
                        s.ContractId == c.ContractId &&
                        s.UserId == userId &&
                        s.Status == ESignatureStatus.Pending) &&
                    (c.Status == EEContractStatus.PendingSignatures ||
                     c.Status == EEContractStatus.PartiallySigned))
                    .OrderBy(c => c.SignatureDeadline)
                    .ToList();

                var summaries = new List<ContractSummary>();
                foreach (var contract in pendingContracts)
                {
                    summaries.Add(await MapToContractSummary(contract, userId));
                }

                return new BaseResponse<List<ContractSummary>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending contracts");
                return new BaseResponse<List<ContractSummary>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        public async Task<BaseResponse<List<ContractSummary>>> GetSignedContractsAsync(int userId, int? vehicleId = null)
        {
            try
            {
                var query = _contracts.Where(c =>
                    _signatures.Any(s =>
                        s.ContractId == c.ContractId &&
                        s.UserId == userId &&
                        s.Status == ESignatureStatus.Signed));

                if (vehicleId.HasValue)
                {
                    query = query.Where(c => c.VehicleId == vehicleId.Value);
                }

                var signedContracts = query
                    .OrderByDescending(c => c.FullySignedAt ?? c.UpdatedAt)
                    .ToList();

                var summaries = new List<ContractSummary>();
                foreach (var contract in signedContracts)
                {
                    summaries.Add(await MapToContractSummary(contract, userId));
                }

                return new BaseResponse<List<ContractSummary>>
                {
                    StatusCode = 200,
                    Message = "SUCCESS",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signed contracts");
                return new BaseResponse<List<ContractSummary>>
                {
                    StatusCode = 500,
                    Message = "INTERNAL_SERVER_ERROR",
                    Data = null
                };
            }
        }

        #endregion

        #region Helper Methods

        private async Task<ContractResponse> MapToContractResponse(ContractData contract, int userId)
        {
            // Get vehicle details
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(contract.VehicleId);

            // Get creator details
            var creator = await _unitOfWork.UserRepository.GetByIdAsync(contract.CreatedByUserId);

            // Get signatures
            var signatures = _signatures.Where(s => s.ContractId == contract.ContractId).ToList();
            var mappedSignatures = new List<ContractSignature>();

            foreach (var sig in signatures)
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(sig.UserId);
                mappedSignatures.Add(new ContractSignature
                {
                    SignatureId = sig.SignatureId,
                    UserId = sig.UserId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    Email = user?.Email ?? "",
                    Status = sig.Status.ToString(),
                    SignedAt = sig.SignedAt,
                    DeclinedAt = sig.DeclinedAt,
                    DeclineReason = sig.DeclineReason,
                    SignerNotes = sig.SignerNotes,
                    IpAddress = sig.IpAddress,
                    DeviceInfo = sig.DeviceInfo,
                    Geolocation = sig.Geolocation,
                    IsRequired = sig.IsRequired,
                    SignatureOrder = sig.SignatureOrder
                });
            }

            var mySignature = signatures.FirstOrDefault(s => s.UserId == userId);
            var signedCount = signatures.Count(s => s.Status == ESignatureStatus.Signed);
            var pendingCount = signatures.Count(s => s.Status == ESignatureStatus.Pending);
            var declinedCount = signatures.Count(s => s.Status == ESignatureStatus.Declined);

            string? terminatedByName = null;
            if (contract.TerminatedByUserId.HasValue)
            {
                var terminator = await _unitOfWork.UserRepository.GetByIdAsync(contract.TerminatedByUserId.Value);
                terminatedByName = terminator != null ? $"{terminator.FirstName} {terminator.LastName}" : null;
            }

            return new ContractResponse
            {
                ContractId = contract.ContractId,
                VehicleId = contract.VehicleId,
                VehicleName = $"{vehicle?.Brand} {vehicle?.Model}",
                VehicleLicensePlate = vehicle?.LicensePlate ?? "",
                TemplateType = contract.TemplateType.ToString(),
                Status = contract.Status.ToString(),
                Title = contract.Title,
                Description = contract.Description,
                ContractContent = contract.ContractContent,
                CustomTerms = contract.CustomTerms,
                CreatedByUserId = contract.CreatedByUserId,
                CreatedByUserName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
                CreatedByEmail = creator?.Email ?? "",
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,
                EffectiveDate = contract.EffectiveDate,
                ExpiryDate = contract.ExpiryDate,
                SignatureDeadline = contract.SignatureDeadline,
                FullySignedAt = contract.FullySignedAt,
                ActivatedAt = contract.ActivatedAt,
                TerminatedAt = contract.TerminatedAt,
                Signatures = mappedSignatures.OrderBy(s => s.SignatureOrder).ToList(),
                TotalSignatories = signatures.Count,
                SignedCount = signedCount,
                PendingCount = pendingCount,
                DeclinedCount = declinedCount,
                IsFullySigned = signedCount == signatures.Count,
                RequiresMySignature = mySignature != null,
                MySignatureStatus = mySignature?.Status.ToString(),
                AttachmentUrls = contract.AttachmentUrls,
                AutoActivate = contract.AutoActivate,
                TerminationReason = contract.TerminationReason,
                TerminatedByUserId = contract.TerminatedByUserId,
                TerminatedByUserName = terminatedByName
            };
        }

        private async Task<ContractSummary> MapToContractSummary(ContractData contract, int userId)
        {
            var vehicle = await _unitOfWork.VehicleRepository.GetByIdAsync(contract.VehicleId);
            var creator = await _unitOfWork.UserRepository.GetByIdAsync(contract.CreatedByUserId);
            var signatures = _signatures.Where(s => s.ContractId == contract.ContractId).ToList();
            var mySignature = signatures.FirstOrDefault(s => s.UserId == userId);
            var signedCount = signatures.Count(s => s.Status == ESignatureStatus.Signed);

            var daysUntilExpiry = contract.ExpiryDate.HasValue
                ? (int)(contract.ExpiryDate.Value - DateTime.UtcNow).TotalDays
                : int.MaxValue;

            return new ContractSummary
            {
                ContractId = contract.ContractId,
                VehicleId = contract.VehicleId,
                VehicleName = $"{vehicle?.Brand} {vehicle?.Model}",
                VehicleLicensePlate = vehicle?.LicensePlate ?? "",
                TemplateType = contract.TemplateType.ToString(),
                Status = contract.Status.ToString(),
                Title = contract.Title,
                CreatedByUserName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
                CreatedAt = contract.CreatedAt,
                EffectiveDate = contract.EffectiveDate,
                ExpiryDate = contract.ExpiryDate,
                TotalSignatories = signatures.Count,
                SignedCount = signedCount,
                IsFullySigned = signedCount == signatures.Count,
                RequiresMySignature = mySignature != null,
                MySignatureStatus = mySignature?.Status.ToString(),
                DaysUntilExpiry = daysUntilExpiry,
                IsExpiringSoon = daysUntilExpiry > 0 && daysUntilExpiry <= 30
            };
        }

        private ContractStatistics CalculateStatistics(List<ContractData> contracts, int userId)
        {
            var pendingMySignature = contracts.Count(c =>
                _signatures.Any(s => s.ContractId == c.ContractId &&
                                   s.UserId == userId &&
                                   s.Status == ESignatureStatus.Pending));

            var signedByMe = contracts.Count(c =>
                _signatures.Any(s => s.ContractId == c.ContractId &&
                                   s.UserId == userId &&
                                   s.Status == ESignatureStatus.Signed));

            var expiringWithin30Days = contracts.Count(c =>
                c.ExpiryDate.HasValue &&
                c.Status == EEContractStatus.Active &&
                (c.ExpiryDate.Value - DateTime.UtcNow).TotalDays <= 30 &&
                (c.ExpiryDate.Value - DateTime.UtcNow).TotalDays > 0);

            return new ContractStatistics
            {
                TotalContracts = contracts.Count,
                DraftContracts = contracts.Count(c => c.Status == EEContractStatus.Draft),
                PendingSignatures = contracts.Count(c => c.Status == EEContractStatus.PendingSignatures || c.Status == EEContractStatus.PartiallySigned),
                ActiveContracts = contracts.Count(c => c.Status == EEContractStatus.Active),
                ExpiredContracts = contracts.Count(c => c.Status == EEContractStatus.Expired),
                TerminatedContracts = contracts.Count(c => c.Status == EEContractStatus.Terminated),
                PendingMySignature = pendingMySignature,
                SignedByMe = signedByMe,
                CreatedByMe = contracts.Count(c => c.CreatedByUserId == userId),
                ExpiringWithin30Days = expiringWithin30Days
            };
        }

        private string GenerateContractContent(
            EContractTemplateType templateType,
            CreateContractRequest request,
            Vehicle vehicle)
        {
            var template = GetTemplate(templateType);
            var content = template.ContentTemplate;

            // Replace placeholders
            content = content.Replace("{{VEHICLE_BRAND}}", vehicle.Brand);
            content = content.Replace("{{VEHICLE_MODEL}}", vehicle.Model);
            content = content.Replace("{{LICENSE_PLATE}}", vehicle.LicensePlate);
            content = content.Replace("{{TITLE}}", request.Title);
            content = content.Replace("{{DESCRIPTION}}", request.Description ?? "");
            content = content.Replace("{{EFFECTIVE_DATE}}", request.EffectiveDate?.ToString("yyyy-MM-dd") ?? "Upon full signature");
            content = content.Replace("{{EXPIRY_DATE}}", request.ExpiryDate?.ToString("yyyy-MM-dd") ?? "No expiry");
            content = content.Replace("{{CREATED_DATE}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            return content;
        }

        private List<ContractTemplateResponse> GetAllTemplates()
        {
            var templates = new List<ContractTemplateResponse>();
            foreach (EContractTemplateType type in Enum.GetValues(typeof(EContractTemplateType)))
            {
                templates.Add(GetTemplate(type));
            }
            return templates;
        }

        private ContractTemplateResponse GetTemplate(EContractTemplateType type)
        {
            return type switch
            {
                EContractTemplateType.CoOwnershipAgreement => new ContractTemplateResponse
                {
                    TemplateType = type.ToString(),
                    Name = "Co-Ownership Agreement",
                    Description = "Agreement defining co-ownership terms, responsibilities, and rights for the electric vehicle",
                    ContentTemplate = GetCoOwnershipTemplate(),
                    RequiredFields = new List<string> { "VEHICLE_BRAND", "VEHICLE_MODEL", "LICENSE_PLATE", "OWNERSHIP_PERCENTAGE", "INVESTMENT_AMOUNT" },
                    OptionalFields = new List<string> { "CUSTOM_TERMS" },
                    RequiresAllCoOwnersSignature = true,
                    MinimumSignatories = 2,
                    Category = "Ownership"
                },
                EContractTemplateType.VehicleUsageAgreement => new ContractTemplateResponse
                {
                    TemplateType = type.ToString(),
                    Name = "Vehicle Usage Agreement",
                    Description = "Agreement outlining vehicle usage rules, booking procedures, and user responsibilities",
                    ContentTemplate = GetUsageAgreementTemplate(),
                    RequiredFields = new List<string> { "VEHICLE_BRAND", "VEHICLE_MODEL" },
                    OptionalFields = new List<string> { "USAGE_LIMITS", "BOOKING_RULES" },
                    RequiresAllCoOwnersSignature = true,
                    MinimumSignatories = 1,
                    Category = "Usage"
                },
                EContractTemplateType.CostSharingAgreement => new ContractTemplateResponse
                {
                    TemplateType = type.ToString(),
                    Name = "Cost Sharing Agreement",
                    Description = "Agreement defining how costs (maintenance, insurance, charging) are shared among co-owners",
                    ContentTemplate = GetCostSharingTemplate(),
                    RequiredFields = new List<string> { "VEHICLE_BRAND", "VEHICLE_MODEL", "COST_SPLIT_METHOD" },
                    OptionalFields = new List<string> { "PAYMENT_SCHEDULE" },
                    RequiresAllCoOwnersSignature = true,
                    MinimumSignatories = 2,
                    Category = "Financial"
                },
                _ => new ContractTemplateResponse
                {
                    TemplateType = type.ToString(),
                    Name = type.ToString().Replace("Agreement", " Agreement"),
                    Description = $"Template for {type}",
                    ContentTemplate = GetDefaultTemplate(),
                    RequiredFields = new List<string> { "VEHICLE_BRAND", "VEHICLE_MODEL" },
                    OptionalFields = new List<string>(),
                    RequiresAllCoOwnersSignature = false,
                    MinimumSignatories = 1,
                    Category = "General"
                }
            };
        }

        private string GetCoOwnershipTemplate()
        {
            return @"
CO-OWNERSHIP AGREEMENT

Vehicle: {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} ({{LICENSE_PLATE}})
Agreement Title: {{TITLE}}
Date: {{CREATED_DATE}}

This Co-Ownership Agreement (""Agreement"") is entered into by and between the undersigned parties (""Co-Owners"") for the joint ownership and operation of the above-mentioned electric vehicle.

1. OWNERSHIP STRUCTURE
   - Each Co-Owner holds a specified ownership percentage as recorded in the system
   - Ownership rights are proportional to investment amount and agreed ownership percentage
   - Transfer of ownership requires consent of all Co-Owners

2. RIGHTS AND RESPONSIBILITIES
   - All Co-Owners have equal access rights subject to fair booking procedures
   - Each Co-Owner is responsible for costs proportional to their ownership percentage
   - Decisions affecting the vehicle require majority or unanimous consent as specified

3. USAGE TERMS
   - Fair usage policy applies to all Co-Owners
   - Booking system must be respected
   - Vehicle must be returned in same condition as received

4. COST SHARING
   - Maintenance costs shared according to ownership percentage
   - Insurance and registration costs shared equally or by agreement
   - Charging costs tracked and settled monthly

5. DISPUTE RESOLUTION
   - Disputes resolved through internal mediation first
   - Escalation procedures available through platform
   - Legal action as last resort

6. TERMINATION
   - Any Co-Owner may exit by selling their share with consent
   - Buy-out provisions apply as per system rules

{{DESCRIPTION}}

Effective Date: {{EFFECTIVE_DATE}}
Expiry Date: {{EXPIRY_DATE}}
";
        }

        private string GetUsageAgreementTemplate()
        {
            return @"
VEHICLE USAGE AGREEMENT

Vehicle: {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} ({{LICENSE_PLATE}})
Title: {{TITLE}}
Date: {{CREATED_DATE}}

1. BOOKING PROCEDURES
   - All usage must be pre-booked through the platform
   - Minimum advance booking: 24 hours (unless emergency)
   - Maximum booking duration: As per vehicle policy

2. USAGE RESPONSIBILITIES
   - Check-in and check-out procedures must be followed
   - Vehicle condition documentation required
   - Report any issues immediately

3. PROHIBITED USES
   - No commercial use without agreement
   - No subleasing to third parties
   - No modifications without consent

{{DESCRIPTION}}

Effective: {{EFFECTIVE_DATE}}
";
        }

        private string GetCostSharingTemplate()
        {
            return @"
COST SHARING AGREEMENT

Vehicle: {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} ({{LICENSE_PLATE}})
Title: {{TITLE}}
Date: {{CREATED_DATE}}

1. COST CATEGORIES
   - Fixed costs: Insurance, registration
   - Variable costs: Charging, maintenance
   - Major repairs: Shared by ownership percentage

2. PAYMENT TERMS
   - Monthly settlements through platform
   - Transparent cost tracking
   - Dispute resolution for contested charges

{{DESCRIPTION}}

Effective: {{EFFECTIVE_DATE}}
";
        }

        private string GetDefaultTemplate()
        {
            return @"
E-CONTRACT AGREEMENT

Vehicle: {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} ({{LICENSE_PLATE}})
Title: {{TITLE}}
Date: {{CREATED_DATE}}

Description:
{{DESCRIPTION}}

Effective Date: {{EFFECTIVE_DATE}}
Expiry Date: {{EXPIRY_DATE}}
";
        }

        #endregion

        #region Internal Data Models

        private class ContractData
        {
            public int ContractId { get; set; }
            public int VehicleId { get; set; }
            public EContractTemplateType TemplateType { get; set; }
            public EEContractStatus Status { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ContractContent { get; set; } = string.Empty;
            public string? CustomTerms { get; set; }
            public int CreatedByUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public DateTime? EffectiveDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime? SignatureDeadline { get; set; }
            public DateTime? FullySignedAt { get; set; }
            public DateTime? ActivatedAt { get; set; }
            public DateTime? TerminatedAt { get; set; }
            public List<string> AttachmentUrls { get; set; } = new();
            public bool AutoActivate { get; set; }
            public string? TerminationReason { get; set; }
            public int? TerminatedByUserId { get; set; }
        }

        private class ContractSignatureData
        {
            public int SignatureId { get; set; }
            public int ContractId { get; set; }
            public int UserId { get; set; }
            public ESignatureStatus Status { get; set; }
            public DateTime? SignedAt { get; set; }
            public DateTime? DeclinedAt { get; set; }
            public string? Signature { get; set; }
            public string? DeclineReason { get; set; }
            public string? SuggestedChanges { get; set; }
            public string? SignerNotes { get; set; }
            public string? IpAddress { get; set; }
            public string? DeviceInfo { get; set; }
            public string? Geolocation { get; set; }
            public bool IsRequired { get; set; }
            public int SignatureOrder { get; set; }
        }

        #endregion
    }
}
