using System.Security.Cryptography;
using System.Text;

namespace EvCoOwnership.Helpers.Helpers;

/// <summary>
/// Provides string hashing functionality using SHA256 algorithm
/// </summary>
public static class StringHasher
{
    /// <summary>
    /// Hashes a string using SHA256 algorithm
    /// </summary>
    /// <param name="input">The string to hash</param>
    /// <returns>The SHA256 hash as a hexadecimal string</returns>
    public static string Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a cryptographically secure salt
    /// </summary>
    /// <param name="length">The length of the salt in bytes (default: 32)</param>
    /// <returns>The salt as a base64 string</returns>
    public static string GenerateSalt(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("Salt length must be greater than 0", nameof(length));

        var saltBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Hashes a string with a salt using SHA256
    /// </summary>
    /// <param name="input">The string to hash</param>
    /// <param name="salt">The salt to use</param>
    /// <returns>The salted SHA256 hash as a hexadecimal string</returns>
    public static string HashWithSalt(string input, string salt)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        var saltedInput = input + salt;
        return Hash(saltedInput);
    }

    /// <summary>
    /// Verifies if a plain text string matches a hash
    /// </summary>
    /// <param name="plainText">The plain text to verify</param>
    /// <param name="hash">The hash to compare against</param>
    /// <param name="salt">Optional salt used in the original hash</param>
    /// <returns>True if the plain text matches the hash, false otherwise</returns>
    public static bool VerifyHash(string plainText, string hash, string? salt = null)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(hash))
            return false;

        var computedHash = salt != null ? HashWithSalt(plainText, salt) : Hash(plainText);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}