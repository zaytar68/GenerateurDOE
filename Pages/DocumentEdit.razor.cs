using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using GenerateurDOE.Models;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Pages;

/// <summary>
/// Code-behind pour la page d'édition de documents
/// Contient toute la logique métier séparée de l'interface utilisateur
/// </summary>
public partial class DocumentEdit : ComponentBase
{
    #region Services Injectés

    [Inject] private IDocumentGenereService documentGenereService { get; set; } = null!;
    [Inject] private IDocumentDownloadService documentDownloadService { get; set; } = null!;
    [Inject] private IPdfProgressDialogService pdfProgressDialogService { get; set; } = null!;
    [Inject] private IChantierService chantierService { get; set; } = null!;
    [Inject] private ISectionConteneurService sectionConteneurService { get; set; } = null!;
    [Inject] private IFTConteneurService ftConteneurService { get; set; } = null!;
    [Inject] private ITypeSectionService typeSectionService { get; set; } = null!;
    [Inject] private IPageGardeTemplateService pageGardeTemplateService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private Radzen.NotificationService NotificationService { get; set; } = null!;
    [Inject] private Radzen.DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    #endregion

    #region Paramètres

    /// <summary>
    /// Identifiant du document à éditer (mode édition)
    /// </summary>
    [Parameter] public int? DocumentId { get; set; }

    /// <summary>
    /// Identifiant du chantier pour un nouveau document (mode création)
    /// </summary>
    [Parameter] public int? ChantierId { get; set; }

    #endregion

    #region État du Composant

    private DocumentGenere? document;
    private Chantier? chantier;
    private List<SectionConteneur> sectionsConteneurs = new();
    private FTConteneur? ftConteneur;
    private List<TypeSection> typesSection = new();
    private List<PageGardeTemplate> availableTemplates = new();

    private bool isLoading = true;
    private bool isSaving = false;
    private bool isGenerating = false;
    private bool hasUnsavedChanges = false;
    private string activeTab = "general";
    private bool isLoadingData = false;

    private string selectedTypeDocumentString = string.Empty;
    private bool typeDocumentError = false;
    private bool isFileNameManuallySet = false;

    #endregion

    #region Propriétés Calculées

    /// <summary>
    /// Indique si on est en mode édition (document existant) ou création
    /// </summary>
    private bool IsEditMode => DocumentId.HasValue && DocumentId.Value > 0;

    /// <summary>
    /// Identifiant du chantier actuel
    /// </summary>
    private int CurrentChantierId => IsEditMode ? document?.ChantierId ?? 0 : ChantierId ?? 0;

    /// <summary>
    /// Titre de la page pour l'en-tête
    /// </summary>
    public string PageTitle => IsEditMode ? $"Modifier {document?.NomFichier}" : "Nouveau Document";

