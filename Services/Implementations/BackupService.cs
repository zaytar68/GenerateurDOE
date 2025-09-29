using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Concurrent;
using GenerateurDOE.Data;
using GenerateurDOE.Services.Interfaces;
using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Implementations;

public class BackupService : IBackupService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IConfigurationService _configurationService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BackupService> _logger;

    private static readonly ConcurrentDictionary<string, BackupStatus> _backupStatuses = new();
    private readonly string _tempBackupDirectory;

    public BackupService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IConfigurationService configurationService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<BackupService> logger)
    {
        _contextFactory = contextFactory;
        _configurationService = configurationService;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;

        _tempBackupDirectory = Path.Combine(_environment.WebRootPath, "temp", "backups");
        Directory.CreateDirectory(_tempBackupDirectory);
    }

    public async Task<BackupResult> CreateCompleteBackupAsync()
    {
        var backupId = Guid.NewGuid().ToString("N")[..12];
        var startTime = DateTime.UtcNow;

        var status = new BackupStatus
        {
            BackupId = backupId,
            State = BackupState.Starting,
            ProgressPercentage = 0,
            CurrentOperation = "Initialisation de la sauvegarde...",
            StartTime = startTime
        };
        _backupStatuses.TryAdd(backupId, status);

        try
        {
            _logger.LogInformation("Démarrage de la sauvegarde complète avec l'ID: {BackupId}", backupId);

            // Étape 1: Obtenir les paramètres de configuration
            var appSettings = await _configurationService.GetAppSettingsAsync().ConfigureAwait(false);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"GenerateurDOE_Backup_{timestamp}_{backupId}.zip";
            var backupFilePath = Path.Combine(_tempBackupDirectory, backupFileName);

            var contentInfo = new BackupContentInfo();
            var messages = new List<string>();

            // Étape 2: Sauvegarde de la base de données
            await UpdateStatusAsync(backupId, BackupState.BackingUpDatabase, 10,
                "Création de la sauvegarde de base de données...").ConfigureAwait(false);

            var sqlBackupPath = await CreateDatabaseBackupAsync(backupId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(sqlBackupPath))
            {
                var sqlFileInfo = new FileInfo(sqlBackupPath);
                contentInfo.DatabaseSizeMB = (int)(sqlFileInfo.Length / (1024 * 1024));
                messages.Add($"Sauvegarde SQL créée: {contentInfo.DatabaseSizeMB} MB");
            }

            // Étape 3: Analyser les dossiers et créer le ZIP
            await UpdateStatusAsync(backupId, BackupState.CompressingFiles, 30,
                "Compression des fichiers PDF et images...").ConfigureAwait(false);

            using var zipArchive = ZipFile.Open(backupFilePath, ZipArchiveMode.Create);

            // Ajouter le fichier SQL au ZIP
            if (!string.IsNullOrEmpty(sqlBackupPath) && File.Exists(sqlBackupPath))
            {
                zipArchive.CreateEntryFromFile(sqlBackupPath, Path.GetFileName(sqlBackupPath));
                messages.Add("Fichier SQL ajouté à l'archive");
            }

            // Ajouter les fichiers PDF
            await UpdateStatusAsync(backupId, BackupState.CompressingFiles, 50,
                "Compression des fichiers PDF...").ConfigureAwait(false);

            if (Directory.Exists(appSettings.RepertoireStockagePDF))
            {
                var pdfInfo = await AddDirectoryToZipAsync(zipArchive, appSettings.RepertoireStockagePDF,
                    "PDF", backupId).ConfigureAwait(false);
                contentInfo.PdfFilesCount = pdfInfo.FileCount;
                contentInfo.PdfFolderSizeMB = pdfInfo.SizeMB;
                messages.Add($"Fichiers PDF ajoutés: {pdfInfo.FileCount} fichiers ({pdfInfo.SizeMB} MB)");
            }

            // Ajouter les fichiers Images
            await UpdateStatusAsync(backupId, BackupState.CompressingFiles, 70,
                "Compression des images...").ConfigureAwait(false);

            if (Directory.Exists(appSettings.RepertoireStockageImages))
            {
                var imgInfo = await AddDirectoryToZipAsync(zipArchive, appSettings.RepertoireStockageImages,
                    "Images", backupId).ConfigureAwait(false);
                contentInfo.ImageFilesCount = imgInfo.FileCount;
                contentInfo.ImageFolderSizeMB = imgInfo.SizeMB;
                messages.Add($"Images ajoutées: {imgInfo.FileCount} fichiers ({imgInfo.SizeMB} MB)");
            }

            // Finalisation
            await UpdateStatusAsync(backupId, BackupState.Finalizing, 90,
                "Finalisation de la sauvegarde...").ConfigureAwait(false);

            contentInfo.TotalUncompressedSizeMB = contentInfo.DatabaseSizeMB +
                contentInfo.PdfFolderSizeMB + contentInfo.ImageFolderSizeMB;

            var duration = DateTime.UtcNow - startTime;
            var backupFileInfo = new FileInfo(backupFilePath);

            // Nettoyage du fichier SQL temporaire
            if (!string.IsNullOrEmpty(sqlBackupPath) && File.Exists(sqlBackupPath))
            {
                try { File.Delete(sqlBackupPath); } catch { }
            }

            await UpdateStatusAsync(backupId, BackupState.Completed, 100,
                "Sauvegarde terminée avec succès").ConfigureAwait(false);

            var result = new BackupResult
            {
                Success = true,
                BackupId = backupId,
                FileName = backupFileName,
                FilePath = backupFilePath,
                FileSizeBytes = backupFileInfo.Length,
                CreatedAt = startTime,
                Duration = duration,
                Messages = messages,
                ContentInfo = contentInfo
            };

            _logger.LogInformation("Sauvegarde complète terminée avec succès. ID: {BackupId}, Taille: {Size} MB, Durée: {Duration}s",
                backupId, backupFileInfo.Length / (1024 * 1024), duration.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de la sauvegarde {BackupId}", backupId);

            await UpdateStatusAsync(backupId, BackupState.Failed, 0,
                $"Erreur: {ex.Message}").ConfigureAwait(false);

            return new BackupResult
            {
                Success = false,
                BackupId = backupId,
                ErrorMessage = ex.Message,
                CreatedAt = startTime,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<string?> CreateDatabaseBackupAsync(string backupId)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var providerName = context.Database.ProviderName;
            var connectionString = context.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Chaîne de connexion vide pour la sauvegarde");
                return null;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (providerName?.Contains("SqlServer") == true)
            {
                return await CreateSqlServerBackupAsync(connectionString, timestamp, backupId).ConfigureAwait(false);
            }
            else if (providerName?.Contains("Npgsql") == true)
            {
                return await CreatePostgreSqlBackupAsync(connectionString, timestamp, backupId).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Provider de base de données non supporté: {Provider}", providerName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde de la base de données");
            return null;
        }
    }

    private async Task<string?> CreateSqlServerBackupAsync(string connectionString, string timestamp, string backupId)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            var backupFilePath = Path.Combine(_tempBackupDirectory, $"GenerateurDOE_DB_{timestamp}.bak");
            var sqlBackupPath = Path.Combine(_tempBackupDirectory, $"GenerateurDOE_DB_{timestamp}.sql");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            // Créer un backup natif SQL Server d'abord
            var backupCommand = $@"
                BACKUP DATABASE [{databaseName}]
                TO DISK = N'{backupFilePath}'
                WITH FORMAT, INIT,
                NAME = N'GenerateurDOE-Full Database Backup',
                SKIP, NOREWIND, NOUNLOAD, STATS = 10";

            using var cmd = new SqlCommand(backupCommand, connection);
            cmd.CommandTimeout = 300; // 5 minutes
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            // Ensuite créer un script SQL pour plus de portabilité
            await CreateSqlScriptFromDatabaseAsync(connection, databaseName, sqlBackupPath, backupId).ConfigureAwait(false);

            // Supprimer le fichier .bak et garder seulement le .sql
            if (File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
            }

            return sqlBackupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde SQL Server");
            throw;
        }
    }

    private async Task CreateSqlScriptFromDatabaseAsync(SqlConnection connection, string databaseName, string outputPath, string backupId)
    {
        try
        {
            using var writer = new StreamWriter(outputPath);
            await writer.WriteLineAsync($"-- Sauvegarde de la base de données {databaseName}").ConfigureAwait(false);
            await writer.WriteLineAsync($"-- Générée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}").ConfigureAwait(false);
            await writer.WriteLineAsync($"-- ID de sauvegarde: {backupId}").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            await writer.WriteLineAsync($"USE [{databaseName}];").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            // Obtenir la liste des tables
            var getTablesQuery = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            using var tablesCmd = new SqlCommand(getTablesQuery, connection);
            using var tablesReader = await tablesCmd.ExecuteReaderAsync().ConfigureAwait(false);

            var tables = new List<string>();
            while (await tablesReader.ReadAsync().ConfigureAwait(false))
            {
                tables.Add(tablesReader.GetString(0));
            }
            tablesReader.Close();

            // Pour chaque table, générer les INSERT statements
            foreach (var tableName in tables)
            {
                await writer.WriteLineAsync($"-- Données de la table {tableName}").ConfigureAwait(false);

                var selectQuery = $"SELECT * FROM [{tableName}]";
                using var dataCmd = new SqlCommand(selectQuery, connection);
                using var dataReader = await dataCmd.ExecuteReaderAsync().ConfigureAwait(false);

                if (dataReader.HasRows)
                {
                    var columnNames = new List<string>();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        columnNames.Add(dataReader.GetName(i));
                    }

                    while (await dataReader.ReadAsync().ConfigureAwait(false))
                    {
                        var values = new List<string>();
                        for (int i = 0; i < dataReader.FieldCount; i++)
                        {
                            var value = dataReader.IsDBNull(i) ? "NULL" : $"'{dataReader.GetValue(i).ToString()?.Replace("'", "''")}'";
                            values.Add(value);
                        }

                        await writer.WriteLineAsync($"INSERT INTO [{tableName}] ({string.Join(", ", columnNames.Select(c => $"[{c}]"))}) VALUES ({string.Join(", ", values)});").ConfigureAwait(false);
                    }
                }

                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération du script SQL");
            throw;
        }
    }

    private async Task<string?> CreatePostgreSqlBackupAsync(string connectionString, string timestamp, string backupId)
    {
        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            var host = builder.Host;
            var port = builder.Port;
            var username = builder.Username;
            var password = builder.Password;

            var backupFilePath = Path.Combine(_tempBackupDirectory, $"GenerateurDOE_DB_{timestamp}.sql");

            // D'abord essayer pg_dump si disponible
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = $"--host={host} --port={port} --username={username} --dbname={databaseName} --verbose --clean --no-owner --no-acl --format=plain",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                processInfo.Environment["PGPASSWORD"] = password;

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Impossible de démarrer pg_dump");
                }

                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

                await process.WaitForExitAsync().ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Erreur pg_dump: {error}");
                }

                // Ajouter en-tête au fichier SQL PostgreSQL pour cohérence
                var sqlHeader = $@"-- Sauvegarde PostgreSQL de la base de données {databaseName}
