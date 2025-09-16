namespace GenerateurDOE.Services.Interfaces;

public interface ILoadingStateService
{
    /// <summary>
    /// Event déclenché quand l'état de chargement change
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Définit l'état de chargement pour une opération spécifique
    /// </summary>
    /// <param name="operation">Nom de l'opération (ex: "pdf-generation", "save-document")</param>
    /// <param name="isLoading">True si l'opération est en cours</param>
    void SetLoading(string operation, bool isLoading);

    /// <summary>
    /// Vérifie si une opération spécifique est en cours de chargement
    /// </summary>
    /// <param name="operation">Nom de l'opération</param>
    /// <returns>True si l'opération est en cours</returns>
    bool IsLoading(string operation);

    /// <summary>
    /// Vérifie si au moins une opération est en cours de chargement
    /// </summary>
    /// <returns>True si au moins une opération est en cours</returns>
    bool IsAnyLoading();

    /// <summary>
    /// Obtient toutes les opérations en cours de chargement
    /// </summary>
    /// <returns>Liste des noms d'opérations en cours</returns>
    IEnumerable<string> GetLoadingOperations();

    /// <summary>
    /// Remet à zéro tous les états de chargement
    /// </summary>
    void ClearAll();
}