using System.Collections.Generic;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models
{
    public partial class Role
    {
        public int Id { get; set; }

        public EUserRole RoleNameEnum { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
