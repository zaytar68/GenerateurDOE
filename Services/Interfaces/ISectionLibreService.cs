using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface ISectionLibreService
{
    Task<IEnumerable<SectionLibre>> GetAllAsync();
    Task<IEnumerable<SectionLibre>> GetByTypeSectionAsync(int typeSectionId);
    Task<SectionLibre?> GetByIdAsync(int id);
    Task<SectionLibre> CreateAsync(SectionLibre sectionLibre);
    Task<SectionLibre> UpdateAsync(SectionLibre sectionLibre);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> ReorderAsync(int sectionId, int newOrder);
    Task<IEnumerable<SectionLibre>> GetOrderedSectionsAsync();
    Task<int> GetNextOrderAsync();
}