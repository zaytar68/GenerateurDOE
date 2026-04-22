using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Controllers;

/// <summary>
/// Contrôleur API pour le téléchargement de documents générés
/// Gère les gros fichiers PDF qui dépassent les limites de sérialisation JSON
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentDownloadController : ControllerBase
{
    private readonly IDocumentDownloadService _documentDownloadService;
    private readonly ILogger<DocumentDownloadController> _logger;

    public DocumentDownloadController(
        IDocumentDownloadService documentDownloadService,
        ILogger<DocumentDownloadController> logger)
    {
        _documentDownloadService = documentDownloadService;
        _logger = logger;
    }

    /// <summary>
    /// Génère et télécharge un document
    /// Supporte les gros fichiers PDF sans limitation de taille
    /// </summary>
    /// <param name="documentId">ID du document à générer</param>
    /// <returns>Le fichier généré en streaming</returns>
    [HttpGet("{documentId}")]
    public async Task<IActionResult> DownloadDocument(int documentId)
    {
        try
        {
            _logger.LogInformation("Demande de téléchargement pour le document {DocumentId}", documentId);

            // Préparation du document (génération PDF, HTML, etc.)
            var result = await _documentDownloadService.PrepareDocumentForDownloadAsync(documentId);

            if (!result.Success)
            {
                _logger.LogWarning("Échec de la génération du document {DocumentId}: {Error}",
                    documentId, result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation("Document {DocumentId} généré avec succès - Taille: {Size} octets",
                documentId, result.FileBytes.Length);

            // Retourner le fichier en streaming (pas de limite de taille)
            return File(
                result.FileBytes,
                result.MimeType,
                result.FileName,
                enableRangeProcessing: true // Support des téléchargements partiels
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement du document {DocumentId}", documentId);
            return StatusCode(500, new { error = "Erreur serveur lors de la génération du document" });
        }
    }
}
