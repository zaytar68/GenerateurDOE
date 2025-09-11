using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface ISectionConteneurService
{
    Task<SectionConteneur> CreateAsync(int documentGenereId, int typeSectionId, string? titre = null);
    Task<SectionConteneur> GetByIdAsync(int id);
    Task<SectionConteneur?> GetByDocumentAndTypeAsync(int documentGenereId, int typeSectionId);
    Task<IEnumerable<SectionConteneur>> GetByDocumentIdAsync(int documentGenereId);
    Task<SectionConteneur> UpdateAsync(SectionConteneur sectionConteneur);
    Task<bool> DeleteAsync(int id);
    
    // Méthodes pour gérer les SectionConteneurItem avec ordre et sélection
    Task<SectionConteneurItem> AddSectionLibreWithOrderAsync(int sectionConteneursId, int sectionLibreId, int ordre);
    Task<List<SectionConteneurItem>> AddMultipleSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds);
    Task<bool> RemoveSectionLibreItemAsync(int sectionConteneurItemId);
    Task<List<SectionConteneurItem>> ReorderItemsAsync(int sectionConteneursId, List<int> itemIds);
    Task<List<SectionLibre>> GetAvailableSectionsForConteneurAsync(int sectionConteneursId, int typeSectionId);
    
    // Méthodes héritées - gardées pour compatibilité temporaire
    [Obsolete("Utilisez AddSectionLibreWithOrderAsync à la place")]
    Task<SectionConteneur> AddSectionLibreAsync(int sectionConteneursId, int sectionLibreId);
    [Obsolete("Utilisez RemoveSectionLibreItemAsync à la place")]
    Task<bool> RemoveSectionLibreAsync(int sectionConteneursId, int sectionLibreId);
    [Obsolete("Utilisez ReorderItemsAsync à la place")]
    Task<SectionConteneur> ReorderSectionsLibresAsync(int sectionConteneursId, List<int> sectionLibreIds);
    
    Task<bool> CanCreateForTypeAsync(int documentGenereId, int typeSectionId);
    Task<bool> ValidateTypeConsistencyAsync(int sectionConteneursId, int sectionLibreId);
}