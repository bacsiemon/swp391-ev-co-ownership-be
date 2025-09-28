using System;
using System.Collections.Generic;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models;

public partial class CoOwnerGroup
{
    public int CoOwnerId { get; set; }

    public int GroupId { get; set; }

    public decimal OwnershipPercentage { get; set; }

    public DateOnly JoinDate { get; set; }

    public decimal InvestmentAmount { get; set; }

    public ECoOwnerStatus? StatusEnum { get; set; }

    public virtual CoOwner CoOwner { get; set; } = null!;

    public virtual Group Group { get; set; } = null!;
}
