using Microsoft.Extensions.Caching.Memory;

namespace GenerateurDOE.Services.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Récupère une valeur depuis le cache ou l'exécute si elle n'existe pas
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    
    /// <summary>
    /// Récupère une valeur depuis le cache ou l'exécute si elle n'existe pas (version synchrone)
    /// </summary>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);
    
    /// <summary>
    /// Supprime une clé du cache
    /// </summary>
    void Remove(string key);
    
    /// <summary>
    /// Supprime toutes les clés commençant par le préfixe donné
    /// </summary>
    void RemoveByPrefix(string prefix);
    
    /// <summary>
    /// Vide complètement le cache
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Vérifie si une clé existe dans le cache
    /// </summary>
    bool Exists(string key);
}