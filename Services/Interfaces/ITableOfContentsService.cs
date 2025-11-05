using GenerateurDOE.Controllers;

namespace GenerateurDOE.Services.Interfaces;

/// <summary>
/// Service pour la génération de structures de table des matières
/// </summary>
public interface ITableOfContentsService
{
    /// <summary>
    /// Génère la structure complète de la table des matières pour un document
    /// </summary>
    /// <param name="documentId">ID du document</param>
    /// <returns>Structure de la table des matières avec entrées et numéros de pages calculés</returns>
    Task<TocStructureResponse> GenerateStructureAsync(int documentId);
}
