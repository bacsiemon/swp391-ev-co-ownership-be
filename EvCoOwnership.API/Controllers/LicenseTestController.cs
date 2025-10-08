using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Services.Mapping;
using EvCoOwnership.Repositories.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.API.Controllers
{
    /// <summary>
    /// Test controller for license verification functionality
    /// </summary>
    [Route("api/test/[controller]")]
    [ApiController]
    public class LicenseTestController : ControllerBase
    {
        /// <summary>
        /// Test license format validation
        /// </summary>
        /// <response code="200">Test results</response>
        [HttpGet("format-validation")]
        public IActionResult TestFormatValidation()
        {
            var testCases = new[]
            {
                "123456789",      // Valid 9 digits
                "A12345678",      // Valid letter + 8 digits
                "123456789012",   // Valid 12 digits
                "12345678",       // Invalid - too short
                "ABCD123456",     // Invalid - too many letters
                "123-456-789",    // Invalid - contains dashes
                ""                // Invalid - empty
            };

            var results = testCases.Select(licenseNumber => new
            {
                LicenseNumber = licenseNumber,
                IsValid = LicenseMapper.IsValidVietnameseLicenseFormat(licenseNumber),
                Length = licenseNumber?.Length ?? 0
            }).ToList();

            return Ok(new
            {
                StatusCode = 200,
                Message = "FORMAT_VALIDATION_TEST_COMPLETED",
                Data = results
            });
        }

        /// <summary>
        /// Test license request validation
        /// </summary>
        /// <response code="200">Validation test results</response>
        [HttpPost("request-validation")]
        public IActionResult TestRequestValidation([FromBody] VerifyLicenseRequest request)
        {
            var validator = new VerifyLicenseRequestValidator();
            var validationResult = validator.Validate(request);

            var result = new
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.Select(e => new
                {
                    Property = e.PropertyName,
                    ErrorMessage = e.ErrorMessage,
                    AttemptedValue = e.AttemptedValue
                }).ToList()
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = validationResult.IsValid ? "VALIDATION_PASSED" : "VALIDATION_FAILED",
                Data = result
            });
        }

        /// <summary>
        /// Test license mapping functionality
        /// </summary>
        /// <response code="200">Mapping test results</response>
        [HttpGet("mapping-test")]
        public IActionResult TestMapping()
        {
            // Create sample request
            var request = new VerifyLicenseRequest
            {
                LicenseNumber = "123456789",
                IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                IssuedBy = "HO CHI MINH",
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25))
            };

            // Test mapping to entity
            var entity = request.ToEntity(1, "https://example.com/license.jpg");

            // Test mapping back to details
            var details = entity.ToLicenseDetails();

            // Test summary mapping
            var summary = entity.ToSummary();

            // Test extension methods
            var daysUntilExpiry = entity.GetDaysUntilExpiry();
            var isExpired = entity.IsExpired();
            var isExpiringSoon = entity.IsExpiringSoon();

            return Ok(new
            {
                StatusCode = 200,
                Message = "MAPPING_TEST_COMPLETED",
                Data = new
                {
                    OriginalRequest = request,
                    MappedEntity = new
                    {
                        entity.Id,
                        entity.CoOwnerId,
                        entity.LicenseNumber,
                        entity.IssuedBy,
                        entity.IssueDate,
                        entity.ExpiryDate,
                        entity.LicenseImageUrl
                    },
                    LicenseDetails = details,
                    Summary = summary,
                    ExtensionMethods = new
                    {
                        DaysUntilExpiry = daysUntilExpiry,
                        IsExpired = isExpired,
                        IsExpiringSoon = isExpiringSoon
                    }
                }
            });
        }

        /// <summary>
        /// Test various license scenarios
        /// </summary>
        /// <response code="200">Scenario test results</response>
        [HttpGet("scenarios")]
        public IActionResult TestScenarios()
        {
            var scenarios = new List<object>();

            // Scenario 1: Valid active license
            scenarios.Add(new
            {
                Name = "Valid Active License",
                Request = new VerifyLicenseRequest
                {
                    LicenseNumber = "123456789",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                    IssuedBy = "HO CHI MINH",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25))
                },
                ExpectedResult = "VALID"
            });

            // Scenario 2: Expired license
            scenarios.Add(new
            {
                Name = "Expired License",
                Request = new VerifyLicenseRequest
                {
                    LicenseNumber = "987654321",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-15)),
                    IssuedBy = "HA NOI",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-35))
                },
                ExpectedResult = "EXPIRED"
            });

            // Scenario 3: Invalid format
            scenarios.Add(new
            {
                Name = "Invalid Format",
                Request = new VerifyLicenseRequest
                {
                    LicenseNumber = "INVALID123",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                    IssuedBy = "DA NANG",
                    FirstName = "Bob",
                    LastName = "Johnson",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30))
                },
                ExpectedResult = "INVALID_FORMAT"
            });

            // Scenario 4: Age requirement not met
            scenarios.Add(new
            {
                Name = "Age Requirement Not Met",
                Request = new VerifyLicenseRequest
                {
                    LicenseNumber = "555666777",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                    IssuedBy = "CAN THO",
                    FirstName = "Young",
                    LastName = "Person",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)) // Only 15 when license issued
                },
                ExpectedResult = "AGE_REQUIREMENT_NOT_MET"
            });

            // Process each scenario
            var results = scenarios.Select((scenario, index) =>
            {
                dynamic s = scenario;
                var request = (VerifyLicenseRequest)s.Request;
                var validator = new VerifyLicenseRequestValidator();
                var validationResult = validator.Validate(request);

                return new
                {
                    ScenarioNumber = index + 1,
                    Name = (string)s.Name,
                    Request = request,
                    ExpectedResult = (string)s.ExpectedResult,
                    ValidationPassed = validationResult.IsValid,
                    ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                    FormatValid = LicenseMapper.IsValidVietnameseLicenseFormat(request.LicenseNumber)
                };
            }).ToList();

            return Ok(new
            {
                StatusCode = 200,
                Message = "SCENARIO_TESTS_COMPLETED",
                Data = new
                {
                    TotalScenarios = results.Count,
                    Results = results
                }
            });
        }

        /// <summary>
        /// Test age calculation functionality
        /// </summary>
        /// <response code="200">Age calculation test results</response>
        [HttpGet("age-calculation")]
        public IActionResult TestAgeCalculation()
        {
            var testCases = new[]
            {
                new
                {
                    Description = "Valid - 18 years old when license issued",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-7)),
                    ExpectedValid = true
                },
                new
                {
                    Description = "Invalid - Only 15 years old when license issued",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                    ExpectedValid = false
                },
                new
                {
                    Description = "Edge case - Exactly 18 years old when license issued",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                    ExpectedValid = true
                },
                new
                {
                    Description = "Future issue date (invalid)",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1)),
                    ExpectedValid = false
                }
            };

            var results = testCases.Select(tc =>
            {
                var ageAtIssue = tc.IssueDate.Year - tc.DateOfBirth.Year;
                if (tc.DateOfBirth > tc.IssueDate.AddYears(-ageAtIssue))
                    ageAtIssue--;

                var isValidAge = ageAtIssue >= 16;
                var isValidDate = tc.IssueDate <= DateOnly.FromDateTime(DateTime.Now);
                var isOverallValid = isValidAge && isValidDate;

                return new
                {
                    tc.Description,
                    tc.DateOfBirth,
                    tc.IssueDate,
                    CalculatedAgeAtIssue = ageAtIssue,
                    IsValidAge = isValidAge,
                    IsValidDate = isValidDate,
                    IsOverallValid = isOverallValid,
                    tc.ExpectedValid,
                    TestPassed = isOverallValid == tc.ExpectedValid
                };
            }).ToList();

            return Ok(new
            {
                StatusCode = 200,
                Message = "AGE_CALCULATION_TESTS_COMPLETED",
                Data = new
                {
                    TotalTests = results.Count,
                    PassedTests = results.Count(r => r.TestPassed),
                    Results = results
                }
            });
        }

        /// <summary>
        /// Generate sample test data for development
        /// </summary>
        /// <response code="200">Sample test data</response>
        [HttpGet("sample-data")]
        public IActionResult GenerateSampleData()
        {
            var sampleRequests = new[]
            {
                new VerifyLicenseRequest
                {
                    LicenseNumber = "123456789",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                    IssuedBy = "HO CHI MINH",
                    FirstName = "Nguyen",
                    LastName = "Van A",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-28))
                },
                new VerifyLicenseRequest
                {
                    LicenseNumber = "B87654321",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                    IssuedBy = "HO CHI MINH",
                    FirstName = "Tran",
                    LastName = "Thi B",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-35))
                },
                new VerifyLicenseRequest
                {
                    LicenseNumber = "A12345678",
                    IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                    IssuedBy = "HA NOI",
                    FirstName = "Le",
                    LastName = "Van C",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-22))
                }
            };

            return Ok(new
            {
                StatusCode = 200,
                Message = "SAMPLE_DATA_GENERATED",
                Data = new
                {
                    SampleRequests = sampleRequests,
                    TestInstructions = new
                    {
                        ValidLicenseFormats = new[]
                        {
                            "123456789 (9 digits)",
                            "A12345678 (letter + 8 digits)",
                            "123456789012 (12 digits)"
                        },
                        ValidAuthorities = new[]
                        {
                            "HO CHI MINH",
                            "HA NOI",
                            "DA NANG",
                            "CAN THO"
                        },
                        AgeRequirement = "Must be at least 18 years old when license was issued",
                        DateRequirements = "Issue date must be in the past and not older than 50 years"
                    }
                }
            });
        }
    }
}