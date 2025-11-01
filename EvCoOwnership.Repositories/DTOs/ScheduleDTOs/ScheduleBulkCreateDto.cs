using System.Collections.Generic;
namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleBulkCreateDto {
        public List<ScheduleCreateDto> Schedules { get; set; }
    }
}