using Microsoft.EntityFrameworkCore;
using GenerateurDOE.Data;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class SectionConteneurService : ISectionConteneurService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILoggingService _loggingService;

    public SectionConteneurService(IDbContextFactory<ApplicationDbContext> contextFactory, ILoggingService loggingService)
    {
        _contextFactory = contextFactory;
        _loggingService = loggingService;
    }

    public async Task<SectionConteneur> CreateAsync(int documentGenereId, int typeSectionId, string? titre = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await CanCreateForTypeAsync(documentGenereId, typeSectionId))
        {
            var typeSection = await context.TypesSections.FindAsync(typeSectionId).ConfigureAwait(false);
            throw new InvalidOperationException($"Un conteneur pour le type '{typeSection?.Nom}' existe déjà pour ce document");
        }

        var document = await context.DocumentsGeneres.FindAsync(documentGenereId).ConfigureAwait(false);
        var type = await context.TypesSections.FindAsync(typeSectionId).ConfigureAwait(false);

        if (document == null || type == null)
            throw new ArgumentException("Document ou type de section non trouvé");

        var maxOrder = await context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .MaxAsync(sc => (int?)sc.Ordre).ConfigureAwait(false) ?? 0;

        var sectionConteneur = new SectionConteneur
        {
            DocumentGenereId = documentGenereId,
            TypeSectionId = typeSectionId,
            Titre = titre ?? type.Nom,
            Ordre = maxOrder + 1
        };

        context.SectionsConteneurs.Add(sectionConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneur créé : {sectionConteneur.Titre} pour document {documentGenereId}");
        return sectionConteneur;
    }

    public async Task<SectionConteneur> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // 🔧 CORRECTION CONCURRENCE CRITIQUE: AsSingleQuery pour éviter conflits
        var sectionConteneur = await context.SectionsConteneurs
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .Include(sc => sc.DocumentGenere)
            .AsSingleQuery()  // ✅ Single query pour contrôler concurrence
            .FirstOrDefaultAsync(sc => sc.Id == id).ConfigureAwait(false);

        if (sectionConteneur == null)
            throw new ArgumentException("SectionConteneur non trouvé", nameof(id));

        return sectionConteneur;
    }

    public async Task<SectionConteneur?> GetByDocumentAndTypeAsync(int documentGenereId, int typeSectionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // 🔧 CORRECTION CONCURRENCE: Single query pour éviter split
        return await context.SectionsConteneurs
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .AsSingleQuery()  // ✅ Contrôle explicite du split
            .FirstOrDefaultAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SectionConteneur>> GetByDocumentIdAsync(int documentGenereId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // 🔧 CORRECTION CONCURRENCE CRITIQUE: Single query pour liste multiple
        return await context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == documentGenereId)
            .Include(sc => sc.Items.OrderBy(item => item.Ordre))
                .ThenInclude(item => item.SectionLibre)
            .Include(sc => sc.TypeSection)
            .AsSingleQuery()  // ✅ Évite split sur collection multiples
            .OrderBy(sc => sc.Ordre)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<SectionConteneur> UpdateAsync(SectionConteneur sectionConteneur)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        sectionConteneur.DateModification = DateTime.Now;
        context.SectionsConteneurs.Update(sectionConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneur mis à jour : {sectionConteneur.Titre}");
        return sectionConteneur;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionConteneur = await context.SectionsConteneurs.FindAsync(id).ConfigureAwait(false);
        if (sectionConteneur == null)
            return false;

        context.SectionsConteneurs.Remove(sectionConteneur);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneur supprimé : ID {id}");
        return true;
    }

    public async Task<SectionConteneur> AddSectionLibreAsync(int sectionConteneursId, int sectionLibreId)
    {
        if (!await ValidateTypeConsistencyAsync(sectionConteneursId, sectionLibreId))
            throw new InvalidOperationException("La section libre n'est pas compatible avec le type du conteneur");

        var sectionConteneur = await GetByIdAsync(sectionConteneursId);

        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var sectionLibre = await context.SectionsLibres.FindAsync(sectionLibreId).ConfigureAwait(false);

        if (sectionLibre == null)
            throw new ArgumentException("SectionLibre non trouvée", nameof(sectionLibreId));

        // TODO: Refactorer cette méthode pour utiliser SectionConteneurItem
        throw new NotImplementedException("Utilisez les nouvelles méthodes avec SectionConteneurItem");

        return sectionConteneur;
    }

    public async Task<bool> RemoveSectionLibreAsync(int sectionConteneursId, int sectionLibreId)
    {
        var sectionConteneur = await GetByIdAsync(sectionConteneursId);
        // TODO: Refactorer cette méthode pour utiliser SectionConteneurItem
        throw new NotImplementedException("Utilisez RemoveSectionLibreItemAsync à la place");
    }

    public async Task<SectionConteneur> ReorderSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionConteneur = await GetByIdAsync(sectionConteneursId);

        for (int i = 0; i < sectionLibreIds.Count; i++)
        {
            var sectionLibre = await context.SectionsLibres.FindAsync(sectionLibreIds[i]).ConfigureAwait(false);
            if (sectionLibre != null)
            {
                sectionLibre.Ordre = i + 1;
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        _loggingService.LogInformation($"Ordre des sections réorganisé pour le conteneur {sectionConteneursId}");

        return await GetByIdAsync(sectionConteneursId);
    }

    public async Task<bool> CanCreateForTypeAsync(int documentGenereId, int typeSectionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var existing = await context.SectionsConteneurs
            .AnyAsync(sc => sc.DocumentGenereId == documentGenereId && sc.TypeSectionId == typeSectionId).ConfigureAwait(false);
        return !existing;
    }

    public async Task<bool> ValidateTypeConsistencyAsync(int sectionConteneursId, int sectionLibreId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var sectionConteneur = await context.SectionsConteneurs.FindAsync(sectionConteneursId).ConfigureAwait(false);
        var sectionLibre = await context.SectionsLibres.FindAsync(sectionLibreId).ConfigureAwait(false);

        if (sectionConteneur == null || sectionLibre == null)
            return false;

        return sectionConteneur.TypeSectionId == sectionLibre.TypeSectionId;
    }

    // Nouvelles méthodes pour gérer les SectionConteneurItem
    public async Task<SectionConteneurItem> AddSectionLibreWithOrderAsync(int sectionConteneursId, int sectionLibreId, int ordre)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (!await ValidateTypeConsistencyAsync(sectionConteneursId, sectionLibreId))
            throw new InvalidOperationException("La section libre n'est pas compatible avec le type du conteneur");

        // Vérifier si cette section n'est pas déjà dans le conteneur
        var existingItem = await context.SectionConteneurItems
            .FirstOrDefaultAsync(sci => sci.SectionConteneursId == sectionConteneursId && sci.SectionLibreId == sectionLibreId).ConfigureAwait(false);

        if (existingItem != null)
            throw new InvalidOperationException("Cette section est déjà présente dans le conteneur");

        var item = new SectionConteneurItem
        {
            SectionConteneursId = sectionConteneursId,
            SectionLibreId = sectionLibreId,
            Ordre = ordre,
            DateAjout = DateTime.Now
        };

        context.SectionConteneurItems.Add(item);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await context.Entry(item).Reference(i => i.SectionLibre).LoadAsync().ConfigureAwait(false);
        await context.Entry(item).Reference(i => i.SectionConteneur).LoadAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionLibre {sectionLibreId} ajoutée au conteneur {sectionConteneursId} avec ordre {ordre}");
        return item;
    }

    public async Task<List<SectionConteneurItem>> AddMultipleSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var items = new List<SectionConteneurItem>();

        // Calculer l'ordre de départ (prendre le maximum + 1)
        var maxOrder = await context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Select(sci => (int?)sci.Ordre)
            .MaxAsync().ConfigureAwait(false) ?? 0;

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
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var item = await context.SectionConteneurItems.FindAsync(sectionConteneurItemId).ConfigureAwait(false);
        if (item == null)
            return false;

        context.SectionConteneurItems.Remove(item);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneurItem {sectionConteneurItemId} supprimé");
        return true;
    }

    public async Task<List<SectionConteneurItem>> ReorderItemsAsync(int sectionConteneursId, List<int> itemIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var items = await context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Include(sci => sci.SectionLibre)
            .ToListAsync().ConfigureAwait(false);

        // Réordonner selon l'ordre fourni
        for (int i = 0; i < itemIds.Count; i++)
        {
            var item = items.FirstOrDefault(it => it.Id == itemIds[i]);
            if (item != null)
            {
                item.Ordre = i + 1;
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        _loggingService.LogInformation($"Ordre des items du conteneur {sectionConteneursId} mis à jour");

        return items.OrderBy(i => i.Ordre).ToList();
    }

    public async Task<List<SectionLibre>> GetAvailableSectionsForConteneurAsync(int sectionConteneursId, int typeSectionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // Récupérer les sections déjà utilisées dans ce conteneur
        var usedSectionIds = await context.SectionConteneurItems
            .Where(sci => sci.SectionConteneursId == sectionConteneursId)
            .Select(sci => sci.SectionLibreId)
            .ToListAsync().ConfigureAwait(false);

        // Retourner les sections du bon type qui ne sont pas déjà utilisées
        return await context.SectionsLibres
            .Where(sl => sl.TypeSectionId == typeSectionId && !usedSectionIds.Contains(sl.Id))
            .OrderBy(sl => sl.Titre)
            .ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Réorganise l'ordre d'un conteneur de sections
    /// </summary>
    /// <param name="conteneurId">ID du conteneur à déplacer</param>
    /// <param name="nouvelOrdre">Nouvel ordre souhaité</param>
    /// <returns>True si la réorganisation a réussi</returns>
    public async Task<bool> ReorderConteneurAsync(int conteneurId, int nouvelOrdre)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var conteneur = await context.SectionsConteneurs.FindAsync(conteneurId).ConfigureAwait(false);
        if (conteneur == null)
        {
            _loggingService.LogWarning($"Conteneur {conteneurId} introuvable pour réorganisation");
            return false;
        }

        var ancienOrdre = conteneur.Ordre;
        if (ancienOrdre == nouvelOrdre)
        {
            return true; // Aucun changement nécessaire
        }

        // Récupérer tous les conteneurs du même document pour réorganiser
        var autresConteneurs = await context.SectionsConteneurs
            .Where(sc => sc.DocumentGenereId == conteneur.DocumentGenereId && sc.Id != conteneurId)
            .ToListAsync().ConfigureAwait(false);

        // Décaler les autres conteneurs
        if (nouvelOrdre < ancienOrdre)
        {
            // Monter le conteneur : décaler vers le bas ceux entre nouvelOrdre et ancienOrdre
            foreach (var autreCont in autresConteneurs.Where(sc => sc.Ordre >= nouvelOrdre && sc.Ordre < ancienOrdre))
            {
                autreCont.Ordre += 1;
                autreCont.DateModification = DateTime.Now;
            }
        }
        else
        {
            // Descendre le conteneur : décaler vers le haut ceux entre ancienOrdre et nouvelOrdre
            foreach (var autreCont in autresConteneurs.Where(sc => sc.Ordre > ancienOrdre && sc.Ordre <= nouvelOrdre))
            {
                autreCont.Ordre -= 1;
                autreCont.DateModification = DateTime.Now;
            }
        }

        // Mettre à jour l'ordre du conteneur déplacé
        conteneur.Ordre = nouvelOrdre;
        conteneur.DateModification = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);
        _loggingService.LogInformation($"Conteneur {conteneurId} réorganisé de l'ordre {ancienOrdre} vers {nouvelOrdre}");

        return true;
    }

    /// <summary>
    /// Récupère un SectionConteneurItem par son ID avec ses relations
    /// </summary>
    public async Task<SectionConteneurItem> GetItemByIdAsync(int itemId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var item = await context.SectionConteneurItems
            .Include(i => i.SectionLibre)
            .ThenInclude(sl => sl.TypeSection)
            .Include(i => i.SectionConteneur)
            .FirstOrDefaultAsync(i => i.Id == itemId).ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"SectionConteneurItem {itemId} introuvable");

        return item;
    }

    /// <summary>
    /// Personnalise le contenu d'un item pour un document spécifique
    /// </summary>
    public async Task<SectionConteneurItem> PersonnaliserItemAsync(int itemId, string? titrePersonnalise, string contenuHtmlPersonnalise)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var item = await context.SectionConteneurItems
            .Include(i => i.SectionLibre)
            .FirstOrDefaultAsync(i => i.Id == itemId).ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"SectionConteneurItem {itemId} introuvable");

        // Mettre à jour les champs de personnalisation
        item.TitrePersonnalise = string.IsNullOrWhiteSpace(titrePersonnalise) ? null : titrePersonnalise;
        item.ContenuHtmlPersonnalise = contenuHtmlPersonnalise;
        item.DateModificationPersonnalisation = DateTime.Now;

        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneurItem {itemId} personnalisé pour le document");

        return item;
    }

    /// <summary>
    /// Réinitialise un item à son contenu par défaut (supprime la personnalisation)
    /// </summary>
    public async Task<SectionConteneurItem> ResetItemToDefaultAsync(int itemId)
    {
        using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var item = await context.SectionConteneurItems
            .Include(i => i.SectionLibre)
            .FirstOrDefaultAsync(i => i.Id == itemId).ConfigureAwait(false);

        if (item == null)
            throw new InvalidOperationException($"SectionConteneurItem {itemId} introuvable");

        // Réinitialiser les champs de personnalisation
        item.TitrePersonnalise = null;
        item.ContenuHtmlPersonnalise = null;
        item.DateModificationPersonnalisation = null;

        await context.SaveChangesAsync().ConfigureAwait(false);

        _loggingService.LogInformation($"SectionConteneurItem {itemId} réinitialisé au contenu par défaut");

        return item;
    }
}