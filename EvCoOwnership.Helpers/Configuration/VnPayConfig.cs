namespace EvCoOwnership.Helpers.Configuration
{
    /// <summary>
    /// VNPay configuration settings
    /// </summary>
    public class VnPayConfig
    {
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "2.1.0";
        public string Command { get; set; } = "pay";
        public string CurrCode { get; set; } = "VND";
        public string Locale { get; set; } = "vn";
        public string TimeZoneId { get; set; } = "SE Asia Standard Time";
    }
}
