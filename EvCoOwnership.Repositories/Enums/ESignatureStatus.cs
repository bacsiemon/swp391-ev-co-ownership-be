namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Status of a contract signature
    /// </summary>
    public enum ESignatureStatus
    {
        /// <summary>
        /// Signature is pending - waiting for user to sign
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Contract has been signed
        /// </summary>
        Signed = 1,

        /// <summary>
        /// Signature was declined/rejected
        /// </summary>
        Declined = 2,

        /// <summary>
        /// Signature expired (time limit exceeded)
        /// </summary>
        Expired = 3,

        /// <summary>
        /// Signature was revoked/cancelled
        /// </summary>
        Revoked = 4
    }
}
