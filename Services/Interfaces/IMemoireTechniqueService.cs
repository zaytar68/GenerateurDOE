using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IMemoireTechniqueService
{
    Task<IEnumerable<Methode>> GetAllMethodesAsync();
    Task<Methode?> GetMethodeByIdAsync(int id);
    Task<Methode> CreateMethodeAsync(Methode methode);
    Task<Methode> UpdateMethodeAsync(Methode methode);
    Task<bool> DeleteMethodeAsync(int id);
    Task<ImageMethode> AddImageToMethodeAsync(int methodeId, ImageMethode imageMethode);
    Task<bool> RemoveImageFromMethodeAsync(int imageId);
    Task<string> SaveImageFileAsync(Stream fileStream, string originalFileName);
    Task<IEnumerable<Methode>> GetMethodesOrderedAsync();
    Task UpdateMethodesOrderAsync(IEnumerable<(int id, int ordre)> ordres);
}