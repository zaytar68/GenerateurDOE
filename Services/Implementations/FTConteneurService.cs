using Microsoft.EntityFrameworkCore;
using System.Text;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class FTConteneurService : IFTConteneurService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILoggingService _loggingService;

    public FTConteneurService(IDbContextFactory<ApplicationDbContext> contextFactory, ILoggingService loggingService)
    {
        _contextFactory = contextFactory;
        _loggingService = loggingService;
    }

    public async Task<FTConteneur> CreateAsync(int documentGenereId, string? titre = null)
    {
        using var context = _contextFactory.CreateDbContext();

        if (!await CanCreateForDocumentAsync(documentGenereId))
            throw new InvalidOperationException("Un conteneur de fiches techniques existe déjà pour ce document");

        var document = await context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        var maxOrder = await GetMaxOrderForDocument(documentGenereId);

        var ftConteneur = new FTConteneur
        {
            DocumentGenereId = documentGenereId,
            Titre = titre ?? "Fiches Techniques",
            Ordre = maxOrder + 1
        };

        context.FTConteneurs.Add(ftConteneur);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur créé : {ftConteneur.Titre} pour document {documentGenereId}");
        return ftConteneur;
    }

    public async Task<FTConteneur> GetByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();

        // 🔧 CORRECTION CONCURRENCE: DbContext isolé + Single Include chain pour éviter conflits
        var ftConteneur = await context.FTConteneurs
            .Include(ftc => ftc.Elements.OrderBy(fte => fte.Ordre))
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF)
                    .ThenInclude(ip => ip!.TypeDocumentImport)
            .Include(ftc => ftc.DocumentGenere)
            .AsSingleQuery()  // ✅ Forcer single query pour éviter split concurrentiel
            .FirstOrDefaultAsync(ftc => ftc.Id == id);

        if (ftConteneur == null)
            throw new ArgumentException("FTConteneur non trouvé", nameof(id));

        return ftConteneur;
    }

    public async Task<FTConteneur?> GetByDocumentIdAsync(int documentGenereId)
    {
        using var context = _contextFactory.CreateDbContext();

        // 🔧 CORRECTION CONCURRENCE: DbContext isolé + même pattern sécurisé que GetByIdAsync
        return await context.FTConteneurs
            .Include(ftc => ftc.Elements.OrderBy(fte => fte.Ordre))
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF)
                    .ThenInclude(ip => ip!.TypeDocumentImport)
            .AsSingleQuery()  // ✅ Single query pour éviter concurrence
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId);
    }

    public async Task<FTConteneur> UpdateAsync(FTConteneur ftConteneur)
    {
        ftConteneur.DateModification = DateTime.Now;
        using var context = _contextFactory.CreateDbContext();

        context.FTConteneurs.Update(ftConteneur);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur mis à jour : {ftConteneur.Titre}");
        return ftConteneur;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();

        var ftConteneur = await context.FTConteneurs.FindAsync(id);
        if (ftConteneur == null)
            return false;

        context.FTConteneurs.Remove(ftConteneur);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur supprimé : ID {id}");
        return true;
    }

    public async Task<FTElement> AddFTElementAsync(int ftConteneursId, int ficheTechniqueId, string? positionMarche, int? importPDFId = null, string? commentaire = null)
    {
        using var context = _contextFactory.CreateDbContext();

        var ftConteneur = await context.FTConteneurs.FindAsync(ftConteneursId);
        var ficheTechnique = await context.FichesTechniques.FindAsync(ficheTechniqueId);

        if (ftConteneur == null || ficheTechnique == null)
            throw new ArgumentException("FTConteneur ou FicheTechnique non trouvé");

        if (importPDFId.HasValue)
        {
            var importPDF = await context.ImportsPDF.FindAsync(importPDFId.Value);
            if (importPDF == null || importPDF.FicheTechniqueId != ficheTechniqueId)
                throw new ArgumentException("ImportPDF non valide pour cette fiche technique");
        }

        var maxOrder = await context.FTElements
            .Where(fte => fte.FTConteneursId == ftConteneursId)
            .MaxAsync(fte => (int?)fte.Ordre) ?? 0;

        var ftElement = new FTElement
        {
            FTConteneursId = ftConteneursId,
            FicheTechniqueId = ficheTechniqueId,
            ImportPDFId = importPDFId,
            PositionMarche = positionMarche,
            Commentaire = commentaire,
            Ordre = maxOrder + 1
        };

        context.FTElements.Add(ftElement);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTElement ajouté : {positionMarche} - {ficheTechnique.NomProduit}");
        return ftElement;
    }

    public async Task<bool> RemoveFTElementAsync(int ftElementId)
    {
        using var context = _contextFactory.CreateDbContext();

        var ftElement = await context.FTElements.FindAsync(ftElementId);
        if (ftElement == null)
            return false;

        context.FTElements.Remove(ftElement);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTElement supprimé : ID {ftElementId}");
        return true;
    }

    public async Task<FTConteneur> ReorderFTElementsAsync(int ftConteneursId, List<int> ftElementIds)
    {
        using var context = _contextFactory.CreateDbContext();

        for (int i = 0; i < ftElementIds.Count; i++)
        {
            var ftElement = await context.FTElements.FindAsync(ftElementIds[i]);
            if (ftElement != null && ftElement.FTConteneursId == ftConteneursId)
            {
                ftElement.Ordre = i + 1;
            }
        }

        await context.SaveChangesAsync();
        _loggingService.LogInformation($"Ordre des FTElements réorganisé pour le conteneur {ftConteneursId}");
        
        return await GetByIdAsync(ftConteneursId);
    }

    public async Task<FTElement> UpdateFTElementAsync(FTElement ftElement)
    {
        using var context = _contextFactory.CreateDbContext();

        context.FTElements.Update(ftElement);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"FTElement mis à jour : {ftElement.PositionMarche}");
        return ftElement;
    }

    public async Task<string> GenerateTableauRecapitulatifHtmlAsync(int ftConteneursId)
    {
        var ftConteneur = await GetByIdAsync(ftConteneursId);
        
        var html = new StringBuilder();
        html.AppendLine("<div class='tableau-recapitulatif'>");
        html.AppendLine($"<h2>{ftConteneur.Titre} - Tableau Récapitulatif</h2>");
        html.AppendLine("<table class='table table-bordered table-striped'>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>Position Marché</th>");
        html.AppendLine("<th>Marque</th>");
        html.AppendLine("<th>Nom du Produit</th>");
        html.AppendLine("<th>Type de Produit</th>");
        html.AppendLine("<th>Types de Documents</th>");
        html.AppendLine("<th>Page</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");

        foreach (var element in ftConteneur.Elements.OrderBy(e => e.Ordre))
        {
            var typesDocuments = await GetTypesDocumentsForFicheTechnique(element.FicheTechniqueId);
            
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{element.PositionMarche}</td>");
            html.AppendLine($"<td>{element.FicheTechnique.NomFabricant}</td>");
            html.AppendLine($"<td>{element.FicheTechnique.NomProduit}</td>");
            html.AppendLine($"<td>{element.FicheTechnique.TypeProduit}</td>");
            html.AppendLine($"<td>{string.Join(", ", typesDocuments)}</td>");
            html.AppendLine($"<td>{element.NumeroPage?.ToString() ?? "N/A"}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        html.AppendLine("</div>");

        return html.ToString();
    }

    public async Task<FTConteneur> CalculatePageNumbersAsync(int ftConteneursId)
    {
        using var context = _contextFactory.CreateDbContext();

        var ftConteneur = await GetByIdAsync(ftConteneursId);
        
        int currentPage = 1;
        foreach (var element in ftConteneur.Elements.OrderBy(e => e.Ordre))
        {
            element.NumeroPage = currentPage;
            
            if (element.ImportPDF != null)
            {
                int pdfPages = await EstimatePdfPagesAsync(element.ImportPDF.CheminFichier);
                currentPage += pdfPages;
            }
            else
            {
                currentPage += 1;
            }
        }

        await context.SaveChangesAsync();
        _loggingService.LogInformation($"Numéros de pages calculés pour le conteneur {ftConteneursId}");
        
        return ftConteneur;
    }

    public async Task<bool> CanCreateForDocumentAsync(int documentGenereId)
    {
        using var context = _contextFactory.CreateDbContext();

        var existing = await context.FTConteneurs
            .AnyAsync(ftc => ftc.DocumentGenereId == documentGenereId);
        return !existing;
    }

    public async Task<IEnumerable<FicheTechnique>> GetAvailableFichesTechniquesAsync(int documentGenereId)
    {
        using var context = _contextFactory.CreateDbContext();

        // 🔧 CORRECTION CONCURRENCE: DbContext isolé pour cette requête critique
        return await context.FichesTechniques
            .Include(ft => ft.ImportsPDF)
                .ThenInclude(ip => ip.TypeDocumentImport)
            .Where(ft => ft.ImportsPDF.Any())
            .AsSingleQuery()  // ✅ Single query pour éviter split query concurrentiel
            .ToListAsync()
            .ConfigureAwait(false);
    }

    private async Task<int> GetMaxOrderForDocument(int documentGenereId)
    {
        using var context = _contextFactory.CreateDbContext();

        var maxSectionOrder = await context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre) ?? 0;

        var maxFTOrder = await context.FTConteneurs
            .Where(ftc => ftc.DocumentGenereId == documentGenereId)
            .MaxAsync(ftc => (int?)ftc.Ordre) ?? 0;

        return Math.Max(maxSectionOrder, maxFTOrder);
    }

    private async Task<IEnumerable<string>> GetTypesDocumentsForFicheTechnique(int ficheTechniqueId)
    {
        using var context = _contextFactory.CreateDbContext();

        var types = await context.ImportsPDF
            .Where(ip => ip.FicheTechniqueId == ficheTechniqueId)
            // TODO(human): Remplacez ip.TypeDocument par ip.TypeDocumentImport?.Nom ?? "Non défini"
            .Select(ip => ip.TypeDocumentImport != null ? ip.TypeDocumentImport.Nom : "Non défini")
            .Distinct()
            .ToListAsync();

        return types;
    }

    private async Task<int> EstimatePdfPagesAsync(string cheminFichier)
    {
        await Task.Delay(10);
        return 2;
    }
}