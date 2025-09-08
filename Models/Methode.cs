using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class Methode
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
    public string Titre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La description est requise")]
    [StringLength(5000, ErrorMessage = "La description ne peut pas dépasser 5000 caractères")]
    public string Description { get; set; } = string.Empty;
    
    public int OrdreAffichage { get; set; }
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    public virtual ICollection<ImageMethode> Images { get; set; } = new List<ImageMethode>();
}

public class ImageMethode
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le chemin du fichier est requis")]
    [StringLength(500, ErrorMessage = "Le chemin du fichier ne peut pas dépasser 500 caractères")]
    public string CheminFichier { get; set; } = string.Empty;
    
    [StringLength(255, ErrorMessage = "Le nom du fichier ne peut pas dépasser 255 caractères")]
    public string NomFichierOriginal { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string Description { get; set; } = string.Empty;
    
    public int OrdreAffichage { get; set; }
    
    public DateTime DateImport { get; set; } = DateTime.Now;
    
    [Required]
    public int MethodeId { get; set; }
    public virtual Methode Methode { get; set; } = null!;
}