using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Markdig;
using System.Text;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service principal de gestion des documents générés avec génération de contenu, export PDF et gestion des sections
/// </summary>
public class DocumentGenereService : IDocumentGenereService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDocumentRepositoryService _documentRepository;
    private readonly IDocumentExportService _documentExport;
    private readonly AppSettings _appSettings;
    private readonly IFicheTechniqueService _ficheTechniqueService;
    private readonly IMemoireTechniqueService _memoireTechniqueService;
    private readonly IPdfGenerationService _pdfGenerationService;

    /// <summary>
    /// Initialise une nouvelle instance du service DocumentGenereService
    /// </summary>
    /// <param name="contextFactory">Factory pour créer les contextes Entity Framework</param>
    /// <param name="documentRepository">Service repository pour l'accès optimisé aux données</param>
    /// <param name="documentExport">Service d'export de documents en différents formats</param>
    /// <param name="appSettings">Configuration de l'application</param>
    /// <param name="ficheTechniqueService">Service de gestion des fiches techniques</param>
    /// <param name="memoireTechniqueService">Service de gestion des mémoires techniques</param>
    /// <param name="pdfGenerationService">Service de génération PDF avec PuppeteerSharp + PDFSharp</param>
    public DocumentGenereService(IDbContextFactory<ApplicationDbContext> contextFactory, IDocumentRepositoryService documentRepository,
        IDocumentExportService documentExport, IOptions<AppSettings> appSettings, IFicheTechniqueService ficheTechniqueService,
        IMemoireTechniqueService memoireTechniqueService, IPdfGenerationService pdfGenerationService)
    {
        _contextFactory = contextFactory;
        _documentRepository = documentRepository;
        _documentExport = documentExport;
        _appSettings = appSettings.Value;
        _ficheTechniqueService = ficheTechniqueService;
        _memoireTechniqueService = memoireTechniqueService;
        _pdfGenerationService = pdfGenerationService;
    }

    /// <summary>
    /// Exporte un document généré dans le format spécifié
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document à exporter</param>
    /// <param name="format">Format d'export (PDF, HTML, Markdown, Word)</param>
    /// <returns>Chemin du fichier exporté ou contenu formaté</returns>
    /// <exception cref="ArgumentException">Si le document n'existe pas</exception>
    public async Task<string> ExportDocumentAsync(int documentGenereId, FormatExport format)
    {
        var documentContent = await GenerateContentAsync(documentGenereId);
        return await _documentExport.ExportContentAsync(documentContent, format);
    }

    /// <summary>
    /// Génère le contenu complet d'un document en assemblant page de garde, table des matières et sections
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document à générer</param>
    /// <returns>Contenu complet du document en format Markdown</returns>
    /// <exception cref="ArgumentException">Si le document n'existe pas</exception>
    public async Task<string> GenerateContentAsync(int documentGenereId)
    {
        var document = await _documentRepository.GetWithCompleteContentAsync(documentGenereId);

        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var content = new StringBuilder();

        if (document.IncludePageDeGarde)
        {
            var typeDocumentTitle = GetDocumentTypeTitle(document.TypeDocument);
            content.AppendLine(GeneratePageDeGarde(document, typeDocumentTitle));
            content.AppendLine();
        }

        var allSections = document.SectionsConteneurs
            .Cast<IDocumentSection>()
            .Concat(document.FTConteneur != null ? new[] { document.FTConteneur } : Array.Empty<IDocumentSection>())
            .OrderBy(s => s.Ordre)
            .ToList();

        if (document.IncludeTableMatieres && allSections.Any())
        {
            content.AppendLine(GenerateTableMatieres(allSections, document));
            content.AppendLine();
        }

        foreach (var section in allSections)
        {
            switch (section)
            {
                case SectionConteneur sectionConteneur:
                    content.AppendLine(GenerateSectionConteneurContent(sectionConteneur));
                    break;
                case FTConteneur ftConteneur:
                    content.AppendLine(GenerateFTConteneurContent(ftConteneur));
                    break;
            }
            content.AppendLine();
        }

        return content.ToString();
    }

    /// <summary>
    /// Sauvegarde un nouveau document généré en base de données
    /// </summary>
    /// <param name="documentGenere">Document à sauvegarder</param>
    /// <returns>Document sauvegardé avec son identifiant généré</returns>
    public async Task<DocumentGenere> SaveDocumentGenereAsync(DocumentGenere documentGenere)
    {
        return await _documentRepository.CreateAsync(documentGenere);
    }

    /// <summary>
    /// Récupère tous les documents générés pour un chantier spécifique avec optimisation DTO
    /// </summary>
    /// <param name="chantierId">Identifiant du chantier</param>
    /// <returns>Liste des documents du chantier</returns>
    public async Task<IEnumerable<DocumentGenere>> GetDocumentsGeneresByChantierId(int chantierId)
    {
        var summaries = await _documentRepository.GetDocumentSummariesByChantierId(chantierId);
        return summaries.Select(s => new DocumentGenere 
        { 
            Id = s.Id, 
            NomFichier = s.NomFichier,
            TypeDocument = s.TypeDocument,
            FormatExport = s.FormatExport,
            DateCreation = s.DateCreation,
            EnCours = s.EnCours,
            IncludePageDeGarde = s.IncludePageDeGarde,
            IncludeTableMatieres = s.IncludeTableMatieres,
            NumeroLot = s.NumeroLot,
            IntituleLot = s.IntituleLot,
            ChantierId = chantierId
        });
    }

    /// <summary>
    /// Récupère un document généré par son identifiant
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <returns>Document trouvé ou null si non trouvé</returns>
    public async Task<DocumentGenere> GetByIdAsync(int documentGenereId)
    {
        return await _documentRepository.GetByIdAsync(documentGenereId);
    }

    /// <summary>
    /// Met à jour un document généré existant
    /// </summary>
    /// <param name="documentGenere">Document avec les modifications</param>
    /// <returns>Document mis à jour</returns>
    public async Task<DocumentGenere> UpdateAsync(DocumentGenere documentGenere)
    {
        return await _documentRepository.UpdateAsync(documentGenere);
    }

    /// <summary>
    /// Duplique un document existant avec un nouveau nom dans le même chantier
    /// </summary>
    /// <param name="documentId">Identifiant du document source</param>
    /// <param name="newName">Nouveau nom pour la copie</param>
    /// <returns>Document dupliqué</returns>
    public async Task<DocumentGenere> DuplicateAsync(int documentId, string newName)
    {
        return await _documentRepository.DuplicateAsync(documentId, newName);
    }

    /// <summary>
    /// Duplique un document vers un autre chantier avec de nouvelles informations de lot
    /// </summary>
    /// <param name="documentId">Identifiant du document source</param>
    /// <param name="newName">Nouveau nom pour la copie</param>
    /// <param name="newChantierId">Identifiant du chantier de destination</param>
    /// <param name="numeroLot">Numéro du lot pour le nouveau document</param>
    /// <param name="intituleLot">Intitulé du lot pour le nouveau document</param>
    /// <returns>Document dupliqué dans le nouveau chantier</returns>
    public async Task<DocumentGenere> DuplicateToChantierAsync(int documentId, string newName, int newChantierId, string numeroLot, string intituleLot)
    {
        return await _documentRepository.DuplicateToChantierAsync(documentId, newName, newChantierId, numeroLot, intituleLot);
    }

    /// <summary>
    /// Supprime définitivement un document généré et toutes ses données associées
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document à supprimer</param>
    /// <returns>True si suppression réussie, False si document non trouvé</returns>
    public async Task<bool> DeleteDocumentGenereAsync(int documentGenereId)
    {
        return await _documentRepository.DeleteAsync(documentGenereId);
    }

    /// <summary>
    /// Génère le contenu de la page de garde en format Markdown
    /// </summary>
    /// <param name="document">Document pour lequel générer la page de garde</param>
    /// <param name="typeDocument">Titre du type de document (DOE, Dossier Technique, etc.)</param>
    /// <returns>Contenu de la page de garde en Markdown</returns>
    private string GeneratePageDeGarde(DocumentGenere document, string typeDocument)
    {
        var pageDeGarde = new StringBuilder();

        pageDeGarde.AppendLine($"# {typeDocument}");
        pageDeGarde.AppendLine();
        pageDeGarde.AppendLine($"**Projet :** {document.Chantier.NomProjet}");
        pageDeGarde.AppendLine($"**Maître d'œuvre :** {document.Chantier.MaitreOeuvre}");
        pageDeGarde.AppendLine($"**Maître d'ouvrage :** {document.Chantier.MaitreOuvrage}");
        pageDeGarde.AppendLine($"**Adresse :** {document.Chantier.Adresse}");
        pageDeGarde.AppendLine($"**Lot :** {document.NumeroLot} - {document.IntituleLot}");
        pageDeGarde.AppendLine();
        pageDeGarde.AppendLine($"**Société :** {_appSettings.NomSociete}");
        pageDeGarde.AppendLine($"**Date :** {DateTime.Now:dd/MM/yyyy}");

        return pageDeGarde.ToString();
    }

    /// <summary>
    /// Convertit le type de document en titre lisible
    /// </summary>
    /// <param name="typeDocument">Type de document énuméré</param>
    /// <returns>Titre complet du type de document</returns>
    private string GetDocumentTypeTitle(TypeDocumentGenere typeDocument)
    {
        return typeDocument switch
        {
            TypeDocumentGenere.DOE => "Dossier d'Ouvrages Exécutés",
            TypeDocumentGenere.DossierTechnique => "Dossier Technique",
            TypeDocumentGenere.MemoireTechnique => "Mémoire Technique",
            _ => "Document"
        };
    }

    /// <summary>
    /// Génère la table des matières basée sur la configuration du document
    /// </summary>
    /// <param name="sections">Sections du document à inclure</param>
    /// <param name="document">Document contenant la configuration de la table des matières</param>
    /// <returns>Table des matières formatée en Markdown</returns>
    private string GenerateTableMatieres(IEnumerable<IDocumentSection> sections, DocumentGenere document)
    {
        // Extraire les paramètres de la table des matières depuis le JSON
        var tocConfig = ExtractTableMatieresConfig(document.Parametres);

        var tableDesMatieres = new StringBuilder();
        tableDesMatieres.AppendLine($"## {tocConfig.Titre}");
        tableDesMatieres.AppendLine();

        var currentNumber = 1;
        var currentPage = 1;

        // Ajouter la page de garde si configurée
        if (tocConfig.IncludePageGarde && document.IncludePageDeGarde)
        {
            var entry = GenerateTableMatieresEntry(
                "Page de garde",
                currentNumber++,
                currentPage++,
                tocConfig,
                0);
            tableDesMatieres.AppendLine(entry);
        }

        // Ajouter les sections du document
        foreach (var section in sections)
        {
            var entry = GenerateTableMatieresEntry(
                section.Titre,
                currentNumber++,
                currentPage++,
                tocConfig,
                0);
            tableDesMatieres.AppendLine(entry);
        }

        return tableDesMatieres.ToString();
    }

    /// <summary>
    /// Génère une entrée individuelle de la table des matières
    /// </summary>
    /// <param name="titre">Titre de la section</param>
    /// <param name="numero">Numéro de la section</param>
    /// <param name="page">Numéro de page</param>
    /// <param name="config">Configuration de style de la table des matières</param>
    /// <param name="niveau">Niveau d'indentation de l'entrée</param>
    /// <returns>Entrée formatée pour la table des matières</returns>
    private string GenerateTableMatieresEntry(string titre, int numero, int page, TableMatieresConfig config, int niveau)
    {
        var entry = new StringBuilder();

        // Ajouter l'indentation selon le niveau
        for (int i = 0; i < niveau; i++)
        {
            entry.Append("  ");
        }

        // Ajouter le marqueur selon le style
        switch (config.StyleAffichage.ToLower())
        {
            case "numerote":
                entry.Append($"{numero}. ");
                break;
            case "liste":
            default:
                entry.Append("- ");
                break;
            case "tableau":
                // Pour markdown, on utilise le format liste même pour "tableau"
                entry.Append("- ");
                break;
        }

        entry.Append(titre);

        // Ajouter le numéro de page si activé
        if (config.IncludeNumeroPages)
        {
            entry.Append($" .......... {page}");
        }

        return entry.ToString();
    }

    /// <summary>
    /// Extrait la configuration de la table des matières depuis les paramètres JSON
    /// </summary>
    /// <param name="parametres">Paramètres JSON du document</param>
    /// <returns>Configuration de la table des matières ou configuration par défaut</returns>
    private TableMatieresConfig ExtractTableMatieresConfig(string? parametres)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(parametres))
            {
                var settings = System.Text.Json.JsonSerializer.Deserialize<TableMatieresSettings>(parametres);
                if (settings?.TableMatieres != null)
                {
                    return settings.TableMatieres;
                }
            }
        }
        catch
        {
            // Si erreur de désérialisation, utiliser les valeurs par défaut
        }

        // Retourner la configuration par défaut
        return new TableMatieresConfig();
    }

    /// <summary>
    /// Classe conteneur pour les paramètres de configuration JSON
    /// </summary>
    private class TableMatieresSettings
    {
        public TableMatieresConfig? TableMatieres { get; set; }
    }

    /// <summary>
    /// Configuration détaillée de la table des matières
    /// </summary>
    private class TableMatieresConfig
    {
        public string Titre { get; set; } = "Table des matières";
        public bool IncludeNumeroPages { get; set; } = true;
        public string StyleAffichage { get; set; } = "liste";
        public int ProfondeurMaximale { get; set; } = 2;
        public bool IncludePageGarde { get; set; } = true;
        public bool OrdreSectionsPersonnalise { get; set; } = false;
    }

    /// <summary>
    /// Génère le contenu d'un conteneur de sections libres
    /// </summary>
    /// <param name="sectionConteneur">Conteneur de sections à traiter</param>
    /// <returns>Contenu formaté en Markdown</returns>
    private string GenerateSectionConteneurContent(SectionConteneur sectionConteneur)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {sectionConteneur.Titre}");
        content.AppendLine();

        foreach (var item in sectionConteneur.Items.OrderBy(i => i.Ordre))
        {
            var sectionLibre = item.SectionLibre;
            if (sectionLibre == null) continue;

            if (!string.IsNullOrEmpty(sectionLibre.Titre))
            {
                content.AppendLine($"## {sectionLibre.Titre}");
                content.AppendLine();
            }

            if (!string.IsNullOrEmpty(sectionLibre.ContenuHtml))
            {
                content.AppendLine(ConvertHtmlToMarkdown(sectionLibre.ContenuHtml));
                content.AppendLine();
            }
        }

        return content.ToString();
    }

    /// <summary>
    /// Génère le contenu d'un conteneur de fiches techniques
    /// </summary>
    /// <param name="ftConteneur">Conteneur de fiches techniques à traiter</param>
    /// <returns>Contenu formaté en Markdown avec les fiches et PDFs</returns>
    private string GenerateFTConteneurContent(FTConteneur ftConteneur)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {ftConteneur.Titre}");
        content.AppendLine();

        foreach (var element in ftConteneur.Elements.OrderBy(e => e.Ordre))
        {
            if (element.FicheTechnique != null)
            {
                var fiche = element.FicheTechnique;
                content.AppendLine($"## {fiche.NomProduit}");
                content.AppendLine($"**Fabricant :** {fiche.NomFabricant}");
                content.AppendLine($"**Type :** {fiche.TypeProduit}");

                if (!string.IsNullOrEmpty(fiche.Description))
                {
                    content.AppendLine($"**Description :** {fiche.Description}");
                }
            }

            if (element.ImportPDF != null)
            {
                var pdf = element.ImportPDF;
                content.AppendLine($"**Document :** {pdf.NomFichierOriginal} ({pdf.TypeDocumentImport?.Nom ?? "Non défini"})");
            }

            content.AppendLine();
        }

        return content.ToString();
    }

    /// <summary>
    /// Convertit du HTML en Markdown (implémentation basique)
    /// </summary>
    /// <param name="html">Contenu HTML à convertir</param>
    /// <returns>Contenu converti en Markdown</returns>
    private string ConvertHtmlToMarkdown(string html)
    {
        // TODO: Implémentation basique - à améliorer avec une librairie de conversion HTML->Markdown
        var text = html
            .Replace("<p>", "")
            .Replace("</p>", "\n")
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<strong>", "**")
            .Replace("</strong>", "**")
            .Replace("<em>", "*")
            .Replace("</em>", "*")
            .Replace("<h1>", "# ")
            .Replace("</h1>", "")
            .Replace("<h2>", "## ")
            .Replace("</h2>", "")
            .Replace("<h3>", "### ")
            .Replace("</h3>", "");

        return text.Trim();
    }

    /// <summary>
    /// Formate le contenu selon le format d'export demandé
    /// </summary>
    /// <param name="content">Contenu source en Markdown</param>
    /// <param name="format">Format de sortie désiré</param>
    /// <returns>Contenu formaté</returns>
    /// <exception cref="ArgumentException">Si format non supporté</exception>
    private async Task<string> FormatContentAsync(string content, FormatExport format)
    {
        return format switch
        {
            FormatExport.Markdown => content,
            FormatExport.HTML => await ConvertToHtmlAsync(content),
            FormatExport.PDF => await ConvertToPdfAsync(content),
            FormatExport.Word => await ConvertToWordAsync(content),
            _ => throw new ArgumentException("Format non supporté", nameof(format))
        };
    }

    /// <summary>
    /// Convertit du Markdown en HTML complet avec styles CSS
    /// </summary>
    /// <param name="markdown">Contenu Markdown source</param>
    /// <returns>Document HTML complet avec styles intégrés</returns>
    private async Task<string> ConvertToHtmlAsync(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var html = Markdown.ToHtml(markdown, pipeline);

        await Task.CompletedTask;

        var htmlDocument = new StringBuilder();
        htmlDocument.AppendLine("<!DOCTYPE html>");
        htmlDocument.AppendLine("<html lang=\"fr\">");
        htmlDocument.AppendLine("<head>");
        htmlDocument.AppendLine("<meta charset=\"UTF-8\">");
        htmlDocument.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        htmlDocument.AppendLine("<title>Document Généré</title>");
        htmlDocument.AppendLine("<style>");
        htmlDocument.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }");
        htmlDocument.AppendLine("h1 { color: #2c3e50; border-bottom: 2px solid #3498db; }");
        htmlDocument.AppendLine("h2 { color: #34495e; margin-top: 30px; }");
        htmlDocument.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        htmlDocument.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
        htmlDocument.AppendLine("th { background-color: #f2f2f2; }");
        htmlDocument.AppendLine("</style>");
        htmlDocument.AppendLine("</head>");
        htmlDocument.AppendLine("<body>");
        htmlDocument.AppendLine(html);
        htmlDocument.AppendLine("</body>");
        htmlDocument.AppendLine("</html>");

        return htmlDocument.ToString();
    }

    /// <summary>
    /// Convertit du contenu en PDF via PuppeteerSharp
    /// </summary>
    /// <param name="content">Contenu Markdown à convertir</param>
    /// <returns>Chemin du fichier PDF généré</returns>
    private async Task<string> ConvertToPdfAsync(string content)
    {
        // Génération HTML complète à partir du markdown
        var html = await ConvertToHtmlAsync(content);

        // Conversion en PDF via le nouveau service
        var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(html);

        // Retourner le chemin du fichier sauvegardé ou les bytes encodés
        var fileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(_appSettings.RepertoireStockagePDF, fileName);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return filePath;
    }

    /// <summary>
    /// Convertit du contenu en format Word (actuellement simulé)
    /// </summary>
    /// <param name="content">Contenu à convertir</param>
    /// <returns>Contenu simulé ou chemin du fichier Word</returns>
    private async Task<string> ConvertToWordAsync(string content)
    {
        await Task.Delay(10);
        return $"[WORD] Simulation - Le contenu sera converti en Word :\n{content}";
    }

    /// <summary>
    /// Crée un nouveau conteneur de sections pour un document et type de section spécifiques
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document parent</param>
    /// <param name="typeSectionId">Identifiant du type de section</param>
    /// <param name="titre">Titre personnalisé ou null pour utiliser le nom du type</param>
    /// <returns>Conteneur de sections créé</returns>
    /// <exception cref="ArgumentException">Si document ou type de section non trouvé</exception>
    /// <exception cref="InvalidOperationException">Si un conteneur existe déjà pour ce type</exception>
    public async Task<SectionConteneur> CreateSectionConteneurAsync(int documentGenereId, int typeSectionId, string? titre = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var document = await context.DocumentsGeneres.FindAsync(documentGenereId).ConfigureAwait(false);
        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var typeSection = await context.TypesSections.FindAsync(typeSectionId).ConfigureAwait(false);
        if (typeSection == null)
            throw new ArgumentException("Type de section non trouvé", nameof(typeSectionId));

        var existingConteneur = await context.SectionsConteneurs
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId).ConfigureAwait(false);

        if (existingConteneur != null)
            throw new InvalidOperationException($"Un conteneur pour le type de section '{typeSection.Nom}' existe déjà pour ce document");

        var sectionConteneur = new SectionConteneur
        {
            DocumentGenereId = documentGenereId,
            TypeSectionId = typeSectionId,
            Titre = titre ?? typeSection.Nom,
            Ordre = await GetNextOrderForSectionConteneur(documentGenereId, context)
        };

        context.SectionsConteneurs.Add(sectionConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return sectionConteneur;
    }

    /// <summary>
    /// Récupère un conteneur de sections par document et type de section
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <param name="typeSectionId">Identifiant du type de section</param>
    /// <returns>Conteneur de sections avec ses éléments</returns>
    /// <exception cref="ArgumentException">Si le conteneur n'existe pas</exception>
    public async Task<SectionConteneur> GetSectionConteneurAsync(int documentGenereId, int typeSectionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionConteneur = await context.SectionsConteneurs
            .Include(sc => sc.Items)
            .Include(sc => sc.TypeSection)
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId).ConfigureAwait(false);

        if (sectionConteneur == null)
            throw new ArgumentException("Conteneur de section non trouvé");

        return sectionConteneur;
    }

    /// <summary>
    /// Récupère tous les conteneurs de sections d'un document triés par ordre
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <returns>Liste des conteneurs de sections ordonnés</returns>
    public async Task<IEnumerable<SectionConteneur>> GetSectionsConteneursByDocumentAsync(int documentGenereId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .Include(sc => sc.Items)
            .Include(sc => sc.TypeSection)
            .OrderBy(sc => sc.Ordre)
            .ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Supprime un conteneur de sections et tous ses éléments associés
    /// </summary>
    /// <param name="sectionConteneurId">Identifiant du conteneur à supprimer</param>
    /// <returns>True si suppression réussie, False si conteneur non trouvé</returns>
    public async Task<bool> DeleteSectionConteneurAsync(int sectionConteneurId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionConteneur = await context.SectionsConteneurs.FindAsync(sectionConteneurId).ConfigureAwait(false);
        if (sectionConteneur == null)
            return false;

        context.SectionsConteneurs.Remove(sectionConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Crée un conteneur de fiches techniques pour un document
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document parent</param>
    /// <param name="titre">Titre personnalisé ou null pour "Fiches Techniques"</param>
    /// <returns>Conteneur de fiches techniques créé</returns>
    /// <exception cref="ArgumentException">Si le document n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si un conteneur FT existe déjà</exception>
    public async Task<FTConteneur> CreateFTConteneurAsync(int documentGenereId, string? titre = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var document = await context.DocumentsGeneres.FindAsync(documentGenereId).ConfigureAwait(false);
        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var existingFTConteneur = await context.FTConteneurs
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId).ConfigureAwait(false);

        if (existingFTConteneur != null)
            throw new InvalidOperationException("Un conteneur de fiches techniques existe déjà pour ce document");

        var ftConteneur = new FTConteneur
        {
            DocumentGenereId = documentGenereId,
            Titre = titre ?? "Fiches Techniques",
            Ordre = await GetNextOrderForDocument(documentGenereId, context)
        };

        context.FTConteneurs.Add(ftConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return ftConteneur;
    }

    /// <summary>
    /// Récupère le conteneur de fiches techniques d'un document avec optimisation de requête
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <returns>Conteneur de fiches techniques ou null si non trouvé</returns>
    public async Task<FTConteneur?> GetFTConteneurByDocumentAsync(int documentGenereId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // 🔧 CORRECTION CONCURRENCE: Un seul Include avec navigation property multiple
        return await context.FTConteneurs
            .Include(ftc => ftc.Elements.OrderBy(fte => fte.Ordre))
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF!.TypeDocumentImport)
            .AsSplitQuery()  // ✅ Forcer split explicite pour contrôler la concurrence
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId).ConfigureAwait(false);
    }

    /// <summary>
    /// Met à jour un conteneur de fiches techniques existant
    /// </summary>
    /// <param name="ftConteneur">Conteneur avec les modifications</param>
    /// <returns>Conteneur mis à jour</returns>
    public async Task<FTConteneur> UpdateFTConteneurAsync(FTConteneur ftConteneur)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        context.FTConteneurs.Update(ftConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return ftConteneur;
    }

    /// <summary>
    /// Supprime un conteneur de fiches techniques et tous ses éléments
    /// </summary>
    /// <param name="ftConteneursId">Identifiant du conteneur à supprimer</param>
    /// <returns>True si suppression réussie, False si conteneur non trouvé</returns>
    public async Task<bool> DeleteFTConteneurAsync(int ftConteneursId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var ftConteneur = await context.FTConteneurs.FindAsync(ftConteneursId).ConfigureAwait(false);
        if (ftConteneur == null)
            return false;

        context.FTConteneurs.Remove(ftConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Finalise un document en cours en le marquant comme terminé
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document à finaliser</param>
    /// <returns>Document finalisé</returns>
    /// <exception cref="ArgumentException">Si le document n'existe pas</exception>
    /// <exception cref="InvalidOperationException">Si le document ne peut pas être finalisé</exception>
    public async Task<DocumentGenere> FinalizeDocumentAsync(int documentGenereId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var document = await context.DocumentsGeneres.FindAsync(documentGenereId).ConfigureAwait(false);
        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        if (!await CanFinalizeDocumentAsync(documentGenereId))
            throw new InvalidOperationException("Le document ne peut pas être finalisé dans son état actuel");

        document.EnCours = false;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return document;
    }

    /// <summary>
    /// Génère un PDF complet avec toutes les sections du document
    /// </summary>
    public async Task<byte[]> GenerateCompletePdfAsync(int documentGenereId, PdfGenerationOptions? options = null)
    {
        // 🔧 CORRECTION CONCURRENCE: Utiliser Repository Pattern pour requête optimisée
        var document = await _documentRepository.GetWithCompleteContentAsync(documentGenereId);

        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        return await _pdfGenerationService.GenerateCompletePdfAsync(document, options);
    }

    /// <summary>
    /// Sauvegarde un PDF généré sur le système de fichiers
    /// </summary>
    /// <summary>
    /// Sauvegarde un PDF généré sur le système de fichiers
    /// </summary>
    /// <param name="pdfBytes">Données binaires du PDF</param>
    /// <param name="fileName">Nom du fichier de destination</param>
    /// <returns>Chemin complet du fichier sauvegardé</returns>
    public async Task<string> SavePdfAsync(byte[] pdfBytes, string fileName)
    {
        var fullPath = Path.Combine(_appSettings.RepertoireStockagePDF, fileName);
        await File.WriteAllBytesAsync(fullPath, pdfBytes);
        return fullPath;
    }

    /// <summary>
    /// Vérifie si un document peut être finalisé (contient du contenu)
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document à vérifier</param>
    /// <returns>True si le document peut être finalisé</returns>
    public async Task<bool> CanFinalizeDocumentAsync(int documentGenereId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // 🔧 CORRECTION CONCURRENCE: Requête optimisée sans Include multiple
        var hasContent = await context.DocumentsGeneres
            .Where(d => d.Id == documentGenereId)
            .Select(d => new
            {
                HasSectionsContent = d.SectionsConteneurs.Any(sc => sc.Items.Any()),
                HasFTContent = d.FTConteneur != null && d.FTConteneur.Elements.Any()
            })
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (hasContent == null)
            return false;

        return hasContent.HasSectionsContent || hasContent.HasFTContent;
    }

    /// <summary>
    /// Calcule le prochain ordre disponible pour un conteneur de sections
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <param name="context">Contexte EF optionnel pour réutiliser une transaction</param>
    /// <returns>Numéro d'ordre suivant</returns>
    private async Task<int> GetNextOrderForSectionConteneur(int documentGenereId, ApplicationDbContext? context = null)
    {
        if (context != null)
        {
            var maxOrder = await context.SectionsConteneurs
                .Where(sc => sc.DocumentGenereId == documentGenereId)
                .MaxAsync(sc => (int?)sc.Ordre).ConfigureAwait(false) ?? 0;
            return maxOrder + 1;
        }

        using var localContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var maxOrderLocal = await localContext.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre).ConfigureAwait(false) ?? 0;
        return maxOrderLocal + 1;
    }

    /// <summary>
    /// Calcule le prochain ordre disponible pour tous les éléments d'un document
    /// </summary>
    /// <param name="documentGenereId">Identifiant du document</param>
    /// <param name="context">Contexte EF optionnel pour réutiliser une transaction</param>
    /// <returns>Numéro d'ordre suivant global</returns>
    private async Task<int> GetNextOrderForDocument(int documentGenereId, ApplicationDbContext? context = null)
    {
        if (context != null)
        {
            var maxSectionOrder = await context.SectionsConteneurs
                .Where(sc => sc.DocumentGenereId == documentGenereId)
                .MaxAsync(sc => (int?)sc.Ordre).ConfigureAwait(false) ?? 0;

            var ftOrder = await context.FTConteneurs
                .Where(ftc => ftc.DocumentGenereId == documentGenereId)
                .MaxAsync(ftc => (int?)ftc.Ordre).ConfigureAwait(false) ?? 0;

            return Math.Max(maxSectionOrder, ftOrder) + 1;
        }

        using var localContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var maxSectionOrderLocal = await localContext.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre).ConfigureAwait(false) ?? 0;

        var ftOrderLocal = await localContext.FTConteneurs
            .Where(ftc => ftc.DocumentGenereId == documentGenereId)
            .MaxAsync(ftc => (int?)ftc.Ordre).ConfigureAwait(false) ?? 0;

        return Math.Max(maxSectionOrderLocal, ftOrderLocal) + 1;
    }
    
    /// <summary>
    /// Récupère tous les documents en cours de création triés par date de création décroissante
    /// </summary>
    /// <returns>Liste des documents en cours avec leurs chantiers</returns>
    public async Task<List<DocumentGenere>> GetAllDocumentsEnCoursAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.DocumentsGeneres
            .Where(d => d.EnCours)
            .Include(d => d.Chantier)
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync().ConfigureAwait(false);
    }   
}