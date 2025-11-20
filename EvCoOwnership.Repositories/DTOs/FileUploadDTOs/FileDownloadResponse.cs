namespace EvCoOwnership.Repositories.DTOs.FileUploadDTOs
{
    public class FileDownloadResponse
    {
        public byte[] Data { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
    }
}