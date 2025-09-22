using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IImageService
{
    Task<ImageUploadResult> SaveImageAsync(IFormFile imageFile);
    Task<bool> DeleteImageAsync(string fileName);
    Task<List<string>> GetAllImageUrlsAsync();
    bool IsValidImageFormat(IFormFile file);
    string GenerateUniqueFileName(string originalFileName);
}

public class ImageUploadResult
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

