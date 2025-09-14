using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static GenerateurDOE.Services.Implementations.CacheServiceExtensions;

namespace GenerateurDOE.Services.Implementations;

public class TypeProduitService : ITypeProduitService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cache;

    public TypeProduitService(ApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IEnumerable<TypeProduit>> GetAllAsync()
    {
        // ⚡ Cache L1 : TypesProduits avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_PRODUITS_KEY, async () =>
        {
            return await _context.TypesProduits
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }, TimeSpan.FromHours(1));
    }

    public async Task<IEnumerable<TypeProduit>> GetActiveAsync()
    {
        // ⚡ Cache L1 : TypesProduits actifs avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_PRODUITS_ACTIVE_KEY, async () =>
        {
            return await _context.TypesProduits
                .Where(t => t.IsActive)
                .OrderBy(t => t.Nom)
                .ToListAsync();
        }, TimeSpan.FromHours(1));
    }

    public async Task<TypeProduit?> GetByIdAsync(int id)
    {
        return await _context.TypesProduits
            .Include(t => t.FichesTechniques)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TypeProduit> CreateAsync(TypeProduit typeProduit)
    {
        typeProduit.DateCreation = DateTime.Now;
        typeProduit.DateModification = DateTime.Now;

        _context.TypesProduits.Add(typeProduit);
        await _context.SaveChangesAsync();
        
        // ⚡ Invalidation cache après création
        _cache.RemoveByPrefix(TYPES_PREFIX);
        
        return typeProduit;
    }

    public async Task<bool> UpdateAsync(TypeProduit typeProduit)
    {
        var existingType = await _context.TypesProduits.FindAsync(typeProduit.Id);
        if (existingType == null)
            return false;

        existingType.Nom = typeProduit.Nom;
        existingType.Description = typeProduit.Description;
        existingType.IsActive = typeProduit.IsActive;
        existingType.DateModification = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
            
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
        var typeProduit = await _context.TypesProduits.FindAsync(id);
        if (typeProduit == null)
            return false;

        if (await CanDeleteAsync(id))
        {
            _context.TypesProduits.Remove(typeProduit);
            await _context.SaveChangesAsync();
            
            // ⚡ Invalidation cache après suppression
            _cache.RemoveByPrefix(TYPES_PREFIX);
            
            return true;
        }

        return false;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var typeProduit = await _context.TypesProduits.FindAsync(id);
        if (typeProduit == null)
            return false;

        typeProduit.IsActive = !typeProduit.IsActive;
        typeProduit.DateModification = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
            
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
        return await _context.TypesProduits
            .AnyAsync(t => t.Nom.ToLower() == nom.ToLower());
    }

    public async Task InitializeDefaultTypesAsync()
    {
        if (!await _context.TypesProduits.AnyAsync())
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

            _context.TypesProduits.AddRange(defaultTypes);
            await _context.SaveChangesAsync();
        }
    }
}