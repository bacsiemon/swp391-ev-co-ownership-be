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
        /// <param name="number">An optional integer parameter</param>
        /// <param name="randomCharacters">An optional string parameter</param>
        /// <param name="trueOrFalse">An optional boolean parameter</param>
        /// <response code="200">API is working</response>
        /// <remarks>
        ///     Sample remarks
        /// </remarks>
        [HttpGet]
        public IActionResult Get(string? randomCharacters, int? number, bool? trueOrFalse)
        {
            return Ok("API is working!");
        }
    }
}
