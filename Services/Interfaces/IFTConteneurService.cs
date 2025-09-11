using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IFTConteneurService
{
    Task<FTConteneur> CreateAsync(int documentGenereId, string? titre = null);
    Task<FTConteneur> GetByIdAsync(int id);
    Task<FTConteneur?> GetByDocumentIdAsync(int documentGenereId);
    Task<FTConteneur> UpdateAsync(FTConteneur ftConteneur);
    Task<bool> DeleteAsync(int id);
    
    Task<FTElement> AddFTElementAsync(int ftConteneursId, int ficheTechniqueId, string positionMarche, int? importPDFId = null);
    Task<bool> RemoveFTElementAsync(int ftElementId);
    Task<FTConteneur> ReorderFTElementsAsync(int ftConteneursId, List<int> ftElementIds);
    Task<FTElement> UpdateFTElementAsync(FTElement ftElement);
    
    Task<string> GenerateTableauRecapitulatifHtmlAsync(int ftConteneursId);
    Task<FTConteneur> CalculatePageNumbersAsync(int ftConteneursId);
    
    Task<bool> CanCreateForDocumentAsync(int documentGenereId);
    Task<IEnumerable<FicheTechnique>> GetAvailableFichesTechniquesAsync(int documentGenereId);
}