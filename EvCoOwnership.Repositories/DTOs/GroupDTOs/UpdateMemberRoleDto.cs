using System.ComponentModel.DataAnnotations;

namespace EvCoOwnership.Repositories.DTOs.GroupDTOs
{
    public class UpdateMemberRoleDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;
        
        [Range(0, 100)]
        public decimal? OwnershipPercentage { get; set; }
    }
}