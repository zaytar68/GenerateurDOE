using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class FileExplorerService : IFileExplorerService
{
    public async Task<List<FolderItem>> GetDirectoriesAsync(string path)
    {
        await Task.Delay(1);
        
        var items = new List<FolderItem>();

        try
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return items;

            // Ajouter le dossier parent si ce n'est pas la racine
            if (!IsRootPath(path))
            {
                var parentPath = GetParentDirectory(path);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    items.Add(new FolderItem
                    {
                        Name = "..",
                        FullPath = parentPath,
                        IsDirectory = true,
                        IsParentDirectory = true
                    });
                }
            }

            // Ajouter les dossiers
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories.OrderBy(d => Path.GetFileName(d)))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(directory);
                    var hasSubDirs = dirInfo.GetDirectories().Length > 0;

                    items.Add(new FolderItem
                    {
                        Name = dirInfo.Name,
                        FullPath = dirInfo.FullName,
                        IsDirectory = true,
                        LastModified = dirInfo.LastWriteTime,
                        HasSubDirectories = hasSubDirs
                    });
                }
                catch
                {
                    // Ignorer les dossiers inaccessibles
                }
            }
        }
        catch
        {
            // En cas d'erreur, retourner une liste vide
        }

        return items;
    }

    public async Task<List<FolderItem>> GetDrivesAsync()
    {
        await Task.Delay(1);
        
        var drives = new List<FolderItem>();

        try
        {
            var allDrives = DriveInfo.GetDrives();
            foreach (var drive in allDrives.Where(d => d.IsReady))
            {
                var driveName = $"{drive.Name} ({drive.DriveType})";
                if (drive.DriveType == DriveType.Fixed && !string.IsNullOrEmpty(drive.VolumeLabel))
                {
                    driveName = $"{drive.Name} {drive.VolumeLabel} ({FormatBytes(drive.TotalSize)})";
                }

                drives.Add(new FolderItem
                {
                    Name = driveName,
                    FullPath = drive.RootDirectory.FullName,
                    IsDirectory = true,
                    HasSubDirectories = true
                });
            }
        }
        catch
        {
            // En cas d'erreur, ajouter au moins C:
            drives.Add(new FolderItem
            {
                Name = "C:\\ (Local Disk)",
                FullPath = "C:\\",
                IsDirectory = true,
                HasSubDirectories = true
            });
        }

        return drives.OrderBy(d => d.Name).ToList();
    }

    public async Task<bool> DirectoryExistsAsync(string path)
    {
        await Task.Delay(1);
        
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public async Task<FolderItem?> GetDirectoryInfoAsync(string path)
    {
        await Task.Delay(1);
        
        try
        {
            if (!Directory.Exists(path))
                return null;

            var dirInfo = new DirectoryInfo(path);
            return new FolderItem
            {
                Name = dirInfo.Name,
                FullPath = dirInfo.FullName,
                IsDirectory = true,
                LastModified = dirInfo.LastWriteTime,
                HasSubDirectories = dirInfo.GetDirectories().Length > 0
            };
        }
        catch
        {
            return null;
        }
    }

    public string GetParentDirectory(string path)
    {
        try
        {
            var parentDir = Directory.GetParent(path);
            return parentDir?.FullName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool IsValidPath(string path)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path);
        }
        catch
        {
            return false;
        }
    }

    private bool IsRootPath(string path)
    {
        try
        {
            var rootPath = Path.GetPathRoot(path);
            return string.Equals(path.TrimEnd('\\'), rootPath?.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double value = bytes;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{value:N1} {suffixes[suffixIndex]}";
    }
}