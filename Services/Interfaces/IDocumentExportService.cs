using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentExportService
{
    Task<string> ExportContentAsync(string content, FormatExport format);
    Task<ExportResult> ExportWithMetadataAsync(string content, FormatExport format, ExportOptions options);
}

public class ExportResult
{
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public TimeSpan GenerationTime { get; set; }
}

public class ExportOptions
{
    public string FileName { get; set; } = string.Empty;
    public bool SaveToFile { get; set; } = true;
    public string OutputDirectory { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}

public interface IExportStrategy
{
    Task<ExportResult> ExportAsync(string content, ExportOptions options);
    string GetMimeType();
    string GetFileExtension();
}