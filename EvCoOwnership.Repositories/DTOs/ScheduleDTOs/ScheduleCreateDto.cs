namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleCreateDto {
        public int VehicleId { get; set; }
        public string Title { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Description { get; set; }
        public string ScheduleType { get; set; }
        public int Priority { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
    }
}