using System.Threading.Tasks;

namespace GenerateurDOE.Services.Interfaces
{
    /// <summary>
    /// Service pour compter et mettre en cache le nombre de pages des fichiers PDF
    /// </summary>
    public interface IPdfPageCountService
    {
        /// <summary>
        /// Obtient le nombre de pages d'un fichier PDF avec mise en cache
        /// </summary>
        /// <param name="filePath">Chemin complet du fichier PDF</param>
        /// <returns>Nombre de pages dans le PDF, ou null si le fichier n'existe pas</returns>
        Task<int?> GetPageCountAsync(string filePath);

        /// <summary>
        /// Invalide le cache pour un fichier spécifique
        /// </summary>
        /// <param name="filePath">Chemin du fichier à invalider</param>
        void InvalidateCache(string filePath);

        /// <summary>
        /// Invalide tout le cache
        /// </summary>
        void InvalidateAllCache();

        /// <summary>
        /// Pré-charge le cache pour plusieurs fichiers
        /// </summary>
        /// <param name="filePaths">Liste des chemins de fichiers à pré-charger</param>
        Task PreloadCacheAsync(IEnumerable<string> filePaths);
    }
}