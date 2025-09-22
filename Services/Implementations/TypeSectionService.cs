using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static GenerateurDOE.Services.Implementations.CacheServiceExtensions;

namespace GenerateurDOE.Services.Implementations;

public class TypeSectionService : ITypeSectionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;

    public TypeSectionService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<IEnumerable<TypeSection>> GetAllAsync()
    {
        // ⚡ Cache L1 : TypesSections avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_SECTIONS_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesSections
                .Include(t => t.SectionsLibres)
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<IEnumerable<TypeSection>> GetActiveAsync()
    {
        // ⚡ Cache L1 : TypesSections actifs avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_SECTIONS_ACTIVE_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesSections
                .Where(t => t.IsActive)
                .Include(t => t.SectionsLibres)
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<TypeSection?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesSections
            .Include(t => t.SectionsLibres)
            .FirstOrDefaultAsync(t => t.Id == id).ConfigureAwait(false);
    }

    public async Task<TypeSection> CreateAsync(TypeSection typeSection)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        typeSection.DateCreation = DateTime.Now;
        typeSection.DateModification = DateTime.Now;

        context.TypesSections.Add(typeSection);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après création
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return typeSection;
    }

    public async Task<TypeSection> UpdateAsync(TypeSection typeSection)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var existingType = await context.TypesSections.FindAsync(typeSection.Id).ConfigureAwait(false);
        if (existingType == null)
        {
            throw new ArgumentException($"TypeSection avec l'ID {typeSection.Id} introuvable.");
        }

        existingType.Nom = typeSection.Nom;
        existingType.Description = typeSection.Description;
        existingType.IsActive = typeSection.IsActive;
        existingType.DateModification = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après modification
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return existingType;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var typeSection = await context.TypesSections
            .Include(t => t.SectionsLibres)
            .FirstOrDefaultAsync(t => t.Id == id).ConfigureAwait(false);

        if (typeSection == null)
            return false;

        // Vérifier s'il y a des sections associées
        if (typeSection.SectionsLibres.Any())
        {
            throw new InvalidOperationException($"Impossible de supprimer le type de section '{typeSection.Nom}' car il est utilisé par {typeSection.SectionsLibres.Count} section(s).");
        }

        context.TypesSections.Remove(typeSection);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après suppression
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var typeSection = await context.TypesSections.FindAsync(id).ConfigureAwait(false);
        if (typeSection == null)
            return false;

        typeSection.IsActive = !typeSection.IsActive;
        typeSection.DateModification = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après changement statut
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesSections.AnyAsync(t => t.Id == id).ConfigureAwait(false);
    }

    public async Task<bool> HasSectionsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres.AnyAsync(s => s.TypeSectionId == id).ConfigureAwait(false);
    }

    public async Task InitializeDefaultTypesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await context.TypesSections.AnyAsync().ConfigureAwait(false))
        {
            var defaultTypes = new[]
            {
                new TypeSection { Nom = "Introduction", Description = "Section d'introduction du document" },
                new TypeSection { Nom = "Méthodologie", Description = "Description des méthodes employées" },
                new TypeSection { Nom = "Présentation société", Description = "Présentation de l'entreprise" },
                new TypeSection { Nom = "Références chantier", Description = "Références et expériences sur des chantiers similaires" },
                new TypeSection { Nom = "Démarche environnementale", Description = "Approche et mesures environnementales du projet" },
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

            context.TypesSections.AddRange(defaultTypes);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        else
        {
            // Ajouter les nouveaux types s'ils n'existent pas déjà
            await AddMissingDefaultTypesAsync();
        }
    }

    private async Task AddMissingDefaultTypesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var newTypes = new[]
        {
            new { Nom = "Références chantier", Description = "Références et expériences sur des chantiers similaires" },
            new { Nom = "Démarche environnementale", Description = "Approche et mesures environnementales du projet" }
        };

        foreach (var newType in newTypes)
        {
            var exists = await context.TypesSections.AnyAsync(t => t.Nom == newType.Nom).ConfigureAwait(false);
            if (!exists)
            {
                context.TypesSections.Add(new TypeSection
                {
                    Nom = newType.Nom,
                    Description = newType.Description,
                    IsActive = true,
                    DateCreation = DateTime.Now,
                    DateModification = DateTime.Now
                });
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après ajout de nouveaux types
        _cache.RemoveByPrefix(TYPES_PREFIX);
    }
}