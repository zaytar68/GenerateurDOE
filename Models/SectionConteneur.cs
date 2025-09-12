using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class SectionConteneur
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "L'ordre est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ordre doit être un nombre positif")]
    public int Ordre { get; set; } = 1;
    
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
    public string Titre { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Le document généré est requis")]
    public int DocumentGenereId { get; set; }
    public virtual DocumentGenere DocumentGenere { get; set; } = null!;
    
    [Required(ErrorMessage = "Le type de section est requis")]
    public int TypeSectionId { get; set; }
    public virtual TypeSection TypeSection { get; set; } = null!;
    
    public virtual ICollection<SectionConteneurItem> Items { get; set; } = new List<SectionConteneurItem>();
    
    // Propriété de convenance pour accéder aux sections libres ordonnées
    public IEnumerable<SectionLibre> SectionsLibresOrdered => Items.OrderBy(i => i.Ordre).Select(i => i.SectionLibre);
    
    // TEMPORAIRE : Propriété de compatibilité pour permettre compilation pendant migration
    [System.Obsolete("Utilisez Items ou SectionsLibresOrdered à la place - sera supprimée en Phase 2")]
    public ICollection<SectionLibre> SectionsLibres => Items.Select(i => i.SectionLibre).ToList();
}