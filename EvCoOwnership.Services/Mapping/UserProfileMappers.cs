using EvCoOwnership.DTOs.UserDTOs;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Mapping
{
    /// <summary>
    /// Mappers for User Profile operations
    /// </summary>
    public static class UserProfileMappers
    {
        /// <summary>
        /// Maps User entity to UserProfileResponse
        /// </summary>
        public static UserProfileResponse ToUserProfileResponse(User user)
        {
            return new UserProfileResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                ProfileImageUrl = user.ProfileImageUrl,
                Role = user.RoleEnum?.ToString() ?? "CoOwner",
                Status = user.StatusEnum?.ToString() ?? "Active",
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Stats = new UserProfileStats
                {
                    TotalVehiclesOwned = 0, // Will be calculated separately
                    TotalVehiclesCoOwned = 0, // Will be calculated separately
                    TotalBookings = 0, // Will be calculated separately
                    PendingInvitations = 0, // Will be calculated separately
                    HasValidDrivingLicense = false, // Will be calculated separately
                    TotalPayments = 0, // Will be calculated separately
                    TotalInvestmentAmount = 0 // Will be calculated separately
                }
            };
        }

        /// <summary>
        /// Calculates profile completeness percentage
        /// </summary>
        public static decimal CalculateCompletenessPercentage(User user)
        {
            var fields = new List<bool>
            {
                !string.IsNullOrEmpty(user.FirstName),
                !string.IsNullOrEmpty(user.LastName),
                !string.IsNullOrEmpty(user.Phone),
                user.DateOfBirth.HasValue,
                !string.IsNullOrEmpty(user.Address),
                !string.IsNullOrEmpty(user.ProfileImageUrl)
            };

            var completedFields = fields.Count(f => f);
            return (decimal)completedFields / fields.Count * 100;
        }

        /// <summary>
        /// Calculates profile completeness validation
        /// </summary>
        public static UserProfileCompleteness CalculateProfileCompleteness(User user)
        {
            var missingFields = new List<string>();
            var suggestions = new List<string>();

            if (string.IsNullOrEmpty(user.FirstName))
            {
                missingFields.Add("firstName");
                suggestions.Add("Vui lòng thêm tên");
            }

            if (string.IsNullOrEmpty(user.LastName))
            {
                missingFields.Add("lastName");
                suggestions.Add("Vui lòng thêm họ");
            }

            if (string.IsNullOrEmpty(user.Phone))
            {
                missingFields.Add("phone");
                suggestions.Add("Vui lòng thêm số điện thoại");
            }

            if (!user.DateOfBirth.HasValue)
            {
                missingFields.Add("dateOfBirth");
                suggestions.Add("Vui lòng thêm ngày sinh");
            }

            if (string.IsNullOrEmpty(user.Address))
            {
                missingFields.Add("address");
                suggestions.Add("Vui lòng thêm địa chỉ");
            }

            if (string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                missingFields.Add("profileImageUrl");
                suggestions.Add("Thêm ảnh đại diện để hoàn thiện profile");
            }

            return new UserProfileCompleteness
            {
                Completeness = CalculateCompletenessPercentage(user),
                MissingFields = missingFields,
                Suggestions = suggestions
            };
        }
    }
}