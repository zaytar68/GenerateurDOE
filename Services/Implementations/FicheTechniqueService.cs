using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class FicheTechniqueService : IFicheTechniqueService
{
    private readonly ApplicationDbContext _context;
    private readonly AppSettings _appSettings;

    public FicheTechniqueService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
    {
        _context = context;
        _appSettings = appSettings.Value;
    }

    public async Task<IEnumerable<FicheTechnique>> GetAllAsync()
    {
        return await _context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .Include(f => f.Chantier)
            .OrderBy(f => f.NomProduit)
            .ToListAsync();
    }

    public async Task<FicheTechnique?> GetByIdAsync(int id)
    {
        return await _context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .Include(f => f.Chantier)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<FicheTechnique>> GetByChantierId(int chantierId)
    {
        return await _context.FichesTechniques
            .Include(f => f.ImportsPDF)
            .Where(f => f.ChantierId == chantierId)
            .OrderBy(f => f.NomProduit)
            .ToListAsync();
    }

    public async Task<FicheTechnique> CreateAsync(FicheTechnique ficheTechnique)
    {
        ficheTechnique.DateCreation = DateTime.Now;
        ficheTechnique.DateModification = DateTime.Now;

        _context.FichesTechniques.Add(ficheTechnique);
        await _context.SaveChangesAsync();
        return ficheTechnique;
    }

    public async Task<FicheTechnique> UpdateAsync(FicheTechnique ficheTechnique)
    {
        var existingFiche = await _context.FichesTechniques.FindAsync(ficheTechnique.Id);
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

        await _context.SaveChangesAsync();
        return existingFiche;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ficheTechnique = await _context.FichesTechniques
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

        _context.FichesTechniques.Remove(ficheTechnique);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ImportPDF> AddPDFAsync(int ficheTechniqueId, ImportPDF importPDF)
    {
        importPDF.FicheTechniqueId = ficheTechniqueId;
        importPDF.DateImport = DateTime.Now;

        _context.ImportsPDF.Add(importPDF);
        await _context.SaveChangesAsync();
        return importPDF;
    }

    public async Task<bool> RemovePDFAsync(int importPDFId)
    {
        var importPDF = await _context.ImportsPDF.FindAsync(importPDFId);
        if (importPDF == null)
            return false;

        if (File.Exists(importPDF.CheminFichier))
        {
            File.Delete(importPDF.CheminFichier);
        }

        _context.ImportsPDF.Remove(importPDF);
        await _context.SaveChangesAsync();
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
}