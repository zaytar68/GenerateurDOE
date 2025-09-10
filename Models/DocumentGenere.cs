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
    
    [Required]
    public int ChantierId { get; set; }
    public virtual Chantier Chantier { get; set; } = null!;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
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