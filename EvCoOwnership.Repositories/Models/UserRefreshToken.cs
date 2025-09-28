using System;
using System.Collections.Generic;

namespace EvCoOwnership.Repositories.Models;

public partial class UserRefreshToken
{
    public int UserId { get; set; }

    public string RefreshToken { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public virtual User User { get; set; } = null!;
}
