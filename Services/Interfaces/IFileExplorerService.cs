using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IFileExplorerService
{
    Task<List<FolderItem>> GetDirectoriesAsync(string path);
    Task<List<FolderItem>> GetDrivesAsync();
    Task<bool> DirectoryExistsAsync(string path);
    Task<FolderItem?> GetDirectoryInfoAsync(string path);
    string GetParentDirectory(string path);
    bool IsValidPath(string path);
}