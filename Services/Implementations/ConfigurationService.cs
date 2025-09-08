using Microsoft.Extensions.Options;
using System.Text.Json;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly IWebHostEnvironment _environment;

    public ConfigurationService(IConfiguration configuration, IOptionsMonitor<AppSettings> appSettings, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _appSettings = appSettings;
        _environment = environment;
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        await Task.Delay(1);
        return _appSettings.CurrentValue;
    }

    public async Task<bool> UpdateAppSettingsAsync(AppSettings appSettings)
    {
        try
        {
            var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            
            if (!File.Exists(appSettingsPath))
                return false;

            var json = await File.ReadAllTextAsync(appSettingsPath);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement.Clone();

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "AppSettings")
                        {
                            writer.WritePropertyName("AppSettings");
                            writer.WriteStartObject();
                            writer.WriteString("RepertoireStockagePDF", appSettings.RepertoireStockagePDF);
                            writer.WriteString("RepertoireStockageImages", appSettings.RepertoireStockageImages);
                            writer.WriteString("NomSociete", appSettings.NomSociete);
                            writer.WriteString("TailleMaxFichierPDF", appSettings.TailleMaxFichierPDF);
                            writer.WriteEndObject();
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                await File.WriteAllTextAsync(appSettingsPath, updatedJson);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateDirectoryAsync(string directoryPath)
    {
        await Task.Delay(1);
        
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            return Path.IsPathRooted(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateDirectoryAsync(string directoryPath)
    {
        await Task.Delay(1);
        
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TestDirectoryWriteAccessAsync(string directoryPath)
    {
        await Task.Delay(1);
        
        try
        {
            if (!Directory.Exists(directoryPath))
                return false;

            var testFilePath = Path.Combine(directoryPath, $"test_{Guid.NewGuid()}.tmp");
            
            await File.WriteAllTextAsync(testFilePath, "test");
            
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CustomDirectoryInfo> GetDirectoryInfoAsync(string directoryPath)
    {
        await Task.Delay(1);
        
        var dirInfo = new CustomDirectoryInfo
        {
            Path = directoryPath
        };

        try
        {
            if (Directory.Exists(directoryPath))
            {
                dirInfo.Exists = true;
                
                var driveInfo = new DriveInfo(Path.GetPathRoot(directoryPath)!);
                if (driveInfo.IsReady)
                {
                    dirInfo.AvailableSpace = driveInfo.AvailableFreeSpace;
                }

                dirInfo.HasWriteAccess = await TestDirectoryWriteAccessAsync(directoryPath);
            }
            else
            {
                dirInfo.Exists = false;
                dirInfo.HasWriteAccess = false;
                dirInfo.ErrorMessage = "Le répertoire n'existe pas";
            }
        }
        catch (Exception ex)
        {
            dirInfo.Exists = false;
            dirInfo.HasWriteAccess = false;
            dirInfo.ErrorMessage = ex.Message;
        }

        return dirInfo;
    }
}