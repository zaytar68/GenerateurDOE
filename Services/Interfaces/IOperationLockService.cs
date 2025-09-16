using System;
using System.Threading.Tasks;

namespace GenerateurDOE.Services.Interfaces;

/// <summary>
/// Service de protection anti-concurrence pour opérations DbContext
/// Élimine les erreurs "A second operation was started on this context instance"
/// </summary>
public interface IOperationLockService
{
    /// <summary>
    /// Exécute une opération async de manière sécurisée avec protection anti-concurrence
    /// </summary>
    /// <param name="operationKey">Clé unique pour identifier l'opération (ex: "sectionconteneur-add")</param>
    /// <param name="operation">Opération async à exécuter</param>
    /// <param name="timeoutMs">Timeout en millisecondes (défaut: 10000ms)</param>
    /// <returns>True si opération exécutée, False si ignorée (déjà en cours)</returns>
    Task<bool> ExecuteWithLockAsync(string operationKey, Func<Task> operation, int timeoutMs = 10000);

    /// <summary>
    /// Exécute une opération async avec retour de manière sécurisée
    /// </summary>
    /// <typeparam name="T">Type de retour</typeparam>
    /// <param name="operationKey">Clé unique pour identifier l'opération</param>
    /// <param name="operation">Opération async à exécuter</param>
    /// <param name="timeoutMs">Timeout en millisecondes</param>
    /// <returns>Tuple: (success, result) - success=False si opération ignorée</returns>
    Task<(bool success, T? result)> ExecuteWithLockAsync<T>(string operationKey, Func<Task<T>> operation, int timeoutMs = 10000);

    /// <summary>
    /// Vérifie si une opération est actuellement en cours
    /// </summary>
    bool IsOperationInProgress(string operationKey);

    /// <summary>
    /// Libère manuellement un verrou (pour cas d'urgence)
    /// </summary>
    void ReleaseLock(string operationKey);
}