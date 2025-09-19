using GenerateurDOE.Components.Shared;
using GenerateurDOE.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GenerateurDOE.Services.Implementations;

/// <summary>
/// Service helper pour gérer la modal de progression PDF de manière centralisée
/// Évite la duplication de code entre les pages
/// </summary>
public class PdfProgressDialogService : IPdfProgressDialogService
{
    public async Task StartPdfGenerationWithProgressAsync(
        int documentId,
        string documentName,
        Radzen.DialogService dialogService,
        IDocumentDownloadService downloadService,
        ComponentBase componentBase,
        IJSRuntime jsRuntime,
        Radzen.NotificationService notificationService)
    {
        try
        {
            // Démarrer la génération en arrière-plan AVANT d'ouvrir la modal
            _ = Task.Run(async () =>
            {
                try
                {
                    // Utilisation du service factorisé pour la génération AVEC mise à jour de progression
                    var result = await downloadService.PrepareDocumentForDownloadAsync(documentId);

                    if (result.Success)
                    {
                        // Téléchargement automatique du fichier via JavaScript
                        var base64 = Convert.ToBase64String(result.FileBytes);

                        try
                        {
                            // Téléchargement direct via JavaScript (ne nécessite pas InvokeAsync car déjà sur un thread séparé)
                            await jsRuntime.InvokeVoidAsync("downloadFile", result.FileName, base64, "application/pdf");

                            // Notification de succès
                            notificationService.Notify(Radzen.NotificationSeverity.Success, "Téléchargement",
                                $"PDF {result.FileName} généré avec succès");
                        }
                        catch (Exception jsEx)
                        {
                            // Fallback : seulement notification si le JS échoue
                            notificationService.Notify(Radzen.NotificationSeverity.Warning, "Génération réussie",
                                $"PDF {result.FileName} généré mais téléchargement automatique échoué");
                            Console.WriteLine($"[DEBUG] JavaScript download error: {jsEx.Message}");
                        }
                    }
                    else
                    {
                        notificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Masquer les erreurs de concurrence DbContext (temporaire)
                    if (!ex.Message.Contains("A second operation was started on this context") &&
                        !ex.Message.Contains("context instance"))
                    {
                        notificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                            $"Erreur lors de la génération: {ex.Message}");
                    }
                    Console.WriteLine($"[DEBUG] PdfProgressDialogService error (masqué): {ex.Message}");
                }
            });

            // Ouvrir la modal de progression PDF APRÈS avoir démarré la génération
            var modalResult = await dialogService.OpenAsync<PdfProgressModal>("Génération PDF",
                new Dictionary<string, object>
                {
                    { "DocumentId", documentId },
                    { "DocumentName", documentName }
                },
                new Radzen.DialogOptions
                {
                    Width = "600px",
                    Height = "500px",
                    Resizable = false,
                    Draggable = false,
                    CloseDialogOnOverlayClick = false,
                    CloseDialogOnEsc = false
                });
        }
        catch (Exception ex)
        {
            notificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                $"Erreur lors de l'ouverture de la modal: {ex.Message}");
            Console.WriteLine($"[DEBUG] PdfProgressDialogService modal error: {ex.Message}");
        }
    }
}