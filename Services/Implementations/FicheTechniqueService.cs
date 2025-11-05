using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service de gestion des fiches techniques avec gestion des PDFs associés
/// Gère les opérations CRUD et l'upload/suppression des documents techniques
/// </summary>
public class FicheTechniqueService : IFicheTechniqueService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AppSettings _appSettings;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initialise une nouvelle instance du service FicheTechniqueService
    /// </summary>
    /// <param name="contextFactory">Factory pour créer les contextes EF thread-safe</param>
    /// <param name="appSettings">Configuration de l'application</param>
    /// <param name="loggingService">Service de logging</param>
    public FicheTechniqueService(IDbContextFactory<ApplicationDbContext> contextFactory, IOptions<AppSettings> appSettings, ILoggingService loggingService)
    {
        _contextFactory = contextFactory;
        _appSettings = appSettings.Value;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Récupère toutes les fiches techniques avec leurs PDFs triées par nom de produit
    /// </summary>
    /// <returns>Liste des fiches techniques avec ImportsPDF chargés</returns>
    public async Task<IEnumerable<FicheTechnique>> GetAllAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .OrderBy(f => f.NomProduit)
            .ToListAsync();
    }

    /// <summary>
    /// Récupère une fiche technique par son identifiant avec ses PDFs
    /// </summary>
    /// <param name="id">Identifiant de la fiche technique</param>
    /// <returns>Fiche technique avec PDFs ou null si non trouvée</returns>
    public async Task<FicheTechnique?> GetByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .FirstOrDefaultAsync(f => f.Id == id);
    }


    /// <summary>
    /// Crée une nouvelle fiche technique avec horodatage automatique
    /// </summary>
    /// <param name="ficheTechnique">Fiche technique à créer</param>
    /// <returns>Fiche technique créée avec ID généré</returns>
    public async Task<FicheTechnique> CreateAsync(FicheTechnique ficheTechnique)
    {
        using var context = _contextFactory.CreateDbContext();
        ficheTechnique.DateCreation = DateTime.Now;
        ficheTechnique.DateModification = DateTime.Now;

        context.FichesTechniques.Add(ficheTechnique);
        await context.SaveChangesAsync();
        return ficheTechnique;
    }

    /// <summary>
    /// Met à jour une fiche technique existante avec tracking de modification
    /// </summary>
    /// <param name="ficheTechnique">Fiche technique avec modifications</param>
    /// <returns>Fiche technique mise à jour</returns>
    /// <exception cref="InvalidOperationException">Si la fiche n'existe pas</exception>
    public async Task<FicheTechnique> UpdateAsync(FicheTechnique ficheTechnique)
    {
        using var context = _contextFactory.CreateDbContext();
        var existingFiche = await context.FichesTechniques.FindAsync(ficheTechnique.Id);
        if (existingFiche == null)
        {
            throw new InvalidOperationException($"FicheTechnique avec l'ID {ficheTechnique.Id} n'existe pas.");
        }

        // Copier les propriétés modifiables
        existingFiche.NomProduit = ficheTechnique.NomProduit;
        existingFiche.NomFabricant = ficheTechnique.NomFabricant;
        existingFiche.TypeProduit = ficheTechnique.TypeProduit;
        existingFiche.Description = ficheTechnique.Description;
        existingFiche.DateModification = DateTime.Now;

        await context.SaveChangesAsync();
        return existingFiche;
    }

    /// <summary>
    /// Supprime une fiche technique et tous ses fichiers PDF associés
    /// </summary>
    /// <param name="id">Identifiant de la fiche à supprimer</param>
    /// <returns>True si suppression réussie, False si fiche inexistante</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var ficheTechnique = await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficheTechnique == null)
            return false;

        // 1. Supprimer d'abord tous les FTElements qui référencent cette fiche technique
        var ftElements = await context.FTElements
            .Where(fte => fte.FicheTechniqueId == id)
            .ToListAsync();

        if (ftElements.Any())
        {
            context.FTElements.RemoveRange(ftElements);
            _loggingService.LogInformation($"Suppression de {ftElements.Count} FTElement(s) référençant la fiche technique {id}");
        }

        // 2. Supprimer les fichiers PDF physiques
        foreach (var importPDF in ficheTechnique.ImportsPDF)
        {
            if (File.Exists(importPDF.CheminFichier))
            {
                File.Delete(importPDF.CheminFichier);
                _loggingService.LogInformation($"Fichier PDF supprimé : {importPDF.CheminFichier}");
            }
        }

        // 3. Supprimer la fiche technique (les ImportsPDF seront supprimés en cascade)
        context.FichesTechniques.Remove(ficheTechnique);
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"Fiche technique {id} supprimée avec succès");
        return true;
    }

    /// <summary>
    /// Ajoute un fichier PDF à une fiche technique existante
    /// </summary>
    /// <param name="ficheTechniqueId">Identifiant de la fiche technique</param>
    /// <param name="importPDF">PDF à associer avec horodatage automatique</param>
    /// <returns>ImportPDF créé avec ID généré</returns>
    public async Task<ImportPDF> AddPDFAsync(int ficheTechniqueId, ImportPDF importPDF)
    {
        using var context = _contextFactory.CreateDbContext();
        importPDF.FicheTechniqueId = ficheTechniqueId;
        importPDF.DateImport = DateTime.Now;

        context.ImportsPDF.Add(importPDF);
        await context.SaveChangesAsync();
        return importPDF;
    }

    public async Task<bool> RemovePDFAsync(int importPDFId)
    {
        using var context = _contextFactory.CreateDbContext();
        var importPDF = await context.ImportsPDF.FindAsync(importPDFId);
        if (importPDF == null)
            return false;

        if (File.Exists(importPDF.CheminFichier))
        {
            File.Delete(importPDF.CheminFichier);
        }

        context.ImportsPDF.Remove(importPDF);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Met à jour le type de document d'un ImportPDF
    /// </summary>
    /// <param name="importPDFId">Identifiant de l'ImportPDF à modifier</param>
    /// <param name="typeDocumentImportId">Nouveau type de document</param>
    /// <returns>ImportPDF mis à jour</returns>
    /// <exception cref="InvalidOperationException">Si l'ImportPDF n'existe pas</exception>
    public async Task<ImportPDF> UpdatePDFTypeAsync(int importPDFId, int typeDocumentImportId)
    {
        using var context = _contextFactory.CreateDbContext();
        var importPDF = await context.ImportsPDF
            .Include(i => i.TypeDocumentImport)
            .FirstOrDefaultAsync(i => i.Id == importPDFId);

        if (importPDF == null)
        {
            throw new InvalidOperationException($"ImportPDF avec l'ID {importPDFId} n'existe pas.");
        }

        importPDF.TypeDocumentImportId = typeDocumentImportId;
        await context.SaveChangesAsync();

        _loggingService.LogInformation($"Type de document mis à jour pour ImportPDF {importPDFId}: TypeDocumentImportId = {typeDocumentImportId}");

        return importPDF;
    }

    public async Task<string> SavePDFFileAsync(Stream fileStream, string originalFileName)
    {
        var repertoireStockage = _appSettings.RepertoireStockagePDF;
        
        try
        {
            if (!Directory.Exists(repertoireStockage))
            {
                Directory.CreateDirectory(repertoireStockage);
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}";
            var filePath = Path.Combine(repertoireStockage, fileName);

            using (var outputFileStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(outputFileStream);
            }

            // Vérifier que le fichier a été créé
            if (!File.Exists(filePath))
            {
                throw new IOException($"Le fichier n'a pas pu être sauvegardé à : {filePath}");
            }

            return filePath;
        }
        catch (Exception ex)
        {
            throw new IOException($"Erreur lors de la sauvegarde du fichier PDF '{originalFileName}' dans '{repertoireStockage}': {ex.Message}", ex);
        }
    }

    public async Task<ImportPDF?> GetPDFFileAsync(int importPDFId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.ImportsPDF.FindAsync(importPDFId);
    }
}