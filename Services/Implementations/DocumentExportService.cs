using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Markdig;
using System.Text;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class DocumentExportService : IDocumentExportService
{
    private readonly ApplicationDbContext _context;
    private readonly AppSettings _appSettings;
    private readonly IFicheTechniqueService _ficheTechniqueService;
    private readonly IMemoireTechniqueService _memoireTechniqueService;

    public DocumentExportService(ApplicationDbContext context, IOptions<AppSettings> appSettings,
        IFicheTechniqueService ficheTechniqueService, IMemoireTechniqueService memoireTechniqueService)
    {
        _context = context;
        _appSettings = appSettings.Value;
        _ficheTechniqueService = ficheTechniqueService;
        _memoireTechniqueService = memoireTechniqueService;
    }

    public async Task<string> ExportDocumentAsync(int chantierId, TypeDocumentExport typeDocument, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        return typeDocument switch
        {
            TypeDocumentExport.DOE => await GenerateDOEAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            TypeDocumentExport.DossierTechnique => await GenerateDossierTechniqueAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            TypeDocumentExport.MemoireTechnique => await GenerateMemoireTechniqueAsync(chantierId, format, includePageDeGarde, includeTableMatieres),
            _ => throw new ArgumentException("Type de document non supporté", nameof(typeDocument))
        };
    }

    public async Task<string> GenerateDOEAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true)
    {
        var chantier = await _context.Chantiers.FindAsync(chantierId);
        if (chantier == null)
            throw new ArgumentException("Chantier non trouvé", nameof(chantierId));

        var fichesTechniques = await _ficheTechniqueService.GetByChantierId(chantierId);

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

        var fichesTechniques = await _ficheTechniqueService.GetByChantierId(chantierId);

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

    public async Task<DocumentExport> SaveDocumentExportAsync(DocumentExport documentExport)
    {
        documentExport.DateCreation = DateTime.Now;

        _context.DocumentsExport.Add(documentExport);
        await _context.SaveChangesAsync();
        return documentExport;
    }

    public async Task<IEnumerable<DocumentExport>> GetDocumentExportsByChantierId(int chantierId)
    {
        return await _context.DocumentsExport
            .Where(d => d.ChantierId == chantierId)
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync();
    }

    public async Task<bool> DeleteDocumentExportAsync(int documentExportId)
    {
        var document = await _context.DocumentsExport.FindAsync(documentExportId);
        if (document == null)
            return false;

        if (!string.IsNullOrEmpty(document.CheminFichier) && File.Exists(document.CheminFichier))
        {
            File.Delete(document.CheminFichier);
        }

        _context.DocumentsExport.Remove(document);
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
}