    /// <summary>
    /// Titre principal affiché dans l'interface
    /// </summary>
    public string HeaderTitle => IsEditMode ? $"Modifier : {document?.NomFichier}" : "Nouveau Document";

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Initialisation du composant - chargement des données
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await ChargerDonnees();
    }

    /// <summary>
    /// Rechargement quand les paramètres changent
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if ((DocumentId.HasValue || ChantierId.HasValue) && !isLoadingData)
        {
            await ChargerDonnees();
        }
    }

    #endregion

    #region Méthodes de Chargement

    /// <summary>
    /// Charge toutes les données nécessaires pour l'affichage
    /// </summary>
    private async Task ChargerDonnees()
    {
        // Éviter les appels concurrents
        if (isLoadingData) return;

        try
        {
            isLoadingData = true;
            isLoading = true;
            StateHasChanged();

            // Charger les types de section
            typesSection = (await typeSectionService.GetAllAsync()).ToList();

            // Charger les templates de page de garde disponibles
            await LoadAvailableTemplatesAsync();

            if (IsEditMode && DocumentId.HasValue)
            {
                // Mode édition - charger le document existant
                document = await documentGenereService.GetByIdAsync(DocumentId.Value);
                if (document != null)
                {
                    chantier = await chantierService.GetByIdAsync(document.ChantierId);
                    await ChargerConteneurs();

                    // Initialiser les valeurs des select
                    selectedTypeDocumentString = document.TypeDocument.ToString();
                }
            }
            else if (ChantierId.HasValue)
            {
                // Mode création - nouveau document
                chantier = await chantierService.GetByIdAsync(ChantierId.Value);
                document = new DocumentGenere
                {
                    ChantierId = ChantierId.Value,
                    IncludePageDeGarde = true,
                    IncludeTableMatieres = true,
                    EnCours = true,
                    TypeDocument = TypeDocumentGenere.DOE // Valeur par défaut
                };

                // Initialiser la string de sélection avec la valeur par défaut
                selectedTypeDocumentString = document.TypeDocument.ToString();

                GenerateDefaultFileName();
            }
        }
        catch (Exception ex)
        {
            // Masquer les erreurs de concurrence DbContext (temporaire)
            if (!ex.Message.Contains("A second operation was started on this context") &&
                !ex.Message.Contains("context instance"))
            {
                NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                    $"Erreur lors du chargement: {ex.Message}");
            }
            Console.WriteLine($"[DEBUG] ChargerDonnees error (masqué): {ex.Message}");
        }
        finally
        {
            isLoadingData = false;
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Charge les conteneurs de sections et fiches techniques du document
    /// </summary>
    private async Task ChargerConteneurs()
    {
        if (document?.Id > 0)
        {
            sectionsConteneurs = (await documentGenereService.GetSectionsConteneursByDocumentAsync(document.Id)).ToList();
            ftConteneur = await documentGenereService.GetFTConteneurByDocumentAsync(document.Id);
        }
    }

    /// <summary>
    /// Charge les templates de page de garde disponibles
    /// </summary>
    private async Task LoadAvailableTemplatesAsync()
    {
        try
        {
            var templates = await pageGardeTemplateService.GetAllTemplatesAsync();
            availableTemplates = templates.ToList();
        }
        catch (Exception ex)
        {
            // Masquer les erreurs de concurrence DbContext (temporaire)
            if (!ex.Message.Contains("A second operation was started on this context") &&
                !ex.Message.Contains("context instance"))
            {
                NotificationService.Notify(Radzen.NotificationSeverity.Warning, "Attention",
                    $"Impossible de charger les templates : {ex.Message}");
            }
            Console.WriteLine($"[DEBUG] LoadAvailableTemplates error (masqué): {ex.Message}");
            availableTemplates = new List<PageGardeTemplate>();
        }
    }

    #endregion

    #region Méthodes de Navigation et UI

    /// <summary>
    /// Change l'onglet actif
    /// </summary>
    public void SetActiveTab(string tabName)
    {
        activeTab = tabName;
        StateHasChanged();
    }

    /// <summary>
    /// Retour à la page du chantier
    /// </summary>
    public void RetourChantier()
    {
        if (hasUnsavedChanges)
        {
            // TODO: Ajouter confirmation pour les modifications non sauvées
        }
        Navigation.NavigateTo($"/chantier/{CurrentChantierId}");
    }

    #endregion

    #region Méthodes de Gestion du Document

    /// <summary>
    /// Génère un nom de fichier par défaut basé sur le type de document et le chantier
    /// </summary>
    private void GenerateDefaultFileName()
    {
        if (document != null && (!isFileNameManuallySet || string.IsNullOrWhiteSpace(document.NomFichier) || IsAutomaticallyGeneratedName(document.NomFichier)))
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            document.NomFichier = document.TypeDocument switch
            {
                TypeDocumentGenere.DOE => $"DOE_{chantier?.NomProjet?.Replace(" ", "_") ?? "Document"}_{timestamp}",
                TypeDocumentGenere.DossierTechnique => $"DossierTechnique_{chantier?.NomProjet?.Replace(" ", "_") ?? "Document"}_{timestamp}",
                TypeDocumentGenere.MemoireTechnique => $"MemoireTechnique_{chantier?.NomProjet?.Replace(" ", "_") ?? "Document"}_{timestamp}",
                _ => $"Document_{timestamp}"
            };

            // Marquer comme nom généré automatiquement
            isFileNameManuallySet = false;
        }
    }

    /// <summary>
    /// Détecte si un nom de fichier suit le pattern de génération automatique
    /// </summary>
    private bool IsAutomaticallyGeneratedName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        return fileName.StartsWith("DOE_") ||
               fileName.StartsWith("DossierTechnique_") ||
               fileName.StartsWith("MemoireTechnique_") ||
               fileName.StartsWith("Document_");
    }

    /// <summary>
    /// Valide les données du document avant sauvegarde
    /// </summary>
    private bool ValidateDocument()
    {
        if (document == null) return false;

        // Validation des champs obligatoires avec mise à jour des erreurs visuelles
        typeDocumentError = string.IsNullOrEmpty(selectedTypeDocumentString);

        var errors = new List<string>();

        if (typeDocumentError)
            errors.Add("Le type de document est requis");

        if (string.IsNullOrWhiteSpace(document.NomFichier))
            errors.Add("Le nom du fichier est requis");

        if (errors.Any())
        {
            NotificationService.Notify(Radzen.NotificationSeverity.Warning, "Validation",
                string.Join(", ", errors));
            SetActiveTab("general"); // Retour à l'onglet général pour corriger
            StateHasChanged(); // Mettre à jour l'affichage des erreurs
            return false;
        }

        return true;
    }

    #endregion

    #region Actions Principales

    /// <summary>
    /// Sauvegarde le document (création ou mise à jour)
    /// </summary>
    public async Task SauvegarderDocument()
    {
        if (document == null) return;

        try
        {
            isSaving = true;
            StateHasChanged();

            if (!ValidateDocument()) return;

            if (IsEditMode)
            {
                document = await documentGenereService.UpdateAsync(document);
                NotificationService.Notify(Radzen.NotificationSeverity.Success, "Succès", "Document mis à jour avec succès");
            }
            else
            {
                document = await documentGenereService.SaveDocumentGenereAsync(document);
                NotificationService.Notify(Radzen.NotificationSeverity.Success, "Succès", "Document créé avec succès");

                // Redirection vers le mode édition
                Navigation.NavigateTo($"/document/edit/{document.Id}");
                return;
            }

            hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            // Masquer les erreurs de concurrence DbContext (temporaire)
            if (!ex.Message.Contains("A second operation was started on this context") &&
                !ex.Message.Contains("context instance"))
            {
                NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                    $"Erreur lors de la sauvegarde: {ex.Message}");
            }
            Console.WriteLine($"[DEBUG] SaveChanges error (masqué): {ex.Message}");
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Finalise le document (le marque comme terminé)
    /// </summary>
    public async Task FinaliserDocument()
    {
        if (document?.Id == null) return;

        var result = await DialogService.Confirm(
            "Êtes-vous sûr de vouloir finaliser ce document ? Cette action ne pourra pas être annulée.",
            "Finalisation du document",
            new Radzen.ConfirmOptions { OkButtonText = "Finaliser", CancelButtonText = "Annuler" });

        if (result == true)
        {
            try
            {
                await documentGenereService.FinalizeDocumentAsync(document.Id);
                NotificationService.Notify(Radzen.NotificationSeverity.Success, "Succès", "Document finalisé");
                await ChargerDonnees(); // Recharger pour mettre à jour l'état
            }
            catch (Exception ex)
            {
                // Masquer les erreurs de concurrence DbContext (temporaire)
                if (!ex.Message.Contains("A second operation was started on this context") &&
                    !ex.Message.Contains("context instance"))
                {
                    NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur", ex.Message);
                }
                Console.WriteLine($"[DEBUG] FinalizeDocument error (masqué): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Lance la génération du document PDF
    /// </summary>
    public async Task GenererDocument()
    {
        if (document?.Id == null) return;

        try
        {
            isGenerating = true;
            StateHasChanged();

            // Utiliser le service centralisé pour la modal de progression
            await pdfProgressDialogService.StartPdfGenerationWithProgressAsync(
                document.Id,
                document.NomFichier ?? "Document",
                DialogService,
                documentDownloadService,
                this,
                JSRuntime,
                NotificationService);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                $"Erreur lors du lancement de la génération: {ex.Message}");
            Console.WriteLine($"[DEBUG] GenererDocument error: {ex.Message}");
        }
        finally
        {
            isGenerating = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Crée un conteneur de fiches techniques pour le document
    /// </summary>
    public async Task CreerFTConteneur()
    {
        if (document?.Id == null) return;

        try
        {
            ftConteneur = await documentGenereService.CreateFTConteneurAsync(document.Id);
            NotificationService.Notify(Radzen.NotificationSeverity.Success, "Succès", "Conteneur de fiches techniques créé");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            // Masquer les erreurs de concurrence DbContext (temporaire)
            if (!ex.Message.Contains("A second operation was started on this context") &&
                !ex.Message.Contains("context instance"))
            {
                NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur", ex.Message);
            }
            Console.WriteLine($"[DEBUG] CreateFTConteneur error (masqué): {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Gestion du changement de type de document
    /// </summary>
    public void OnTypeDocumentChanged()
    {
        typeDocumentError = string.IsNullOrEmpty(selectedTypeDocumentString);

        if (!typeDocumentError && document != null)
        {
            if (Enum.TryParse<TypeDocumentGenere>(selectedTypeDocumentString, out var typeDocument))
            {
                document.TypeDocument = typeDocument;
                GenerateDefaultFileName();
            }
        }

        hasUnsavedChanges = true;
        StateHasChanged();
    }

    /// <summary>
    /// Marque le nom de fichier comme ayant été modifié manuellement par l'utilisateur
    /// </summary>
    public void OnFileNameManuallyChanged()
    {
        isFileNameManuallySet = true;
        hasUnsavedChanges = true;
        StateHasChanged();
    }


    /// <summary>
    /// Gestion des changements dans les sections
    /// </summary>
    public void OnSectionsChanged(List<SectionConteneur> sections)
    {
        sectionsConteneurs = sections;
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    /// <summary>
    /// Gestion des changements dans la table des matières
    /// </summary>
    public void OnDocumentTableMatieresChanged(DocumentGenere updatedDocument)
    {
        if (document != null && updatedDocument != null)
        {
            // Synchroniser toutes les modifications de la table des matières
            document.IncludeTableMatieres = updatedDocument.IncludeTableMatieres;
            document.Parametres = updatedDocument.Parametres; // Synchroniser les paramètres JSON
            hasUnsavedChanges = true;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gestion des changements dans le conteneur de fiches techniques
    /// </summary>
    public void OnFTConteneurChanged(FTConteneur conteneur)
    {
        ftConteneur = conteneur;
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    #endregion

    #region Méthodes Utilitaires

    /// <summary>
    /// Ouvre l'éditeur de template de page de garde
    /// </summary>
    public async Task OpenPageGardeEditor()
    {
        try
        {
            // Ouvrir l'éditeur de page de garde dans une nouvelle fenêtre/onglet
            var templateId = document?.PageGardeTemplateId;
            var url = templateId.HasValue ?
                $"/pageGarde/edit/{templateId}" :
                "/pageGarde/nouveau";

            Navigation.NavigateTo(url);
        }
        catch (Exception ex)
        {
            // Masquer les erreurs de concurrence DbContext (temporaire)
            if (!ex.Message.Contains("A second operation was started on this context") &&
                !ex.Message.Contains("context instance"))
            {
                NotificationService.Notify(Radzen.NotificationSeverity.Error, "Erreur",
                    $"Erreur lors de l'ouverture de l'éditeur : {ex.Message}");
            }
            Console.WriteLine($"[DEBUG] OpenPageGardeEditor error (masqué): {ex.Message}");
        }
    }

    /// <summary>
    /// Retourne le libellé d'affichage pour un type de document
    /// </summary>
    public string GetTypeDisplayName(TypeDocumentGenere type)
    {
        return type switch
        {
            TypeDocumentGenere.DOE => "DOE (Dossier d'Ouvrages Exécutés)",
            TypeDocumentGenere.DossierTechnique => "Dossier Technique",
            TypeDocumentGenere.MemoireTechnique => "Mémoire Technique",
            _ => type.ToString()
        };
    }

    #endregion
}