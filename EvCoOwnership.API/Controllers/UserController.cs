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

        /// <summary>
        /// Gets paginated list of users
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <response code="200">Users retrieved successfully</response>
        [HttpGet]
        public async Task<IActionResult> GetUsers(int pageIndex = 1, int pageSize = 10)
        {
            return Ok(await _userService.GetPagingAsync(pageIndex, pageSize));
        }
    }
}
