using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    [Route("api/staff")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Staff)]
    public class StaffController : ControllerBase
    {
        // --- Quản lý nhóm xe đồng sở hữu ---
        [HttpGet("groups")]
        public IActionResult GetGroups() { return Ok(); }

        [HttpGet("group/{id}")]
        public IActionResult GetGroup(int id) { return Ok(); }

        // --- Quản lý hợp đồng pháp lý ---
        [HttpGet("contracts")]
        public IActionResult GetContracts() { return Ok(); }

        [HttpPatch("contract/{id}/status")]
        public IActionResult UpdateContractStatus(int id) { return Ok(); }

        // --- Check-in/Check-out ---
        [HttpPost("checkin")]
        public IActionResult CheckIn() { return Ok(); }

        [HttpPost("checkout")]
        public IActionResult CheckOut() { return Ok(); }

        // --- Dịch vụ xe ---
        [HttpGet("services")]
        public IActionResult GetServices() { return Ok(); }

        [HttpPost("service")]
        public IActionResult CreateService() { return Ok(); }

        [HttpPatch("service/{id}/status")]
        public IActionResult UpdateServiceStatus(int id) { return Ok(); }

        // --- Tranh chấp & báo cáo ---
        [HttpGet("disputes")]
        public IActionResult GetDisputes() { return Ok(); }

        [HttpPatch("dispute/{id}/status")]
        public IActionResult UpdateDisputeStatus(int id) { return Ok(); }

        [HttpGet("reports")]
        public IActionResult GetReports() { return Ok(); }
    }
}
