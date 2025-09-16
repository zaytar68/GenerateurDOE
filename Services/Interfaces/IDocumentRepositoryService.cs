using GenerateurDOE.Models;
using GenerateurDOE.Models.DTOs;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentRepositoryService
{
    // CRUD optimisé avec projections DTO
    Task<DocumentGenere> GetByIdAsync(int documentId);
    Task<DocumentGenere> GetWithCompleteContentAsync(int documentId);
    Task<DocumentSummaryDto> GetSummaryAsync(int documentId);
    Task<List<DocumentSummaryDto>> GetDocumentSummariesAsync();
    Task<List<DocumentSummaryDto>> GetDocumentSummariesByChantierId(int chantierId);
    
    // ⚡ NOUVELLES MÉTHODES OPTIMISÉES PHASE 3 - Performance +30-50%
    Task<PagedResult<DocumentListDto>> GetPagedDocumentsAsync(int page = 1, int pageSize = 20, int? chantierId = null);
    Task<PagedResult<ChantierSummaryDto>> GetPagedChantierSummariesAsync(int page = 1, int pageSize = 20, bool includeArchived = false);
    Task<PagedResult<FicheTechniqueSummaryDto>> GetPagedFicheTechniquesSummariesAsync(int page = 1, int pageSize = 20, string searchTerm = "");
    
    // Pagination legacy (à migrer progressivement)
    Task<PagedResult<DocumentSummaryDto>> GetPagedDocumentsByChantierId(int chantierId, int page, int pageSize);
    
    // CRUD operations
    Task<DocumentGenere> CreateAsync(DocumentGenere document);
    Task<DocumentGenere> UpdateAsync(DocumentGenere document);
    Task<bool> DeleteAsync(int documentId);
    Task<DocumentGenere> DuplicateAsync(int documentId, string newName);
    Task<DocumentGenere> DuplicateToChantierAsync(int documentId, string newName, int newChantierId, string numeroLot, string intituleLot);
    
    // Queries spécialisées
    Task<List<DocumentGenere>> GetDocumentsEnCoursAsync();
    Task<bool> ExistsAsync(int documentId);
    Task<bool> CanFinalizeAsync(int documentId);

    // Méthodes pour tests et composants
    Task<DocumentGenere?> GetFirstDocumentAsync();
    Task<DocumentGenere?> GetDocumentWithFTContainerAsync();
}

