using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

public class SectionLibreService : ISectionLibreService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public SectionLibreService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<SectionLibre>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres
            .Include(s => s.TypeSection)
            .OrderBy(s => s.Ordre)
            .ThenBy(s => s.Titre)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<SectionLibre>> GetByTypeSectionAsync(int typeSectionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres
            .Include(s => s.TypeSection)
            .Where(s => s.TypeSectionId == typeSectionId)
            .OrderBy(s => s.Ordre)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<SectionLibre?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres
            .Include(s => s.TypeSection)
            .FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }

    public async Task<SectionLibre> CreateAsync(SectionLibre sectionLibre)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        sectionLibre.DateCreation = DateTime.Now;
        sectionLibre.DateModification = DateTime.Now;

        // Si aucun ordre n'est spécifié, prendre le suivant
        if (sectionLibre.Ordre <= 0)
        {
            sectionLibre.Ordre = await GetNextOrderAsync(context);
        }

        context.SectionsLibres.Add(sectionLibre);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return sectionLibre;
    }

    public async Task<SectionLibre> UpdateAsync(SectionLibre sectionLibre)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var existingSection = await context.SectionsLibres.FindAsync(sectionLibre.Id).ConfigureAwait(false);
        if (existingSection == null)
        {
            throw new ArgumentException($"SectionLibre avec l'ID {sectionLibre.Id} introuvable.");
        }

        existingSection.Titre = sectionLibre.Titre;
        existingSection.Ordre = sectionLibre.Ordre;
        existingSection.ContenuHtml = sectionLibre.ContenuHtml;
        existingSection.ContenuJson = sectionLibre.ContenuJson;
        existingSection.TypeSectionId = sectionLibre.TypeSectionId;
        existingSection.DateModification = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return existingSection;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionLibre = await context.SectionsLibres.FindAsync(id).ConfigureAwait(false);
        if (sectionLibre == null)
            return false;

        context.SectionsLibres.Remove(sectionLibre);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // Réorganiser les ordres après suppression
        await ReorganizeOrdersAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres.AnyAsync(s => s.Id == id).ConfigureAwait(false);
    }

    public async Task<bool> ReorderAsync(int sectionId, int newOrder)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var section = await context.SectionsLibres.FindAsync(sectionId).ConfigureAwait(false);
        if (section == null)
            return false;

        var oldOrder = section.Ordre;

        if (oldOrder == newOrder)
            return true;

        // Décaler les autres sections
        if (newOrder < oldOrder)
        {
            // Monter la section : décaler les sections entre newOrder et oldOrder vers le bas
            var sectionsToShift = await context.SectionsLibres
                .Where(s => s.Ordre >= newOrder && s.Ordre < oldOrder && s.Id != sectionId)
                .ToListAsync().ConfigureAwait(false);

            foreach (var s in sectionsToShift)
            {
                s.Ordre += 1;
                s.DateModification = DateTime.Now;
            }
        }
        else
        {
            // Descendre la section : décaler les sections entre oldOrder et newOrder vers le haut
            var sectionsToShift = await context.SectionsLibres
                .Where(s => s.Ordre > oldOrder && s.Ordre <= newOrder && s.Id != sectionId)
                .ToListAsync().ConfigureAwait(false);

            foreach (var s in sectionsToShift)
            {
                s.Ordre -= 1;
                s.DateModification = DateTime.Now;
            }
        }

        // Mettre à jour la section déplacée
        section.Ordre = newOrder;
        section.DateModification = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<IEnumerable<SectionLibre>> GetOrderedSectionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.SectionsLibres
            .Include(s => s.TypeSection)
            .Where(s => s.TypeSection.IsActive) // Seulement les sections avec types actifs
            .OrderBy(s => s.Ordre)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<int> GetNextOrderAsync()
    {
        return await GetNextOrderAsync(null);
    }

    private async Task<int> GetNextOrderAsync(ApplicationDbContext? context)
    {
        if (context != null)
        {
            var maxOrder = await context.SectionsLibres.MaxAsync(s => (int?)s.Ordre).ConfigureAwait(false) ?? 0;
            return maxOrder + 1;
        }

        using var localContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var maxOrderLocal = await localContext.SectionsLibres.MaxAsync(s => (int?)s.Ordre).ConfigureAwait(false) ?? 0;
        return maxOrderLocal + 1;
    }

    private async Task ReorganizeOrdersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sections = await context.SectionsLibres
            .OrderBy(s => s.Ordre)
            .ToListAsync().ConfigureAwait(false);

        for (int i = 0; i < sections.Count; i++)
        {
            sections[i].Ordre = i + 1;
            sections[i].DateModification = DateTime.Now;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}