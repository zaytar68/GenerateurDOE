using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class FTElement
{
    public int Id { get; set; }
    
    [StringLength(100, ErrorMessage = "La position marché ne peut pas dépasser 100 caractères")]
    public string? PositionMarche { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Le numéro de page doit être un nombre positif")]
    public int? NumeroPage { get; set; }
    
    [Required(ErrorMessage = "L'ordre est requis")]
    [Range(1, int.MaxValue, ErrorMessage = "L'ordre doit être un nombre positif")]
    public int Ordre { get; set; } = 1;

    [StringLength(500, ErrorMessage = "Le commentaire ne peut pas dépasser 500 caractères")]
    public string? Commentaire { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Le conteneur FT est requis")]
    public int FTConteneursId { get; set; }
    public virtual FTConteneur FTConteneur { get; set; } = null!;
    
    [Required(ErrorMessage = "La fiche technique est requise")]
    public int FicheTechniqueId { get; set; }
    public virtual FicheTechnique FicheTechnique { get; set; } = null!;
    
    public int? ImportPDFId { get; set; }
    public virtual ImportPDF? ImportPDF { get; set; }
}