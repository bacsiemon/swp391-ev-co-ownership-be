using EvCoOwnership.API.Attributes;
using EvCoOwnership.Repositories.DTOs.FileUploadDTOs;
using EvCoOwnership.Repositories.Enums;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>Admin, Staff, CoOwner</summary>
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles] // Requires authentication for all endpoints
    public class FileUploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        /// <summary>
        /// Initializes a new instance of the FileUploadController
        /// </summary>
        /// <param name="fileUploadService">File upload service</param>
        public FileUploadController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        /// <summary>User</summary>
        /// <remarks>
        /// Upload a new file to the system.
        /// 
        /// **Parameters:**  
        /// - **file:** Required, file to upload. Maximum size: 10MB.
        /// 
        /// **Supported file types:**  
        /// - Images: JPEG, JPG, PNG, GIF, WEBP  
        /// - Documents: PDF, DOC, DOCX, XLS, XLSX, TXT  
        /// 
        /// **Sample request:**  
        /// ```
        /// POST /api/fileupload/upload
        /// Content-Type: multipart/form-data
        /// 
        /// file: [binary file data]
        /// ```
        /// </remarks>
        /// <param name="file">File to upload</param>
        /// <response code="201">File upload successful. Possible messages:  
        /// - FILE_UPLOAD_SUCCESS  
        /// </response>
        /// <response code="400">Validation error. Possible messages:  
        /// - FILE_REQUIRED  
        /// - INVALID_FILE_TYPE  
        /// - FILE_SIZE_EXCEEDS_LIMIT  
        /// </response>
        /// <response code="500">Server error. Possible messages:  
        /// - FILE_UPLOAD_FAILED  
        /// </response>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { StatusCode = 400, Message = "FILE_REQUIRED" });
            }
            var request = new FileUploadRequest { File = file };
            
            var response = await _fileUploadService.UploadFileAsync(request);
            
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>User</summary>
        /// <remarks>
        /// Download a file by its ID. Returns the file content with appropriate headers.
        /// </remarks>
        /// <param name="id">File ID</param>
        /// <response code="200">File download successful</response>
        /// <response code="404">File not found. Possible messages:  
        /// - FILE_NOT_FOUND  
        /// </response>
        /// <response code="500">Server error. Possible messages:  
        /// - FILE_RETRIEVAL_FAILED  
        /// </response>
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var response = await _fileUploadService.GetFileAsync(id);
            
            if (response.StatusCode == 200 && response.Data is FileDownloadResponse fileData)
            {
                return File(fileData.Data, fileData.MimeType, fileData.FileName);
            }
            
            return response.StatusCode switch
            {
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var response = await _fileUploadService.GetFileAsync(id);

            if (response.StatusCode == 200 && response.Data is FileDownloadResponse fileData)
            {
                return File(fileData.Data, fileData.MimeType);
            }

            return response.StatusCode switch
            {
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>User</summary>
        /// <remarks>
        /// Get file information without downloading the file content.  
        /// Returns file metadata including size, type, upload date, and download URL.
        /// </remarks>
        /// <param name="id">File ID</param>
        /// <response code="200">File info retrieved successfully. Possible messages:  
        /// - FILE_INFO_RETRIEVED_SUCCESS  
        /// </response>
        /// <response code="404">File not found. Possible messages:  
        /// - FILE_NOT_FOUND  
        /// </response>
        /// <response code="500">Server error. Possible messages:  
        /// - FILE_INFO_RETRIEVAL_FAILED  
        /// </response>
        [HttpGet("{id:int}/info")]
        public async Task<IActionResult> GetFileInfo(int id)
        {
            var response = await _fileUploadService.GetFileInfoAsync(id);

            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }

        /// <summary>Admin only - Delete files</summary>
        /// <remarks>
        /// Delete a file from the system. This action is irreversible.  
        /// Only administrators can delete files.
        /// </remarks>
        /// <param name="id">File ID</param>
        /// <response code="200">File deletion successful. Possible messages:  
        /// - FILE_DELETE_SUCCESS  
        /// </response>
        /// <response code="404">File not found. Possible messages:  
        /// - FILE_NOT_FOUND  
        /// </response>
        /// <response code="500">Server error. Possible messages:  
        /// - FILE_DELETE_FAILED  
        /// </response>
        [HttpDelete("{id:int}")]
        [AuthorizeRoles(EUserRole.Admin)]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var response = await _fileUploadService.DeleteFileAsync(id);
            
            return response.StatusCode switch
            {
                200 => Ok(response),
                404 => NotFound(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}