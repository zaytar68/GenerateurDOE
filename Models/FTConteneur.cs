using System.ComponentModel.DataAnnotations;
using GenerateurDOE.Services.Interfaces;

namespace GenerateurDOE.Models;

public class FTConteneur : IDocumentSection
{
    public int Id { get; set; }
    
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
    public string Titre { get; set; } = "Fiches Techniques";
    
    [Required(ErrorMessage = "L'ordre est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ordre doit être un nombre positif")]
    public int Ordre { get; set; } = 1;
    
    public bool AfficherTableauRecapitulatif { get; set; } = true;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Le document généré est requis")]
    public int DocumentGenereId { get; set; }
    public virtual DocumentGenere DocumentGenere { get; set; } = null!;
    
    public virtual ICollection<FTElement> Elements { get; set; } = new List<FTElement>();
}