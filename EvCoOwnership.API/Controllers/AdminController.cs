using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCoOwnership.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [AuthorizeRoles(EUserRole.Admin)]
    public class AdminController : ControllerBase
    {
        // --- Quản lý người dùng ---
        [HttpGet("users")]
        public IActionResult GetAllUsers() { return Ok(); }

        [HttpPost("user")]
        public IActionResult CreateUser() { return Ok(); }

        [HttpPatch("user/{id}")]
        public IActionResult UpdateUser(int id) { return Ok(); }

        [HttpDelete("user/{id}")]
        public IActionResult DeleteUser(int id) { return Ok(); }

        // --- Quản lý license ---
        [HttpGet("licenses")]
        public IActionResult GetLicenses() { return Ok(); }

        [HttpPatch("license/{id}/approve")]
        public IActionResult ApproveLicense(int id) { return Ok(); }

        [HttpPatch("license/{id}/reject")]
        public IActionResult RejectLicense(int id) { return Ok(); }

        // --- Quản lý nhóm & hệ thống ---
        [HttpGet("groups")]
        public IActionResult GetGroups() { return Ok(); }

        [HttpPatch("group/{id}/status")]
        public IActionResult UpdateGroupStatus(int id) { return Ok(); }

        // --- Cấu hình hệ thống ---
        [HttpGet("settings")]
        public IActionResult GetSettings() { return Ok(); }

        [HttpPatch("settings")]
        public IActionResult UpdateSettings() { return Ok(); }

        // --- Báo cáo & giám sát ---
        [HttpGet("reports")]
        public IActionResult GetReports() { return Ok(); }

        [HttpGet("audit-logs")]
        public IActionResult GetAuditLogs() { return Ok(); }
    }
}
