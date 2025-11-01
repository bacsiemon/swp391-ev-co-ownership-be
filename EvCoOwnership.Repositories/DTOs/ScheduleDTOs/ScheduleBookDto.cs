namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleBookDto {
        public int VehicleId { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Purpose { get; set; }
        public string? Notes { get; set; }
    }
}