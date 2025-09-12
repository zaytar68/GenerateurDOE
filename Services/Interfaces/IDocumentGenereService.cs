using GenerateurDOE.Models;

namespace GenerateurDOE.Services.Interfaces;

public interface IDocumentGenereService
{
    Task<string> ExportDocumentAsync(int chantierId, TypeDocumentGenere typeDocument, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDOEAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateDossierTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<string> GenerateMemoireTechniqueAsync(int chantierId, FormatExport format, bool includePageDeGarde = true, bool includeTableMatieres = true);
    Task<DocumentGenere> SaveDocumentGenereAsync(DocumentGenere documentGenere);
    Task<DocumentGenere> GetByIdAsync(int documentGenereId);
    Task<DocumentGenere> UpdateAsync(DocumentGenere documentGenere);
    Task<DocumentGenere> DuplicateAsync(int documentId, string newName);
    Task<IEnumerable<DocumentGenere>> GetDocumentsGeneresByChantierId(int chantierId);
    Task<bool> DeleteDocumentGenereAsync(int documentGenereId);

    Task<SectionConteneur> CreateSectionConteneurAsync(int documentGenereId, int typeSectionId, string? titre = null);
    Task<SectionConteneur> GetSectionConteneurAsync(int documentGenereId, int typeSectionId);
    Task<IEnumerable<SectionConteneur>> GetSectionsConteneursByDocumentAsync(int documentGenereId);
    Task<bool> DeleteSectionConteneurAsync(int sectionConteneurId);

    Task<FTConteneur> CreateFTConteneurAsync(int documentGenereId, string? titre = null);
    Task<FTConteneur?> GetFTConteneurByDocumentAsync(int documentGenereId);
    Task<FTConteneur> UpdateFTConteneurAsync(FTConteneur ftConteneur);
    Task<bool> DeleteFTConteneurAsync(int ftConteneursId);

    Task<DocumentGenere> FinalizeDocumentAsync(int documentGenereId);
    Task<bool> CanFinalizeDocumentAsync(int documentGenereId);
    
    Task<byte[]> GenerateCompletePdfAsync(int documentGenereId, PdfGenerationOptions? options = null);
    Task<string> SavePdfAsync(byte[] pdfBytes, string fileName);
    
    Task<List<DocumentGenere>> GetAllDocumentsEnCoursAsync();
}