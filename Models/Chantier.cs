using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class Chantier
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le nom du projet est requis")]
    [StringLength(200, ErrorMessage = "Le nom du projet ne peut pas dépasser 200 caractères")]
    public string NomProjet { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le maître d'œuvre est requis")]
    [StringLength(200, ErrorMessage = "Le nom du maître d'œuvre ne peut pas dépasser 200 caractères")]
    public string MaitreOeuvre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le maître d'ouvrage est requis")]
    [StringLength(200, ErrorMessage = "Le nom du maître d'ouvrage ne peut pas dépasser 200 caractères")]
    public string MaitreOuvrage { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'adresse est requise")]
    [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
    public string Adresse { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    public bool EstArchive { get; set; } = false;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
    public virtual ICollection<DocumentGenere> DocumentsGeneres { get; set; } = new List<DocumentGenere>();
}