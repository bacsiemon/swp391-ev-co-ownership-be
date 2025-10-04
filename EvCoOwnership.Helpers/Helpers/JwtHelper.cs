using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EvCoOwnership.Helpers.Helpers
{
    /// <summary>
    /// Interface for user data needed for JWT token generation
    /// </summary>
    public interface IJwtUserData
    {
        int Id { get; }
        string Email { get; }
        string FirstName { get; }
        string LastName { get; }
    }

    /// <summary>
    /// Helper class for JWT token operations
    /// </summary>
    public static class JwtHelper
    {
        /// <summary>
        /// Generates an access token for the user
        /// </summary>
        /// <param name="user">User data</param>
        /// <param name="roles">User roles</param>
        /// <param name="configuration">Configuration containing JWT settings</param>
        /// <returns>JWT access token</returns>
        public static string GenerateAccessToken(IJwtUserData user, IEnumerable<string> roles, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        /// <returns>Refresh token string</returns>
        public static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Gets the access token expiration time
        /// </summary>
        /// <param name="configuration">Configuration containing JWT settings</param>
        /// <returns>Expiration DateTime</returns>
        public static DateTime GetAccessTokenExpiration(IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");
            return DateTime.UtcNow.AddMinutes(expirationMinutes);
        }

        /// <summary>
        /// Gets the refresh token expiration time
        /// </summary>
        /// <param name="configuration">Configuration containing JWT settings</param>
        /// <returns>Expiration DateTime</returns>
        public static DateTime GetRefreshTokenExpiration(IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var expirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");
            return DateTime.UtcNow.AddDays(expirationDays);
        }

        /// <summary>
        /// Validates and extracts claims from a JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <param name="configuration">Configuration containing JWT settings</param>
        /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
        public static ClaimsPrincipal? ValidateToken(string token, IConfiguration configuration)
        {
            try
            {
                var jwtSettings = configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                var issuer = jwtSettings["Issuer"];
                var audience = jwtSettings["Audience"];

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}