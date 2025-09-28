using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class CoOwner
{
    public int UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CoOwnerGroup> CoOwnerGroups { get; set; } = new List<CoOwnerGroup>();

    public virtual ICollection<DrivingLicense> DrivingLicenses { get; set; } = new List<DrivingLicense>();

    public virtual ICollection<FundAddition> FundAdditions { get; set; } = new List<FundAddition>();

    public virtual User User { get; set; } = null!;
}
