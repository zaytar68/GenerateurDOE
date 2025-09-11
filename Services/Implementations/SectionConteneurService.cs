using Microsoft.EntityFrameworkCore;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class SectionConteneurService : ISectionConteneurService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggingService _loggingService;

    public SectionConteneurService(ApplicationDbContext context, ILoggingService loggingService)
    {
        _context = context;
        _loggingService = loggingService;
    }

    public async Task<SectionConteneur> CreateAsync(int documentGenereId, int typeSectionId, string? titre = null)
    {
        if (!await CanCreateForTypeAsync(documentGenereId, typeSectionId))
        {
            var typeSection = await _context.TypesSections.FindAsync(typeSectionId);
            throw new InvalidOperationException($"Un conteneur pour le type '{typeSection?.Nom}' existe déjà pour ce document");
        }

        var document = await _context.DocumentsGeneres.FindAsync(documentGenereId);
        var type = await _context.TypesSections.FindAsync(typeSectionId);
        
        if (document == null || type == null)
            throw new ArgumentException("Document ou type de section non trouvé");

        var maxOrder = await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre) ?? 0;

        var sectionConteneur = new SectionConteneur
        {
            DocumentGenereId = documentGenereId,
            TypeSectionId = typeSectionId,
            Titre = titre ?? type.Nom,
            Ordre = maxOrder + 1
        };

        _context.SectionsConteneurs.Add(sectionConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"SectionConteneur créé : {sectionConteneur.Titre} pour document {documentGenereId}");
        return sectionConteneur;
    }

    public async Task<SectionConteneur> GetByIdAsync(int id)
    {
        var sectionConteneur = await _context.SectionsConteneurs
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .Include(sc => sc.DocumentGenere)
            .FirstOrDefaultAsync(sc => sc.Id == id);

        if (sectionConteneur == null)
            throw new ArgumentException("SectionConteneur non trouvé", nameof(id));

        return sectionConteneur;
    }

    public async Task<SectionConteneur?> GetByDocumentAndTypeAsync(int documentGenereId, int typeSectionId)
    {
        return await _context.SectionsConteneurs
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId);
    }

    public async Task<IEnumerable<SectionConteneur>> GetByDocumentIdAsync(int documentGenereId)
    {
        return await _context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .OrderBy(sc => sc.Ordre)
            .ToListAsync();
    }

    public async Task<SectionConteneur> UpdateAsync(SectionConteneur sectionConteneur)
    {
        sectionConteneur.DateModification = DateTime.Now;
        _context.SectionsConteneurs.Update(sectionConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"SectionConteneur mis à jour : {sectionConteneur.Titre}");
        return sectionConteneur;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sectionConteneur = await _context.SectionsConteneurs.FindAsync(id);
        if (sectionConteneur == null)
            return false;

        _context.SectionsConteneurs.Remove(sectionConteneur);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"SectionConteneur supprimé : ID {id}");
        return true;
    }

    public async Task<SectionConteneur> AddSectionLibreAsync(int sectionConteneursId, int sectionLibreId)
    {
        if (!await ValidateTypeConsistencyAsync(sectionConteneursId, sectionLibreId))
            throw new InvalidOperationException("La section libre n'est pas compatible avec le type du conteneur");

        var sectionConteneur = await GetByIdAsync(sectionConteneursId);
        var sectionLibre = await _context.SectionsLibres.FindAsync(sectionLibreId);

        if (sectionLibre == null)
            throw new ArgumentException("SectionLibre non trouvée", nameof(sectionLibreId));

        if (!sectionConteneur.SectionsLibres.Any(sl => sl.Id == sectionLibreId))
        {
            sectionConteneur.SectionsLibres.Add(sectionLibre);
            await _context.SaveChangesAsync();
            _loggingService.LogInformation($"SectionLibre {sectionLibreId} ajoutée au conteneur {sectionConteneursId}");
        }

        return sectionConteneur;
    }

    public async Task<bool> RemoveSectionLibreAsync(int sectionConteneursId, int sectionLibreId)
    {
        var sectionConteneur = await GetByIdAsync(sectionConteneursId);
        var sectionLibre = sectionConteneur.SectionsLibres.FirstOrDefault(sl => sl.Id == sectionLibreId);

        if (sectionLibre == null)
            return false;

        sectionConteneur.SectionsLibres.Remove(sectionLibre);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"SectionLibre {sectionLibreId} retirée du conteneur {sectionConteneursId}");
        return true;
    }

    public async Task<SectionConteneur> ReorderSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds)
    {
        var sectionConteneur = await GetByIdAsync(sectionConteneursId);
        
        for (int i = 0; i < sectionLibreIds.Count; i++)
        {
            var sectionLibre = await _context.SectionsLibres.FindAsync(sectionLibreIds[i]);
            if (sectionLibre != null)
            {
                sectionLibre.Ordre = i + 1;
            }
        }

        await _context.SaveChangesAsync();
        _loggingService.LogInformation($"Ordre des sections réorganisé pour le conteneur {sectionConteneursId}");
        
        return await GetByIdAsync(sectionConteneursId);
    }

    public async Task<bool> CanCreateForTypeAsync(int documentGenereId, int typeSectionId)
    {
        var existing = await _context.SectionsConteneurs
            .AnyAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId);
        return !existing;
    }

    public async Task<bool> ValidateTypeConsistencyAsync(int sectionConteneursId, int sectionLibreId)
    {
        var sectionConteneur = await _context.SectionsConteneurs.FindAsync(sectionConteneursId);
        var sectionLibre = await _context.SectionsLibres.FindAsync(sectionLibreId);

        if (sectionConteneur == null || sectionLibre == null)
            return false;

        return sectionConteneur.TypeSectionId == sectionLibre.TypeSectionId;
    }

    // Nouvelles méthodes pour gérer les SectionConteneurItem
    public async Task<SectionConteneurItem> AddSectionLibreWithOrderAsync(int sectionConteneursId, int sectionLibreId, int ordre)
    {
        if (!await ValidateTypeConsistencyAsync(sectionConteneursId, sectionLibreId))
            throw new InvalidOperationException("La section libre n'est pas compatible avec le type du conteneur");

        // Vérifier si cette section n'est pas déjà dans le conteneur
        var existingItem = await _context.SectionConteneurItems
            .FirstOrDefaultAsync(sci => sci.SectionConteneursId == sectionConteneursId && sci.SectionLibreId == sectionLibreId);
        
        if (existingItem != null)
            throw new InvalidOperationException("Cette section est déjà présente dans le conteneur");

        var item = new SectionConteneurItem
        {
            SectionConteneursId = sectionConteneursId,
            SectionLibreId = sectionLibreId,
            Ordre = ordre,
            DateAjout = DateTime.Now
        };

        _context.SectionConteneurItems.Add(item);
        await _context.SaveChangesAsync();

        await _context.Entry(item).Reference(i => i.SectionLibre).LoadAsync();
        await _context.Entry(item).Reference(i => i.SectionConteneur).LoadAsync();

        _loggingService.LogInformation($"SectionLibre {sectionLibreId} ajoutée au conteneur {sectionConteneursId} avec ordre {ordre}");
        return item;
    }

    public async Task<List<SectionConteneurItem>> AddMultipleSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds)
    {
        var items = new List<SectionConteneurItem>();
        
        // Calculer l'ordre de départ (prendre le maximum + 1)
        var maxOrder = await _context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Select(sci => (int?)sci.Ordre)
            .MaxAsync() ?? 0;

        for (int i = 0; i < sectionLibreIds.Count; i++)
        {
            try
            {
                var item = await AddSectionLibreWithOrderAsync(sectionConteneursId, sectionLibreIds[i], maxOrder + i + 1);
                items.Add(item);
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Impossible d'ajouter la section {sectionLibreIds[i]}: {ex.Message}");
            }
        }

        return items;
    }

    public async Task<bool> RemoveSectionLibreItemAsync(int sectionConteneurItemId)
    {
        var item = await _context.SectionConteneurItems.FindAsync(sectionConteneurItemId);
        if (item == null)
            return false;

        _context.SectionConteneurItems.Remove(item);
        await _context.SaveChangesAsync();

        _loggingService.LogInformation($"SectionConteneurItem {sectionConteneurItemId} supprimé");
        return true;
    }

    public async Task<List<SectionConteneurItem>> ReorderItemsAsync(int sectionConteneursId, List<int> itemIds)
    {
        var items = await _context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Include(sci => sci.SectionLibre)
            .ToListAsync();

        // Réordonner selon l'ordre fourni
        for (int i = 0; i < itemIds.Count; i++)
        {
            var item = items.FirstOrDefault(it => it.Id == itemIds[i]);
            if (item != null)
            {
                item.Ordre = i + 1;
            }
        }

        await _context.SaveChangesAsync();
        _loggingService.LogInformation($"Ordre des items du conteneur {sectionConteneursId} mis à jour");

        return items.OrderBy(i => i.Ordre).ToList();
    }

    public async Task<List<SectionLibre>> GetAvailableSectionsForConteneurAsync(int sectionConteneursId, int typeSectionId)
    {
        // Récupérer les sections déjà utilisées dans ce conteneur
        var usedSectionIds = await _context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Select(sci => sci.SectionLibreId)
            .ToListAsync();

        // Retourner les sections du bon type qui ne sont pas déjà utilisées
        return await _context.SectionsLibres
            .Where(sl => sl.TypeSectionId == typeSectionId && !usedSectionIds.Contains(sl.Id))
            .OrderBy(sl => sl.Titre)
            .ToListAsync();
    }
}