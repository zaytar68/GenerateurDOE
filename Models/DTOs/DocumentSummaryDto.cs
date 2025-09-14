namespace GenerateurDOE.Models.DTOs;

/// <summary>
/// DTO legacy pour compatibilité avec l'ancienne interface
/// À migrer progressivement vers DocumentListDto
/// </summary>
public class DocumentSummaryDto
{
    public int Id { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public string ChantierNom { get; set; } = string.Empty;
    public string ChantierAdresse { get; set; } = string.Empty;
    public string ChantierLot { get; set; } = string.Empty;
    public string NumeroLot { get; set; } = string.Empty;
    public string IntituleLot { get; set; } = string.Empty;
    public TypeDocumentGenere TypeDocument { get; set; }
    public FormatExport FormatExport { get; set; }
    public DateTime DateCreation { get; set; }
    public bool EnCours { get; set; }
    public bool IncludePageDeGarde { get; set; }
    public bool IncludeTableMatieres { get; set; }
    public int NombreSections { get; set; }
    public int NombreFichesTechniques { get; set; }
}