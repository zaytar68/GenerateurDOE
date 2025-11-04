using System.ComponentModel.DataAnnotations;

namespace GenerateurDOE.Models;

public class TypeDocumentImport
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le nom du type de document est requis")]
    [StringLength(100, ErrorMessage = "Le nom du type de document ne peut pas dépasser 100 caractères")]
    public string Nom { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime DateModification { get; set; } = DateTime.Now;

    // Navigation property : documents PDF utilisant ce type
    public virtual ICollection<ImportPDF> ImportsPDF { get; set; } = new List<ImportPDF>();
}