using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class DrivingLicense
{
    public int Id { get; set; }

    public int? CoOwnerId { get; set; }

    public string LicenseNumber { get; set; } = null!;

    public string IssuedBy { get; set; } = null!;

    public DateOnly IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? LicenseImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CoOwner? CoOwner { get; set; }
}
