using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class TypeProduit
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le nom du type de produit est requis")]
    [StringLength(100, ErrorMessage = "Le nom du type de produit ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
}