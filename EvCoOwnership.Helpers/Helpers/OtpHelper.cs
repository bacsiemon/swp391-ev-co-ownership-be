using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace EvCoOwnership.Helpers.Helpers;

/// <summary>
/// Provides OTP (One-Time Password) generation and verification functionality with storage and automatic cleanup
/// </summary>
public static class OtpHelper
{
    private static readonly ConcurrentDictionary<string, OtpData> _otpStorage = new();
    private static readonly Timer _cleanupTimer;

    static OtpHelper()
    {
        // Cleanup expired OTPs every 60 seconds
        _cleanupTimer = new Timer(CleanupExpiredOtps, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Generates a cryptographically secure numeric OTP and stores it with the provided key
    /// </summary>
    /// <param name="key">The key to associate with this OTP (e.g., user ID, email, phone number)</param>
    /// <param name="length">The length of the OTP (default: 6)</param>
    /// <param name="expirationMinutes">OTP expiration time in minutes (default: 5)</param>
    /// <returns>A numeric OTP string</returns>
    public static string GenerateOtp(string key, int length = 6, int expirationMinutes = 5)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (length <= 0)
            throw new ArgumentException("OTP length must be greater than 0", nameof(length));

        if (expirationMinutes <= 0)
            throw new ArgumentException("Expiration minutes must be greater than 0", nameof(expirationMinutes));

        var otp = string.Empty;
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];

        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 10;
            otp += randomNumber.ToString();
        }

        var otpData = new OtpData
        {
            Code = otp,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        _otpStorage.AddOrUpdate(key, otpData, (k, v) => otpData);

        return otp;
    }

    /// <summary>
    /// Verifies if the provided OTP matches the stored OTP for the given key
    /// </summary>
    /// <param name="key">The key associated with the OTP</param>
    /// <param name="providedOtp">The OTP provided by the user</param>
    /// <param name="removeOnSuccess">Whether to remove the OTP from storage on successful verification (default: true)</param>
    /// <returns>True if the OTPs match and haven't expired, false otherwise</returns>
    public static bool VerifyOtp(string key, string providedOtp, bool removeOnSuccess = true)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(providedOtp))
            return false;

        if (!_otpStorage.TryGetValue(key, out var otpData))
            return false;

        // Check if OTP has expired
        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            _otpStorage.TryRemove(key, out _);
            return false;
        }

        var isValid = string.Equals(otpData.Code, providedOtp, StringComparison.Ordinal);

        if (isValid && removeOnSuccess)
        {
            _otpStorage.TryRemove(key, out _);
        }

        return isValid;
    }

    public static OtpData? GetOtpData(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        _otpStorage.TryGetValue(key, out var otpData);
        return otpData;
    }

    /// <summary>
    /// Removes an OTP from storage
    /// </summary>
    /// <param name="key">The key associated with the OTP to remove</param>
    /// <returns>True if the OTP was found and removed, false otherwise</returns>
    public static bool RemoveOtp(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return _otpStorage.TryRemove(key, out _);
    }

    /// <summary>
    /// Checks if an OTP exists for the given key
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if an OTP exists and hasn't expired, false otherwise</returns>
    public static bool HasValidOtp(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (!_otpStorage.TryGetValue(key, out var otpData))
            return false;

        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            _otpStorage.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the count of stored OTPs
    /// </summary>
    /// <returns>The number of OTPs currently in storage</returns>
    public static int GetStoredOtpCount()
    {
        return _otpStorage.Count;
    }

    /// <summary>
    /// Clears all stored OTPs
    /// </summary>
    public static void ClearAllOtps()
    {
        _otpStorage.Clear();
    }

    /// <summary>
    /// Cleanup timer callback that removes expired OTPs
    /// </summary>
    private static void CleanupExpiredOtps(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _otpStorage
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _otpStorage.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Represents OTP data with metadata
    /// </summary>
    public class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}