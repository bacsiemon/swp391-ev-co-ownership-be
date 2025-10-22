using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.MaintenanceDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Controller for vehicle maintenance management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        /// <summary>
        /// Initializes a new instance of the MaintenanceController
        /// </summary>
        /// <param name="maintenanceService">Maintenance service</param>
        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        /// <summary>
        /// Creates a new maintenance record
        /// </summary>
        /// <param name="request">Create maintenance request</param>
        /// <response code="201">Maintenance created successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Vehicle or booking not found</response>
        [HttpPost]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff, EUserRole.CoOwner)]
        public async Task<IActionResult> CreateMaintenance([FromBody] CreateMaintenanceRequest request)
        {
            var response = await _maintenanceService.CreateMaintenanceAsync(request);
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets maintenance record by ID
        /// </summary>
        /// <param name="id">Maintenance ID</param>
        /// <response code="200">Maintenance retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Maintenance not found</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMaintenance(int id)
        {
            var response = await _maintenanceService.GetMaintenanceByIdAsync(id);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets maintenance records for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Maintenances retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        [HttpGet("vehicle/{vehicleId:int}")]
        public async Task<IActionResult> GetVehicleMaintenances(int vehicleId, int pageIndex = 1, int pageSize = 10)
        {
            var response = await _maintenanceService.GetVehicleMaintenancesAsync(vehicleId, pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets complete maintenance history for a vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Maintenance history retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("vehicle/{vehicleId:int}/history")]
        public async Task<IActionResult> GetVehicleMaintenanceHistory(int vehicleId)
        {
            var response = await _maintenanceService.GetVehicleMaintenanceHistoryAsync(vehicleId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets all maintenance records (Admin/Staff only)
        /// </summary>
        /// <param name="pageIndex">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <response code="200">Maintenances retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetAllMaintenances(int pageIndex = 1, int pageSize = 10)
        {
            var response = await _maintenanceService.GetAllMaintenancesAsync(pageIndex, pageSize);
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Updates a maintenance record
        /// </summary>
        /// <param name="id">Maintenance ID</param>
        /// <param name="request">Update maintenance request</param>
        /// <response code="200">Maintenance updated successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        /// <response code="404">Maintenance not found</response>
        [HttpPut("{id:int}")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> UpdateMaintenance(int id, [FromBody] UpdateMaintenanceRequest request)
        {
            var response = await _maintenanceService.UpdateMaintenanceAsync(id, request);
            return response.StatusCode switch
            {
                200 => Ok(response),
                400 => BadRequest(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Marks a maintenance as paid
        /// </summary>
        /// <param name="id">Maintenance ID</param>
        /// <response code="200">Maintenance marked as paid successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        /// <response code="404">Maintenance not found</response>
        [HttpPost("{id:int}/mark-paid")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var response = await _maintenanceService.MarkMaintenanceAsPaidAsync(id);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Deletes a maintenance record (Admin only)
        /// </summary>
        /// <param name="id">Maintenance ID</param>
        /// <response code="200">Maintenance deleted successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin role required</response>
        /// <response code="404">Maintenance not found</response>
        [HttpDelete("{id:int}")]
        [AuthorizeRoles(EUserRole.Admin)]
        public async Task<IActionResult> DeleteMaintenance(int id)
        {
            var response = await _maintenanceService.DeleteMaintenanceAsync(id);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>
        /// Gets overall maintenance statistics (Admin/Staff only)
        /// </summary>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="403">Access denied - admin/staff role required</response>
        [HttpGet("statistics")]
        [AuthorizeRoles(EUserRole.Admin, EUserRole.Staff)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _maintenanceService.GetMaintenanceStatisticsAsync();
            return response.StatusCode == 200 ? Ok(response) : StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Gets maintenance statistics for a specific vehicle
        /// </summary>
        /// <param name="vehicleId">Vehicle ID</param>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet("vehicle/{vehicleId:int}/statistics")]
        public async Task<IActionResult> GetVehicleStatistics(int vehicleId)
        {
            var response = await _maintenanceService.GetVehicleMaintenanceStatisticsAsync(vehicleId);
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
