using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service de protection anti-concurrence pour opérations DbContext
/// Utilise des semaphores pour éviter les erreurs de concurrence EF Core
/// </summary>
public class OperationLockService : IOperationLockService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _operationLocks = new();
    private readonly ILoggingService _logger;

    public OperationLockService(ILoggingService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Exécute une opération avec protection anti-concurrence
    /// </summary>
    public async Task<bool> ExecuteWithLockAsync(string operationKey, Func<Task> operation, int timeoutMs = 10000)
    {
        var semaphore = _operationLocks.GetOrAdd(operationKey, _ => new SemaphoreSlim(1, 1));

        // Tentative d'acquisition du verrou avec timeout
        var acquired = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);

        if (!acquired)
        {
            _logger.LogWarning($"⚠️ Opération '{operationKey}' ignorée - timeout ({timeoutMs}ms) ou déjà en cours");
            return false;
        }

        try
        {
            _logger.LogInformation($"🔒 Début opération protégée: '{operationKey}'");
            await operation().ConfigureAwait(false);
            _logger.LogInformation($"✅ Fin opération protégée: '{operationKey}'");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Erreur opération '{operationKey}': {ex.Message}");
            throw; // Re-throw pour préserver le stack trace
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Exécute une opération avec retour et protection anti-concurrence
    /// </summary>
    public async Task<(bool success, T? result)> ExecuteWithLockAsync<T>(string operationKey, Func<Task<T>> operation, int timeoutMs = 10000)
    {
        var semaphore = _operationLocks.GetOrAdd(operationKey, _ => new SemaphoreSlim(1, 1));

        var acquired = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);

        if (!acquired)
        {
            _logger.LogWarning($"⚠️ Opération '{operationKey}' ignorée - timeout ou déjà en cours");
            return (false, default(T));
        }

        try
        {
            _logger.LogInformation($"🔒 Début opération protégée avec retour: '{operationKey}'");
            var result = await operation().ConfigureAwait(false);
            _logger.LogInformation($"✅ Fin opération protégée avec retour: '{operationKey}'");
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Erreur opération '{operationKey}': {ex.Message}");
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Vérifie si une opération est en cours
    /// </summary>
    public bool IsOperationInProgress(string operationKey)
    {
        if (!_operationLocks.TryGetValue(operationKey, out var semaphore))
            return false;

        return semaphore.CurrentCount == 0;
    }

    /// <summary>
    /// Libération manuelle d'un verrou (cas d'urgence)
    /// </summary>
    public void ReleaseLock(string operationKey)
    {
        if (_operationLocks.TryGetValue(operationKey, out var semaphore))
        {
            try
            {
                semaphore.Release();
                _logger.LogWarning($"🔓 Verrou '{operationKey}' libéré manuellement");
            }
            catch (SemaphoreFullException)
            {
                _logger.LogWarning($"⚠️ Verrou '{operationKey}' était déjà libéré");
            }
        }
    }

    /// <summary>
    /// Nettoyage des ressources
    /// </summary>
    public void Dispose()
    {
        foreach (var semaphore in _operationLocks.Values)
        {
            semaphore?.Dispose();
        }
        _operationLocks.Clear();
    }
}