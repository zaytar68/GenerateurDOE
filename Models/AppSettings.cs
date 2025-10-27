using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GenerateurDOE.Models;

public class AppSettings
{
    [Required(ErrorMessage = "Le répertoire de stockage PDF est requis")]
    [StringLength(500, ErrorMessage = "Le chemin ne peut pas dépasser 500 caractères")]
    public string RepertoireStockagePDF { get; set; } = "C:\\GenerateurDOE\\Documents\\PDF";

    [Required(ErrorMessage = "Le répertoire de stockage des images est requis")]
    [StringLength(500, ErrorMessage = "Le chemin ne peut pas dépasser 500 caractères")]
    public string RepertoireStockageImages { get; set; } = "C:\\GenerateurDOE\\Documents\\Images";

    [Required(ErrorMessage = "Le nom de la société est requis")]
    [StringLength(200, ErrorMessage = "Le nom de la société ne peut pas dépasser 200 caractères")]
    public string NomSociete { get; set; } = "Multisols";

    [Required(ErrorMessage = "La taille maximale des fichiers PDF est requise")]
    [RegularExpression(@"^\d+(\.\d+)?(KB|MB|GB)$", ErrorMessage = "Format invalide. Utilisez : 50MB, 100KB, 2GB")]
    public string TailleMaxFichierPDF { get; set; } = "50MB";

    /// <summary>
    /// Version de l'application extraite automatiquement depuis l'assembly .NET
    /// Source unique de vérité : GenerateurDOE.csproj <Version>
    /// Format: Major.Minor.Patch (ex: 2.1.7)
    /// </summary>
    public string ApplicationVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

    /// <summary>
    /// Configuration des styles PDF pour la génération de documents
    /// </summary>
    public PdfStylesConfig StylesPDF { get; set; } = new();
}

/// <summary>
/// Configuration des styles pour la génération de documents PDF
/// Permet de personnaliser l'apparence des sections libres et autres éléments
/// </summary>
public class PdfStylesConfig
{
    /// <summary>
    /// Facteur d'échelle de la taille des polices (0.5 = 50%, 1.0 = 100%, 1.5 = 150%)
    /// </summary>
    [Range(0.5, 2.0, ErrorMessage = "L'échelle doit être entre 0.5 et 2.0")]
    public float FontSizeScale { get; set; } = 0.8f; // 80% de la taille par défaut

    /// <summary>
    /// Couleur des titres principaux (format hexadécimal #RRGGBB)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Format couleur invalide. Utilisez #RRGGBB")]
    public string TitleColor { get; set; } = "#2c3e50";

    /// <summary>
    /// Couleur des sous-titres (format hexadécimal #RRGGBB)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Format couleur invalide. Utilisez #RRGGBB")]
    public string SubtitleColor { get; set; } = "#2980b9";

    /// <summary>
    /// Couleur du texte principal (format hexadécimal #RRGGBB)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Format couleur invalide. Utilisez #RRGGBB")]
    public string TextColor { get; set; } = "#333333";

    /// <summary>
    /// Couleur des bordures et séparateurs (format hexadécimal #RRGGBB)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Format couleur invalide. Utilisez #RRGGBB")]
    public string BorderColor { get; set; } = "#3498db";

    /// <summary>
    /// Espacement des lignes (1.0 = normal, 1.2 = plus aéré, 1.6 = très aéré)
    /// </summary>
    [Range(1.0, 2.0, ErrorMessage = "L'interligne doit être entre 1.0 et 2.0")]
    public float LineHeight { get; set; } = 1.6f;

    /// <summary>
    /// Template prédéfini sélectionné (Compact, Normal, Large)
    /// </summary>
    public string TemplatePredefini { get; set; } = "Normal";

    /// <summary>
    /// Calcule la taille de police de base selon l'échelle configurée
    /// </summary>
    public float GetBaseFontSize() => 14f * FontSizeScale;

    /// <summary>
    /// Calcule la taille de police pour l'impression selon l'échelle configurée
    /// </summary>
    public float GetPrintFontSize() => 12f * FontSizeScale;
}