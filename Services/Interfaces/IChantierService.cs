using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IChantierService
{
    Task<List<Chantier>> GetAllAsync(bool includeArchived = false);
    Task<List<Chantier>> SearchAsync(string searchTerm, bool includeArchived = false);
    Task<Chantier?> GetByIdAsync(int id);
    Task<Chantier?> GetByIdWithDocumentsAsync(int id);
    Task<Chantier> CreateAsync(Chantier chantier);
    Task<Chantier> UpdateAsync(Chantier chantier);
    Task DeleteAsync(int id);
    Task<Chantier> ArchiveAsync(int id);
    Task<Chantier> UnarchiveAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<List<Chantier>> GetRecentAsync(int count = 5, bool includeArchived = false);
    Task<int> CountAsync(bool includeArchived = false);
}