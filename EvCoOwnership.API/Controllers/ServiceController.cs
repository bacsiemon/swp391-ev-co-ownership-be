using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// API controller for service management (list, start, complete)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        /// <summary>
        /// List all services
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] object query) => Ok();

        /// <summary>
        /// Start a service
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(int id) => Ok();

        /// <summary>
        /// Complete a service
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(int id) => Ok();
    }
}