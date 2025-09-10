using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface ITypeSectionService
{
    Task<IEnumerable<TypeSection>> GetAllAsync();
    Task<IEnumerable<TypeSection>> GetActiveAsync();
    Task<TypeSection?> GetByIdAsync(int id);
    Task<TypeSection> CreateAsync(TypeSection typeSection);
    Task<TypeSection> UpdateAsync(TypeSection typeSection);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> HasSectionsAsync(int id);
    Task InitializeDefaultTypesAsync();
}