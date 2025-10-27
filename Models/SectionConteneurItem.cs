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

    // 🆕 Champs de personnalisation (nullable = utilise la valeur par défaut de SectionLibre)
    [StringLength(200, ErrorMessage = "Le titre personnalisé ne peut pas dépasser 200 caractères")]
    public string? TitrePersonnalise { get; set; }

    [StringLength(int.MaxValue, ErrorMessage = "Le contenu HTML personnalisé est trop volumineux")]
    public string? ContenuHtmlPersonnalise { get; set; }

    public DateTime? DateModificationPersonnalisation { get; set; }

    // 🆕 Méthodes helper pour accéder au contenu effectif (personnalisé ou par défaut)
    public string GetTitreEffectif() => TitrePersonnalise ?? SectionLibre?.Titre ?? string.Empty;

    public string GetContenuEffectif() => ContenuHtmlPersonnalise ?? SectionLibre?.ContenuHtml ?? string.Empty;

    public bool EstPersonnalise => !string.IsNullOrEmpty(ContenuHtmlPersonnalise);
}