using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Controllers;

[ApiController]
[Route("api/toc")]
public class TableOfContentsController : ControllerBase
{
    private readonly ITableOfContentsService _tableOfContentsService;
    private readonly ILogger<TableOfContentsController> _logger;

    public TableOfContentsController(
        ITableOfContentsService tableOfContentsService,
        ILogger<TableOfContentsController> logger)
    {
        _tableOfContentsService = tableOfContentsService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère la structure de la table des matières pour un document donné
    /// </summary>
    /// <param name="documentId">ID du document</param>
    /// <returns>Structure de la table des matières avec entrées et numéros de pages calculés</returns>
    [HttpGet("structure/{documentId}")]
    public async Task<IActionResult> GetDocumentStructure(int documentId)
    {
        try
        {
            _logger.LogInformation("Récupération de la structure TDM pour le document {DocumentId}", documentId);

            // Déléguer la génération au service
            var response = await _tableOfContentsService.GenerateStructureAsync(documentId).ConfigureAwait(false);

            _logger.LogInformation("Structure TDM générée avec {Count} entrées pour le document {DocumentId}",
                response.Entries.Count, documentId);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("introuvable"))
        {
            _logger.LogWarning("Document {DocumentId} non trouvé", documentId);
            return NotFound(new { Message = $"Document avec l'ID {documentId} non trouvé" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la structure TDM pour le document {DocumentId}", documentId);
            return StatusCode(500, new { Message = "Erreur interne du serveur", Error = ex.Message });
        }
    }
}

/// <summary>
/// Réponse de l'API pour la structure de la table des matières
/// </summary>
public class TocStructureResponse
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = "";
    public bool IncludePageGuard { get; set; }
    public bool IncludeTableOfContents { get; set; }
    public List<TocEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// DTO pour une entrée de table des matières
/// </summary>
public class TocEntryDto
{
    public string Title { get; set; } = "";
    public int Level { get; set; }
    public int PageNumber { get; set; }
    public List<TocEntryDto> Children { get; set; } = new();
}