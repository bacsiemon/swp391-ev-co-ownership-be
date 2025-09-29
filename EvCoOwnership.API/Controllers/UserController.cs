using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EvCoOwnership.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting users with pageIndex: {PageIndex}, pageSize: {PageSize}", pageIndex, pageSize);
                
                var users = await _userService.GetUsersAsync(pageIndex, pageSize);
                
                _logger.LogInformation("Successfully retrieved {UserCount} users", users.Items?.Count() ?? 0);
                
                return Ok(new BaseResponse()
                {
                    StatusCode = "OK",
                    Message = "Success",
                    Data = users.Items,
                    AdditionalData = users.AdditionalData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users with pageIndex: {PageIndex}, pageSize: {PageSize}", pageIndex, pageSize);
                
                return StatusCode(500, new BaseResponse()
                {
                    StatusCode = "Error",
                    Message = "An error occurred while retrieving users",
                    Data = null
                });
            }
        }
    }
}
