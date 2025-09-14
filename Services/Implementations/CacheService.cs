using Microsoft.Extensions.Caching.Memory;
using GenerateurDOE.Services.Interfaces;
using System.Collections.Concurrent;
using System.Collections;
using System.Reflection;

namespace GenerateurDOE.Services.Implementations;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    
    // ⚡ Pour tracking des clés et permettre le nettoyage par préfixe
    private readonly ConcurrentBag<string> _keys = new();
    
    // ⚡ Configuration par défaut des expirations pour différents types de données
    private readonly TimeSpan _defaultTypesExpiration = TimeSpan.FromHours(1);
    private readonly TimeSpan _defaultConfigExpiration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _defaultDataExpiration = TimeSpan.FromMinutes(15);

    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Vérifier si la valeur existe déjà dans le cache
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Cache HIT pour la clé: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS pour la clé: {Key} - Exécution de la factory", key);

        // Calculer la valeur et la mettre en cache
        var value = await factory();
        
        // Déterminer l'expiration appropriée
        var finalExpiration = expiration ?? GetDefaultExpiration(key);
        
        // Configuration du cache avec expiration
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = finalExpiration,
            Priority = GetCachePriority(key),
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (cacheKey, _, reason, _) =>
                    {
                        _logger.LogDebug("Cache éviction pour {Key}, raison: {Reason}", cacheKey, reason);
                    }
                }
            }
        };

        _memoryCache.Set(key, value, cacheEntryOptions);
        _keys.Add(key);

        _logger.LogDebug("Cache SET pour la clé: {Key}, expiration: {Expiration}", key, finalExpiration);
        return value;
    }

    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Cache HIT pour la clé: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS pour la clé: {Key} - Exécution de la factory", key);

        var value = factory();
        var finalExpiration = expiration ?? GetDefaultExpiration(key);

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = finalExpiration,
            Priority = GetCachePriority(key)
        };

        _memoryCache.Set(key, value, cacheEntryOptions);
        _keys.Add(key);

        return value;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        _logger.LogDebug("Cache REMOVE pour la clé: {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        
        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
        }
        
        _logger.LogDebug("Cache REMOVE par préfixe: {Prefix}, {Count} clés supprimées", prefix, keysToRemove.Count);
    }

    public void Clear()
    {
        // ⚡ Méthode non-publique pour vider le cache via reflection
        // Note: IMemoryCache ne propose pas de méthode Clear() publique
        var field = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field?.GetValue(_memoryCache) is object coherentState)
        {
            var entriesCollection = coherentState.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
            {
                entries.Clear();
            }
        }
        
        // Nettoyer notre tracking de clés
        while (_keys.TryTake(out _)) { }
        
        _logger.LogDebug("Cache complètement vidé");
    }

    public bool Exists(string key)
    {
        return _memoryCache.TryGetValue(key, out _);
    }

    // ⚡ Méthodes privées pour configuration intelligente du cache

    private TimeSpan GetDefaultExpiration(string key)
    {
        // ⚡ Configuration intelligente basée sur le pattern de la clé
        return key.ToLowerInvariant() switch
        {
            // Types et données référentielles : cache long (1h)
            var k when k.Contains("types") || k.Contains("config") => _defaultTypesExpiration,
            
            // Configuration app : cache moyen (30min)
            var k when k.Contains("settings") || k.Contains("appconfig") => _defaultConfigExpiration,
            
            // Données transactionnelles : cache court (15min)
            _ => _defaultDataExpiration
        };
    }

    private CacheItemPriority GetCachePriority(string key)
    {
        // ⚡ Priorité intelligente pour éviction automatique
        return key.ToLowerInvariant() switch
        {
            // Types = priorité haute (gardés le plus longtemps)
            var k when k.Contains("types") => CacheItemPriority.High,
            
            // Configuration = priorité normale
            var k when k.Contains("config") || k.Contains("settings") => CacheItemPriority.Normal,
            
            // Autres données = priorité basse
            _ => CacheItemPriority.Low
        };
    }
}

// ⚡ Extensions pour simplifier l'utilisation
public static class CacheServiceExtensions 
{
    // Clés standardisées pour les différents types de données
    public const string TYPES_SECTIONS_KEY = "types:sections";
    public const string TYPES_SECTIONS_ACTIVE_KEY = "types:sections:active";
    public const string TYPES_PRODUITS_KEY = "types:produits";
    public const string TYPES_PRODUITS_ACTIVE_KEY = "types:produits:active";
    public const string TYPES_DOCUMENTS_KEY = "types:documents";
    public const string TYPES_DOCUMENTS_ACTIVE_KEY = "types:documents:active";
    public const string APP_SETTINGS_KEY = "config:appsettings";
    
    // Clés de cache pour templates HTML (Phase 3C)
    public const string TEMPLATE_PAGE_GARDE_KEY = "template:html:pagegarde";
    public const string TEMPLATE_TABLE_MATIERES_KEY = "template:html:tabledesmatières";
    public const string TEMPLATE_SECTION_LIBRE_KEY = "template:html:sectionlibre";
    public const string TEMPLATE_FT_CONTENEUR_KEY = "template:html:ftconteneur";
    public const string TEMPLATE_CSS_BASE_KEY = "template:css:base";
    
    // Préfixes pour nettoyage groupé
    public const string TYPES_PREFIX = "types:";
    public const string CONFIG_PREFIX = "config:";
    public const string TEMPLATE_PREFIX = "template:";
}