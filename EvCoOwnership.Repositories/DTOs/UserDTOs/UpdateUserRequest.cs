namespace EvCoOwnership.Repositories.DTOs.UserDTOs
{
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? CitizenId { get; set; }
        public int? ProfileImageId { get; set; }
    }
}
