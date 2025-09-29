using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// Get API status
        /// </summary>
        /// <param name="optionalInteger">An optional integer parameter</param>
        /// <param name="optionalString">An optional string parameter</param>
        /// <param name="optionalBoolean">An optional boolean parameter</param>
        /// <response code="200">API is working</response>
        /// <remarks>
        ///     Sample remarks
        /// </remarks>
        [HttpGet("test-api")]
        public IActionResult Get(string? optionalString, int? optionalInteger, bool? optionalBoolean)
        {
            return Ok("API is working!");
        }

        [HttpGet("test-exception-middleware")]
        public IActionResult TestExceptionMiddleware()
        {
            throw new Exception("This is a test exception.");
        }
    }
}
