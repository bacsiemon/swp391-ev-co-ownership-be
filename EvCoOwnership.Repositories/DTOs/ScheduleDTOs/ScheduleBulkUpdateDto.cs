using System.Collections.Generic;
namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleBulkUpdateDto {
        public List<ScheduleUpdateDto> Updates { get; set; }
    }
}