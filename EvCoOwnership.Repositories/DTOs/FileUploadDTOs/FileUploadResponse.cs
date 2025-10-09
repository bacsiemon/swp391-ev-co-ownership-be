namespace EvCoOwnership.DTOs.FileUploadDTOs
{
    public class FileUploadResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime? UploadedAt { get; set; }
        public string DownloadUrl { get; set; } = null!;
    }
}