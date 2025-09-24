using GenerateurDOE.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Collections.Concurrent;

namespace GenerateurDOE.Services.Implementations
{
    /// <summary>
    /// Service pour compter et mettre en cache le nombre de pages des fichiers PDF
    /// </summary>
    public class PdfPageCountService : IPdfPageCountService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<PdfPageCountService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private const string CACHE_KEY_PREFIX = "pdf_page_count_";
        private readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromHours(1);

        // Structure pour stocker les infos en cache
        private record CachedPageInfo(int PageCount, DateTime LastWriteTime);

        public PdfPageCountService(
            IMemoryCache cache,
            ILogger<PdfPageCountService> logger,
            IWebHostEnvironment environment)
        {
            _cache = cache;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Obtient le nombre de pages d'un fichier PDF avec mise en cache
        /// </summary>
        public async Task<int?> GetPageCountAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("GetPageCountAsync appelé avec un chemin vide");
                return null;
            }

            try
            {
                // Construire le chemin complet si nécessaire
                var fullPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : Path.Combine(_environment.ContentRootPath, filePath);

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("Fichier PDF introuvable : {FilePath}", fullPath);
                    return null;
                }

                var fileInfo = new FileInfo(fullPath);
                var cacheKey = $"{CACHE_KEY_PREFIX}{fullPath.ToLowerInvariant()}";

                // Vérifier le cache
                if (_cache.TryGetValue<CachedPageInfo>(cacheKey, out var cachedInfo))
                {
                    // Vérifier si le fichier n'a pas été modifié
                    if (cachedInfo.LastWriteTime == fileInfo.LastWriteTime)
                    {
                        _logger.LogDebug("Nombre de pages récupéré du cache pour {FilePath}: {PageCount} pages",
                            filePath, cachedInfo.PageCount);
                        return cachedInfo.PageCount;
                    }
                    else
                    {
                        _logger.LogDebug("Cache invalidé pour {FilePath} - fichier modifié", filePath);
                    }
                }

                // Compter les pages du PDF
                await _semaphore.WaitAsync();
                try
                {
                    // Double-check après avoir acquis le sémaphore
                    if (_cache.TryGetValue<CachedPageInfo>(cacheKey, out cachedInfo))
                    {
                        if (cachedInfo.LastWriteTime == fileInfo.LastWriteTime)
                        {
                            return cachedInfo.PageCount;
                        }
                    }

                    var pageCount = await Task.Run(() => CountPdfPages(fullPath));

                    if (pageCount.HasValue)
                    {
                        // Stocker en cache
                        var newCachedInfo = new CachedPageInfo(pageCount.Value, fileInfo.LastWriteTime);
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = CACHE_EXPIRATION,
                            Priority = CacheItemPriority.Normal,
                            Size = 1 // Pour limiter la taille du cache si configuré
                        };

                        _cache.Set(cacheKey, newCachedInfo, cacheOptions);
                        _logger.LogInformation("Nombre de pages calculé et mis en cache pour {FilePath}: {PageCount} pages",
                            filePath, pageCount.Value);
                    }

                    return pageCount;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des pages pour {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Compte le nombre de pages dans un fichier PDF
        /// </summary>
        private int? CountPdfPages(string filePath)
        {
            try
            {
                // Utiliser PdfSharp pour compter les pages (Import mode pour lecture seule)
                using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                return document.PageCount;
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                _logger.LogWarning(ex, "Impossible de lire le PDF {FilePath} - fichier peut-être corrompu", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la lecture du PDF {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Invalide le cache pour un fichier spécifique
        /// </summary>
        public void InvalidateCache(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var fullPath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(_environment.ContentRootPath, filePath);

            var cacheKey = $"{CACHE_KEY_PREFIX}{fullPath.ToLowerInvariant()}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Cache invalidé pour {FilePath}", filePath);
        }

        /// <summary>
        /// Invalide tout le cache
        /// </summary>
        public void InvalidateAllCache()
        {
            // Note: MemoryCache ne permet pas de supprimer toutes les entrées facilement
            // Cette méthode est laissée pour compatibilité future
            _logger.LogInformation("Demande d'invalidation totale du cache (non implémentée avec MemoryCache standard)");
        }

        /// <summary>
        /// Pré-charge le cache pour plusieurs fichiers
        /// </summary>
        public async Task PreloadCacheAsync(IEnumerable<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any())
                return;

            _logger.LogInformation("Préchargement du cache pour {Count} fichiers PDF", filePaths.Count());

            var tasks = filePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(async path =>
                {
                    try
                    {
                        await GetPageCountAsync(path);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors du préchargement du cache pour {FilePath}", path);
                    }
                });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Préchargement du cache terminé");
        }
    }
}