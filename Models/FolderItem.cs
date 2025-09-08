namespace GenerateurDOE.Models;

public class FolderItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public DateTime LastModified { get; set; }
    public long Size { get; set; }
    public bool IsParentDirectory { get; set; }
    public bool HasSubDirectories { get; set; }
    public bool IsExpanded { get; set; }
    public List<FolderItem> Children { get; set; } = new List<FolderItem>();
}