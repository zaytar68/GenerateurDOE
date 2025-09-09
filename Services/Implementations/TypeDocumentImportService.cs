using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

public class TypeDocumentImportService : ITypeDocumentImportService
{
    private readonly ApplicationDbContext _context;

    public TypeDocumentImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TypeDocumentImport>> GetAllAsync()
    {
        return await _context.TypesDocuments
            .OrderBy(t => t.Nom)
            .ToListAsync();
    }

    public async Task<IEnumerable<TypeDocumentImport>> GetActiveAsync()
    {
        return await _context.TypesDocuments
            .Where(t => t.IsActive)
            .OrderBy(t => t.Nom)
            .ToListAsync();
    }

    public async Task<TypeDocumentImport?> GetByIdAsync(int id)
    {
        return await _context.TypesDocuments
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TypeDocumentImport> CreateAsync(TypeDocumentImport typeDocument)
    {
        typeDocument.DateCreation = DateTime.Now;
        typeDocument.DateModification = DateTime.Now;

        _context.TypesDocuments.Add(typeDocument);
        await _context.SaveChangesAsync();
        
        return typeDocument;
    }

    public async Task<bool> UpdateAsync(TypeDocumentImport typeDocument)
    {
        var existingType = await _context.TypesDocuments.FindAsync(typeDocument.Id);
        if (existingType == null)
            return false;

        existingType.Nom = typeDocument.Nom;
        existingType.Description = typeDocument.Description;
        existingType.IsActive = typeDocument.IsActive;
        existingType.DateModification = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var typeDocument = await _context.TypesDocuments.FindAsync(id);
        if (typeDocument == null)
            return false;

        if (await CanDeleteAsync(id))
        {
            _context.TypesDocuments.Remove(typeDocument);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var typeDocument = await _context.TypesDocuments.FindAsync(id);
        if (typeDocument == null)
            return false;

        typeDocument.IsActive = !typeDocument.IsActive;
        typeDocument.DateModification = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> CanDeleteAsync(int id)
    {
        // TODO: Pour l'instant, on ne peut pas vérifier l'usage car on utilise encore l'enum TypeDocument
        var usageCount = 0;
            
        return usageCount == 0;
    }

    public async Task<bool> ExistsAsync(string nom)
    {
        return await _context.TypesDocuments
            .AnyAsync(t => t.Nom.ToLower() == nom.ToLower());
    }

    public async Task InitializeDefaultTypesAsync()
    {
        if (!await _context.TypesDocuments.AnyAsync())
        {
            var defaultTypes = new[]
            {
                new TypeDocumentImport { Nom = "Fiche Technique", Description = "Documents techniques des produits" },
                new TypeDocumentImport { Nom = "Nuancier", Description = "Nuanciers de couleurs et finitions" },
                new TypeDocumentImport { Nom = "Brochure", Description = "Brochures commerciales" },
                new TypeDocumentImport { Nom = "Classement Feu", Description = "Documents de classement au feu" },
                new TypeDocumentImport { Nom = "Classement UPEC", Description = "Documents de classement UPEC" },
                new TypeDocumentImport { Nom = "Autre", Description = "Autres types de documents" }
            };

            _context.TypesDocuments.AddRange(defaultTypes);
            await _context.SaveChangesAsync();
        }
    }
}