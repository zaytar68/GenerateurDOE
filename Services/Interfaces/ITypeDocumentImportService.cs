using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface ITypeDocumentImportService
{
    Task<IEnumerable<TypeDocumentImport>> GetAllAsync();
    Task<IEnumerable<TypeDocumentImport>> GetActiveAsync();
    Task<TypeDocumentImport?> GetByIdAsync(int id);
    Task<TypeDocumentImport> CreateAsync(TypeDocumentImport typeDocument);
    Task<bool> UpdateAsync(TypeDocumentImport typeDocument);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> CanDeleteAsync(int id);
    Task<bool> ExistsAsync(string nom);
    Task InitializeDefaultTypesAsync();
}