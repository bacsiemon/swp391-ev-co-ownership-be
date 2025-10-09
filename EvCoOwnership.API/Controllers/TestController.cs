using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.DTOs.TestDTOs;
using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.DTOs.FileUploadDTOs;
using EvCoOwnership.API.Attributes;
using EvCoOwnership.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public TestController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

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

        [HttpGet("test-base-response")]
        public IActionResult TestBaseResponse()
        {
            var response = new BaseResponse
            {
                StatusCode = 200,
                Message = "Operation completed successfully.",
                Data = new { Id = 1, Name = "Test Item" }
            };

            return response.StatusCode switch {
                200 => Ok(response),
                400 => BadRequest(response),
                _ => Ok(response)
            };
        }

        /// <summary>
        /// Test FluentValidation auto-validation functionality
        /// </summary>
        /// <param name="request">Test validation request</param>
        /// <response code="200">Validation passed</response>
        /// <response code="400">Validation failed</response>
        /// <remarks>
        /// This endpoint demonstrates automatic FluentValidation.
        /// Try sending invalid data to see validation errors:
        /// - Empty name
        /// - Invalid email format
        /// - Negative age
        /// - OptionalField longer than 100 characters
        /// </remarks>
        [HttpPost("test-fluentvalidation")]
        public IActionResult TestFluentValidation([FromBody] TestValidationRequest request)
        {
            var response = new BaseResponse
            {
                StatusCode = 200,
                Message = "Validation passed successfully!",
                Data = new 
                { 
                    ReceivedData = request,
                    ValidationMessage = "All validation rules passed automatically using FluentValidation"
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Test existing auth DTO validation (ForgotPasswordRequest)
        /// </summary>
        /// <param name="request">Forgot password request</param>
        /// <response code="200">Validation passed</response>
        /// <response code="400">Validation failed</response>
        /// <remarks>
        /// This endpoint tests the existing ForgotPasswordRequest DTO validation.
        /// Try sending an invalid email to see validation errors.
        /// </remarks>
        [HttpPost("test-auth-validation")]
        public IActionResult TestAuthValidation([FromBody] ForgotPasswordRequest request)
        {
            var response = new BaseResponse
            {
                StatusCode = 200,
                Message = "Auth DTO validation passed!",
                Data = new 
                { 
                    Email = request.Email,
                    ValidationMessage = "ForgotPasswordRequest validation passed using FluentValidation"
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Test endpoint with validation disabled
        /// </summary>
        /// <param name="request">Test validation request (validation will be skipped)</param>
        /// <response code="200">Always succeeds (validation skipped)</response>
        /// <remarks>
        /// This endpoint demonstrates how to skip validation for specific actions.
        /// Even invalid data will be accepted here.
        /// </remarks>
        [HttpPost("test-skip-validation")]
        [SkipValidation]
        public IActionResult TestSkipValidation([FromBody] TestValidationRequest request)
        {
            var response = new BaseResponse
            {
                StatusCode = 200,
                Message = "Validation was skipped for this endpoint",
                Data = new 
                { 
                    ReceivedData = request,
                    ValidationMessage = "This endpoint uses [SkipValidation] attribute"
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Test file upload functionality
        /// </summary>
        /// <param name="file">File to upload for testing</param>
        /// <response code="201">File upload test successful</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Upload failed</response>
        /// <remarks>
        /// This endpoint tests the file upload functionality.
        /// Upload any supported file type to test the complete upload flow.
        /// 
        /// Supported file types:
        /// - Images: JPEG, JPG, PNG, GIF, WEBP
        /// - Documents: PDF, DOC, DOCX, XLS, XLSX, TXT
        /// 
        /// Maximum file size: 10MB
        /// </remarks>
        [HttpPost("test-file-upload")]
        public async Task<IActionResult> TestFileUpload(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new BaseResponse
                {
                    StatusCode = 400,
                    Message = "FILE_REQUIRED",
                    Data = "Please select a file to upload for testing"
                });
            }

            var request = new FileUploadRequest { File = file };
            
            var response = await _fileUploadService.UploadFileAsync(request);
            
            if (response.StatusCode == 201)
            {
                response.Message = "FILE_UPLOAD_TEST_SUCCESS";
                response.Data = new
                {
                    UploadResult = response.Data,
                    TestMessage = "File upload test completed successfully! You can now download the file using the provided URL."
                };
            }
            
            return response.StatusCode switch
            {
                201 => StatusCode(201, response),
                400 => BadRequest(response),
                500 => StatusCode(500, response),
                _ => StatusCode(response.StatusCode, response)
            };
        }
    }
}
