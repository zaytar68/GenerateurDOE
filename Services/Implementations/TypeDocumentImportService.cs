using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static GenerateurDOE.Services.Implementations.CacheServiceExtensions;

namespace GenerateurDOE.Services.Implementations;

public class TypeDocumentImportService : ITypeDocumentImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;

    public TypeDocumentImportService(IDbContextFactory<ApplicationDbContext> contextFactory, ICacheService cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<IEnumerable<TypeDocumentImport>> GetAllAsync()
    {
        // ⚡ Cache L1 : TypesDocuments avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_DOCUMENTS_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesDocuments
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<IEnumerable<TypeDocumentImport>> GetActiveAsync()
    {
        // ⚡ Cache L1 : TypesDocuments actifs avec expiration 1h
        return await _cache.GetOrCreateAsync(TYPES_DOCUMENTS_ACTIVE_KEY, async () =>
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await context.TypesDocuments
                .Where(t => t.IsActive)
                .OrderBy(t => t.Nom)
                .ToListAsync().ConfigureAwait(false);
        }, TimeSpan.FromHours(1));
    }

    public async Task<TypeDocumentImport?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesDocuments
            .FirstOrDefaultAsync(t => t.Id == id).ConfigureAwait(false);
    }

    public async Task<TypeDocumentImport> CreateAsync(TypeDocumentImport typeDocument)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        typeDocument.DateCreation = DateTime.Now;
        typeDocument.DateModification = DateTime.Now;

        context.TypesDocuments.Add(typeDocument);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // ⚡ Invalidation cache après création
        _cache.RemoveByPrefix(TYPES_PREFIX);

        return typeDocument;
    }

    public async Task<bool> UpdateAsync(TypeDocumentImport typeDocument)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var existingType = await context.TypesDocuments.FindAsync(typeDocument.Id).ConfigureAwait(false);
        if (existingType == null)
            return false;

        existingType.Nom = typeDocument.Nom;
        existingType.Description = typeDocument.Description;
        existingType.IsActive = typeDocument.IsActive;
        existingType.DateModification = DateTime.Now;

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après modification (AJOUTÉ)
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

        var typeDocument = await context.TypesDocuments.FindAsync(id).ConfigureAwait(false);
        if (typeDocument == null)
            return false;

        if (await CanDeleteAsync(id))
        {
            context.TypesDocuments.Remove(typeDocument);
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après suppression (AJOUTÉ)
            _cache.RemoveByPrefix(TYPES_PREFIX);

            return true;
        }

        return false;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var typeDocument = await context.TypesDocuments.FindAsync(id).ConfigureAwait(false);
        if (typeDocument == null)
            return false;

        typeDocument.IsActive = !typeDocument.IsActive;
        typeDocument.DateModification = DateTime.Now;

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);

            // ⚡ Invalidation cache après changement statut (AJOUTÉ)
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
        // TODO: Pour l'instant, on ne peut pas vérifier l'usage car on utilise encore l'enum TypeDocument
        var usageCount = 0;
            
        return Task.FromResult(usageCount == 0);
    }

    public async Task<bool> ExistsAsync(string nom)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TypesDocuments
            .AnyAsync(t => t.Nom.ToLower() == nom.ToLower()).ConfigureAwait(false);
    }

    public async Task InitializeDefaultTypesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await context.TypesDocuments.AnyAsync().ConfigureAwait(false))
        {
            var defaultTypes = new[]
            {
                new TypeDocumentImport { Nom = "Fiche technique", Description = "Fiche technique produit standard" },
                new TypeDocumentImport { Nom = "Notice de pose", Description = "Instructions d'installation et de pose" },
                new TypeDocumentImport { Nom = "Certificat UPEC", Description = "Certificats et homologations" },
                new TypeDocumentImport { Nom = "Garantie", Description = "Documents de garantie constructeur" },
                new TypeDocumentImport { Nom = "Avis technique", Description = "Avis techniques officiels" },
                new TypeDocumentImport { Nom = "PV d'essai", Description = "Procès-verbaux d'essais et tests" },
                new TypeDocumentImport { Nom = "FDS", Description = "Fiche de données de sécurité" },
                new TypeDocumentImport { Nom = "FDES", Description = "Fiche de Déclaration Environnementale et sanitaire" },
                new TypeDocumentImport { Nom = "Entretien", Description = "Notice d'entretien" },
                new TypeDocumentImport { Nom = "Brochure", Description = "Brochure commerciale" }
            };

            context.TypesDocuments.AddRange(defaultTypes);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}