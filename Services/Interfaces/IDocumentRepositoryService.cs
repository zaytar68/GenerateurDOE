using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentRepositoryService
{
    // CRUD optimisé avec projections DTO
    Task<DocumentGenere> GetByIdAsync(int documentId);
    Task<DocumentGenere> GetWithCompleteContentAsync(int documentId);
    Task<DocumentSummaryDto> GetSummaryAsync(int documentId);
    Task<List<DocumentSummaryDto>> GetDocumentSummariesAsync();
    Task<List<DocumentSummaryDto>> GetDocumentSummariesByChantierId(int chantierId);
    
    // Pagination optimisée
    Task<PagedResult<DocumentSummaryDto>> GetPagedDocumentsAsync(int page, int pageSize, string? searchTerm = null);
    Task<PagedResult<DocumentSummaryDto>> GetPagedDocumentsByChantierId(int chantierId, int page, int pageSize);
    
    // CRUD operations
    Task<DocumentGenere> CreateAsync(DocumentGenere document);
    Task<DocumentGenere> UpdateAsync(DocumentGenere document);
    Task<bool> DeleteAsync(int documentId);
    Task<DocumentGenere> DuplicateAsync(int documentId, string newName);
    
    // Queries spécialisées
    Task<List<DocumentGenere>> GetDocumentsEnCoursAsync();
    Task<bool> ExistsAsync(int documentId);
    Task<bool> CanFinalizeAsync(int documentId);
}

// DTOs de projection pour optimiser les transferts
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

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class DocumentContentDto
{
    public int Id { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public ChantierSummaryDto Chantier { get; set; } = null!;
    public TypeDocumentGenere TypeDocument { get; set; }
    public FormatExport FormatExport { get; set; }
    public bool IncludePageDeGarde { get; set; }
    public bool IncludeTableMatieres { get; set; }
    public List<SectionConteneurSummaryDto> Sections { get; set; } = new();
    public FTConteneurSummaryDto? FTConteneur { get; set; }
}

public class ChantierSummaryDto  
{
    public int Id { get; set; }
    public string NomProjet { get; set; } = string.Empty;
    public string MaitreOeuvre { get; set; } = string.Empty;
    public string MaitreOuvrage { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
}

public class SectionConteneurSummaryDto
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public string TypeSectionNom { get; set; } = string.Empty;
    public int NombreItems { get; set; }
}

public class FTConteneurSummaryDto
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public int NombreElements { get; set; }
    public bool AfficherTableauRecapitulatif { get; set; }
}