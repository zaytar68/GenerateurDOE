using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Markdig;
using System.Text;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class DocumentGenereService : IDocumentGenereService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentRepositoryService _documentRepository;
    private readonly IDocumentExportService _documentExport;
    private readonly AppSettings _appSettings;
    private readonly IFicheTechniqueService _ficheTechniqueService;
    private readonly IMemoireTechniqueService _memoireTechniqueService;
    private readonly IPdfGenerationService _pdfGenerationService;

    public DocumentGenereService(ApplicationDbContext context, IDocumentRepositoryService documentRepository, 
        IDocumentExportService documentExport, IOptions<AppSettings> appSettings, IFicheTechniqueService ficheTechniqueService, 
        IMemoireTechniqueService memoireTechniqueService, IPdfGenerationService pdfGenerationService)
    {
        _context = context;
        _documentRepository = documentRepository;
        _documentExport = documentExport;
        _appSettings = appSettings.Value;
        _ficheTechniqueService = ficheTechniqueService;
        _memoireTechniqueService = memoireTechniqueService;
        _pdfGenerationService = pdfGenerationService;
    }

    public async Task<string> ExportDocumentAsync(int documentGenereId, FormatExport format)
    {
        var documentContent = await GenerateContentAsync(documentGenereId);
        return await _documentExport.ExportContentAsync(documentContent, format);
    }

    public async Task<string> GenerateContentAsync(int documentGenereId)
    {
        var document = await _documentRepository.GetWithCompleteContentAsync(documentGenereId);

        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var content = new StringBuilder();

        if (document.IncludePageDeGarde)
        {
            var typeDocumentTitle = GetDocumentTypeTitle(document.TypeDocument);
            content.AppendLine(GeneratePageDeGarde(document.Chantier!, typeDocumentTitle));
            content.AppendLine();
        }

        var allSections = document.SectionsConteneurs
            .Cast<IDocumentSection>()
            .Concat(document.FTConteneur != null ? new[] { document.FTConteneur } : Array.Empty<IDocumentSection>())
            .OrderBy(s => s.Ordre)
            .ToList();

        if (document.IncludeTableMatieres && allSections.Any())
        {
            content.AppendLine(GenerateTableMatieres(allSections));
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

    public async Task<DocumentGenere> SaveDocumentGenereAsync(DocumentGenere documentGenere)
    {
        return await _documentRepository.CreateAsync(documentGenere);
    }

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
            IncludeTableMatieres = s.IncludeTableMatieres
        });
    }

    public async Task<DocumentGenere> GetByIdAsync(int documentGenereId)
    {
        return await _documentRepository.GetByIdAsync(documentGenereId);
    }

    public async Task<DocumentGenere> UpdateAsync(DocumentGenere documentGenere)
    {
        return await _documentRepository.UpdateAsync(documentGenere);
    }

    public async Task<DocumentGenere> DuplicateAsync(int documentId, string newName)
    {
        return await _documentRepository.DuplicateAsync(documentId, newName);
    }

    public async Task<bool> DeleteDocumentGenereAsync(int documentGenereId)
    {
        return await _documentRepository.DeleteAsync(documentGenereId);
    }

    private string GeneratePageDeGarde(Chantier chantier, string typeDocument)
    {
        var pageDeGarde = new StringBuilder();

        pageDeGarde.AppendLine($"# {typeDocument}");
        pageDeGarde.AppendLine();
        pageDeGarde.AppendLine($"**Projet :** {chantier.NomProjet}");
        pageDeGarde.AppendLine($"**Maître d'œuvre :** {chantier.MaitreOeuvre}");
        pageDeGarde.AppendLine($"**Maître d'ouvrage :** {chantier.MaitreOuvrage}");
        pageDeGarde.AppendLine($"**Adresse :** {chantier.Adresse}");
        pageDeGarde.AppendLine($"**Lot :** {chantier.NumeroLot} - {chantier.IntituleLot}");
        pageDeGarde.AppendLine();
        pageDeGarde.AppendLine($"**Société :** {_appSettings.NomSociete}");
        pageDeGarde.AppendLine($"**Date :** {DateTime.Now:dd/MM/yyyy}");

        return pageDeGarde.ToString();
    }

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

    private string GenerateTableMatieres(IEnumerable<IDocumentSection> sections)
    {
        var tableDesMatieres = new StringBuilder();
        tableDesMatieres.AppendLine("## Table des Matières");
        tableDesMatieres.AppendLine();

        foreach (var section in sections)
        {
            tableDesMatieres.AppendLine($"- {section.Titre}");
        }

        return tableDesMatieres.ToString();
    }

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

    private async Task<string> ConvertToWordAsync(string content)
    {
        await Task.Delay(10);
        return $"[WORD] Simulation - Le contenu sera converti en Word :\n{content}";
    }

    public async Task<SectionConteneur> CreateSectionConteneurAsync(int documentGenereId, int typeSectionId, string? titre = null)
    {
        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var typeSection = await _context.TypesSections.FindAsync(typeSectionId);
        if (typeSection == null)
            throw new ArgumentException("Type de section non trouvé", nameof(typeSectionId));

        var existingConteneur = await _context.SectionsConteneurs
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId);

        if (existingConteneur != null)
            throw new InvalidOperationException($"Un conteneur pour le type de section '{typeSection.Nom}' existe déjà pour ce document");

        var sectionConteneur = new SectionConteneur
        {
            DocumentGenereId = documentGenereId,
            TypeSectionId = typeSectionId,
            Titre = titre ?? typeSection.Nom,
            Ordre = await GetNextOrderForSectionConteneur(documentGenereId)
        };

        _context.SectionsConteneurs.Add(sectionConteneur);
        await _context.SaveChangesAsync();
        return sectionConteneur;
    }

    public async Task<SectionConteneur> GetSectionConteneurAsync(int documentGenereId, int typeSectionId)
    {
        var sectionConteneur = await _context.SectionsConteneurs
            .Include(sc => sc.Items)
            .Include(sc => sc.TypeSection)
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId);

        if (sectionConteneur == null)
            throw new ArgumentException("Conteneur de section non trouvé");

        return sectionConteneur;
    }

    public async Task<IEnumerable<SectionConteneur>> GetSectionsConteneursByDocumentAsync(int documentGenereId)
    {
        return await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .Include(sc => sc.Items)
            .Include(sc => sc.TypeSection)
            .OrderBy(sc => sc.Ordre)
            .ToListAsync();
    }

    public async Task<bool> DeleteSectionConteneurAsync(int sectionConteneurId)
    {
        var sectionConteneur = await _context.SectionsConteneurs.FindAsync(sectionConteneurId);
        if (sectionConteneur == null)
            return false;

        _context.SectionsConteneurs.Remove(sectionConteneur);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FTConteneur> CreateFTConteneurAsync(int documentGenereId, string? titre = null)
    {
        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            throw new ArgumentException("Document généré non trouvé", nameof(documentGenereId));

        var existingFTConteneur = await _context.FTConteneurs
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId);

        if (existingFTConteneur != null)
            throw new InvalidOperationException("Un conteneur de fiches techniques existe déjà pour ce document");

        var ftConteneur = new FTConteneur
        {
            DocumentGenereId = documentGenereId,
            Titre = titre ?? "Fiches Techniques",
            Ordre = await GetNextOrderForDocument(documentGenereId)
        };

        _context.FTConteneurs.Add(ftConteneur);
        await _context.SaveChangesAsync();
        return ftConteneur;
    }

    public async Task<FTConteneur?> GetFTConteneurByDocumentAsync(int documentGenereId)
    {
        return await _context.FTConteneurs
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF)
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId);
    }

    public async Task<FTConteneur> UpdateFTConteneurAsync(FTConteneur ftConteneur)
    {
        _context.FTConteneurs.Update(ftConteneur);
        await _context.SaveChangesAsync();
        return ftConteneur;
    }

    public async Task<bool> DeleteFTConteneurAsync(int ftConteneursId)
    {
        var ftConteneur = await _context.FTConteneurs.FindAsync(ftConteneursId);
        if (ftConteneur == null)
            return false;

        _context.FTConteneurs.Remove(ftConteneur);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DocumentGenere> FinalizeDocumentAsync(int documentGenereId)
    {
        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        if (!await CanFinalizeDocumentAsync(documentGenereId))
            throw new InvalidOperationException("Le document ne peut pas être finalisé dans son état actuel");

        document.EnCours = false;
        await _context.SaveChangesAsync();
        return document;
    }

    /// <summary>
    /// Génère un PDF complet avec toutes les sections du document
    /// </summary>
    public async Task<byte[]> GenerateCompletePdfAsync(int documentGenereId, PdfGenerationOptions? options = null)
    {
        var document = await _context.DocumentsGeneres
            .Include(d => d.Chantier)
            .Include(d => d.SectionsConteneurs)
                .ThenInclude(sc => sc.Items)
            .Include(d => d.FTConteneur)
                .ThenInclude(ftc => ftc!.Elements)
                    .ThenInclude(fte => fte.FicheTechnique)
            .Include(d => d.FTConteneur)
                .ThenInclude(ftc => ftc!.Elements)
                    .ThenInclude(fte => fte.ImportPDF)
            .FirstOrDefaultAsync(d => d.Id == documentGenereId);

        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        return await _pdfGenerationService.GenerateCompletePdfAsync(document, options);
    }

    /// <summary>
    /// Sauvegarde un PDF généré sur le système de fichiers
    /// </summary>
    public async Task<string> SavePdfAsync(byte[] pdfBytes, string fileName)
    {
        var fullPath = Path.Combine(_appSettings.RepertoireStockagePDF, fileName);
        await File.WriteAllBytesAsync(fullPath, pdfBytes);
        return fullPath;
    }

    public async Task<bool> CanFinalizeDocumentAsync(int documentGenereId)
    {
        var document = await _context.DocumentsGeneres
            .Include(d => d.SectionsConteneurs)
                .ThenInclude(sc => sc.Items)
            .Include(d => d.FTConteneur)
                .ThenInclude(ftc => ftc!.Elements)
            .FirstOrDefaultAsync(d => d.Id == documentGenereId);

        if (document == null)
            return false;

        bool hasContent = document.SectionsConteneurs.Any(sc => sc.Items.Any()) ||
                         (document.FTConteneur?.Elements.Any() == true);

        return hasContent;
    }

    private async Task<int> GetNextOrderForSectionConteneur(int documentGenereId)
    {
        var maxOrder = await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre) ?? 0;
        return maxOrder + 1;
    }

    private async Task<int> GetNextOrderForDocument(int documentGenereId)
    {
        var maxSectionOrder = await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre) ?? 0;

        var ftOrder = await _context.FTConteneurs
            .Where(ftc => ftc.DocumentGenereId == documentGenereId)
            .MaxAsync(ftc => (int?)ftc.Ordre) ?? 0;

        return Math.Max(maxSectionOrder, ftOrder) + 1;
    }
    
    public async Task<List<DocumentGenere>> GetAllDocumentsEnCoursAsync()
    {
        return await _context.DocumentsGeneres
            .Where(d => d.EnCours)
            .Include(d => d.Chantier)
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync();
    }   
}