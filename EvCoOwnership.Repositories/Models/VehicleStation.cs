using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class VehicleStation
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Address { get; set; } = null!;

    public string? ContactNumber { get; set; }

    public decimal LocationLatitude { get; set; }

    public decimal LocationLongitude { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<CheckOut> CheckOuts { get; set; } = new List<CheckOut>();
}
