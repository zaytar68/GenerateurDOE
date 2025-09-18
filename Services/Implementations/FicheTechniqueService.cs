using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class FicheTechniqueService : IFicheTechniqueService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AppSettings _appSettings;

    public FicheTechniqueService(IDbContextFactory<ApplicationDbContext> contextFactory, IOptions<AppSettings> appSettings)
    {
        _contextFactory = contextFactory;
        _appSettings = appSettings.Value;
    }

    public async Task<IEnumerable<FicheTechnique>> GetAllAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .OrderBy(f => f.NomProduit)
            .ToListAsync();
    }

    public async Task<FicheTechnique?> GetByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .FirstOrDefaultAsync(f => f.Id == id);
    }


    public async Task<FicheTechnique> CreateAsync(FicheTechnique ficheTechnique)
    {
        using var context = _contextFactory.CreateDbContext();
        ficheTechnique.DateCreation = DateTime.Now;
        ficheTechnique.DateModification = DateTime.Now;

        context.FichesTechniques.Add(ficheTechnique);
        await context.SaveChangesAsync();
        return ficheTechnique;
    }

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

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var ficheTechnique = await context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficheTechnique == null)
            return false;

        foreach (var importPDF in ficheTechnique.ImportsPDF)
        {
            if (File.Exists(importPDF.CheminFichier))
            {
                File.Delete(importPDF.CheminFichier);
            }
        }

        context.FichesTechniques.Remove(ficheTechnique);
        await context.SaveChangesAsync();
        return true;
    }

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