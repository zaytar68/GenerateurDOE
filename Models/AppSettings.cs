using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class AppSettings
{
    [Required(ErrorMessage = "Le répertoire de stockage PDF est requis")]
    [StringLength(500, ErrorMessage = "Le chemin ne peut pas dépasser 500 caractères")]
    public string RepertoireStockagePDF { get; set; } = "C:\\GenerateurDOE\\Documents\\PDF";

    [Required(ErrorMessage = "Le répertoire de stockage des images est requis")]
    [StringLength(500, ErrorMessage = "Le chemin ne peut pas dépasser 500 caractères")]
    public string RepertoireStockageImages { get; set; } = "C:\\GenerateurDOE\\Documents\\Images";

    [Required(ErrorMessage = "Le nom de la société est requis")]
    [StringLength(200, ErrorMessage = "Le nom de la société ne peut pas dépasser 200 caractères")]
    public string NomSociete { get; set; } = "Votre Société";

    [Required(ErrorMessage = "La taille maximale des fichiers PDF est requise")]
    [RegularExpression(@"^\d+(\.\d+)?(KB|MB|GB)$", ErrorMessage = "Format invalide. Utilisez : 50MB, 100KB, 2GB")]
    public string TailleMaxFichierPDF { get; set; } = "50MB";
    
    public string ApplicationVersion { get; set; } = "0.1";
}