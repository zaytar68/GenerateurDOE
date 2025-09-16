using GenerateurDOE.Services.Interfaces;
using System.Collections.Concurrent;

namespace GenerateurDOE.Services.Implementations;

public class LoadingStateService : ILoadingStateService
{
    private readonly ConcurrentDictionary<string, bool> _loadingStates = new();

    public event Action? StateChanged;

    public void SetLoading(string operation, bool isLoading)
    {
        if (string.IsNullOrWhiteSpace(operation))
            return;

        bool stateChanged = false;

        if (isLoading)
        {
            stateChanged = _loadingStates.TryAdd(operation, true) || !_loadingStates[operation];
            _loadingStates[operation] = true;
        }
        else
        {
            stateChanged = _loadingStates.TryRemove(operation, out _);
        }

        if (stateChanged)
        {
            StateChanged?.Invoke();
        }
    }

    public bool IsLoading(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            return false;

        return _loadingStates.TryGetValue(operation, out var isLoading) && isLoading;
    }

    public bool IsAnyLoading()
    {
        return _loadingStates.Any(kvp => kvp.Value);
    }

    public IEnumerable<string> GetLoadingOperations()
    {
        return _loadingStates
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public void ClearAll()
    {
        bool hadLoadingStates = _loadingStates.Any();
        _loadingStates.Clear();

        if (hadLoadingStates)
        {
            StateChanged?.Invoke();
        }
    }
}