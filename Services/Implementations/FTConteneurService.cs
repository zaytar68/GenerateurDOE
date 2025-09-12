using Microsoft.EntityFrameworkCore;
using System.Text;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class FTConteneurService : IFTConteneurService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggingService _loggingService;

    public FTConteneurService(ApplicationDbContext context, ILoggingService loggingService)
    {
        _context = context;
        _loggingService = loggingService;
    }

    public async Task<FTConteneur> CreateAsync(int documentGenereId, string? titre = null)
    {
        if (!await CanCreateForDocumentAsync(documentGenereId))
            throw new InvalidOperationException("Un conteneur de fiches techniques existe déjà pour ce document");

        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        if (document == null)
            throw new ArgumentException("Document non trouvé", nameof(documentGenereId));

        var maxOrder = await GetMaxOrderForDocument(documentGenereId);

        var ftConteneur = new FTConteneur
        {
            DocumentGenereId = documentGenereId,
            Titre = titre ?? "Fiches Techniques",
            Ordre = maxOrder + 1
        };

        _context.FTConteneurs.Add(ftConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur créé : {ftConteneur.Titre} pour document {documentGenereId}");
        return ftConteneur;
    }

    public async Task<FTConteneur> GetByIdAsync(int id)
    {
        var ftConteneur = await _context.FTConteneurs
            .Include(ftc => ftc.Elements.OrderBy(fte => fte.Ordre))
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF)
            .Include(ftc => ftc.DocumentGenere)
            .FirstOrDefaultAsync(ftc => ftc.Id == id);

        if (ftConteneur == null)
            throw new ArgumentException("FTConteneur non trouvé", nameof(id));

        return ftConteneur;
    }

    public async Task<FTConteneur?> GetByDocumentIdAsync(int documentGenereId)
    {
        return await _context.FTConteneurs
            .Include(ftc => ftc.Elements.OrderBy(fte => fte.Ordre))
                .ThenInclude(fte => fte.FicheTechnique)
            .Include(ftc => ftc.Elements)
                .ThenInclude(fte => fte.ImportPDF)
            .FirstOrDefaultAsync(ftc => ftc.DocumentGenereId == documentGenereId);
    }

    public async Task<FTConteneur> UpdateAsync(FTConteneur ftConteneur)
    {
        ftConteneur.DateModification = DateTime.Now;
        _context.FTConteneurs.Update(ftConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur mis à jour : {ftConteneur.Titre}");
        return ftConteneur;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ftConteneur = await _context.FTConteneurs.FindAsync(id);
        if (ftConteneur == null)
            return false;

        _context.FTConteneurs.Remove(ftConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"FTConteneur supprimé : ID {id}");
        return true;
    }

    public async Task<FTElement> AddFTElementAsync(int ftConteneursId, int ficheTechniqueId, string positionMarche, int? importPDFId = null)
    {
        var ftConteneur = await _context.FTConteneurs.FindAsync(ftConteneursId);
        var ficheTechnique = await _context.FichesTechniques.FindAsync(ficheTechniqueId);

        if (ftConteneur == null || ficheTechnique == null)
            throw new ArgumentException("FTConteneur ou FicheTechnique non trouvé");

        if (importPDFId.HasValue)
        {
            var importPDF = await _context.ImportsPDF.FindAsync(importPDFId.Value);
            if (importPDF == null || importPDF.FicheTechniqueId != ficheTechniqueId)
                throw new ArgumentException("ImportPDF non valide pour cette fiche technique");
        }

        var maxOrder = await _context.FTElements
            .Where(fte => fte.FTConteneursId == ftConteneursId)
            .MaxAsync(fte => (int?)fte.Ordre) ?? 0;

        var ftElement = new FTElement
        {
            FTConteneursId = ftConteneursId,
            FicheTechniqueId = ficheTechniqueId,
            ImportPDFId = importPDFId,
            PositionMarche = positionMarche,
            Ordre = maxOrder + 1
        };

        _context.FTElements.Add(ftElement);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"FTElement ajouté : {positionMarche} - {ficheTechnique.NomProduit}");
        return ftElement;
    }

    public async Task<bool> RemoveFTElementAsync(int ftElementId)
    {
        var ftElement = await _context.FTElements.FindAsync(ftElementId);
        if (ftElement == null)
            return false;

        _context.FTElements.Remove(ftElement);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"FTElement supprimé : ID {ftElementId}");
        return true;
    }

    public async Task<FTConteneur> ReorderFTElementsAsync(int ftConteneursId, List<int> ftElementIds)
    {
        for (int i = 0; i < ftElementIds.Count; i++)
        {
            var ftElement = await _context.FTElements.FindAsync(ftElementIds[i]);
            if (ftElement != null && ftElement.FTConteneursId == ftConteneursId)
            {
                ftElement.Ordre = i + 1;
            }
        }

        await _context.SaveChangesAsync();
        _loggingService.LogInformation($"Ordre des FTElements réorganisé pour le conteneur {ftConteneursId}");
        
        return await GetByIdAsync(ftConteneursId);
    }

    public async Task<FTElement> UpdateFTElementAsync(FTElement ftElement)
    {
        _context.FTElements.Update(ftElement);
        await _context.SaveChangesAsync();

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

        await _context.SaveChangesAsync();
        _loggingService.LogInformation($"Numéros de pages calculés pour le conteneur {ftConteneursId}");
        
        return ftConteneur;
    }

    public async Task<bool> CanCreateForDocumentAsync(int documentGenereId)
    {
        var existing = await _context.FTConteneurs
            .AnyAsync(ftc => ftc.DocumentGenereId == documentGenereId);
        return !existing;
    }

    public async Task<IEnumerable<FicheTechnique>> GetAvailableFichesTechniquesAsync(int documentGenereId)
    {
        return await _context.FichesTechniques
            .Include(ft => ft.ImportsPDF)
            .Where(ft => ft.ImportsPDF.Any())
            .ToListAsync();
    }

    private async Task<int> GetMaxOrderForDocument(int documentGenereId)
    {
        var maxSectionOrder = await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre) ?? 0;

        var maxFTOrder = await _context.FTConteneurs
            .Where(ftc => ftc.DocumentGenereId == documentGenereId)
            .MaxAsync(ftc => (int?)ftc.Ordre) ?? 0;

        return Math.Max(maxSectionOrder, maxFTOrder);
    }

    private async Task<IEnumerable<string>> GetTypesDocumentsForFicheTechnique(int ficheTechniqueId)
    {
        var types = await _context.ImportsPDF
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