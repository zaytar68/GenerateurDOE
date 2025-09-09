using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface ITypeProduitService
{
    Task<IEnumerable<TypeProduit>> GetAllAsync();
    Task<IEnumerable<TypeProduit>> GetActiveAsync();
    Task<TypeProduit?> GetByIdAsync(int id);
    Task<TypeProduit> CreateAsync(TypeProduit typeProduit);
    Task<bool> UpdateAsync(TypeProduit typeProduit);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> CanDeleteAsync(int id);
    Task<bool> ExistsAsync(string nom);
    Task InitializeDefaultTypesAsync();
}