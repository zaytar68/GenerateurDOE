using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class SectionLibre
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le titre de la section est requis")]
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
    public string Titre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'ordre est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ordre doit être un nombre positif")]
    public int Ordre { get; set; } = 1;
    
    [StringLength(int.MaxValue, ErrorMessage = "Le contenu HTML est trop volumineux")]
    public string ContenuHtml { get; set; } = string.Empty;
    
    [StringLength(int.MaxValue, ErrorMessage = "Le contenu JSON est trop volumineux")]
    public string ContenuJson { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Le type de section est requis")]
    public int TypeSectionId { get; set; }
    public virtual TypeSection TypeSection { get; set; } = null!;
    public virtual ICollection<SectionConteneurItem> ConteneurItems { get; set; } = new List<SectionConteneurItem>();
}