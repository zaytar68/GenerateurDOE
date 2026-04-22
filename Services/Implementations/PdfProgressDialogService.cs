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
            // 🚀 Démarrer la génération PDF EN ARRIÈRE-PLAN avant d'ouvrir la modal
            // La modal va suivre la progression via le ProgressService
            var generationTask = Task.Run(async () =>
            {
                try
                {
                    // Génération du PDF avec suivi de progression
                    await downloadService.PrepareDocumentForDownloadAsync(documentId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Erreur génération PDF: {ex.Message}");
                }
            });

            // ⏱️ Ouvrir la modal de progression IMMÉDIATEMENT
            // La modal va afficher la progression en temps réel via polling
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

            // Attendre que la génération soit terminée (au cas où la modal se ferme avant)
            await generationTask;

            // 📥 Déclencher le téléchargement via l'API après la fermeture de la modal
            try
            {
                // Utiliser l'API de téléchargement pour éviter les limitations de taille
                var downloadUrl = $"/api/DocumentDownload/{documentId}";

                // Ouvrir le téléchargement dans une nouvelle fenêtre
                await jsRuntime.InvokeVoidAsync("open", downloadUrl, "_blank");

                // Notification de succès
                notificationService.Notify(Radzen.NotificationSeverity.Success, "Téléchargement",
                    "Le téléchargement du PDF devrait démarrer dans quelques instants...");
            }
            catch (Exception jsEx)
            {
                notificationService.Notify(Radzen.NotificationSeverity.Warning, "Information",
                    "Veuillez utiliser le bouton de téléchargement pour récupérer le document.");
                Console.WriteLine($"[ERROR] JavaScript download error: {jsEx.Message}");
            }
        }
        catch (Exception ex)
        {
            notificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                $"Erreur lors de l'ouverture de la modal: {ex.Message}");
            Console.WriteLine($"[DEBUG] PdfProgressDialogService modal error: {ex.Message}");
        }
    }
}