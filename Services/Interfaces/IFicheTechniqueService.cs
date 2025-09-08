using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IFicheTechniqueService
{
    Task<IEnumerable<FicheTechnique>> GetAllAsync();
    Task<FicheTechnique?> GetByIdAsync(int id);
    Task<IEnumerable<FicheTechnique>> GetByChantierId(int chantierId);
    Task<FicheTechnique> CreateAsync(FicheTechnique ficheTechnique);
    Task<FicheTechnique> UpdateAsync(FicheTechnique ficheTechnique);
    Task<bool> DeleteAsync(int id);
    Task<ImportPDF> AddPDFAsync(int ficheTechniqueId, ImportPDF importPDF);
    Task<bool> RemovePDFAsync(int importPDFId);
    Task<string> SavePDFFileAsync(Stream fileStream, string originalFileName);
}