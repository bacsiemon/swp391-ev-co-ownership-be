using System;
using System.Collections.Generic;
using EvCoOwnership.Repositories.Enums;

namespace EvCoOwnership.Repositories.Models
{
    public partial class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? ProfileImageUrl { get; set; }
        public EUserStatus? StatusEnum { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string NormalizedEmail { get; set; } = null!;
        public string PasswordSalt { get; set; } = null!;

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
        public virtual ICollection<CheckOut> CheckOuts { get; set; } = new List<CheckOut>();
        public virtual CoOwner? CoOwner { get; set; }
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual UserRefreshToken? UserRefreshToken { get; set; }
        public virtual ICollection<VehicleCondition> VehicleConditions { get; set; } = new List<VehicleCondition>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
