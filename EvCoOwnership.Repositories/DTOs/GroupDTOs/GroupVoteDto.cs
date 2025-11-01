namespace EvCoOwnership.Repositories.DTOs.GroupDTOs
{
    public class GroupVoteDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}