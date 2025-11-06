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
        CoOwnershipAgreement = 0,

        /// <summary>
        /// Vehicle usage agreement
        /// </summary>
        VehicleUsageAgreement = 1,

        /// <summary>
        /// Cost sharing agreement
        /// </summary>
        CostSharingAgreement = 2,

        /// <summary>
        /// Maintenance responsibility agreement
        /// </summary>
        MaintenanceAgreement = 3,

        /// <summary>
        /// Insurance agreement
        /// </summary>
        InsuranceAgreement = 4,

        /// <summary>
        /// Ownership transfer agreement
        /// </summary>
        OwnershipTransferAgreement = 5,

        /// <summary>
        /// Dispute resolution agreement
        /// </summary>
        DisputeResolutionAgreement = 6,

        /// <summary>
        /// Exit/Termination agreement
        /// </summary>
        ExitAgreement = 7
    }
}
