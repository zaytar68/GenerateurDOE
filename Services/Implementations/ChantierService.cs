using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

public class ChantierService : IChantierService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ChantierService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

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

    public async Task<Chantier?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Chantiers
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Chantier?> GetByIdWithDocumentsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Chantiers
            .Include(c => c.DocumentsGeneres)
            .Include(c => c.FichesTechniques)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Chantier> CreateAsync(Chantier chantier)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        chantier.DateCreation = DateTime.Now;
        chantier.DateModification = DateTime.Now;

        context.Chantiers.Add(chantier);
        await context.SaveChangesAsync();
        
        return chantier;
    }

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