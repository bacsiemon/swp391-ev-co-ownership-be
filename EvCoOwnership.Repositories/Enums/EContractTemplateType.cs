namespace EvCoOwnership.Repositories.Enums
{
    /// <summary>
    /// Types of contract templates in the system
    /// </summary>
    public enum EContractTemplateType
    {
        /// <summary>
        /// Co-ownership agreement contract
        /// </summary>
        CoOwnershipAgreement = 1,

        /// <summary>
        /// Vehicle usage agreement
        /// </summary>
        VehicleUsageAgreement = 2,

        /// <summary>
        /// Cost sharing agreement
        /// </summary>
        CostSharingAgreement = 3,

        /// <summary>
        /// Maintenance responsibility agreement
        /// </summary>
        MaintenanceAgreement = 4,

        /// <summary>
        /// Insurance agreement
        /// </summary>
        InsuranceAgreement = 5,

        /// <summary>
        /// Ownership transfer agreement
        /// </summary>
        OwnershipTransferAgreement = 6,

        /// <summary>
        /// Dispute resolution agreement
        /// </summary>
        DisputeResolutionAgreement = 7,

        /// <summary>
        /// Exit/Termination agreement
        /// </summary>
        ExitAgreement = 8
    }
}
