using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class ImportPDF
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le chemin du fichier est requis")]
    [StringLength(500, ErrorMessage = "Le chemin du fichier ne peut pas dépasser 500 caractères")]
    public string CheminFichier { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nom du fichier original est requis")]
    [StringLength(255, ErrorMessage = "Le nom du fichier ne peut pas dépasser 255 caractères")]
    public string NomFichierOriginal { get; set; } = string.Empty;

    public int TypeDocumentImportId { get; set; }
    public virtual TypeDocumentImport TypeDocumentImport { get; set; } = null!;
    
    public long TailleFichier { get; set; }
    
    public DateTime DateImport { get; set; } = DateTime.Now;
    
    [Required]
    public int FicheTechniqueId { get; set; }
    public virtual FicheTechnique FicheTechnique { get; set; } = null!;
}

