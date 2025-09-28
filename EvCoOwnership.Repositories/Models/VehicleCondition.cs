using System;
using System.Collections.Generic;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models;

public partial class VehicleCondition
{
    public int Id { get; set; }

    public int? VehicleId { get; set; }

    public int? ReportedBy { get; set; }

    public EVehicleConditionType? ConditionTypeEnum { get; set; }

    public string? Description { get; set; }

    public string? PhotoUrls { get; set; }

    public int? OdometerReading { get; set; }

    public decimal? FuelLevel { get; set; }

    public bool? DamageReported { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<CheckOut> CheckOuts { get; set; } = new List<CheckOut>();

    public virtual User? ReportedByNavigation { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
