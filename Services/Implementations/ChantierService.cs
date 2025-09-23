using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service de gestion des chantiers de construction avec recherche et archivage
/// Logique des lots migrée vers DocumentGenereService (Phase 2 - Migration)
/// </summary>
public class ChantierService : IChantierService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    /// <summary>
    /// Initialise une nouvelle instance du service ChantierService
    /// </summary>
    /// <param name="contextFactory">Factory pour créer les contextes EF thread-safe</param>
    public ChantierService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Récupère tous les chantiers triés par date de modification décroissante
    /// </summary>
    /// <param name="includeArchived">Inclure les chantiers archivés (défaut false)</param>
    /// <returns>Liste des chantiers actifs ou tous selon le paramètre</returns>
    public async Task<List<Chantier>> GetAllAsync(bool includeArchived = false)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = context.Chantiers.AsQueryable();
        
        if (!includeArchived)
        {
            query = query.Where(c => !c.EstArchive);
        }
        
        return await query
            .OrderByDescending(c => c.DateModification)
            .ToListAsync();
    }

    /// <summary>
    /// Recherche des chantiers par terme dans nom projet, maîtres d'œuvre/ouvrage et adresse
    /// Limité à 50 résultats pour l'autocomplétion
    /// </summary>
    /// <param name="searchTerm">Terme de recherche (nom, maîtr, adresse)</param>
    /// <param name="includeArchived">Inclure les chantiers archivés</param>
    /// <returns>Chantiers correspondants limités à 50 entrées</returns>
    public async Task<List<Chantier>> SearchAsync(string searchTerm, bool includeArchived = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync(includeArchived);
        }

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        searchTerm = searchTerm.Trim().ToLower();

        var query = context.Chantiers.AsQueryable();
        
        if (!includeArchived)
        {
            query = query.Where(c => !c.EstArchive);
        }
        
        query = query.Where(c => 
            c.NomProjet.ToLower().Contains(searchTerm) ||
            c.MaitreOeuvre.ToLower().Contains(searchTerm) ||
            c.MaitreOuvrage.ToLower().Contains(searchTerm) ||
            c.Adresse.ToLower().Contains(searchTerm)
        );
        
        return await query
            .OrderByDescending(c => c.DateModification)
            .Take(50) // Limiter les résultats pour l'autocomplétion
            .ToListAsync();
    }

    /// <summary>
    /// Récupère un chantier par son identifiant
    /// </summary>
    /// <param name="id">Identifiant du chantier</param>
    /// <returns>Chantier trouvé ou null</returns>
    public async Task<Chantier?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Chantiers
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Récupère un chantier avec ses documents générés et fiches techniques associées
    /// </summary>
    /// <param name="id">Identifiant du chantier</param>
    /// <returns>Chantier avec relations chargées ou null</returns>
    public async Task<Chantier?> GetByIdWithDocumentsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Chantiers
            .Include(c => c.DocumentsGeneres)
            .Include(c => c.FichesTechniques)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Crée un nouveau chantier avec horodatage automatique
    /// </summary>
    /// <param name="chantier">Chantier à créer</param>
    /// <returns>Chantier créé avec ID généré</returns>
    public async Task<Chantier> CreateAsync(Chantier chantier)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        chantier.DateCreation = DateTime.Now;
        chantier.DateModification = DateTime.Now;

        context.Chantiers.Add(chantier);
        await context.SaveChangesAsync();
        
        return chantier;
    }

    /// <summary>
    /// Met à jour un chantier existant avec tracking de modification
    /// </summary>
    /// <param name="chantier">Chantier avec modifications</param>
    /// <returns>Chantier mis à jour</returns>
    public async Task<Chantier> UpdateAsync(Chantier chantier)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        chantier.DateModification = DateTime.Now;

        context.Chantiers.Update(chantier);
        await context.SaveChangesAsync();
        
        return chantier;
    }

    public async Task DeleteAsync(int id)
    {
        var chantier = await GetByIdAsync(id);
        if (chantier != null)
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            context.Chantiers.Remove(chantier);
            await context.SaveChangesAsync();
        }
    }

    public async Task<Chantier> ArchiveAsync(int id)
    {
        var chantier = await GetByIdAsync(id);
        if (chantier == null)
        {
            throw new InvalidOperationException($"Chantier avec l'ID {id} introuvable.");
        }
        
        chantier.EstArchive = true;
        chantier.DateModification = DateTime.Now;

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Chantiers.Update(chantier);
        await context.SaveChangesAsync();
        return chantier;
    }

    public async Task<Chantier> UnarchiveAsync(int id)
    {
        var chantier = await GetByIdAsync(id);
        if (chantier == null)
        {
            throw new InvalidOperationException($"Chantier avec l'ID {id} introuvable.");
        }
        
        chantier.EstArchive = false;
        chantier.DateModification = DateTime.Now;

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Chantiers.Update(chantier);
        await context.SaveChangesAsync();
        return chantier;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Chantiers
            .AnyAsync(c => c.Id == id);
    }

    public async Task<List<Chantier>> GetRecentAsync(int count = 5, bool includeArchived = false)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = context.Chantiers.AsQueryable();
        
        if (!includeArchived)
        {
            query = query.Where(c => !c.EstArchive);
        }
        
        return await query
            .OrderByDescending(c => c.DateModification)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> CountAsync(bool includeArchived = false)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var query = context.Chantiers.AsQueryable();
        
        if (!includeArchived)
        {
            query = query.Where(c => !c.EstArchive);
        }
        
        return await query.CountAsync();
    }
}