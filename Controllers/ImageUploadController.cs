using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Controllers;

[ApiController]
[Route("api/images")]
public class ImageUploadController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImageUploadController> _logger;

    public ImageUploadController(IImageService imageService, ILogger<ImageUploadController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Réception d'une demande d'upload d'image");

            if (file == null)
            {
                _logger.LogWarning("Aucun fichier fourni dans la requête d'upload");
                return BadRequest(new { 
                    success = false, 
                    message = "Aucun fichier fourni" 
                });
            }

            var result = await _imageService.SaveImageAsync(file);

            if (result.Success)
            {
                _logger.LogInformation("Image uploadée avec succès : {FileName}", result.FileName);
                
                // Réponse compatible avec RadzenHtmlEditor
                return Ok(new
                {
                    success = true,
                    url = result.ImageUrl,
                    fileName = result.FileName,
                    size = result.FileSize
                });
            }
            else
            {
                _logger.LogWarning("Échec de l'upload : {ErrorMessage}", result.ErrorMessage);
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'upload d'image");
            return StatusCode(500, new
            {
                success = false,
                message = "Erreur interne du serveur"
            });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetImages()
    {
        try
        {
            var imageUrls = await _imageService.GetAllImageUrlsAsync();
            return Ok(new
            {
                success = true,
                images = imageUrls
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des images");
            return StatusCode(500, new
            {
                success = false,
                message = "Erreur lors de la récupération des images"
            });
        }
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteImage(string fileName)
    {
        try
        {
            var success = await _imageService.DeleteImageAsync(fileName);
            if (success)
            {
                return Ok(new { success = true, message = "Image supprimée avec succès" });
            }
            else
            {
                return NotFound(new { success = false, message = "Image non trouvée" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'image {FileName}", fileName);
            return StatusCode(500, new
            {
                success = false,
                message = "Erreur lors de la suppression"
            });
        }
    }
}