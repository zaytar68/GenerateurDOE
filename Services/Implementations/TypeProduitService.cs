using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static GenerateurDOE.Services.Implementations.CacheServiceExtensions;

namespace GenerateurDOE.Services.Implementations;

public class TypeProduitService : ITypeProduitService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;

    public TypeProduitService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<IEnumerable<TypeProduit>> GetAllAsync()
    {
        // ⚡ Cache L1 : TypesProduits avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_PRODUITS_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesProduits
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<IEnumerable<TypeProduit>> GetActiveAsync()
    {
        // ⚡ Cache L1 : TypesProduits actifs avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_PRODUITS_ACTIVE_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesProduits
                .Where(t => t.IsActive)
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<TypeProduit?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesProduits
            .Include(t => t.FichesTechniques)
            .FirstOrDefaultAsync(t => t.Id == id).ConfigureAwait(false);
    }

    public async Task<TypeProduit> CreateAsync(TypeProduit typeProduit)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        typeProduit.DateCreation = DateTime.Now;
        typeProduit.DateModification = DateTime.Now;

        context.TypesProduits.Add(typeProduit);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après création
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return typeProduit;
    }

    public async Task<bool> UpdateAsync(TypeProduit typeProduit)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var existingType = await context.TypesProduits.FindAsync(typeProduit.Id).ConfigureAwait(false);
        if (existingType == null)
            return false;

        existingType.Nom = typeProduit.Nom;
        existingType.Description = typeProduit.Description;
        existingType.IsActive = typeProduit.IsActive;
        existingType.DateModification = DateTime.Now;

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après modification
            _cache.RemoveByPrefix(TYPES_PREFIX);

            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var typeProduit = await context.TypesProduits.FindAsync(id).ConfigureAwait(false);
        if (typeProduit == null)
            return false;

        if (await CanDeleteAsync(id))
        {
            context.TypesProduits.Remove(typeProduit);
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après suppression
            _cache.RemoveByPrefix(TYPES_PREFIX);

            return true;
        }

        return false;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var typeProduit = await context.TypesProduits.FindAsync(id).ConfigureAwait(false);
        if (typeProduit == null)
            return false;

        typeProduit.IsActive = !typeProduit.IsActive;
        typeProduit.DateModification = DateTime.Now;

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après changement statut
            _cache.RemoveByPrefix(TYPES_PREFIX);

            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public Task<bool> CanDeleteAsync(int id)
    {
        // TODO: Pour le moment, on ne peut pas vérifier l'usage car on utilise encore le champ string TypeProduit
        // Cette méthode sera mise à jour après la migration vers TypeProduitId
        var usageCount = 0;
            
        return Task.FromResult(usageCount == 0);
    }

    public async Task<bool> ExistsAsync(string nom)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesProduits
            .AnyAsync(t => t.Nom.ToLower() == nom.ToLower()).ConfigureAwait(false);
    }

    public async Task InitializeDefaultTypesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await context.TypesProduits.AnyAsync().ConfigureAwait(false))
        {
            var defaultTypes = new[]
            {
                new TypeProduit { Nom = "Isolant thermique", Description = "Matériaux d'isolation thermique" },
                new TypeProduit { Nom = "Isolant phonique", Description = "Matériaux d'isolation phonique" },
                new TypeProduit { Nom = "Revêtement sol", Description = "Revêtements de sol" },
                new TypeProduit { Nom = "Revêtement mur", Description = "Revêtements muraux" },
                new TypeProduit { Nom = "Menuiserie", Description = "Portes, fenêtres, volets" },
                new TypeProduit { Nom = "Plomberie", Description = "Équipements sanitaires et plomberie" },
                new TypeProduit { Nom = "Électricité", Description = "Équipements électriques" },
                new TypeProduit { Nom = "Chauffage", Description = "Systèmes de chauffage" },
                new TypeProduit { Nom = "Ventilation", Description = "Systèmes de ventilation" },
                new TypeProduit { Nom = "Étanchéité", Description = "Produits d'étanchéité" }
            };

            context.TypesProduits.AddRange(defaultTypes);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}