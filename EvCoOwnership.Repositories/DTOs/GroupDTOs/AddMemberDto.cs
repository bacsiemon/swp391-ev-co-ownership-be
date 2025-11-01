namespace EvCoOwnership.Repositories.DTOs.GroupDTOs
{
    public class AddMemberDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "Member";
        public decimal OwnershipPercentage { get; set; } = 0;
        public decimal InvestmentAmount { get; set; } = 0;
    }
}