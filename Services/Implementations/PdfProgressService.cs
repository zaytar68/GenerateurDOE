using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;
using System.Collections.Concurrent;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service pour gérer la progression de génération PDF en temps réel
/// Utilise un ConcurrentDictionary pour le stockage thread-safe en mémoire
/// </summary>
public class PdfProgressService : IPdfProgressService
{
    private readonly ConcurrentDictionary<int, PdfGenerationProgress> _progressDict = new();
    private readonly ILoggingService _loggingService;

    public PdfProgressService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void InitializeProgress(int documentId)
    {
        var progress = new PdfGenerationProgress
        {
            DocumentId = documentId,
            CurrentStep = PdfGenerationStep.Initialisation,
            ProgressPercentage = 0,
            StepMessage = "Initialisation de la génération PDF...",
            LastUpdate = DateTime.Now,
            IsCompleted = false,
            HasError = false
        };

        _progressDict.AddOrUpdate(documentId, progress, (key, oldValue) => progress);

        _loggingService.LogInformation($"Progression PDF initialisée pour document {documentId}");
    }

    public void UpdateProgress(int documentId, PdfGenerationStep step, string? customMessage = null)
    {
        _progressDict.AddOrUpdate(documentId,
            // Si la clé n'existe pas, créer une nouvelle progression
            new PdfGenerationProgress
            {
                DocumentId = documentId
            },
            // Si la clé existe, mettre à jour la progression
            (key, existingProgress) =>
            {
                existingProgress.UpdateStep(step, customMessage);
                return existingProgress;
            });

        var message = customMessage ?? PdfGenerationProgress.GetStepLabel(step);
        var percentage = PdfGenerationProgress.GetStepPercentage(step);

        _loggingService.LogInformation($"PDF Progress - Document {documentId}: {percentage}% - {message}");
    }

    public void CompleteProgress(int documentId, string? successMessage = null)
    {
        UpdateProgress(documentId, PdfGenerationStep.Termine, successMessage ?? "PDF généré avec succès !");

        _loggingService.LogInformation($"Génération PDF terminée avec succès pour document {documentId}");
    }

    public void SetError(int documentId, string errorMessage)
    {
        _progressDict.AddOrUpdate(documentId,
            // Créer une nouvelle progression avec erreur si elle n'existe pas
            new PdfGenerationProgress
            {
                DocumentId = documentId,
                HasError = true,
                ErrorMessage = errorMessage,
                StepMessage = $"Erreur : {errorMessage}"
            },
            // Mettre à jour l'existante avec l'erreur
            (key, existingProgress) =>
            {
                existingProgress.SetError(errorMessage);
                return existingProgress;
            });

        _loggingService.LogError($"Erreur génération PDF pour document {documentId}: {errorMessage}");
    }

    public PdfGenerationProgress? GetProgress(int documentId)
    {
        _progressDict.TryGetValue(documentId, out var progress);
        return progress;
    }

    public void ClearProgress(int documentId)
    {
        _progressDict.TryRemove(documentId, out _);
        _loggingService.LogInformation($"Progression PDF nettoyée pour document {documentId}");
    }

    public bool IsGenerationInProgress(int documentId)
    {
        var progress = GetProgress(documentId);
        return progress != null && !progress.IsCompleted && !progress.HasError;
    }

    public void CleanupExpiredProgress(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.Now.Subtract(maxAge);
        var expiredKeys = _progressDict
            .Where(kvp => kvp.Value.LastUpdate < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _progressDict.TryRemove(key, out _);
        }

        if (expiredKeys.Any())
        {
            _loggingService.LogInformation($"Nettoyage automatique: {expiredKeys.Count} progressions expirées supprimées");
        }
    }

    /// <summary>
    /// Méthode utilitaire pour obtenir toutes les progressions en cours (pour debug/monitoring)
    /// </summary>
    public IEnumerable<PdfGenerationProgress> GetAllActiveProgress()
    {
        return _progressDict.Values
            .Where(p => !p.IsCompleted && !p.HasError)
            .ToList();
    }

    /// <summary>
    /// Méthode utilitaire pour obtenir des statistiques du service
    /// </summary>
    public (int Active, int Completed, int Failed) GetStatistics()
    {
        var active = _progressDict.Values.Count(p => !p.IsCompleted && !p.HasError);
        var completed = _progressDict.Values.Count(p => p.IsCompleted);
        var failed = _progressDict.Values.Count(p => p.HasError);

        return (active, completed, failed);
    }
}