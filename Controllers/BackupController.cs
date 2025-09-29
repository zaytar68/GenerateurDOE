using Microsoft.AspNetCore.Mvc;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Controllers;

[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            _logger.LogInformation("Demande de création de sauvegarde complète initiée");

            var result = await _backupService.CreateCompleteBackupAsync();

            if (result.Success)
            {
                _logger.LogInformation("Sauvegarde créée avec succès. ID: {BackupId}, Taille: {Size} MB",
                    result.BackupId, result.FileSizeBytes / (1024 * 1024));

                return Ok(new
                {
                    success = true,
                    backupId = result.BackupId,
                    fileName = result.FileName,
                    fileSizeBytes = result.FileSizeBytes,
                    fileSizeMB = Math.Round(result.FileSizeBytes / (1024.0 * 1024.0), 2),
                    duration = result.Duration.TotalSeconds,
                    createdAt = result.CreatedAt,
                    messages = result.Messages,
                    contentInfo = new
                    {
                        databaseSizeMB = result.ContentInfo.DatabaseSizeMB,
                        pdfFilesCount = result.ContentInfo.PdfFilesCount,
                        pdfFolderSizeMB = result.ContentInfo.PdfFolderSizeMB,
                        imageFilesCount = result.ContentInfo.ImageFilesCount,
                        imageFolderSizeMB = result.ContentInfo.ImageFolderSizeMB,
                        totalUncompressedSizeMB = result.ContentInfo.TotalUncompressedSizeMB
                    },
                    downloadUrl = $"/api/backup/download/{result.BackupId}"
                });
            }
            else
            {
                _logger.LogError("Échec de la création de sauvegarde: {Error}", result.ErrorMessage);
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    backupId = result.BackupId,
                    duration = result.Duration.TotalSeconds
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de sauvegarde");
            return StatusCode(500, new
            {
                success = false,
                error = "Erreur interne du serveur lors de la création de sauvegarde"
            });
        }
    }

    [HttpGet("status/{backupId}")]
    public async Task<IActionResult> GetBackupStatus(string backupId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupId))
            {
                return BadRequest(new { error = "ID de sauvegarde requis" });
            }

            var status = await _backupService.GetBackupStatusAsync(backupId);

            return Ok(new
            {
                backupId = status.BackupId,
                state = status.State.ToString(),
                progressPercentage = status.ProgressPercentage,
                currentOperation = status.CurrentOperation,
                startTime = status.StartTime,
                isCompleted = status.IsCompleted,
                errorMessage = status.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du statut de sauvegarde {BackupId}", backupId);
            return StatusCode(500, new { error = "Erreur interne du serveur" });
        }
    }

    [HttpGet("download/{backupId}")]
    public async Task<IActionResult> DownloadBackup(string backupId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupId))
            {
                _logger.LogWarning("Tentative de téléchargement avec ID de sauvegarde vide");
                return BadRequest("ID de sauvegarde requis");
            }

            _logger.LogInformation("Demande de téléchargement de sauvegarde avec l'ID: {BackupId}", backupId);

            var filePath = await _backupService.GetBackupFilePathAsync(backupId);

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Fichier de sauvegarde non trouvé pour l'ID: {BackupId}", backupId);
                return NotFound(new { error = "Fichier de sauvegarde non trouvé" });
            }

            // Vérification basique du chemin (sécurité)
            if (filePath.Contains("..") || !filePath.Contains("temp/backups"))
            {
                _logger.LogWarning("Chemin de fichier suspect: {FilePath}", filePath);
                return BadRequest(new { error = "Chemin de fichier non valide" });
            }

            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);

            _logger.LogInformation("Téléchargement de sauvegarde démarré: {FileName}, Taille: {Size} MB",
                fileName, fileInfo.Length / (1024 * 1024));

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Améliorer les en-têtes pour forcer le téléchargement
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            Response.Headers.Add("Content-Type", "application/octet-stream");

            // Programmation de la suppression immédiate après l'envoi de la réponse
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // Attendre 10 secondes pour s'assurer que le téléchargement est terminé
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Fichier de sauvegarde temporaire supprimé après téléchargement: {FileName}", fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossible de supprimer le fichier temporaire: {FileName}", fileName);
                }
            });

            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement de la sauvegarde {BackupId}", backupId);
            return StatusCode(500, new { error = "Erreur interne du serveur" });
        }
    }

    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupOldBackups([FromQuery] int maxAgeHours = 24)
    {
        try
        {
            if (maxAgeHours < 1 || maxAgeHours > 168) // Entre 1h et 1 semaine
            {
                return BadRequest(new { error = "L'âge maximum doit être entre 1 et 168 heures" });
            }

            _logger.LogInformation("Démarrage du nettoyage des sauvegardes > {MaxAge}h", maxAgeHours);

            var success = await _backupService.CleanupOldBackupsAsync(TimeSpan.FromHours(maxAgeHours));

            if (success)
            {
                _logger.LogInformation("Nettoyage des sauvegardes terminé avec succès");
                return Ok(new { success = true, message = "Nettoyage terminé avec succès" });
            }
            else
            {
                return StatusCode(500, new { success = false, error = "Erreur lors du nettoyage" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du nettoyage des sauvegardes");
            return StatusCode(500, new { success = false, error = "Erreur interne du serveur" });
        }
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetBackupInfo()
    {
        try
        {
            await Task.Delay(1); // Async pour cohérence

            // Informations générales sur les sauvegardes disponibles
            var tempBackupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "backups");

            if (!Directory.Exists(tempBackupDirectory))
            {
                return Ok(new
                {
                    availableBackups = 0,
                    totalSizeMB = 0,
                    oldestBackup = (DateTime?)null,
                    newestBackup = (DateTime?)null
                });
            }

            var backupFiles = Directory.GetFiles(tempBackupDirectory, "*.zip");
            var totalSize = backupFiles.Sum(f => new FileInfo(f).Length);

            var backupDates = backupFiles
                .Select(f => new FileInfo(f).CreationTime)
                .OrderBy(d => d)
                .ToList();

            return Ok(new
            {
                availableBackups = backupFiles.Length,
                totalSizeMB = Math.Round(totalSize / (1024.0 * 1024.0), 2),
                oldestBackup = backupDates.FirstOrDefault(),
                newestBackup = backupDates.LastOrDefault(),
                files = backupFiles.Select(f =>
                {
                    var fileInfo = new FileInfo(f);
                    return new
                    {
                        name = fileInfo.Name,
                        sizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2),
                        createdAt = fileInfo.CreationTime
                    };
                }).OrderByDescending(f => f.createdAt)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des informations de sauvegarde");
            return StatusCode(500, new { error = "Erreur interne du serveur" });
        }
    }
}