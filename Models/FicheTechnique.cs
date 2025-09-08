using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class FicheTechnique
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le nom du produit est requis")]
    [StringLength(200, ErrorMessage = "Le nom du produit ne peut pas dépasser 200 caractères")]
    public string NomProduit { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nom du fabricant est requis")]
    [StringLength(200, ErrorMessage = "Le nom du fabricant ne peut pas dépasser 200 caractères")]
    public string NomFabricant { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le type de produit est requis")]
    [StringLength(100, ErrorMessage = "Le type de produit ne peut pas dépasser 100 caractères")]
    public string TypeProduit { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
    public string Description { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    public int? ChantierId { get; set; }
    public virtual Chantier? Chantier { get; set; }
    
    public virtual ICollection<ImportPDF> ImportsPDF { get; set; } = new List<ImportPDF>();
}