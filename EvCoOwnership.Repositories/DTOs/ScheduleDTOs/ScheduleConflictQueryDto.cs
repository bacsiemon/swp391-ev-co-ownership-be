namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleConflictQueryDto {
        public int VehicleId { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
    }
}