using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class Configuration
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
