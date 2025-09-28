using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public int? FundId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CoOwnerGroup> CoOwnerGroups { get; set; } = new List<CoOwnerGroup>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Fund? Fund { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
