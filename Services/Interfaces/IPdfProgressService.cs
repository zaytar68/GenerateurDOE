using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

/// <summary>
/// Service pour gérer la progression de génération PDF en temps réel
/// </summary>
public interface IPdfProgressService
{
    /// <summary>
    /// Initialise le suivi de progression pour un document
    /// </summary>
    void InitializeProgress(int documentId);

    /// <summary>
    /// Met à jour la progression d'un document
    /// </summary>
    void UpdateProgress(int documentId, PdfGenerationStep step, string? customMessage = null);

    /// <summary>
    /// Marque la génération comme terminée avec succès
    /// </summary>
    void CompleteProgress(int documentId, string? successMessage = null);

    /// <summary>
    /// Marque la génération comme échouée
    /// </summary>
    void SetError(int documentId, string errorMessage);

    /// <summary>
    /// Récupère l'état actuel de la progression
    /// </summary>
    PdfGenerationProgress? GetProgress(int documentId);

    /// <summary>
    /// Supprime le suivi de progression (nettoyage)
    /// </summary>
    void ClearProgress(int documentId);

    /// <summary>
    /// Vérifie si une génération est en cours pour ce document
    /// </summary>
    bool IsGenerationInProgress(int documentId);

    /// <summary>
    /// Nettoyage automatique des progressions expirées
    /// </summary>
    void CleanupExpiredProgress(TimeSpan maxAge);
}