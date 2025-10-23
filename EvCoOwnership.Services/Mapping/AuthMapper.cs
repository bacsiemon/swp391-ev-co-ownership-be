using EvCoOwnership.DTOs.AuthDTOs;
using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvCoOwnership.Services.Mapping
{
    /// <summary>
    /// Static mapper class for mappings between Auth DTOs and entities
    /// </summary>
    public static class AuthMapper
    {
        /// <summary>
        /// Maps RegisterRequest DTO to User entity
        /// </summary>
        /// <param name="request">RegisterRequest DTO</param>
        /// <param name="passwordHash">Hashed password</param>
        /// <param name="passwordSalt">Password salt</param>
        /// <returns>User entity</returns>
        public static User ToEntity(this RegisterRequest request, string passwordHash, string passwordSalt)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            if (string.IsNullOrEmpty(passwordHash))
                throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));
            
            if (string.IsNullOrEmpty(passwordSalt))
                throw new ArgumentException("Password salt cannot be null or empty", nameof(passwordSalt));

            return new User
            {
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpperInvariant(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                RoleEnum = EUserRole.CoOwner, // Default role for new registrations
                StatusEnum = EUserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Maps User entity to UserInfo DTO
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>UserInfo DTO</returns>
        public static UserInfo ToUserInfo(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var roles = new List<string> { user.RoleEnum.ToString() };

            return new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                ProfileImageUrl = user.ProfileImageUrl,
                Status = user.StatusEnum.ToString(),
                Roles = roles
            };
        }

        /// <summary>
        /// Creates a LoginResponse DTO with tokens and user information
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="accessToken">JWT access token</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="accessTokenExpires">Access token expiration date</param>
        /// <param name="refreshTokenExpires">Refresh token expiration date</param>
        /// <returns>LoginResponse DTO</returns>
        public static LoginResponse ToLoginResponse(this User user, 
            string accessToken, 
            string refreshToken, 
            DateTime accessTokenExpires, 
            DateTime refreshTokenExpires)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
            
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpires,
                RefreshTokenExpiresAt = refreshTokenExpires,
                User = user.ToUserInfo()
            };
        }

        /// <summary>
        /// Maps User entity to registration response DTO
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>Registration response data</returns>
        public static object ToRegistrationResponse(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return new
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }
    }
}