namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Represents the payment methods available in the system
    /// </summary>
    public enum EPaymentMethod
    {
        /// <summary>
        /// Bank transfer payment method
        /// </summary>
        BankTransfer = 0,

        /// <summary>
        /// Credit card payment method
        /// </summary>
        CreditCard = 1,

        /// <summary>
        /// Debit card payment method
        /// </summary>
        DebitCard = 2,

        /// <summary>
        /// Cash payment method
        /// </summary>
        Cash = 3
    }
}