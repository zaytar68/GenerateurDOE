using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GenerateurDOE.Services.Interfaces;

/// <summary>
/// Service helper pour gérer la modal de progression PDF de manière centralisée
/// </summary>
public interface IPdfProgressDialogService
{
    /// <summary>
    /// Ouvre la modal de progression et lance la génération PDF en arrière-plan
    /// </summary>
    /// <param name="documentId">ID du document à générer</param>
    /// <param name="documentName">Nom du document pour affichage</param>
    /// <param name="dialogService">Service Dialog Radzen</param>
    /// <param name="downloadService">Service de téléchargement</param>
    /// <param name="componentBase">Composant Blazor appelant (pour InvokeAsync)</param>
    /// <param name="jsRuntime">Runtime JavaScript</param>
    /// <param name="notificationService">Service de notification</param>
    /// <returns>Task de l'opération complète</returns>
    Task StartPdfGenerationWithProgressAsync(
        int documentId,
        string documentName,
        Radzen.DialogService dialogService,
        IDocumentDownloadService downloadService,
        ComponentBase componentBase,
        IJSRuntime jsRuntime,
        Radzen.NotificationService notificationService);
}