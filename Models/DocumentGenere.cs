using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

/// <summary>
/// Représente un document généré avec son chantier, type, format et informations de lot
/// Les lots sont maintenant gérés au niveau document (Phase 2 Migration)
/// </summary>
public class DocumentGenere
{
    /// <summary>
    /// Identifiant unique du document généré
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Type de document (DOE, Dossier Technique, Mémoire Technique)
    /// </summary>
    [Required(ErrorMessage = "Le type de document est requis")]
    public TypeDocumentGenere TypeDocument { get; set; }
    
    /// <summary>
    /// Format d'export du document (PDF, HTML, Markdown, Word)
    /// </summary>
    [Required(ErrorMessage = "Le format d'export est requis")]
    public FormatExport FormatExport { get; set; }
    
    [Required(ErrorMessage = "Le nom du fichier est requis")]
    [StringLength(255, ErrorMessage = "Le nom du fichier ne peut pas dépasser 255 caractères")]
    public string NomFichier { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Le chemin du fichier ne peut pas dépasser 500 caractères")]
    public string CheminFichier { get; set; } = string.Empty;
    
    [StringLength(10000, ErrorMessage = "Les paramètres ne peuvent pas dépasser 10000 caractères")]
    public string Parametres { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    
    public bool IncludePageDeGarde { get; set; } = true;
    public bool IncludeTableMatieres { get; set; } = true;

    public int? PageGardeTemplateId { get; set; }
    public virtual PageGardeTemplate? PageGardeTemplate { get; set; }
    
    public bool EnCours { get; set; } = true;
    
    [Required(ErrorMessage = "Le numéro de lot est requis")]
    [StringLength(50, ErrorMessage = "Le numéro de lot ne peut pas dépasser 50 caractères")]
    public string NumeroLot { get; set; } = string.Empty;
    
    /// <summary>
    /// Intitulé du lot spécifique à ce document (migration Phase 2)
    /// </summary>
    [Required(ErrorMessage = "L'intitulé du lot est requis")]
    [StringLength(300, ErrorMessage = "L'intitulé du lot ne peut pas dépasser 300 caractères")]
    public string IntituleLot { get; set; } = string.Empty;
    
    /// <summary>
    /// Identifiant du chantier parent
    /// </summary>
    [Required]
    public int ChantierId { get; set; }

    /// <summary>
    /// Chantier associé au document
    /// </summary>
    public virtual Chantier Chantier { get; set; } = null!;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
    
    /// <summary>
    /// Conteneurs de sections libres ordonnés par propriété Ordre
    /// </summary>
    public virtual ICollection<SectionConteneur> SectionsConteneurs { get; set; } = new List<SectionConteneur>();
    
    /// <summary>
    /// Conteneur optionnel de fiches techniques pour ce document
    /// </summary>
    public virtual FTConteneur? FTConteneur { get; set; }
}

public enum TypeDocumentGenere
{
    DOE, // Dossier d'Ouvrages Exécutés
    DossierTechnique,
    MemoireTechnique
}

public enum FormatExport
{
    PDF,
    HTML,
    Markdown,
    Word
}