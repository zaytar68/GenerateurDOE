using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Models;

namespace GenerateurDOE.Controllers;

[ApiController]
[Route("api/toc")]
public class TableOfContentsController : ControllerBase
{
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IDocumentRepositoryService _documentRepository;
    private readonly IPdfPageCountService _pdfPageCountService;
    private readonly ILogger<TableOfContentsController> _logger;

    public TableOfContentsController(
        IPdfGenerationService pdfGenerationService,
        IDocumentRepositoryService documentRepository,
        IPdfPageCountService pdfPageCountService,
        ILogger<TableOfContentsController> logger)
    {
        _pdfGenerationService = pdfGenerationService;
        _documentRepository = documentRepository;
        _pdfPageCountService = pdfPageCountService;
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

            // Récupérer le document avec toutes ses relations pour la génération TOC
            var document = await _documentRepository.GetWithCompleteContentAsync(documentId);

            // Générer la structure de la table des matières
            // Note: Cette méthode est actuellement privée dans PdfGenerationService
            // On va créer une méthode publique pour l'exposer
            var tocData = await GenerateTableOfContentsDataAsync(document);

            var response = new TocStructureResponse
            {
                DocumentId = documentId,
                DocumentTitle = document.NomFichier,
                IncludePageGuard = document.IncludePageDeGarde,
                IncludeTableOfContents = document.IncludeTableMatieres,
                Entries = tocData.Entries.Select(MapToApiEntry).ToList()
            };

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

    /// <summary>
    /// Génère les données de la table des matières pour un document
    /// Cette méthode réplique la logique de BuildTableOfContentsAsync du PdfGenerationService
    /// </summary>
    private async Task<TableOfContentsData> GenerateTableOfContentsDataAsync(Models.DocumentGenere document)
    {
        var tocData = new TableOfContentsData();
        var pageNumber = 1;

        // Page de garde
        if (document.IncludePageDeGarde)
            pageNumber++;

        // Table des matières elle-même (estimation : 1 à 2 pages selon le nombre d'entrées)
        var estimatedTocPages = document.SectionsConteneurs?.Count() > 10 ? 2 : 1;
        pageNumber += estimatedTocPages;

        // Sections libres
        if (document.SectionsConteneurs?.Any() == true)
        {
            foreach (var container in document.SectionsConteneurs.OrderBy(sc => sc.Ordre))
            {
                var entry = new TocEntry
                {
                    Title = container.Titre,
                    Level = 1,
                    PageNumber = pageNumber
                };

                if (container.Items?.Any() == true)
                {
                    foreach (var section in container.Items.OrderBy(sl => sl.Ordre))
                    {
                        entry.Children.Add(new TocEntry
                        {
                            Title = section.SectionLibre.Titre,
                            Level = 2,
                            PageNumber = pageNumber
                        });
                    }
                }

                tocData.Entries.Add(entry);
                // Estimation plus précise : 1 page par section + 1 page pour chaque 3 sous-sections
                var sectionPages = 1 + (container.Items?.Count() ?? 0) / 3;
                pageNumber += sectionPages;
            }
        }

        // Tableau de synthèse des produits (si activé)
        if (document.FTConteneur?.AfficherTableauRecapitulatif == true &&
            document.FTConteneur?.Elements?.Any() == true)
        {
            var syntheseEntry = new TocEntry
            {
                Title = "Tableau de Synthèse des Produits",
                Level = 1,
                PageNumber = pageNumber
            };
            tocData.Entries.Add(syntheseEntry);
            pageNumber += 1;
        }

        // Fiches techniques avec calcul précis des pages PDF
        if (document.FTConteneur?.Elements?.Any() == true)
        {
            var ftEntry = new TocEntry
            {
                Title = document.FTConteneur.Titre,
                Level = 1,
                PageNumber = pageNumber
            };

            // Précharger le cache pour tous les fichiers PDF
            var pdfPaths = document.FTConteneur.Elements
                .Where(e => e.ImportPDF != null)
                .Select(e => e.ImportPDF.CheminFichier)
                .ToList();

            if (pdfPaths.Any())
            {
                await _pdfPageCountService.PreloadCacheAsync(pdfPaths);
            }

            foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
            {
                var title = element.FicheTechnique?.NomProduit ?? element.ImportPDF?.NomFichierOriginal ?? "Document";
                ftEntry.Children.Add(new TocEntry
                {
                    Title = title,
                    Level = 2,
                    PageNumber = pageNumber
                });

                // Calculer le nombre exact de pages pour ce PDF
                var pdfPageCount = 1; // Par défaut
                if (element.ImportPDF != null)
                {
                    var count = await _pdfPageCountService.GetPageCountAsync(element.ImportPDF.CheminFichier);
                    pdfPageCount = count ?? 1; // Si erreur, estimation à 1 page

                    // Mettre à jour la base de données si nécessaire
                    if (count.HasValue && element.ImportPDF.PageCount != count.Value)
                    {
                        element.ImportPDF.PageCount = count.Value;
                        // Note: La sauvegarde sera gérée par le service appelant si nécessaire
                    }
                }

                pageNumber += pdfPageCount;
            }

            tocData.Entries.Add(ftEntry);
        }

        await Task.CompletedTask;
        return tocData;
    }

    /// <summary>
    /// Mappe une TocEntry vers l'API DTO
    /// </summary>
    private static TocEntryDto MapToApiEntry(TocEntry entry)
    {
        return new TocEntryDto
        {
            Title = entry.Title,
            Level = entry.Level,
            PageNumber = entry.PageNumber,
            Children = entry.Children.Select(MapToApiEntry).ToList()
        };
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