using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IConfigurationService
{
    Task<AppSettings> GetAppSettingsAsync();
    Task<bool> UpdateAppSettingsAsync(AppSettings appSettings);
    Task<bool> ValidateDirectoryAsync(string directoryPath);
    Task<bool> CreateDirectoryAsync(string directoryPath);
    Task<bool> TestDirectoryWriteAccessAsync(string directoryPath);
    Task<CustomDirectoryInfo> GetDirectoryInfoAsync(string directoryPath);
}

public class CustomDirectoryInfo
{
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool HasWriteAccess { get; set; }
    public long AvailableSpace { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}