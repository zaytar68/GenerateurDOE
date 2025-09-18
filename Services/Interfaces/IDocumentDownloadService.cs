using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentDownloadService
{
    Task<DocumentDownloadResult> PrepareDocumentForDownloadAsync(int documentId);
}

public class DocumentDownloadResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}