namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the verification status types for vehicles in the system
    /// </summary>
    public enum EVehicleVerificationStatus
    {
        Pending = 0,
        VerificationRequested = 1,
        RequiresRecheck = 2,
        Verified = 3,
        Rejected = 4
    }
}