using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class SectionConteneurItem
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "L'ordre est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ordre doit être un nombre positif")]
    public int Ordre { get; set; } = 1;
    
    public DateTime DateAjout { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Le conteneur est requis")]
    public int SectionConteneursId { get; set; }
    public virtual SectionConteneur SectionConteneur { get; set; } = null!;
    
    [Required(ErrorMessage = "La section libre est requise")]
    public int SectionLibreId { get; set; }
    public virtual SectionLibre SectionLibre { get; set; } = null!;
}