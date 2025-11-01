namespace EvCoOwnership.DTOs.ScheduleDTOs
{
    public class ScheduleQueryDto {
        public int? VehicleId { get; set; }
        public int? UserId { get; set; }
        public string? Date { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Duration { get; set; }
    }
}