using GenerateurDOE.Services.Interfaces;
using Microsoft.Extensions.Options;
using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Implementations;

public class ImageService : IImageService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ImageService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedMimeTypes = { 
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" 
    };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImageService(IConfigurationService configurationService, ILogger<ImageService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<ImageUploadResult> SaveImageAsync(IFormFile imageFile)
    {
        var result = new ImageUploadResult();

        try
        {
            // Validation du fichier
            if (imageFile == null || imageFile.Length == 0)
            {
                result.ErrorMessage = "Aucun fichier fourni";
                return result;
            }

            if (imageFile.Length > MaxFileSize)
            {
                result.ErrorMessage = $"Le fichier est trop volumineux (max {MaxFileSize / (1024 * 1024)} MB)";
                return result;
            }

            if (!IsValidImageFormat(imageFile))
            {
                result.ErrorMessage = "Format d'image non supporté";
                return result;
            }

            // Obtenir le répertoire de stockage depuis la configuration
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var imagesDirectory = appSettings.RepertoireStockageImages;

            if (string.IsNullOrEmpty(imagesDirectory))
            {
                result.ErrorMessage = "Répertoire de stockage des images non configuré";
                return result;
            }

            // S'assurer que le répertoire existe
            if (!Directory.Exists(imagesDirectory))
            {
                await _configurationService.CreateDirectoryAsync(imagesDirectory);
            }

            // Générer un nom de fichier unique
            var uniqueFileName = GenerateUniqueFileName(imageFile.FileName);
            var filePath = Path.Combine(imagesDirectory, uniqueFileName);

            // Sauvegarder le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            result.Success = true;
            result.FileName = uniqueFileName;
            result.ImageUrl = $"/images/{uniqueFileName}";
            result.FileSize = imageFile.Length;

            _logger.LogInformation("Image sauvegardée avec succès : {FileName}", uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde de l'image");
            result.ErrorMessage = "Erreur lors de la sauvegarde de l'image";
        }

        return result;
    }

    public async Task<bool> DeleteImageAsync(string fileName)
    {
        try
        {
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var filePath = Path.Combine(appSettings.RepertoireStockageImages, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Image supprimée : {FileName}", fileName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'image {FileName}", fileName);
            return false;
        }
    }

    public async Task<List<string>> GetAllImageUrlsAsync()
    {
        try
        {
            var appSettings = await _configurationService.GetAppSettingsAsync();
            var imagesDirectory = appSettings.RepertoireStockageImages;

            if (!Directory.Exists(imagesDirectory))
            {
                return new List<string>();
            }

            var imageFiles = Directory.GetFiles(imagesDirectory)
                .Where(f => _allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => $"/images/{Path.GetFileName(f)}")
                .ToList();

            return imageFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des images");
            return new List<string>();
        }
    }

    public bool IsValidImageFormat(IFormFile file)
    {
        if (file == null)
            return false;

        // Vérifier l'extension
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            return false;

        // Vérifier le type MIME
        if (!_allowedMimeTypes.Contains(file.ContentType.ToLower()))
            return false;

        return true;
    }

    public string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        
        // Nettoyer le nom de fichier
        nameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(
            nameWithoutExtension, @"[^a-zA-Z0-9\-_]", "_");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8]; // 8 premiers caractères du GUID

        return $"{nameWithoutExtension}_{timestamp}_{guid}{extension}";
    }
}