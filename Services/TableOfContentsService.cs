using GenerateurDOE.Controllers;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GenerateurDOE.Services;

/// <summary>
/// Service pour la génération de structures de table des matières
/// </summary>
public class TableOfContentsService : ITableOfContentsService
{
    private readonly IDocumentRepositoryService _documentRepository;
    private readonly IPdfPageCountService _pdfPageCountService;
    private readonly ILogger<TableOfContentsService> _logger;

    public TableOfContentsService(
        IDocumentRepositoryService documentRepository,
        IPdfPageCountService pdfPageCountService,
        ILogger<TableOfContentsService> logger)
    {
        _documentRepository = documentRepository;
        _pdfPageCountService = pdfPageCountService;
        _logger = logger;
    }

    /// <summary>
    /// Génère la structure complète de la table des matières pour un document
    /// </summary>
    public async Task<TocStructureResponse> GenerateStructureAsync(int documentId)
    {
        try
        {
            _logger.LogInformation("Génération de la structure TDM pour le document {DocumentId}", documentId);

            // Récupérer le document avec toutes ses relations pour la génération TOC
            var document = await _documentRepository.GetWithCompleteContentAsync(documentId).ConfigureAwait(false);

            if (document == null)
            {
                throw new InvalidOperationException($"Document avec l'ID {documentId} introuvable");
            }

            // Générer la structure de la table des matières
            var tocData = await GenerateTableOfContentsDataAsync(document).ConfigureAwait(false);

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

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération de la structure TDM pour le document {DocumentId}", documentId);
            throw;
        }
    }

    /// <summary>
    /// Génère les données de la table des matières pour un document
    /// Cette méthode calcule les numéros de pages pour chaque entrée
    /// </summary>
    private async Task<TableOfContentsData> GenerateTableOfContentsDataAsync(DocumentGenere document)
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
                .Select(e => e.ImportPDF!.CheminFichier)
                .ToList();

            if (pdfPaths.Any())
            {
                await _pdfPageCountService.PreloadCacheAsync(pdfPaths).ConfigureAwait(false);
            }

            foreach (var element in document.FTConteneur.Elements.OrderBy(e => e.Ordre))
            {
                var title = FormatFicheTechniqueTitle(element);
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
                    var count = await _pdfPageCountService.GetPageCountAsync(element.ImportPDF.CheminFichier).ConfigureAwait(false);
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

    /// <summary>
    /// Formate le titre d'une fiche technique pour la table des matières
    /// Format: <strong>Marque</strong> Nom du produit - <em>type de document</em>
    /// </summary>
    private string FormatFicheTechniqueTitle(FTElement element)
    {
        if (element.FicheTechnique == null)
        {
            return element.ImportPDF?.NomFichierOriginal ?? "Document";
        }

        var marque = element.FicheTechnique.NomFabricant;
        var produit = element.FicheTechnique.NomProduit;
        var typeDocument = element.ImportPDF?.TypeDocumentImport?.Nom;

        // Construction du titre selon les données disponibles
        var titleParts = new List<string>();

        // Ajouter la marque en gras si disponible
        if (!string.IsNullOrWhiteSpace(marque))
        {
            titleParts.Add($"<strong>{marque}</strong>");
        }

        // Ajouter le nom du produit
        titleParts.Add(produit);

        // Construire la première partie (marque + produit)
        var mainPart = string.Join(" ", titleParts);

        // Ajouter le type de document en italique si disponible
        if (!string.IsNullOrWhiteSpace(typeDocument))
        {
            return $"{mainPart} - <em>{typeDocument}</em>";
        }

        return mainPart;
    }
}

/// <summary>
/// Classe publique pour stocker les données de la table des matières
/// Partagée entre TableOfContentsService et PdfGenerationService
/// </summary>
public class TableOfContentsData
{
    public List<TocEntry> Entries { get; set; } = new();
}

/// <summary>
/// Classe publique pour représenter une entrée de table des matières
/// Partagée entre TableOfContentsService et PdfGenerationService
/// </summary>
public class TocEntry
{
    public string Title { get; set; } = "";
    public int Level { get; set; }
    public int PageNumber { get; set; }
    public List<TocEntry> Children { get; set; } = new();
}
