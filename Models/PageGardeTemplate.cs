using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class PageGardeTemplate
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom du template est requis")]
    [StringLength(100, ErrorMessage = "Le nom du template ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le contenu HTML est requis")]
    [StringLength(int.MaxValue, ErrorMessage = "Le contenu HTML est trop volumineux")]
    public string ContenuHtml { get; set; } = string.Empty;

    [StringLength(int.MaxValue, ErrorMessage = "Le contenu JSON est trop volumineux")]
    public string ContenuJson { get; set; } = string.Empty;

    public bool EstParDefaut { get; set; } = false;

    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;
}