namespace GenerateurDOE.Models;

public enum PdfGenerationStep
{
    Initialisation = 0,
    PageDeGarde = 1,
    AnalyseTableMatieres = 2,
    SectionsLibres = 3,
    TableauSynthese = 4,
    FichesTechniques = 5,
    InsertionTableMatieres = 6,
    AssemblyFinal = 7,
    Termine = 8
}

public class PdfGenerationProgress
{
    public int DocumentId { get; set; }
    public int ProgressPercentage { get; set; }
    public PdfGenerationStep CurrentStep { get; set; }
    public string StepMessage { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; } = DateTime.Now;
    public bool IsCompleted { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public static string GetStepLabel(PdfGenerationStep step) => step switch
    {
        PdfGenerationStep.Initialisation => "Initialisation...",
        PdfGenerationStep.PageDeGarde => "Génération de la page de garde",
        PdfGenerationStep.AnalyseTableMatieres => "Analyse pour table des matières",
        PdfGenerationStep.SectionsLibres => "Conversion des sections HTML",
        PdfGenerationStep.TableauSynthese => "Génération du tableau de synthèse",
        PdfGenerationStep.FichesTechniques => "Intégration des fiches techniques",
        PdfGenerationStep.InsertionTableMatieres => "Insertion de la table des matières",
        PdfGenerationStep.AssemblyFinal => "Assembly final du PDF",
        PdfGenerationStep.Termine => "PDF généré avec succès !",
        _ => "Génération en cours..."
    };

    public static int GetStepPercentage(PdfGenerationStep step) => step switch
    {
        PdfGenerationStep.Initialisation => 0,
        PdfGenerationStep.PageDeGarde => 15,
        PdfGenerationStep.AnalyseTableMatieres => 25,
        PdfGenerationStep.SectionsLibres => 40,
        PdfGenerationStep.TableauSynthese => 55,
        PdfGenerationStep.FichesTechniques => 70,
        PdfGenerationStep.InsertionTableMatieres => 85,
        PdfGenerationStep.AssemblyFinal => 95,
        PdfGenerationStep.Termine => 100,
        _ => 0
    };

    public void UpdateStep(PdfGenerationStep step, string? customMessage = null)
    {
        CurrentStep = step;
        ProgressPercentage = GetStepPercentage(step);
        StepMessage = customMessage ?? GetStepLabel(step);
        LastUpdate = DateTime.Now;
        IsCompleted = step == PdfGenerationStep.Termine;
    }

    public void SetError(string errorMessage)
    {
        HasError = true;
        ErrorMessage = errorMessage;
        StepMessage = $"Erreur : {errorMessage}";
        LastUpdate = DateTime.Now;
    }
}