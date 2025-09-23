using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

/// <summary>
/// Représente un chantier de construction avec informations projet et intervenants
/// Les lots sont maintenant gérés au niveau DocumentGenere (Phase 2 Migration)
/// </summary>
public class Chantier
{
    /// <summary>
    /// Identifiant unique du chantier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Nom du projet de construction
    /// </summary>
    [Required(ErrorMessage = "Le nom du projet est requis")]
    [StringLength(200, ErrorMessage = "Le nom du projet ne peut pas dépasser 200 caractères")]
    public string NomProjet { get; set; } = string.Empty;
    
    /// <summary>
    /// Maître d'œuvre responsable du projet
    /// </summary>
    [Required(ErrorMessage = "Le maître d'œuvre est requis")]
    [StringLength(200, ErrorMessage = "Le nom du maître d'œuvre ne peut pas dépasser 200 caractères")]
    public string MaitreOeuvre { get; set; } = string.Empty;
    
    /// <summary>
    /// Maître d'ouvrage propriétaire du projet
    /// </summary>
    [Required(ErrorMessage = "Le maître d'ouvrage est requis")]
    [StringLength(200, ErrorMessage = "Le nom du maître d'ouvrage ne peut pas dépasser 200 caractères")]
    public string MaitreOuvrage { get; set; } = string.Empty;
    
    /// <summary>
    /// Adresse complète du chantier
    /// </summary>
    [Required(ErrorMessage = "L'adresse est requise")]
    [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
    public string Adresse { get; set; } = string.Empty;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Indique si le chantier est archivé (non visible par défaut)
    /// </summary>
    public bool EstArchive { get; set; } = false;
    
    public virtual ICollection<FicheTechnique> FichesTechniques { get; set; } = new List<FicheTechnique>();
    /// <summary>
    /// Documents générés pour ce chantier avec leurs lots spécifiques
    /// </summary>
    public virtual ICollection<DocumentGenere> DocumentsGeneres { get; set; } = new List<DocumentGenere>();
}