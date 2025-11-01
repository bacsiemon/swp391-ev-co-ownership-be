namespace EvCoOwnership.Repositories.DTOs.GroupDTOs
{
    public class GroupMemberDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
    }
}