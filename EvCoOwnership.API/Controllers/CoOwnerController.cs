using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for managing Co-owner eligibility in the EV Co-ownership system
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoOwnerController : ControllerBase
    {
        private readonly ICoOwnerEligibilityService _coOwnerEligibilityService;

        /// <summary>
        /// Initializes a new instance of the CoOwnerController
        /// </summary>
        /// <param name="coOwnerEligibilityService">Co-owner eligibility service</param>
        public CoOwnerController(ICoOwnerEligibilityService coOwnerEligibilityService)
        {
            _coOwnerEligibilityService = coOwnerEligibilityService;
        }

        /// <summary>
        /// Checks eligibility for Co-owner status
        /// </summary>
        /// <param name="userId">User ID to check (optional, defaults to current user)</param>
        /// <response code="200">Eligibility check completed. Possible messages:  
        /// - ELIGIBLE_FOR_CO_OWNERSHIP  
        /// - NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - can only check own eligibility unless admin</response>
        /// <response code="404">User not found. Possible messages:  
        /// - USER_NOT_FOUND  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Checks if a user meets all requirements to become a Co-owner:  
        /// - Active account status  
        /// - Age 18 or older  
        /// - Valid driving license registered  
        /// 
        /// Users can only check their own eligibility unless they are admin/staff.
        /// </remarks>
        [HttpGet("eligibility")]
        public async Task<IActionResult> CheckEligibility([FromQuery] int? userId = null)
        {
            // Get current user ID from token
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            // Determine target user ID
            var targetUserId = userId ?? currentUserId;

            // Check if user has permission to check other users' eligibility
            if (targetUserId != currentUserId)
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if (!userRoles.Contains("Admin") && !userRoles.Contains("Staff"))
                {
                    return Forbid("ACCESS_DENIED");
                }
            }

            var response = await _coOwnerEligibilityService.CheckCoOwnerEligibilityAsync(targetUserId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Promotes current user to Co-owner status
        /// </summary>
        /// <response code="200">Promotion successful. Possible messages:  
        /// - PROMOTION_TO_CO_OWNER_SUCCESS  
        /// </response>
        /// <response code="400">Promotion failed - requirements not met. Possible messages:  
        /// - USER_NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">User already has Co-owner status. Possible messages:  
        /// - USER_ALREADY_CO_OWNER  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// - CO_OWNER_ROLE_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Promotes the current authenticated user to Co-owner status after verifying eligibility.  
        /// User must meet all requirements: active account, age 18+, and valid driving license.
        /// </remarks>
        [HttpPost("promote")]
        public async Task<IActionResult> PromoteToCoOwner()
        {
            // Get current user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "INVALID_TOKEN" });
            }

            var response = await _coOwnerEligibilityService.PromoteToCoOwnerAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Promotes a specific user to Co-owner status (Admin only)
        /// </summary>
        /// <param name="userId">User ID to promote</param>
        /// <response code="200">Promotion successful. Possible messages:  
        /// - PROMOTION_TO_CO_OWNER_SUCCESS  
        /// </response>
        /// <response code="400">Promotion failed - requirements not met. Possible messages:  
        /// - USER_NOT_ELIGIBLE_FOR_CO_OWNERSHIP  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="409">User already has Co-owner status. Possible messages:  
        /// - USER_ALREADY_CO_OWNER  
        /// </response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// - CO_OWNER_ROLE_NOT_FOUND  
        /// </response>
        /// <remarks>
        /// Allows administrators to promote any eligible user to Co-owner status.  
        /// Target user must meet all eligibility requirements.
        /// </remarks>
        [HttpPost("promote/{userId:int}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> PromoteUserToCoOwner(int userId)
        {
            var response = await _coOwnerEligibilityService.PromoteToCoOwnerAsync(userId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets Co-ownership system statistics (Admin only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully. Possible messages:  
        /// - CO_OWNERSHIP_STATS_RETRIEVED  
        /// </response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="500">Internal server error. Possible messages:  
        /// - INTERNAL_SERVER_ERROR  
        /// </response>
        /// <remarks>
        /// Provides comprehensive statistics about the Co-ownership system including:  
        /// - Total users and Co-owners  
        /// - Co-ownership adoption rate  
        /// - License registration statistics  
        /// - Expired and expiring licenses  
        /// </remarks>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetCoOwnershipStats()
        {
            var response = await _coOwnerEligibilityService.GetCoOwnershipStatsAsync();
            return response.StatusCode switch
            {
                200 => Ok(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        #region Development/Testing Endpoints

        /// <summary>
        /// Test eligibility checking with various scenarios (Development only)
        /// </summary>
        /// <response code="200">Test scenarios completed</response>
        /// <remarks>
        /// Provides mock eligibility scenarios for testing purposes.
        /// </remarks>
        [HttpGet("test/eligibility-scenarios")]
        public IActionResult TestEligibilityScenarios()
        {
            var scenarios = new
            {
                EligibleUser = new
                {
                    Description = "User with all requirements met",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "25 years old",
                        DrivingLicense = "Valid, not expired",
                        Result = "ELIGIBLE"
                    }
                },
                TooYoung = new
                {
                    Description = "User under 18 years old",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "17 years old",
                        DrivingLicense = "Valid",
                        Result = "NOT_ELIGIBLE - MINIMUM_AGE_NOT_MET"
                    }
                },
                NoLicense = new
                {
                    Description = "User without driving license",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "25 years old",
                        DrivingLicense = "Not registered",
                        Result = "NOT_ELIGIBLE - NO_DRIVING_LICENSE_REGISTERED"
                    }
                },
                ExpiredLicense = new
                {
                    Description = "User with expired license",
                    Requirements = new
                    {
                        AccountStatus = "Active",
                        Age = "30 years old",
                        DrivingLicense = "Expired",
                        Result = "NOT_ELIGIBLE - NO_VALID_DRIVING_LICENSE"
                    }
                },
                InactiveAccount = new
                {
                    Description = "User with inactive account",
                    Requirements = new
                    {
                        AccountStatus = "Inactive/Suspended",
                        Age = "25 years old",
                        DrivingLicense = "Valid",
                        Result = "NOT_ELIGIBLE - ACCOUNT_NOT_ACTIVE"
                    }
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "ELIGIBILITY_SCENARIOS_GENERATED",
                Data = scenarios
            });
        }

        /// <summary>
        /// Test promotion workflow (Development only)
        /// </summary>
        /// <response code="200">Test workflow completed</response>
        /// <remarks>
        /// Demonstrates the promotion workflow steps and requirements.
        /// </remarks>
        [HttpGet("test/promotion-workflow")]
        public IActionResult TestPromotionWorkflow()
        {
            var workflow = new
            {
                Steps = new[]
                {
                    new
                    {
                        Step = 1,
                        Action = "Register Account",
                        Description = "User creates account with basic information"
                    },
                    new
                    {
                        Step = 2,
                        Action = "Verify Identity",
                        Description = "Upload and verify ID/Passport documents"
                    },
                    new
                    {
                        Step = 3,
                        Action = "Register Driving License",
                        Description = "Submit driving license for verification"
                    },
                    new
                    {
                        Step = 4,
                        Action = "Check Eligibility",
                        Description = "System verifies all requirements are met"
                    },
                    new
                    {
                        Step = 5,
                        Action = "Promote to Co-owner",
                        Description = "User or Admin triggers promotion process"
                    },
                    new
                    {
                        Step = 6,
                        Action = "Access Co-ownership Features",
                        Description = "User can now create/join groups and book vehicles"
                    }
                },
                Requirements = new
                {
                    MinimumAge = "18 years old",
                    AccountStatus = "Active",
                    DrivingLicense = "Valid and not expired",
                    IdentityVerification = "Completed"
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "PROMOTION_WORKFLOW_GENERATED",
                Data = workflow
            });
        }

        #endregion
    }
}