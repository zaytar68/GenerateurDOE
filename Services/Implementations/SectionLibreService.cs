using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenerateurDOE.Services.Implementations;

public class SectionLibreService : ISectionLibreService
{
    private readonly ApplicationDbContext _context;

    public SectionLibreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SectionLibre>> GetAllAsync()
    {
        return await _context.SectionsLibres
            .Include(s => s.TypeSection)
            .OrderBy(s => s.Ordre)
            .ThenBy(s => s.Titre)
            .ToListAsync();
    }

    public async Task<IEnumerable<SectionLibre>> GetByTypeSectionAsync(int typeSectionId)
    {
        return await _context.SectionsLibres
            .Include(s => s.TypeSection)
            .Where(s => s.TypeSectionId == typeSectionId)
            .OrderBy(s => s.Ordre)
            .ToListAsync();
    }

    public async Task<SectionLibre?> GetByIdAsync(int id)
    {
        return await _context.SectionsLibres
            .Include(s => s.TypeSection)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SectionLibre> CreateAsync(SectionLibre sectionLibre)
    {
        sectionLibre.DateCreation = DateTime.Now;
        sectionLibre.DateModification = DateTime.Now;
        
        // Si aucun ordre n'est spécifié, prendre le suivant
        if (sectionLibre.Ordre <= 0)
        {
            sectionLibre.Ordre = await GetNextOrderAsync();
        }

        _context.SectionsLibres.Add(sectionLibre);
        await _context.SaveChangesAsync();
        return sectionLibre;
    }

    public async Task<SectionLibre> UpdateAsync(SectionLibre sectionLibre)
    {
        var existingSection = await _context.SectionsLibres.FindAsync(sectionLibre.Id);
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

        await _context.SaveChangesAsync();
        return existingSection;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sectionLibre = await _context.SectionsLibres.FindAsync(id);
        if (sectionLibre == null)
            return false;

        _context.SectionsLibres.Remove(sectionLibre);
        await _context.SaveChangesAsync();

        // Réorganiser les ordres après suppression
        await ReorganizeOrdersAsync();
        
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.SectionsLibres.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> ReorderAsync(int sectionId, int newOrder)
    {
        var section = await _context.SectionsLibres.FindAsync(sectionId);
        if (section == null)
            return false;

        var oldOrder = section.Ordre;

        if (oldOrder == newOrder)
            return true;

        // Décaler les autres sections
        if (newOrder < oldOrder)
        {
            // Monter la section : décaler les sections entre newOrder et oldOrder vers le bas
            var sectionsToShift = await _context.SectionsLibres
                .Where(s => s.Ordre >= newOrder && s.Ordre < oldOrder && s.Id != sectionId)
                .ToListAsync();

            foreach (var s in sectionsToShift)
            {
                s.Ordre += 1;
                s.DateModification = DateTime.Now;
            }
        }
        else
        {
            // Descendre la section : décaler les sections entre oldOrder et newOrder vers le haut
            var sectionsToShift = await _context.SectionsLibres
                .Where(s => s.Ordre > oldOrder && s.Ordre <= newOrder && s.Id != sectionId)
                .ToListAsync();

            foreach (var s in sectionsToShift)
            {
                s.Ordre -= 1;
                s.DateModification = DateTime.Now;
            }
        }

        // Mettre à jour la section déplacée
        section.Ordre = newOrder;
        section.DateModification = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SectionLibre>> GetOrderedSectionsAsync()
    {
        return await _context.SectionsLibres
            .Include(s => s.TypeSection)
            .Where(s => s.TypeSection.IsActive) // Seulement les sections avec types actifs
            .OrderBy(s => s.Ordre)
            .ToListAsync();
    }

    public async Task<int> GetNextOrderAsync()
    {
        var maxOrder = await _context.SectionsLibres.MaxAsync(s => (int?)s.Ordre) ?? 0;
        return maxOrder + 1;
    }

    private async Task ReorganizeOrdersAsync()
    {
        var sections = await _context.SectionsLibres
            .OrderBy(s => s.Ordre)
            .ToListAsync();

        for (int i = 0; i < sections.Count; i++)
        {
            sections[i].Ordre = i + 1;
            sections[i].DateModification = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }
}