using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IBackupService
{
    Task<BackupResult> CreateCompleteBackupAsync(BackupType backupType = BackupType.Complete);
    Task<BackupStatus> GetBackupStatusAsync(string backupId);
    Task<bool> CleanupOldBackupsAsync(TimeSpan maxAge);
    Task<string> GetBackupFilePathAsync(string backupId);
}

/// <summary>
/// Type de sauvegarde de base de données
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Sauvegarde complète : structure (DDL) + données (DML)
    /// </summary>
    Complete,

    /// <summary>
    /// Structure uniquement : CREATE TABLE, indexes, contraintes, etc.
    /// </summary>
    SchemaOnly,

    /// <summary>
    /// Données uniquement : INSERT statements sans structure
    /// </summary>
    DataOnly
}

public class BackupResult
{
    public bool Success { get; set; }
    public string BackupId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Messages { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public BackupContentInfo ContentInfo { get; set; } = new();
}

public class BackupContentInfo
{
    public int DatabaseSizeMB { get; set; }
    public int PdfFilesCount { get; set; }
    public long PdfFolderSizeMB { get; set; }
    public int ImageFilesCount { get; set; }
    public long ImageFolderSizeMB { get; set; }
    public long TotalUncompressedSizeMB { get; set; }
}

public class BackupStatus
{
    public string BackupId { get; set; } = string.Empty;
    public BackupState State { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCompleted => State == BackupState.Completed || State == BackupState.Failed;
}

public enum BackupState
{
    Starting,
    BackingUpDatabase,
    CompressingFiles,
    Finalizing,
    Completed,
    Failed
}