-- Générée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}
-- ID de sauvegarde: {backupId}
-- Commande: pg_dump --host={host} --port={port} --username={username} --dbname={databaseName}

{output}";

                await File.WriteAllTextAsync(backupFilePath, sqlHeader).ConfigureAwait(false);

                _logger.LogInformation("Sauvegarde PostgreSQL créée avec pg_dump: {BackupPath}, Taille: {Size} KB",
                    backupFilePath, new FileInfo(backupFilePath).Length / 1024);

                return backupFilePath;
            }
            catch (Exception pgDumpEx)
            {
                _logger.LogWarning(pgDumpEx, "pg_dump non disponible, utilisation de la méthode de fallback pour PostgreSQL");

                // Méthode de fallback : extraction manuelle des données via Npgsql
                return await CreatePostgreSqlBackupManualAsync(connectionString, timestamp, backupId, backupFilePath).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde PostgreSQL");
            throw;
        }
    }

    private async Task<string?> CreatePostgreSqlBackupManualAsync(string connectionString, string timestamp, string backupId, string backupFilePath)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            var databaseName = connection.Database;

            using var writer = new StreamWriter(backupFilePath);
            await writer.WriteLineAsync($"-- Sauvegarde PostgreSQL de la base de données {databaseName}").ConfigureAwait(false);
            await writer.WriteLineAsync($"-- Générée le {DateTime.Now:yyyy-MM-dd HH:mm:ss}").ConfigureAwait(false);
            await writer.WriteLineAsync($"-- ID de sauvegarde: {backupId}").ConfigureAwait(false);
            await writer.WriteLineAsync($"-- Méthode: Extraction manuelle via Npgsql (pg_dump non disponible)").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);

            // Obtenir la liste des tables
            var getTablesQuery = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                AND table_schema = 'public'
                ORDER BY table_name";

            using var tablesCmd = new Npgsql.NpgsqlCommand(getTablesQuery, connection);
            using var tablesReader = await tablesCmd.ExecuteReaderAsync().ConfigureAwait(false);

            var tables = new List<string>();
            while (await tablesReader.ReadAsync().ConfigureAwait(false))
            {
                tables.Add(tablesReader.GetString(0));
            }
            tablesReader.Close();

            // Pour chaque table, générer les INSERT statements
            foreach (var tableName in tables)
            {
                await writer.WriteLineAsync($"-- Données de la table {tableName}").ConfigureAwait(false);

                var selectQuery = $"SELECT * FROM \"{tableName}\"";
                using var dataCmd = new Npgsql.NpgsqlCommand(selectQuery, connection);
                using var dataReader = await dataCmd.ExecuteReaderAsync().ConfigureAwait(false);

                if (dataReader.HasRows)
                {
                    var columnNames = new List<string>();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        columnNames.Add(dataReader.GetName(i));
                    }

                    while (await dataReader.ReadAsync().ConfigureAwait(false))
                    {
                        var values = new List<string>();
                        for (int i = 0; i < dataReader.FieldCount; i++)
                        {
                            if (dataReader.IsDBNull(i))
                            {
                                values.Add("NULL");
                            }
                            else
                            {
                                var value = dataReader.GetValue(i);
                                var formattedValue = value switch
                                {
                                    string s => $"'{s.Replace("'", "''")}'",
                                    DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                                    bool b => b ? "TRUE" : "FALSE",
                                    _ => $"'{value.ToString()?.Replace("'", "''") ?? ""}'"
                                };
                                values.Add(formattedValue);
                            }
                        }

                        await writer.WriteLineAsync($"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ({string.Join(", ", values)});").ConfigureAwait(false);
                    }
                }

                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            _logger.LogInformation("Sauvegarde PostgreSQL manuelle créée: {BackupPath}, Taille: {Size} KB",
                backupFilePath, new FileInfo(backupFilePath).Length / 1024);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde PostgreSQL manuelle");
            throw;
        }
    }

    private async Task<(int FileCount, long SizeMB)> AddDirectoryToZipAsync(ZipArchive archive, string directoryPath,
        string archiveFolderName, string backupId)
    {
        int fileCount = 0;
        long totalSize = 0;

        if (!Directory.Exists(directoryPath))
        {
            return (0, 0);
        }

        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(directoryPath, filePath);
                var entryName = Path.Combine(archiveFolderName, relativePath).Replace('\\', '/');

                archive.CreateEntryFromFile(filePath, entryName);

                fileCount++;
                totalSize += fileInfo.Length;

                if (fileCount % 50 == 0)
                {
                    await Task.Delay(1).ConfigureAwait(false); // Permettre d'autres tâches
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Impossible d'ajouter le fichier {FilePath} à l'archive: {Error}", filePath, ex.Message);
            }
        }

        return (fileCount, totalSize / (1024 * 1024));
    }

    private async Task UpdateStatusAsync(string backupId, BackupState state, int progress, string operation)
    {
        if (_backupStatuses.TryGetValue(backupId, out var status))
        {
            status.State = state;
            status.ProgressPercentage = progress;
            status.CurrentOperation = operation;
            _backupStatuses.TryUpdate(backupId, status, status);
        }

        await Task.Delay(1).ConfigureAwait(false);
    }

    public async Task<BackupStatus> GetBackupStatusAsync(string backupId)
    {
        await Task.Delay(1).ConfigureAwait(false);

        return _backupStatuses.TryGetValue(backupId, out var status)
            ? status
            : new BackupStatus { BackupId = backupId, State = BackupState.Failed, ErrorMessage = "Sauvegarde non trouvée" };
    }

    public async Task<bool> CleanupOldBackupsAsync(TimeSpan maxAge)
    {
        try
        {
            await Task.Delay(1).ConfigureAwait(false);

            if (!Directory.Exists(_tempBackupDirectory))
                return true;

            var cutoffTime = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(_tempBackupDirectory, "*.zip");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffTime)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation("Ancien fichier de sauvegarde supprimé: {FileName}", fileInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Impossible de supprimer le fichier {FileName}: {Error}", fileInfo.Name, ex.Message);
                    }
                }
            }

            // Nettoyer les statuts en mémoire
            var expiredStatuses = _backupStatuses.Where(kvp =>
                DateTime.UtcNow - kvp.Value.StartTime > maxAge).ToList();

            foreach (var kvp in expiredStatuses)
            {
                _backupStatuses.TryRemove(kvp.Key, out _);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du nettoyage des sauvegardes");
            return false;
        }
    }

    public async Task<string> GetBackupFilePathAsync(string backupId)
    {
        await Task.Delay(1).ConfigureAwait(false);

        // Rechercher le fichier avec le backupId dans le nom
        if (!Directory.Exists(_tempBackupDirectory))
            return string.Empty;

        var files = Directory.GetFiles(_tempBackupDirectory, "*.zip");

        // Maintenant que l'ID est inclus dans le nom, la recherche est directe
        return files.FirstOrDefault(f => Path.GetFileName(f).Contains(backupId)) ?? string.Empty;
    }
}