using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class DocumentGenere
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le type de document est requis")]
    public TypeDocumentGenere TypeDocument { get; set; }
    
    [Required(ErrorMessage = "Le format d'export est requis")]
    public FormatExport FormatExport { get; set; }
    
    [Required(ErrorMessage = "Le nom du fichier est requis")]
    [StringLength(255, ErrorMessage = "Le nom du fichier ne peut pas dépasser 255 caractères")]
    public string NomFichier { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Le chemin du fichier ne peut pas dépasser 500 caractères")]
    public string CheminFichier { get; set; } = string.Empty;
    
    [StringLength(2000, ErrorMessage = "Les paramètres ne peuvent pas dépasser 2000 caractères")]
    public string Parametres { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    
    public bool IncludePageDeGarde { get; set; } = true;
    public bool IncludeTableMatieres { get; set; } = true;
    
    public bool EnCours { get; set; } = true;
    
    [Required(ErrorMessage = "Le numéro de lot est requis")]
    [StringLength(50, ErrorMessage = "Le numéro de lot ne peut pas dépasser 50 caractères")]
    public string NumeroLot { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'intitulé du lot est requis")]
    [StringLength(300, ErrorMessage = "L'intitulé du lot ne peut pas dépasser 300 caractères")]
    public string IntituleLot { get; set; } = string.Empty;
    
    [Required]
    public int ChantierId { get; set; }
    public virtual Chantier Chantier { get; set; } = null!;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
    
    public virtual ICollection<SectionConteneur> SectionsConteneurs { get; set; } = new List<SectionConteneur>();
    
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