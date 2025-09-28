using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class Fund
{
    public int Id { get; set; }

    public decimal? CurrentBalance { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FundAddition> FundAdditions { get; set; } = new List<FundAddition>();

    public virtual ICollection<FundUsage> FundUsages { get; set; } = new List<FundUsage>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
