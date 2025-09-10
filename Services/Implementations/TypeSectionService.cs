using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

public class TypeSectionService : ITypeSectionService
{
    private readonly ApplicationDbContext _context;

    public TypeSectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TypeSection>> GetAllAsync()
    {
        return await _context.TypesSections
            .Include(t => t.SectionsLibres)
            .OrderBy(t => t.Nom)
            .ToListAsync();
    }

    public async Task<IEnumerable<TypeSection>> GetActiveAsync()
    {
        return await _context.TypesSections
            .Where(t => t.IsActive)
            .Include(t => t.SectionsLibres)
            .OrderBy(t => t.Nom)
            .ToListAsync();
    }

    public async Task<TypeSection?> GetByIdAsync(int id)
    {
        return await _context.TypesSections
            .Include(t => t.SectionsLibres)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TypeSection> CreateAsync(TypeSection typeSection)
    {
        typeSection.DateCreation = DateTime.Now;
        typeSection.DateModification = DateTime.Now;

        _context.TypesSections.Add(typeSection);
        await _context.SaveChangesAsync();
        return typeSection;
    }

    public async Task<TypeSection> UpdateAsync(TypeSection typeSection)
    {
        var existingType = await _context.TypesSections.FindAsync(typeSection.Id);
        if (existingType == null)
        {
            throw new ArgumentException($"TypeSection avec l'ID {typeSection.Id} introuvable.");
        }

        existingType.Nom = typeSection.Nom;
        existingType.Description = typeSection.Description;
        existingType.IsActive = typeSection.IsActive;
        existingType.DateModification = DateTime.Now;

        await _context.SaveChangesAsync();
        return existingType;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var typeSection = await _context.TypesSections
            .Include(t => t.SectionsLibres)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (typeSection == null)
            return false;

        // Vérifier s'il y a des sections associées
        if (typeSection.SectionsLibres.Any())
        {
            throw new InvalidOperationException($"Impossible de supprimer le type de section '{typeSection.Nom}' car il est utilisé par {typeSection.SectionsLibres.Count} section(s).");
        }

        _context.TypesSections.Remove(typeSection);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var typeSection = await _context.TypesSections.FindAsync(id);
        if (typeSection == null)
            return false;

        typeSection.IsActive = !typeSection.IsActive;
        typeSection.DateModification = DateTime.Now;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TypesSections.AnyAsync(t => t.Id == id);
    }

    public async Task<bool> HasSectionsAsync(int id)
    {
        return await _context.SectionsLibres.AnyAsync(s => s.TypeSectionId == id);
    }

    public async Task InitializeDefaultTypesAsync()
    {
        if (!await _context.TypesSections.AnyAsync())
        {
            var defaultTypes = new[]
            {
                new TypeSection { Nom = "Introduction", Description = "Section d'introduction du document" },
                new TypeSection { Nom = "Méthodologie", Description = "Description des méthodes employées" },
                new TypeSection { Nom = "Présentation société", Description = "Présentation de l'entreprise" },
                new TypeSection { Nom = "Conclusion", Description = "Section de conclusion" },
                new TypeSection { Nom = "Annexes", Description = "Documents annexes" },
                new TypeSection { Nom = "Section libre", Description = "Section personnalisée" }
            };

            foreach (var type in defaultTypes)
            {
                type.DateCreation = DateTime.Now;
                type.DateModification = DateTime.Now;
                type.IsActive = true;
            }

            _context.TypesSections.AddRange(defaultTypes);
            await _context.SaveChangesAsync();
        }
    }
}