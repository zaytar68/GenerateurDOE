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
    private readonly AppSettings _appSettings;
    private readonly IFicheTechniqueService _ficheTechniqueService;
    private readonly IMemoireTechniqueService _memoireTechniqueService;

    public DocumentGenereService(ApplicationDbContext context, IOptions<AppSettings> appSettings,
        IFicheTechniqueService ficheTechniqueService, IMemoireTechniqueService memoireTechniqueService)
    {
        _context = context;
        _appSettings = appSettings.Value;
        _ficheTechniqueService = ficheTechniqueService;
        _memoireTechniqueService = memoireTechniqueService;
    }

    public async Task<string> ExportDocumentAsync(int chantierId, TypeDocumentGenere typeDocument, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        return typeDocument switch
        {
            TypeDocumentGenere.DOE => await GenerateDOEAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            TypeDocumentGenere.DossierTechnique => await GenerateDossierTechniqueAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            TypeDocumentGenere.MemoireTechnique => await GenerateMemoireTechniqueAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            _ => throw new ArgumentException("Type de document non supporté", nameof(typeDocument))
        };
    }

    public async Task<string> GenerateDOEAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        var chantier = await _context.Chantiers.FindAsync(chantierId);
        if (chantier == null)
            throw new ArgumentException("Chantier non trouvé", nameof(chantierId));

        var fichesTechniques = await _ficheTechniqueService.GetAllAsync();

        var content = new StringBuilder();

        if (includePageDeGarde)
        {
            content.AppendLine(GeneratePageDeGarde(chantier, "Dossier d'Ouvrages Exécutés"));
            content.AppendLine();
        }

        if (includeTableMatieres)
        {
            content.AppendLine(GenerateTableMatieres(fichesTechniques));
            content.AppendLine();
        }

        content.AppendLine("# Fiches Techniques");
        content.AppendLine();

        foreach (var fiche in fichesTechniques)
        {
            content.AppendLine($"## {fiche.NomProduit}");
            content.AppendLine($"**Fabricant :** {fiche.NomFabricant}");
            content.AppendLine($"**Type :** {fiche.TypeProduit}");
            
            if (!string.IsNullOrEmpty(fiche.Description))
            {
                content.AppendLine($"**Description :** {fiche.Description}");
            }

            if (fiche.ImportsPDF.Any())
            {
                content.AppendLine("**Documents associés :**");
                foreach (var pdf in fiche.ImportsPDF)
                {
                    content.AppendLine($"- {pdf.NomFichierOriginal} ({pdf.TypeDocument})");
                }
            }

            content.AppendLine();
        }

        return await FormatContentAsync(content.ToString(), format);
    }

    public async Task<string> GenerateDossierTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        var chantier = await _context.Chantiers.FindAsync(chantierId);
        if (chantier == null)
            throw new ArgumentException("Chantier non trouvé", nameof(chantierId));

        var fichesTechniques = await _ficheTechniqueService.GetAllAsync();

        var content = new StringBuilder();

        if (includePageDeGarde)
        {
            content.AppendLine(GeneratePageDeGarde(chantier, "Dossier Technique"));
            content.AppendLine();
        }

        if (includeTableMatieres)
        {
            content.AppendLine(GenerateTableMatieres(fichesTechniques));
            content.AppendLine();
        }

        content.AppendLine("# Matériaux Prévus");
        content.AppendLine();

        foreach (var fiche in fichesTechniques.GroupBy(f => f.TypeProduit))
        {
            content.AppendLine($"## {fiche.Key}");
            content.AppendLine();

            foreach (var produit in fiche)
            {
                content.AppendLine($"### {produit.NomProduit}");
                content.AppendLine($"**Fabricant :** {produit.NomFabricant}");
                
                if (!string.IsNullOrEmpty(produit.Description))
                {
                    content.AppendLine($"**Description :** {produit.Description}");
                }

                content.AppendLine();
            }
        }

        return await FormatContentAsync(content.ToString(), format);
    }

    public async Task<string> GenerateMemoireTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        var chantier = await _context.Chantiers.FindAsync(chantierId);
        if (chantier == null)
            throw new ArgumentException("Chantier non trouvé", nameof(chantierId));

        var methodes = await _memoireTechniqueService.GetMethodesOrderedAsync();

        var content = new StringBuilder();

        if (includePageDeGarde)
        {
            content.AppendLine(GeneratePageDeGarde(chantier, "Mémoire Technique"));
            content.AppendLine();
        }

        content.AppendLine($"# Présentation de {_appSettings.NomSociete}");
        content.AppendLine();
        content.AppendLine("## Notre Expertise");
        content.AppendLine();

        if (includeTableMatieres && methodes.Any())
        {
            content.AppendLine("## Table des Matières - Méthodologies");
            content.AppendLine();
            foreach (var methode in methodes)
            {
                content.AppendLine($"- {methode.Titre}");
            }
            content.AppendLine();
        }

        content.AppendLine("# Méthodologies");
        content.AppendLine();

        foreach (var methode in methodes)
        {
            content.AppendLine($"## {methode.Titre}");
            content.AppendLine();
            content.AppendLine(methode.Description);
            content.AppendLine();

            if (methode.Images.Any())
            {
                content.AppendLine("**Illustrations :**");
                foreach (var image in methode.Images.OrderBy(i => i.OrdreAffichage))
                {
                    content.AppendLine($"![{image.Description ?? image.NomFichierOriginal}]({image.CheminFichier})");
                }
                content.AppendLine();
            }
        }

        return await FormatContentAsync(content.ToString(), format);
    }

    public async Task<DocumentGenere> SaveDocumentGenereAsync(DocumentGenere documentGenere)
    {
        documentGenere.DateCreation = DateTime.Now;

        _context.DocumentsGeneres.Add(documentGenere);
        await _context.SaveChangesAsync();
        return documentGenere;
    }

    public async Task<IEnumerable<DocumentGenere>> GetDocumentsGeneresByChantierId(int chantierId)
    {
        return await _context.DocumentsGeneres
            .Where(d => d.ChantierId == chantierId)
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync();
    }

    public async Task<DocumentGenere> GetByIdAsync(int documentGenereId)
    {
        var document = await _context.DocumentsGeneres
            .Include(d => d.Chantier)
            .Include(d => d.FichesTechniques)
            .FirstOrDefaultAsync(d => d.Id == documentGenereId);
        
        if (document == null)
            throw new InvalidOperationException($"Document avec l'ID {documentGenereId} introuvable");
            
        return document;
    }

    public async Task<DocumentGenere> UpdateAsync(DocumentGenere documentGenere)
    {
        var existingDocument = await _context.DocumentsGeneres.FindAsync(documentGenere.Id);
        if (existingDocument == null)
            throw new InvalidOperationException($"Document avec l'ID {documentGenere.Id} introuvable");

        // Mise à jour des propriétés
        existingDocument.TypeDocument = documentGenere.TypeDocument;
        existingDocument.FormatExport = documentGenere.FormatExport;
        existingDocument.NomFichier = documentGenere.NomFichier;
        existingDocument.IncludePageDeGarde = documentGenere.IncludePageDeGarde;
        existingDocument.IncludeTableMatieres = documentGenere.IncludeTableMatieres;
        existingDocument.Parametres = documentGenere.Parametres;

        await _context.SaveChangesAsync();
        return existingDocument;
    }

    public async Task<DocumentGenere> DuplicateAsync(int documentId, string newName)
    {
        var originalDocument = await GetByIdAsync(documentId);
        
        var duplicatedDocument = new DocumentGenere
        {
            TypeDocument = originalDocument.TypeDocument,
            FormatExport = originalDocument.FormatExport,
            NomFichier = newName,
            ChantierId = originalDocument.ChantierId,
            IncludePageDeGarde = originalDocument.IncludePageDeGarde,
            IncludeTableMatieres = originalDocument.IncludeTableMatieres,
            Parametres = originalDocument.Parametres,
            DateCreation = DateTime.Now
        };

        return await SaveDocumentGenereAsync(duplicatedDocument);
    }

    public async Task<bool> DeleteDocumentGenereAsync(int documentGenereId)
    {
        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            return false;

        if (!string.IsNullOrEmpty(document.CheminFichier) && File.Exists(document.CheminFichier))
        {
            File.Delete(document.CheminFichier);
        }

        _context.DocumentsGeneres.Remove(document);
        await _context.SaveChangesAsync();
        return true;
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

    private string GenerateTableMatieres(IEnumerable<FicheTechnique> fichesTechniques)
    {
        var tableDesMatieres = new StringBuilder();
        tableDesMatieres.AppendLine("## Table des Matières");
        tableDesMatieres.AppendLine();

        foreach (var fiche in fichesTechniques)
        {
            tableDesMatieres.AppendLine($"- {fiche.NomProduit} ({fiche.NomFabricant})");
        }

        return tableDesMatieres.ToString();
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
        await Task.Delay(10);
        return $"[PDF] Simulation - Le contenu sera converti en PDF :\n{content}";
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
            .Include(sc => sc.SectionsLibres)
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
            .Include(sc => sc.SectionsLibres)
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

    public async Task<bool> CanFinalizeDocumentAsync(int documentGenereId)
    {
        var document = await _context.DocumentsGeneres
            .Include(d => d.SectionsConteneurs)
                .ThenInclude(sc => sc.SectionsLibres)
            .Include(d => d.FTConteneur)
                .ThenInclude(ftc => ftc!.Elements)
            .FirstOrDefaultAsync(d => d.Id == documentGenereId);

        if (document == null)
            return false;

        bool hasContent = document.SectionsConteneurs.Any(sc => sc.SectionsLibres.Any()) ||
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
}