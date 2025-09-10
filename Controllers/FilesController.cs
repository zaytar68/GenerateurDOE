using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFicheTechniqueService _ficheTechniqueService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFicheTechniqueService ficheTechniqueService, ILogger<FilesController> logger)
    {
        _ficheTechniqueService = ficheTechniqueService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFile(int id)
    {
        try
        {
            _logger.LogInformation("Demande de téléchargement du fichier PDF avec l'ID: {Id}", id);

            var importPDF = await _ficheTechniqueService.GetPDFFileAsync(id);
            if (importPDF == null)
            {
                _logger.LogWarning("Fichier PDF non trouvé avec l'ID: {Id}", id);
                return NotFound("Fichier non trouvé");
            }

            if (!System.IO.File.Exists(importPDF.CheminFichier))
            {
                _logger.LogWarning("Fichier physique non trouvé: {CheminFichier}", importPDF.CheminFichier);
                return NotFound("Fichier physique non trouvé");
            }

            // Vérification basique du chemin (optionnelle pour une app interne)
            if (importPDF.CheminFichier.Contains(".."))
            {
                _logger.LogWarning("Chemin de fichier suspect: {CheminFichier}", importPDF.CheminFichier);
                return BadRequest("Chemin de fichier non valide");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(importPDF.CheminFichier);
            
            _logger.LogInformation("Fichier PDF envoyé avec succès: {NomFichier}", importPDF.NomFichierOriginal);

            return File(fileBytes, "application/pdf", importPDF.NomFichierOriginal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement du fichier PDF avec l'ID: {Id}", id);
            return StatusCode(500, "Erreur interne du serveur");
        }
    }
}